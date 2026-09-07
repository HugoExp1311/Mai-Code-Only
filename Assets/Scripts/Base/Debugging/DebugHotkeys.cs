#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Base.Persistence;
using Base.SexScenes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Base.Debugging
{
    /// <summary>
    /// Debug hotkeys for QA/testing (devlog save_load_25aug.md §5 helpers).
    /// Auto-bootstrapped at runtime — no scene wiring needed. Stripped from release builds.
    /// F7   — Force a daily auto-save (writes to the rolling AutoSave_1..3 pool).
    /// F8   — Delete throwaway slot "99" (cleanup after F9/F12 tests).
    /// F9   — Two-phase manual round-trip: press 1 saves slot 99 + captures baseline;
    ///        mutate state however you like; press 2 loads slot 99 and diffs key stats.
    /// F10  — Skip intro CG, or end the currently active dialogue (skip R18 scenes while testing).
    /// F11  — Unlock every sex scene (persistence test helper; no scene content played).
    /// F12  — One-shot save/load round-trip check on safe slot "99", then clean it up.
    /// </summary>
    public static class DebugHotkeys
    {
        private const string TestSlotId = "99";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameObject host = new GameObject("DebugHotkeys (AUTO)");
            host.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(host);
            host.AddComponent<DebugHotkeysBehaviour>();
        }

        private class DebugHotkeysBehaviour : MonoBehaviour
        {
            private void Update()
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return;

                if (keyboard.f7Key.wasPressedThisFrame) ForceDailyAutoSave();
                if (keyboard.f8Key.wasPressedThisFrame) DeleteTestSlot();
                if (keyboard.f9Key.wasPressedThisFrame) ManualTestSaveLoad();
                if (keyboard.f10Key.wasPressedThisFrame) SkipIntroOrDialogue();
                if (keyboard.f11Key.wasPressedThisFrame) UnlockAllSexScenes();
                if (keyboard.f12Key.wasPressedThisFrame) RunSaveRoundTripCheck();
            }
        }

        private static void SkipIntroOrDialogue()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[DebugHotkeys] GameManager not ready — F10 ignored.");
                return;
            }

            if (gm.GetIsPlayingIntro())
            {
                gm.SkipIntroCG();
                return; // SkipIntroCG ends dialogue itself
            }

            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                DialogueManager.Instance.EndDialogue();
                Debug.Log("[DebugHotkeys] Active dialogue ended via F10.");
            }
        }

        private static void UnlockAllSexScenes()
        {
            int count = 0;
            foreach (var pair in SexSceneUnlockService.GetDefinitions())
            {
                SexSceneUnlockService.SetUnlocked(pair.Key, true);
                count++;
            }
            PlayerPrefs.Save();
            Debug.Log($"[DebugHotkeys] F11: unlocked all {count} sex scenes.");
        }

        private static void RunSaveRoundTripCheck()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[DebugHotkeys] GameManager not ready — F12 ignored.");
                return;
            }

            // Capture + write to a safe throwaway slot (never touches Quick or 1..3).
            SaveData before = gm.CaptureSaveData();
            if (before == null)
            {
                Debug.LogWarning("[DebugHotkeys] F12: no active game to capture — ignored.");
                return;
            }

            SaveLoadService.Write(TestSlotId, before, "F12 round-trip test");

            // Mutate nothing between write and load; restore should reproduce identical state.
            bool loaded = gm.LoadGame(SaveLoadService.Read(TestSlotId));
            if (!loaded)
            {
                Debug.LogError("[DebugHotkeys] F12: FAILED — LoadGame returned false.");
                SaveLoadService.Delete(TestSlotId);
                return;
            }

            SaveData after = gm.CaptureSaveData();
            string jsonBefore = JsonUtility.ToJson(before);
            string jsonAfter = JsonUtility.ToJson(after);
            bool pass = jsonBefore == jsonAfter;

            SaveLoadService.Delete(TestSlotId);
            PlayerPrefs.Save();

            if (pass)
            {
                Debug.Log($"[DebugHotkeys] F12: PASS — SaveData round-trip identical ({jsonAfter.Length} chars JSON).\n{jsonAfter}");
            }
            else
            {
                Debug.LogError($"[DebugHotkeys] F12: FAIL — state differs after load.\nBEFORE: {jsonBefore}\nAFTER:  {jsonAfter}");
            }
        }

        private static SaveData _manualTestBaseline;

        /// <summary>
        /// F9 — two-phase manual round-trip on safe slot "99".
        /// Press 1: write baseline. Mutate game state however you like. Press 2: load & diff.
        /// Uses SaveLoadService directly: GameManager.SaveGame/LoadGame clamp slots to 1..3.
        /// </summary>
        private static void ManualTestSaveLoad()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[DebugHotkeys] GameManager not ready — F9 ignored.");
                return;
            }

            if (_manualTestBaseline == null)
            {
                SaveData baseline = gm.CaptureSaveData();
                if (baseline == null)
                {
                    Debug.LogWarning("[DebugHotkeys] F9: no active game to capture — ignored.");
                    return;
                }

                _manualTestBaseline = baseline;
                SaveLoadService.Write(TestSlotId, baseline, "F9 manual test");
                Debug.Log("[DebugHotkeys] F9 [1/2]: slot 99 saved + baseline captured. Mutate state (gameplay/cheats), then press F9 again to load & compare.");
                return;
            }

            if (!gm.LoadGame(SaveLoadService.Read(TestSlotId)))
            {
                Debug.LogError("[DebugHotkeys] F9 [2/2]: FAILED — LoadGame returned false. Baseline kept; press F9 to retry or F8 to clean up.");
                return;
            }

            SaveData after = gm.CaptureSaveData();
            bool identical = JsonUtility.ToJson(_manualTestBaseline) == JsonUtility.ToJson(after);
            Debug.Log(
                "[DebugHotkeys] F9 [2/2]: slot 99 loaded (kept on disk until F8).\n" +
                $"stamina {_manualTestBaseline.stamina}->{after.stamina} | money {_manualTestBaseline.money}->{after.money} | love {_manualTestBaseline.love}->{after.love}\n" +
                $"skillPoint {_manualTestBaseline.skillPoint}->{after.skillPoint} | workLevel {_manualTestBaseline.workLevel}->{after.workLevel} | workProgress {_manualTestBaseline.workProgress}->{after.workProgress}\n" +
                $"inventory {_manualTestBaseline.inventory.Count}->{after.inventory.Count} items | time {_manualTestBaseline.time} -> {after.time}\n" +
                (identical
                    ? "JSON identical to baseline: PASS"
                    : "JSON MISMATCH — some state did not round-trip; press F12 for a full JSON diff."));
            _manualTestBaseline = null;
        }

        /// <summary>F7 — force a daily auto-save for testing the rolling auto-save pool.</summary>
        private static void ForceDailyAutoSave()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[DebugHotkeys] GameManager not ready — F7 ignored.");
                return;
            }

            bool saved = gm.DailyAutoSave();
            if (saved)
            {
                Debug.Log("[DebugHotkeys] F7: DailyAutoSave completed.");
            }
            else
            {
                Debug.LogWarning("[DebugHotkeys] F7: DailyAutoSave returned false (no active game?).");
            }
        }

        /// <summary>F8 — delete throwaway slot "99" and reset any pending F9 baseline.</summary>
        private static void DeleteTestSlot()
        {
            _manualTestBaseline = null;
            SaveLoadService.Delete(TestSlotId);
            PlayerPrefs.Save();
            Debug.Log($"[DebugHotkeys] F8: test slot '{TestSlotId}' deleted. HasSave(99) = {SaveLoadService.HasSave(TestSlotId)}");
        }
    }
}
#endif

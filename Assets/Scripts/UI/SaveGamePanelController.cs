using System.Collections.Generic;
using System.Text.RegularExpressions;
using Base.Localization;
using Base.Persistence;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the In-Game "Save Game Popup" (devlog save_load_25aug TODO #3, GDD §3 Save (Number)).
///
/// Expected row layout under a child named "Saves" (6 rows in a 2x3 grid):
///   rows 0..2 -> auto-save slots (AutoSave_1..3), display-only (saving is automatic per day)
///   rows 3..5 -> manual slots 1..3
///
/// Clicking an empty manual slot saves immediately; clicking an occupied manual slot asks
/// "Overwrite this save?" through the Confirm Popup. Both paths show a notice and refresh
/// the slot labels. Auto-save rows are non-interactive (greyed out).
/// </summary>
public class SaveGamePanelController : MonoBehaviour
{
    private const string ConfirmPopupPanelId = "Confirm Popup";
    private const string SaveNumberLabelKey = "Start Load Game Save No"; // same "Save {n}" label as the Load Game popup
    private const string SlotInfoLabelKey = "Start Load Game Slot Info";
    private const string OverwriteQuestionKey = "Start Save Game Overwrite Question";
    private const string OverwriteYesKey = "Start Save Game Overwrite Yes";
    private const string OverwriteNoKey = "Start Confirm No";
    private const string NoticeGameSavedKey = "Notice Game Saved";
    private const string NoticeSaveFailedKey = "Notice Save Failed";
    private const string AutoSaveLabelKey = "Start Save Game Auto Save";
    private const string SavesChildPath = "Saves";

    // Save labels are written by GameManager as "Day {DaysPlayed} - {HH:mm}"
    // (manual) or "AutoSave Day {DaysPlayed} - {HH:mm}" (auto-save).
    private static readonly Regex SlotLabelPattern = new Regex(@"(?:AutoSave\s+)?Day\s+(\d+)\s*-\s*(\d{1,2}:\d{2})", RegexOptions.Compiled);

    private readonly List<SlotRow> rows = new List<SlotRow>();

    private UIPanel panel;

    /// <summary>Depth-first search for a descendant transform by name ("Saves" sits under an intermediate content node).</summary>
    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private sealed class SlotRow
    {
        public Transform RowTransform;
        public Button SlotButton;
        public LocalizedText Label;
        public string SlotId;
        public int ManualSlotNumber;
        public bool IsAutoSave;
    }

    private void Awake()
    {
        panel = GetComponent<UIPanel>();
        if (panel != null)
        {
            panel.OnShown.AddListener(Refresh);
        }

        BindRows();
        Refresh();
    }

    private void OnDestroy()
    {
        if (panel != null)
        {
            panel.OnShown.RemoveListener(Refresh);
        }
    }

    private void BindRows()
    {
        rows.Clear();

        Transform saves = FindDeep(transform, SavesChildPath);
        if (saves == null)
        {
            Debug.LogError($"[SaveGamePanelController] '{SavesChildPath}' child not found under '{gameObject.name}'.");
            return;
        }

        for (int i = 0; i < saves.childCount; i++)
        {
            Transform rowTransform = saves.GetChild(i);

            // Layout: 6 rows in a 2x3 grid.
            //   rows 0..2 -> auto-save slots (AutoSave_1..3), display-only
            //   rows 3..5 -> manual slots 1..3
            bool isAutoRow = i < SaveLoadService.AutoSlotCount;
            int autoIndex = i + 1; // 1-based
            int manualSlotNumber = i - SaveLoadService.AutoSlotCount + 1; // 1-based

            if (!isAutoRow && manualSlotNumber > SaveLoadService.SlotCount)
            {
                rowTransform.gameObject.SetActive(false);
                continue;
            }

            // Ensure all usable rows are active (row 0 may have been hidden by legacy layout).
            rowTransform.gameObject.SetActive(true);

            var row = new SlotRow
            {
                RowTransform = rowTransform,
                SlotButton = rowTransform.GetComponent<Button>(),
                SlotId = isAutoRow ? SaveLoadService.GetAutoSlotId(autoIndex) : manualSlotNumber.ToString(),
                ManualSlotNumber = isAutoRow ? 0 : manualSlotNumber,
                IsAutoSave = isAutoRow
            };

            if (row.SlotButton == null)
            {
                Debug.LogError($"[SaveGamePanelController] Row '{rowTransform.name}' has no Button.");
                continue;
            }

            row.Label = rowTransform.childCount > 0 ? rowTransform.GetChild(0).GetComponent<LocalizedText>() : null;
            if (row.Label == null)
            {
                Debug.LogWarning($"[SaveGamePanelController] Row '{rowTransform.name}' has no LocalizedText label.");
            }

            // Auto-save rows are display-only (saving is automatic per day).
            // Manual rows are clickable to save.
            if (isAutoRow)
            {
                row.SlotButton.interactable = false;
            }
            else
            {
                row.SlotButton.onClick.AddListener(() => HandleSlotClicked(row));
            }

            rows.Add(row);
        }
    }

    /// <summary>Public so edit-mode verification can rebuild the slot list without playing.</summary>
    public void Refresh()
    {
        if (rows.Count == 0)
        {
            BindRows();
        }

        foreach (SlotRow row in rows)
        {
            bool hasSave = SaveLoadService.HasSave(row.SlotId);
            SaveLoadService.SaveSlotMeta meta = SaveLoadService.ReadMeta(row.SlotId);

            if (row.Label == null) continue;

            if (hasSave && meta != null && TryGetSlotDisplay(meta, out string day, out string time))
            {
                row.Label.SetLocalized(SlotInfoLabelKey, LocalizationDomains.UI, new Dictionary<string, object>
                {
                    ["day"] = day,
                    ["time"] = time
                });
            }
            else if (row.IsAutoSave)
            {
                row.Label.SetLocalized(AutoSaveLabelKey, LocalizationDomains.UI);
            }
            else
            {
                row.Label.SetLocalized(SaveNumberLabelKey, LocalizationDomains.UI, new Dictionary<string, object>
                {
                    ["0"] = row.ManualSlotNumber.ToString()
                });
            }
        }
    }

    private static bool TryGetSlotDisplay(SaveLoadService.SaveSlotMeta meta, out string day, out string time)
    {
        day = null;
        time = null;

        if (meta == null) return false;

        Match match = SlotLabelPattern.Match(meta.label ?? string.Empty);
        if (match.Success)
        {
            day = match.Groups[1].Value;
            time = match.Groups[2].Value;
            return true;
        }

        // Fallback for legacy/corrupt labels: meta.gameDay (0 when not filled) + real timestamp.
        if (meta.gameDay > 0 || !string.IsNullOrEmpty(meta.realTimeStamp))
        {
            day = meta.gameDay > 0 ? meta.gameDay.ToString() : "-";
            time = !string.IsNullOrEmpty(meta.realTimeStamp) ? meta.realTimeStamp : "-";
            return true;
        }

        return false;
    }

    private void HandleSlotClicked(SlotRow row)
    {
        if (!SaveLoadService.HasSave(row.SlotId))
        {
            DoSave(row);
            return;
        }

        var confirmData = ConfirmPopupData.CreateLocalized(
            OverwriteQuestionKey,
            null,
            OverwriteYesKey,
            () => DoSave(row),
            true,
            null,
            OverwriteNoKey);

        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.ShowPanel(ConfirmPopupPanelId, confirmData);
        }
    }

    private void DoSave(SlotRow row)
    {
        GameManager gameManager = GameManager.Instance;
        bool saved = gameManager != null && gameManager.SaveGame(row.ManualSlotNumber);

        NoticeUI.ShowLocalized(saved ? NoticeGameSavedKey : NoticeSaveFailedKey);
        Refresh();
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Base;
using Base.Localization;
using Base.Persistence;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller for the Main Menu "Load Game Popup" (TaskTodo P1.1, devlog save_load_25aug TODO #1, GDD §3).
///
/// Expected row layout under a child named "Saves" (6 rows in a 2x3 grid):
///   rows 0..2 -> auto-save slots "AutoSave_1".."AutoSave_3" (daily auto-saves)
///   rows 3..5 -> manual slots "1".."3"
///
/// Occupied rows show "Day X - HH:mm" parsed from the SaveSlotMeta label, empty rows keep their
/// localized "Auto Save"/"Save {n}" label. Clicking an occupied row asks "Start from here?"
/// through the Confirm Popup; each occupied row also gets a small delete button with its own
/// confirmation. Clicking an empty row shows a notice.
/// </summary>
public class LoadGamePanelController : MonoBehaviour
{
    private const string ConfirmPopupPanelId = "Confirm Popup";
    private const string SaveNumberLabelKey = "Start Load Game Save No";
    private const string SlotInfoLabelKey = "Start Load Game Slot Info";
    private const string LoadQuestionKey = "Start Confirm Question";
    private const string LoadYesKey = "Start Confirm Yes";
    private const string LoadNoKey = "Start Confirm No";
    private const string DeleteQuestionKey = "Start Load Game Delete Question";
    private const string DeleteYesKey = "Start Load Game Delete Yes";
    private const string DeleteNoKey = "Start Confirm No";
    private const string NoticeLoadFailedKey = "Notice Load Failed";
    private const string NoticeSlotEmptyKey = "Notice Save Slot Empty";
    private const string AutoSaveLabelKey = "Start Save Game Auto Save";
    private const string SavesChildPath = "Saves";
    private const string DeleteButtonName = "Delete Button";

    // Save labels are written by GameManager as "Day {DaysPlayed} - {HH:mm}"
    // (manual/quick) or "AutoSave Day {DaysPlayed} - {HH:mm}" (auto-save).
    private static readonly Regex SlotLabelPattern = new Regex(@"(?:AutoSave\s+)?Day\s+(\d+)\s*-\s*(\d{1,2}:\d{2})", RegexOptions.Compiled);

    private readonly List<SlotRow> rows = new List<SlotRow>();

    private UIPanel panel;

    /// <summary>
    /// Loading a save mid-run is not supported; only the Main Menu copy of this popup
    /// (section MainMenu) may load. The in-game copy shows greyed-out slot buttons.
    /// Derived in Awake from the panel's GameSection.
    /// </summary>
    private bool allowLoad = true;

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
        public Button DeleteButton;
        public string SlotId;
        public int ManualSlotNumber; // 0 for auto-save rows
        public bool IsAutoSave;
    }

    private void Awake()
    {
        panel = GetComponent<UIPanel>();
       // allowLoad = panel == null || panel.GameSection == GameSection.MainMenu;
        allowLoad = true;
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
            Debug.LogError($"[LoadGamePanelController] '{SavesChildPath}' child not found under '{gameObject.name}'.");
            return;
        }

        for (int i = 0; i < saves.childCount; i++)
        {
            Transform rowTransform = saves.GetChild(i);

            // Layout: 6 rows in a 2x3 grid.
            //   rows 0..2 -> auto-save slots (AutoSave_1..3)
            //   rows 3..5 -> manual slots "1".."3"
            bool isAutoRow = i < SaveLoadService.AutoSlotCount;
            int autoIndex = i + 1; // 1-based
            int manualSlotNumber = i - SaveLoadService.AutoSlotCount + 1; // 1-based

            bool isUsable = isAutoRow || manualSlotNumber <= SaveLoadService.SlotCount;
            rowTransform.gameObject.SetActive(isUsable);
            if (!isUsable) continue;

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
                Debug.LogError($"[LoadGamePanelController] Row '{rowTransform.name}' has no Button.");
                continue;
            }

            row.Label = rowTransform.childCount > 0 ? rowTransform.GetChild(0).GetComponent<LocalizedText>() : null;
            if (row.Label == null)
            {
                Debug.LogWarning($"[LoadGamePanelController] Row '{rowTransform.name}' has no LocalizedText label.");
            }

            row.SlotButton.onClick.AddListener(() => HandleSlotClicked(row));
            row.DeleteButton = EnsureDeleteButton(rowTransform, row);
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

            if (row.Label != null)
            {
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

            // Mid-run loading is disabled: grey out slot buttons in the In-Game copy
            // (ColorTint transition picks up disabledColor automatically).
            if (row.SlotButton != null)
            {
                row.SlotButton.interactable = allowLoad;
            }

            if (row.DeleteButton != null)
            {
                row.DeleteButton.gameObject.SetActive(hasSave);
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
        if (!allowLoad) return; // In-Game copy: loading is blocked.

        if (!SaveLoadService.HasSave(row.SlotId))
        {
            NoticeUI.ShowLocalized(NoticeSlotEmptyKey);
            return;
        }

        var confirmData = ConfirmPopupData.CreateLocalized(
            LoadQuestionKey,
            null,
            LoadYesKey,
            () => BeginLoad(row),
            true,
            null,
            LoadNoKey);

        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.ShowPanel(ConfirmPopupPanelId, confirmData);
        }
    }

    private void BeginLoad(SlotRow row)
    {
        if (!allowLoad) return; // In-Game copy: loading is blocked.

        if (panel != null)
        {
            panel.Hide();
        }

        GameManager gameManager = GameManager.Instance;
        bool loaded;
        if (gameManager == null)
        {
            loaded = false;
        }
        else if (row.IsAutoSave)
        {
            int autoIndex = int.Parse(row.SlotId.Substring(SaveLoadService.AutoSlotPrefix.Length));
            loaded = gameManager.LoadAutoSave(autoIndex);
        }
        else
        {
            loaded = gameManager.LoadGame(row.ManualSlotNumber);
        }

        if (!loaded)
        {
            NoticeUI.ShowLocalized(NoticeLoadFailedKey);
        }
    }

    private void HandleDeleteClicked(SlotRow row)
    {
        PlayButtonSound();

        var confirmData = ConfirmPopupData.CreateLocalized(
            DeleteQuestionKey,
            null,
            DeleteYesKey,
            () =>
            {
                SaveLoadService.Delete(row.SlotId);
                Refresh();
            },
            true,
            null,
            DeleteNoKey);

        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.ShowPanel(ConfirmPopupPanelId, confirmData);
        }
    }

    /// <summary>Creates (or reuses) a small "×" delete button in the top-right corner of a row.</summary>
    private Button EnsureDeleteButton(Transform rowTransform, SlotRow row)
    {
        Transform existing = rowTransform.Find(DeleteButtonName);
        GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(DeleteButtonName, typeof(RectTransform), typeof(Image), typeof(Button));

        if (existing == null)
        {
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.SetParent(rowTransform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -10f);
            rect.sizeDelta = new Vector2(48f, 48f);

            // Invisible but raycastable target graphic for the Button.
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;

            var labelObject = new GameObject("Text (TMP)", typeof(RectTransform));
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
            TextMeshProUGUI source = rowTransform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (source != null && source.font != null)
            {
                text.font = source.font;
            }
            text.fontSize = 40f;
            text.text = "\u00D7"; // multiplication sign as a close/delete glyph
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        Button result = buttonObject.GetComponent<Button>();
        result.onClick.RemoveAllListeners();
        result.onClick.AddListener(() => HandleDeleteClicked(row));
        return result;
    }

    private static void PlayButtonSound()
    {
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnButton);
        }
    }
}

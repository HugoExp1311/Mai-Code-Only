using Base;
using Base.Character.Action;
using Base.Character.Skills;
using Base.Character.Stats;
using Base.Dialogues;
using Base.Localization;
using EventBus;
using Events;
using UnityEngine;
using UnityEngine.UI;

public class SkillUpgradeUI : MonoBehaviour
{
    [Header("Skill Configuration")]
    [SerializeField] private SkillType skillType;

    [Header("UI Elements")]
    [SerializeField] private Button upgradeBtn;
    [SerializeField] private Button degradeBtn;
    [SerializeField] private LocalizedText skillValueText;
    [SerializeField] private LocalizedText upgradeCostText;

    // OPT-49: Allocate binding once in Awake, reuse
    private EventBinding<ResourceChangedEvent> resourceChangedBinding;
    private UIPanel parentPanel;

    private void Awake()
    {
        resourceChangedBinding = new EventBinding<ResourceChangedEvent>(HandleResourceChanged);
        EventBus<ResourceChangedEvent>.Register(resourceChangedBinding);

        // Find the parent UIPanel (the tab managed by TabManager)
        // so we can refresh when the tab becomes visible
        parentPanel = GetComponentInParent<UIPanel>();
        if (parentPanel != null)
        {
            parentPanel.OnShown.AddListener(OnParentShown);
        }
    }

    private void Start()
    {
        upgradeBtn?.onClick.AddListener(OnUpgradeClicked);
        degradeBtn?.onClick.AddListener(OnDowngradeClicked);
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);

        if (parentPanel != null)
        {
            parentPanel.OnShown.RemoveListener(OnParentShown);
        }
    }

    /// <summary>
    /// Called when the parent tab UIPanel is shown by TabManager
    /// </summary>
    private void OnParentShown()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// When skill points change (from any SkillUpgradeUI or external source),
    /// refresh this instance's display so the cost text stays current.
    /// </summary>
    private void HandleResourceChanged(ResourceChangedEvent args)
    {
        if (args.Target == RewardTarget.Player && args.Resource == BasicResource.SkillPoint)
        {
            UpdateDisplay();
        }
    }

    private async void OnUpgradeClicked()
    {
        var player = GameManager.Instance?.Player;
        if (player == null) return;
        if (!player.IsSkillUnlocked(skillType)) return;

        var skill = player.GetSkill(skillType);
        if (skill == null) return;

        int requiredPoints = skill.SkillPointRequired();
        int currentPoints = player.GetSkillPoint();
        int maxLevel = DefaultSettings.SkillMaxLevels[skillType];
        int currentLevel = skill.SkillLevel();

        // Check if at max level
        if (requiredPoints < 0 || currentLevel >= maxLevel)
        {
            NoticeUI.ShowLocalized("Notice Skill Max Level");
            return;
        }

        // Check if enough points
        if (currentPoints < requiredPoints)
        {
            NoticeUI.ShowLocalized("Notice Not Enough Sex Points");
            return;
        }

        // Perform upgrade
        var result = await player.DoAction(new CharacterActions.SkillLevelUp(skillType));
        if (result is not UIAction.FalseWithNothing)
        {
            UpdateDisplay();
            RaiseSkillPointChanged();
        }
    }

    private async void OnDowngradeClicked()
    {
        var player = GameManager.Instance?.Player;
        if (player == null) return;
        if (!player.IsSkillUnlocked(skillType)) return;

        var skill = player.GetSkill(skillType);
        if (skill == null) return;

        int currentLevel = skill.SkillLevel();

        // Check if at minimum level
        if (currentLevel <= DefaultSettings.BeginLevel)
        {
            NoticeUI.ShowLocalized("Notice Skill Min Level");
            return;
        }

        // Perform downgrade
        var result = await player.DoAction(new CharacterActions.DowngradeSkill(skillType));
        if (result is not UIAction.FalseWithNothing)
        {
            UpdateDisplay();
            RaiseSkillPointChanged();
        }
    }

    /// <summary>
    /// Notify other UI panels (e.g. SkillPanel) and sibling SkillUpgradeUI instances
    /// that skill points changed
    /// </summary>
    private void RaiseSkillPointChanged()
    {
        var player = GameManager.Instance?.Player;
        if (player == null) return;

        EventBus<ResourceChangedEvent>.Raise(new ResourceChangedEvent
        {
            Target = RewardTarget.Player,
            Resource = BasicResource.SkillPoint,
            NewValue = player.GetSkillPoint()
        });
    }

    private void UpdateDisplay()
    {
        var player = GameManager.Instance?.Player;
        if (player == null) return;

        var skill = player.GetSkill(skillType);
        if (skill == null) return;

        bool isUnlocked = player.IsSkillUnlocked(skillType);

        if (upgradeBtn != null)
        {
            upgradeBtn.interactable = isUnlocked;
        }

        if (degradeBtn != null)
        {
            degradeBtn.interactable = isUnlocked;
        }

        int value = skill.SkillValue();
        int requiredPoints = skill.SkillPointRequired();

        // Update value display (format depends on skill type)
        if (skillValueText != null)
        {
            if (isUnlocked)
            {
                SetValueText(skillValueText, FormatSkillValue(skillType, value));
            }
            else
            {
                SetEmpty(skillValueText);
            }
        }

        // Update upgrade cost display
        if (upgradeCostText != null)
        {
            if (!isUnlocked)
            {
                SetEmpty(upgradeCostText);
            }
            else if (requiredPoints < 0)
            {
                upgradeCostText.SetLocalized("UI Value Max", LocalizationDomains.UI);
            }
            else
            {
                SetInteger(upgradeCostText, requiredPoints);
            }
        }
    }

    private static void SetInteger(LocalizedText text, int amount)
    {
        text?.SetLocalized("UI Value Integer", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["amount"] = amount
        });
    }

    private static void SetValueText(LocalizedText text, string value)
    {
        text?.SetLocalized("UI Value Text", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["value"] = value ?? string.Empty
        });
    }

    private static void SetEmpty(LocalizedText text)
    {
        text?.SetLocalized("UI Empty", LocalizationDomains.UI);
    }

    private string FormatSkillValue(SkillType type, int value)
    {
        return type switch
        {
            // Hand/Tongue: value in tenths of percent (5 = 0.5%, 25 = 2.5%)
            SkillType.Hand or SkillType.Tongue =>
                FormatTenthsPercent(value),

            // F/A: value in tenths of percent (5 = 0.5%, 50 = 5.0%)
            SkillType.F or SkillType.A =>
                FormatTenthsPercent(value),

            // Cum: value in tenths of percent (50 = 5.0%, 10 = 1.0%) — lower is better
            SkillType.Cum =>
                FormatTenthsPercent(value),

            // LongNight: value is already whole percent (0, 10, 20, ... 100)
            SkillType.LongNight => $"{value}%",

            // Bullet: raw count (1-10)
            SkillType.Bullet => value.ToString(),

            // Size: stamina consumption in whole percent (100 = 10%, 10 = 1%)
            SkillType.Size => FormatTenthsPercent(value),

            _ => value.ToString()
        };
    }

    /// <summary>
    /// Format a value stored in tenths of percent to a display string.
    /// e.g. 5 → "0.5%", 25 → "2.5%", 50 → "5%", 100 → "10%"
    /// Shows one decimal place only when needed.
    /// </summary>
    private string FormatTenthsPercent(int tenthsValue)
    {
        float percent = tenthsValue / 10f;
        // Show decimal only if fractional part exists
        return percent % 1 == 0
            ? $"{(int)percent}%"
            : $"{percent:0.#}%";
    }
}

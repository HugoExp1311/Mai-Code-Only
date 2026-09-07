using Base;
using Base.Character.Stats;
using Base.Settings;
using Events;
using EventBus;
using System;
using UnityEngine;
using Base.Localization;

/// <summary>
/// Navigation panel for In Game section that displays player stats and time information
/// Inherits from UIPanel to integrate with the panel management system
/// </summary>
public class InGameNavigationPanel : UIPanel
{
    [Header("Player Stats")]
    [SerializeField] private LocalizedText staminaText;
    [SerializeField] private LocalizedText moneyText;

    [Header("Extended Player Stats")]
    [SerializeField] private LocalizedText skillPointText;
    [SerializeField] private LocalizedText charmingText;
    [SerializeField] private LocalizedText knowledgeText;
    [SerializeField] private LocalizedText workLevelText;
    [SerializeField] private LocalizedText workProgressText;

    [Header("Day Information")]
    [SerializeField] private LocalizedText dayText; // Day counter e.g. "Day 3"
    [SerializeField] private LocalizedText cycleText; // Morning/Evening/Night
    
    [Header("Time Information")]
    [SerializeField] private LocalizedText timeText; // Time with Smart String
    [SerializeField] private LocalizedText dayOfWeekText; // Day of week
    

    
    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<StatsChangedEvent> statsChangedBinding;
    private EventBinding<ResourceChangedEvent> resourceChangedBinding;
    private EventBinding<TimeChangedEvent> timeChangedBinding;
    private DateTime currentDateTime;
    private DayCycle currentCycle;

    // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
    private void Awake()
    {
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        statsChangedBinding = new EventBinding<StatsChangedEvent>(HandleStatsChanged);
        resourceChangedBinding = new EventBinding<ResourceChangedEvent>(HandleResourceChanged);
        timeChangedBinding = new EventBinding<TimeChangedEvent>(HandleTimeChanged);
    }

    private void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<StatsChangedEvent>.Register(statsChangedBinding);
        EventBus<ResourceChangedEvent>.Register(resourceChangedBinding);
        EventBus<TimeChangedEvent>.Register(timeChangedBinding);

        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;

        // Initial update if player exists
        if (GameManager.Instance?.Player != null)
        {
            UpdateAllUI();
        }
    }

    private void Start()
    {
        // Ensure UI is updated after all components are initialized
        if (GameManager.Instance?.Player != null)
        {
            UpdateAllUI();
        }
    }

    /// <summary>
    /// Update UI with a small delay to ensure LocalizedText components are ready
    /// </summary>
    private System.Collections.IEnumerator UpdateUIDelayed()
    {
        yield return null; // Wait one frame
        UpdateAllUI();
    }
    
    private void OnDisable()
    {
        // OPT-49: Deregister only — bindings are reused, not reallocated
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
        EventBus<TimeChangedEvent>.Deregister(timeChangedBinding);

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnDestroy()
    {
        // Ensure bindings are deregistered
        if (gameStartBinding != null)
        {
            EventBus<GameStartEvent>.Deregister(gameStartBinding);
            gameStartBinding = null;
        }

        if (placeChangedBinding != null)
        {
            EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
            placeChangedBinding = null;
        }

        if (statsChangedBinding != null)
        {
            EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
            statsChangedBinding = null;
        }

        if (resourceChangedBinding != null)
        {
            EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
            resourceChangedBinding = null;
        }

        if (timeChangedBinding != null)
        {
            EventBus<TimeChangedEvent>.Deregister(timeChangedBinding);
            timeChangedBinding = null;
        }

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged;
    }
    
    protected override void OnShow(object data)
    {
        base.OnShow(data);

        // Update UI when panel is shown with a small delay to ensure LocalizedText is ready
        StartCoroutine(UpdateUIDelayed());
    }
    
    /// <summary>
    /// Handle game start event
    /// </summary>
    private void HandleGameStart(GameStartEvent args)
    {
        currentDateTime = args.Time;
        currentCycle = args.Cycle;
        UpdateAllUI();
    }
    
    /// <summary>
    /// Handle place changed event
    /// </summary>
    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        currentDateTime = args.Time;
        currentCycle = args.Cycle;
        UpdateAllUI();
    }

    /// <summary>
    /// Handle stats changed event
    /// </summary>
    private void HandleStatsChanged(StatsChangedEvent args)
    {
        // Only update if the stat change is for the player
        if (args.Target == RewardTarget.Player)
        {
            UpdateAllUI();
        }
    }

    /// <summary>
    /// Handle resource changed event (e.g., Money from shop purchases)
    /// </summary>
    private void HandleResourceChanged(ResourceChangedEvent args)
    {
        // Only update if the resource change is for the player
        if (args.Target == RewardTarget.Player)
        {
            // Full refresh: Money AND SkillPoint both arrive via ResourceChangedEvent
            UpdateAllUI();
        }
    }

    /// <summary>
    /// Handle time changed event
    /// </summary>
    private void HandleTimeChanged(TimeChangedEvent args)
    {
        currentDateTime = args.Time;
        currentCycle = args.Cycle;
        UpdateTime();
        UpdateDayOfWeek();
        UpdateDayAndCycle();
        // Work progress/level changes fire no event, but every work action advances time
        UpdateWorkStats();
    }

    private void OnLanguageChanged(Language language, string localeCode, System.Globalization.CultureInfo culture)
    {
        // Update time-related displays when locale changes
        if (currentDateTime != default(DateTime))
        {
            UpdateTime();
            UpdateDayOfWeek();
            UpdateDayAndCycle();
        }
        // Re-render non-time stats too (labels/values are re-resolved by LocalizedText)
        UpdateWorkStats();
    }
    
    /// <summary>
    /// Update all UI elements
    /// </summary>
    private void UpdateAllUI()
    {
        UpdateStamina();
        UpdateMoney();
        UpdateTime();
        UpdateDayOfWeek();
        UpdateDayAndCycle();
        UpdateSkillPoints();
        UpdateCharming();
        UpdateKnowledge();
        UpdateWorkStats();
    }
    
    /// <summary>
    /// Update stamina display
    /// Shows current/max format (e.g., "60/110")
    /// </summary>
    private void UpdateStamina()
    {
        if (staminaText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            int stamina = GameManager.Instance.Player.GetStamina();
            int maxStamina = GameManager.Instance.Player.GetMaxStamina();
            SetRatio(staminaText, stamina, maxStamina);
        }
        else
        {
            SetRatio(staminaText, 0, 0);
        }
    }
    
    /// <summary>
    /// Update money display
    /// </summary>
    private void UpdateMoney()
    {
        if (moneyText == null) return;
        
        if (GameManager.Instance?.Player != null)
        {
            int money = GameManager.Instance.Player.GetMoney();
            SetInteger(moneyText, money);
        }
        else
        {
            SetInteger(moneyText, 0);
        }
    }
    
    /// <summary>
    /// Update day counter and day cycle (Morning/Evening/Night) display
    /// </summary>
    private void UpdateDayAndCycle()
    {
        if (GameManager.Instance != null && currentDateTime != default(DateTime))
        {
            int day = GameManager.Instance.DaysPlayed;
            SetInteger(dayText, day);

            string cycleKey = currentCycle switch
            {
                DayCycle.Morning => "In Game Cycle Morning",
                DayCycle.Evening => "In Game Cycle Evening",
                DayCycle.Night => "In Game Cycle Night",
                _ => "In Game Cycle Morning"
            };
            cycleText?.SetLocalized(cycleKey, LocalizationDomains.UI);
            cycleText?.Refresh();
        }
        else
        {
            SetEmpty(dayText);
            SetEmpty(cycleText);
        }
    }

    /// <summary>
    /// Update skill points display
    /// </summary>
    private void UpdateSkillPoints()
    {
        if (skillPointText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            SetInteger(skillPointText, GameManager.Instance.Player.GetSkillPoint());
        }
        else
        {
            SetInteger(skillPointText, 0);
        }
    }

    /// <summary>
    /// Update charming stat display
    /// </summary>
    private void UpdateCharming()
    {
        if (charmingText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            SetInteger(charmingText, GameManager.Instance.Player.GetCharming());
        }
        else
        {
            SetInteger(charmingText, 0);
        }
    }

    /// <summary>
    /// Update knowledge stat display
    /// </summary>
    private void UpdateKnowledge()
    {
        if (knowledgeText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            SetInteger(knowledgeText, GameManager.Instance.Player.GetKnowledge());
        }
        else
        {
            SetInteger(knowledgeText, 0);
        }
    }

    /// <summary>
    /// Update work level ("Lv 2") and work progress ("120/150" or "MAX") display
    /// </summary>
    private void UpdateWorkStats()
    {
        if (GameManager.Instance == null) return;

        if (workLevelText != null)
        {
            workLevelText.SetLocalized("UI Value Level", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
            {
                ["amount"] = GameManager.Instance.GetWorkLevel()
            });
        }

        if (workProgressText != null)
        {
            int required = GameManager.Instance.GetNextLevelRequirement();
            if (required <= 0)
            {
                // Max work level reached — show "MAX"
                workProgressText.SetLocalized("UI Value Max", LocalizationDomains.UI);
                workProgressText.Refresh();
            }
            else
            {
                SetRatio(workProgressText, GameManager.Instance.GetWorkProgress(), required);
            }
        }
    }

    /// <summary>
    /// Update time display using LocalizedText with Smart String
    /// </summary>
    private void UpdateTime()
    {
        if (timeText == null) return;
        
        if (GameManager.Instance != null && currentDateTime != default(DateTime))
        {
            var culture = GameManager.Instance.CultureInfo;
            
            // Clone the culture to modify AM/PM designators
            var customCulture = (System.Globalization.CultureInfo)culture.Clone();
            
            // Set culture-specific AM/PM designators
            switch (culture.Name)
            {
                case "vi-VN":
                    customCulture.DateTimeFormat.AMDesignator = "SA";
                    customCulture.DateTimeFormat.PMDesignator = "CH";
                    break;
                    
                case "ja-JP":
                    customCulture.DateTimeFormat.AMDesignator = "午前";
                    customCulture.DateTimeFormat.PMDesignator = "午後";
                    break;
                    
                case "en-US":
                default:
                    // Keep default AM/PM
                    break;
            }
            
            string timeString;
            
            if (currentDateTime.Hour == 0)
            {
                // Special case for midnight (00:00)
                timeString = $"12:{currentDateTime.ToString("mm", customCulture)} {currentDateTime.ToString("tt", customCulture)}";
            }
            else
            {
                // Use culture-specific 12-hour format with AM/PM designators
                timeString = currentDateTime.ToString("hh:mm tt", customCulture);
            }
            
            SetValueText(timeText, timeString);
        }
        else
        {
            SetEmpty(timeText);
        }
    }
    
    /// <summary>
    /// Update day of week display using LocalizedText
    /// </summary>
    /// <summary>
    /// Update day of week display using LocalizedText
    /// </summary>
    private void UpdateDayOfWeek()
    {
        if (dayOfWeekText == null) return;
        
        if (GameManager.Instance != null && currentDateTime != default(DateTime))
        {
            string key = GetDayOfWeekKey(currentDateTime.DayOfWeek);
            dayOfWeekText.SetLocalized(key, LocalizationDomains.UI);
            
            // Force refresh to ensure the text is updated immediately
            dayOfWeekText.Refresh();
        }
        else
        {
            SetEmpty(dayOfWeekText);
        }
    }

    private static void SetInteger(LocalizedText text, int amount)
    {
        text?.SetLocalized("UI Value Integer", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["amount"] = amount
        });
    }

    private static void SetRatio(LocalizedText text, int current, int max)
    {
        text?.SetLocalized("UI Value Ratio", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["current"] = current,
            ["max"] = max
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
    
    /// <summary>
    /// Get localization key for day of week
    /// </summary>
    private string GetDayOfWeekKey(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => "In Game DayOfWeek Sunday",
            DayOfWeek.Monday => "In Game DayOfWeek Monday",
            DayOfWeek.Tuesday => "In Game DayOfWeek Tuesday",
            DayOfWeek.Wednesday => "In Game DayOfWeek Wednesday",
            DayOfWeek.Thursday => "In Game DayOfWeek Thursday",
            DayOfWeek.Friday => "In Game DayOfWeek Friday",
            DayOfWeek.Saturday => "In Game DayOfWeek Saturday",
            _ => "In Game DayOfWeek Sunday"
        };
    }
}


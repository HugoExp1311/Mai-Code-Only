using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Base.Localization;
using Base.UI.Popup;
using Base.Character.Action;
using Base.Character.Stats;

/// <summary>
/// Action Confirm Popup that displays action information and provides confirmation buttons
/// Inherits from UIPanel to integrate with the panel management system
/// Belongs to InGame section and displays dynamic content based on PopupState from database
/// 
/// The popup receives PopupState data which defines:
/// - Header text and icon
/// - Configuration for 3 buttons (text, enabled state, interactable state, actions to execute)
/// 
/// Button actions are executed directly by this component using PopupButtonAction records
/// </summary>
public class ActionConfirmPopup : UIPanel
{
    [Header("UI References")]
    [Tooltip("Image component for displaying the action icon")]
    [SerializeField] private Image iconImage;
    
    [Tooltip("LocalizedText component for displaying the header/title")]
    [SerializeField] private Base.Localization.LocalizedText headerText;
    
    [Header("Boost/Preview Text")]
    [Tooltip("LocalizedText component for displaying stat change preview (visible during initial state)")]
    [SerializeField] private Base.Localization.LocalizedText boostText;
    
    [Header("Result Text")]
    [Tooltip("LocalizedText component for displaying actual stat changes (visible during result state)")]
    [SerializeField] private Base.Localization.LocalizedText resultText;
    
    [Header("Buttons References")]
    [Tooltip("First button GameObject")]
    [SerializeField] private GameObject button1GameObject;
    
    [Tooltip("Second button GameObject")]
    [SerializeField] private GameObject button2GameObject;
    
    [Tooltip("Third button GameObject")]
    [SerializeField] private GameObject button3GameObject;
    
    [Header("Progress Layout References")]
    [Tooltip("Container for Progress-specific UI (Work Level popup)")]
    [SerializeField] private GameObject progressLayoutContainer;
    
    [Tooltip("Value text showing current Work Level number")]
    [SerializeField] private LocalizedText progressWorkLevelValueText;
    
    [Tooltip("Value text showing current Progress points number")]
    [SerializeField] private LocalizedText progressPointsValueText;
    
    [Tooltip("Value text showing Next Level requirement number")]
    [SerializeField] private LocalizedText progressNextLevelValueText;
    
    [Tooltip("Promote button for Progress popup")]
    [SerializeField] private Button progressPromoteButton;
    
    // Cached button components
    private Button button1;
    private Button button2;
    private Button button3;
    private Base.Localization.LocalizedText button1Text;
    private Base.Localization.LocalizedText button2Text;
    private Base.Localization.LocalizedText button3Text;
    
    private PopupState currentState;
    private string currentActionName; // Track which action triggered the popup
    private Coroutine autoCloseCoroutine; // Auto-close timer for result states

    private void Awake()
    {
        // Cache button components
        if (button1GameObject != null)
        {
            button1 = button1GameObject.GetComponent<Button>();
            button1Text = button1GameObject.GetComponentInChildren<Base.Localization.LocalizedText>();
        }
        if (button2GameObject != null)
        {
            button2 = button2GameObject.GetComponent<Button>();
            button2Text = button2GameObject.GetComponentInChildren<Base.Localization.LocalizedText>();
        }
        if (button3GameObject != null)
        {
            button3 = button3GameObject.GetComponent<Button>();
            button3Text = button3GameObject.GetComponentInChildren<Base.Localization.LocalizedText>();
        }
        
        // Setup Progress popup promote button
        if (progressPromoteButton != null)
        {
            progressPromoteButton.onClick.AddListener(OnProgressPromoteClicked);
        }
    }
    
    /// <summary>
    /// Called when panel is shown - populates UI from PopupState data
    /// </summary>
    protected override void OnShow(object data)
    {
        base.OnShow(data);
        
        if (data is PopupState state)
        {
            // Determine action name from state ID
            DetermineActionName(state.stateId);
            ApplyPopupState(state);
        }
        else
        {
            Debug.LogWarning($"[ActionConfirmPopup] Expected PopupState data, got {data?.GetType().Name ?? "null"}");
        }
    }
    
    /// <summary>
    /// Determine action name from state ID
    /// </summary>
    private void DetermineActionName(string stateId)
    {
        if (stateId.Contains("Sleep"))
            currentActionName = "Sleep";
        else if (stateId.Contains("Eating"))
            currentActionName = "Eating";
        else if (stateId.Contains("Sex"))
            currentActionName = "Sex";
        else if (stateId.Contains("Working"))
            currentActionName = "Working";
        else if (stateId.Contains("Talking"))
            currentActionName = "Talking";
        else if (stateId.Contains("Exercise"))
            currentActionName = "Exercise";
        else if (stateId.Contains("Progress"))
            currentActionName = "Progress";
        else
            currentActionName = "";
    }
    
    /// <summary>
    /// Apply popup state to UI elements and configure buttons
    /// </summary>
    private void ApplyPopupState(PopupState state)
    {
        if (state == null || !state.IsValid())
        {
            Debug.LogError("[ActionConfirmPopup] Invalid PopupState - must have exactly 3 buttons");
            return;
        }
        
        currentState = state;
        
        // Check if this is a Progress action - use unique layout
        bool isProgressAction = state.stateId.Contains("Progress");
        
        // Toggle layouts based on action type
        SetProgressLayoutActive(isProgressAction);
        
        if (isProgressAction)
        {
            // Apply Progress-specific layout
            ApplyProgressLayout();
            return;
        }
        
        // Check if this is a result state (first button non-interactable with action)
        bool isResultState = state.buttons != null &&
                            state.buttons.Length > 0 &&
                            !state.buttons[0].isInteractable &&
                            state.buttons[0].isEnabled &&
                            state.buttons[0].action != null;
        
        // Standard layout handling below
        
        // Update header with localization key
        if (headerText != null)
        {
            string headerKey = GetLocalizationKey(state.stateId, "Header");
            if (!string.IsNullOrEmpty(headerKey))
            {
                headerText.SetLocalized(headerKey, Base.Localization.LocalizationDomains.UI);
            }
            else
            {
                SetValueText(headerText, state.headerText);
            }
        }
        
        if (iconImage != null)
        {
            // Try to get custom icon first, fall back to state icon
            Sprite iconToUse = null;
            if (!string.IsNullOrEmpty(currentActionName))
            {
                iconToUse = ActionConfirmPopupDatabase.GetCustomIcon(currentActionName, state.stateId);
            }
            
            if (iconToUse == null)
            {
                iconToUse = state.iconSprite;
            }
            
            if (iconToUse != null)
            {
                iconImage.sprite = iconToUse;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }
        
        // Handle boost text and result text visibility based on state type
        if (isResultState)
        {
            // Result state: hide boost text, show result text, hide all buttons
            SetBoostTextVisible(false);
            ApplyResultText(state);
            
            // Hide all buttons during result state
            if (button1GameObject != null) button1GameObject.SetActive(false);
            if (button2GameObject != null) button2GameObject.SetActive(false);
            if (button3GameObject != null) button3GameObject.SetActive(false);
        }
        else
        {
            // Initial state: show boost text, hide result text, show buttons
            ApplyBoostText(state);
            SetResultTextVisible(false);
            
            // Apply button states
            ApplyButtonState(button1, button1Text, state.buttons[0], 0);
            ApplyButtonState(button2, button2Text, state.buttons[1], 1);
            ApplyButtonState(button3, button3Text, state.buttons[2], 2);
        }
    }

    /// <summary>
    /// Set boost text visibility
    /// </summary>
    private void SetBoostTextVisible(bool visible)
    {
        if (boostText != null)
        {
            boostText.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Set result text visibility
    /// </summary>
    private void SetResultTextVisible(bool visible)
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Apply boost text (preview of stat changes) for initial states
    /// Uses boosterText from the first button's state
    /// </summary>
    private void ApplyBoostText(PopupState state)
    {
        if (boostText == null) return;
        
        // Get boost text from first button's boosterText field
        string boosterContent = state.buttons[0].boosterText;
        
        if (string.IsNullOrEmpty(boosterContent))
        {
            // No boost text to show
            SetBoostTextVisible(false);
            return;
        }
        
        SetBoostTextVisible(true);
        
        var statChanges = ParseStatChanges(boosterContent);
        if (statChanges.Count > 0)
        {
            SetStatChangeList(boostText, statChanges);
        }
        else
        {
            SetValueText(boostText, boosterContent);
        }
    }
    
    /// <summary>
    /// Apply result text (actual stat changes) for result states
    /// </summary>
    private void ApplyResultText(PopupState state)
    {
        if (resultText == null) return;
        
        SetResultTextVisible(true);
        
        // Check if this is a Work result state - needs dynamic reward calculation
        bool isWorkResultState = state.stateId == "WorkNormalState" || state.stateId == "WorkHardState";
        
        string statChangeText;
        if (isWorkResultState)
        {
            // Generate dynamic work rewards text
            statChangeText = GenerateWorkRewardsText(state.stateId == "WorkHardState");
        }
        else
        {
            // Use buttonText from first button which contains the stat changes
            statChangeText = state.buttons[0].buttonText;
        }
        
        // Parse and display stat changes with localization
        var statChanges = ParseStatChanges(statChangeText);
        
        if (statChanges.Count > 0)
        {
            SetStatChangeList(resultText, statChanges);
        }
        else
        {
            SetValueText(resultText, statChangeText);
        }
    }
    
    /// <summary>
    /// Get localization key for a state element
    /// </summary>
    private string GetLocalizationKey(string stateId, string element)
    {
        // Map state IDs to localization keys
        // Format: Action Confirm {Action} {State} {Element}
        
        // Sleep states
        if (stateId == "SleepInitial") return "Action Confirm Sleep Initial Header";
        if (stateId == "NapState") return "Action Confirm Sleep Nap Header";
        if (stateId == "DeepSleepState") return "Action Confirm Sleep DeepSleep Header";
        
        // Eating states
        if (stateId == "EatingInitial") return "Action Confirm Eating Initial Header";
        if (stateId == "EatingWithMaiState") return "Action Confirm Eating WithMai Header";
        
        // Sex states
        if (stateId == "SexInitial") return "Action Confirm Sex Initial Header";
        
        // Working states
        if (stateId == "WorkingInitial") return "Action Confirm Working Initial Header";
        if (stateId == "WorkNormalState" || stateId == "WorkHardState") return "Action Confirm Working Done Header";
        
        // Talking states
        if (stateId == "TalkingInitial") return "Action Confirm Talking Initial Header";
        if (stateId == "TalkingResultState") return "Action Confirm Talking Result Header";
        
        // Exercise states
        if (stateId == "ExerciseInitial") return "Action Confirm Exercise Initial Header";
        if (stateId == "ExerciseNormalState" || stateId == "ExerciseHardState") return "Action Confirm Exercise Done Header";
        
        // Progress states
        if (stateId == "ProgressInitial") return "Action Confirm Progress Initial Header";
        
        return null;
    }
    
    /// <summary>
    /// Get localization key for a button based on state ID and button index
    /// This is more reliable than text matching
    /// </summary>
    private string GetButtonKeyFromState(string stateId, int buttonIndex, string fallbackText)
    {
        // Map based on state ID and button index
        switch (stateId)
        {
            // Sleep states
            case "SleepInitial":
                if (buttonIndex == 0) return "Action Confirm Sleep Nap Button";
                if (buttonIndex == 1) return "Action Confirm Sleep DeepSleep Button";
                if (buttonIndex == 2) return "Action Confirm Sleep Cancel Button";
                break;
                
            // Eating states
            case "EatingInitial":
                if (buttonIndex == 0) return "Action Confirm Eating Accept Button";
                if (buttonIndex == 1) return "Action Confirm Eating Decline Button";
                break;
                
            // Sex states
            case "SexInitial":
                if (buttonIndex == 0) return "Action Confirm Sex Accept Button";
                if (buttonIndex == 1) return "Action Confirm Sex Decline Button";
                break;
                
            // Working states
            case "WorkingInitial":
                if (buttonIndex == 0) return "Action Confirm Working Normal Button";
                if (buttonIndex == 1) return "Action Confirm Working Hard Button";
                if (buttonIndex == 2) return "Action Confirm Working Cancel Button";
                break;
                
            // Talking states
            case "TalkingInitial":
                if (buttonIndex == 0) return "Action Confirm Talking Accept Button";
                if (buttonIndex == 1) return "Action Confirm Talking Decline Button";
                break;
                
            // Exercise states
            case "ExerciseInitial":
                if (buttonIndex == 0) return "Action Confirm Exercise Normal Button";
                if (buttonIndex == 1) return "Action Confirm Exercise Hard Button";
                if (buttonIndex == 2) return "Action Confirm Exercise Cancel Button";
                break;
        }
        
        return null;
    }
    
    /// <summary>
    /// Get localization key for a button based on button text (legacy fallback)
    /// </summary>
    private string GetButtonLocalizationKey(string buttonText)
    {
        // Map button texts to localization keys
        switch (buttonText)
        {
            // Sleep buttons
            case "Take a nap": return "Action Confirm Sleep Nap Button";
            case "Sweet dream...": return "Action Confirm Sleep DeepSleep Button";
            case "Nope, I'm still awake": return "Action Confirm Sleep Cancel Button";
            
            // Eating buttons
            case "Sure, why not?": return "Action Confirm Eating Accept Button";
            case "I'm not hungry": return "Action Confirm Eating Decline Button";
            
            // Sex buttons
            case "Fuck yeah!": return "Action Confirm Sex Accept Button";
            case "I wanna sleep": return "Action Confirm Sex Decline Button";
            
            // Working buttons
            case "Work": return "Action Confirm Working Normal Button";
            case "Work hard": return "Action Confirm Working Hard Button";
            case "I'm Lazy": return "Action Confirm Working Cancel Button";
            
            // Talking buttons
            case "Let's talk!": return "Action Confirm Talking Accept Button";
            case "I'm an introvert": return "Action Confirm Talking Decline Button";
            
            // Exercise buttons
            case "Okay...": return "Action Confirm Exercise Normal Button";
            case "Try my best!": return "Action Confirm Exercise Hard Button";
            case "I'm lazy...": return "Action Confirm Exercise Cancel Button";
            
            default: return null;
        }
    }
    
    /// <summary>
    /// Apply button state configuration
    /// </summary>
    private void ApplyButtonState(Button button, Base.Localization.LocalizedText text, PopupButtonState state, int buttonIndex)
    {
        if (button == null) return;

        // Set button visibility
        button.gameObject.SetActive(state.isEnabled);

        // Set button interactability
        button.interactable = state.isInteractable;

        // Set button text with localization
        if (text != null)
        {
            // Check if this is a Work result state - needs dynamic reward calculation
            bool isWorkResultState = currentState != null && 
                (currentState.stateId == "WorkNormalState" || currentState.stateId == "WorkHardState");
            
            if (isWorkResultState && buttonIndex == 0)
            {
                // Generate dynamic work rewards text
                string dynamicButtonText = GenerateWorkRewardsText(currentState.stateId == "WorkHardState");
                var statChanges = ParseStatChanges(dynamicButtonText);
                SetStatChangeList(text, statChanges);
            }
            // Check if this is a result button (contains stat changes with actual numbers)
            else if (state.buttonText.Contains("+") || state.buttonText.Contains("-"))
            {
                // Parse stat changes from buttonText
                var statChanges = ParseStatChanges(state.buttonText);
                
                // Only use localized list if we successfully parsed stat changes
                if (statChanges.Count > 0)
                {
                    SetStatChangeList(text, statChanges);
                }
                else
                {
                    SetValueText(text, state.buttonText);
                }
            }
            else
            {
                // This is a regular button - use localization key based on state ID and button index
                string buttonKey = GetButtonKeyFromState(currentState.stateId, buttonIndex, state.buttonText);
                if (!string.IsNullOrEmpty(buttonKey))
                {
                    text.SetLocalized(buttonKey, Base.Localization.LocalizationDomains.UI);
                }
                else
                {
                    SetValueText(text, state.buttonText);
                }
            }
        }

        // Remove old listeners
        button.onClick.RemoveAllListeners();

        // Add onClick listener only for interactive buttons
        // Result state buttons (non-interactable with action) don't need onClick
        // because their actions are executed immediately in TransitionToState
        if (state.isInteractable && state.action != null)
        {
            button.onClick.AddListener(() => OnButtonClicked(state.action, buttonIndex));
        }
    }

    /// <summary>
    /// Parse stat changes from button text
    /// Example: "+20 Energy\n-2 Knowledge" -> [(stat: "Energy", amount: "+20"), (stat: "Knowledge", amount: "-2")]
    /// </summary>
    private System.Collections.Generic.List<(string stat, string amount)> ParseStatChanges(string buttonText)
    {
        var statChanges = new System.Collections.Generic.List<(string stat, string amount)>();
        
        // Split by newlines
        string[] lines = buttonText.Split('\n');
        
        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;

            if ((trimmedLine[0] == '+' || trimmedLine[0] == '-') && trimmedLine.Length > 1 && !char.IsDigit(trimmedLine[1]) && trimmedLine[1] != ' ')
            {
                statChanges.Add((stat: trimmedLine.Substring(1).Trim(), amount: trimmedLine[0].ToString()));
                continue;
            }
            
            // Parse the line - handles both "+20 Energy" and "+ 20 Energy" formats
            // Split into parts and recombine sign+number if separated by space
            string[] parts = trimmedLine.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
            {
                string amount;
                string statName;
                
                // Check if first part is just a sign (+ or -) with number as second part
                if ((parts[0] == "+" || parts[0] == "-") && parts.Length >= 3)
                {
                    // Recombine: "+" + "20" = "+20", stat = remaining parts
                    amount = parts[0] + parts[1];
                    statName = string.Join(" ", parts, 2, parts.Length - 2);
                }
                else
                {
                    // Normal format: "+20 Energy" or "-2 Knowledge"
                    amount = parts[0];
                    statName = string.Join(" ", parts, 1, parts.Length - 1);
                }
                
                statChanges.Add((stat: statName, amount: amount));
            }
        }
        
        return statChanges;
    }

    private void SetStatChangeList(LocalizedText text, System.Collections.Generic.List<(string stat, string amount)> statChanges)
    {
        if (text == null)
            return;

        text.SetLocalizedList(
            "Action Confirm Stat Change",
            statChanges,
            stat => new Dictionary<string, object>
            {
                ["stat"] = GetLocalizedStatName(stat.stat),
                ["amount"] = stat.amount
            },
            tableName: LocalizationDomains.UI,
            separator: "\n");
    }

    private static void SetInteger(LocalizedText text, int amount)
    {
        text?.SetLocalized("UI Value Integer", LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["amount"] = amount
        });
    }

    private static void SetValueText(LocalizedText text, string value)
    {
        text?.SetLocalized("UI Value Text", LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["value"] = value ?? string.Empty
        });
    }

    private static string GetLocalizedStatName(string statName)
    {
        string key = statName switch
        {
            "Money" => "Stat Money",
            "Knowledge" => "Stat Knowledge",
            "Progress" => "Stat Progress",
            "Energy" => "Stat Energy",
            "Stamina" => "Stat Energy",
            "Love" => "Stat Love",
            "Libido" => "Stat Libido",
            "Charming" => "Stat Charming",
            "Sex Points" => "Stat Sex Points",
            "Skill Point" => "Stat Sex Points",
            "Skill Points" => "Stat Sex Points",
            _ => null
        };

        if (string.IsNullOrEmpty(key) || LocalizationManager.Instance == null)
            return statName;

        return LocalizationManager.Instance.GetLocalizedString(key, LocalizationDomains.UI);
    }

    /// <summary>
    /// Generate dynamic work rewards text based on current work level
    /// </summary>
    private string GenerateWorkRewardsText(bool isHardWork)
    {
        if (GameManager.Instance == null)
            return "+Money\n+Knowledge\n+Progress\n-Energy";
        
        int workLevel = GameManager.Instance.GetWorkLevel();
        var (money, knowledge, progress, stamina, timeHours) = Base.DefaultSettings.GetWorkRewards(workLevel);
        
        // Apply hard work multiplier
        if (isHardWork)
        {
            money *= 2;
            knowledge *= 2;
            progress *= 2;
            stamina = -50;
        }
        
        // Build the text in format that ParseStatChanges can handle
        var lines = new System.Collections.Generic.List<string>();
        lines.Add($"+{money} Money");
        lines.Add($"+{knowledge} Knowledge");
        lines.Add($"+{progress} Progress");
        lines.Add($"{stamina} Energy"); // stamina is already negative
        
        return string.Join("\n", lines);
    }
    
    /// <summary>
    /// Execute a popup button action
    /// </summary>
    /// <summary>
    /// Handle button click - play sound then execute action
    /// </summary>
    private void OnButtonClicked(PopupButtonAction action, int buttonIndex)
    {
        // Intercept sleep button clicks to check daily limits
        if (currentState != null && currentState.stateId == "SleepInitial")
        {
            if (HandleSleepButtonClick(action, buttonIndex))
                return; // Handled by sleep logic (either blocked or will execute)
        }
        
        PlayButtonSound();

        if (TryShowEnergyNotice(action))
            return;

        ExecuteButtonAction(action, buttonIndex);
    }
    
    /// <summary>
    /// Handle sleep button clicks with daily limit checks
    /// Returns true if handled (caller should not execute default logic)
    /// </summary>
    private bool HandleSleepButtonClick(PopupButtonAction action, int buttonIndex)
    {
        if (GameManager.Instance == null)
            return false;
        
        // Button 0: Take a nap (max 2/day)
        if (buttonIndex == 0)
        {
            if (!GameManager.Instance.CanNapToday())
            {
                // Show "enough sleep" notice
                PlayButtonSound();
                NoticeUI.ShowLocalized("Notice Enough Sleep");
                return true;
            }
            
            // Execute nap action
            PlayButtonSound();

            if (TryShowEnergyNotice(action))
                return true;

            ExecuteButtonAction(action, buttonIndex);
            GameManager.Instance.MarkNapUsed();
            
            // Show result state
            TransitionToResultState("NapState");
            return true;
        }
        
        // Button 1: Sweet dream (max 1/day, 9 PM - 6 AM only)
        if (buttonIndex == 1)
        {
            // Check time window first
            if (!GameManager.Instance.IsDeepSleepTimeWindow())
            {
                // Show "enough sleep" notice (reuse same message for simplicity)
                PlayButtonSound();
                NoticeUI.ShowLocalized("Notice Enough Sleep");
                return true;
            }
            
            if (!GameManager.Instance.CanDeepSleepToday())
            {
                // Show "enough sleep" notice
                PlayButtonSound();
                NoticeUI.ShowLocalized("Notice Enough Sleep");
                return true;
            }
            
            // Execute deep sleep action
            PlayButtonSound();

            if (TryShowEnergyNotice(action))
                return true;

            ExecuteButtonAction(action, buttonIndex);
            GameManager.Instance.MarkDeepSleepUsed();
            
            // Show result state
            TransitionToResultState("DeepSleepState");
            return true;
        }
        
        // Button 2: Cancel — let default handler close popup
        return false;
    }

    private bool TryShowEnergyNotice(PopupButtonAction action)
    {
        if (!TryGetEnergyChangeInfo(action, out int requiredEnergy, out bool restoresEnergy))
            return false;

        var player = GameManager.Instance?.Player;
        if (player == null)
            return false;

        if (requiredEnergy > 0 && player.GetStamina() < requiredEnergy)
        {
            NoticeUI.ShowLocalized("Notice Not Enough Energy");
            return true;
        }

        if (requiredEnergy == 0 && restoresEnergy && player.GetStamina() >= player.GetMaxStamina())
        {
            NoticeUI.ShowLocalized("Notice Enough Energy");
            return true;
        }

        return false;
    }

    private bool TryGetEnergyChangeInfo(PopupButtonAction action, out int requiredEnergy, out bool restoresEnergy)
    {
        requiredEnergy = 0;
        restoresEnergy = false;

        if (action == null)
            return false;

        switch (action)
        {
            case PopupButtonAction.ChangeStat changeStat
                when changeStat.Target == RewardTarget.Player && changeStat.Stat == BasicStats.Stamina:
                if (changeStat.Amount < 0)
                {
                    requiredEnergy = -changeStat.Amount;
                    return true;
                }

                if (changeStat.Amount > 0)
                {
                    restoresEnergy = true;
                    return true;
                }

                return false;

            case PopupButtonAction.ApplyWorkRewards applyWorkRewards:
                requiredEnergy = GetWorkEnergyCost(applyWorkRewards.IsHardWork);
                return requiredEnergy > 0;

            case PopupButtonAction.MultipleActions multipleActions:
                bool foundEnergyChange = false;

                foreach (var subAction in multipleActions.Actions)
                {
                    if (!TryGetEnergyChangeInfo(subAction, out int subRequiredEnergy, out bool subRestoresEnergy))
                        continue;

                    requiredEnergy += subRequiredEnergy;
                    restoresEnergy |= subRestoresEnergy;
                    foundEnergyChange = true;
                }

                return foundEnergyChange;

            case PopupButtonAction.TransitionState transitionState:
                PopupState targetState = ActionConfirmPopupDatabase.GetState(transitionState.TargetStateId);
                bool isResultState = targetState != null &&
                                     targetState.buttons != null &&
                                     targetState.buttons.Length > 0 &&
                                     !targetState.buttons[0].isInteractable &&
                                     targetState.buttons[0].isEnabled &&
                                     targetState.buttons[0].action != null;

                if (!isResultState)
                    return false;

                return TryGetEnergyChangeInfo(targetState.buttons[0].action, out requiredEnergy, out restoresEnergy);

            default:
                return false;
        }
    }

    private int GetWorkEnergyCost(bool isHardWork)
    {
        if (isHardWork)
            return 50;

        int workLevel = GameManager.Instance != null ? GameManager.Instance.GetWorkLevel() : 1;
        var (_, _, _, stamina, _) = Base.DefaultSettings.GetWorkRewards(workLevel);
        return Math.Max(0, -stamina);
    }
    
    /// <summary>
    /// Transition to a result state for visual feedback (without executing the result state's action)
    /// Shows the result header/text and auto-closes after delay
    /// </summary>
    private void TransitionToResultState(string stateId)
    {
        PopupState resultState = ActionConfirmPopupDatabase.GetState(stateId);
        if (resultState == null) return;
        
        // Apply UI layout without executing result state actions (already executed)
        ApplyPopupState(resultState);
        
        // Start auto-close timer
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }
        autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay(2.5f));
    }
    
    /// <summary>
    /// Play button click sound effect
    /// </summary>
    private void PlayButtonSound()
    {
        if (Base.AudioManager.Instance != null && Base.FMODEvents.Instance != null)
        {
            Base.AudioManager.Instance.PlayOneShot(Base.FMODEvents.Instance.OnButton);
        }
    }

    private async void ExecuteButtonAction(PopupButtonAction action, int buttonIndex)
    {
        if (action == null) return;

        switch (action)
        {
            case PopupButtonAction.ProgressTime pt:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AdvanceTimePublic(TimeSpan.FromHours(pt.Hours));
                }
                break;

            case PopupButtonAction.ProgressTimeToNextDay ptnd:
                ProgressToNextDay(ptnd.TargetHour, ptnd.TargetMinute);
                break;

            case PopupButtonAction.ChangeStat cs:
                if (cs.Target == RewardTarget.Player)
                {
                    if (GameManager.Instance != null && GameManager.Instance.Player != null)
                    {
                        await GameManager.Instance.Player.DoAction(new CharacterActions.ChangeStat(cs.Target, cs.Stat, cs.Amount));
                    }
                }
                else if (cs.Target == RewardTarget.Mai)
                {
                    if (GameManager.Instance?.DataManager != null)
                    {
                        var mai = GameManager.Instance.DataManager.GetCurrentBoss();
                        if (mai != null)
                        {
                            await mai.DoAction(new CharacterActions.ChangeStat(cs.Target, cs.Stat, cs.Amount));
                        }
                    }
                }
                break;

            case PopupButtonAction.ChangeMaxStat cms:
                if (cms.Target == RewardTarget.Player)
                {
                    if (GameManager.Instance != null && GameManager.Instance.Player != null)
                    {
                        await GameManager.Instance.Player.DoAction(new CharacterActions.ChangeMaxStat(cms.Target, cms.Stat, cms.Amount));
                    }
                }
                else if (cms.Target == RewardTarget.Mai)
                {
                    if (GameManager.Instance?.DataManager != null)
                    {
                        var mai = GameManager.Instance.DataManager.GetCurrentBoss();
                        if (mai != null)
                        {
                            await mai.DoAction(new CharacterActions.ChangeMaxStat(cms.Target, cms.Stat, cms.Amount));
                        }
                    }
                }
                break;

            case PopupButtonAction.ChangeResource cr:
                if (cr.Target == RewardTarget.Player)
                {
                    if (GameManager.Instance != null && GameManager.Instance.Player != null)
                    {
                        await GameManager.Instance.Player.DoAction(new CharacterActions.ChangeResource(cr.Target, cr.Resource, cr.Amount));
                    }
                }
                else if (cr.Target == RewardTarget.Mai)
                {
                    if (GameManager.Instance?.DataManager != null)
                    {
                        var mai = GameManager.Instance.DataManager.GetCurrentBoss();
                        if (mai != null)
                        {
                            await mai.DoAction(new CharacterActions.ChangeResource(cr.Target, cr.Resource, cr.Amount));
                        }
                    }
                }
                break;

            case PopupButtonAction.TransitionState ts:
                TransitionToState(ts.TargetStateId);
                break;

            case PopupButtonAction.ClosePopup:
                // Don't mark action as used here - ClosePopup is used for cancel/decline buttons
                // Action should only be marked as used when actually completed (via MultipleActions in result states)
                if (UIPanelManager.Instance != null)
                {
                    UIPanelManager.Instance.HidePanel(PanelId);
                }
                break;

            case PopupButtonAction.MultipleActions ma:
                foreach (var subAction in ma.Actions)
                {
                    ExecuteButtonAction(subAction, buttonIndex);
                }
                // Mark action as used after all sub-actions complete
                MarkActionUsed();
                break;
                
            case PopupButtonAction.AddWorkProgress awp:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddWorkProgress(awp.Amount);
                }
                break;
                
            case PopupButtonAction.ApplyWorkRewards awr:
                await ApplyWorkRewardsBasedOnLevel(awr.IsHardWork);
                break;
                
            case PopupButtonAction.SwitchSection ss:
                if (UIPanelManager.Instance != null)
                {
                    UIPanelManager.Instance.SwitchToSection(ss.Section, ss.UseTransition);
                }
                break;
        }
    }
    
    /// <summary>
    /// Mark the current action as used (for once-per-day tracking)
    /// </summary>
    private void MarkActionUsed()
    {
        if (currentState == null || GameManager.Instance == null) return;
        
        // Determine which action was used based on current state ID
        if (currentState.stateId.Contains("Eating"))
        {
            GameManager.Instance.MarkEatingUsed();
        }
        else if (currentState.stateId.Contains("Sex"))
        {
            GameManager.Instance.MarkSexUsed();
        }
        else if (currentState.stateId.Contains("Working"))
        {
            GameManager.Instance.MarkWorkingUsed();
        }
        else if (currentState.stateId.Contains("Talking"))
        {
            // Talking is once-per-day-per-area
            GameManager.Instance.MarkTalkingUsedInArea(GameManager.Instance.Area);
        }
        else if (currentState.stateId.Contains("Exercise"))
        {
            GameManager.Instance.MarkExerciseUsed();
        }
        // Sleep tracking is handled directly in HandleSleepButtonClick
    }
    
    /// <summary>
    /// Progress time to next day at the specified hour/minute
    /// </summary>
    private void ProgressToNextDay(int targetHour, int targetMinute = 0)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.AdvanceToNextDayMorning(targetHour, targetMinute);
    }
    
    /// <summary>
    /// Transition popup to a different state
    /// </summary>
    private void TransitionToState(string stateId)
    {
        PopupState newState = ActionConfirmPopupDatabase.GetState(stateId);
        if (newState != null)
        {
            // Check if this is a result state (first button non-interactable with action)
            bool isResultState = newState.buttons != null &&
                                newState.buttons.Length > 0 &&
                                !newState.buttons[0].isInteractable &&
                                newState.buttons[0].isEnabled &&
                                newState.buttons[0].action != null;

            if (isResultState)
            {
                if (TryShowEnergyNotice(newState.buttons[0].action))
                    return;

                // IMMEDIATELY execute the action (grant stats, advance time)
                ExecuteResultStateAction(newState.buttons[0].action);

                // Change layout to show result
                ApplyPopupState(newState);

                // Start auto-close timer (2.5 seconds)
                if (autoCloseCoroutine != null)
                {
                    StopCoroutine(autoCloseCoroutine);
                }
                autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay(2.5f));
            }
            else
            {
                // Normal state transition (not a result state)
                ApplyPopupState(newState);
            }
        }
        else
        {
            Debug.LogWarning($"[ActionConfirmPopup] Failed to transition to state: {stateId}");
        }
    }

    /// <summary>
    /// Execute button action asynchronously (for result state actions)
    /// </summary>
    private async Task ExecuteButtonActionAsync(PopupButtonAction action, int buttonIndex)
    {
        if (action == null) return;

        switch (action)
        {
            case PopupButtonAction.ProgressTime pt:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AdvanceTimePublic(TimeSpan.FromHours(pt.Hours));
                }
                break;

            case PopupButtonAction.ProgressTimeToNextDay ptnd:
                ProgressToNextDay(ptnd.TargetHour, ptnd.TargetMinute);
                break;

            case PopupButtonAction.ChangeStat cs:
                if (cs.Target == RewardTarget.Player)
                {
                    if (GameManager.Instance?.Player != null)
                    {
                        await GameManager.Instance.Player.DoAction(
                            new CharacterActions.ChangeStat(cs.Target, cs.Stat, cs.Amount));
                    }
                }
                else if (cs.Target == RewardTarget.Mai)
                {
                    if (GameManager.Instance?.DataManager != null)
                    {
                        var mai = GameManager.Instance.DataManager.GetCurrentBoss();
                        if (mai != null)
                        {
                            await mai.DoAction(
                                new CharacterActions.ChangeStat(cs.Target, cs.Stat, cs.Amount));
                        }
                    }
                }
                break;

            case PopupButtonAction.ChangeMaxStat cms:
                if (cms.Target == RewardTarget.Player)
                {
                    if (GameManager.Instance?.Player != null)
                    {
                        await GameManager.Instance.Player.DoAction(
                            new CharacterActions.ChangeMaxStat(cms.Target, cms.Stat, cms.Amount));
                    }
                }
                else if (cms.Target == RewardTarget.Mai)
                {
                    if (GameManager.Instance?.DataManager != null)
                    {
                        var mai = GameManager.Instance.DataManager.GetCurrentBoss();
                        if (mai != null)
                        {
                            await mai.DoAction(
                                new CharacterActions.ChangeMaxStat(cms.Target, cms.Stat, cms.Amount));
                        }
                    }
                }
                break;

            case PopupButtonAction.TransitionState ts:
                TransitionToState(ts.TargetStateId);
                break;
                
            case PopupButtonAction.AddWorkProgress awp:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddWorkProgress(awp.Amount);
                }
                break;
                
            case PopupButtonAction.ChangeResource cr:
                if (cr.Target == RewardTarget.Player)
                {
                    if (GameManager.Instance?.Player != null)
                    {
                        await GameManager.Instance.Player.DoAction(
                            new CharacterActions.ChangeResource(cr.Target, cr.Resource, cr.Amount));
                    }
                }
                else if (cr.Target == RewardTarget.Mai)
                {
                    if (GameManager.Instance?.DataManager != null)
                    {
                        var mai = GameManager.Instance.DataManager.GetCurrentBoss();
                        if (mai != null)
                        {
                            await mai.DoAction(
                                new CharacterActions.ChangeResource(cr.Target, cr.Resource, cr.Amount));
                        }
                    }
                }
                break;
                
            case PopupButtonAction.ApplyWorkRewards awr:
                await ApplyWorkRewardsBasedOnLevel(awr.IsHardWork);
                break;

            // Note: ClosePopup is handled separately via auto-close timer
        }
    }

    /// <summary>
    /// Execute result state action immediately (stats grant, time progress)
    /// Skips ClosePopup action as that's handled by auto-close timer
    /// </summary>
    private async void ExecuteResultStateAction(PopupButtonAction action)
    {
        if (action == null) return;

        // If it's MultipleActions, execute each EXCEPT ClosePopup
        if (action is PopupButtonAction.MultipleActions ma)
        {
            foreach (var subAction in ma.Actions)
            {
                // Skip ClosePopup - we'll handle closing via auto-timer
                if (subAction is PopupButtonAction.ClosePopup)
                    continue;

                // Execute all other actions (ChangeStat, ProgressTime, etc.)
                await ExecuteButtonActionAsync(subAction, 0);
            }
        }
        else if (action is not PopupButtonAction.ClosePopup)
        {
            // Single action that's not ClosePopup
            await ExecuteButtonActionAsync(action, 0);
        }
    }

    /// <summary>
    /// Auto-close popup after delay
    /// </summary>
    private IEnumerator AutoCloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Close the popup
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.HidePanel(PanelId);
        }

        autoCloseCoroutine = null;
    }

    /// <summary>
    /// Override Hide to cancel auto-close timer
    /// </summary>
    public override void Hide()
    {
        // Cancel auto-close coroutine if running
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }

        // Reset all button GameObjects to active state for clean next show
        // This prevents issues when switching between actions with different button counts
        // (e.g., Talk has 2 buttons, Work has 3 buttons)
        if (button1GameObject != null) button1GameObject.SetActive(true);
        if (button2GameObject != null) button2GameObject.SetActive(true);
        if (button3GameObject != null) button3GameObject.SetActive(true);
        
        // Reset boost text and result text visibility for clean next show
        SetBoostTextVisible(true);
        SetResultTextVisible(false);

        base.Hide();
    }

    
    // ========== PROGRESS LAYOUT METHODS ==========
    
    /// <summary>
    /// Toggle between standard and Progress layouts
    /// </summary>
    private void SetProgressLayoutActive(bool active)
    {
        // Hide/show standard elements
        if (iconImage != null)
            iconImage.gameObject.SetActive(!active);
        if (headerText != null)
            headerText.gameObject.SetActive(!active);
        if (button1GameObject != null)
            button1GameObject.SetActive(!active);
        if (button2GameObject != null)
            button2GameObject.SetActive(!active);
        if (button3GameObject != null)
            button3GameObject.SetActive(!active);
        
        // Hide boost/result text when Progress layout is active
        // (these belong to the standard layout and overlap with Progress UI)
        SetBoostTextVisible(!active);
        SetResultTextVisible(!active);
        
        // Show/hide Progress layout
        if (progressLayoutContainer != null)
            progressLayoutContainer.SetActive(active);
    }
    
    /// <summary>
    /// Apply Progress-specific layout with work level data
    /// </summary>
    private void ApplyProgressLayout()
    {
        if (GameManager.Instance == null) return;
        
        int workLevel = GameManager.Instance.GetWorkLevel();
        int workProgress = GameManager.Instance.GetWorkProgress();
        int nextLevelReq = GameManager.Instance.GetNextLevelRequirement();
        
        // Update localized value displays.
        if (progressWorkLevelValueText != null)
        {
            SetInteger(progressWorkLevelValueText, workLevel);
        }
        
        if (progressPointsValueText != null)
        {
            SetInteger(progressPointsValueText, workProgress);
        }
        
        if (progressNextLevelValueText != null)
        {
            if (nextLevelReq > 0)
            {
                SetInteger(progressNextLevelValueText, nextLevelReq);
            }
            else
            {
                SetValueText(progressNextLevelValueText, "-");
            }
        }
    }
    
    
    /// <summary>
    /// Handle Progress promote button click
    /// </summary>
    private void OnProgressPromoteClicked()
    {
        if (GameManager.Instance == null) return;
        
        bool promoted = GameManager.Instance.TryPromoteWorkLevel();
        
        if (promoted)
        {
            int newLevel = GameManager.Instance.GetWorkLevel();
            // Show success notice with localized message
            NoticeUI.ShowLocalized("Action Confirm Progress Promote Success");
            
            // Refresh the Progress layout to show updated values
            ApplyProgressLayout();
        }
        else
        {
            // Show failure notice
            NoticeUI.ShowLocalized("Action Confirm Progress Promote Fail");
        }
    }

    
    /// <summary>
    /// Apply work rewards based on current work level using DefaultSettings.GetWorkRewards()
    /// </summary>
    /// <param name="isHardWork">If true, applies 2x multiplier on top of level multiplier</param>
    private async Task ApplyWorkRewardsBasedOnLevel(bool isHardWork = false)
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null) return;
        
        int workLevel = GameManager.Instance.GetWorkLevel();
        var (money, knowledge, progress, stamina, timeHours) = Base.DefaultSettings.GetWorkRewards(workLevel);
        
        // Apply hard work multiplier (2x on money, knowledge, and progress, more stamina drain)
        if (isHardWork)
        {
            money *= 2;
            knowledge *= 2;
            progress *= 2;
            stamina = -50; // Hard work drains more energy
        }
        
        // Apply Money
        await GameManager.Instance.Player.DoAction(
            new CharacterActions.ChangeResource(RewardTarget.Player, BasicResource.Money, money));
        
        // Apply Knowledge
        await GameManager.Instance.Player.DoAction(
            new CharacterActions.ChangeStat(RewardTarget.Player, BasicStats.Knowledge, knowledge));
        
        // Apply Stamina (energy consumption)
        await GameManager.Instance.Player.DoAction(
            new CharacterActions.ChangeStat(RewardTarget.Player, BasicStats.Stamina, stamina));
        
        // Add work progress
        GameManager.Instance.AddWorkProgress(progress);
        
        // Advance time
        GameManager.Instance.AdvanceTimePublic(TimeSpan.FromHours(timeHours));
    }

    /// <summary>
    /// Validate UI references in Editor
    /// </summary>
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-find components if not assigned
        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>();
        }
        
        if (headerText == null)
        {
            headerText = GetComponentInChildren<Base.Localization.LocalizedText>();
        }
    }
    #endif
}


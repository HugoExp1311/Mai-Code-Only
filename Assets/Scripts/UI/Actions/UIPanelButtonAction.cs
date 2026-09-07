using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Base;
using Base.Settings;
using Base.Character.Action;
using Base.UI.Popup;
using EventBus;
using Events;

/// <summary>
/// Component-based button action for panel operations with integrated sound support
/// Add this to any Button to control panels - no code required!
/// </summary>
[RequireComponent(typeof(Button))]
public class UIPanelButtonAction : MonoBehaviour
{
    public enum ActionType
    {
        Show,        // Show a panel
        Hide,        // Hide a panel
        Toggle,      // Toggle panel visibility
        Chain,       // Hide one panel, show another (for transitions)
        SwitchTab,    // Switch to a tab within a panel
        SwitchSection, // Switch to a different game section
        ChangeArea,  // Change game area/place
        ShowConfirmPopup, // Show Action Confirm Popup with configurable data
        Custom,      // Custom action (handled by external code, e.g., MaiPanel)
    }

    public enum ParentPanelBehavior
    {
        Hide,           // Hide parent panel (removes from stack, breaks breadcrumbs)
        KeepVisible     // Keep parent panel visible (stays in stack, enables breadcrumbs)
    }

    [Header("Action Configuration")]
    [Tooltip("Type of action to perform when button is clicked")]
    [SerializeField] private ActionType actionType = ActionType.Show;

    [Header("Show Panel Settings")]
    [Tooltip("Panel ID to show (only used for Show/Toggle actions)")]
    [SerializeField] private string showPanelId = "";

    [Tooltip("Data to pass to panel when showing (optional)")]
    [SerializeField] private bool passData = false;

    [Tooltip("Delay before showing panel (seconds)")]
    [SerializeField] private float showDelay = 0f;

    [Tooltip("How to handle parent panel when showing new panel. Hide: removes from stack. KeepVisible: stays in stack for breadcrumbs.")]
    [SerializeField] private ParentPanelBehavior parentPanelBehavior = ParentPanelBehavior.Hide;

    [Header("Hide Panel Settings")]
    [Tooltip("Panel ID to hide (only used for Hide/Toggle actions). Leave empty to auto-detect parent panel.")]
    [SerializeField] private string hidePanelId = "";

    [Tooltip("Hide current panel instead of specific panel ID")]
    [SerializeField] private bool hideCurrentPanel = false;

    [Header("Chain Action Settings")]
    [Tooltip("Panel ID to hide first (only used for Chain action)")]
    [SerializeField] private string chainHidePanelId = "";

    [Tooltip("Panel ID to show after hiding (only used for Chain action)")]
    [SerializeField] private string chainShowPanelId = "";

    [Tooltip("Data to pass when showing in chain (optional)")]
    [SerializeField] private bool chainPassData = false;
    
    // Property to access chainPassData (prevents CS0414 warning)
    public bool ChainPassData => chainPassData;

    [Header("Section Switch Settings")]
    [Tooltip("Target section to switch to (only used for SwitchSection action)")]
    [SerializeField] private GameSection targetSection = GameSection.MainMenu;

    [Tooltip("Use transition panel when switching sections (only used for SwitchSection action)")]
    [SerializeField] private bool useSectionTransition = true;

    [Header("Tab Switch Settings")]
    [Tooltip("Tab ID to switch to (only used for SwitchTab action)")]
    [SerializeField] private string tabId = "";

    [Tooltip("Parent panel ID containing the tabs (only used for SwitchTab action). Leave empty to auto-detect parent panel.")]
    [SerializeField] private string parentPanelId = "";

    [Header("Sound Settings")]
    [Tooltip("Type of sound to play when button is clicked")]
    [SerializeField] private SoundType soundType = SoundType.Normal;

    [Tooltip("Specific area for place-specific sounds (only used if Sound Type is Place)")]
    [SerializeField] private Area specificArea = Area.Home;

    [Header("Show Confirm Popup Settings")]
    [Tooltip("Daily action to trigger (Sleep, Eating, Sex) - content configured in ActionConfirmPopupDatabase")]
    [SerializeField] private PlayerDailyAction dailyAction = PlayerDailyAction.None;

    [Tooltip("Icon sprites for popup states (optional - overrides database icons if set)\n" +
             "Index 0 = Initial state, Index 1 = Secondary state (e.g., Eating with Mai), etc.")]
    [SerializeField] private Sprite[] actionIcons;

    [Tooltip("Panel ID for the Action Confirm Popup (default: 'Action Confirm Popup')")]
    [SerializeField] private string confirmPopupPanelId = "Action Confirm Popup";


    [Header("Custom Action Settings")]
    [Tooltip("Description of what this custom action does (only used for Custom action type)")]
    [TextArea(2, 5)]
    [SerializeField] private string customActionDescription = "";
    
    // Property to access customActionDescription (prevents CS0414 warning)
    public string CustomActionDescription => customActionDescription;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [SerializeField] private Button button;

    // Event bindings for dynamic button visibility
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<TimeChangedEvent> timeChangedBinding;

    private void Awake()
    {
        // Button should be assigned via serialized field in Inspector
        // RequireComponent ensures it exists, but validate assignment
        if (button == null)
        {
            Debug.LogError($"[UIPanelButtonAction] Button component not assigned in Inspector on {gameObject.name}!");
            return;
        }

        // Automatically wire up button click
        button.onClick.AddListener(OnButtonClick);

        // Create and register event bindings (persists even when GameObject is disabled)
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(OnPlaceChanged);
        timeChangedBinding = new EventBinding<TimeChangedEvent>(OnTimeChanged);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<TimeChangedEvent>.Register(timeChangedBinding);
    }

    private void Start()
    {
        // Set initial button visibility based on current area
        UpdateButtonVisibility();
    }

    private void OnPlaceChanged(PlaceChangedEvent evt)
    {
        UpdateButtonVisibility();
    }

    private void OnTimeChanged(TimeChangedEvent evt)
    {
        UpdateButtonVisibility();
    }

    private void OnDestroy()
    {
        // Cleanup button listener
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }

        // Deregister event bindings
        if (placeChangedBinding != null)
        {
            EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
            placeChangedBinding = null;
        }

        if (timeChangedBinding != null)
        {
            EventBus<TimeChangedEvent>.Deregister(timeChangedBinding);
            timeChangedBinding = null;
        }
    }

    private void OnButtonClick()
    {
        // Play sound first (if configured)
        PlayButtonSound();

        // Then execute action
        switch (actionType)
        {
            case ActionType.Show:
                ExecuteShowAction();
                break;

            case ActionType.Hide:
                ExecuteHideAction();
                break;

            case ActionType.Toggle:
                ExecuteToggleAction();
                break;

            case ActionType.Chain:
                ExecuteChainAction();
                break;

            case ActionType.SwitchSection:
                ExecuteSwitchSectionAction();
                break;

            case ActionType.SwitchTab:
                ExecuteSwitchTabAction();
                break;

            case ActionType.ChangeArea:
                ExecuteChangeAreaAction();
                break;

            case ActionType.ShowConfirmPopup:
                ExecuteShowConfirmPopupAction();
                break;

            case ActionType.Custom:
                // Custom actions are handled by external code (e.g., MaiPanel)
                // This component only plays sound, actual action is handled elsewhere
                break;
        }
    }

    private void ExecuteShowAction()
    {
        if (string.IsNullOrEmpty(showPanelId))
        {
            LogError("Show Panel ID is empty!");
            return;
        }

        // Hide parent panel before showing new panel (unless it's a Base panel)
        HideParentPanelIfNeeded();

        if (showDelay > 0f)
        {
            StartCoroutine(ShowWithDelay());
        }
        else
        {
            ShowPanel(showPanelId);
        }
    }

    private IEnumerator ShowWithDelay()
    {
        yield return new WaitForSeconds(showDelay);
        ShowPanel(showPanelId);
    }

    private void ExecuteHideAction()
    {
        if (hideCurrentPanel)
        {
            HideCurrentPanel();
        }
        else
        {
            // If hidePanelId is empty, try to find parent panel automatically (runtime fallback)
            string targetPanelId = hidePanelId;

            if (string.IsNullOrEmpty(targetPanelId))
            {
                targetPanelId = FindParentPanelId();

                if (string.IsNullOrEmpty(targetPanelId))
                {
                    LogError("Hide Panel ID is empty and no parent UIPanel found!");
                    return;
                }

                if (debugMode)
                {
                    Log($"Auto-detected parent panel at runtime: {targetPanelId}");
                }
            }

            HidePanel(targetPanelId);
        }
    }

    /// <summary>
    /// Traverse up the parent hierarchy to find the first UIPanel component (runtime fallback)
    /// Note: Editor script provides auto-fill button for better UX in Inspector
    /// </summary>
    private string FindParentPanelId()
    {
        Transform current = transform.parent;

        while (current != null)
        {
            var panel = current.GetComponent<UIPanel>();
            if (panel != null)
            {
                return panel.PanelId;
            }

            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// Hide parent panel if it exists and is not a Base panel
    /// Base panels should not be hidden as they are persistent screens
    /// Behavior depends on parentPanelBehavior setting:
    /// - Hide: Hides parent panel (removes from stack)
    /// - KeepVisible: Keeps parent visible (stays in stack for breadcrumbs)
    /// </summary>
    private void HideParentPanelIfNeeded()
    {
        // If behavior is KeepVisible, don't hide parent panel (enables breadcrumbs)
        if (parentPanelBehavior == ParentPanelBehavior.KeepVisible)
            return;

        if (UIPanelManager.Instance == null)
            return;

        string parentPanelId = FindParentPanelId();
        if (string.IsNullOrEmpty(parentPanelId))
            return;

        // Check if parent panel is a Base panel - Base panels should not be hidden
        var parentPanel = UIPanelManager.Instance.GetPanel(parentPanelId);
        if (parentPanel != null && parentPanel.IsBasePanel)
        {
            // Skip hiding Base panels
            return;
        }

        // Hide the parent panel (removes from stack)
        HidePanel(parentPanelId);
    }

    private void ExecuteToggleAction()
    {
        if (string.IsNullOrEmpty(showPanelId))
        {
            LogError("Toggle Panel ID is empty!");
            return;
        }

        var panel = UIPanelManager.Instance?.GetPanel(showPanelId);
        if (panel != null)
        {
            if (panel.IsVisible)
            {
                panel.Hide();
            }
            else
            {
                ShowPanel(showPanelId);
            }
        }
        else
        {
            LogError($"Panel not found: {showPanelId}");
        }
    }

    private void ExecuteChainAction()
    {
        if (string.IsNullOrEmpty(chainHidePanelId) || string.IsNullOrEmpty(chainShowPanelId))
        {
            LogError("Chain action requires both Hide Panel ID and Show Panel ID!");
            return;
        }

        // Hide first panel
        HidePanel(chainHidePanelId);

        // Then show second panel (with small delay for smooth transition)
        StartCoroutine(ChainShowDelayed());
    }

    private IEnumerator ChainShowDelayed()
    {
        yield return new WaitForSeconds(0.1f); // Small delay for smooth transition
        ShowPanel(chainShowPanelId);
    }

    private void ExecuteSwitchSectionAction()
    {
        Debug.Log($"[UIPanelButtonAction] ExecuteSwitchSectionAction called: targetSection={targetSection}, useSectionTransition={useSectionTransition}");
        
        if (UIPanelManager.Instance == null)
        {
            LogError("UIPanelManager.Instance is null! Make sure UIPanelManager exists in scene.");
            return;
        }

        Debug.Log($"[UIPanelButtonAction] Calling UIPanelManager.SwitchToSection({targetSection}, {useSectionTransition})");
        UIPanelManager.Instance.SwitchToSection(targetSection, useSectionTransition);
        Log($"Switch to section: {targetSection}");
    }

    private void ExecuteSwitchTabAction()
    {
        if (string.IsNullOrEmpty(tabId))
        {
            LogError("Tab ID is empty!");
            return;
        }

        if (UIPanelManager.Instance == null)
        {
            LogError("UIPanelManager.Instance is null! Make sure UIPanelManager exists in scene.");
            return;
        }

        // Find parent panel
        string targetPanelId = parentPanelId;

        if (string.IsNullOrEmpty(targetPanelId))
        {
            targetPanelId = FindParentPanelId();

            if (string.IsNullOrEmpty(targetPanelId))
            {
                LogError("Parent Panel ID is empty and no parent UIPanel found!");
                return;
            }

            if (debugMode)
            {
                Log($"Auto-detected parent panel at runtime: {targetPanelId}");
            }
        }

        // Get current section
        GameSection currentSection = UIPanelManager.Instance.GetCurrentSection();

        // Try to find panel in current section first
        UIPanel parentPanel = null;
        var panelsInSection = UIPanelManager.Instance.GetPanelsInSection(currentSection);
        foreach (var panel in panelsInSection)
        {
            if (panel != null && panel.PanelId == targetPanelId)
            {
                parentPanel = panel;
                break;
            }
        }

        // If not found in current section, try section-independent panels
        if (parentPanel == null)
        {
            var sectionIndependentPanels = UIPanelManager.Instance.GetSectionIndependentPanels();
            foreach (var panel in sectionIndependentPanels)
            {
                if (panel != null && panel.PanelId == targetPanelId)
                {
                    parentPanel = panel;
                    break;
                }
            }
        }

        // If still not found, fall back to global lookup (but validate section)
        if (parentPanel == null)
        {
            parentPanel = UIPanelManager.Instance.GetPanel(targetPanelId);

            if (parentPanel != null)
            {
                // Validate that panel belongs to current section or is section-independent
                if (!parentPanel.IsSectionIndependent && parentPanel.GameSection != currentSection)
                {
                    LogError($"Parent panel '{targetPanelId}' belongs to section {parentPanel.GameSection} but current section is {currentSection}. Cannot switch tabs.");
                    return;
                }
            }
        }

        if (parentPanel == null)
        {
            LogError($"Parent panel not found: {targetPanelId}");
            return;
        }

        // Get TabManager from parent panel
        var tabManager = parentPanel.GetComponent<TabManager>();
        if (tabManager == null)
        {
            LogError($"TabManager component not found on parent panel: {targetPanelId}");
            return;
        }

        // Switch to the tab
        tabManager.SwitchTab(tabId);
        Log($"Switch to tab: {tabId} in panel: {targetPanelId}");
    }

    /// <summary>
    /// Execute change area action - changes game area and optionally hides current panel on success
    /// </summary>
    private void ExecuteChangeAreaAction()
    {
        if (GameManager.Instance == null)
        {
            LogError("GameManager.Instance is null! Make sure GameManager exists in scene.");
            return;
        }

        // Store previous area before change
        Area previousArea = GameManager.Instance.Area;

        // Store current panel ID BEFORE area change to avoid hiding panels that appear as a result of the change
        // (e.g., MaiPanel appears when changing to Home, but we want to hide Place Popup, not MaiPanel)
        string panelToHideId = null;
        if (hideCurrentPanel && UIPanelManager.Instance != null)
        {
            var panelBeforeChange = UIPanelManager.Instance.GetCurrentPanel();
            if (panelBeforeChange != null)
            {
                panelToHideId = panelBeforeChange.PanelId;
            }
        }

        // Change area using specificArea field
        GameManager.Instance.ChangeArea((int)specificArea);

        // Hide panel that was visible before area change (not panels that appear as a result of the change)
        if (GameManager.Instance.Area != previousArea)
        {
            if (hideCurrentPanel)
            {
                // Only hide if we stored a panel ID and it's not MaiPanel (which may appear as a result of area change)
                if (!string.IsNullOrEmpty(panelToHideId) && panelToHideId != "Mai In Game")
                {
                    var panelToHide = UIPanelManager.Instance?.GetPanel(panelToHideId);
                    if (panelToHide != null && !panelToHide.IsBasePanel)
                    {
                        HidePanel(panelToHideId);
                    }
                }
            }
            else
            {
                // Try to find and hide parent panel
                string parentPanelId = FindParentPanelId();
                if (!string.IsNullOrEmpty(parentPanelId))
                {
                    // Check if parent panel is a Base panel - Base panels should not be hidden individually
                    var parentPanel = UIPanelManager.Instance?.GetPanel(parentPanelId);
                    if (parentPanel != null && parentPanel.IsBasePanel)
                    {
                        // Skip hiding Base panels
                    }
                    else
                    {
                        HidePanel(parentPanelId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Execute show confirm popup action - shows Action Confirm Popup with database-driven content
    /// </summary>
    private void ExecuteShowConfirmPopupAction()
    {
        if (UIPanelManager.Instance == null)
        {
            LogError("UIPanelManager.Instance is null! Make sure UIPanelManager exists in scene.");
            return;
        }

        if (dailyAction == PlayerDailyAction.None)
        {
            LogError("Daily action is set to None! Please select a valid action (Sleep, Eating, Sex).");
            return;
        }

        // Special check for Sex action: Mai must be at least "Ready to have sex".
        // Blocked while "Not interested" or "Normal".
        if (dailyAction == PlayerDailyAction.Sex)
        {
            if (GameManager.Instance != null && GameManager.Instance.DataManager != null)
            {
                var mai = GameManager.Instance.DataManager.GetCurrentBoss() as Base.Character.Target;
                if (mai != null && !mai.IsLibidoReadyForSex())
                {
                    // Show NoticeUI with localized message
                    NoticeUI.ShowLocalized("Notice Libido Too Low");

                    if (debugMode)
                    {
                        Log($"Sex unavailable: Libido state={mai.GetLibidoState()}, days={mai.GetLibido()}, required state>={DefaultSettings.LibidoReadyStateIndex}");
                    }

                    return; // Don't show popup
                }
            }
        }

        // Check availability (time window + usage)
        if (GameManager.Instance != null)
        {
            DateTime currentTime = GameManager.Instance.Time;

            if (!IsActionAvailableAndUsable(dailyAction, currentTime))
            {
                // Action unavailable - show NoticeUI message
                ShowUnavailableNotice();

                if (debugMode)
                {
                    bool timeOk = IsDailyActionAvailable(dailyAction, currentTime);
                    bool usedToday = HasBeenUsedToday(dailyAction);
                    Log($"{dailyAction} unavailable: TimeOK={timeOk}, UsedToday={usedToday}, CurrentTime={currentTime:HH:mm}");
                }

                return; // Don't show popup
            }
        }

        // Action is available - get initial popup state from database
        string actionName = dailyAction.ToString(); // "Sleep", "Eating", "Sex"
        PopupState initialState = ActionConfirmPopupDatabase.GetInitialState(actionName);

        if (initialState == null)
        {
            LogError($"No popup state found for action: {actionName}");
            return;
        }

        // Override icon if one is set in Inspector (index 0 = initial state)
        if (actionIcons != null && actionIcons.Length > 0 && actionIcons[0] != null)
        {
            initialState.iconSprite = actionIcons[0];
        }

        // Pass icon array to popup for state transitions
        if (actionIcons != null && actionIcons.Length > 0)
        {
            ActionConfirmPopupDatabase.SetCustomIcons(actionName, actionIcons);
        }

        // Show the popup with state data
        string popupId = string.IsNullOrEmpty(confirmPopupPanelId) ? "Action Confirm Popup" : confirmPopupPanelId;
        UIPanelManager.Instance.ShowPanel(popupId, initialState);

        Log($"Show confirm popup: {popupId} for action: {actionName}");
    }

    private void ShowPanel(string panelId)
    {
        if (UIPanelManager.Instance == null)
        {
            LogError("UIPanelManager.Instance is null! Make sure UIPanelManager exists in scene.");
            return;
        }

        object data = passData ? CreateDataForPanel(panelId) : null;
        UIPanelManager.Instance.ShowPanel(panelId, data);

        Log($"Show panel: {panelId}");
    }

    private void HidePanel(string panelId)
    {
        if (UIPanelManager.Instance == null)
        {
            LogError("UIPanelManager.Instance is null!");
            return;
        }

        UIPanelManager.Instance.HidePanel(panelId);
        Log($"Hide panel: {panelId}");
    }

    private void HideCurrentPanel()
    {
        if (UIPanelManager.Instance == null)
        {
            LogError("UIPanelManager.Instance is null!");
            return;
        }

        UIPanelManager.Instance.HideCurrentPanel();
        Log("Hide current panel");
    }

    /// <summary>
    /// Override this method to provide custom data for panels
    /// </summary>
    protected virtual object CreateDataForPanel(string panelId)
    {
        // Default: return null (no data)
        // Override in derived classes if you need to pass specific data
        return null;
    }

    /// <summary>
    /// Play button sound effect based on configuration
    /// </summary>
    private void PlayButtonSound()
    {
        if (soundType == SoundType.None)
            return;

        if (AudioManager.Instance == null || FMODEvents.Instance == null)
        {
            if (debugMode)
            {
                LogWarning("AudioManager or FMODEvents instance is null - cannot play button sound");
            }
            return;
        }

        // For ChangeArea action type, check availability before playing sound
        if (actionType == ActionType.ChangeArea)
        {
            // Check if the area is available before playing sound
            if (!IsPlaceAvailable(specificArea))
            {
                if (debugMode)
                {
                    Log($"{specificArea} is unavailable, not playing sound for ChangeArea action.");
                }
                return; // Do not play sound if unavailable
            }
        }

        switch (soundType)
        {
            case SoundType.Normal:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnButton);
                Log("Play sound: Normal button");
                break;

            case SoundType.Back:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnBack);
                Log("Play sound: Back button");
                break;

            case SoundType.Action:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnAction);
                Log("Play sound: Action");
                break;

            case SoundType.Place:
                PlayPlaceSound(); // PlayPlaceSound already has its own availability check
                break;
        }
    }

    /// <summary>
    /// Play place-specific sound with availability check
    /// </summary>
    private void PlayPlaceSound()
    {
        if (FMODEvents.Instance.OnPlaces == null || FMODEvents.Instance.OnPlaces.Length == 0)
        {
            LogWarning("OnPlaces array is not configured in FMODEvents");
            return;
        }

        int areaIndex = (int)specificArea;
        if (areaIndex < 0 || areaIndex >= FMODEvents.Instance.OnPlaces.Length)
        {
            LogWarning($"Area index {areaIndex} is out of range for OnPlaces array");
            return;
        }

        // Check if the place is available before playing SFX
        bool isAvailable = IsPlaceAvailable(specificArea);

        if (debugMode)
        {
            Log($"{specificArea} availability check result: {isAvailable}");
        }

        if (isAvailable)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnPlaces[areaIndex]);
            Log($"Play sound: Place ({specificArea})");
        }
        else
        {
            // Don't play place SFX - the unavailable event will handle the SFX
            if (debugMode)
            {
                Log($"{specificArea} is unavailable, not playing place SFX");
            }
        }
    }

    /// <summary>
    /// Check if a place is available at the estimated arrival time
    /// </summary>
    private bool IsPlaceAvailable(Area targetArea)
    {
        // Only check availability for Company and Park (the restricted areas)
        if (targetArea != Area.Company && targetArea != Area.Park)
            return true;

        if (GameManager.Instance == null)
        {
            if (debugMode)
            {
                LogWarning("GameManager.Instance is null - assuming place is available");
            }
            return true; // Default to available if GameManager doesn't exist
        }

        int travelTime = GetTravelTime(targetArea);
        System.TimeSpan travelDuration = System.TimeSpan.FromMinutes(travelTime);
        System.DateTime estimatedArrivalTime = GameManager.Instance.Time + travelDuration;

        bool isUnavailable = IsAreaUnavailableAtTime(targetArea, estimatedArrivalTime);

        if (debugMode)
        {
            Log($"{targetArea} at {estimatedArrivalTime:HH:mm} - isUnavailable: {isUnavailable}");
        }

        return !isUnavailable;
    }

    /// <summary>
    /// Get travel time to an area
    /// </summary>
    private int GetTravelTime(Area area)
    {
        return DefaultSettings.GetTravelTime(area);
    }

    /// <summary>
    /// Check if area is unavailable at a specific time
    /// </summary>
    private bool IsAreaUnavailableAtTime(Area area, System.DateTime timeToCheck)
    {
        return DefaultSettings.IsAreaUnavailableAtTime(area, timeToCheck);
    }

    /// <summary>
    /// Check if completing an action would push the time into the area's unavailable period
    /// </summary>
    private bool WouldActionExceedAreaAvailability(PlayerDailyAction action, DateTime currentTime, Area currentArea)
    {
        // Get the action's time duration
        int actionTimeHours = GetActionTimeDuration(action);
        if (actionTimeHours <= 0)
            return false; // No time progression, no issue
        
        // Calculate end time after action completes
        DateTime endTime = currentTime.AddHours(actionTimeHours);
        
        // Check if end time would be in the area's unavailable period
        return IsAreaUnavailableAtTime(currentArea, endTime);
    }
    
    /// <summary>
    /// Get the time duration (in hours) for a daily action
    /// </summary>
    private int GetActionTimeDuration(PlayerDailyAction action)
    {
        switch (action)
        {
            case PlayerDailyAction.Working:
                // Work uses level-based rewards, but time is always 8 hours
                return Base.DefaultSettings.WorkBaseTimeHours;
                
            case PlayerDailyAction.Sleep:
                // Sleep can be 2 or 8 hours - use max to be safe
                return 8;
                
            case PlayerDailyAction.Eating:
                if (Base.DefaultSettings.DailyActionRewards.TryGetValue(
                    Base.DefaultSettings.DailyActionVariant.EatingWithMai, out var eatingReward))
                    return eatingReward.TimeHours;
                return 1;
                
            case PlayerDailyAction.Talking:
                if (Base.DefaultSettings.DailyActionRewards.TryGetValue(
                    Base.DefaultSettings.DailyActionVariant.Talking, out var talkingReward))
                    return talkingReward.TimeHours;
                return 1;
                
            case PlayerDailyAction.Exercise:
                // Exercise can be 2 hours for both normal and hard
                return 2;
                
            case PlayerDailyAction.Sex:
                // Sex progresses to next day, not a fixed duration
                return 0;
                
            case PlayerDailyAction.Progress:
                // Progress doesn't advance time
                return 0;
                
            default:
                return 0;
        }
    }

    private void Log(string message)
    {
        // Debug logging removed - use Unity's built-in logging if needed
        // if (debugMode)
        // {
        //     Debug.Log($"[UIPanelButtonAction] {gameObject.name}: {message}");
        // }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[UIPanelButtonAction] {gameObject.name}: {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[UIPanelButtonAction] {gameObject.name}: {message}");
    }

    /// <summary>
    /// Check if a daily action is within its time window
    /// </summary>
    private bool IsDailyActionAvailable(PlayerDailyAction action, DateTime currentTime)
    {
        if (action == PlayerDailyAction.None)
            return true;

        if (!DailyActionMetadata.TimeWindows.TryGetValue(action, out var availability))
            return true;

        if (!availability.HasValue)
            return true; // null = always available (Sleep, Working, Talking)

        double startHour = availability.Value.Start;
        double durationHours = availability.Value.Duration;

        DateTime startTime = currentTime.Date.AddHours(startHour);
        DateTime endTime = startTime.AddHours(durationHours);

        return currentTime >= startTime && currentTime < endTime;
    }

    /// <summary>
    /// Check if action has been used today (or in current area for per-area actions)
    /// </summary>
    private bool HasBeenUsedToday(PlayerDailyAction action)
    {
        if (GameManager.Instance == null)
            return false;

        switch (action)
        {
            case PlayerDailyAction.Eating:
                return GameManager.Instance.HasEatenToday();
            case PlayerDailyAction.Working:
                return GameManager.Instance.HasWorkedToday();
            case PlayerDailyAction.Talking:
                // Talking is once-per-day-per-area
                return GameManager.Instance.HasTalkedTodayInArea(GameManager.Instance.Area);
            case PlayerDailyAction.Exercise:
                return GameManager.Instance.HasExercisedToday();
            default:
                return false; // Sleep limits checked per-button at popup level, Sex = time jump handles it
        }
    }

    /// <summary>
    /// Check if action is available (time window + usage)
    /// </summary>
    private bool IsActionAvailableAndUsable(PlayerDailyAction action, DateTime currentTime)
    {
        // Check time window
        if (!IsDailyActionAvailable(action, currentTime))
            return false;

        // Check once-per-day usage (only Eating, Working, Talking)
        if (HasBeenUsedToday(action))
            return false;

        // Check area availability
        if (GameManager.Instance != null)
        {
            Area currentArea = GameManager.Instance.Area;
            if (!DailyActionMetadata.IsAvailableInArea(action, currentArea))
                return false;
            
            // Check if completing the action would push time into area's unavailable period
            if (WouldActionExceedAreaAvailability(action, currentTime, currentArea))
                return false;
        }

        return true;
    }


    /// <summary>
    /// Update button visibility based on current availability
    /// Hides/shows entire GameObject based on area, time, and usage restrictions
    /// </summary>
    private void UpdateButtonVisibility()
    {
        // CUSTOM ACTION FIX: Don't auto-manage visibility for Custom action types
        // Let the parent panel/external code control visibility
        if (actionType == ActionType.Custom)
            return;

        if (dailyAction == PlayerDailyAction.None)
        {
            gameObject.SetActive(true);
            return;
        }

        if (GameManager.Instance == null)
        {
            gameObject.SetActive(true);
            return;
        }

        Area currentArea = GameManager.Instance.Area;
        DateTime currentTime = GameManager.Instance.Time;
        
        // Check area availability
        bool isAvailableInArea = DailyActionMetadata.IsAvailableInArea(dailyAction, currentArea);
        
        // Check time window availability
        bool isAvailableInTimeWindow = IsDailyActionAvailable(dailyAction, currentTime);
        
        // Button is only visible if both area AND time window are valid
        gameObject.SetActive(isAvailableInArea && isAvailableInTimeWindow);
    }


    /// <summary>
    /// Show NoticeUI with "I don't want to do it now" message
    /// </summary>
    private void ShowUnavailableNotice()
    {
        if (UIPanelManager.Instance == null)
            return;

        // Show NoticeUI with unavailable message
        UIPanelManager.Instance.ShowPanel("Notice Popup", "I don't want to do it now");

        if (debugMode)
        {
            Log($"Showing unavailable notice for {dailyAction}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Validate configuration in Editor
    /// </summary>
    private void OnValidate()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        // Ensure button component exists
        if (GetComponent<Button>() == null && GetComponentInParent<Button>() == null)
        {
            Debug.LogWarning($"[UIPanelButtonAction] {gameObject.name}: Button component not found. This component requires a Button.");
        }

        // Auto-set sound type to Back when ActionType is Hide
        if (actionType == ActionType.Hide && soundType != SoundType.Back)
        {
            soundType = SoundType.Back;
        }
    }
#endif
}


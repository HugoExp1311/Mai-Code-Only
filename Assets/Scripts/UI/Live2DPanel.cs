using Base;
using Base.Dialogues;
using Base.CG;
using Events;
using EventBus;
using UnityEngine;
using Base.Settings;
using MaisLoveStory.Live2D;
using UI.SexPosition;

/// <summary>
/// Central panel that manages ALL Live2D character displays.
/// All Live2D models in the hierarchy are referenced here.
/// - Mai: Shows during dialogue with Live2D animations
/// - Staff: Shows when at HiepMart
/// - Missionary: Shows during Simulation section
/// </summary>
public class Live2DPanel : UIPanel
{
    [Header("Live2D Motion Controllers")]
    [Tooltip("Live2D Motion Controller for Mai (dialogue)")]
    [SerializeField] private Live2DMotionController maiMotionController;
    
    [Tooltip("Live2D Motion Controller for Staff (HiepMart)")]
    [SerializeField] private Live2DMotionController staffMotionController;
    
    [Tooltip("Live2D Motion Controller for Missionary (Simulation)")]
    [SerializeField] private Live2DMotionController missionaryMotionController;

    [Tooltip("Live2D Motion Controller for Cowgirl (Simulation)")]
    [SerializeField] private Live2DMotionController cowgirlMotionController;

    [Tooltip("Live2D Motion Controller for Roleplay Pussy (Simulation)")]
    [SerializeField] private Live2DMotionController roleplayPussyMotionController;

    [Tooltip("Live2D Motion Controller for Roleplay Butthole (Simulation)")]
    [SerializeField] private Live2DMotionController roleplayButtholeMotionController;

    [Tooltip("Live2D Motion Controller for Roleplay Blowjob (Simulation)")]
    [SerializeField] private Live2DMotionController roleplayBlowjobMotionController;

    [Tooltip("Live2D Motion Controller for Roleplay Paizuri (Simulation)")]
    [SerializeField] private Live2DMotionController roleplayPaizuriMotionController;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    
    #region Public API
    
    /// <summary>
    /// Get the Mai dialogue motion controller
    /// </summary>
    public Live2DMotionController MaiMotionController => maiMotionController;
    
    /// <summary>
    /// Get the Missionary simulation motion controller
    /// </summary>
    public Live2DMotionController MissionaryMotionController => missionaryMotionController;

    public Live2DMotionController CowgirlMotionController => cowgirlMotionController;

    /// <summary>
    /// Get the Roleplay Pussy simulation motion controller
    /// </summary>
    public Live2DMotionController RoleplayPussyMotionController => roleplayPussyMotionController;

    /// <summary>
    /// Get the Roleplay Butthole simulation motion controller
    /// </summary>
    public Live2DMotionController RoleplayButtholeMotionController =>
        roleplayButtholeMotionController;

    public Live2DMotionController RoleplayBlowjobMotionController =>
        roleplayBlowjobMotionController;

    public Live2DMotionController RoleplayPaizuriMotionController =>
        roleplayPaizuriMotionController;

    /// <summary>
    /// Get the simulation motion controller for a given position type.
    /// </summary>
    public Live2DMotionController GetSimulationMotionController(SexPositionConfigType positionType)
    {
        switch (positionType)
        {
            case SexPositionConfigType.RoleplayPussy:
                return roleplayPussyMotionController;
            case SexPositionConfigType.RoleplayButthole:
                return roleplayButtholeMotionController;
            case SexPositionConfigType.RoleplayBlowjob:
                return roleplayBlowjobMotionController;
            case SexPositionConfigType.RoleplayPaizuri:
                return roleplayPaizuriMotionController;
            case SexPositionConfigType.Cowgirl:
                return cowgirlMotionController;
            case SexPositionConfigType.Missionary:
            default:
                return missionaryMotionController;
        }
    }

    #endregion
    
    private void Awake()
    {
        // Initialize all Live2D models as inactive
        if (maiMotionController != null)
        {
            maiMotionController.gameObject.SetActive(false);
        }
        
        if (staffMotionController != null)
        {
            staffMotionController.gameObject.SetActive(false);
        }
        
        if (missionaryMotionController != null)
        {
            missionaryMotionController.gameObject.SetActive(false);
        }

        if (cowgirlMotionController != null)
        {
            cowgirlMotionController.gameObject.SetActive(false);
        }

        if (roleplayPussyMotionController != null)
        {
            roleplayPussyMotionController.gameObject.SetActive(false);
        }

        if (roleplayButtholeMotionController != null)
        {
            roleplayButtholeMotionController.gameObject.SetActive(false);
        }

        if (roleplayBlowjobMotionController != null)
        {
            roleplayBlowjobMotionController.gameObject.SetActive(false);
        }

        if (roleplayPaizuriMotionController != null)
        {
            roleplayPaizuriMotionController.gameObject.SetActive(false);
        }

        // Register event bindings in Awake (persistent, not dependent on GameObject enabled state)
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
    }
    
    private void Start()
    {
        // Subscribe to dialogue events in Start() - ensures DialogueManager.Instance is available
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart += HandleDialogueStart;
            DialogueManager.Instance.OnDialogueEnd += HandleDialogueEnd;
            DialogueManager.Instance.OnDialogueLineDisplayed += HandleDialogueLineDisplayed;
        }
        else
        {
            Debug.LogWarning("[Live2DPanel] DialogueManager.Instance is null! Cannot subscribe to dialogue events.");
        }
    }
    
    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart -= HandleDialogueStart;
            DialogueManager.Instance.OnDialogueEnd -= HandleDialogueEnd;
            DialogueManager.Instance.OnDialogueLineDisplayed -= HandleDialogueLineDisplayed;
        }
        
        // Ensure event bindings are deregistered
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
    }
    
    #region Dialogue Handlers
    
    /// <summary>
    /// Handle dialogue start - show Live2D Mai and play default motion
    /// Only active in InGame section (not CG section)
    /// </summary>
    private void HandleDialogueStart()
    {
        // Only respond to dialogue events in InGame section
        if (UIPanelManager.Instance != null && UIPanelManager.Instance.GetCurrentSection() != GameSection.InGame)
        {
            if (enableDebugLogs) Debug.Log($"[Live2DPanel] Ignoring dialogue start - not in InGame section (current: {UIPanelManager.Instance.GetCurrentSection()})");
            return;
        }
        
        ShowMaiLive2D();
        
        // Play default motion when dialogue starts
        if (maiMotionController != null)
        {
            if (enableDebugLogs) Debug.Log("[Live2DPanel] Calling PlayDefaultMotion at dialogue start");
            maiMotionController.PlayDefaultMotion();
        }
        else
        {
            Debug.LogWarning("[Live2DPanel] maiMotionController is NULL!");
        }
    }
    
    /// <summary>
    /// Handle dialogue end - hide Live2D Mai and play default motion
    /// Only active in InGame section (not CG section)
    /// </summary>
    private void HandleDialogueEnd()
    {
        // Only respond to dialogue events in InGame section
        if (UIPanelManager.Instance != null && UIPanelManager.Instance.GetCurrentSection() != GameSection.InGame)
        {
            if (enableDebugLogs) Debug.Log($"[Live2DPanel] Ignoring dialogue end - not in InGame section (current: {UIPanelManager.Instance.GetCurrentSection()})");
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.ShouldSuppressInGameUIForIntroTransition())
        {
            if (enableDebugLogs) Debug.Log("[Live2DPanel] Suppressing dialogue end cleanup during intro transition");
            return;
        }
        
        // Play default motion when dialogue ends
        if (maiMotionController != null)
        {
            if (enableDebugLogs) Debug.Log("[Live2DPanel] Calling PlayDefaultMotion at dialogue end");
            maiMotionController.PlayDefaultMotion();
        }
        
        HideMaiLive2D();
    }
    
    /// <summary>
    /// Handle dialogue line displayed - play Live2D animation if this is a normal dialogue line
    /// Only active in InGame section (not CG section)
    /// Ignores CG dialogue lines (CGDialogueLine)
    /// </summary>
    private void HandleDialogueLineDisplayed(DialogueLine line)
    {
        // Only respond to dialogue events in InGame section
        if (UIPanelManager.Instance != null && UIPanelManager.Instance.GetCurrentSection() != GameSection.InGame)
        {
            if (enableDebugLogs) Debug.Log($"[Live2DPanel] Ignoring dialogue line - not in InGame section (current: {UIPanelManager.Instance.GetCurrentSection()})");
            return;
        }
        
        // Ignore CG dialogue lines - they don't use Live2D animations
        if (line is CGDialogueLine)
        {
            if (enableDebugLogs) Debug.Log("[Live2DPanel] Ignoring CGDialogueLine - no Live2D animation");
            return;
        }
        
        // Play Live2D animation for normal dialogue lines
        if (maiMotionController != null && line.animationToPlay != AnimationType.None)
        {
            string motionName = line.animationToPlay.ToString();
            if (enableDebugLogs) Debug.Log($"[Live2DPanel] Playing motion: '{motionName}', loop={line.loopAnimation}, floatValue={line.animationFloatValue}");
            maiMotionController.PlayMotion(motionName, line.loopAnimation, line.animationFloatValue);
        }
    }
    
    #endregion
    
    #region Staff Management
    
    /// <summary>
    /// Handle game start event - show/hide staff based on initial area
    /// </summary>
    private void HandleGameStart(GameStartEvent args)
    {
        UpdateStaffVisibility(args.Area);
        UpdateMaiVisibilityForArea(args.Area);
    }
    
    /// <summary>
    /// Handle place changed event - show/hide staff based on area
    /// </summary>
    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        UpdateStaffVisibility(args.Area);
        UpdateMaiVisibilityForArea(args.Area);
    }
    
    /// <summary>
    /// Update Live2D Staff visibility based on current area
    /// Staff is shown in HiepMart, hidden in other areas
    /// </summary>
    private void UpdateStaffVisibility(Area area)
    {
        if (staffMotionController == null) return;
        
        bool shouldShow = area == Area.HiepMart;
        
        if (shouldShow && !staffMotionController.gameObject.activeSelf)
        {
            staffMotionController.gameObject.SetActive(true);
        }
        else if (!shouldShow && staffMotionController.gameObject.activeSelf)
        {
            staffMotionController.gameObject.SetActive(false);
        }
    }

    private void UpdateMaiVisibilityForArea(Area area)
    {
        if (area == Area.Home)
            return;

        if (maiMotionController != null && maiMotionController.gameObject.activeSelf)
        {
            if (enableDebugLogs) Debug.Log($"[Live2DPanel] Hiding Mai Live2D for area change to {area}");
            maiMotionController.PlayDefaultMotion();
        }

        HideMaiLive2D();
    }
    
    #endregion
    
    #region Simulation Live2D Management
    
    /// <summary>
    /// Show the simulation Live2D model for the given position.
    /// Called by SimulationNavigationPanel when entering Simulation section
    /// </summary>
    public void ShowSimulationLive2D(SexPositionConfigType positionType = SexPositionConfigType.Missionary)
    {
        Live2DMotionController controller = GetSimulationMotionController(positionType);
        if (controller != null)
        {
            HideAllSimulationLive2DExcept(controller);
            controller.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError($"[Live2DPanel] ShowSimulationLive2D - motion controller for {positionType} is NULL!");
        }
    }

    /// <summary>
    /// Hide the simulation Live2D model for the given position and reset all animations
    /// Called by SimulationNavigationPanel when leaving Simulation section
    /// </summary>
    public void HideSimulationLive2D(SexPositionConfigType positionType = SexPositionConfigType.Missionary)
    {
        Live2DMotionController controller = GetSimulationMotionController(positionType);
        if (controller != null)
        {
            controller.ResetAllBoolParameters();
            controller.gameObject.SetActive(false);
        }
    }

    private void HideAllSimulationLive2DExcept(Live2DMotionController selected)
    {
        Live2DMotionController[] controllers =
        {
            missionaryMotionController,
            cowgirlMotionController,
            roleplayPussyMotionController,
            roleplayButtholeMotionController,
            roleplayBlowjobMotionController,
            roleplayPaizuriMotionController
        };

        foreach (Live2DMotionController controller in controllers)
        {
            if (controller != null && controller != selected)
            {
                controller.ResetAllBoolParameters();
                controller.gameObject.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region Mai Live2D Show/Hide
    
    /// <summary>
    /// Show Live2D Mai model
    /// </summary>
    private void ShowMaiLive2D()
    {
        if (maiMotionController != null)
        {
            maiMotionController.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Hide Live2D Mai model
    /// </summary>
    private void HideMaiLive2D()
    {
        if (maiMotionController != null)
        {
            maiMotionController.gameObject.SetActive(false);
        }
    }
    
    #endregion
}

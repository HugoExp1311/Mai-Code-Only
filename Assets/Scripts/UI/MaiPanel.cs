using Base;
using Base.Character;
using Base.Settings;
using Events;
using EventBus;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Mai panel that automatically shows/hides based on area and time
/// Inherits from UIPanel to integrate with the panel management system
/// 
/// IMPORTANT CONFIGURATION (Unity Inspector):
/// - isSectionIndependent MUST be set to true (so it can appear in any section)
/// - behaviorType should be Overlay or Popup (so it can be shown/hidden)
/// - Panel GameObject should be DISABLED initially in scene (prevents state desynchronization)
/// - Awake() runs even for disabled GameObjects, so event subscriptions will work
/// - ShowPanel() will enable the GameObject when conditions are met
/// 
/// NOTE: Event subscription happens in Awake() to persist even when panel is disabled.
/// This ensures MaiPanel always receives events, regardless of its enabled state.
/// Unity calls Awake() for disabled GameObjects in the scene, so the panel can be disabled initially.
/// </summary>
public class MaiPanel : UIPanel
{
    [Header("Mai Interaction Buttons")]
    [SerializeField] private UIPanelButtonAction maiSpriteButtonAction;
    [SerializeField] private UIPanelButtonAction dialogueButtonAction;
    [SerializeField] private UIPanelButtonAction giftButtonAction;
    
    // Cached Button components from UIPanelButtonAction GameObjects
    private Button maiSpriteButton;
    private Button dialogueButton;
    private Button giftButton;
    
    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<TimeChangedEvent> timeChangedBinding;
    
    private bool isListeningForOutsideClicks = false;
    private bool maiWasVisibleBeforeDialogue = false;
    
    private void Awake()
    {
        // UIPanel.Awake() is private, so Unity will call it automatically
        // We just need to initialize our own components here
        
        // Subscribe to events in Awake() so subscriptions persist even when panel is disabled
        // This ensures MaiPanel always receives events, regardless of its enabled state
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        timeChangedBinding = new EventBinding<TimeChangedEvent>(HandleTimeChanged);
        
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<TimeChangedEvent>.Register(timeChangedBinding);
        
        // Get Button components from UIPanelButtonAction GameObjects
        if (maiSpriteButtonAction != null)
        {
            maiSpriteButton = maiSpriteButtonAction.GetComponent<Button>();
            if (maiSpriteButton != null)
            {
                maiSpriteButton.onClick.AddListener(OnMaiSpriteClick);
            }
            else
            {
                Debug.LogWarning("[MaiPanel] Button component not found on maiSpriteButtonAction GameObject.");
            }
        }
        
        if (dialogueButtonAction != null)
        {
            dialogueButton = dialogueButtonAction.GetComponent<Button>();
            if (dialogueButton != null)
            {
                dialogueButton.onClick.AddListener(OnDialogueButtonClick);
            }
            else
            {
                Debug.LogWarning("[MaiPanel] Button component not found on dialogueButtonAction GameObject.");
            }
        }
        
        if (giftButtonAction != null)
        {
            giftButton = giftButtonAction.GetComponent<Button>();
            if (giftButton != null)
            {
                giftButton.onClick.AddListener(OnGiftButtonClick);
            }
            else
            {
                Debug.LogWarning("[MaiPanel] Button component not found on giftButtonAction GameObject.");
            }
        }
    }
    
    private void Start()
    {
        // Ensure initial state: if GameObject is active but IsVisible is false, disable it
        // This prevents UIPanel.Update() and PanelWatchdog from interfering
        // The panel should only be active when properly shown through UIPanelManager
        if (gameObject.activeSelf && !IsVisible)
        {
            gameObject.SetActive(false);
        }
        
        // Subscribe to dialogue events in Start() - ensures DialogueManager.Instance is available
        // (Unity guarantees all Awake() methods complete before any Start() method runs)
        // Subscriptions to instance methods persist even when GameObject is disabled
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart += HandleDialogueStart;
            DialogueManager.Instance.OnDialogueEnd += HandleDialogueEnd;
        }
        else
        {
            Debug.LogError("[MaiPanel] DialogueManager.Instance is still NULL in Start()! This should not happen. Subscriptions failed.");
        }
    }
    
    private void OnDestroy()
    {
        // Ensure bindings are deregistered when GameObject is destroyed
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
        
        if (timeChangedBinding != null)
        {
            EventBus<TimeChangedEvent>.Deregister(timeChangedBinding);
            timeChangedBinding = null;
        }
        
        // Remove button click listeners
        if (maiSpriteButton != null)
        {
            maiSpriteButton.onClick.RemoveListener(OnMaiSpriteClick);
        }
        
        if (dialogueButton != null)
        {
            dialogueButton.onClick.RemoveListener(OnDialogueButtonClick);
        }
        
        if (giftButton != null)
        {
            giftButton.onClick.RemoveListener(OnGiftButtonClick);
        }
        
        // Unsubscribe from dialogue events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart -= HandleDialogueStart;
            DialogueManager.Instance.OnDialogueEnd -= HandleDialogueEnd;
        }
    }
    
    private void OnEnable()
    {
        isListeningForOutsideClicks = true;
    }
    
    private void OnDisable()
    {
        isListeningForOutsideClicks = false;
        // Disable interaction buttons when panel is disabled
        DisableInteractionButtons();
    }
    
    private void Update()
    {
        // Check for clicks outside MaiPanel when listening is enabled
        if (isListeningForOutsideClicks && IsVisible)
        {
            // Check for mouse click
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                CheckForOutsideClick();
            }
            
            // Check for touch input
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                CheckForOutsideClick();
            }
        }
    }
    
    /// <summary>
    /// Handle game start event - update Mai visibility
    /// </summary>
    private void HandleGameStart(GameStartEvent args)
    {
        UpdateVisibility(args.Area, args.Time);
    }
    
    /// <summary>
    /// Handle place changed event - update Mai visibility
    /// </summary>
    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        UpdateVisibility(args.Area, args.Time);
    }
    
    /// <summary>
    /// Handle time changed event - update Mai visibility
    /// </summary>
    private void HandleTimeChanged(TimeChangedEvent args)
    {
        UpdateVisibility(args.Area, args.Time);
    }
    
    /// <summary>
    /// Update panel visibility based on area and time conditions
    /// </summary>
    private void UpdateVisibility(Area area, DateTime time)
    {
        // Don't update visibility during dialogue - let dialogue system manage it
        if (IsDialogueActive())
            return;
        
        // Mai should only be visible at Home and according to the time schedule
        bool shouldShow = (area == Area.Home) && TimeManager.IsMaiAtHome(time);
        
        if (shouldShow)
        {
            // Only show if not already visible
            if (!IsVisible)
            {
                // Use UIPanelManager to show the panel (ensures proper registration and management)
                if (UIPanelManager.Instance != null)
                {
                    // Check if panel exists in UIPanelManager before calling ShowPanel
                    var panel = UIPanelManager.Instance.GetPanel(PanelId);
                    if (panel != null)
                    {
                        UIPanelManager.Instance.ShowPanel(PanelId);
                    }
                    else
                    {
                        // Panel not registered yet - show directly and it will register when Awake runs
                        // This handles the case where events fire before registration completes
                        Show();
                    }
                }
                else
                {
                    // Fallback: show directly if UIPanelManager is not available
                    Show();
                }
            }
        }
        else
        {
            // Only hide if currently visible and GameObject is active
            if (IsVisible && gameObject.activeSelf)
            {
                // Hide the panel
                if (UIPanelManager.Instance != null)
                {
                    var panel = UIPanelManager.Instance.GetPanel(PanelId);
                    if (panel != null)
                    {
                        UIPanelManager.Instance.HidePanel(PanelId);
                    }
                    else
                    {
                        // Panel not registered yet - hide directly
                        Hide();
                    }
                }
                else
                {
                    // Fallback: hide directly if UIPanelManager is not available
                    Hide();
                }
            }
        }
    }
    
    /// <summary>
    /// Check if dialogue is currently active
    /// </summary>
    private bool IsDialogueActive()
    {
        return DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
    }
    
    /// <summary>
    /// Handle dialogue start event - hide MaiPanel properly through UIPanelManager
    /// </summary>
    private void HandleDialogueStart()
    {
        // Store whether MaiPanel was visible before dialogue started
        maiWasVisibleBeforeDialogue = IsVisible;
        
        // Always hide MaiPanel when dialogue starts (regardless of visibility state)
        // This ensures MaiPanel is hidden during dialogue
        if (IsVisible)
        {
            if (UIPanelManager.Instance != null)
            {
                UIPanelManager.Instance.HidePanel(PanelId);
            }
            else
            {
                // Fallback: hide directly if UIPanelManager is not available
                Debug.LogWarning("[MaiPanel] UIPanelManager.Instance is null, hiding directly");
                Hide();
            }
        }
    }
    
    /// <summary>
    /// Handle dialogue end event - always check current area/time conditions and update visibility
    /// </summary>
    private void HandleDialogueEnd()
    {
        if (GameManager.Instance != null && GameManager.Instance.ShouldSuppressInGameUIForIntroTransition())
        {
            maiWasVisibleBeforeDialogue = false;
            return;
        }

        // Always check current conditions and update visibility based on area and time
        // This ensures MaiPanel shows/hides correctly regardless of whether it was visible before dialogue
        if (GameManager.Instance != null)
        {
            Area currentArea = GameManager.Instance.Area;
            DateTime currentTime = GameManager.Instance.Time;
            UpdateVisibility(currentArea, currentTime);
        }
        else
        {
            Debug.LogWarning("[MaiPanel] GameManager.Instance is null, cannot update visibility");
        }
        
        // Reset the flag for cleanup
        maiWasVisibleBeforeDialogue = false;
    }
    
    /// <summary>
    /// Called when Mai's Sprite button is clicked - enables Dialogue and Gift buttons
    /// </summary>
    private void OnMaiSpriteClick()
    {
        EnableInteractionButtons();
    }
    
    /// <summary>
    /// Called when Dialogue button is clicked - triggers dialogue and disables buttons
    /// </summary>
    private void OnDialogueButtonClick()
    {
        // Check if dialogue is already active
        if (IsDialogueActive())
        {
            return;
        }
        
        // Trigger dialogue
        if (CharacterInteractManager.Instance != null)
        {
            CharacterInteractManager.Instance.InitiateTalk();
        }
        else
        {
            Debug.LogWarning("[MaiPanel] CharacterInteractManager.Instance is null. Cannot initiate dialogue.");
        }
        
        // Disable interaction buttons
        DisableInteractionButtons();
    }
    
    /// <summary>
    /// Called when Gift button is clicked - triggers gift interaction (template) and disables buttons
    /// </summary>
    private void OnGiftButtonClick()
    {
        // Trigger gift interaction (template/placeholder)
        if (CharacterInteractManager.Instance != null)
        {
            CharacterInteractManager.Instance.InitiateGift();
        }
        else
        {
            Debug.LogWarning("[MaiPanel] CharacterInteractManager.Instance is null. Cannot initiate gift.");
        }
        
        // Disable interaction buttons
        DisableInteractionButtons();
    }
    
    /// <summary>
    /// Enable Dialogue and Gift button GameObjects
    /// </summary>
    private void EnableInteractionButtons()
    {
        if (dialogueButtonAction != null)
        {
            dialogueButtonAction.gameObject.SetActive(true);
        }
        
        if (giftButtonAction != null)
        {
            giftButtonAction.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Disable Dialogue and Gift button GameObjects
    /// </summary>
    private void DisableInteractionButtons()
    {
        if (dialogueButtonAction != null)
        {
            dialogueButtonAction.gameObject.SetActive(false);
        }
        
        if (giftButtonAction != null)
        {
            giftButtonAction.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Check if a clicked object is within MaiPanel's hierarchy
    /// </summary>
    private bool IsClickWithinMaiPanel(GameObject clickedObject)
    {
        if (clickedObject == null)
            return false;
        
        // Traverse up the hierarchy to see if this object is a child of MaiPanel
        Transform current = clickedObject.transform;
        while (current != null)
        {
            if (current == transform)
            {
                return true;
            }
            current = current.parent;
        }
        
        return false;
    }
    
    // Cached to avoid per-click allocations
    private PointerEventData _cachedPointerData;
    private readonly System.Collections.Generic.List<RaycastResult> _cachedRaycastResults = new System.Collections.Generic.List<RaycastResult>();
    
    /// <summary>
    /// Check for clicks outside MaiPanel and disable interaction buttons if detected
    /// </summary>
    private void CheckForOutsideClick()
    {
        // Check if pointer is over a UI element
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Reuse cached PointerEventData
            if (_cachedPointerData == null)
            {
                _cachedPointerData = new PointerEventData(EventSystem.current);
            }
            _cachedPointerData.position = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            
            _cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(_cachedPointerData, _cachedRaycastResults);
            
            // Check if any hit object is within MaiPanel
            bool clickWithinPanel = false;
            foreach (var result in _cachedRaycastResults)
            {
                if (IsClickWithinMaiPanel(result.gameObject))
                {
                    clickWithinPanel = true;
                    break;
                }
            }
            
            // If click is outside MaiPanel, disable interaction buttons
            if (!clickWithinPanel)
            {
                DisableInteractionButtons();
            }
        }
        else
        {
            // Click is not over any UI element (clicked on world/background)
            DisableInteractionButtons();
        }
    }
    
    /// <summary>
    /// Override OnShow to initialize button states when panel is shown
    /// IMPORTANT: Disable buttons FIRST before calling base.OnShow() to prevent pre-enabling
    /// </summary>
    protected override void OnShow(object data)
    {
        // CRITICAL: Disable interaction buttons FIRST before any other logic runs
        // This prevents buttons from being pre-enabled when MaiPanel shows up
        DisableInteractionButtons();
        
        base.OnShow(data);
    }
}


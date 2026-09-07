using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Base;

/// <summary>
/// Simple panel component with direct configuration fields
/// Supports scene object references and automatic sorting order management
/// </summary>
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [Header("Panel Identity")]
    [SerializeField] private string panelId = "NewPanel";
    
    [Header("Display Settings")]
    [SerializeField] private string panelTitle;
    [SerializeField] private Sprite backgroundImage;
    [SerializeField] private Color backgroundColor = Color.white;
    
    [Header("Behavior")]
    [SerializeField] private PanelBehaviorType behaviorType = PanelBehaviorType.Popup;
    [SerializeField] private bool closeOnBackgroundClick = true;
    [SerializeField] private bool disableUnderlyingPanels = true;
    
    [Header("Background Click Sound")]
    [Tooltip("Sound to play when background is clicked (to close panel)")]
    [SerializeField] private SoundType backgroundClickSound = SoundType.Back;
    
    [Header("Animation")]
    [SerializeField] private bool animateShow = true;
    [SerializeField] private bool animateHide = true;
    [SerializeField] private float animationDuration = 0.15f;
    
    [Header("Section Assignment")]
    [Tooltip("Which game section this panel belongs to")]
    [SerializeField] private GameSection gameSection = GameSection.MainMenu;
    [Tooltip("Panel is independent of sections (e.g., Live2D panels)")]
    [SerializeField] private bool isSectionIndependent = false;
    
    [Header("Events")]
    [Tooltip("Invoked when this panel is shown")]
    public UnityEvent OnShown = new UnityEvent();
    [Tooltip("Invoked when this panel is hidden")]
    public UnityEvent OnHidden = new UnityEvent();
    [Tooltip("Invoked when this panel receives data from another panel")]
    public UnityEvent<object> OnDataReceived = new UnityEvent<object>();
    
    [Header("Global Events")]
    [Tooltip("Invoked when any panel is opened (includes panel ID)")]
    public UnityEvent<string> OnAnyPanelOpened;
    [Tooltip("Invoked when any panel is closed (includes panel ID)")]
    public UnityEvent<string> OnAnyPanelClosed;
    
    // References (assigned in Inspector via RequireComponent)
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image background;
    private GameObject clickBlocker;
    
    // State
    private bool isVisible;
    private Coroutine currentAnimation;
    private object panelData;
    private RuntimePanelConfig? runtimeConfigOverride; // Nullable for optional override
    
    // Component management
    private Dictionary<Component, bool> defaultComponentStates = new Dictionary<Component, bool>();
    private Dictionary<string, Dictionary<Component, bool>> scenarioConfigurations = new Dictionary<string, Dictionary<Component, bool>>();
    
    // Properties
    public string PanelId => panelId;
    public bool IsVisible => isVisible;
    public PanelBehaviorType BehaviorType => GetActiveConfig().behaviorType;
    public object PanelData => panelData;
    public Canvas Canvas => canvas;
    public GameSection GameSection => gameSection;
    public bool IsSectionIndependent => isSectionIndependent;
    
    /// <summary>
    /// Check if this panel is a base panel (derived from BehaviorType)
    /// </summary>
    public bool IsBasePanel => BehaviorType == PanelBehaviorType.Base && !isSectionIndependent;
    
    #if UNITY_EDITOR
    /// <summary>
    /// Validate configuration in Editor
    /// </summary>
    private void OnValidate()
    {
        // Validate section configuration
        if (isSectionIndependent && gameSection != GameSection.None)
        {
            gameSection = GameSection.None;
            Debug.LogWarning($"[UIPanel] {gameObject.name}: Panel is section-independent but has section assigned. Setting to None.");
        }

        // Warn if Base behavior type is used but panel is section-independent
        if (isSectionIndependent && behaviorType == PanelBehaviorType.Base)
        {
            Debug.LogWarning($"[UIPanel] {gameObject.name}: Base behavior type should not be used with section-independent panels. Consider using Overlay or Popup instead.");
        }
        
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (animationDuration != 0.15f)
        {
            animationDuration = 0.15f;
        }
    }
    #endif
    
    private void Awake()
    {
        // Components are assigned via serialized fields in Inspector
        // RequireComponent ensures they exist, but we still need to validate
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"[UIPanel] {gameObject.name}: Canvas component not found or not assigned in Inspector!");
            }
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        if (background == null)
        {
            background = GetComponent<Image>();
        }
        
        // Store default component states (for component management API)
        StoreDefaultComponentStates();
        
        ApplyConfig();
        
        // Validate section configuration
        ValidateSectionConfiguration();
        
        // Register this panel with UIPanelManager (registration pattern)
        // Use StartCoroutine to delay registration until UIPanelManager.Instance is available
        StartCoroutine(RegisterPanelDelayed());
    }
    
    private System.Collections.IEnumerator RegisterPanelDelayed()
    {
        // Wait until UIPanelManager.Instance is available
        while (UIPanelManager.Instance == null)
        {
            yield return null;
        }
        
        // Register this panel
        UIPanelManager.Instance.Register(this);
    }
    
    private void Update()
    {
        // Continuous check: if panel is active but not visible and not a base panel, disable it
        // This catches panels enabled by external scripts or Unity's auto-enable behavior
        // Skip section-independent panels - they manage their own visibility
        if (gameObject.activeSelf && !isVisible && BehaviorType != PanelBehaviorType.Base && !isSectionIndependent)
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Validate section configuration
    /// </summary>
    private void ValidateSectionConfiguration()
    {
        if (isSectionIndependent && gameSection != GameSection.None)
        {
            Debug.LogWarning($"[UIPanel] {gameObject.name}: Panel is section-independent but has section {gameSection}. Setting to None.");
            gameSection = GameSection.None;
        }
        
        // Warn if Base behavior type is used but panel is section-independent
        if (isSectionIndependent && behaviorType == PanelBehaviorType.Base)
        {
            Debug.LogWarning($"[UIPanel] {gameObject.name}: Base behavior type should not be used with section-independent panels.");
        }
    }
    
    /// <summary>
    /// Get the active configuration (runtime override or base)
    /// </summary>
    private RuntimePanelConfig GetActiveConfig()
    {
        if (runtimeConfigOverride.HasValue)
        {
            return runtimeConfigOverride.Value;
        }
        
        // Return base config from component fields
        return new RuntimePanelConfig
        {
            panelTitle = panelTitle,
            backgroundImage = backgroundImage,
            backgroundColor = backgroundColor,
            behaviorType = behaviorType,
            closeOnBackgroundClick = closeOnBackgroundClick,
            disableUnderlyingPanels = disableUnderlyingPanels,
            animateShow = animateShow,
            animateHide = animateHide,
            animationDuration = animationDuration
        };
    }
    
    /// <summary>
    /// Apply configuration to panel
    /// </summary>
    public void ApplyConfig()
    {
        var cfg = GetActiveConfig();
        
        // Canvas setup - ensure canvas is initialized
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError($"[UIPanel] Canvas component not found on {gameObject.name}. Cannot apply config.");
                return;
            }
        }
        canvas.overrideSorting = true;
        
        // Background
        if (background != null)
        {
            if (cfg.backgroundImage != null)
            {
                background.sprite = cfg.backgroundImage;
            }
            background.color = cfg.backgroundColor;
        }
        
        // Click blocker
        // Base panels should never have background click blockers (they're always enabled and not closeable)
        if (BehaviorType == PanelBehaviorType.Base)
        {
            // Destroy any existing click blocker for Base panels
            if (clickBlocker != null)
            {
                Destroy(clickBlocker);
                clickBlocker = null;
            }
        }
        else if (cfg.closeOnBackgroundClick)
        {
            SetupClickBlocker();
        }
        else if (clickBlocker != null)
        {
            Destroy(clickBlocker);
            clickBlocker = null;
        }
    }
    
    /// <summary>
    /// Apply runtime config override (for scenario-based changes)
    /// </summary>
    public void ApplyRuntimeConfig(RuntimePanelConfig config)
    {
        runtimeConfigOverride = config;
        ApplyConfig();
    }
    
    /// <summary>
    /// Clear runtime config override (revert to base config)
    /// </summary>
    public void ClearRuntimeConfig()
    {
        runtimeConfigOverride = null;
        ApplyConfig();
    }
    
    /// <summary>
    /// Set background image directly (for dynamic updates)
    /// </summary>
    public void SetBackgroundImage(Sprite sprite, Color color)
    {
        if (background != null)
        {
            background.sprite = sprite;
            background.color = color;
        }
        
        // Also update the serialized field for consistency
        backgroundImage = sprite;
        backgroundColor = color;
    }
    
    /// <summary>
    /// Show panel with optional data
    /// </summary>
    public virtual void Show(object data = null)
    {
        // Base panels should only be shown through EnableSection(), not individually
        // This prevents Base panels from being shown directly and ensures section management works correctly
        if (BehaviorType == PanelBehaviorType.Base && !isSectionIndependent)
        {
            // Check if this is being called from UIPanelManager.EnableSection()
            // using a lightweight flag instead of expensive StackTrace allocation
            if (!UIPanelManager.IsEnablingSectionInProgress)
            {
                Debug.LogWarning($"[UIPanel] EARLY RETURN - Attempted to show Base panel '{PanelId}' individually. Base panels should only be shown through EnableSection(). Use UIPanelManager.SwitchToSection() instead.");
                return;
            }
        }
        
        panelData = data;
        // Set isVisible BEFORE SetActive to prevent OnEnable() from blocking legitimate Show() calls
        isVisible = true;
        gameObject.SetActive(true);
        
        // Apply config to ensure everything is set up
        ApplyConfig();
        
        var cfg = GetActiveConfig();
        
        // Animation (skip for Base type panels - they are persistent screens)
        if (cfg.animateShow && BehaviorType != PanelBehaviorType.Base)
        {

            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateShow());
        }
        else if (BehaviorType == PanelBehaviorType.Base)
        {
            // Base panels should always be fully visible immediately
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
        
        // Call virtual hook for custom behavior
        OnShow(data);
        
        // Events
        OnShown?.Invoke();
        OnDataReceived?.Invoke(data);
        
        // Global events
        if (OnAnyPanelOpened != null && OnAnyPanelOpened.GetPersistentEventCount() > 0)
        {
            OnAnyPanelOpened.Invoke(PanelId);
        }
        
        // Notify manager
        UIPanelManager.Instance?.NotifyPanelShown(this);
    }

    /// <summary>
    /// Show the panel immediately without fade animation.
    /// Used by subclasses for chained dialogue continuations.
    /// </summary>
    protected void ShowImmediate()
    {
        // Cancel any running animation (e.g., AnimateHide from a prior EndDialogue)
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        isVisible = true;
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        ApplyConfig();
        OnShow(null);
    }
    
    /// <summary>
    /// Override this method to add custom behavior when panel is shown
    /// </summary>
    protected virtual void OnShow(object data)
    {
        // Override in derived classes for custom show logic
    }
    
    /// <summary>
    /// Hide panel
    /// </summary>
    public virtual void Hide()
    {
        // Always set isVisible to false immediately for state management
        isVisible = false;
        
        // Call virtual hook for custom behavior
        OnHide();
        
        var cfg = GetActiveConfig();
        
        // Animation (skip for Base type panels - they should hide immediately)
        // Also skip animation if GameObject is already inactive (can't start coroutines on inactive objects)
        if (cfg.animateHide && BehaviorType != PanelBehaviorType.Base && gameObject.activeInHierarchy)
        {
            // For animated panels, keep GameObject active during animation, then deactivate
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateHide());
        }
        else
        {
            // Base panels, non-animated panels, or already inactive: deactivate immediately
            gameObject.SetActive(false);
        }
        
        // Events
        OnHidden?.Invoke();
        
        // Global events
        if (OnAnyPanelClosed != null && OnAnyPanelClosed.GetPersistentEventCount() > 0)
        {
            OnAnyPanelClosed.Invoke(PanelId);
        }
        
        // Notify manager
        UIPanelManager.Instance?.NotifyPanelHidden(this);
    }
    
    /// <summary>
    /// Override this method to add custom behavior when panel is hidden
    /// </summary>
    protected virtual void OnHide()
    {
        // Override in derived classes for custom hide logic
    }
    
    /// <summary>
    /// Force the panel into hidden state without events or animations.
    /// Used by UIPanelManager for section cleanup — replaces reflection-based isVisible access.
    /// </summary>
    public void ForceHidden()
    {
        isVisible = false;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Check if this panel is a tab (has TabComponent or parent has TabManager)
    /// </summary>
    private bool IsTab()
    {
        // Check if panel has TabComponent
        if (GetComponent<TabComponent>() != null)
        {
            return true;
        }
        
        // Check if parent has TabManager
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.GetComponent<TabManager>() != null)
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }
    
    /// <summary>
    /// Send data to another panel
    /// </summary>
    public void SendDataToPanel(string targetPanelId, object data)
    {
        var targetPanel = UIPanelManager.Instance?.GetPanel(targetPanelId);
        if (targetPanel != null)
        {
            targetPanel.ReceiveData(data);
        }
    }
    
    /// <summary>
    /// Receive data from another panel
    /// </summary>
    public virtual void ReceiveData(object data)
    {
        panelData = data;
        
        // Call virtual hook for custom data handling
        OnReceiveData(data);
        
        // Fire event
        OnDataReceived?.Invoke(data);
    }
    
    /// <summary>
    /// Override this method to add custom behavior when panel receives data
    /// </summary>
    protected virtual void OnReceiveData(object data)
    {
        // Override in derived classes for custom data handling
    }
    
    private IEnumerator AnimateShow()
    {
        // CanvasGroup should be assigned via serialized field in Inspector
        if (canvasGroup == null)
        {
            Debug.LogError($"[UIPanel] CanvasGroup not assigned in Inspector on {gameObject.name}. Skipping animation.");
            yield break;
        }
        
        canvasGroup.alpha = 0;
        float elapsed = 0;
        var cfg = GetActiveConfig();
        
        while (elapsed < cfg.animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / cfg.animationDuration;
            yield return null;
        }
        
        canvasGroup.alpha = 1;
    }
    
    private IEnumerator AnimateHide()
    {
        // Ensure canvasGroup is initialized before use
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        // If canvasGroup is still null, skip animation and hide panel directly
        if (canvasGroup == null)
        {
            // Deactivate GameObject directly since animation is skipped
            gameObject.SetActive(false);
            yield break;
        }
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0;
        var cfg = GetActiveConfig();
        
        while (elapsed < cfg.animationDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, elapsed / cfg.animationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0;
        // Deactivate GameObject after animation completes
        // Hide() already set isVisible = false, so this just ensures GameObject state matches
        gameObject.SetActive(false);
    }
    
    private void SetupClickBlocker()
    {
        if (clickBlocker != null) return; // Already set up
        
        clickBlocker = new GameObject("BackgroundClickBlocker");
        clickBlocker.transform.SetParent(transform, false);
        clickBlocker.transform.SetAsFirstSibling();
        
        var rect = clickBlocker.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        var img = clickBlocker.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.01f);
        img.raycastTarget = true;
        
        // Use EventTrigger with PointerClick instead of Button to avoid cursor changes
        // EventTrigger doesn't trigger CursorManager's button detection
        var eventTrigger = clickBlocker.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
        clickEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
        clickEntry.callback.AddListener((data) => OnBackgroundClicked());
        eventTrigger.triggers.Add(clickEntry);
        
        clickBlocker.SetActive(true);
    }
    
    /// <summary>
    /// Called when background is clicked - plays sound then hides panel
    /// </summary>
    private void OnBackgroundClicked()
    {
        PlayBackgroundClickSound();
        Hide();
    }
    
    /// <summary>
    /// Play sound for background click
    /// </summary>
    private void PlayBackgroundClickSound()
    {
        if (backgroundClickSound == SoundType.None)
            return;
        
        if (AudioManager.Instance == null || FMODEvents.Instance == null)
            return;
        
        switch (backgroundClickSound)
        {
            case SoundType.Normal:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnButton);
                break;
                
            case SoundType.Back:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnBack);
                break;
                
            case SoundType.Action:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnAction);
                break;
                
            // Note: Place sounds not typically used for background clicks
            // but included for consistency
            case SoundType.Place:
                // Could implement place-specific logic here if needed
                // For now, default to back sound
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnBack);
                break;
        }
    }
    
    #region Component Management API
    
    /// <summary>
    /// Store default component states on Awake
    /// </summary>
    private void StoreDefaultComponentStates()
    {
        Component[] allComponents = GetComponentsInChildren<Component>(true);
        foreach (var comp in allComponents)
        {
            // Skip structural components
            if (comp is Transform || comp is RectTransform || 
                comp is Canvas || comp is CanvasGroup || comp is UIPanel)
                continue;
            
            // Store enabled state
            if (comp is MonoBehaviour mono)
            {
                defaultComponentStates[comp] = mono.enabled;
            }
            else
            {
                // For other components, check if their GameObject is active
                defaultComponentStates[comp] = comp != null && comp.gameObject != null && comp.gameObject.activeSelf;
            }
        }
    }
    
    /// <summary>
    /// Enable/disable components of a specific type within this panel
    /// </summary>
    public void SetComponentEnabled<T>(bool enabled) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (var comp in components)
        {
            SetComponentActive(comp, enabled);
        }
    }
    
    /// <summary>
    /// Enable/disable component by name
    /// </summary>
    public void SetComponentEnabled(string componentName, bool enabled)
    {
        Component[] allComponents = GetComponentsInChildren<Component>(true);
        foreach (var comp in allComponents)
        {
            if (comp.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
            {
                SetComponentActive(comp, enabled);
            }
        }
    }
    
    /// <summary>
    /// Enable/disable all components (excluding specified types)
    /// </summary>
    public void SetAllComponentsEnabled(bool enabled, params Type[] excludeTypes)
    {
        Component[] allComponents = GetComponentsInChildren<Component>(true);
        foreach (var comp in allComponents)
        {
            // Skip structural components
            if (comp is Transform || comp is RectTransform || 
                comp is Canvas || comp is CanvasGroup || comp is UIPanel)
                continue;
            
            // Skip excluded types
            bool shouldExclude = false;
            foreach (var excludeType in excludeTypes)
            {
                if (excludeType.IsAssignableFrom(comp.GetType()))
                {
                    shouldExclude = true;
                    break;
                }
            }
            
            if (!shouldExclude)
            {
                SetComponentActive(comp, enabled);
            }
        }
    }
    
    /// <summary>
    /// Check if a component type is enabled
    /// </summary>
    public bool IsComponentEnabled<T>() where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        if (components.Length == 0) return false;
        
        // Return true if any component of this type is enabled
        foreach (var comp in components)
        {
            if (IsComponentActive(comp))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Configure panel for a specific scenario (from saved presets)
    /// </summary>
    public void ConfigureForScenario(string scenarioId)
    {
        if (scenarioConfigurations.TryGetValue(scenarioId, out var config))
        {
            foreach (var kvp in config)
            {
                SetComponentActive(kvp.Key, kvp.Value);
            }
        }
        else
        {
            Debug.LogWarning($"[UIPanel] Scenario '{scenarioId}' not found for panel '{PanelId}'");
        }
    }
    
    /// <summary>
    /// Reset panel to default component configuration
    /// </summary>
    public void ResetToDefaultConfiguration()
    {
        foreach (var kvp in defaultComponentStates)
        {
            SetComponentActive(kvp.Key, kvp.Value);
        }
    }
    
    /// <summary>
    /// Save current component states as a scenario preset (Editor only)
    /// </summary>
    public void SaveScenarioPreset(string scenarioId)
    {
        if (string.IsNullOrEmpty(scenarioId))
        {
            Debug.LogWarning("[UIPanel] Scenario ID cannot be empty");
            return;
        }
        
        Dictionary<Component, bool> currentStates = new Dictionary<Component, bool>();
        Component[] allComponents = GetComponentsInChildren<Component>(true);
        
        foreach (var comp in allComponents)
        {
            if (comp is Transform || comp is RectTransform || 
                comp is Canvas || comp is CanvasGroup || comp is UIPanel)
                continue;
            
            currentStates[comp] = IsComponentActive(comp);
        }
        
        scenarioConfigurations[scenarioId] = currentStates;
        Debug.Log($"[UIPanel] Saved scenario '{scenarioId}' for panel '{PanelId}'");
    }
    
    /// <summary>
    /// Set component active state (helper method)
    /// </summary>
    private void SetComponentActive(Component comp, bool active)
    {
        if (comp == null) return;
        
        if (comp is MonoBehaviour mono)
        {
            mono.enabled = active;
        }
        else if (comp.gameObject != null)
        {
            // For other component types, activate/deactivate their GameObject
            comp.gameObject.SetActive(active);
        }
    }
    
    /// <summary>
    /// Check if component is active (helper method)
    /// </summary>
    private bool IsComponentActive(Component comp)
    {
        if (comp == null) return false;
        
        if (comp is MonoBehaviour mono)
        {
            return mono.enabled;
        }
        
        // For other component types, check if their GameObject is active
        return comp.gameObject != null && comp.gameObject.activeSelf;
    }
    
    #endregion
}

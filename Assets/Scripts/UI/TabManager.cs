using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages tabs within a parent panel
/// Supports UIPanel sub-panels as tabs (and can be extended for GameObject tabs)
/// Handles tab switching and state management via flexible handler system
/// </summary>
[RequireComponent(typeof(UIPanel))]
public class TabManager : MonoBehaviour
{
    [Header("Tab Configuration")]
    [Tooltip("List of UIPanel sub-panels that act as tabs")]
    [SerializeField] private List<UIPanel> tabs = new List<UIPanel>();
    
    [Header("Tab Initialization")]
    [Tooltip("Tab ID to pre-enable when parent panel is shown. Leave empty to keep all tabs hidden initially.")]
    [SerializeField] private string preEnableTabId = "";
    
    [Header("Tab State Management")]
    [Tooltip("Type of state handler to use for tab button visual feedback")]
    [SerializeField] private TabStateHandlerType stateHandlerType = TabStateHandlerType.Animator;
    
    [Header("Animator Handler Settings")]
    [Tooltip("Animator Controller for tab button states (used with AnimatorTabStateHandler)")]
    [SerializeField] private Animator tabAnimator;
    
    [Tooltip("Tab IDs that correspond to animator bool parameters")]
    [SerializeField] private string[] animatorTabIds = new string[0];
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Internal state
    private Dictionary<string, UIPanel> tabLookup = new Dictionary<string, UIPanel>();
    private ITabStateHandler stateHandler;
    [SerializeField] private UIPanel parentPanel;
    private string currentActiveTabId = null;
    private bool isInitializing = false; // Flag to prevent external SwitchTab calls during initialization
    private string preEnabledTabId = null; // Track which tab was pre-enabled to ignore redundant switches
    private UnityEngine.Coroutine visibilityCheckCoroutine = null; // Coroutine for periodic visibility checks
    
    public enum TabStateHandlerType
    {
        None,       // No state management
        Animator    // Animator Controller-based (current implementation)
        // Future: Color, Image, etc.
    }
    
    private void Awake()
    {
        // Parent panel should be assigned via serialized field in Inspector
        // RequireComponent ensures it exists, but validate assignment
        if (parentPanel == null)
        {
            Debug.LogError($"[TabManager] {gameObject.name}: UIPanel component not assigned in Inspector!");
            return;
        }
        
        BuildTabLookup();
        InitializeStateHandler();
    }
    
    private void Start()
    {
        // Hide all tabs initially
        HideAllTabs();
        
        // Subscribe to parent panel's show/hide events to manage tabs
        if (parentPanel != null)
        {
            parentPanel.OnShown.AddListener(OnParentPanelShown);
            parentPanel.OnHidden.AddListener(OnParentPanelHidden);
            
            // If parent panel is already shown, initialize tabs now (with delay)
            if (parentPanel.IsVisible)
            {
                StartCoroutine(InitializeTabsDelayed());
            }
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (parentPanel != null)
        {
            if (parentPanel.OnShown != null)
            {
                parentPanel.OnShown.RemoveListener(OnParentPanelShown);
            }
            if (parentPanel.OnHidden != null)
            {
                parentPanel.OnHidden.RemoveListener(OnParentPanelHidden);
            }
        }
    }
    
    /// <summary>
    /// Called when parent panel is shown - re-initialize tabs
    /// </summary>
    private void OnParentPanelShown()
    {
        if (debugMode)
        {
            Log($"OnParentPanelShown called - parent panel visible: {parentPanel?.IsVisible}, active: {parentPanel?.gameObject.activeSelf}");
        }
        
        // Use coroutine to ensure panel is fully shown before initializing tabs
        StartCoroutine(InitializeTabsDelayed());
    }
    
    /// <summary>
    /// Coroutine to initialize tabs after a small delay to ensure panel is fully shown
    /// </summary>
    private System.Collections.IEnumerator InitializeTabsDelayed()
    {
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Starting coroutine");
        }
        
        // Set initialization flag to prevent external SwitchTab calls
        isInitializing = true;
        
        // Wait for end of frame to ensure panel is fully shown
        yield return new WaitForEndOfFrame();
        
        if (debugMode)
        {
            Log($"InitializeTabsDelayed: After WaitForEndOfFrame x2 - parent panel visible: {parentPanel?.IsVisible}, active: {parentPanel?.gameObject.activeSelf}");
        }
        
        // Rebuild tab lookup in case tabs were added/removed or changed
        BuildTabLookup();
        
        if (debugMode)
        {
            Log($"InitializeTabsDelayed: Tab lookup rebuilt - {tabLookup.Count} tabs, preEnableTabId: '{preEnableTabId}'");
        }
        
        // Now initialize tabs
        InitializeTabs();
        
        // Log tab state immediately after initialization
        if (debugMode)
        {
            LogTabState("Immediately after InitializeTabs()");
        }
        
        // Store the pre-enabled tab ID for protection
        preEnabledTabId = !string.IsNullOrEmpty(preEnableTabId) ? preEnableTabId : null;
        
        // Wait multiple frames to ensure everything is settled (Animator events, button clicks, etc.)
        // Animator state changes can trigger UnityEvents that cause button clicks
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Waiting for frames...");
        }
        yield return new WaitForEndOfFrame();
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Frame 1 complete");
        }
        yield return new WaitForEndOfFrame();
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Frame 2 complete");
        }
        yield return new WaitForEndOfFrame();
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Frame 3 complete");
        }
        
        // Wait a bit more to ensure any delayed events from Animator have settled
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Waiting 0.15s for Animator events to settle...");
        }
        yield return new WaitForSeconds(0.15f);
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Wait complete, checking for pending SwitchTab calls...");
            LogTabState("After 0.15s wait");
        }
        
        // Clear initialization flag - now external SwitchTab calls are allowed
        isInitializing = false;
        
        if (debugMode)
        {
            Log($"InitializeTabsDelayed: Initialization complete, external SwitchTab calls now allowed. Pre-enabled tab: '{preEnabledTabId}', Current active: '{currentActiveTabId}'");
            LogTabState("After initialization flag cleared");
        }
        
        // Clear pre-enabled tab ID after a short delay to allow normal switching
        if (debugMode)
        {
            Log("InitializeTabsDelayed: Waiting 0.1s before clearing pre-enabled tab protection...");
        }
        yield return new WaitForSeconds(0.1f);
        string previousPreEnabledTabId = preEnabledTabId;
        preEnabledTabId = null;
        
        if (debugMode)
        {
            Log($"InitializeTabsDelayed: Pre-enabled tab protection cleared (was: '{previousPreEnabledTabId}')");
            LogTabState("After protection cleared");
        }
        
        // Start periodic visibility check
        StartVisibilityCheck();
        
        // Log initial tab state after initialization
        if (debugMode)
        {
            LogTabState("Post-initialization (final)");
        }
    }
    
    /// <summary>
    /// Start periodic visibility check coroutine
    /// </summary>
    private void StartVisibilityCheck()
    {
        // Stop existing coroutine if any
        if (visibilityCheckCoroutine != null)
        {
            StopCoroutine(visibilityCheckCoroutine);
        }
        
        visibilityCheckCoroutine = StartCoroutine(VisibilityCheckCoroutine());
    }
    
    /// <summary>
    /// Stop periodic visibility check coroutine
    /// </summary>
    private void StopVisibilityCheck()
    {
        if (visibilityCheckCoroutine != null)
        {
            StopCoroutine(visibilityCheckCoroutine);
            visibilityCheckCoroutine = null;
        }
    }
    
    /// <summary>
    /// Coroutine to periodically check tab visibility state
    /// </summary>
    private System.Collections.IEnumerator VisibilityCheckCoroutine()
    {
        while (parentPanel != null && parentPanel.IsVisible)
        {
            yield return new WaitForSeconds(0.5f);
            
            if (debugMode && parentPanel != null && parentPanel.IsVisible)
            {
                LogTabState("Periodic check");
            }
        }
        
        visibilityCheckCoroutine = null;
    }
    
    /// <summary>
    /// Log current tab visibility state
    /// </summary>
    private void LogTabState(string context)
    {
        if (string.IsNullOrEmpty(currentActiveTabId))
        {
            Log($"{context}: No active tab (currentActiveTabId is null)");
            return;
        }
        
        if (!tabLookup.ContainsKey(currentActiveTabId))
        {
            Log($"{context}: Active tab ID '{currentActiveTabId}' not found in tab lookup!");
            return;
        }
        
        var activeTab = tabLookup[currentActiveTabId];
        if (activeTab == null)
        {
            Log($"{context}: Active tab '{currentActiveTabId}' is null!");
            return;
        }
        
        bool isVisible = activeTab.IsVisible;
        bool isActive = activeTab.gameObject.activeSelf;
        
        Log($"{context}: Active tab '{currentActiveTabId}' - IsVisible: {isVisible}, activeSelf: {isActive}");
        
        // If tab should be visible but isn't, log warning
        if (!isVisible && !isActive)
        {
            Log($"WARNING: {context}: Pre-enabled tab '{currentActiveTabId}' is NOT visible! This shouldn't happen.");
        }
    }
    
    /// <summary>
    /// Called when parent panel is hidden - reset tab states
    /// </summary>
    private void OnParentPanelHidden()
    {
        // Stop visibility check
        StopVisibilityCheck();
        
        ResetTabStates();
    }
    
    /// <summary>
    /// Hide all tabs without resetting state
    /// Ensures all tabs are properly deactivated, not just hidden
    /// </summary>
    private void HideAllTabs()
    {
        foreach (var tab in tabs)
        {
            if (tab == null) continue;
            
            // If tab is visible, use Hide() for proper state management
            if (tab.IsVisible)
            {
                tab.Hide();
            }
            // If tab GameObject is active but not visible (state mismatch), force deactivate
            else if (tab.gameObject.activeSelf)
            {
                if (debugMode)
                {
                    Log($"HideAllTabs: Deactivating tab '{tab.PanelId}' - active but not visible (state mismatch)");
                }
                
                // Force deactivate using ForceHidden()
                tab.ForceHidden();
            }
        }
    }
    
    /// <summary>
    /// Initialize tabs: hide all first, then pre-enable the specified tab if set
    /// Should only be called when parent panel is visible
    /// </summary>
    private void InitializeTabs()
    {
        if (debugMode)
        {
            Log($"InitializeTabs: Starting - parent panel: {parentPanel?.name}, visible: {parentPanel?.IsVisible}, active: {parentPanel?.gameObject.activeSelf}");
        }
        
        // Only initialize if parent panel is visible and active
        if (parentPanel == null || !parentPanel.IsVisible || !parentPanel.gameObject.activeSelf)
        {
            if (debugMode)
            {
                Log($"Skipping tab initialization - parent panel is null: {parentPanel == null}, visible: {parentPanel?.IsVisible}, active: {parentPanel?.gameObject.activeSelf}");
            }
            return;
        }
        
        // Ensure tab lookup is built (safety check)
        if (tabLookup.Count == 0 && tabs.Count > 0)
        {
            if (debugMode)
            {
                Log($"Tab lookup is empty (count: {tabLookup.Count}), rebuilding from {tabs.Count} tabs...");
            }
            BuildTabLookup();
        }
        
        if (debugMode)
        {
            Log($"InitializeTabs: Tab lookup has {tabLookup.Count} tabs. Keys: [{string.Join(", ", tabLookup.Keys)}]");
        }
        
        // First, hide all tabs
        HideAllTabs();
        
        foreach (var tab in tabs)
        {
            if (tab != null && tab.gameObject.activeSelf)
            {
                if (debugMode)
                {
                    Log($"InitializeTabs: Deactivating tab '{tab.PanelId}' - Unity reactivated it (active: {tab.gameObject.activeSelf}, visible: {tab.IsVisible})");
                }
                
                // Force deactivate using ForceHidden()
                tab.ForceHidden();
            }
        }
        
        if (debugMode)
        {
            Log("InitializeTabs: All tabs hidden and verified deactivated");
        }
        
        // Reset tab state handler
        if (stateHandler != null)
        {
            stateHandler.ResetAllStates();
            if (debugMode)
            {
                Log("InitializeTabs: Tab state handler reset");
            }
        }
        
        currentActiveTabId = null;
        
        // If preEnableTabId is set and valid, show that tab
        if (!string.IsNullOrEmpty(preEnableTabId))
        {
            if (debugMode)
            {
                Log($"InitializeTabs: Pre-enable tab ID is set: '{preEnableTabId}'");
            }
            
            if (tabLookup.ContainsKey(preEnableTabId))
            {
                var preEnableTab = tabLookup[preEnableTabId];
                if (preEnableTab != null)
                {
                    if (debugMode)
                    {
                        Log($"InitializeTabs: Found pre-enable tab '{preEnableTabId}' - panel: {preEnableTab.name}, visible: {preEnableTab.IsVisible}, active: {preEnableTab.gameObject.activeSelf}");
                    }
                    
                    // Show the pre-enabled tab
                    // Use UIPanelManager if available to ensure proper registration and state management
                    if (!preEnableTab.IsVisible)
                    {
                        if (debugMode)
                        {
                            Log($"InitializeTabs: Showing pre-enable tab '{preEnableTabId}'");
                        }
                        
                        // Try to use UIPanelManager if panel is registered
                        if (UIPanelManager.Instance != null)
                        {
                            var registeredPanel = UIPanelManager.Instance.GetPanel(preEnableTab.PanelId);
                            if (registeredPanel != null)
                            {
                                if (debugMode)
                                {
                                    Log($"InitializeTabs: Using UIPanelManager.ShowPanel() for pre-enable tab '{preEnableTabId}'");
                                }
                                UIPanelManager.Instance.ShowPanel(preEnableTab.PanelId);
                            }
                            else
                            {
                                // Panel not registered yet - show directly (will register when Awake runs)
                                if (debugMode)
                                {
                                    Log($"InitializeTabs: Tab '{preEnableTabId}' not registered yet, showing directly");
                                }
                                preEnableTab.Show();
                            }
                        }
                        else
                        {
                            // UIPanelManager not available - show directly
                            if (debugMode)
                            {
                                Log($"InitializeTabs: UIPanelManager.Instance is null, showing tab '{preEnableTabId}' directly");
                            }
                            preEnableTab.Show();
                        }
                    }
                    else if (debugMode)
                    {
                        Log($"InitializeTabs: Pre-enable tab '{preEnableTabId}' is already visible");
                    }
                    
                    // Update tab state handler
                    if (stateHandler != null)
                    {
                        if (debugMode)
                        {
                            Log($"InitializeTabs: Updating state handler for tab '{preEnableTabId}'");
                        }
                        stateHandler.SwitchToTab(preEnableTabId);
                    }
                    
                    currentActiveTabId = preEnableTabId;
                    
                    if (debugMode)
                    {
                        Log($"InitializeTabs: Successfully pre-enabled tab: {preEnableTabId}");
                    }
                }
                else
                {
                    LogError($"Pre-enable tab '{preEnableTabId}' is null!");
                }
            }
            else
            {
                LogError($"Pre-enable tab '{preEnableTabId}' not found in tab lookup! Available tabs: [{string.Join(", ", tabLookup.Keys)}]");
            }
        }
        else if (debugMode)
        {
            Log("InitializeTabs: No tab pre-enabled (all tabs hidden initially)");
        }
    }
    
    private void BuildTabLookup()
    {
        tabLookup.Clear();
        
        foreach (var tab in tabs)
        {
            if (tab == null) continue;
            
            // Get tab ID from TabComponent if available, otherwise use PanelId
            string tabId = GetTabId(tab);
            
            if (string.IsNullOrEmpty(tabId))
            {
                Debug.LogWarning($"[TabManager] {gameObject.name}: Tab '{tab.name}' has no ID. Skipping.");
                continue;
            }
            
            if (tabLookup.ContainsKey(tabId))
            {
                Debug.LogWarning($"[TabManager] {gameObject.name}: Duplicate tab ID '{tabId}'. Skipping duplicate.");
                continue;
            }
            
            tabLookup[tabId] = tab;
        }
        
        if (debugMode)
        {
            Log($"Built tab lookup: {tabLookup.Count} tabs registered");
        }
    }
    
    private string GetTabId(UIPanel tab)
    {
        // First check for TabComponent
        var tabComponent = tab.GetComponent<TabComponent>();
        if (tabComponent != null && !string.IsNullOrEmpty(tabComponent.TabId))
        {
            return tabComponent.TabId;
        }
        
        // Fallback to PanelId
        return tab.PanelId;
    }
    
    private void InitializeStateHandler()
    {
        switch (stateHandlerType)
        {
            case TabStateHandlerType.None:
                stateHandler = null;
                break;
                
            case TabStateHandlerType.Animator:
                if (tabAnimator == null)
                {
                    Debug.LogWarning($"[TabManager] {gameObject.name}: Animator handler selected but no Animator assigned.");
                    stateHandler = null;
                    break;
                }
                
                var animatorHandler = new AnimatorTabStateHandler();
                animatorHandler.Animator = tabAnimator;
                
                // Use animatorTabIds if provided, otherwise use all tab IDs
                string[] tabIds = animatorTabIds.Length > 0 
                    ? animatorTabIds 
                    : tabLookup.Keys.ToArray();
                
                animatorHandler.Initialize(tabIds);
                stateHandler = animatorHandler;
                break;
                
            default:
                stateHandler = null;
                break;
        }
    }
    
    /// <summary>
    /// Switch to a specific tab by ID
    /// </summary>
    /// <param name="tabId">ID of the tab to switch to</param>
    public void SwitchTab(string tabId)
    {
        if (debugMode)
        {
            Log($"SwitchTab: Called with tabId='{tabId}', isInitializing={isInitializing}, preEnabledTabId='{preEnabledTabId}', currentActiveTabId='{currentActiveTabId}'");
        }
        
        // Block external SwitchTab calls during initialization to prevent overriding pre-enabled tab
        if (isInitializing)
        {
            if (debugMode)
            {
                Log($"SwitchTab: BLOCKED call to '{tabId}' - initialization in progress");
            }
            return;
        }
        
        // If switching to the same tab that was just pre-enabled, ignore it (likely from Animator trigger)
        if (!string.IsNullOrEmpty(preEnabledTabId) && tabId == preEnabledTabId && currentActiveTabId == preEnabledTabId)
        {
            if (debugMode)
            {
                Log($"SwitchTab: IGNORED redundant switch to pre-enabled tab '{tabId}' (already active)");
            }
            return;
        }
        
        if (string.IsNullOrEmpty(tabId))
        {
            LogError("Tab ID is empty!");
            return;
        }
        
        if (!tabLookup.ContainsKey(tabId))
        {
            LogError($"Tab not found: {tabId}");
            return;
        }
        
        var targetTab = tabLookup[tabId];
        if (targetTab == null)
        {
            LogError($"Tab '{tabId}' is null!");
            return;
        }
        
        if (debugMode)
        {
            Log($"SwitchTab: Processing switch to '{tabId}' - current active: '{currentActiveTabId}', target visible: {targetTab.IsVisible}");
        }
        
        // Hide all other tabs
        foreach (var kvp in tabLookup)
        {
            if (kvp.Key != tabId && kvp.Value != null)
            {
                if (kvp.Value.IsVisible)
                {
                    if (debugMode)
                    {
                        Log($"SwitchTab: Hiding tab '{kvp.Key}'");
                    }
                    kvp.Value.Hide();
                }
            }
        }
        
        // Show target tab
        if (!targetTab.IsVisible)
        {
            if (debugMode)
            {
                Log($"SwitchTab: Showing tab '{tabId}'");
            }
            targetTab.Show();
        }
        else if (debugMode)
        {
            Log($"SwitchTab: Tab '{tabId}' is already visible");
        }
        
        // Update tab button states
        if (stateHandler != null)
        {
            if (debugMode)
            {
                Log($"SwitchTab: Updating state handler for tab '{tabId}'");
            }
            stateHandler.SwitchToTab(tabId);
        }
        
        currentActiveTabId = tabId;
        
        if (debugMode)
        {
            Log($"SwitchTab: Successfully switched to tab: {tabId}");
        }
    }
    
    /// <summary>
    /// Get the currently active tab ID
    /// </summary>
    public string GetCurrentTabId()
    {
        return currentActiveTabId;
    }
    
    /// <summary>
    /// Get all registered tab IDs
    /// </summary>
    public string[] GetAllTabIds()
    {
        return tabLookup.Keys.ToArray();
    }
    
    /// <summary>
    /// Reset all tab states (called when panel is closed)
    /// Hides all tabs and resets state handler
    /// </summary>
    public void ResetTabStates()
    {
        // Hide all tabs first
        HideAllTabs();
        
        // Reset tab state handler
        if (stateHandler != null)
        {
            stateHandler.ResetAllStates();
        }
        
        currentActiveTabId = null;
    }
    
    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[TabManager] {gameObject.name}: {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[TabManager] {gameObject.name}: {message}");
    }
    
    void OnValidate()
    {
        if (parentPanel == null)
        {
            parentPanel = GetComponent<UIPanel>();
        }
    }

    #if UNITY_EDITOR
    /// <summary>
    /// Auto-discover tabs in children (editor helper)
    /// </summary>
    [ContextMenu("Auto-Discover Tabs")]
    private void AutoDiscoverTabs()
    {
        tabs.Clear();
        UIPanel[] childPanels = GetComponentsInChildren<UIPanel>(true);
        
        foreach (var panel in childPanels)
        {
            // Skip self (parent panel)
            if (panel == parentPanel) continue;
            
            // Only add if it's a direct or indirect child
            if (panel.transform.IsChildOf(transform))
            {
                tabs.Add(panel);
            }
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[TabManager] {gameObject.name}: Auto-discovered {tabs.Count} tabs");
    }
    #endif
}


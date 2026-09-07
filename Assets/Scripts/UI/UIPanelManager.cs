using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EventBus;

/// <summary>
/// Lightweight panel manager with automatic sorting order management
/// Handles panel lifecycle and ensures correct z-ordering
/// </summary>
public class UIPanelManager : MonoBehaviour
{
    public static UIPanelManager Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private bool debugMode = true;
    
    [Header("Global Events")]
    [Tooltip("Invoked when any panel is opened (includes panel ID)")]
    public UnityEvent<string> OnAnyPanelOpened = new UnityEvent<string>();
    [Tooltip("Invoked when any panel is closed (includes panel ID)")]
    public UnityEvent<string> OnAnyPanelClosed = new UnityEvent<string>();
    [Tooltip("Invoked when switching between sections (includes old and new section)")]
    public UnityEvent<GameSection, GameSection> OnSectionSwitched = new UnityEvent<GameSection, GameSection>();
    
    [Header("Section Management")]
    [Tooltip("Initial section to enable when game starts")]
    [SerializeField] private GameSection initialSection = GameSection.MainMenu;
    
    [Header("Base Panels per Section")]
    [Tooltip("Base panel ID for Main Menu section")]
    [SerializeField] private string mainMenuBasePanelId = "";
    [Tooltip("Base panel ID for In Game section")]
    [SerializeField] private string inGameBasePanelId = "";
    [Tooltip("Base panel ID for Minigame section")]
    [SerializeField] private string minigameBasePanelId = "";
    [Tooltip("Base panel ID for Simulation section")]
    [SerializeField] private string simulationBasePanelId = "";
    
    // Panel tracking
    private Dictionary<string, UIPanel> panels = new Dictionary<string, UIPanel>();
    private Stack<UIPanel> panelStack = new Stack<UIPanel>();
    
    // Section management
    private GameSection currentSection = GameSection.None;
    private Dictionary<GameSection, List<UIPanel>> basePanels = new Dictionary<GameSection, List<UIPanel>>();
    private Dictionary<GameSection, List<UIPanel>> sectionPanels = new Dictionary<GameSection, List<UIPanel>>();
    private List<UIPanel> sectionIndependentPanels = new List<UIPanel>();
    private bool isTransitioning = false;
    private GameSection? pendingSectionSwitch;
    
    // Flag for UIPanel.Show() to check if EnableSection is in progress (replaces expensive StackTrace)
    public static bool IsEnablingSectionInProgress { get; private set; }
    
    // Cleanup coroutine tracking - track cleanup coroutines per section to prevent old ones from interfering
    private Dictionary<GameSection, Coroutine> cleanupCoroutines = new Dictionary<GameSection, Coroutine>();
    
    // Cache for IsTab results to avoid GetComponent+parent traversal per panel
    private Dictionary<UIPanel, bool> _isTabCache = new Dictionary<UIPanel, bool>();
    // Reusable list for stack rebuild (avoids ToList() allocation)
    private List<UIPanel> _stackRebuildList = new List<UIPanel>();
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Panels now register themselves in Awake() via registration pattern
        // No need to call RegisterAllPanels() - panels will register as they initialize
        
        // Organize panels by section, disable them, and switch to initial section
        // All done in one coroutine to ensure proper ordering
        StartCoroutine(OrganizePanelsDelayed());
    }
    
    /// <summary>
    /// Disable all panels immediately (called in Awake to handle panels active in scene)
    /// This runs before Start() to ensure clean initial state
    /// </summary>
    private void DisableAllPanelsImmediate()
    {
        int disabledCount = 0;
        foreach (var panel in panels.Values)
        {
            if (panel == null) continue;
            
            // Skip if panel is section-independent (those manage themselves)
            if (panel.IsSectionIndependent) continue;
            
            // Disable all panels - the active section will be enabled in Start()
            // Use Hide() for proper state management if panel is visible, otherwise direct SetActive
            if (panel.IsVisible)
            {
                panel.Hide();
                disabledCount++;
                
                if (debugMode)
                {
                    Log($"Disabled panel in Awake: {panel.PanelId}");
                }
            }
            else if (panel.gameObject.activeSelf)
            {
                // Panel is active but not visible (inconsistent state) - force disable directly
                panel.gameObject.SetActive(false);
                disabledCount++;
                
                if (debugMode)
                {
                    Log($"Force disabled inactive panel in Awake: {panel.PanelId}");
                }
            }
        }
        
        if (debugMode && disabledCount > 0)
        {
            Log($"Disabled {disabledCount} panels in Awake");
        }
    }
    
    private void Start()
    {
        // Initial section switching is now handled in OrganizePanelsDelayed() coroutine
        // to ensure all panels are registered and organized first
        
        // Start persistent watchdog to catch panels enabled by external scripts or Unity
        StartCoroutine(PanelWatchdog());
        if (debugMode)
        {
            Log("Started persistent panel watchdog");
        }
    }
    
    private System.Collections.IEnumerator OrganizePanelsDelayed()
    {
        // Wait multiple frames to ensure all panels have registered themselves
        // Panels register in their Awake() which runs after UIPanelManager.Awake()
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        // Organize panels by section
        OrganizePanelsBySection();
        
        // After organizing, disable all panels initially
        DisableAllPanelsImmediate();
        
        // Now switch to initial section (delayed to ensure everything is ready)
        if (initialSection != GameSection.None)
        {
            SwitchToSection(initialSection, useTransition: false);
        }
    }
    
    private System.Collections.IEnumerator DisableAllPanelsDelayed()
    {
        // This is now handled in OrganizePanelsDelayed() to ensure proper ordering
        yield break;
    }
    
    /// <summary>
    /// Register a panel with the manager (called by panels themselves in Awake)
    /// </summary>
    public void Register(UIPanel panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UIPanelManager] Attempted to register null panel!");
            return;
        }
        
        if (!panels.ContainsKey(panel.PanelId))
        {
            panels[panel.PanelId] = panel;
            Log($"Registered: {panel.PanelId}");
        }
        else
        {
            Debug.LogWarning($"[UIPanelManager] Panel with ID '{panel.PanelId}' already registered! Skipping duplicate.");
        }
    }
    
    /// <summary>
    /// Organize panels by section after registration
    /// Note: We iterate over ALL panels found in the scene, not just registered ones,
    /// to handle cases where multiple panels share the same ID (in different sections)
    /// </summary>
    private void OrganizePanelsBySection()
    {
        basePanels.Clear();
        sectionPanels.Clear();
        sectionIndependentPanels.Clear();
        
        // Find ALL panels in scene (not just registered ones) to handle duplicate IDs
        var allPanels = FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (var panel in allPanels)
        {
            if (panel == null) continue;
            
            if (panel.IsSectionIndependent)
            {
                sectionIndependentPanels.Add(panel);
                continue;
            }
            
            GameSection section = panel.GameSection;
            
            // Register base panels (panels with BehaviorType.Base are base panels)
            if (panel.BehaviorType == PanelBehaviorType.Base)
            {
                if (!basePanels.ContainsKey(section))
                {
                    basePanels[section] = new List<UIPanel>();
                }
                if (!basePanels[section].Contains(panel))
                {
                    basePanels[section].Add(panel);
                }
            }
            
            // Add to section list
            if (!sectionPanels.ContainsKey(section))
            {
                sectionPanels[section] = new List<UIPanel>();
            }
            if (!sectionPanels[section].Contains(panel))
            {
                sectionPanels[section].Add(panel);
            }
        }
        
        // Validate base panel IDs from inspector
        ValidateBasePanelIds();
        
        Log($"Organized panels: {sectionPanels.Count} sections, {basePanels.Count} base panels, {sectionIndependentPanels.Count} independent");
    }
    
    /// <summary>
    /// Validate base panel IDs from inspector configuration
    /// Ensures inspector-configured base panels are in the list (for backward compatibility)
    /// Other Base panels are already added by OrganizePanels()
    /// </summary>
    private void ValidateBasePanelIds()
    {
        // Ensure inspector-configured base panels are in the list
        // They should already be there if they have BehaviorType.Base, but add them if not
        if (!string.IsNullOrEmpty(mainMenuBasePanelId) && panels.TryGetValue(mainMenuBasePanelId, out var panel))
        {
            if (!basePanels.ContainsKey(GameSection.MainMenu))
            {
                basePanels[GameSection.MainMenu] = new List<UIPanel>();
            }
            if (!basePanels[GameSection.MainMenu].Contains(panel))
            {
                basePanels[GameSection.MainMenu].Add(panel);
            }
        }
        if (!string.IsNullOrEmpty(inGameBasePanelId) && panels.TryGetValue(inGameBasePanelId, out panel))
        {
            if (!basePanels.ContainsKey(GameSection.InGame))
            {
                basePanels[GameSection.InGame] = new List<UIPanel>();
            }
            if (!basePanels[GameSection.InGame].Contains(panel))
            {
                basePanels[GameSection.InGame].Add(panel);
            }
        }
        if (!string.IsNullOrEmpty(minigameBasePanelId) && panels.TryGetValue(minigameBasePanelId, out panel))
        {
            if (!basePanels.ContainsKey(GameSection.Minigame))
            {
                basePanels[GameSection.Minigame] = new List<UIPanel>();
            }
            if (!basePanels[GameSection.Minigame].Contains(panel))
            {
                basePanels[GameSection.Minigame].Add(panel);
            }
        }
        if (!string.IsNullOrEmpty(simulationBasePanelId) && panels.TryGetValue(simulationBasePanelId, out panel))
        {
            if (!basePanels.ContainsKey(GameSection.Simulation))
            {
                basePanels[GameSection.Simulation] = new List<UIPanel>();
            }
            if (!basePanels[GameSection.Simulation].Contains(panel))
            {
                basePanels[GameSection.Simulation].Add(panel);
            }
        }
    }
    
    /// <summary>
    /// Show panel by ID with optional data
    /// Only shows panels from active section or section-independent panels
    /// Prioritizes panels from current section when multiple panels share the same ID
    /// </summary>
    public void ShowPanel(string panelId, object data = null)
    {
        UIPanel panel = null;
        
        // First, try to find panel in current section
        var panelsInSection = GetPanelsInSection(currentSection);
        foreach (var p in panelsInSection)
        {
            if (p != null && p.PanelId == panelId)
            {
                panel = p;
                break;
            }
        }
        
        // If not found in current section, try section-independent panels
        if (panel == null)
        {
            foreach (var p in sectionIndependentPanels)
            {
                if (p != null && p.PanelId == panelId)
                {
                    panel = p;
                    break;
                }
            }
        }
        
        // If still not found, fall back to global lookup (but validate section)
        if (panel == null)
        {
            if (!panels.TryGetValue(panelId, out panel))
            {
                Debug.LogError($"[UIPanelManager] Panel not found: {panelId}");
                return;
            }
            
            // Validate that panel belongs to current section or is section-independent
            if (!panel.IsSectionIndependent && panel.GameSection != currentSection)
            {
                Debug.LogWarning($"[UIPanelManager] Cannot show panel '{panelId}' - it belongs to section {panel.GameSection} but current section is {currentSection}");
                return;
            }
        }
        
        // Final validation: ensure panel can be shown
        if (!panel.IsSectionIndependent && panel.GameSection != currentSection)
        {
            Debug.LogWarning($"[UIPanelManager] Cannot show panel '{panelId}' - it belongs to section {panel.GameSection} but current section is {currentSection}");
            return;
        }
        
        panel.Show(data);
    }
    
    /// <summary>
    /// Hide panel by ID
    /// Prioritizes panels from current section when multiple panels share the same ID
    /// </summary>
    public void HidePanel(string panelId)
    {
        UIPanel panel = null;
        
        // First, try to find panel in current section
        var panelsInSection = GetPanelsInSection(currentSection);
        foreach (var p in panelsInSection)
        {
            if (p != null && p.PanelId == panelId)
            {
                panel = p;
                break;
            }
        }
        
        // If not found in current section, try section-independent panels
        if (panel == null)
        {
            foreach (var p in sectionIndependentPanels)
            {
                if (p != null && p.PanelId == panelId)
                {
                    panel = p;
                    break;
                }
            }
        }
        
        // If still not found, fall back to global lookup
        if (panel == null)
        {
            panels.TryGetValue(panelId, out panel);
        }
        
        if (panel != null)
        {
            panel.Hide();
        }
    }
    
    /// <summary>
    /// Get panel by ID (for direct access)
    /// Note: This method does NOT filter by section - it returns the first panel found with the given ID.
    /// For section-aware lookup, use GetPanelsInSection() or call ShowPanel/HidePanel which prioritize current section.
    /// </summary>
    public UIPanel GetPanel(string panelId)
    {
        panels.TryGetValue(panelId, out var panel);
        return panel;
    }
    
    /// <summary>
    /// Get current panel (top of stack)
    /// </summary>
    public UIPanel GetCurrentPanel()
    {
        return panelStack.Count > 0 ? panelStack.Peek() : null;
    }
    
    /// <summary>
    /// Check if panel is visible
    /// </summary>
    public bool IsPanelVisible(string panelId)
    {
        var panelsInSection = GetPanelsInSection(currentSection);
        foreach (var p in panelsInSection)
        {
            if (p != null && p.PanelId == panelId)
            {
                return p.IsVisible;
            }
        }
        
        foreach (var p in sectionIndependentPanels)
        {
            if (p != null && p.PanelId == panelId)
            {
                return p.IsVisible;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Get all visible panels
    /// </summary>
    public List<UIPanel> GetVisiblePanels()
    {
        return panels.Values.Where(p => p.IsVisible).ToList();
    }
    
    /// <summary>
    /// Hide current panel
    /// </summary>
    public void HideCurrentPanel()
    {
        if (panelStack.Count > 0)
        {
            var current = panelStack.Peek();
            current.Hide();
        }
    }
    
    /// <summary>
    /// Called by panels when they are shown
    /// </summary>
    public void NotifyPanelShown(UIPanel panel)
    {
        // Check if this panel is a tab (should not be added to stack)
        // Tabs are managed by TabManager and should not interfere with panel stack
        if (IsTab(panel))
        {
            // Fire global event but don't add to stack
            OnAnyPanelOpened?.Invoke(panel.PanelId);
            if (debugMode)
            {
                Log($"Shown tab: {panel.PanelId} (not added to stack)");
            }
            return;
        }
        
        if (!panelStack.Contains(panel))
        {
            panelStack.Push(panel);
        }
        
        // Fire global event
        OnAnyPanelOpened?.Invoke(panel.PanelId);
        
        Log($"Shown: {panel.PanelId} (stack: {panelStack.Count})");
    }
    
    /// <summary>
    /// Check if a panel is a tab (managed by TabManager)
    /// A panel is a tab if it has TabComponent or its parent has TabManager
    /// Tabs are excluded from cleanup methods because they are managed separately by TabManager
    /// and should be shown/hidden with their parent panel, not by UIPanelManager's cleanup logic
    /// </summary>
    private bool IsTab(UIPanel panel)
    {
        if (panel == null) return false;
        
        // Check cache first
        if (_isTabCache.TryGetValue(panel, out bool cached))
        {
            return cached;
        }
        
        // Check if panel has TabComponent
        if (panel.GetComponent<TabComponent>() != null)
        {
            _isTabCache[panel] = true;
            return true;
        }
        
        // Check if parent has TabManager
        Transform parent = panel.transform.parent;
        while (parent != null)
        {
            if (parent.GetComponent<TabManager>() != null)
            {
                _isTabCache[panel] = true;
                return true;
            }
            parent = parent.parent;
        }
        
        _isTabCache[panel] = false;
        return false;
    }
    
    /// <summary>
    /// Called by panels when they are hidden
    /// </summary>
    public void NotifyPanelHidden(UIPanel panel)
    {
        // Check if this panel is a tab (should not be in stack, but remove if present)
        bool isTab = IsTab(panel);
        
        if (isTab)
        {
            // Tabs shouldn't be in stack, but remove if somehow present
            if (panelStack.Contains(panel))
            {
                RebuildStackWithout(panel);
            }
            
            // Fire global event but don't log stack count for tabs
            OnAnyPanelClosed?.Invoke(panel.PanelId);
            if (debugMode)
            {
                Log($"Hidden tab: {panel.PanelId} (not in stack)");
            }
            return;
        }
        
        // Remove from stack
        if (panelStack.Count > 0 && panelStack.Peek() == panel)
        {
            panelStack.Pop();
        }
        else
        {
            // Remove from anywhere in stack if present
            if (panelStack.Contains(panel))
            {
                RebuildStackWithout(panel);
            }
        }
        
        // Fire global event
        OnAnyPanelClosed?.Invoke(panel.PanelId);
        
        Log($"Hidden: {panel.PanelId} (stack: {panelStack.Count})");
    }
    
    /// <summary>
    /// Rebuild the panel stack without the specified panel (avoids ToList allocation)
    /// </summary>
    private void RebuildStackWithout(UIPanel panelToRemove)
    {
        _stackRebuildList.Clear();
        // Drain stack into list (reversed order)
        while (panelStack.Count > 0)
        {
            var p = panelStack.Pop();
            if (p != panelToRemove)
            {
                _stackRebuildList.Add(p);
            }
        }
        // Push back in reverse order to restore original order
        for (int i = _stackRebuildList.Count - 1; i >= 0; i--)
        {
            panelStack.Push(_stackRebuildList[i]);
        }
        _stackRebuildList.Clear();
    }
    
    #region Section Management
    
    /// <summary>
    /// Switch to a different section (exclusive - only one section active)
    /// </summary>
    public void SwitchToSection(GameSection section, bool useTransition = true)
    {
        Log($"SwitchToSection called: {currentSection} -> {section}, useTransition={useTransition}");
        
        if (section == currentSection)
        {
            Log($"Already in section: {section}");
            return;
        }
        
        if (isTransitioning)
        {
            LogWarning($"Already transitioning, cannot switch to: {section}");
            return;
        }

        if (useTransition && TransitionPanel.Instance != null)
        {
            Log("Starting SwitchSectionWithTransition coroutine");
            StartCoroutine(SwitchSectionWithTransition(section));
        }
        else
        {
            Log("Using immediate section switch");
            SwitchSectionImmediate(section);
        }
    }
    
    /// <summary>
    /// Switch section with transition panel animation
    /// </summary>
    private IEnumerator SwitchSectionWithTransition(GameSection targetSection)
    {
        Log($"SwitchSectionWithTransition called: {currentSection} -> {targetSection}");
        isTransitioning = true;
        
        GameSection oldSection = currentSection;  // Store old section BEFORE changing
        
        // Check if TransitionPanel exists
        Log($"Checking TransitionPanel.Instance: {(TransitionPanel.Instance != null ? "EXISTS" : "NULL")}");
        
        if (TransitionPanel.Instance != null)
        {
            Log("Using event-driven transition");
            // Use event-driven transition
            this.pendingSectionSwitch = targetSection;
            bool transitionComplete = false;
            
            EventBinding<TransitionMidPointEvent> midPointBinding = new EventBinding<TransitionMidPointEvent>((evt) =>
            {
                Log("TransitionMidPointEvent received");
                if (this.pendingSectionSwitch.HasValue)
                {
                    GameSection previousSection = currentSection;
                    
                    if (previousSection != GameSection.None)
                    {
                        StopCleanupCoroutine(previousSection);
                        HideChildPanels(previousSection);
                        DisableSection(previousSection);
                    }
                    
                    EnableSection(this.pendingSectionSwitch.Value);
                    
                    // Update current section
                    currentSection = this.pendingSectionSwitch.Value;
                    
                    // Fire OnSectionSwitched HERE at mid-point, BEFORE fade-out
                    // This allows handlers to prepare the section while screen is black
                    OnSectionSwitched?.Invoke(oldSection, currentSection);
                    
                    this.pendingSectionSwitch = null;
                }
            });
            
            EventBinding<TransitionCompleteEvent> completeBinding = new EventBinding<TransitionCompleteEvent>((evt) =>
            {
                Log("TransitionCompleteEvent received");
                transitionComplete = true;
            });
            
            Log("Registering event bindings");
            EventBus<TransitionMidPointEvent>.Register(midPointBinding);
            EventBus<TransitionCompleteEvent>.Register(completeBinding);
            
            Log("Raising TransitionRequestEvent");
            EventBus<TransitionRequestEvent>.Raise(new TransitionRequestEvent());
            
            Log("Waiting for transition to complete");
            while (!transitionComplete)
                yield return null;
            
            Log("Transition complete, deregistering events");
            EventBus<TransitionMidPointEvent>.Deregister(midPointBinding);
            EventBus<TransitionCompleteEvent>.Deregister(completeBinding);
            
            // Set isTransitioning to false after transition is fully complete
            isTransitioning = false;
        }
        else
        {
            Log("Fallback: No TransitionPanel, executing immediately");
            // Fallback: No TransitionPanel, execute immediately
            GameSection previousSection = currentSection;
            
            if (previousSection != GameSection.None)
            {
                StopCleanupCoroutine(previousSection);
                HideChildPanels(previousSection);
                DisableSection(previousSection);
            }
            
            EnableSection(targetSection);
            currentSection = targetSection;
            
            isTransitioning = false;
            OnSectionSwitched?.Invoke(oldSection, targetSection);
        }
        
        Log($"Switched to section: {targetSection}");
    }
    
    /// <summary>
    /// Switch section immediately without transition
    /// </summary>
    private void SwitchSectionImmediate(GameSection section)
    {
        // Disable current section
        GameSection oldSection = currentSection;
        if (currentSection != GameSection.None)
        {
            // Stop cleanup coroutine for old section
            StopCleanupCoroutine(currentSection);
            DisableSection(currentSection);
        }
        else
        {
            // When switching from None (initial state), hide all panels from all sections
            // This ensures a clean state on game start
            HideAllPanels();
        }
        
        // Enable new section
        EnableSection(section);
        currentSection = section;
        
        // Fire section switched event
        OnSectionSwitched?.Invoke(oldSection, section);
        
        Log($"Switched to section: {section}");
    }
    
    /// <summary>
    /// Enable a section (show ALL base panels, child panels remain hidden)
    /// </summary>
    private void EnableSection(GameSection section)
    {
        IsEnablingSectionInProgress = true;
        try
        {
            EnableSectionInternal(section);
        }
        finally
        {
            IsEnablingSectionInProgress = false;
        }
    }
    
    /// <summary>
    /// Internal implementation of EnableSection
    /// </summary>
    private void EnableSectionInternal(GameSection section)
    {
        if (basePanels.TryGetValue(section, out var basePanelsList) && basePanelsList.Count > 0)
        {
            if (debugMode)
            {
                Log($"[EnableSection] Found {basePanelsList.Count} base panel(s) for section {section}");
            }
            
            // FIRST: Disable all child panels BEFORE showing the base panels
            // This prevents Unity from auto-enabling children when parent is activated
            if (debugMode)
            {
                Log($"[EnableSection] Calling DisableAllChildPanelsInSection (first time)");
            }
            DisableAllChildPanelsInSection(section, basePanelsList);
            
            // THEN: Show all base panels
            if (debugMode)
            {
                Log($"[EnableSection] Showing {basePanelsList.Count} base panel(s)");
            }
            foreach (var basePanel in basePanelsList)
            {
                if (basePanel != null)
                {
                    if (debugMode)
                    {
                        Log($"[EnableSection] Showing base panel '{basePanel.PanelId}'");
                    }
                    basePanel.Show();
                }
            }
            Log($"Enabled section: {section} ({basePanelsList.Count} base panel(s))");
            
            // IMMEDIATELY after showing base panels, disable all child panels again
            // Unity may auto-enable children when parent GameObjects are activated
            if (debugMode)
            {
                Log($"[EnableSection] Calling DisableAllChildPanelsInSection (second time)");
            }
            DisableAllChildPanelsInSection(section, basePanelsList);
            
            // Final safety check: one frame delay to catch any very late activations
            // Stop any existing cleanup coroutine for this section first
            StopCleanupCoroutine(section);
            
            if (debugMode)
            {
                Log($"[EnableSection] Starting DisableChildPanelsDelayed coroutine for section {section}");
            }
            var cleanupCoroutine = StartCoroutine(DisableChildPanelsDelayed(section, basePanelsList));
            cleanupCoroutines[section] = cleanupCoroutine;
        }
        else
        {
            LogWarning($"No base panels found for section: {section}");
        }
        
        // Child panels remain hidden until explicitly shown
        // (navigation/transitions will show them as needed)
        
        if (debugMode)
        {
            Log($"[EnableSection] Completed for section {section}");
        }
    }
    
    /// <summary>
    /// Disable all child panels in a section before showing the base panels
    /// Note: Tabs (managed by TabManager) are excluded from this cleanup - they are managed separately
    /// </summary>
    private void DisableAllChildPanelsInSection(GameSection section, List<UIPanel> basePanelsList)
    {
        if (debugMode)
        {
            Log($"[DisableAllChildPanelsInSection] Called for section {section} with {basePanelsList.Count} base panel(s)");
        }
        

        if (sectionPanels.TryGetValue(section, out var sectionPanelList))
        {
            if (debugMode)
            {
                Log($"[DisableAllChildPanelsInSection] Checking {sectionPanelList.Count} panels in section {section}");
            }
            
            foreach (var panel in sectionPanelList)
            {
                if (panel != null && !basePanelsList.Contains(panel))
                {
                    bool isTab = IsTab(panel);
                    if (debugMode)
                    {
                        Log($"[DisableAllChildPanelsInSection] Panel '{panel.PanelId}' - isBasePanel: false, isTab: {isTab}, isVisible: {panel.IsVisible}, activeSelf: {panel.gameObject.activeSelf}");
                    }
                    
                    // Skip tabs
                    if (isTab)
                    {
                        if (debugMode)
                        {
                            Log($"[DisableAllChildPanelsInSection] Skipping tab '{panel.PanelId}' - managed by TabManager");
                        }
                        continue;
                    }
                    
                    // Use Hide() for proper state management if panel is visible
                    if (panel.IsVisible)
                    {
                        if (debugMode)
                        {
                            Log($"[DisableAllChildPanelsInSection] Hiding panel '{panel.PanelId}'");
                        }
                        panel.Hide();
                    }
                    else
                    {
                        // Always disable, even if inactive - prevents Unity auto-enable when parent activates
                        if (debugMode)
                        {
                            Log($"[DisableAllChildPanelsInSection] Force disabling inactive panel '{panel.PanelId}'");
                        }
                        panel.ForceHidden();
                    }
                }
            }
        }
        
        // Also disable any nested UIPanel components that might be children of base panels
        // BUT skip tabs - they are managed by TabManager and should be shown/hidden with their parent
        if (debugMode)
        {
            Log($"[DisableAllChildPanelsInSection] Checking nested panels in {basePanelsList.Count} base panel(s)");
        }
        
        foreach (var basePanel in basePanelsList)
        {
            if (basePanel != null)
            {
                UIPanel[] nestedPanels = basePanel.GetComponentsInChildren<UIPanel>(true);
                if (debugMode)
                {
                    Log($"[DisableAllChildPanelsInSection] Found {nestedPanels.Length} nested panels in base panel '{basePanel.PanelId}'");
                }
                
                foreach (var nestedPanel in nestedPanels)
                {
                    if (nestedPanel != null && !basePanelsList.Contains(nestedPanel))
                    {
                        bool isTab = IsTab(nestedPanel);
                        if (debugMode)
                        {
                            Log($"[DisableAllChildPanelsInSection] Nested panel '{nestedPanel.PanelId}' in base '{basePanel.PanelId}' - isTab: {isTab}, isSectionIndependent: {nestedPanel.IsSectionIndependent}, isVisible: {nestedPanel.IsVisible}, activeSelf: {nestedPanel.gameObject.activeSelf}");
                        }
                        
                        // Skip tabs - they are managed by TabManager and should not be disabled here
                        if (isTab)
                        {
                            if (debugMode)
                            {
                                Log($"[DisableAllChildPanelsInSection] Skipping tab '{nestedPanel.PanelId}' in cleanup - managed by TabManager");
                            }
                            continue;
                        }
                        
                        // Skip section-independent panels - they manage themselves
                        if (nestedPanel.IsSectionIndependent)
                        {
                            if (debugMode)
                            {
                                Log($"[DisableAllChildPanelsInSection] Skipping section-independent panel '{nestedPanel.PanelId}' - manages itself");
                            }
                            continue;
                        }
                        
                        // Use Hide() for proper state management if panel is visible
                        if (nestedPanel.IsVisible)
                        {
                            if (debugMode)
                            {
                                Log($"[DisableAllChildPanelsInSection] Hiding nested panel '{nestedPanel.PanelId}'");
                            }
                            nestedPanel.Hide();
                        }
                        else
                        {
                            // Always disable, even if inactive - prevents Unity auto-enable when parent activates
                            if (debugMode)
                            {
                                Log($"[DisableAllChildPanelsInSection] Force disabling inactive nested panel '{nestedPanel.PanelId}'");
                            }
                            nestedPanel.ForceHidden();
                        }
                    }
                }
            }
        }
        
        if (debugMode)
        {
            Log($"[DisableAllChildPanelsInSection] Completed for section {section}");
        }
    }
    
    /// <summary>
    /// Delayed cleanup to ensure child panels stay disabled
    /// Runs at end of frame to catch any panels enabled by external scripts or Unity's auto-enable
    /// Note: Tabs (managed by TabManager) are excluded from this cleanup - they are managed separately
    /// </summary>
    private System.Collections.IEnumerator DisableChildPanelsDelayed(GameSection section, List<UIPanel> basePanelsList)
    {
        if (debugMode)
        {
            Log($"[DisableChildPanelsDelayed] Starting coroutine for section {section} with {basePanelsList.Count} base panel(s)");
        }
        

        yield return new WaitForEndOfFrame();
        
        // Verify this coroutine is still working on the correct section
        // If section has changed, stop the coroutine early to prevent interfering with new panels
        if (currentSection != section)
        {
            if (debugMode)
            {
                Log($"[DisableChildPanelsDelayed] Section changed from {section} to {currentSection} - stopping cleanup coroutine early");
            }
            // Remove from tracking
            if (cleanupCoroutines.ContainsKey(section) && cleanupCoroutines[section] != null)
            {
                cleanupCoroutines.Remove(section);
            }
            yield break;
        }
        
        if (debugMode)
        {
            Log($"[DisableChildPanelsDelayed] After WaitForEndOfFrame - checking panels in section {section}");
        }
        
        // Re-check all panels in this section
        if (sectionPanels.TryGetValue(section, out var sectionPanelList))
        {
            if (debugMode)
            {
                Log($"[DisableChildPanelsDelayed] Checking {sectionPanelList.Count} panels in section {section}");
            }
            
            foreach (var panel in sectionPanelList)
            {
                if (panel != null && !basePanelsList.Contains(panel) && panel.gameObject.activeSelf)
                {
                    bool isTab = IsTab(panel);
                    if (debugMode)
                    {
                        Log($"[DisableChildPanelsDelayed] Panel '{panel.PanelId}' - isTab: {isTab}, isVisible: {panel.IsVisible}, activeSelf: {panel.gameObject.activeSelf}");
                    }
                    
                    // Skip tabs
                    if (isTab)
                    {
                        if (debugMode)
                        {
                            Log($"[DisableChildPanelsDelayed] Skipping tab '{panel.PanelId}' - managed by TabManager");
                        }
                        continue;
                    }
                    
                    // Skip section-independent panels - they manage themselves
                    if (panel.IsSectionIndependent)
                    {
                        if (debugMode)
                        {
                            Log($"[DisableChildPanelsDelayed] Skipping section-independent panel '{panel.PanelId}' - manages itself");
                        }
                        continue;
                    }
                    
                    // Use Hide() if visible for proper state management
                    if (panel.IsVisible)
                    {
                        if (debugMode)
                        {
                            Log($"[DisableChildPanelsDelayed] Hiding panel '{panel.PanelId}'");
                        }
                        panel.Hide();
                    }
                    else
                    {
                        // Force disable and set isVisible for consistency
                        if (debugMode)
                        {
                            Log($"[DisableChildPanelsDelayed] Force disabling inactive panel '{panel.PanelId}'");
                        }
                        panel.ForceHidden();
                    }
                }
            }
        }
        
        // Also check nested panels in base panels
        // BUT skip tabs - they are managed by TabManager and should be shown/hidden with their parent
        if (debugMode)
        {
            Log($"[DisableChildPanelsDelayed] Checking nested panels in {basePanelsList.Count} base panel(s)");
        }
        
        foreach (var basePanel in basePanelsList)
        {
            if (basePanel != null)
            {
                UIPanel[] nestedPanels = basePanel.GetComponentsInChildren<UIPanel>(true);
                if (debugMode)
                {
                    Log($"[DisableChildPanelsDelayed] Found {nestedPanels.Length} nested panels in base panel '{basePanel.PanelId}'");
                }
                
                foreach (var nestedPanel in nestedPanels)
                {
                    if (nestedPanel != null && !basePanelsList.Contains(nestedPanel) && nestedPanel.gameObject.activeSelf)
                    {
                        bool isTab = IsTab(nestedPanel);
                        if (debugMode)
                        {
                            Log($"[DisableChildPanelsDelayed] Nested panel '{nestedPanel.PanelId}' in base '{basePanel.PanelId}' - isTab: {isTab}, isVisible: {nestedPanel.IsVisible}, activeSelf: {nestedPanel.gameObject.activeSelf}");
                        }
                        
                        // Skip tabs - they are managed by TabManager and should not be disabled here
                        if (isTab)
                        {
                            if (debugMode)
                            {
                                Log($"[DisableChildPanelsDelayed] Skipping tab '{nestedPanel.PanelId}' in delayed cleanup - managed by TabManager");
                            }
                            continue;
                        }
                        
                        // Skip section-independent panels - they manage themselves
                        if (nestedPanel.IsSectionIndependent)
                        {
                            if (debugMode)
                            {
                                Log($"[DisableChildPanelsDelayed] Skipping section-independent panel '{nestedPanel.PanelId}' - manages itself");
                            }
                            continue;
                        }
                        
                        // Use Hide() if visible for proper state management
                        if (nestedPanel.IsVisible)
                        {
                            if (debugMode)
                            {
                                Log($"[DisableChildPanelsDelayed] Hiding nested panel '{nestedPanel.PanelId}'");
                            }
                            nestedPanel.Hide();
                        }
                        else
                        {
                            // Force disable and set isVisible for consistency
                            if (debugMode)
                            {
                                Log($"[DisableChildPanelsDelayed] Force disabling inactive nested panel '{nestedPanel.PanelId}'");
                            }
                            nestedPanel.ForceHidden();
                        }
                    }
                }
            }
        }
        
        if (debugMode)
        {
            Log($"[DisableChildPanelsDelayed] Completed for section {section}");
        }
        
        // Remove from tracking when done
        if (cleanupCoroutines.ContainsKey(section))
        {
            cleanupCoroutines.Remove(section);
            if (debugMode)
            {
                Log($"[DisableChildPanelsDelayed] Removed cleanup coroutine tracking for section {section}");
            }
        }
    }
    
    /// <summary>
    /// Stop cleanup coroutine for a specific section
    /// </summary>
    private void StopCleanupCoroutine(GameSection section)
    {
        if (cleanupCoroutines.TryGetValue(section, out var coroutine) && coroutine != null)
        {
            if (debugMode)
            {
                Log($"[StopCleanupCoroutine] Stopping existing cleanup coroutine for section {section}");
            }
            StopCoroutine(coroutine);
            cleanupCoroutines.Remove(section);
        }
    }
    
    /// <summary>
    /// Disable a section (hide ALL base panels and all child panels)
    /// </summary>
    private void DisableSection(GameSection section)
    {
        // Hide all base panels
        if (basePanels.TryGetValue(section, out var basePanelsList))
        {
            foreach (var basePanel in basePanelsList)
            {
                if (basePanel != null)
                {
                    basePanel.Hide();
                }
            }
        }
        
        // Hide all child panels in section
        HideChildPanels(section);
        
        Log($"Disabled section: {section}");
    }
    
    /// <summary>
    /// Hide all child panels in a section (excluding all base panels)
    /// </summary>
    private void HideChildPanels(GameSection section)
    {
        if (sectionPanels.TryGetValue(section, out var panels))
        {
            var basePanelsList = GetBasePanels(section);
            foreach (var panel in panels)
            {
                if (panel != null && !basePanelsList.Contains(panel) && panel.IsVisible)
                {
                    panel.Hide();
                }
            }
        }
    }
    
    /// <summary>
    /// Hide all panels from all sections (used when switching from None/initial state)
    /// Section-independent panels are not affected
    /// </summary>
    private void HideAllPanels()
    {
        // Hide all panels from all sections
        // For panels active in scene but not "shown" through system, we need to deactivate them directly
        foreach (var kvp in sectionPanels)
        {
            GameSection section = kvp.Key;
            foreach (var panel in kvp.Value)
            {
                if (panel == null) continue;
                
                // If panel was shown through the system, use Hide() to properly handle state and events
                if (panel.IsVisible)
                {
                    panel.Hide();
                }
                // If GameObject is active in scene (but hasn't been "shown"), deactivate it directly
                // This ensures all panels are hidden regardless of their initial scene state
                else if (panel.gameObject.activeSelf)
                {
                    panel.ForceHidden();
                }
            }
        }
        
        Log("Hidden all panels from all sections");
    }
    
    /// <summary>
    /// Persistent watchdog that monitors all panels and disables unauthorized activations
    /// Runs continuously in the background to catch panels enabled by external scripts or Unity
    /// </summary>
    private System.Collections.IEnumerator PanelWatchdog()
    {
        if (debugMode)
        {
            Log("[Watchdog] Started - monitoring panels every 0.5s");
        }
        
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check every 0.5 seconds
            
            if (debugMode)
            {
                Log($"[Watchdog] Checking panels - current section: {currentSection}");
            }
            
            // Check all panels in all sections (including current section)
            foreach (var section in sectionPanels.Keys)
            {
                if (sectionPanels.TryGetValue(section, out var panelList))
                {
                    bool isCurrentSection = (section == currentSection);
                    if (debugMode)
                    {
                        Log($"[Watchdog] Checking section {section} ({panelList.Count} panels) - isCurrentSection: {isCurrentSection}");
                    }
                    
                    foreach (var panel in panelList)
                    {
                        if (panel != null && panel.gameObject.activeSelf && !panel.IsVisible)
                        {
                            // Check if this is a tab - tabs should be skipped (they're managed by TabManager)
                            if (IsTab(panel))
                            {
                                if (debugMode)
                                {
                                    Log($"[Watchdog] Skipping tab '{panel.PanelId}' in section {section} - managed by TabManager");
                                }
                                continue;
                            }
                            
                            // Check if panel is section-independent - these manage themselves
                            if (panel.IsSectionIndependent)
                            {
                                if (debugMode)
                                {
                                    Log($"[Watchdog] Skipping section-independent panel '{panel.PanelId}' - manages its own visibility");
                                }
                                continue;
                            }
                            
                            // Panel is active but shouldn't be - disable it
                            // This applies to both current and non-current sections
                            Debug.LogWarning($"[Watchdog] Detected unauthorized panel activation: {panel.PanelId} in section {section} (current: {currentSection}). Disabling.");
                            panel.ForceHidden();
                            
                            if (debugMode)
                            {
                                Log($"[Watchdog] Disabled unauthorized panel '{panel.PanelId}' in section {section}");
                            }
                        }
                        else if (debugMode && panel != null)
                        {
                            // Log panel state for debugging
                            bool isTab = IsTab(panel);
                            Log($"[Watchdog] Panel '{panel.PanelId}' in section {section} - active: {panel.gameObject.activeSelf}, visible: {panel.IsVisible}, isTab: {isTab}, isCurrentSection: {isCurrentSection}");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get the first base panel for a section (for backward compatibility)
    /// Returns the first base panel if multiple exist
    /// </summary>
    public UIPanel GetBasePanel(GameSection section)
    {
        if (basePanels.TryGetValue(section, out var basePanelsList) && basePanelsList.Count > 0)
        {
            return basePanelsList[0];
        }
        return null;
    }
    
    /// <summary>
    /// Get all base panels for a section
    /// </summary>
    public List<UIPanel> GetBasePanels(GameSection section)
    {
        if (basePanels.TryGetValue(section, out var basePanelsList))
        {
            return new List<UIPanel>(basePanelsList); // Return a copy
        }
        return new List<UIPanel>();
    }
    
    /// <summary>
    /// Get all child panels in a section (excluding all base panels)
    /// </summary>
    public List<UIPanel> GetChildPanels(GameSection section)
    {
        List<UIPanel> childPanels = new List<UIPanel>();
        
        if (sectionPanels.TryGetValue(section, out var panels))
        {
            var basePanelsList = GetBasePanels(section);
            foreach (var panel in panels)
            {
                if (panel != null && !basePanelsList.Contains(panel))
                {
                    childPanels.Add(panel);
                }
            }
        }
        
        return childPanels;
    }
    
    /// <summary>
    /// Get all panels in a section (including base panel)
    /// </summary>
    public List<UIPanel> GetPanelsInSection(GameSection section)
    {
        if (sectionPanels.TryGetValue(section, out var panels))
        {
            return new List<UIPanel>(panels);
        }
        return new List<UIPanel>();
    }
    
    /// <summary>
    /// Get current active section
    /// </summary>
    public GameSection GetCurrentSection()
    {
        return currentSection;
    }
    
    /// <summary>
    /// Check if a section is currently active
    /// </summary>
    public bool IsSectionActive(GameSection section)
    {
        return currentSection == section;
    }
    
    /// <summary>
    /// Get all section-independent panels (e.g., Live2D panels)
    /// </summary>
    public List<UIPanel> GetSectionIndependentPanels()
    {
        return new List<UIPanel>(sectionIndependentPanels);
    }
    
    /// <summary>
    /// Fade out a panel (coroutine)
    /// </summary>
    private IEnumerator FadeOutPanel(UIPanel panel)
    {
        if (panel == null || panel.Canvas == null) yield break;
        
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Fade in a panel (coroutine)
    /// </summary>
    private IEnumerator FadeInPanel(UIPanel panel)
    {
        if (panel == null || panel.Canvas == null) yield break;
        
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;
        
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        float duration = 0.3f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    #endregion
    
    private void Log(string message)
    {
        if (debugMode) Debug.Log($"[PanelManager] {message}");
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[PanelManager] {message}");
    }
}

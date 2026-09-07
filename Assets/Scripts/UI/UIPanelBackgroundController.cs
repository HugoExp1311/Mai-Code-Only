using UnityEngine;
using Base;
using Base.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Component that manages background GameObjects based on current place and time (DayCycle)
/// Supports multi-layer backgrounds with conditional layer visibility based on cycle/time
/// Add this to your In Game base panel to have dynamic backgrounds
/// 
/// [DEPRECATED] Use UIPanelWithBackground instead, which inherits from UIPanel and follows the same pattern as NavigationPanel.
/// </summary>
[System.Obsolete("UIPanelBackgroundController is deprecated. Use UIPanelWithBackground instead, which inherits from UIPanel and follows the same pattern as NavigationPanel.")]
[RequireComponent(typeof(UIPanel))]
public class UIPanelBackgroundController : MonoBehaviour
{
    [System.Serializable]
    public class BackgroundLayer
    {
        [Tooltip("Layer GameObject (child of background root)")]
        public GameObject layerObject;
        
        [Tooltip("Which cycles this layer is visible in (empty = always visible)")]
        public DayCycle[] visibleCycles = new DayCycle[0];
        
        [Tooltip("Optional: Specific hour range for visibility (0-23). Set both to same value to disable hour filtering")]
        [Range(0, 23)]
        public int startHour = 0;
        
        [Tooltip("Optional: End hour for visibility range (0-23). Set both to same value to disable hour filtering")]
        [Range(0, 23)]
        public int endHour = 0;
        
        [Tooltip("If true, hour range filtering is enabled")]
        public bool useHourRange = false;
    }
    
    [System.Serializable]
    public class BackgroundMapping
    {
        [Tooltip("Area this background is for")]
        public Area area;
        
        [Tooltip("Day cycles this background is for (can have multiple if background is the same for different cycles)")]
        public DayCycle[] cycles = new DayCycle[1] { DayCycle.Morning };
        
        [Tooltip("Background root GameObject (already in scene, child of this panel)")]
        public GameObject backgroundRoot;
        
        [Tooltip("Layers within this background (for conditional visibility)")]
        public BackgroundLayer[] layers = new BackgroundLayer[0];
    }
    
    [Header("Background Configuration")]
    [Tooltip("List of background mappings for different area/cycle combinations")]
    [SerializeField] private BackgroundMapping[] backgroundMappings = new BackgroundMapping[0];
    
    [Header("Auto-Detection")]
    [Tooltip("If true, automatically find background GameObjects using naming convention: Background_{Area}_{Cycle}")]
    [SerializeField] private bool useAutoDetection = false;
    
    [Tooltip("Name prefix for background GameObjects (used with auto-detection)")]
    [SerializeField] private string backgroundNamePrefix = "Background_";
    
    [Header("Options")]
    [Tooltip("Update background immediately when component starts (if GameManager is available)")]
    [SerializeField] private bool updateOnStart = true;
    
    [Tooltip("Listen to TimeChanged events (requires TimeChangedChannelSO to be enabled in GameManager)")]
    [SerializeField] private bool listenToTimeChanges = true;
    
    // Property to access listenToTimeChanges (prevents CS0414 warning)
    public bool ListenToTimeChanges => listenToTimeChanges;
    
    [SerializeField] private UIPanel panel;
    private Dictionary<(Area area, DayCycle cycle), BackgroundMapping> mappingCache;
    private List<GameObject> allBackgroundRoots = new List<GameObject>();
    private GameObject currentActiveBackground = null;
    
    private void Awake()
    {
        // Panel should be assigned via serialized field in Inspector
        // RequireComponent ensures it exists, but validate assignment
        if (panel == null)
        {
            Debug.LogError($"[UIPanelBackgroundController] {gameObject.name}: UIPanel component not assigned in Inspector!");
            return;
        }
        
        // Collect all background root GameObjects
        CollectBackgroundRoots();
        
        // Build mapping cache
        BuildMappingCache();
    }
    
    private void OnEnable()
    {
        // Note: This class is deprecated. Event subscriptions removed.
        // Use UIPanelWithBackground instead, which uses EventBus.
        
        // Update immediately if GameManager is available
        if (updateOnStart && GameManager.Instance != null)
        {
            UpdateBackground(GameManager.Instance.Area, GameManager.Instance.Cycle, GameManager.Instance.Time);
        }
    }
    
    private void OnDisable()
    {
        // Note: This class is deprecated. No event subscriptions to clean up.
    }
    
    private void OnDestroy()
    {
        // Note: This class is deprecated. No event subscriptions to dispose.
    }
    
    /// <summary>
    /// Collect all potential background root GameObjects from children
    /// </summary>
    private void CollectBackgroundRoots()
    {
        allBackgroundRoots.Clear();
        
        // Collect all direct children that might be background roots
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.name.StartsWith(backgroundNamePrefix) || !useAutoDetection)
            {
                allBackgroundRoots.Add(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// Build mapping cache, including auto-detection if enabled
    /// </summary>
    private void BuildMappingCache()
    {
        mappingCache = new Dictionary<(Area area, DayCycle cycle), BackgroundMapping>();
        
        // If auto-detection is enabled, populate mappings from scene GameObjects
        if (useAutoDetection)
        {
            BuildAutoDetectedMappings();
        }
        
        // Add/override with explicit mappings from Inspector
        // Each mapping can cover multiple cycles
        foreach (var mapping in backgroundMappings)
        {
            if (mapping.backgroundRoot != null && mapping.cycles != null && mapping.cycles.Length > 0)
            {
                // Create an entry for each cycle this mapping covers
                foreach (var cycle in mapping.cycles)
                {
                    var key = (mapping.area, cycle);
                    
                    if (mappingCache.ContainsKey(key))
                    {
                        Debug.LogWarning($"[UIPanelBackgroundController] {gameObject.name}: Duplicate mapping for {mapping.area}/{cycle}. Overriding with explicit mapping.");
                    }
                    
                    mappingCache[key] = mapping;
                }
            }
        }
        
        // Validate all mappings
        ValidateMappings();
    }
    
    /// <summary>
    /// Auto-detect background GameObjects based on naming convention
    /// </summary>
    private void BuildAutoDetectedMappings()
    {
        foreach (var bgRoot in allBackgroundRoots)
        {
            if (bgRoot == null) continue;
            
            string name = bgRoot.name;
            if (!name.StartsWith(backgroundNamePrefix)) continue;
            
            // Parse name: Background_{Area}_{Cycle}
            string suffix = name.Substring(backgroundNamePrefix.Length);
            string[] parts = suffix.Split('_');
            
            if (parts.Length >= 2)
            {
                // Try to parse Area
                if (Enum.TryParse<Area>(parts[0], true, out Area area))
                {
                    // Try to parse Cycle (auto-detection creates single-cycle mapping)
                    if (Enum.TryParse<DayCycle>(parts[1], true, out DayCycle cycle))
                    {
                        var key = (area, cycle);
                        
                        // Create auto-detected mapping (single cycle)
                        var mapping = new BackgroundMapping
                        {
                            area = area,
                            cycles = new DayCycle[1] { cycle },
                            backgroundRoot = bgRoot,
                            layers = AutoDetectLayers(bgRoot)
                        };
                        
                        mappingCache[key] = mapping;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Auto-detect layers from background root's children
    /// All layers default to always visible (no cycle restrictions)
    /// </summary>
    private BackgroundLayer[] AutoDetectLayers(GameObject backgroundRoot)
    {
        List<BackgroundLayer> detectedLayers = new List<BackgroundLayer>();
        
        for (int i = 0; i < backgroundRoot.transform.childCount; i++)
        {
            Transform child = backgroundRoot.transform.GetChild(i);
            detectedLayers.Add(new BackgroundLayer
            {
                layerObject = child.gameObject,
                visibleCycles = new DayCycle[0], // Always visible by default
                useHourRange = false
            });
        }
        
        return detectedLayers.ToArray();
    }
    
    /// <summary>
    /// Validate all mappings
    /// </summary>
    private void ValidateMappings()
    {
        HashSet<BackgroundMapping> validatedMappings = new HashSet<BackgroundMapping>();
        
        foreach (var kvp in mappingCache)
        {
            var mapping = kvp.Value;
            
            // Only validate each mapping once (since same mapping can appear for multiple cycles)
            if (validatedMappings.Contains(mapping)) continue;
            validatedMappings.Add(mapping);
            
            if (mapping.backgroundRoot == null)
            {
                Debug.LogWarning($"[UIPanelBackgroundController] {gameObject.name}: Background mapping for {mapping.area} has null backgroundRoot!");
                continue;
            }
            
            // Validate layers
            foreach (var layer in mapping.layers)
            {
                if (layer.layerObject == null)
                {
                    Debug.LogWarning($"[UIPanelBackgroundController] {gameObject.name}: Layer in {mapping.area} background has null layerObject!");
                }
            }
        }
    }
    
    // Note: Event handlers removed - this class is deprecated.
    // Use UIPanelWithBackground instead, which uses EventBus for event handling.
    
    /// <summary>
    /// Update background GameObject based on area and cycle
    /// </summary>
    public void UpdateBackground(Area area, DayCycle cycle, DateTime currentTime)
    {
        // Find matching background
        var key = (area, cycle);
        if (!mappingCache.TryGetValue(key, out var mapping))
        {
            Debug.LogWarning($"[UIPanelBackgroundController] {gameObject.name}: No background mapping found for {area}/{cycle}");
            // Disable all backgrounds if no match found
            DisableAllBackgrounds();
            return;
        }
        
        if (mapping.backgroundRoot == null)
        {
            Debug.LogWarning($"[UIPanelBackgroundController] {gameObject.name}: Background root is null for {area}/{cycle}");
            return;
        }
        
        // Disable all backgrounds first
        DisableAllBackgrounds();
        
        // Enable the matching background
        mapping.backgroundRoot.SetActive(true);
        currentActiveBackground = mapping.backgroundRoot;
        
        // Update layer visibility based on conditions
        UpdateLayerVisibility(mapping, cycle, currentTime);
    }
    
    /// <summary>
    /// Disable all background root GameObjects
    /// </summary>
    private void DisableAllBackgrounds()
    {
        foreach (var bgRoot in allBackgroundRoots)
        {
            if (bgRoot != null)
            {
                bgRoot.SetActive(false);
            }
        }
        
        // Also disable any mapped backgrounds not in the auto-collected list
        foreach (var kvp in mappingCache)
        {
            var bgRoot = kvp.Value.backgroundRoot;
            if (bgRoot != null && !allBackgroundRoots.Contains(bgRoot))
            {
                bgRoot.SetActive(false);
            }
        }
        
        currentActiveBackground = null;
    }
    
    /// <summary>
    /// Update layer visibility based on cycle and time conditions
    /// </summary>
    private void UpdateLayerVisibility(BackgroundMapping mapping, DayCycle currentCycle, DateTime currentTime)
    {
        foreach (var layer in mapping.layers)
        {
            if (layer.layerObject == null) continue;
            
            bool shouldBeVisible = true;
            
            // Check cycle condition
            if (layer.visibleCycles != null && layer.visibleCycles.Length > 0)
            {
                shouldBeVisible = layer.visibleCycles.Contains(currentCycle);
            }
            
            // Check hour range condition (if enabled)
            if (shouldBeVisible && layer.useHourRange)
            {
                int currentHour = currentTime.Hour;
                
                if (layer.startHour == layer.endHour)
                {
                    // Same value means disabled hour filtering
                    // Keep current visibility
                }
                else if (layer.startHour < layer.endHour)
                {
                    // Normal range (e.g., 6-18)
                    shouldBeVisible = currentHour >= layer.startHour && currentHour < layer.endHour;
                }
                else
                {
                    // Range crosses midnight (e.g., 23-6)
                    shouldBeVisible = currentHour >= layer.startHour || currentHour < layer.endHour;
                }
            }
            
            layer.layerObject.SetActive(shouldBeVisible);
        }
    }
    
    /// <summary>
    /// Manually update background (useful for testing or special cases)
    /// </summary>
    public void UpdateBackgroundNow()
    {
        if (GameManager.Instance != null)
        {
            UpdateBackground(GameManager.Instance.Area, GameManager.Instance.Cycle, GameManager.Instance.Time);
        }
        else
        {
            Debug.LogWarning($"[UIPanelBackgroundController] {gameObject.name}: GameManager.Instance is null. Cannot update background.");
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Validate mappings in editor
    /// </summary>
    private void OnValidate()
    {
        // This will be called when values change in Inspector
        // Rebuild cache if in play mode
        if (Application.isPlaying && mappingCache != null)
        {
            CollectBackgroundRoots();
            BuildMappingCache();
        }
    }
    #endif
}

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages tab-based content sections (typically ScrollViews or content containers)
/// Shows/hides sections based on tab selection
/// Supports flexible state handler system for tab button visual feedback
/// </summary>
public class ScrollViewTabManager : MonoBehaviour
{
    [Header("Content Sections")]
    [Tooltip("List of GameObjects that act as content sections (tabs) - typically ScrollViews or content containers")]
    [SerializeField] private List<GameObject> contentSections = new List<GameObject>();
    
    [Header("Section IDs")]
    [Tooltip("IDs for each content section (must match contentSections order)")]
    [SerializeField] private string[] sectionIds = new string[0];
    
    [Header("Tab Initialization")]
    [Tooltip("Section ID to show initially. Leave empty to show first section.")]
    [SerializeField] private string initialSectionId = "";
    
    [Header("Tab State Management")]
    [Tooltip("Type of state handler to use for tab button visual feedback")]
    [SerializeField] private TabStateHandlerType stateHandlerType = TabStateHandlerType.Animator;
    
    [Header("Animator Handler Settings")]
    [Tooltip("Animator Controller for tab button states")]
    [SerializeField] private Animator tabAnimator;
    
    [Tooltip("Section IDs that correspond to animator bool parameters")]
    [SerializeField] private string[] animatorSectionIds = new string[0];
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    // Internal state
    private Dictionary<string, GameObject> sectionLookup = new Dictionary<string, GameObject>();
    private ITabStateHandler stateHandler;
    private string currentActiveSectionId = null;
    
    public enum TabStateHandlerType
    {
        None,
        Animator
    }
    
    private void Awake()
    {
        ValidateConfiguration();
        BuildSectionLookup();
        InitializeStateHandler();
    }
    
    private void Start()
    {
        InitializeSections();
    }
    
    private void ValidateConfiguration()
    {
        if (contentSections.Count == 0)
        {
            LogError("No content sections assigned!");
            return;
        }
        
        if (sectionIds.Length != contentSections.Count)
        {
            LogError($"Section IDs count ({sectionIds.Length}) doesn't match content sections count ({contentSections.Count})!");
        }
    }
    
    private void BuildSectionLookup()
    {
        sectionLookup.Clear();
        
        for (int i = 0; i < contentSections.Count && i < sectionIds.Length; i++)
        {
            var section = contentSections[i];
            string sectionId = sectionIds[i];
            
            if (section == null)
            {
                LogError($"Content section at index {i} is null!");
                continue;
            }
            
            if (string.IsNullOrEmpty(sectionId))
            {
                LogError($"Section ID at index {i} is empty!");
                continue;
            }
            
            if (sectionLookup.ContainsKey(sectionId))
            {
                LogError($"Duplicate section ID: {sectionId}");
                continue;
            }
            
            sectionLookup[sectionId] = section;
        }
        
        if (debugMode)
        {
            Log($"Built section lookup: {sectionLookup.Count} sections");
        }
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
                    Debug.LogWarning($"[ScrollViewTabManager] {gameObject.name}: Animator handler selected but no Animator assigned.");
                    stateHandler = null;
                    break;
                }
                
                var animatorHandler = new AnimatorTabStateHandler();
                animatorHandler.Animator = tabAnimator;
                
                string[] ids = animatorSectionIds.Length > 0 
                    ? animatorSectionIds 
                    : sectionIds;
                
                animatorHandler.Initialize(ids);
                stateHandler = animatorHandler;
                break;
        }
    }
    
    private void InitializeSections()
    {
        if (!string.IsNullOrEmpty(initialSectionId))
        {
            SwitchToSection(initialSectionId);
        }
        else if (sectionLookup.Count > 0)
        {
            // Show first section by default
            SwitchToSection(sectionLookup.Keys.First());
        }
    }
    
    /// <summary>
    /// Switch to a specific content section
    /// </summary>
    public void SwitchToSection(string sectionId)
    {
        if (string.IsNullOrEmpty(sectionId))
        {
            LogError("Section ID is empty!");
            return;
        }
        
        if (!sectionLookup.ContainsKey(sectionId))
        {
            LogError($"Section not found: {sectionId}");
            return;
        }
        
        if (debugMode)
        {
            Log($"Switching to section: {sectionId}");
        }
        
        // Show only the target section, hide all others
        foreach (var kvp in sectionLookup)
        {
            if (kvp.Value != null)
            {
                kvp.Value.SetActive(kvp.Key == sectionId);
            }
        }
        
        currentActiveSectionId = sectionId;
        
        // Update state handler
        if (stateHandler != null)
        {
            stateHandler.SwitchToTab(sectionId);
        }
    }
    
    /// <summary>
    /// Get the currently active section ID
    /// </summary>
    public string GetCurrentSectionId()
    {
        return currentActiveSectionId;
    }
    
    /// <summary>
    /// Get all registered section IDs
    /// </summary>
    public string[] GetAllSectionIds()
    {
        return sectionLookup.Keys.ToArray();
    }
    
    private void Log(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[ScrollViewTabManager] {gameObject.name}: {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ScrollViewTabManager] {gameObject.name}: {message}");
    }
    
    void OnValidate()
    {
        // Auto-sync sectionIds array size with contentSections
        if (sectionIds.Length != contentSections.Count)
        {
            System.Array.Resize(ref sectionIds, contentSections.Count);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Generate Section IDs")]
    private void AutoGenerateSectionIds()
    {
        for (int i = 0; i < contentSections.Count && i < sectionIds.Length; i++)
        {
            if (contentSections[i] != null && string.IsNullOrEmpty(sectionIds[i]))
            {
                sectionIds[i] = contentSections[i].name;
            }
        }
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ScrollViewTabManager] Auto-generated {contentSections.Count} section IDs");
    }
    
    [ContextMenu("Auto-Discover ScrollView Sections")]
    private void AutoDiscoverScrollViews()
    {
        contentSections.Clear();
        
        // Find all ScrollRect components in children
        UnityEngine.UI.ScrollRect[] scrollRects = GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true);
        
        foreach (var scrollRect in scrollRects)
        {
            contentSections.Add(scrollRect.gameObject);
        }
        
        // Resize section IDs array
        System.Array.Resize(ref sectionIds, contentSections.Count);
        
        // Auto-generate IDs
        AutoGenerateSectionIds();
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ScrollViewTabManager] Auto-discovered {contentSections.Count} ScrollView sections");
    }
#endif
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom editor for TabManager component
/// Provides dropdown for preEnableTabId when tabs and animator tab IDs match
/// </summary>
[CustomEditor(typeof(TabManager))]
[CanEditMultipleObjects]
public class TabManagerEditor : Editor
{
    private SerializedProperty tabs;
    private SerializedProperty preEnableTabId;
    private SerializedProperty stateHandlerType;
    private SerializedProperty tabAnimator;
    private SerializedProperty animatorTabIds;
    private SerializedProperty debugMode;
    
    private void OnEnable()
    {
        tabs = serializedObject.FindProperty("tabs");
        preEnableTabId = serializedObject.FindProperty("preEnableTabId");
        stateHandlerType = serializedObject.FindProperty("stateHandlerType");
        tabAnimator = serializedObject.FindProperty("tabAnimator");
        animatorTabIds = serializedObject.FindProperty("animatorTabIds");
        debugMode = serializedObject.FindProperty("debugMode");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        TabManager tabManager = (TabManager)target;
        
        // Draw default inspector fields
        EditorGUILayout.PropertyField(tabs);
        EditorGUILayout.Space();
        
        // Tab Initialization header
        EditorGUILayout.LabelField("Tab Initialization", EditorStyles.boldLabel);
        
        // Check if we can show dropdown
        bool canShowDropdown = CanShowDropdown(tabManager);
        
        if (canShowDropdown)
        {
            // Show dropdown for preEnableTabId
            DrawPreEnableTabDropdown(tabManager);
        }
        else
        {
            // Show regular text field
            EditorGUILayout.PropertyField(preEnableTabId, 
                new GUIContent("Pre Enable Tab ID", 
                    "Tab ID to pre-enable when parent panel is shown. Leave empty to keep all tabs hidden initially."));
        }
        
        EditorGUILayout.Space();
        
        // Draw remaining fields
        EditorGUILayout.PropertyField(stateHandlerType);
        EditorGUILayout.PropertyField(tabAnimator);
        EditorGUILayout.PropertyField(animatorTabIds);
        EditorGUILayout.PropertyField(debugMode);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// Check if we can show dropdown for preEnableTabId
    /// Returns true if tabs list and animatorTabIds match (same IDs)
    /// Both lists must be populated and contain the same IDs
    /// </summary>
    private bool CanShowDropdown(TabManager tabManager)
    {
        // Get tab IDs from tabs list
        List<string> tabIdsFromTabs = GetTabIdsFromTabsList(tabManager);
        
        // Get animator tab IDs
        List<string> animatorIds = new List<string>();
        if (animatorTabIds.arraySize > 0)
        {
            for (int i = 0; i < animatorTabIds.arraySize; i++)
            {
                string id = animatorTabIds.GetArrayElementAtIndex(i).stringValue;
                if (!string.IsNullOrEmpty(id))
                {
                    animatorIds.Add(id);
                }
            }
        }
        
        // Both lists must be populated
        if (tabIdsFromTabs.Count == 0 || animatorIds.Count == 0)
        {
            return false;
        }
        
        // Check if they match (same count and same IDs)
        if (tabIdsFromTabs.Count != animatorIds.Count)
        {
            return false;
        }
        
        // Check if all IDs match (order doesn't matter)
        foreach (string tabId in tabIdsFromTabs)
        {
            if (!animatorIds.Contains(tabId))
            {
                return false;
            }
        }
        
        // Also check reverse to ensure complete match
        foreach (string animatorId in animatorIds)
        {
            if (!tabIdsFromTabs.Contains(animatorId))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Get tab IDs from the tabs list (using TabComponent or PanelId)
    /// </summary>
    private List<string> GetTabIdsFromTabsList(TabManager tabManager)
    {
        List<string> tabIds = new List<string>();
        
        // Use serialized property to get tabs list
        if (tabs != null && tabs.isArray)
        {
            for (int i = 0; i < tabs.arraySize; i++)
            {
                var tabProperty = tabs.GetArrayElementAtIndex(i);
                var tab = tabProperty.objectReferenceValue as UIPanel;
                
                if (tab != null)
                {
                    string tabId = GetTabId(tab);
                    if (!string.IsNullOrEmpty(tabId))
                    {
                        tabIds.Add(tabId);
                    }
                }
            }
        }
        
        return tabIds;
    }
    
    /// <summary>
    /// Get tab ID from a UIPanel (same logic as TabManager.GetTabId)
    /// </summary>
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
    
    /// <summary>
    /// Draw dropdown for preEnableTabId
    /// Assumes CanShowDropdown() has already verified that tabs and animatorTabIds match
    /// </summary>
    private void DrawPreEnableTabDropdown(TabManager tabManager)
    {
        // Get available tab IDs from animatorTabIds (since they match tabs list)
        List<string> availableTabIds = new List<string>();
        for (int i = 0; i < animatorTabIds.arraySize; i++)
        {
            string id = animatorTabIds.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrEmpty(id))
            {
                availableTabIds.Add(id);
            }
        }
        
        // Add "None" option at the beginning
        List<string> options = new List<string> { "(None)" };
        options.AddRange(availableTabIds);
        
        // Find current selection index
        string currentValue = preEnableTabId.stringValue;
        int currentIndex = 0; // Default to "(None)"
        
        if (!string.IsNullOrEmpty(currentValue))
        {
            int foundIndex = options.IndexOf(currentValue);
            if (foundIndex >= 0)
            {
                currentIndex = foundIndex;
            }
            else if (availableTabIds.Contains(currentValue))
            {
                // Value exists but not in dropdown - add it
                availableTabIds.Add(currentValue);
                options = new List<string> { "(None)" };
                options.AddRange(availableTabIds);
                currentIndex = options.IndexOf(currentValue);
            }
        }
        
        // Draw dropdown
        int newIndex = EditorGUILayout.Popup(
            new GUIContent("Pre Enable Tab ID", 
                "Tab ID to pre-enable when parent panel is shown. Select '(None)' to keep all tabs hidden initially."),
            currentIndex, 
            options.ToArray());
        
        if (newIndex != currentIndex)
        {
            if (newIndex == 0)
            {
                // "(None)" selected
                preEnableTabId.stringValue = "";
            }
            else
            {
                // Tab ID selected
                preEnableTabId.stringValue = options[newIndex];
            }
            EditorUtility.SetDirty(target);
        }
    }
}


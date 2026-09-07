using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom editor for ScrollViewTabManager component
/// Provides dropdown for initialSectionId and improved section management
/// </summary>
[CustomEditor(typeof(ScrollViewTabManager))]
[CanEditMultipleObjects]
public class ScrollViewTabManagerEditor : Editor
{
    private SerializedProperty contentSections;
    private SerializedProperty sectionIds;
    private SerializedProperty initialSectionId;
    private SerializedProperty stateHandlerType;
    private SerializedProperty tabAnimator;
    private SerializedProperty animatorSectionIds;
    private SerializedProperty debugMode;
    
    private void OnEnable()
    {
        contentSections = serializedObject.FindProperty("contentSections");
        sectionIds = serializedObject.FindProperty("sectionIds");
        initialSectionId = serializedObject.FindProperty("initialSectionId");
        stateHandlerType = serializedObject.FindProperty("stateHandlerType");
        tabAnimator = serializedObject.FindProperty("tabAnimator");
        animatorSectionIds = serializedObject.FindProperty("animatorSectionIds");
        debugMode = serializedObject.FindProperty("debugMode");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Content Sections
        EditorGUILayout.LabelField("Content Sections", EditorStyles.boldLabel);
        DrawContentSectionsList();
        EditorGUILayout.Space();
        
        // Tab Initialization
        EditorGUILayout.LabelField("Tab Initialization", EditorStyles.boldLabel);
        DrawInitialSectionDropdown();
        EditorGUILayout.Space();
        
        // Tab State Management
        EditorGUILayout.PropertyField(stateHandlerType);
        EditorGUILayout.PropertyField(tabAnimator);
        EditorGUILayout.PropertyField(animatorSectionIds);
        EditorGUILayout.Space();
        
        // Debug
        EditorGUILayout.PropertyField(debugMode);
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawContentSectionsList()
    {
        EditorGUILayout.PropertyField(contentSections);
        
        // Show section IDs inline with content sections
        if (contentSections.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            int size = contentSections.arraySize;
            
            // Ensure sectionIds array matches size
            if (sectionIds.arraySize != size)
            {
                sectionIds.arraySize = size;
            }
            
            for (int i = 0; i < size; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                var sectionProp = contentSections.GetArrayElementAtIndex(i);
                var idProp = sectionIds.GetArrayElementAtIndex(i);
                
                EditorGUILayout.PropertyField(sectionProp, GUIContent.none, GUILayout.Width(150));
                EditorGUILayout.LabelField("ID:", GUILayout.Width(25));
                EditorGUILayout.PropertyField(idProp, GUIContent.none);
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            
            // Helper buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Generate IDs"))
            {
                AutoGenerateSectionIds();
            }
            if (GUILayout.Button("Auto-Discover ScrollViews"))
            {
                AutoDiscoverScrollViews();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawInitialSectionDropdown()
    {
        List<string> availableSectionIds = GetAvailableSectionIds();
        
        if (availableSectionIds.Count == 0)
        {
            EditorGUILayout.PropertyField(initialSectionId, 
                new GUIContent("Initial Section ID", 
                    "Section ID to show initially. Leave empty to show all sections."));
            return;
        }
        
        // Add "First Section" option
        List<string> options = new List<string> { "(First Section)" };
        options.AddRange(availableSectionIds);
        
        // Find current selection
        string currentValue = initialSectionId.stringValue;
        int currentIndex = 0;
        
        if (!string.IsNullOrEmpty(currentValue))
        {
            int foundIndex = options.IndexOf(currentValue);
            if (foundIndex >= 0)
            {
                currentIndex = foundIndex;
            }
        }
        
        // Draw dropdown
        int newIndex = EditorGUILayout.Popup(
            new GUIContent("Initial Section ID", 
                "Section ID to show initially. Select '(First Section)' to show the first section."),
            currentIndex,
            options.ToArray());
        
        if (newIndex != currentIndex)
        {
            if (newIndex == 0)
            {
                initialSectionId.stringValue = "";
            }
            else
            {
                initialSectionId.stringValue = options[newIndex];
            }
            EditorUtility.SetDirty(target);
        }
    }
    
    private List<string> GetAvailableSectionIds()
    {
        List<string> ids = new List<string>();
        
        for (int i = 0; i < sectionIds.arraySize; i++)
        {
            string id = sectionIds.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrEmpty(id))
            {
                ids.Add(id);
            }
        }
        
        return ids;
    }
    
    private void AutoGenerateSectionIds()
    {
        for (int i = 0; i < contentSections.arraySize && i < sectionIds.arraySize; i++)
        {
            var sectionProp = contentSections.GetArrayElementAtIndex(i);
            var idProp = sectionIds.GetArrayElementAtIndex(i);
            
            GameObject section = sectionProp.objectReferenceValue as GameObject;
            if (section != null && string.IsNullOrEmpty(idProp.stringValue))
            {
                idProp.stringValue = section.name;
            }
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
    
    private void AutoDiscoverScrollViews()
    {
        ScrollViewTabManager manager = (ScrollViewTabManager)target;
        
        // Use reflection to access private method
        var method = typeof(ScrollViewTabManager).GetMethod("AutoDiscoverScrollViews", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(manager, null);
            serializedObject.Update();
        }
    }
}

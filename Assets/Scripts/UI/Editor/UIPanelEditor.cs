using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for UIPanel component
/// Conditionally shows Background Click Sound only for Popup behavior type
/// Supports derived classes (e.g., NavigationPanel) via editorForChildClasses=true
/// </summary>
[CustomEditor(typeof(UIPanel), true)]
[CanEditMultipleObjects]
public class UIPanelEditor : Editor
{
    private string scenarioPresetName = "";
    private SerializedProperty panelId;
    private SerializedProperty panelTitle;
    private SerializedProperty backgroundImage;
    private SerializedProperty backgroundColor;
    private SerializedProperty behaviorType;
    private SerializedProperty closeOnBackgroundClick;
    private SerializedProperty disableUnderlyingPanels;
    private SerializedProperty backgroundClickSound;
    private SerializedProperty animateShow;
    private SerializedProperty animateHide;
    private SerializedProperty animationDuration;
    private SerializedProperty gameSection;
    private SerializedProperty isSectionIndependent;
    private SerializedProperty onShown;
    private SerializedProperty onHidden;
    private SerializedProperty onDataReceived;
    private SerializedProperty onAnyPanelOpened;
    private SerializedProperty onAnyPanelClosed;
    
    private void OnEnable()
    {
        // Get all serialized properties
        panelId = serializedObject.FindProperty("panelId");
        panelTitle = serializedObject.FindProperty("panelTitle");
        backgroundImage = serializedObject.FindProperty("backgroundImage");
        backgroundColor = serializedObject.FindProperty("backgroundColor");
        behaviorType = serializedObject.FindProperty("behaviorType");
        closeOnBackgroundClick = serializedObject.FindProperty("closeOnBackgroundClick");
        disableUnderlyingPanels = serializedObject.FindProperty("disableUnderlyingPanels");
        backgroundClickSound = serializedObject.FindProperty("backgroundClickSound");
        animateShow = serializedObject.FindProperty("animateShow");
        animateHide = serializedObject.FindProperty("animateHide");
        animationDuration = serializedObject.FindProperty("animationDuration");
        gameSection = serializedObject.FindProperty("gameSection");
        isSectionIndependent = serializedObject.FindProperty("isSectionIndependent");
        onShown = serializedObject.FindProperty("OnShown");
        onHidden = serializedObject.FindProperty("OnHidden");
        onDataReceived = serializedObject.FindProperty("OnDataReceived");
        onAnyPanelOpened = serializedObject.FindProperty("OnAnyPanelOpened");
        onAnyPanelClosed = serializedObject.FindProperty("OnAnyPanelClosed");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Panel Identity - use [Header] attribute
        EditorGUILayout.PropertyField(panelId);
        EditorGUILayout.Space();
        
        // Section Assignment - moved to top as it's fundamental
        EditorGUILayout.PropertyField(gameSection);
        EditorGUILayout.PropertyField(isSectionIndependent);
        
        // Get current values after section assignment fields
        bool isSectionIndependentValue = isSectionIndependent.boolValue;
        
        // Show warning if section-independent but has section assigned
        if (isSectionIndependentValue && gameSection.enumValueIndex != (int)GameSection.None)
        {
            EditorGUILayout.HelpBox(
                "Section-independent panels should have section set to 'None'.",
                MessageType.Warning);
        }
        
        EditorGUILayout.Space();
        
        // Display Settings - use [Header] attribute
        EditorGUILayout.PropertyField(panelTitle);
        EditorGUILayout.PropertyField(backgroundImage);
        EditorGUILayout.PropertyField(backgroundColor);
        EditorGUILayout.Space();
        
        // Behavior - use [Header] attribute
        EditorGUILayout.PropertyField(behaviorType);
        
        // Get behavior type after field is drawn (user might have changed it)
        PanelBehaviorType currentBehaviorType = (PanelBehaviorType)behaviorType.enumValueIndex;
        bool isBaseType = currentBehaviorType == PanelBehaviorType.Base && !isSectionIndependentValue;
        
        // Show info if base panel
        if (isBaseType)
        {
            EditorGUILayout.HelpBox(
                $"This is the base panel for {((GameSection)gameSection.enumValueIndex)} section. " +
                "Base panels are always enabled when their section is active.",
                MessageType.Info);
        }
        
        // Hide irrelevant fields for Base type
        if (!isBaseType)
        {
            EditorGUILayout.PropertyField(closeOnBackgroundClick);
            
            // Only show background click sound if behavior type is Popup
            if (currentBehaviorType == PanelBehaviorType.Popup)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(backgroundClickSound);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(disableUnderlyingPanels);
        }
        else
        {
            // Base panels don't need these options
            // Keep values but hide from UI
        }
        
        EditorGUILayout.Space();
        
        // Animation - use [Header] attribute
        // Hide animation options for Base type
        if (!isBaseType)
        {
            EditorGUILayout.PropertyField(animateShow);
            EditorGUILayout.PropertyField(animateHide);
            EditorGUILayout.PropertyField(animationDuration);
        }
        EditorGUILayout.Space();
        
        // Events - use [Header] attribute
        EditorGUILayout.PropertyField(onShown);
        EditorGUILayout.PropertyField(onHidden);
        EditorGUILayout.PropertyField(onDataReceived);
        EditorGUILayout.Space();
        
        // Global Events - use [Header] attribute
        EditorGUILayout.PropertyField(onAnyPanelOpened);
        EditorGUILayout.PropertyField(onAnyPanelClosed);
        
        EditorGUILayout.Space();
        
        // Scenario Preset Management (Editor Only)
        EditorGUILayout.LabelField("Scenario Preset Management", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Configure component states in the panel, then save as a scenario preset. " +
            "Presets can be applied at runtime using ConfigureForScenario().",
            MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        scenarioPresetName = EditorGUILayout.TextField("Scenario ID", scenarioPresetName);
        if (GUILayout.Button("Save Preset", GUILayout.Width(100)))
        {
            SaveCurrentStateAsPreset();
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Reset to Default Configuration"))
        {
            ResetToDefault();
        }
        
        EditorGUILayout.Space();
        
        // Draw remaining properties from derived classes (e.g., NavigationPanel's [SerializeField] fields)
        // Exclude properties we've already drawn manually to avoid duplicates
        DrawPropertiesExcluding(serializedObject, 
            "m_Script", // Exclude the script reference
            "panelId",
            "panelTitle",
            "backgroundImage",
            "backgroundColor",
            "behaviorType",
            "closeOnBackgroundClick",
            "disableUnderlyingPanels",
            "backgroundClickSound",
            "animateShow",
            "animateHide",
            "animationDuration",
            "gameSection",
            "isSectionIndependent",
            "OnShown",
            "OnHidden",
            "OnDataReceived",
            "OnAnyPanelOpened",
            "OnAnyPanelClosed"
        );
        
        serializedObject.ApplyModifiedProperties();
    }
    
    /// <summary>
    /// Save current component states as a scenario preset
    /// </summary>
    private void SaveCurrentStateAsPreset()
    {
        if (string.IsNullOrEmpty(scenarioPresetName))
        {
            EditorUtility.DisplayDialog("Save Scenario Preset", 
                "Please enter a scenario ID.", "OK");
            return;
        }
        
        if (targets.Length > 1)
        {
            EditorUtility.DisplayDialog("Save Scenario Preset", 
                "Please select only one panel to save a scenario preset.", "OK");
            return;
        }
        
        UIPanel panel = (UIPanel)target;
        
        // Use reflection to call SaveScenarioPreset (it's public but Editor-only in practice)
        var method = typeof(UIPanel).GetMethod("SaveScenarioPreset", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(panel, new object[] { scenarioPresetName });
            EditorUtility.SetDirty(panel);
            Debug.Log($"[UIPanelEditor] Saved scenario preset '{scenarioPresetName}' for panel '{panel.PanelId}'");
        }
        else
        {
            Debug.LogError("[UIPanelEditor] Could not find SaveScenarioPreset method");
        }
    }
    
    /// <summary>
    /// Reset panel to default component configuration
    /// </summary>
    private void ResetToDefault()
    {
        if (targets.Length > 1)
        {
            EditorUtility.DisplayDialog("Reset Configuration", 
                "Please select only one panel to reset.", "OK");
            return;
        }
        
        UIPanel panel = (UIPanel)target;
        
        // Use reflection to call ResetToDefaultConfiguration
        var method = typeof(UIPanel).GetMethod("ResetToDefaultConfiguration", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(panel, null);
            EditorUtility.SetDirty(panel);
            Debug.Log($"[UIPanelEditor] Reset panel '{panel.PanelId}' to default configuration");
        }
        else
        {
            Debug.LogError("[UIPanelEditor] Could not find ResetToDefaultConfiguration method");
        }
    }
}


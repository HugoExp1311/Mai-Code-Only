using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for UIPanelManager component
/// Provides section management UI and panel overview
/// </summary>
[CustomEditor(typeof(UIPanelManager))]
[CanEditMultipleObjects]
public class UIPanelManagerEditor : Editor
{
    private SerializedProperty debugMode;
    private SerializedProperty initialSection;
    private SerializedProperty useTransitionPanel;
    private SerializedProperty transitionPanelId;
    private SerializedProperty transitionDuration;
    private SerializedProperty mainMenuBasePanelId;
    private SerializedProperty inGameBasePanelId;
    private SerializedProperty minigameBasePanelId;
    private SerializedProperty simulationBasePanelId;
    private SerializedProperty onAnyPanelOpened;
    private SerializedProperty onAnyPanelClosed;
    private SerializedProperty onSectionSwitched;
    
    private bool showSectionOverview = true;
    
    private void OnEnable()
    {
        debugMode = serializedObject.FindProperty("debugMode");
        initialSection = serializedObject.FindProperty("initialSection");
        useTransitionPanel = serializedObject.FindProperty("useTransitionPanel");
        transitionPanelId = serializedObject.FindProperty("transitionPanelId");
        transitionDuration = serializedObject.FindProperty("transitionDuration");
        mainMenuBasePanelId = serializedObject.FindProperty("mainMenuBasePanelId");
        inGameBasePanelId = serializedObject.FindProperty("inGameBasePanelId");
        minigameBasePanelId = serializedObject.FindProperty("minigameBasePanelId");
        simulationBasePanelId = serializedObject.FindProperty("simulationBasePanelId");
        onAnyPanelOpened = serializedObject.FindProperty("OnAnyPanelOpened");
        onAnyPanelClosed = serializedObject.FindProperty("OnAnyPanelClosed");
        onSectionSwitched = serializedObject.FindProperty("OnSectionSwitched");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        UIPanelManager manager = (UIPanelManager)target;
        
        // Configuration - header comes from [Header] attribute
        EditorGUILayout.PropertyField(debugMode);
        EditorGUILayout.Space();
        
        // Section Management - header comes from [Header] attribute
        EditorGUILayout.PropertyField(initialSection, 
            new GUIContent("Initial Section", 
                "Initial section to enable when game starts"));
        EditorGUILayout.Space();
        
        // Section Transitions - header comes from [Header] attribute
        EditorGUILayout.PropertyField(useTransitionPanel);
        EditorGUILayout.PropertyField(transitionPanelId,
            new GUIContent("Transition Panel ID",
                "Panel ID for transition panel (should be section-independent)"));
        EditorGUILayout.PropertyField(transitionDuration);
        EditorGUILayout.Space();
        
        // Base Panels per Section - header comes from [Header] attribute
        EditorGUILayout.PropertyField(mainMenuBasePanelId,
            new GUIContent("Main Menu Base Panel", 
                "Base panel ID for Main Menu section"));
        EditorGUILayout.PropertyField(inGameBasePanelId, 
            new GUIContent("In Game Base Panel", 
                "Base panel ID for In Game section"));
        EditorGUILayout.PropertyField(minigameBasePanelId, 
            new GUIContent("Minigame Base Panel", 
                "Base panel ID for Minigame section"));
        EditorGUILayout.PropertyField(simulationBasePanelId, 
            new GUIContent("Simulation Base Panel", 
                "Base panel ID for Simulation section"));
        EditorGUILayout.Space();
        
        // Global Events - header comes from [Header] attribute
        EditorGUILayout.PropertyField(onAnyPanelOpened, 
            new GUIContent("On Any Panel Opened", 
                "Invoked when any panel is opened (includes panel ID)"));
        EditorGUILayout.PropertyField(onAnyPanelClosed, 
            new GUIContent("On Any Panel Closed", 
                "Invoked when any panel is closed (includes panel ID)"));
        EditorGUILayout.PropertyField(onSectionSwitched, 
            new GUIContent("On Section Switched", 
                "Invoked when switching between sections (includes old and new section)"));
        EditorGUILayout.Space();
        
        // Section Overview (Runtime only)
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Section Overview", EditorStyles.boldLabel);
            
            showSectionOverview = EditorGUILayout.Foldout(showSectionOverview, "Show Section Details", true);
            
            if (showSectionOverview)
            {
                DrawSectionOverview(manager);
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Section overview will be available at runtime.",
                MessageType.Info);
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void DrawSectionOverview(UIPanelManager manager)
    {
        EditorGUI.indentLevel++;
        
        // Current section
        GameSection currentSection = manager.GetCurrentSection();
        EditorGUILayout.LabelField($"Current Section: {currentSection}", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Show info for each section
        foreach (GameSection section in System.Enum.GetValues(typeof(GameSection)))
        {
            if (section == GameSection.None) continue;
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            bool isActive = manager.IsSectionActive(section);
            EditorGUILayout.LabelField($"{section} Section", 
                isActive ? EditorStyles.boldLabel : EditorStyles.label);
            
            var basePanel = manager.GetBasePanel(section);
            if (basePanel != null)
            {
                EditorGUILayout.LabelField($"  Base Panel: {basePanel.PanelId}");
                EditorGUILayout.LabelField($"  Base Panel Visible: {basePanel.IsVisible}");
            }
            else
            {
                EditorGUILayout.LabelField("  Base Panel: Not Assigned", EditorStyles.miniLabel);
            }
            
            var childPanels = manager.GetChildPanels(section);
            EditorGUILayout.LabelField($"  Child Panels: {childPanels.Count}");
            
            var allPanels = manager.GetPanelsInSection(section);
            int visibleCount = allPanels.Count(p => p.IsVisible);
            EditorGUILayout.LabelField($"  Total Panels: {allPanels.Count} (Visible: {visibleCount})");
            
            EditorGUILayout.EndVertical();
        }
        
        // Section-independent panels
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Section-Independent Panels", EditorStyles.boldLabel);
        var independentPanels = manager.GetSectionIndependentPanels();
        EditorGUILayout.LabelField($"  Count: {independentPanels.Count}");
        foreach (var panel in independentPanels)
        {
            if (panel != null)
            {
                EditorGUILayout.LabelField($"    - {panel.PanelId} (Visible: {panel.IsVisible})", EditorStyles.miniLabel);
            }
        }
        if (independentPanels.Count == 0)
        {
            EditorGUILayout.LabelField("  No section-independent panels found", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
        
        EditorGUI.indentLevel--;
    }
}


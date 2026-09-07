using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Analyzes the current scene to show all UI panel components and their relationships
/// </summary>
public class UIPanelAnalyzer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showHierarchy = true;
    private bool showDetails = true;
    private bool showButtons = true;
    private bool showExpandedPanels = false;
    private Dictionary<string, bool> expandedPanels = new Dictionary<string, bool>();

    [MenuItem("Tools/UI Panel Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<UIPanelAnalyzer>("UI Panel Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI Panel Scene Analysis", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Options
        EditorGUILayout.BeginHorizontal();
        showHierarchy = EditorGUILayout.Toggle("Show Hierarchy", showHierarchy);
        showDetails = EditorGUILayout.Toggle("Show Panel Details", showDetails);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        showButtons = EditorGUILayout.Toggle("Show Buttons", showButtons);
        showExpandedPanels = EditorGUILayout.Toggle("Expand All Panels", showExpandedPanels);
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Analyze Current Scene"))
        {
            AnalyzeScene();
        }

        if (GUILayout.Button("Refresh Display"))
        {
            Repaint();
        }

        GUILayout.Space(10);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Find and display all UI components
        var uiPanels = FindObjectsByType<UIPanel>(FindObjectsSortMode.None);
        var regularButtons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        
        // Summary
        GUILayout.Label("Summary", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"UIPanel components: {uiPanels.Length}");
        GUILayout.Label($"Regular Button components: {regularButtons.Length}");
        GUILayout.Label($"Total UI components: {uiPanels.Length + regularButtons.Length}");
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // UIPanel details
        if (uiPanels.Length > 0)
        {
            GUILayout.Label($"UIPanel Components ({uiPanels.Length})", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var panel in uiPanels.OrderBy(p => p.name))
            {
                DrawUIPanelInfo(panel);
            }
            EditorGUILayout.EndVertical();
        }
        
        GUILayout.Space(10);
        
        // Regular buttons
        if (regularButtons.Length > 0)
        {
            GUILayout.Label($"Regular Buttons ({regularButtons.Length})", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var button in regularButtons.OrderBy(b => b.name))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(button.name, button.gameObject, typeof(GameObject), true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        
        GUILayout.Space(10);
        
        // GameObjects with "Panel" in name but no UIPanel component
        var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var panelGameObjects = allGameObjects.Where(go => 
            go.name.ToLower().Contains("panel") && 
            go.GetComponent<UIPanel>() == null).ToArray();
            
        if (panelGameObjects.Length > 0)
        {
            GUILayout.Label($"GameObjects with 'Panel' in name (No UIPanel component) ({panelGameObjects.Length})", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var go in panelGameObjects.OrderBy(go => go.name))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(go.name, go, typeof(GameObject), true);
                if (GUILayout.Button("Add UIPanel", GUILayout.Width(100)))
                {
                    go.AddComponent<UIPanel>();
                    EditorUtility.SetDirty(go);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawUIPanelInfo(UIPanel panel)
    {
        // Panel header
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        
        // Foldout for expanding panel details
        string panelKey = "UI_" + panel.GetInstanceID();
        bool isExpanded = showExpandedPanels;
        if (!showExpandedPanels)
        {
            if (!expandedPanels.ContainsKey(panelKey))
                expandedPanels[panelKey] = false;
            isExpanded = expandedPanels[panelKey] = EditorGUILayout.Foldout(expandedPanels[panelKey], "");
        }
        
        // Object field
        EditorGUILayout.ObjectField(panel.name, panel.gameObject, typeof(GameObject), true);
        
        if (showDetails)
        {
            // NOTE: Old system properties removed - new system uses different properties
            GUILayout.Label($"ID: {panel.PanelId}", GUILayout.Width(100));
            GUILayout.Label($"Behavior: {panel.BehaviorType}", GUILayout.Width(100));
            GUILayout.Label($"Section: {panel.GameSection}", GUILayout.Width(80));
        }
        
        EditorGUILayout.EndHorizontal();
        
        // Hierarchy path
        if (showHierarchy && isExpanded)
        {
            string path = GetGameObjectPath(panel.gameObject);
            EditorGUILayout.LabelField("Path: " + path, EditorStyles.miniLabel);
        }
        
        // NOTE: ManagedButtons removed - new system doesn't auto-manage buttons
        // Buttons are now managed via UIPanelButtonAction components
        /*
        if (showButtons && isExpanded && panel.ManagedButtons.Count > 0)
        {
            EditorGUI.indentLevel++;
            GUILayout.Label($"Managed Buttons ({panel.ManagedButtons.Count}):", EditorStyles.miniLabel);
            
            foreach (var button in panel.ManagedButtons)
            {
                if (button != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField("  • " + button.name, button.gameObject, typeof(GameObject), true);
                    GUILayout.Label($"Type: {button.ButtonType}", GUILayout.Width(100));
                    GUILayout.Label($"Behavior: {button.Behavior}", GUILayout.Width(120));
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel--;
        }
        */
        
        EditorGUILayout.EndVertical();
    }

    private void DrawButtonInfo(GameObject buttonObject, string type, string behavior)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Object field
        EditorGUILayout.ObjectField(buttonObject.name, buttonObject, typeof(GameObject), true);
        
        if (showDetails)
        {
            GUILayout.Label($"Type: {type}", GUILayout.Width(100));
            GUILayout.Label($"Behavior: {behavior}", GUILayout.Width(120));
        }
        
        // Hierarchy path
        if (showHierarchy)
        {
            string path = GetGameObjectPath(buttonObject);
            GUILayout.Label(path, EditorStyles.miniLabel);
        }
        
        EditorGUILayout.EndHorizontal();
    }

    private string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }

    private void AnalyzeScene()
    {
        Debug.Log("🔍 Starting UI Panel Analysis...");
        
        var uiPanels = FindObjectsByType<UIPanel>(FindObjectsSortMode.None);
        var regularButtons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        
        Debug.Log($"📊 Analysis Results:");
        Debug.Log($"  • UIPanel components: {uiPanels.Length}");
        Debug.Log($"  • Regular Button components: {regularButtons.Length}");
        
        Debug.Log($"📝 UIPanel List:");
        foreach (var panel in uiPanels.OrderBy(p => p.name))
        {
            Debug.Log($"  • {panel.name} - ID: {panel.PanelId}, Behavior: {panel.BehaviorType}, Section: {panel.GameSection}");
        }
        
        // Find GameObjects with "Panel" in name
        var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var panelGameObjects = allGameObjects.Where(go => 
            go.name.ToLower().Contains("panel") && 
            go.GetComponent<UIPanel>() == null).ToArray();
            
        if (panelGameObjects.Length > 0)
        {
            Debug.Log($"⚠️  GameObjects with 'Panel' in name but no UIPanel component ({panelGameObjects.Length}):");
            foreach (var go in panelGameObjects.OrderBy(go => go.name))
            {
                Debug.Log($"  • {go.name} - Path: {GetGameObjectPath(go)}");
            }
        }
        
        Repaint();
    }
}
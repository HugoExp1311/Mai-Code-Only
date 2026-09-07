using UnityEngine;
using UnityEditor;
using Base.Localization;
using Base.Localization.Data;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Main editor window for managing localization in the scene
    /// </summary>
    public class LocalizationEditorWindow : EditorWindow
    {
        private enum Tab
        {
            Overview,
            Components,
            KeyBrowser,
            Validation,
            TemplateValidation,
            Settings
        }
        
        private Tab currentTab = Tab.Overview;
        private Vector2 scrollPosition;
        private string searchFilter = "";
        
        // Component list
        private List<LocalizedText> allComponents = new List<LocalizedText>();
        private Dictionary<string, int> keyUsageCount = new Dictionary<string, int>();
        
        // Validation
        private List<ValidationResult> validationResults = new List<ValidationResult>();
        
        [MenuItem("Localization/Localization Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationEditorWindow>("Localization Manager");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        private void OnEnable()
        {
            RefreshData();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.Space();
            
            switch (currentTab)
            {
                case Tab.Overview:
                    DrawOverviewTab();
                    break;
                case Tab.Components:
                    DrawComponentsTab();
                    break;
                case Tab.KeyBrowser:
                    DrawKeyBrowserTab();
                    break;
                case Tab.Validation:
                    DrawValidationTab();
                    break;
                case Tab.TemplateValidation:
                    DrawTemplateValidationTab();
                    break;
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Toggle(currentTab == Tab.Overview, "Overview", EditorStyles.toolbarButton))
                currentTab = Tab.Overview;
            
            if (GUILayout.Toggle(currentTab == Tab.Components, "Components", EditorStyles.toolbarButton))
                currentTab = Tab.Components;
            
            if (GUILayout.Toggle(currentTab == Tab.KeyBrowser, "Key Browser", EditorStyles.toolbarButton))
                currentTab = Tab.KeyBrowser;
            
            if (GUILayout.Toggle(currentTab == Tab.Validation, "Validation", EditorStyles.toolbarButton))
                currentTab = Tab.Validation;
            
            if (GUILayout.Toggle(currentTab == Tab.TemplateValidation, "Templates", EditorStyles.toolbarButton))
                currentTab = Tab.TemplateValidation;
            
            if (GUILayout.Toggle(currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
                currentTab = Tab.Settings;
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshData();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("Localization Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Statistics
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Components: {allComponents.Count}");
            EditorGUILayout.LabelField($"Unique Keys: {keyUsageCount.Count}");
            EditorGUILayout.LabelField($"Validation Issues: {validationResults.Count}");
            
            var unmanagedComponents = SceneTextScanner.FindUnmanagedTextComponents();
            EditorGUILayout.LabelField($"Unmanaged TextMeshPro: {unmanagedComponents.Count}");
            
            EditorGUILayout.Space();
            
            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Scan for Unmanaged Components"))
            {
                SceneTextScanner.MenuFindUnmanagedComponents();
                RefreshData();
            }
            
            if (GUILayout.Button("Validate All Components"))
            {
                validationResults = SceneTextScanner.ValidateAllLocalizedText();
                currentTab = Tab.Validation;
            }
            
            if (GUILayout.Button("Generate Missing Keys"))
            {
                SceneTextScanner.MenuGenerateMissingKeys();
                RefreshData();
            }
            
            if (GUILayout.Button("Refresh All Localization"))
            {
                if (LocalizationManager.Instance != null)
                {
                    LocalizationManager.Instance.RefreshAll();
                }
            }
        }
        
        private void DrawComponentsTab()
        {
            EditorGUILayout.LabelField("LocalizedText Components", EditorStyles.boldLabel);
            
            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Component list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var filteredComponents = allComponents.Where(c => 
                string.IsNullOrEmpty(searchFilter) || 
                c.gameObject.name.ToLower().Contains(searchFilter.ToLower()) ||
                c.Key.ToLower().Contains(searchFilter.ToLower())
            ).ToList();
            
            foreach (var component in filteredComponents)
            {
                if (component == null) continue;
                
                DrawComponentRow(component);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Showing {filteredComponents.Count} of {allComponents.Count} components");
        }
        
        private void DrawComponentRow(LocalizedText component)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // GameObject name
            EditorGUILayout.LabelField(component.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(200));
            
            // Status indicator
            var settings = component.GetSettings();
            if (settings.IsDirect)
            {
                EditorGUILayout.LabelField("[Direct]", GUILayout.Width(60));
            }
            else if (component.HasPendingUpdate)
            {
                EditorGUILayout.LabelField("[Pending]", GUILayout.Width(60));
            }
            else
            {
                EditorGUILayout.LabelField("[Active]", GUILayout.Width(60));
            }
            
            GUILayout.FlexibleSpace();
            
            // Select button
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeGameObject = component.gameObject;
                EditorGUIUtility.PingObject(component.gameObject);
            }
            
            // Refresh button
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                component.Refresh();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Details
            if (!settings.IsDirect)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Key:", GUILayout.Width(50));
                EditorGUILayout.LabelField(settings.Key);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Domain:", GUILayout.Width(50));
                EditorGUILayout.LabelField(string.IsNullOrEmpty(settings.TableName) ? "[Default UI]" : LocalizationDomains.Normalize(settings.TableName));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
        
        private void DrawKeyBrowserTab()
        {
            EditorGUILayout.LabelField("Key Browser", EditorStyles.boldLabel);
            
            // Search filter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Key list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var filteredKeys = keyUsageCount.Where(kvp => 
                string.IsNullOrEmpty(searchFilter) || 
                kvp.Key.ToLower().Contains(searchFilter.ToLower())
            ).OrderBy(kvp => kvp.Key).ToList();
            
            foreach (var kvp in filteredKeys)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(300));
                EditorGUILayout.LabelField($"Used {kvp.Value} time(s)", GUILayout.Width(100));
                
                if (GUILayout.Button("Find", GUILayout.Width(60)))
                {
                    FindComponentsUsingKey(kvp.Key);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Showing {filteredKeys.Count} of {keyUsageCount.Count} keys");
        }
        
        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField("Validation Results", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Validation"))
            {
                validationResults = SceneTextScanner.ValidateAllLocalizedText();
            }
            
            if (GUILayout.Button("Clear Results"))
            {
                validationResults.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (validationResults.Count == 0)
            {
                EditorGUILayout.HelpBox("No validation issues found. Click 'Run Validation' to check.", MessageType.Info);
                return;
            }
            
            // Group by issue type
            var groupedResults = validationResults.GroupBy(r => r.Issue).OrderBy(g => g.Key);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var group in groupedResults)
            {
                EditorGUILayout.LabelField($"{group.Key} ({group.Count()})", EditorStyles.boldLabel);
                
                foreach (var result in group)
                {
                    if (result.Component == null) continue;
                    
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    EditorGUILayout.LabelField(result.Component.gameObject.name, GUILayout.Width(200));
                    EditorGUILayout.LabelField(result.Message);
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = result.Component.gameObject;
                        EditorGUIUtility.PingObject(result.Component.gameObject);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawTemplateValidationTab()
        {
            EditorGUILayout.LabelField("Template Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tab validates DynamicList mode components and their template patterns.\n" +
                "It checks for missing template keys, invalid patterns, and provides quick-fix suggestions.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate Templates"))
            {
                ValidateTemplates();
            }
            
            if (GUILayout.Button("Clear Results"))
            {
                validationResults.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Filter validation results to only show template-related issues
            var templateIssues = validationResults.Where(r => 
                r.Issue == ValidationIssue.InvalidTemplatePattern || 
                r.Issue == ValidationIssue.MissingTemplateKey).ToList();
            
            if (templateIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("No template validation issues found. Click 'Validate Templates' to check.", MessageType.Info);
                
                // Show statistics about DynamicList components
                var dynamicListComponents = allComponents.Where(c => 
                    c != null && c.GetSettings().Mode == LocalizationMode.DynamicList).ToList();
                
                if (dynamicListComponents.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"DynamicList Components: {dynamicListComponents.Count}", EditorStyles.boldLabel);
                    
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                    
                    foreach (var component in dynamicListComponents)
                    {
                        var settings = component.GetSettings();
                        
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(component.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(200));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeGameObject = component.gameObject;
                            EditorGUIUtility.PingObject(component.gameObject);
                        }
                        EditorGUILayout.EndHorizontal();
                        
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField($"Template Key: {settings.TemplateKey ?? "None"}");
                        EditorGUILayout.LabelField($"Separator: {settings.Separator ?? "\\n"}");
                        
                        if (settings.ListData != null && settings.ListData is System.Collections.IList list)
                        {
                            EditorGUILayout.LabelField($"List Items: {list.Count}");
                        }
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                
                return;
            }
            
            // Display template validation issues with quick-fix suggestions
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField($"Template Issues ({templateIssues.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            foreach (var result in templateIssues)
            {
                if (result.Component == null) continue;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(result.Component.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField($"[{result.Issue}]", GUILayout.Width(150));
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = result.Component.gameObject;
                    EditorGUIUtility.PingObject(result.Component.gameObject);
                }
                EditorGUILayout.EndHorizontal();
                
                // Issue message
                EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);
                
                // Quick-fix suggestions
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Quick-Fix Suggestions:", EditorStyles.boldLabel);
                
                var settings = result.Component.GetSettings();
                
                switch (result.Issue)
                {
                    case ValidationIssue.MissingTemplateKey:
                        EditorGUILayout.HelpBox(
                            "Suggestion: Add a template key to this component.\n" +
                            "Example: Use 'ACP_Stat_Change' for stat change templates.",
                            MessageType.Info);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Suggested Key:", GUILayout.Width(100));
                        string suggestedKey = EditorGUILayout.TextField("ACP_Stat_Change");
                        
                        if (GUILayout.Button("Apply", GUILayout.Width(60)))
                        {
                            // This would require adding a method to LocalizedText to update template key
                            Debug.Log($"Apply template key '{suggestedKey}' to {result.Component.gameObject.name}");
                            EditorUtility.DisplayDialog("Quick Fix", 
                                $"To fix this issue, call SetLocalizedList() or SetLocalizedTemplate() with the template key '{suggestedKey}'.", 
                                "OK");
                        }
                        EditorGUILayout.EndHorizontal();
                        break;
                        
                    case ValidationIssue.InvalidTemplatePattern:
                        EditorGUILayout.HelpBox(
                            "Suggestion: Template patterns must contain variable placeholders.\n" +
                            "Example: 'ACP_Stat_{stat}' or 'Item_{itemId}_Name'",
                            MessageType.Info);
                        
                        EditorGUILayout.LabelField($"Current Pattern: {settings.TemplatePattern}");
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Suggested Pattern:", GUILayout.Width(120));
                        string suggestedPattern = EditorGUILayout.TextField("ACP_Stat_{stat}");
                        
                        if (GUILayout.Button("Apply", GUILayout.Width(60)))
                        {
                            Debug.Log($"Apply template pattern '{suggestedPattern}' to {result.Component.gameObject.name}");
                            EditorUtility.DisplayDialog("Quick Fix", 
                                $"To fix this issue, update the template pattern to '{suggestedPattern}' with proper variable placeholders.", 
                                "OK");
                        }
                        EditorGUILayout.EndHorizontal();
                        break;
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ValidateTemplates()
        {
            // Run full validation
            validationResults = SceneTextScanner.ValidateAllLocalizedText();
            
            // Count template-specific issues
            var templateIssues = validationResults.Where(r => 
                r.Issue == ValidationIssue.InvalidTemplatePattern || 
                r.Issue == ValidationIssue.MissingTemplateKey).Count();
            
            Debug.Log($"[LocalizationEditorWindow] Template validation complete. Found {templateIssues} template-related issues.");
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Localization Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            var config = Resources.Load<LocalizationConfig>("LocalizationConfig");
            
            if (config == null)
            {
                EditorGUILayout.HelpBox("No LocalizationConfig found in Resources folder.", MessageType.Warning);
                
                if (GUILayout.Button("Create LocalizationConfig"))
                {
                    CreateLocalizationConfig();
                }
                return;
            }
            
            // Create editor for config
            var editor = UnityEditor.Editor.CreateEditor(config);
            editor.OnInspectorGUI();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Select Config Asset"))
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }
        
        private void RefreshData()
        {
            // Refresh component list
            allComponents = Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            
            // Calculate key usage
            keyUsageCount.Clear();
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                var settings = component.GetSettings();
                if (!settings.IsDirect && !string.IsNullOrEmpty(settings.Key))
                {
                    if (!keyUsageCount.ContainsKey(settings.Key))
                    {
                        keyUsageCount[settings.Key] = 0;
                    }
                    keyUsageCount[settings.Key]++;
                }
            }
            
            Repaint();
        }
        
        private void FindComponentsUsingKey(string key)
        {
            var components = allComponents.Where(c => 
                c != null && 
                !c.GetSettings().IsDirect && 
                c.GetSettings().Key == key
            ).ToList();
            
            if (components.Count == 0)
            {
                EditorUtility.DisplayDialog("Find Components", $"No components found using key '{key}'.", "OK");
                return;
            }
            
            // Select all components using this key
            Selection.objects = components.Select(c => c.gameObject).ToArray();
            
            EditorUtility.DisplayDialog("Find Components", $"Found {components.Count} component(s) using key '{key}'. They are now selected in the hierarchy.", "OK");
        }
        
        private void CreateLocalizationConfig()
        {
            var config = CreateInstance<LocalizationConfig>();
            
            // Create Resources folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            AssetDatabase.CreateAsset(config, "Assets/Resources/LocalizationConfig.asset");
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("Success", "LocalizationConfig created at Assets/Resources/LocalizationConfig.asset", "OK");
            
            Selection.activeObject = config;
        }
    }
}

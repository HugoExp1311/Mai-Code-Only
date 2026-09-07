using UnityEngine;
using UnityEditor;
using TMPro;
using Base.Localization;
using System.Collections.Generic;
using System.Linq;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Asset post-processor that automatically manages LocalizedText components
    /// when TextMeshPro components are added or modified
    /// </summary>
    public class LocalizedTextPostProcessor : AssetPostprocessor
    {
        private static bool isProcessing = false;
        
        /// <summary>
        /// Called after assets are imported, deleted, moved, or moved from somewhere else
        /// </summary>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Only process scene files
            bool hasSceneChanges = importedAssets.Any(path => path.EndsWith(".unity")) ||
                                   movedAssets.Any(path => path.EndsWith(".unity"));
            
            if (hasSceneChanges && !isProcessing)
            {
                // Delay processing to avoid conflicts
                EditorApplication.delayCall += ProcessSceneChanges;
            }
        }
        
        /// <summary>
        /// Process scene changes and offer to add LocalizedText components
        /// </summary>
        private static void ProcessSceneChanges()
        {
            if (isProcessing) return;
            
            isProcessing = true;
            
            try
            {
                // Check if auto-suggestion is enabled
                var config = Resources.Load<LocalizationConfig>("LocalizationConfig");
                if (config == null || !config.autoSuggestLocalizedText)
                {
                    return;
                }
                
                // Find unmanaged TextMeshPro components
                var unmanagedComponents = SceneTextScanner.FindUnmanagedTextComponents();
                
                if (unmanagedComponents.Count > 0)
                {
                    ProcessUnmanagedComponents(unmanagedComponents, config);
                }
                
                // Validate existing components if enabled
                if (config.validateOnSave)
                {
                    ValidateExistingComponents();
                }
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Process unmanaged TextMeshPro components
        /// </summary>
        private static void ProcessUnmanagedComponents(List<TMP_Text> components, LocalizationConfig config)
        {
            // Filter out components that shouldn't be localized
            var candidateComponents = FilterCandidateComponents(components);
            
            if (candidateComponents.Count == 0) return;
            
            // Show dialog to user
            string message = $"Found {candidateComponents.Count} TextMeshPro component(s) without LocalizedText:\n\n";
            
            for (int i = 0; i < Mathf.Min(candidateComponents.Count, 5); i++)
            {
                var comp = candidateComponents[i];
                string path = GetGameObjectPath(comp.gameObject);
                message += $"• {path}\n";
            }
            
            if (candidateComponents.Count > 5)
            {
                message += $"... and {candidateComponents.Count - 5} more.\n";
            }
            
            message += "\nWould you like to add LocalizedText components?";
            
            int choice = EditorUtility.DisplayDialogComplex(
                "Localization Assistant",
                message,
                "Add to All",
                "Skip",
                "Add to Selected"
            );
            
            switch (choice)
            {
                case 0: // Add to All
                    AddLocalizedTextToComponents(candidateComponents, config);
                    break;
                case 1: // Skip
                    break;
                case 2: // Add to Selected
                    ShowComponentSelectionWindow(candidateComponents, config);
                    break;
            }
        }
        
        /// <summary>
        /// Filter components that are good candidates for localization
        /// </summary>
        private static List<TMP_Text> FilterCandidateComponents(List<TMP_Text> components)
        {
            var candidates = new List<TMP_Text>();
            
            foreach (var component in components)
            {
                if (component == null) continue;
                
                // Skip if already has LocalizedText
                if (component.GetComponent<LocalizedText>() != null) continue;
                
                // Skip if text is empty or looks like placeholder
                if (string.IsNullOrEmpty(component.text) || 
                    component.text == "New Text" || 
                    component.text == "Text (TMP)" ||
                    component.text.StartsWith("Sample"))
                {
                    continue;
                }
                
                // Skip if GameObject name suggests it's not for localization
                string objectName = component.gameObject.name.ToLower();
                if (objectName.Contains("debug") || 
                    objectName.Contains("temp") || 
                    objectName.Contains("test"))
                {
                    continue;
                }
                
                candidates.Add(component);
            }
            
            return candidates;
        }
        
        /// <summary>
        /// Add LocalizedText to components with auto-suggested keys
        /// </summary>
        public static void AddLocalizedTextToComponents(List<TMP_Text> components, LocalizationConfig config)
        {
            int addedCount = 0;
            
            foreach (var component in components)
            {
                if (component == null) continue;
                
                var localizedText = component.gameObject.AddComponent<LocalizedText>();
                
                // Auto-suggest key if enabled
                if (config.autoSuggestKeys)
                {
                    string suggestedKey = SceneTextScanner.SuggestKeysForComponent(component);
                    if (!string.IsNullOrEmpty(suggestedKey))
                    {
                        localizedText.SetLocalized(suggestedKey);
                    }
                }
                
                addedCount++;
            }
            
            Debug.Log($"[LocalizedTextPostProcessor] Added LocalizedText to {addedCount} components.");
            
            // Mark scene as dirty
            if (addedCount > 0)
            {
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            }
        }
        
        /// <summary>
        /// Validate existing LocalizedText components
        /// </summary>
        private static void ValidateExistingComponents()
        {
            var validationResults = SceneTextScanner.ValidateAllLocalizedText();
            
            if (validationResults.Count > 0)
            {
                var errorCount = validationResults.Count(r => r.Issue == ValidationIssue.MissingTable || r.Issue == ValidationIssue.MissingEntry);
                var warningCount = validationResults.Count - errorCount;
                
                if (errorCount > 0)
                {
                    Debug.LogWarning($"[LocalizedTextPostProcessor] Found {errorCount} localization errors and {warningCount} warnings. Use Localization > Scan Scene > Validate All to see details.");
                }
            }
        }
        
        /// <summary>
        /// Show window for selecting which components to add LocalizedText to
        /// </summary>
        private static void ShowComponentSelectionWindow(List<TMP_Text> components, LocalizationConfig config)
        {
            var window = EditorWindow.GetWindow<ComponentSelectionWindow>("Select Components");
            window.Initialize(components, config);
            window.Show();
        }
        
        /// <summary>
        /// Get the full path of a GameObject in the hierarchy
        /// </summary>
        public static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
    }
    
    /// <summary>
    /// Window for selecting which components to add LocalizedText to
    /// </summary>
    public class ComponentSelectionWindow : EditorWindow
    {
        private List<TMP_Text> components;
        private LocalizationConfig config;
        private bool[] selections;
        private Vector2 scrollPosition;
        
        public void Initialize(List<TMP_Text> components, LocalizationConfig config)
        {
            this.components = components;
            this.config = config;
            this.selections = new bool[components.Count];
            
            // Select all by default
            for (int i = 0; i < selections.Length; i++)
            {
                selections[i] = true;
            }
        }
        
        private void OnGUI()
        {
            if (components == null) return;
            
            EditorGUILayout.LabelField("Select components to add LocalizedText:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Select All / None buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                for (int i = 0; i < selections.Length; i++)
                {
                    selections[i] = true;
                }
            }
            if (GUILayout.Button("Select None"))
            {
                for (int i = 0; i < selections.Length; i++)
                {
                    selections[i] = false;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Component list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] == null) continue;
                
                EditorGUILayout.BeginHorizontal();
                
                selections[i] = EditorGUILayout.Toggle(selections[i], GUILayout.Width(20));
                
                string path = LocalizedTextPostProcessor.GetGameObjectPath(components[i].gameObject);
                EditorGUILayout.LabelField(path);
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = components[i].gameObject;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Add LocalizedText"))
            {
                var selectedComponents = new List<TMP_Text>();
                for (int i = 0; i < components.Count; i++)
                {
                    if (selections[i] && components[i] != null)
                    {
                        selectedComponents.Add(components[i]);
                    }
                }
                
                if (selectedComponents.Count > 0)
                {
                    LocalizedTextPostProcessor.AddLocalizedTextToComponents(selectedComponents, config);
                }
                
                Close();
            }
            
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}

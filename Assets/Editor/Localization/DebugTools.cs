using UnityEngine;
using UnityEditor;
using Base.Localization;
using Base.Localization.EditorTools;
using System.Linq;
using System.Text;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Debug tools for localization system
    /// </summary>
    public static class DebugTools
    {
        /// <summary>
        /// List all active LocalizedText components
        /// </summary>
        [MenuItem("Localization/Debug/List Active Components")]
        public static void ListActiveComponents()
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[DebugTools] LocalizationManager not found in scene.");
                return;
            }
            
            var components = LocalizationManager.Instance.GetActiveComponents();
            
            var sb = new StringBuilder();
            sb.AppendLine($"=== Active LocalizedText Components ({components.Count}) ===");
            
            foreach (var component in components)
            {
                if (component == null) continue;
                
                var settings = component.GetSettings();
                sb.AppendLine($"• {component.gameObject.name}");
                sb.AppendLine($"  - ID: {component.ComponentId}");
                sb.AppendLine($"  - Key: {settings.Key}");
                sb.AppendLine($"  - Domain: {LocalizationDomains.Normalize(settings.TableName)}");
                sb.AppendLine($"  - Is Direct: {settings.IsDirect}");
                sb.AppendLine($"  - Has Pending: {component.HasPendingUpdate}");
                sb.AppendLine();
            }
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// List components with pending updates
        /// </summary>
        [MenuItem("Localization/Debug/List Pending Updates")]
        public static void ListPendingUpdates()
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[DebugTools] LocalizationManager not found in scene.");
                return;
            }
            
            var components = LocalizationManager.Instance.GetComponentsWithPendingUpdates();
            
            if (components.Count == 0)
            {
                Debug.Log("[DebugTools] No components with pending updates.");
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"=== Components with Pending Updates ({components.Count}) ===");
            
            foreach (var component in components)
            {
                if (component == null) continue;
                
                var settings = component.GetSettings();
                sb.AppendLine($"• {component.gameObject.name}");
                sb.AppendLine($"  - Key: {settings.Key}");
                sb.AppendLine($"  - Active: {component.gameObject.activeInHierarchy}");
                sb.AppendLine();
            }
            
            Debug.Log(sb.ToString());
        }
        
        /// <summary>
        /// Highlight all components using a specific key
        /// </summary>
        [MenuItem("Localization/Debug/Highlight Components Using Key")]
        public static void HighlightComponentsUsingKey()
        {
            string key = EditorInputDialog.Show("Enter Key", "Enter the localization key to search for:", "");
            
            if (string.IsNullOrEmpty(key)) return;
            
            var allComponents = Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var matchingComponents = allComponents.Where(c => 
                c != null && 
                !c.GetSettings().IsDirect && 
                c.GetSettings().Key == key
            ).ToList();
            
            if (matchingComponents.Count == 0)
            {
                EditorUtility.DisplayDialog("Search Results", $"No components found using key '{key}'.", "OK");
                return;
            }
            
            // Select all matching components
            Selection.objects = matchingComponents.Select(c => c.gameObject).ToArray();
            
            // Log results
            var sb = new StringBuilder();
            sb.AppendLine($"=== Components Using Key '{key}' ({matchingComponents.Count}) ===");
            
            foreach (var component in matchingComponents)
            {
                sb.AppendLine($"• {component.gameObject.name}");
            }
            
            Debug.Log(sb.ToString());
            
            EditorUtility.DisplayDialog("Search Results", $"Found {matchingComponents.Count} component(s) using key '{key}'. They are now selected in the hierarchy.", "OK");
        }
        
        /// <summary>
        /// Get current language
        /// </summary>
        [MenuItem("Localization/Debug/Show Current Language")]
        public static void ShowCurrentLanguage()
        {
            if (LocalizationManager.Instance == null)
            {
                EditorUtility.DisplayDialog("Current Language", "LocalizationManager not found in scene.", "OK");
                return;
            }
            
            var language = LocalizationManager.Instance.GetCurrentLanguage();
            Debug.Log($"[DebugTools] Current Language: {language}");
            EditorUtility.DisplayDialog("Current Language", $"Current Language: {language}", "OK");
        }
        
        /// <summary>
        /// Force refresh all components
        /// </summary>
        [MenuItem("Localization/Debug/Force Refresh All")]
        public static void ForceRefreshAll()
        {
            if (LocalizationManager.Instance == null)
            {
                EditorUtility.DisplayDialog("Error", "LocalizationManager not found in scene.", "OK");
                return;
            }
            
            Debug.Log("[DebugTools] Force refreshing all components...");
            LocalizationManager.Instance.RefreshAll();
            Debug.Log("[DebugTools] All components refreshed.");
            
            EditorUtility.DisplayDialog("Success", "All components have been refreshed.", "OK");
        }
        
        /// <summary>
        /// Show localization statistics
        /// </summary>
        [MenuItem("Localization/Debug/Show Statistics")]
        public static void ShowStatistics()
        {
            var allComponents = Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var activeComponents = allComponents.Where(c => c != null && c.gameObject.activeInHierarchy).ToList();
            var pendingComponents = allComponents.Where(c => c != null && c.HasPendingUpdate).ToList();
            var directComponents = allComponents.Where(c => c != null && c.GetSettings().IsDirect).ToList();
            
            var unmanagedTextComponents = SceneTextScanner.FindUnmanagedTextComponents();
            
            var sb = new StringBuilder();
            sb.AppendLine("=== Localization Statistics ===");
            sb.AppendLine($"Total LocalizedText Components: {allComponents.Length}");
            sb.AppendLine($"Active Components: {activeComponents.Count}");
            sb.AppendLine($"Components with Pending Updates: {pendingComponents.Count}");
            sb.AppendLine($"Direct Text Components: {directComponents.Count}");
            sb.AppendLine($"Unmanaged TextMeshPro Components: {unmanagedTextComponents.Count}");
            sb.AppendLine();
            
            // Runtime CSV domain statistics
            var domainNames = TableRegistry.GetAllTableNames();
            sb.AppendLine($"Available Domains: {domainNames.Count}");
            foreach (var domainName in domainNames)
            {
                var domain = LocalizationDomains.Normalize(domainName);
                foreach (string localeCode in RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes)
                {
                    int count = RuntimeCsvLocalizationEditorUtility.LoadDomain(domain, localeCode).Count;
                    sb.AppendLine($"  • {domain}/{localeCode}: {count} entries");
                }
            }
            
            Debug.Log(sb.ToString());
            
            EditorUtility.DisplayDialog("Localization Statistics", 
                $"Total Components: {allComponents.Length}\n" +
                $"Active: {activeComponents.Count}\n" +
                $"Pending: {pendingComponents.Count}\n" +
                $"Unmanaged: {unmanagedTextComponents.Count}\n" +
                $"Domains: {domainNames.Count}",
                "OK");
        }
        
        /// <summary>
        /// Toggle debug mode
        /// </summary>
        [MenuItem("Localization/Debug/Toggle Debug Mode")]
        public static void ToggleDebugMode()
        {
            var config = Resources.Load<LocalizationConfig>("LocalizationConfig");
            
            if (config == null)
            {
                EditorUtility.DisplayDialog("Error", "LocalizationConfig not found in Resources folder.", "OK");
                return;
            }
            
            config.debugMode = !config.debugMode;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[DebugTools] Debug mode {(config.debugMode ? "enabled" : "disabled")}.");
            EditorUtility.DisplayDialog("Debug Mode", $"Debug mode is now {(config.debugMode ? "enabled" : "disabled")}.", "OK");
        }
        
        /// <summary>
        /// Validate all keys in scene
        /// </summary>
        [MenuItem("Localization/Debug/Validate All Keys")]
        public static void ValidateAllKeys()
        {
            var results = SceneTextScanner.ValidateAllLocalizedText();
            
            if (results.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation", "All keys are valid!", "OK");
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"=== Validation Issues ({results.Count}) ===");
            
            var groupedResults = results.GroupBy(r => r.Issue);
            foreach (var group in groupedResults)
            {
                sb.AppendLine($"\n{group.Key}:");
                foreach (var result in group)
                {
                    if (result.Component != null)
                    {
                        sb.AppendLine($"  • {result.Component.gameObject.name}: {result.Message}");
                    }
                }
            }
            
            Debug.LogWarning(sb.ToString());
            
            EditorUtility.DisplayDialog("Validation", $"Found {results.Count} validation issues. Check console for details.", "OK");
        }
    }
    
    /// <summary>
    /// Simple input dialog for editor
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string inputText = "";
        private string dialogTitle = "";
        private string dialogMessage = "";
        private System.Action<string> onConfirm;
        
        public static string Show(string title, string message, string defaultValue)
        {
            var window = CreateInstance<EditorInputDialog>();
            window.dialogTitle = title;
            window.dialogMessage = message;
            window.inputText = defaultValue;
            window.titleContent = new GUIContent(title);
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);
            
            window.ShowModal();
            
            return window.inputText;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(dialogMessage);
            EditorGUILayout.Space();
            
            inputText = EditorGUILayout.TextField(inputText);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("OK"))
            {
                Close();
            }
            
            if (GUILayout.Button("Cancel"))
            {
                inputText = "";
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}

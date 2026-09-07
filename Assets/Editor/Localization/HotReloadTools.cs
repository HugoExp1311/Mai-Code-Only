using UnityEngine;
using UnityEditor;
using Base.Localization;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Tools for hot reloading runtime localization CSVs during development
    /// </summary>
    public static class HotReloadTools
    {
        private const string HOTKEY_RELOAD_ALL = "Ctrl+Shift+L";
        
        /// <summary>
        /// Reload runtime CSV localization and refresh affected components for one domain
        /// </summary>
        public static void ReloadTable(string tableName)
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[HotReloadTools] LocalizationManager not found in scene.");
                return;
            }

            string domain = LocalizationDomains.Normalize(tableName);
            Debug.Log($"[HotReloadTools] Reloading localization domain '{domain}' from runtime CSV...");

            LocalizationManager.Instance.ReloadCsvLocalization(refresh: false, forceFromDiskInEditor: true);
            LocalizationManager.Instance.RefreshTable(domain);

            Debug.Log($"[HotReloadTools] Domain '{domain}' reloaded successfully.");
        }
        
        /// <summary>
        /// Reload all runtime localization CSVs and refresh all components
        /// </summary>
        public static void ReloadAllTables()
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogWarning("[HotReloadTools] LocalizationManager not found in scene.");
                return;
            }
            
            Debug.Log("[HotReloadTools] Reloading all runtime localization CSVs...");

            LocalizationManager.Instance.ReloadCsvLocalization(refresh: true, forceFromDiskInEditor: true);

            Debug.Log("[HotReloadTools] Runtime localization CSVs reloaded successfully.");
        }
        
        /// <summary>
        /// Menu item: Reload specific table
        /// </summary>
        [MenuItem("Localization/Hot Reload/Reload UI Domain")]
        public static void MenuReloadUITable()
        {
            ReloadTable(LocalizationDomains.UI);
            EditorUtility.DisplayDialog("Hot Reload", "UI domain reloaded successfully.", "OK");
        }
        
        [MenuItem("Localization/Hot Reload/Reload Dialogues Domain")]
        public static void MenuReloadDialogueTable()
        {
            ReloadTable(LocalizationDomains.Dialogues);
            EditorUtility.DisplayDialog("Hot Reload", "Dialogues domain reloaded successfully.", "OK");
        }
        
        /// <summary>
        /// Menu item: Reload all tables with keyboard shortcut
        /// </summary>
        [MenuItem("Localization/Hot Reload/Reload All CSVs %#l")] // Ctrl+Shift+L
        public static void MenuReloadAllTables()
        {
            ReloadAllTables();
            EditorUtility.DisplayDialog("Hot Reload", "All runtime localization CSVs reloaded successfully.", "OK");
        }
        
        /// <summary>
        /// Menu item: Force refresh all components
        /// </summary>
        [MenuItem("Localization/Hot Reload/Force Refresh All Components")]
        public static void MenuForceRefreshAll()
        {
            if (LocalizationManager.Instance == null)
            {
                EditorUtility.DisplayDialog("Error", "LocalizationManager not found in scene.", "OK");
                return;
            }
            
            LocalizationManager.Instance.RefreshAll();
            EditorUtility.DisplayDialog("Hot Reload", "All components refreshed successfully.", "OK");
        }
        
        /// <summary>
        /// Enable hot reload in play mode
        /// </summary>
        [InitializeOnLoadMethod]
        private static void EnablePlayModeHotReload()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Debug.Log("[HotReloadTools] Hot reload enabled in play mode. Press Ctrl+Shift+L to reload runtime localization CSVs.");
            }
        }
    }
    
    /// <summary>
    /// Editor window for hot reload settings and controls
    /// </summary>
    public class HotReloadWindow : EditorWindow
    {
        private string selectedTable = LocalizationDomains.UI;
        private bool autoReloadOnChange = false;
        
        [MenuItem("Localization/Hot Reload/Hot Reload Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<HotReloadWindow>("Hot Reload");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Hot Reload Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Domain selection
            EditorGUILayout.LabelField("Select Domain:");
            var tableNames = TableRegistry.GetAllTableNames();
            
            if (tableNames.Count > 0)
            {
                int currentIndex = tableNames.IndexOf(selectedTable);
                if (currentIndex == -1) currentIndex = 0;
                
                int newIndex = EditorGUILayout.Popup(currentIndex, tableNames.ToArray());
                if (newIndex >= 0 && newIndex < tableNames.Count)
                {
                    selectedTable = tableNames[newIndex];
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No domains found.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Reload buttons
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
            
            if (GUILayout.Button($"Reload '{selectedTable}'"))
            {
                HotReloadTools.ReloadTable(selectedTable);
            }
            
            if (GUILayout.Button("Reload All CSVs"))
            {
                HotReloadTools.ReloadAllTables();
            }
            
            if (GUILayout.Button("Force Refresh All Components"))
            {
                HotReloadTools.MenuForceRefreshAll();
            }
            
            EditorGUILayout.Space();
            
            // Settings
            EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);
            autoReloadOnChange = EditorGUILayout.Toggle("Auto-reload on change", autoReloadOnChange);
            
            if (autoReloadOnChange)
            {
                EditorGUILayout.HelpBox("Auto-reload monitors runtime localization CSV files and reloads them when changed.", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Info
            EditorGUILayout.HelpBox("Keyboard Shortcut: Ctrl+Shift+L to reload all runtime localization CSVs", MessageType.Info);
        }
    }
    
    /// <summary>
    /// Asset post-processor for auto-reload functionality
    /// </summary>
    public class LocalizationAssetPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool hasLocalizationChanges = false;
            
            // Check if any localization assets were modified
            foreach (var asset in importedAssets)
            {
                if (asset.Contains("Localization") && asset.EndsWith(".csv"))
                {
                    hasLocalizationChanges = true;
                    break;
                }
            }
            
            if (hasLocalizationChanges)
            {
                // Delay reload to avoid conflicts
                EditorApplication.delayCall += () =>
                {
                    var config = Resources.Load<LocalizationConfig>("LocalizationConfig");
                    if (config != null && config.autoReloadOnAssetChange)
                    {
                        Debug.Log("[HotReloadTools] Localization assets changed, auto-reloading...");
                        HotReloadTools.ReloadAllTables();
                    }
                };
            }
        }
    }
}

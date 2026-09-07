using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Post-processor that migrates serialized references from UIPanel2 system to UIPanel system
/// Preserves all component data and serialized field values during the migration
/// 
/// ⚠️ OBSOLETE: Migration from UIPanel2 to UIPanel is complete. This tool is no longer needed.
/// Kept for reference only. Can be safely deleted if desired.
/// 
/// Usage (all menu items under Tools/UI Migration):
/// 1. "Capture UIPanel2 GUIDs" - Captures current script GUIDs from .meta files
/// 2. "Validate Migration Readiness (Before Rename)" - Optional: Counts references to verify readiness
/// 3. "Rename UIPanel2 Files to UIPanel" - Automatically renames all files (Unity generates new GUIDs)
/// 4. "Finalize UIPanel2 to UIPanel Migration (After Rename)" - Updates all GUID references in scenes/prefabs
/// </summary>
public class UIPanel2ToUIPanelPostProcessor
{
    // Mapping of old class names to new class names
    private static readonly Dictionary<string, string> ClassNameMapping = new Dictionary<string, string>
    {
        { "UIPanel2", "UIPanel" },
        { "UIPanelManager2", "UIPanelManager" },
        { "UIPanel2WithBackground", "UIPanelWithBackground" },
        { "UIPanel2Editor", "UIPanelEditor" },
        { "UIPanelManager2Editor", "UIPanelManagerEditor" }
    };

    // Mapping of old file paths to new file paths
    private static readonly Dictionary<string, string> FilePathMapping = new Dictionary<string, string>
    {
        { "Assets/Scripts/UI/UIPanel2.cs", "Assets/Scripts/UI/UIPanel.cs" },
        { "Assets/Scripts/UI/UIPanelManager2.cs", "Assets/Scripts/UI/UIPanelManager.cs" },
        { "Assets/Scripts/UI/UIPanel2WithBackground.cs", "Assets/Scripts/UI/UIPanelWithBackground.cs" },
        { "Assets/Scripts/UI/Editor/UIPanel2Editor.cs", "Assets/Scripts/UI/Editor/UIPanelEditor.cs" },
        { "Assets/Scripts/UI/Editor/UIPanelManager2Editor.cs", "Assets/Scripts/UI/Editor/UIPanelManagerEditor.cs" }
    };

    /// <summary>
    /// Captures current GUIDs from .meta files before renaming
    /// Run this FIRST before any renaming
    /// </summary>
    [MenuItem("Tools/UI Migration/Capture UIPanel2 GUIDs")]
    public static void CaptureOldGUIDs()
    {
        Dictionary<string, string> guidMap = new Dictionary<string, string>();
        
        foreach (var kvp in FilePathMapping)
        {
            string oldPath = kvp.Key;
            string metaPath = oldPath + ".meta";
            
            if (File.Exists(metaPath))
            {
                string metaContent = File.ReadAllText(metaPath);
                Match guidMatch = Regex.Match(metaContent, @"guid:\s*([a-f0-9]{32})", RegexOptions.IgnoreCase);
                
                if (guidMatch.Success)
                {
                    string guid = guidMatch.Groups[1].Value;
                    guidMap[oldPath] = guid;
                    Debug.Log($"[UIPanel2ToUIPanel] Captured GUID for {oldPath}: {guid}");
                }
                else
                {
                    Debug.LogWarning($"[UIPanel2ToUIPanel] Could not find GUID in {metaPath}");
                }
            }
            else
            {
                Debug.LogWarning($"[UIPanel2ToUIPanel] Meta file not found: {metaPath}");
            }
        }
        
        // Store GUIDs in EditorPrefs for later use
        string guidMapJson = JsonUtility.ToJson(new SerializableDictionary(guidMap));
        EditorPrefs.SetString("UIPanel2ToUIPanel_GUIDMap", guidMapJson);
        
        Debug.Log($"[UIPanel2ToUIPanel] Captured {guidMap.Count} GUIDs. Ready for migration.");
    }

    /// <summary>
    /// Validates that GUIDs are captured and counts references
    /// Run this BEFORE renaming files to verify everything is ready
    /// This doesn't modify anything - just validates and reports
    /// </summary>
    [MenuItem("Tools/UI Migration/Validate Migration Readiness (Before Rename)")]
    public static void ValidateBeforeRename()
    {
        // First capture GUIDs if not already done
        if (string.IsNullOrEmpty(EditorPrefs.GetString("UIPanel2ToUIPanel_GUIDMap", "")))
        {
            Debug.Log("[UIPanel2ToUIPanel] GUIDs not captured. Capturing now...");
            CaptureOldGUIDs();
        }

        string guidMapJson = EditorPrefs.GetString("UIPanel2ToUIPanel_GUIDMap", "");
        if (string.IsNullOrEmpty(guidMapJson))
        {
            Debug.LogError("[UIPanel2ToUIPanel] No GUID map found. Please run 'Capture UIPanel2 GUIDs' first.");
            return;
        }

        SerializableDictionary guidMap = JsonUtility.FromJson<SerializableDictionary>(guidMapJson);
        
        int sceneCount = 0;
        int prefabCount = 0;
        int totalReferences = 0;

        // Count references in scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            int refs = CountReferencesInAsset(scenePath, guidMap.dictionary);
            if (refs > 0)
            {
                sceneCount++;
                totalReferences += refs;
            }
        }

        // Count references in prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            int refs = CountReferencesInAsset(prefabPath, guidMap.dictionary);
            if (refs > 0)
            {
                prefabCount++;
                totalReferences += refs;
            }
        }

        Debug.Log($"[UIPanel2ToUIPanel] Validation complete! Found {totalReferences} references to UIPanel2 system in {sceneCount} scenes and {prefabCount} prefabs.");
        Debug.Log("[UIPanel2ToUIPanel] You can now rename the files. After renaming, run 'Finalize UIPanel2 to UIPanel Migration (After Rename)' to update all GUID references.");
    }

    /// <summary>
    /// Renames all UIPanel2 system files to remove the "2" suffix
    /// Run this AFTER capturing GUIDs and validating
    /// Unity will automatically generate new GUIDs in new .meta files
    /// </summary>
    [MenuItem("Tools/UI Migration/Rename UIPanel2 Files to UIPanel")]
    public static void RenameFiles()
    {
        // Check if GUIDs are captured
        string guidMapJson = EditorPrefs.GetString("UIPanel2ToUIPanel_GUIDMap", "");
        if (string.IsNullOrEmpty(guidMapJson))
        {
            Debug.LogWarning("[UIPanel2ToUIPanel] GUIDs not captured. Capturing now...");
            CaptureOldGUIDs();
        }

        int successCount = 0;
        int errorCount = 0;

        foreach (var kvp in FilePathMapping)
        {
            string oldPath = kvp.Key;
            string newPath = kvp.Value;

            // Check if old file exists
            if (!File.Exists(oldPath))
            {
                Debug.LogWarning($"[UIPanel2ToUIPanel] Old file not found: {oldPath}. Skipping.");
                continue;
            }

            // Check if new file already exists
            if (File.Exists(newPath))
            {
                Debug.LogError($"[UIPanel2ToUIPanel] New file already exists: {newPath}. Cannot rename. Please delete it first or rename manually.");
                errorCount++;
                continue;
            }

            // Use AssetDatabase.MoveAsset to rename (preserves Unity's internal references)
            string result = AssetDatabase.MoveAsset(oldPath, newPath);
            
            if (string.IsNullOrEmpty(result))
            {
                Debug.Log($"[UIPanel2ToUIPanel] Renamed: {Path.GetFileName(oldPath)} → {Path.GetFileName(newPath)}");
                successCount++;
            }
            else
            {
                Debug.LogError($"[UIPanel2ToUIPanel] Failed to rename {oldPath}: {result}");
                errorCount++;
            }
        }

        AssetDatabase.Refresh();

        if (successCount > 0)
        {
            Debug.Log($"[UIPanel2ToUIPanel] Successfully renamed {successCount} file(s). Unity will generate new GUIDs.");
            Debug.Log("[UIPanel2ToUIPanel] Now run 'Finalize UIPanel2 to UIPanel Migration (After Rename)' to update all references.");
        }

        if (errorCount > 0)
        {
            Debug.LogError($"[UIPanel2ToUIPanel] {errorCount} file(s) could not be renamed. Please check errors above.");
        }
    }

    /// <summary>
    /// Finalizes migration after files are renamed
    /// Updates references from old GUIDs to new GUIDs
    /// Run this AFTER renaming files
    /// </summary>
    [MenuItem("Tools/UI Migration/Finalize UIPanel2 to UIPanel Migration (After Rename)")]
    public static void FinalizeMigration()
    {
        string guidMapJson = EditorPrefs.GetString("UIPanel2ToUIPanel_GUIDMap", "");
        if (string.IsNullOrEmpty(guidMapJson))
        {
            Debug.LogError("[UIPanel2ToUIPanel] No GUID map found. Please run migration before renaming first.");
            return;
        }

        SerializableDictionary oldGuidMap = JsonUtility.FromJson<SerializableDictionary>(guidMapJson);
        Dictionary<string, string> newGuidMap = new Dictionary<string, string>();

        // Capture new GUIDs from renamed files
        foreach (var kvp in FilePathMapping)
        {
            string newPath = kvp.Value;
            string metaPath = newPath + ".meta";
            
            if (File.Exists(metaPath))
            {
                string metaContent = File.ReadAllText(metaPath);
                Match guidMatch = Regex.Match(metaContent, @"guid:\s*([a-f0-9]{32})", RegexOptions.IgnoreCase);
                
                if (guidMatch.Success)
                {
                    string newGuid = guidMatch.Groups[1].Value;
                    string oldPath = kvp.Key;
                    
                    if (oldGuidMap.dictionary.Any(kvp2 => kvp2.key == oldPath))
                    {
                        var oldKvp = oldGuidMap.dictionary.First(kvp2 => kvp2.key == oldPath);
                        string oldGuid = oldKvp.value;
                        newGuidMap[oldGuid] = newGuid;
                        Debug.Log($"[UIPanel2ToUIPanel] Mapped GUID: {oldGuid} → {newGuid} ({Path.GetFileName(newPath)})");
                    }
                }
            }
        }

        if (newGuidMap.Count == 0)
        {
            Debug.LogWarning("[UIPanel2ToUIPanel] No new GUIDs found. Make sure files have been renamed.");
            return;
        }

        int sceneCount = 0;
        int prefabCount = 0;
        int totalUpdates = 0;

        // Process all scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            int updates = UpdateGUIDsInAsset(scenePath, newGuidMap, isPrefab: false);
            if (updates > 0)
            {
                sceneCount++;
                totalUpdates += updates;
            }
        }

        // Process all prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            int updates = UpdateGUIDsInAsset(prefabPath, newGuidMap, isPrefab: true);
            if (updates > 0)
            {
                prefabCount++;
                totalUpdates += updates;
            }
        }

        AssetDatabase.Refresh();

        Debug.Log($"[UIPanel2ToUIPanel] Finalization complete! Updated {totalUpdates} GUID references in {sceneCount} scenes and {prefabCount} prefabs.");
        
        // Clear stored GUID map
        EditorPrefs.DeleteKey("UIPanel2ToUIPanel_GUIDMap");
    }

    /// <summary>
    /// Counts references to UIPanel2 system scripts in an asset file
    /// Used for validation before migration
    /// </summary>
    private static int CountReferencesInAsset(string assetPath, List<KeyValuePair> guidMap)
    {
        if (!File.Exists(assetPath))
            return 0;

        string content = File.ReadAllText(assetPath);
        int referenceCount = 0;

        // Count references by GUID
        foreach (var kvp in guidMap)
        {
            string oldGuid = kvp.value;
            
            // Pattern: m_Script: {fileID: 11400000, guid: <GUID>, type: 3}
            string pattern = $@"guid:\s*{oldGuid}";
            MatchCollection matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            referenceCount += matches.Count;
        }

        return referenceCount;
    }

    /// <summary>
    /// Updates GUID references in an asset file after files are renamed
    /// </summary>
    private static int UpdateGUIDsInAsset(string assetPath, Dictionary<string, string> guidMap, bool isPrefab)
    {
        if (!File.Exists(assetPath))
            return 0;

        string content = File.ReadAllText(assetPath);
        int updateCount = 0;

        foreach (var kvp in guidMap)
        {
            string oldGuid = kvp.Key;
            string newGuid = kvp.Value;

            // Pattern: guid: <GUID>
            string pattern = $@"guid:\s*{oldGuid}";
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                content = Regex.Replace(content, pattern, $"guid: {newGuid}", RegexOptions.IgnoreCase);
                updateCount++;
            }
        }

        if (updateCount > 0)
        {
            File.WriteAllText(assetPath, content);
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log($"[UIPanel2ToUIPanel] Updated {updateCount} GUID references in: {assetPath}");
        }

        return updateCount;
    }

    /// <summary>
    /// Helper class for serializing Dictionary to JSON
    /// </summary>
    [System.Serializable]
    private class SerializableDictionary
    {
        public List<KeyValuePair> dictionary = new List<KeyValuePair>();

        public SerializableDictionary() { }

        public SerializableDictionary(Dictionary<string, string> dict)
        {
            foreach (var kvp in dict)
            {
                dictionary.Add(new KeyValuePair { key = kvp.Key, value = kvp.Value });
            }
        }
    }

    [System.Serializable]
    private class KeyValuePair
    {
        public string key;
        public string value;
    }
}


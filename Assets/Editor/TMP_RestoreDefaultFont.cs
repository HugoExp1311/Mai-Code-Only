using UnityEngine;
using UnityEditor;
using TMPro; // Make sure TextMeshPro is imported
using UnityEditor.SceneManagement; // Required for scene operations

public class TMP_RestoreDefaultFont : EditorWindow
{
    private bool includePrefabs = true;
    private bool includeOpenScenes = true;
    private bool onlyIfMissing = true;

    [MenuItem("Tools/TextMeshPro/Restore Default Font Utility...")]
    public static void ShowWindow()
    {
        GetWindow<TMP_RestoreDefaultFont>("Restore TMP Font");
    }

    void OnGUI()
    {
        GUILayout.Label("Restore Default Font Asset", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This utility finds TextMeshProUGUI components and sets their font asset to the project's default.", MessageType.Info);

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

        if (defaultFont == null)
        {
            EditorGUILayout.HelpBox("No Default Font Asset is assigned in Project Settings -> TextMeshPro -> Settings.", MessageType.Error);
            GUI.enabled = false; // Disable button if no default font
        }
        else
        {
            EditorGUILayout.LabelField("Default Font:", defaultFont.name);
        }

        EditorGUILayout.Space();

        includeOpenScenes = EditorGUILayout.Toggle("Process Open Scenes", includeOpenScenes);
        includePrefabs = EditorGUILayout.Toggle("Process Project Prefabs", includePrefabs);
        onlyIfMissing = EditorGUILayout.Toggle("Only if Font is Missing", onlyIfMissing);

        EditorGUILayout.Space();

        if (GUILayout.Button("Restore Default Font"))
        {
            if (EditorUtility.DisplayDialog("Confirm Font Restoration",
                "This will modify TextMeshProUGUI components in the selected scope(s).\n" +
                $"Target Font: '{defaultFont?.name ?? "NONE"}'\n\n" +
                "It's recommended to backup your project first. Are you sure?",
                "Yes, Restore", "Cancel"))
            {
                RestoreFonts(defaultFont, includeOpenScenes, includePrefabs, onlyIfMissing);
            }
        }

        // Re-enable GUI if it was disabled
        if (defaultFont == null) GUI.enabled = true;
    }

    private static void RestoreFonts(TMP_FontAsset defaultFont, bool processScenes, bool processPrefabs, bool onlyMissing)
    {
        if (defaultFont == null)
        {
            Debug.LogError("Cannot restore font: Default Font Asset is not set in TextMeshPro settings.");
            return;
        }

        int componentsModified = 0;
        int assetsProcessed = 0;

        // --- Process Open Scenes ---
        if (processScenes)
        {
            Debug.Log("--- Processing Open Scenes ---");
            int sceneCount = EditorSceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                Debug.Log($"Processing Scene: {scene.name}");
                assetsProcessed++;
                var rootObjects = scene.GetRootGameObjects();
                bool sceneChanged = false;

                foreach (GameObject go in rootObjects)
                {
                    // Include inactive components using true
                    TextMeshProUGUI[] tmps = go.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (TextMeshProUGUI tmp in tmps)
                    {
                        if (!onlyMissing || tmp.font == null) // Check if font is null (missing) or if we process all
                        {
                            if (tmp.font != defaultFont) // Only change if different
                            {
                                Undo.RecordObject(tmp, "Set TMP Font to Default"); // Register Undo operation
                                tmp.font = defaultFont;
                                componentsModified++;
                                EditorUtility.SetDirty(tmp); // Mark component as dirty
                                sceneChanged = true;
                                Debug.Log($"Updated font on '{tmp.gameObject.name}' in scene '{scene.name}'", tmp.gameObject);
                            }
                        }
                    }
                }
                if (sceneChanged)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Scene '{scene.name}' marked dirty.");
                }
            }
            Debug.Log($"--- Finished Processing Open Scenes ---");
        }


        // --- Process Prefabs ---
        if (processPrefabs)
        {
            Debug.Log("--- Processing Project Prefabs ---");
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int totalPrefabs = guids.Length;
            int currentPrefabIndex = 0;

            EditorUtility.DisplayProgressBar("Processing Prefabs", "Scanning project for prefabs...", 0f);

            foreach (string guid in guids)
            {
                currentPrefabIndex++;
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    bool prefabChanged = false;
                    // GetComponentsInChildren also works on prefab assets
                    TextMeshProUGUI[] tmps = prefab.GetComponentsInChildren<TextMeshProUGUI>(true); // Include inactive

                    if (tmps.Length > 0) // Only display progress for prefabs containing TMP components
                    {
                        EditorUtility.DisplayProgressBar("Processing Prefabs", $"Processing: {prefab.name}", (float)currentPrefabIndex / totalPrefabs);
                        assetsProcessed++;
                    }

                    foreach (TextMeshProUGUI tmp in tmps)
                    {
                        if (!onlyMissing || tmp.font == null) // Check if font is null (missing) or if we process all
                        {
                            if (tmp.font != defaultFont) // Only change if different
                            {
                                Undo.RecordObject(tmp, "Set TMP Font to Default"); // Register Undo operation
                                tmp.font = defaultFont;
                                componentsModified++;
                                EditorUtility.SetDirty(tmp); // Mark component as dirty (important for prefabs!)
                                prefabChanged = true; // Mark the prefab asset itself dirty later if needed
                                Debug.Log($"Updated font on '{tmp.gameObject.name}' in prefab '{prefab.name}'", prefab);
                            }
                        }
                    }

                    // If any component within the prefab was changed, mark the *prefab asset* dirty.
                    // Sometimes SetDirty(component) is enough, but SetDirty(prefab) is safer.
                    if (prefabChanged)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets(); // Save changes made to prefabs
            Debug.Log($"--- Finished Processing Prefabs ---");
        }

        // --- Final Report ---
        Debug.Log($"Font Restoration Complete. Processed {assetsProcessed} assets/scenes. Modified {componentsModified} TextMeshProUGUI components.");
        EditorUtility.DisplayDialog("Restoration Complete", $"Processed {assetsProcessed} assets/scenes.\nModified {componentsModified} TextMeshProUGUI components.", "OK");
    }
}
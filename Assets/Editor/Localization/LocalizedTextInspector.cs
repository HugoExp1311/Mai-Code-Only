using UnityEngine;
using UnityEditor;
using Base.Localization;
using Base.Localization.Data;
using Base.Localization.EditorTools;
using System.Collections.Generic;
using System.Linq;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Custom inspector for LocalizedText component
    /// Provides validation, key suggestions, and preview functionality
    /// </summary>
    [CustomEditor(typeof(LocalizedText))]
    public class LocalizedTextInspector : UnityEditor.Editor
    {
        private SerializedProperty componentIdProp;
        private SerializedProperty excludeFromLocalizationProp;
        private SerializedProperty tableNameProp;
        private SerializedProperty keyProp;
        
        private string[] availableTables;
        private bool showAdvanced = false;
        private bool showModeSettings = false;
        private int previewLocaleIndex;
        private const string PreviewLocaleSessionKey = "MLS.LocalizedTextInspector.PreviewLocale";
        
        // Preview state for DynamicList mode
        private string previewTemplateKey = "";
        private string previewItemsText = "";
        
        private void OnEnable()
        {
            componentIdProp = serializedObject.FindProperty("componentId");
            excludeFromLocalizationProp = serializedObject.FindProperty("excludeFromLocalization");
            tableNameProp = serializedObject.FindProperty("tableName");
            keyProp = serializedObject.FindProperty("key");
            
            // Get available tables
            RefreshAvailableTables();

            string savedPreviewLocale = SessionState.GetString(PreviewLocaleSessionKey, "en-US");
            previewLocaleIndex = System.Array.IndexOf(RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes, savedPreviewLocale);
            if (previewLocaleIndex < 0)
                previewLocaleIndex = 0;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var localizedText = (LocalizedText)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localized Text Component", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Exclude from localization checkbox
            bool wasExcluded = excludeFromLocalizationProp.boolValue;
            EditorGUILayout.PropertyField(excludeFromLocalizationProp, new GUIContent("Exclude from Localization", "Check this for text that doesn't need localization (e.g., numbers, symbols)"));
            bool isExcluded = excludeFromLocalizationProp.boolValue;
            if (!wasExcluded && isExcluded)
            {
                serializedObject.ApplyModifiedProperties();
                LocalizedTextEditorPreview.Release(localizedText, restoreOriginal: true);
                serializedObject.Update();
            }
            
            // If excluded, show a message and skip the rest
            if (isExcluded)
            {
                EditorGUILayout.HelpBox("This component is excluded from localization. It will not be validated or managed by the localization system.", MessageType.Info);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            EditorGUILayout.Space();
            
            // Component ID
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(componentIdProp, new GUIContent("Component ID", "Unique identifier for this component"));
            if (GUILayout.Button("Generate", GUILayout.Width(70)))
            {
                componentIdProp.stringValue = GenerateComponentId(localizedText);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Domain selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Domain", GUILayout.Width(EditorGUIUtility.labelWidth));
            
            int currentTableIndex = System.Array.IndexOf(availableTables, tableNameProp.stringValue);
            if (currentTableIndex == -1) currentTableIndex = 0;
            
            int newTableIndex = EditorGUILayout.Popup(currentTableIndex, availableTables);
            if (newTableIndex >= 0 && newTableIndex < availableTables.Length)
            {
                tableNameProp.stringValue = availableTables[newTableIndex];
            }
            
            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshAvailableTables();
            }
            EditorGUILayout.EndHorizontal();
            
            // Key field with validation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(keyProp, new GUIContent("Key", "Localization key"));
            
            if (GUILayout.Button("Suggest", GUILayout.Width(70)))
            {
                keyProp.stringValue = SuggestKey(localizedText);
            }
            EditorGUILayout.EndHorizontal();
            
            // Validation
            ValidateAndShowWarnings(localizedText);

            EditorGUILayout.Space();

            DrawEditorPreview(localizedText);

            EditorGUILayout.Space();

            // Mode Settings Section (NEW)
            showModeSettings = EditorGUILayout.Foldout(showModeSettings, "Localization Mode Settings", true);
            if (showModeSettings)
            {
                EditorGUI.indentLevel++;
                DrawModeSettings(localizedText);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Preview and actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
            {
                localizedText.Refresh();
            }
            EditorGUILayout.EndHorizontal();
            
            // Advanced options
            EditorGUILayout.Space();
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                
                // Show current settings
                var settings = localizedText.GetSettings();
                EditorGUILayout.LabelField("Current Settings:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Key: {settings.Key}");
                EditorGUILayout.LabelField($"Domain: {LocalizationDomains.Normalize(settings.TableName)}");
                EditorGUILayout.LabelField($"Mode: {settings.Mode}");
                EditorGUILayout.LabelField($"Is Direct: {settings.IsDirect}");
                EditorGUILayout.LabelField($"Has Pending: {localizedText.HasPendingUpdate}");
                
                if (settings.Variables != null && settings.Variables.Count > 0)
                {
                    EditorGUILayout.LabelField("Variables:");
                    EditorGUI.indentLevel++;
                    foreach (var variable in settings.Variables)
                    {
                        EditorGUILayout.LabelField($"{variable.Key}: {variable.Value}");
                    }
                    EditorGUI.indentLevel--;
                }
                
                // Show DynamicList specific info
                if (settings.Mode == LocalizationMode.DynamicList)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("DynamicList Settings:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Template Key: {settings.TemplateKey ?? "None"}");
                    EditorGUILayout.LabelField($"Separator: {settings.Separator ?? "\\n"}");
                    if (settings.ListData != null)
                    {
                        // Try to cast to list of dictionaries
                        if (settings.ListData is System.Collections.IList list)
                        {
                            EditorGUILayout.LabelField($"List Items: {list.Count}");
                        }
                        else
                        {
                            EditorGUILayout.LabelField("List Data: Present");
                        }
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            serializedObject.ApplyModifiedProperties();
        }        

        private void DrawEditorPreview(LocalizedText localizedText)
        {
            EditorGUILayout.LabelField("Editor Language Preview", EditorStyles.boldLabel);

            string[] localeCodes = RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes;
            if (LocalizedTextEditorPreview.IsActive)
            {
                int activeLocaleIndex = System.Array.IndexOf(localeCodes, LocalizedTextEditorPreview.ActiveLocaleCode);
                if (activeLocaleIndex >= 0)
                    previewLocaleIndex = activeLocaleIndex;
            }

            previewLocaleIndex = Mathf.Clamp(previewLocaleIndex, 0, localeCodes.Length - 1);
            int newLocaleIndex = EditorGUILayout.Popup("Preview Locale", previewLocaleIndex, localeCodes);
            if (newLocaleIndex != previewLocaleIndex)
            {
                previewLocaleIndex = newLocaleIndex;
                SessionState.SetString(PreviewLocaleSessionKey, localeCodes[previewLocaleIndex]);

                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    serializedObject.ApplyModifiedProperties();
                    LocalizedTextEditorPreview.PreviewAll(localeCodes[previewLocaleIndex]);
                    serializedObject.Update();
                }
            }

            string previewLocale = localeCodes[previewLocaleIndex];
            if (LocalizedTextEditorPreview.IsActive)
            {
                EditorGUILayout.HelpBox(
                    $"Scene preview is active for {LocalizedTextEditorPreview.ActiveLocaleCode}. Preview text is restored before Play Mode and scene save.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Changing the preview locale starts an all-scene Edit Mode preview.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Preview"))
                {
                    LocalizedTextEditorPreview.ClearPreview();
                    Debug.Log("[LocalizedTextEditorPreview] Cleared editor localization preview.");
                }

                if (GUILayout.Button("Scan Text Fit"))
                {
                    serializedObject.ApplyModifiedProperties();
                    LocalizedTextEditorPreview.LogFitScanReport(LocalizedTextEditorPreview.ScanTextFit(previewLocale));
                    serializedObject.Update();
                }
                EditorGUILayout.EndHorizontal();
            }

            string domain = GetInspectorDomain();
            DrawLocaleValues(domain, keyProp.stringValue);
            DrawSelectedFitWarning(localizedText, domain, keyProp.stringValue, previewLocale);
        }

        private string GetInspectorDomain()
        {
            return string.IsNullOrEmpty(tableNameProp.stringValue)
                ? LocalizationDomains.UI
                : LocalizationDomains.Normalize(tableNameProp.stringValue);
        }

        private void DrawLocaleValues(string domain, string localizationKey)
        {
            if (string.IsNullOrEmpty(localizationKey))
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("CSV Values", EditorStyles.boldLabel);

            RuntimeCsvLocalizationEditorUtility.TryGetText(localizationKey, domain, "en-US", out string englishText, out _);
            bool hasEnglishPlaceholder = false;

            foreach (string localeCode in RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes)
            {
                if (RuntimeCsvLocalizationEditorUtility.TryGetText(localizationKey, domain, localeCode, out string text, out string usedLocale))
                {
                    string label = usedLocale == localeCode ? localeCode : $"{localeCode} (fallback {usedLocale})";
                    if (localeCode != "en-US" && usedLocale == localeCode && text == englishText)
                    {
                        label += " (matches en-US)";
                        hasEnglishPlaceholder = true;
                    }

                    EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

                    using (new EditorGUI.DisabledScope(true))
                    {
                        float height = Mathf.Clamp(
                            EditorStyles.textArea.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth - 48f),
                            34f,
                            92f);
                        EditorGUILayout.TextArea(text, GUILayout.Height(height));
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"{localeCode}: Missing key '{localizationKey}' in domain '{domain}'.", MessageType.Warning);
                }
            }

            if (hasEnglishPlaceholder)
            {
                EditorGUILayout.HelpBox(
                    "One or more non-English locale files contain the same text as en-US for this key. The preview is loading CSV data correctly, but those placeholder translations will not help validate localized text box sizes.",
                    MessageType.Info);
            }
        }

        private void DrawSelectedFitWarning(LocalizedText localizedText, string domain, string localizationKey, string localeCode)
        {
            if (string.IsNullOrEmpty(localizationKey))
                return;

            if (!RuntimeCsvLocalizationEditorUtility.TryGetText(localizationKey, domain, localeCode, out string text, out string usedLocale))
                return;

            if (!LocalizedTextEditorPreview.TryGetFitResult(localizedText, text, out TextFitResult fit))
                return;

            bool isEnglishPlaceholder = localeCode != "en-US"
                && usedLocale == localeCode
                && RuntimeCsvLocalizationEditorUtility.TryGetText(localizationKey, domain, "en-US", out string englishText, out _)
                && text == englishText;

            string sizeSummary =
                $"Rect {fit.Available.x:0.##}x{fit.Available.y:0.##}, preferred {fit.Preferred.x:0.##}x{fit.Preferred.y:0.##}.";

            if (!fit.Fits)
            {
                EditorGUILayout.HelpBox(
                    $"{usedLocale} preview may not fit this TMP box. {sizeSummary}",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"{usedLocale} preview fits this TMP box. {sizeSummary}", MessageType.None);
            }

            if (isEnglishPlaceholder)
            {
                EditorGUILayout.HelpBox(
                    $"{localeCode} currently matches en-US for this key, so this fit check only validates the English placeholder.",
                    MessageType.Info);
            }
        }

        private void DrawModeSettings(LocalizedText localizedText)
        {
            var settings = localizedText.GetSettings();
            
            // Display current mode (read-only)
            EditorGUILayout.LabelField("Current Mode", settings.Mode.ToString(), EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Show mode-specific information and helpers
            switch (settings.Mode)
            {
                case LocalizationMode.Static:
                    EditorGUILayout.HelpBox(
                        "Static Mode: Simple key lookup with no variables.\n" +
                        "Use SetLocalized(key) to set this mode.",
                        MessageType.Info);
                    break;
                    
                case LocalizationMode.SmartString:
                    EditorGUILayout.HelpBox(
                        "SmartString Mode: Key with variable placeholders (e.g., {amount}).\n" +
                        "Use SetLocalized(key, variables) to set this mode.",
                        MessageType.Info);
                    
                    if (settings.Variables != null && settings.Variables.Count > 0)
                    {
                        EditorGUILayout.LabelField("Current Variables:", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        foreach (var variable in settings.Variables)
                        {
                            EditorGUILayout.LabelField($"{variable.Key}: {variable.Value}");
                        }
                        EditorGUI.indentLevel--;
                    }
                    break;
                    
                case LocalizationMode.DynamicList:
                    EditorGUILayout.HelpBox(
                        "DynamicList Mode: Template with list of items for multi-line dynamic content.\n" +
                        "Use SetLocalizedList() or SetLocalizedTemplate() to set this mode.",
                        MessageType.Info);
                    
                    EditorGUILayout.Space();
                    
                    // Template Key
                    EditorGUILayout.LabelField("Template Key:", settings.TemplateKey ?? "None");
                    
                    // Separator
                    EditorGUILayout.LabelField("Separator:", settings.Separator ?? "\\n");
                    
                    // List Data
                    if (settings.ListData != null)
                    {
                        // Try to cast to list of dictionaries
                        if (settings.ListData is System.Collections.IList list && list.Count > 0)
                        {
                            EditorGUILayout.LabelField($"List Items: {list.Count}", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            
                            int itemIndex = 0;
                            foreach (var item in list)
                            {
                                if (item is Dictionary<string, object> dict)
                                {
                                    EditorGUILayout.LabelField($"Item {itemIndex++}:");
                                    EditorGUI.indentLevel++;
                                    foreach (var variable in dict)
                                    {
                                        EditorGUILayout.LabelField($"{variable.Key}: {variable.Value}");
                                    }
                                    EditorGUI.indentLevel--;
                                }
                            }
                            
                            EditorGUI.indentLevel--;
                        }
                    }
                    
                    EditorGUILayout.Space();
                    
                    // Preview section for DynamicList
                    EditorGUILayout.LabelField("Preview DynamicList", EditorStyles.boldLabel);
                    
                    previewTemplateKey = EditorGUILayout.TextField("Template Key", previewTemplateKey);
                    EditorGUILayout.LabelField("Items (one per line, format: stat=value)");
                    previewItemsText = EditorGUILayout.TextArea(previewItemsText, GUILayout.Height(60));
                    
                    if (GUILayout.Button("Preview List"))
                    {
                        PreviewDynamicList(localizedText);
                    }
                    
                    EditorGUILayout.HelpBox(
                        "Example items:\n" +
                        "Energy=20\n" +
                        "Knowledge=-2\n" +
                        "Mood=5",
                        MessageType.None);
                    
                    break;
                    
                case LocalizationMode.Direct:
                    EditorGUILayout.HelpBox(
                        "Direct Mode: Non-localized direct text (deprecated).\n" +
                        "Consider migrating to one of the localized modes.",
                        MessageType.Warning);
                    break;
            }
        }
        
        private void PreviewDynamicList(LocalizedText localizedText)
        {
            if (string.IsNullOrEmpty(previewTemplateKey))
            {
                EditorUtility.DisplayDialog("Preview", "Please enter a template key.", "OK");
                return;
            }
            
            if (string.IsNullOrEmpty(previewItemsText))
            {
                EditorUtility.DisplayDialog("Preview", "Please enter items to preview.", "OK");
                return;
            }
            
            try
            {
                // Parse items from text
                var lines = previewItemsText.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                var items = new List<Dictionary<string, object>>();
                
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var stat = parts[0].Trim();
                        var valueStr = parts[1].Trim();
                        
                        // Try to parse as int
                        if (int.TryParse(valueStr, out int intValue))
                        {
                            items.Add(new Dictionary<string, object>
                            {
                                { "stat", stat },
                                { "amount", intValue > 0 ? $"+{intValue}" : intValue.ToString() }
                            });
                        }
                        else
                        {
                            items.Add(new Dictionary<string, object>
                            {
                                { "stat", stat },
                                { "amount", valueStr }
                            });
                        }
                    }
                }
                
                if (items.Count == 0)
                {
                    EditorUtility.DisplayDialog("Preview", "No valid items found. Use format: stat=value", "OK");
                    return;
                }
                
                // Use TemplateEngine to render
                var templateEngine = LocalizationManager.Instance.Templates;
                string result = templateEngine.RenderTemplate(previewTemplateKey, items, null, "\n");
                
                EditorUtility.DisplayDialog("Preview", $"Template: {previewTemplateKey}\n\nResult:\n{result}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Preview Error", $"Failed to preview: {ex.Message}", "OK");
            }
        }
        
        private void RefreshAvailableTables()
        {
            availableTables = new[] { LocalizationDomains.UI, LocalizationDomains.Dialogues };
        }
        
        private string GenerateComponentId(LocalizedText localizedText)
        {
            // Build path from root to this GameObject
            string path = localizedText.gameObject.name;
            Transform current = localizedText.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            // Add component index if multiple TMP_Text on same GameObject
            var allTextComponents = localizedText.GetComponents<TMPro.TMP_Text>();
            if (allTextComponents.Length > 1)
            {
                var textComponent = localizedText.GetComponent<TMPro.TMP_Text>();
                int index = System.Array.IndexOf(allTextComponents, textComponent);
                path += $"_{index}";
            }
            
            return path;
        }
        
        private string SuggestKey(LocalizedText localizedText)
        {
            string objectName = localizedText.gameObject.name.ToLower();
            
            // Try to suggest based on object name
            if (objectName.Contains("item"))
            {
                if (objectName.Contains("name"))
                    return "Item_{itemId}_Name";
                if (objectName.Contains("desc"))
                    return "Item_{itemId}_Desc";
            }
            
            if (objectName.Contains("title"))
                return "UI_" + localizedText.gameObject.name + "_Title";
            
            if (objectName.Contains("button"))
                return "UI_" + localizedText.gameObject.name + "_Text";
            
            if (objectName.Contains("dialogue"))
                return "Dialogue_{id}";
            
            // Default suggestion
            return "UI_" + localizedText.gameObject.name + "_Text";
        }
        
        private void ValidateAndShowWarnings(LocalizedText localizedText)
        {
            if (string.IsNullOrEmpty(keyProp.stringValue))
            {
                EditorGUILayout.HelpBox("No localization key specified.", MessageType.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(tableNameProp.stringValue))
            {
                EditorGUILayout.HelpBox("No table specified. Will use default UI table.", MessageType.Info);
            }

            string domain = GetInspectorDomain();
            if (!TableRegistry.TableExists(domain))
            {
                EditorGUILayout.HelpBox($"Domain '{domain}' is not a supported runtime CSV localization domain.", MessageType.Error);
            }
            else if (!RuntimeCsvLocalizationEditorUtility.KeyExists(keyProp.stringValue, domain))
            {
                EditorGUILayout.HelpBox(
                    $"Key '{keyProp.stringValue}' was not found in runtime CSV domain '{domain}' for any supported locale.",
                    MessageType.Warning);
            }
        }

    }
}

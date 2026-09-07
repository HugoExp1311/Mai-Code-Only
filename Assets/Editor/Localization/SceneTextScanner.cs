using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using TMPro;
using Base.Localization;
using Base.Localization.Data;
using Base.Localization.EditorTools;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Utilities for scanning and managing TextMeshPro components in the scene
    /// </summary>
    public static class SceneTextScanner
    {
        /// <summary>
        /// Find all TextMeshPro components that don't have LocalizedText
        /// </summary>
        public static List<TMP_Text> FindUnmanagedTextComponents()
        {
            var allTextComponents = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var unmanagedComponents = new List<TMP_Text>();
            
            foreach (var textComponent in allTextComponents)
            {
                var localizedText = textComponent.GetComponent<LocalizedText>();
                if (localizedText == null)
                {
                    unmanagedComponents.Add(textComponent);
                }
            }
            
            return unmanagedComponents;
        }
        
        /// <summary>
        /// Add LocalizedText component to all unmanaged text components
        /// </summary>
        public static void AddLocalizedTextToAll(List<TMP_Text> components)
        {
            if (components == null || components.Count == 0)
            {
                Debug.Log("[SceneTextScanner] No components to process.");
                return;
            }
            
            int addedCount = 0;
            
            foreach (var textComponent in components)
            {
                if (textComponent != null)
                {
                    var localizedText = textComponent.GetComponent<LocalizedText>();
                    if (localizedText == null)
                    {
                        localizedText = textComponent.gameObject.AddComponent<LocalizedText>();
                        
                        // Auto-suggest key
                        string suggestedKey = SuggestKeysForComponent(textComponent);
                        if (!string.IsNullOrEmpty(suggestedKey))
                        {
                            localizedText.SetLocalized(suggestedKey);
                        }
                        
                        addedCount++;
                    }
                }
            }
            
            Debug.Log($"[SceneTextScanner] Added LocalizedText to {addedCount} components.");
            
            // Mark scene as dirty
            if (addedCount > 0)
            {
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            }
        }
        
        /// <summary>
        /// Validate all LocalizedText components in the scene
        /// </summary>
        public static List<ValidationResult> ValidateAllLocalizedText()
        {
            var results = new List<ValidationResult>();
            var allLocalizedText = Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (var localizedText in allLocalizedText)
            {
                var result = ValidateComponent(localizedText);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Suggest a key for a TextMeshPro component based on its hierarchy and content
        /// </summary>
        public static string SuggestKeysForComponent(TMP_Text component)
        {
            if (component == null) return null;
            
            string objectName = component.gameObject.name.ToLower();
            string parentName = component.transform.parent?.name.ToLower() ?? "";
            
            // Check for common patterns
            if (objectName.Contains("item"))
            {
                if (objectName.Contains("name") || objectName.Contains("title"))
                    return "Item_{itemId}_Name";
                if (objectName.Contains("desc") || objectName.Contains("description"))
                    return "Item_{itemId}_Desc";
                if (objectName.Contains("price") || objectName.Contains("cost"))
                    return "Item_{itemId}_Price";
            }
            
            if (parentName.Contains("shop") || objectName.Contains("shop"))
            {
                if (objectName.Contains("title"))
                    return "UI_Shop_Title";
                if (objectName.Contains("button"))
                    return "UI_Shop_" + objectName.Replace("button", "").Trim() + "_Text";
            }
            
            if (objectName.Contains("dialogue") || parentName.Contains("dialogue"))
            {
                return "Dialogue_{id}";
            }
            
            if (objectName.Contains("button"))
            {
                string buttonName = objectName.Replace("button", "").Trim();
                if (string.IsNullOrEmpty(buttonName))
                    buttonName = "Button";
                return $"UI_{buttonName}_Text";
            }
            
            if (objectName.Contains("title"))
            {
                return $"UI_{component.gameObject.name}_Title";
            }
            
            if (objectName.Contains("label"))
            {
                return $"UI_{component.gameObject.name}_Label";
            }
            
            // Try to use current text content as hint
            if (!string.IsNullOrEmpty(component.text) && component.text.Length < 50)
            {
                string cleanText = component.text.Replace(" ", "_").Replace(":", "").Replace("?", "").Replace("!", "");
                if (KeyHelper.IsValidKey(cleanText))
                {
                    return $"UI_{cleanText}";
                }
            }
            
            // Default suggestion based on hierarchy
            string path = component.gameObject.name;
            if (component.transform.parent != null)
            {
                path = component.transform.parent.name + "_" + path;
            }
            
            return $"UI_{path}_Text";
        }
        
        /// <summary>
        /// Validate a single LocalizedText component
        /// </summary>
        private static ValidationResult ValidateComponent(LocalizedText localizedText)
        {
            // Skip components marked as excluded from localization
            if (localizedText.ExcludeFromLocalization)
            {
                return null;
            }
            
            var settings = localizedText.GetSettings();
            
            // Check for deprecated SetDirect usage
            if (settings.IsDirect || settings.Mode == LocalizationMode.Direct)
            {
                return new ValidationResult
                {
                    Component = localizedText,
                    Issue = ValidationIssue.DeprecatedSetDirect,
                    Message = "Using deprecated Direct mode. Consider migrating to localized mode."
                };
            }
            
            if (string.IsNullOrEmpty(settings.Key))
            {
                return new ValidationResult
                {
                    Component = localizedText,
                    Issue = ValidationIssue.MissingKey,
                    Message = "No localization key specified"
                };
            }
            
            string domain = string.IsNullOrEmpty(settings.TableName)
                ? LocalizationDomains.UI
                : LocalizationDomains.Normalize(settings.TableName);
            
            if (!TableRegistry.TableExists(domain))
            {
                return new ValidationResult
                {
                    Component = localizedText,
                    Issue = ValidationIssue.MissingTable,
                    Message = $"Localization domain '{domain}' not found"
                };
            }
            
            if (!RuntimeCsvLocalizationEditorUtility.KeyExists(settings.Key, domain))
            {
                return new ValidationResult
                {
                    Component = localizedText,
                    Issue = ValidationIssue.MissingEntry,
                    Message = $"Key '{settings.Key}' not found in runtime CSV domain '{domain}'"
                };
            }
            
            // Validate DynamicList mode specific settings
            if (settings.Mode == LocalizationMode.DynamicList)
            {
                if (string.IsNullOrEmpty(settings.TemplateKey) && string.IsNullOrEmpty(settings.TemplatePattern))
                {
                    return new ValidationResult
                    {
                        Component = localizedText,
                        Issue = ValidationIssue.MissingTemplateKey,
                        Message = "DynamicList mode requires a template key or pattern"
                    };
                }
                
                // Validate template pattern if present
                if (!string.IsNullOrEmpty(settings.TemplatePattern))
                {
                    if (!settings.TemplatePattern.Contains("{") || !settings.TemplatePattern.Contains("}"))
                    {
                        return new ValidationResult
                        {
                            Component = localizedText,
                            Issue = ValidationIssue.InvalidTemplatePattern,
                            Message = $"Invalid template pattern: '{settings.TemplatePattern}'. Must contain variable placeholders like {{stat}}"
                        };
                    }
                }
            }
            
            return null; // No issues found
        }
        
        /// <summary>
        /// Create menu items for scanner functions
        /// </summary>
        [MenuItem("Localization/Scan Scene/Find Unmanaged Text Components")]
        public static void MenuFindUnmanagedComponents()
        {
            var components = FindUnmanagedTextComponents();
            
            if (components.Count == 0)
            {
                EditorUtility.DisplayDialog("Scan Complete", "All TextMeshPro components have LocalizedText attached.", "OK");
            }
            else
            {
                string message = $"Found {components.Count} TextMeshPro components without LocalizedText:\n\n";
                for (int i = 0; i < Mathf.Min(components.Count, 10); i++)
                {
                    message += $"• {components[i].gameObject.name}\n";
                }
                
                if (components.Count > 10)
                {
                    message += $"... and {components.Count - 10} more.";
                }
                
                bool addComponents = EditorUtility.DisplayDialog("Unmanaged Components Found", message, "Add LocalizedText to All", "Cancel");
                
                if (addComponents)
                {
                    AddLocalizedTextToAll(components);
                }
            }
        }
        
        [MenuItem("Localization/Scan Scene/Validate All LocalizedText")]
        public static void MenuValidateAllComponents()
        {
            var results = ValidateAllLocalizedText();
            
            if (results.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "All LocalizedText components are valid.", "OK");
            }
            else
            {
                // Group by issue type
                var groupedResults = results.GroupBy(r => r.Issue).OrderBy(g => g.Key);
                
                string message = $"Found {results.Count} validation issues:\n\n";
                
                foreach (var group in groupedResults)
                {
                    message += $"{group.Key} ({group.Count()}):\n";
                    
                    int displayCount = Mathf.Min(group.Count(), 3);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var result = group.ElementAt(i);
                        message += $"  • {result.Component.gameObject.name}: {result.Message}\n";
                    }
                    
                    if (group.Count() > 3)
                    {
                        message += $"  ... and {group.Count() - 3} more.\n";
                    }
                    
                    message += "\n";
                }
                
                EditorUtility.DisplayDialog("Validation Issues Found", message, "OK");
                
                // Log detailed results to console
                Debug.Log($"[SceneTextScanner] Validation complete. Found {results.Count} issues:");
                foreach (var result in results)
                {
                    Debug.LogWarning($"[{result.Issue}] {result.Component.gameObject.name}: {result.Message}", result.Component);
                }
            }
        }
        
        [MenuItem("Localization/Scan Scene/Generate Missing Keys")]
        public static void MenuGenerateMissingKeys()
        {
            var allLocalizedText = Object.FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int updatedCount = 0;
            
            foreach (var localizedText in allLocalizedText)
            {
                var settings = localizedText.GetSettings();
                if (!settings.IsDirect && string.IsNullOrEmpty(settings.Key))
                {
                    var textComponent = localizedText.GetComponent<TMP_Text>();
                    if (textComponent != null)
                    {
                        string suggestedKey = SuggestKeysForComponent(textComponent);
                        if (!string.IsNullOrEmpty(suggestedKey))
                        {
                            localizedText.SetLocalized(suggestedKey);
                            updatedCount++;
                        }
                    }
                }
            }
            
            EditorUtility.DisplayDialog("Key Generation Complete", $"Generated keys for {updatedCount} components.", "OK");
            
            if (updatedCount > 0)
            {
                EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            }
        }
    }
    
    /// <summary>
    /// Result of component validation
    /// </summary>
    public class ValidationResult
    {
        public LocalizedText Component;
        public ValidationIssue Issue;
        public string Message;
    }
    
    /// <summary>
    /// Types of validation issues
    /// </summary>
    public enum ValidationIssue
    {
        MissingKey,
        InvalidKey,
        MissingTable,
        MissingEntry,
        InvalidTemplatePattern,
        DeprecatedSetDirect,
        MissingTemplateKey
    }
}

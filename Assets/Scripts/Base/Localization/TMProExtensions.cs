using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace Base.Localization
{
    /// <summary>
    /// Extension methods for TextMeshPro components
    /// Provides clean, fluent API for localization
    /// </summary>
    public static class TMProExtensions
    {
        /// <summary>
        /// Set localized text using a key
        /// Usage: myText.SetLocalized("Item_milk_Name");
        /// </summary>
        public static void SetLocalized(this TMP_Text text, string key, string tableName = null)
        {
            var localizedText = GetOrAddLocalizedText(text);
            localizedText.SetLocalized(key, tableName);
        }
        
        /// <summary>
        /// Set localized text with variables (Dictionary)
        /// Usage: myText.SetLocalized("Hello_{name}", new Dictionary<string, object> { {"name", "Player"} });
        /// </summary>
        public static void SetLocalized(this TMP_Text text, string key, Dictionary<string, object> variables, string tableName = null)
        {
            var localizedText = GetOrAddLocalizedText(text);
            localizedText.SetLocalized(key, tableName, variables);
        }
        
        /// <summary>
        /// Set localized text with variables (params array)
        /// Usage: myText.SetLocalized("Hello_{name}", ("name", "Player"), ("level", 5));
        /// </summary>
        public static void SetLocalized(this TMP_Text text, string key, params (string key, object value)[] variables)
        {
            var dict = variables.ToDictionary(v => v.key, v => v.value);
            var localizedText = GetOrAddLocalizedText(text);
            localizedText.SetLocalized(key, null, dict);
        }
        
        /// <summary>
        /// Set localized text with table and variables (params array)
        /// </summary>
        public static void SetLocalized(this TMP_Text text, string key, string tableName, params (string key, object value)[] variables)
        {
            var dict = variables.ToDictionary(v => v.key, v => v.value);
            var localizedText = GetOrAddLocalizedText(text);
            localizedText.SetLocalized(key, tableName, dict);
        }
        
        /// <summary>
        /// Set direct (non-localized) text.
        /// Note: This bypasses the localization system. Use only for fallback scenarios.
        /// Usage: myText.SetDirect("Hello World");
        /// </summary>
        public static void SetDirect(this TMP_Text text, string content)
        {
            var localizedText = GetOrAddLocalizedText(text);
            localizedText.SetDirect(content);
        }
        
        /// <summary>
        /// Set localized item name
        /// Usage: myText.SetItemName("milk");
        /// </summary>
        public static void SetItemName(this TMP_Text text, string itemId)
        {
            var key = KeyHelper.ItemName(itemId);
            SetLocalized(text, key);
        }
        
        /// <summary>
        /// Set localized item description
        /// Usage: myText.SetItemDesc("milk");
        /// </summary>
        public static void SetItemDesc(this TMP_Text text, string itemId)
        {
            var key = KeyHelper.ItemDesc(itemId);
            SetLocalized(text, key);
        }
        
        /// <summary>
        /// Set localized action text
        /// Usage: myText.SetActionText("Sleep", "Header");
        /// </summary>
        public static void SetActionText(this TMP_Text text, string actionId, string context)
        {
            var key = KeyHelper.ActionKey(actionId, context);
            SetLocalized(text, key);
        }
        
        /// <summary>
        /// Set localized UI text
        /// Usage: myText.SetUIText("MainMenu", "Title");
        /// </summary>
        public static void SetUIText(this TMP_Text text, string elementId, string property)
        {
            var key = KeyHelper.UIKey(elementId, property);
            SetLocalized(text, key);
        }
        
        /// <summary>
        /// Set localized dialogue text
        /// Usage: myText.SetDialogueText("001");
        /// </summary>
        public static void SetDialogueText(this TMP_Text text, string dialogueId, string tableName = null)
        {
            var key = KeyHelper.DialogueKey(dialogueId);
            
            // Use dialogue table by default
            if (string.IsNullOrEmpty(tableName))
            {
                var config = LocalizationManager.Instance?.Config;
                tableName = config != null ? config.defaultDialogueTable : LocalizationDomains.Dialogues;
            }
            
            SetLocalized(text, key, tableName);
        }
        
        /// <summary>
        /// Refresh the localized text
        /// </summary>
        public static void RefreshLocalization(this TMP_Text text)
        {
            var localizedText = text.GetComponent<LocalizedText>();
            if (localizedText != null)
            {
                localizedText.Refresh();
            }
        }
        
        /// <summary>
        /// Get or add LocalizedText component
        /// </summary>
        // ===== NEW API: Dynamic List Support =====
        
        /// <summary>
        /// Set localized text using a list of items with a template
        /// Example: text.SetLocalizedList("Action Confirm Stat Change", statChanges, s => new Dictionary<string, object> { {"stat", s.Name}, {"amount", s.Value} })
        /// </summary>
        public static void SetLocalizedList<T>(this TMP_Text text, 
            string templateKey, 
            IEnumerable<T> items, 
            Func<T, Dictionary<string, object>> itemToVariables,
            string tableName = null,
            string separator = "\n")
        {
            var localizedText = GetOrAddLocalizedText(text);
            localizedText.SetLocalizedList(templateKey, items, itemToVariables, tableName, separator);
        }
        
        /// <summary>
        /// Set localized text using a list of items with a template (anonymous object overload)
        /// Example: text.SetLocalizedList("Action Confirm Stat Change", statChanges, s => new { stat = s.Name, amount = s.Value })
        /// </summary>
        public static void SetLocalizedList<T>(this TMP_Text text, 
            string templateKey, 
            IEnumerable<T> items, 
            Func<T, object> itemToVariables,
            string tableName = null,
            string separator = "\n")
        {
            var localizedText = GetOrAddLocalizedText(text);
            
            // Convert anonymous object to dictionary
            Func<T, Dictionary<string, object>> converter = (item) =>
            {
                var obj = itemToVariables(item);
                if (obj == null) return new Dictionary<string, object>();
                
                var dict = new Dictionary<string, object>();
                foreach (var prop in obj.GetType().GetProperties())
                {
                    dict[prop.Name] = prop.GetValue(obj);
                }
                return dict;
            };
            
            localizedText.SetLocalizedList(templateKey, items, converter, tableName, separator);
        }

        /// <summary>
        /// Set localized stat changes (convenience for ActionConfirmPopup)
        /// Example: text.SetStatChanges(("Energy", 20), ("Knowledge", -2), ("Mood", 5))
        /// Automatically formats amounts with + or - prefix
        /// </summary>
        public static void SetStatChanges(this TMP_Text text, params (string stat, int amount)[] changes)
        {
            if (changes == null || changes.Length == 0)
            {
                text.text = "";
                return;
            }
            
            var localizedText = GetOrAddLocalizedText(text);
            
            // Convert stat changes to list with formatted amounts
            var statList = changes.Select(change => new
            {
                stat = change.stat,
                amount = change.amount > 0 ? $"+{change.amount}" : change.amount.ToString()
            }).ToList();
            
            // Use SetLocalizedList with the stat change template
            localizedText.SetLocalizedList(
                "Action Confirm Stat Change",
                statList,
                item => new Dictionary<string, object>
                {
                    { "stat", item.stat },
                    { "amount", item.amount }
                },
                tableName: null,
                separator: "\n"
            );
        }
        
        /// <summary>
        /// Set localized stat changes with custom template key
        /// Example: text.SetStatChanges("Custom_Stat_Template", ("Energy", 20), ("Knowledge", -2))
        /// </summary>
        public static void SetStatChanges(this TMP_Text text, string templateKey, params (string stat, int amount)[] changes)
        {
            if (changes == null || changes.Length == 0)
            {
                text.text = "";
                return;
            }
            
            var localizedText = GetOrAddLocalizedText(text);
            
            // Convert stat changes to list with formatted amounts
            var statList = changes.Select(change => new
            {
                stat = change.stat,
                amount = change.amount > 0 ? $"+{change.amount}" : change.amount.ToString()
            }).ToList();
            
            // Use SetLocalizedList with the custom template
            localizedText.SetLocalizedList(
                templateKey,
                statList,
                item => new Dictionary<string, object>
                {
                    { "stat", item.stat },
                    { "amount", item.amount }
                },
                tableName: null,
                separator: "\n"
            );
        }

        /// <summary>
        /// Set localized text with fluent variable builder
        /// Usage: text.SetLocalizedFluent("UI_Status").WithVar("health", 100).WithVar("mana", 50).Apply()
        /// </summary>
        public static LocalizedTextBuilder SetLocalizedFluent(this TMP_Text text, string key)
        {
            return new LocalizedTextBuilder(text, key);
        }

        private static LocalizedText GetOrAddLocalizedText(TMP_Text text)
        {
            var localizedText = text.GetComponent<LocalizedText>();
            if (localizedText == null)
            {
                localizedText = text.gameObject.AddComponent<LocalizedText>();
            }
            return localizedText;
        }
        
        /// <summary>
        /// Set localized text by component ID (static method)
        /// Usage: TMProExtensions.SetLocalizedById("MyPanel/Title", "UI_Title");
        /// </summary>
        public static void SetLocalizedById(string componentId, string key, string tableName = null)
        {
            if (LocalizationManager.Instance == null)
            {
                UnityEngine.Debug.LogWarning($"[TMProExtensions] LocalizationManager not available");
                return;
            }
            
            var component = LocalizationManager.Instance.GetComponentById(componentId);
            if (component != null)
            {
                component.SetLocalized(key, tableName);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[TMProExtensions] Component not found with ID: {componentId}");
            }
        }
        
        /// <summary>
        /// Set localized text by component ID with variables
        /// </summary>
        public static void SetLocalizedById(string componentId, string key, Dictionary<string, object> variables, string tableName = null)
        {
            if (LocalizationManager.Instance == null)
            {
                UnityEngine.Debug.LogWarning($"[TMProExtensions] LocalizationManager not available");
                return;
            }
            
            var component = LocalizationManager.Instance.GetComponentById(componentId);
            if (component != null)
            {
                component.SetLocalized(key, tableName, variables);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[TMProExtensions] Component not found with ID: {componentId}");
            }
        }
    }

    /// <summary>
    /// Fluent builder for localized text with variables
    /// Provides a chainable API for setting localized text with multiple variables
    /// </summary>
    public class LocalizedTextBuilder
    {
        private readonly TMP_Text text;
        private readonly string key;
        private readonly Dictionary<string, object> variables;
        private string tableName;
        
        /// <summary>
        /// Create a new LocalizedTextBuilder
        /// </summary>
        internal LocalizedTextBuilder(TMP_Text text, string key)
        {
            this.text = text;
            this.key = key;
            this.variables = new Dictionary<string, object>();
            this.tableName = null;
        }
        
        /// <summary>
        /// Add a variable to the localized text
        /// Usage: builder.WithVar("health", 100)
        /// </summary>
        public LocalizedTextBuilder WithVar(string name, object value)
        {
            if (!string.IsNullOrEmpty(name))
            {
                variables[name] = value;
            }
            return this;
        }
        
        /// <summary>
        /// Set the table name for the localized text
        /// Usage: builder.WithTable("UI")
        /// </summary>
        public LocalizedTextBuilder WithTable(string tableName)
        {
            this.tableName = tableName;
            return this;
        }
        
        /// <summary>
        /// Apply the localized text with all configured variables
        /// This must be called to actually update the text
        /// </summary>
        public void Apply()
        {
            if (text == null)
            {
                UnityEngine.Debug.LogWarning("[LocalizedTextBuilder] Text component is null");
                return;
            }
            
            if (string.IsNullOrEmpty(key))
            {
                UnityEngine.Debug.LogWarning("[LocalizedTextBuilder] Key is null or empty");
                return;
            }
            
            // Use the extension method to set the localized text
            if (variables.Count > 0)
            {
                text.SetLocalized(key, variables, tableName);
            }
            else
            {
                text.SetLocalized(key, tableName);
            }
        }
    }
}

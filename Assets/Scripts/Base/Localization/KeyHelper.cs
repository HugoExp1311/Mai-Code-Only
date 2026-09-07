using System.Text.RegularExpressions;

namespace Base.Localization
{
    /// <summary>
    /// Helper class for generating and validating localization keys
    /// Provides standardized key patterns for different content types
    /// </summary>
    public static class KeyHelper
    {
        // Key Patterns (using spaces, PascalCase):
        // - Items: "Item {ItemName} Name" and "Item {ItemName} Desc"
        // - Actions: "Action {ActionName}"
        // - UI Elements: "UI {ElementId} {Property}"
        // - Dialogue: "Dialogue {DialogueId}"
        
        /// <summary>
        /// Generate item name key
        /// Pattern: Item {ItemName} Name
        /// Example: Item Milk Name
        /// </summary>
        public static string ItemName(string itemId) => $"Item {ToPascalCase(itemId)} Name";
        
        /// <summary>
        /// Generate item description key
        /// Pattern: Item {ItemName} Desc
        /// Example: Item Milk Desc
        /// </summary>
        public static string ItemDesc(string itemId) => $"Item {ToPascalCase(itemId)} Desc";
        
        /// <summary>
        /// Generate action key
        /// Pattern: Action {ActionName}
        /// Example: Action Sleep
        /// </summary>
        public static string ActionKey(string actionId, string context = null) => 
            string.IsNullOrEmpty(context) ? $"Action {actionId}" : $"Action {actionId} {context}";
        
        /// <summary>
        /// Generate UI element key
        /// Pattern: UI {ElementId} {Property}
        /// Example: UI MainMenu Title
        /// </summary>
        public static string UIKey(string elementId, string property) => $"UI {elementId} {property}";
        
        /// <summary>
        /// Generate dialogue key
        /// Pattern: Dialogue {DialogueId}
        /// Example: Dialogue 001
        /// </summary>
        public static string DialogueKey(string dialogueId) => $"Dialogue {dialogueId}";
        
        /// <summary>
        /// Convert snake_case or kebab-case to PascalCase with spaces
        /// Example: "banh_mi" → "Banh Mi", "energy_drink" → "Energy Drink"
        /// </summary>
        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // Split by underscore or hyphen
            var parts = input.Split('_', '-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
            return string.Join(" ", parts);
        }
        
        /// <summary>
        /// Validate if a key follows proper naming conventions
        /// Keys should contain alphanumeric characters and spaces
        /// </summary>
        public static bool IsValidKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            
            // Allow alphanumeric and spaces
            return Regex.IsMatch(key, @"^[a-zA-Z0-9 ]+$");
        }
        
        /// <summary>
        /// Check if a key matches a specific pattern
        /// Pattern uses * as wildcard
        /// Example: "Item * Name" matches "Item Milk Name"
        /// </summary>
        public static bool MatchesPattern(string key, string pattern)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(pattern))
            {
                return false;
            }
            
            // Convert pattern to regex
            // Escape special regex characters except *
            string regexPattern = Regex.Escape(pattern).Replace("\\*", ".*");
            regexPattern = "^" + regexPattern + "$";
            
            return Regex.IsMatch(key, regexPattern);
        }
        
        /// <summary>
        /// Extract item ID from item name key
        /// Example: "Item Milk Name" → "milk"
        /// </summary>
        public static string ExtractItemId(string itemKey)
        {
            if (string.IsNullOrEmpty(itemKey))
            {
                return null;
            }
            
            var match = Regex.Match(itemKey, @"^Item (.+?) (Name|Desc)$");
            if (match.Success)
            {
                // Convert back to snake_case
                return match.Groups[1].Value.Replace(" ", "_").ToLower();
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if key is an item key
        /// </summary>
        public static bool IsItemKey(string key)
        {
            return MatchesPattern(key, "Item * *");
        }
        
        /// <summary>
        /// Check if key is an action key
        /// </summary>
        public static bool IsActionKey(string key)
        {
            return key != null && key.StartsWith("Action ");
        }
        
        /// <summary>
        /// Check if key is a UI key
        /// </summary>
        public static bool IsUIKey(string key)
        {
            return key != null && key.StartsWith("UI ");
        }
        
        /// <summary>
        /// Check if key is a dialogue key
        /// </summary>
        public static bool IsDialogueKey(string key)
        {
            return key != null && key.StartsWith("Dialogue ");
        }
    }
}

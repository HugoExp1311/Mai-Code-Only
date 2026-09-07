using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Base.Localization
{
    /// <summary>
    /// Template engine for rendering dynamic localized content with variable substitution.
    /// Supports list-based templates and custom separators for multi-line content.
    /// </summary>
    public class TemplateEngine
    {
        private readonly LocalizationManager localizationManager;
        private readonly StringBuilder stringBuilder;
        private readonly StringPool stringPool;
        
        /// <summary>
        /// Initialize the template engine with a reference to the localization manager
        /// </summary>
        /// <param name="manager">LocalizationManager instance for string lookups</param>
        public TemplateEngine(LocalizationManager manager)
        {
            localizationManager = manager ?? throw new ArgumentNullException(nameof(manager));
            stringBuilder = new StringBuilder(256);
            stringPool = new StringPool(200); // Pool for common strings
        }
        
        /// <summary>
        /// Get the string pool for statistics and management
        /// </summary>
        public StringPool Pool => stringPool;
        
        /// <summary>
        /// Render a list of items using a template pattern with variable substitution.
        /// Each item is converted to variables and rendered using the template.
        /// </summary>
        /// <typeparam name="T">Type of items in the list</typeparam>
        /// <param name="templateKey">Localization key for the template (e.g., "ACP_Stat_Change")</param>
        /// <param name="items">List of items to render</param>
        /// <param name="itemToVariables">Function to convert each item to a variable dictionary</param>
        /// <param name="tableName">Table name for localization lookup (optional)</param>
        /// <param name="separator">Separator between rendered items (default: newline)</param>
        /// <returns>Rendered string with all items</returns>
        public string RenderList<T>(
            string templateKey, 
            IEnumerable<T> items, 
            Func<T, Dictionary<string, object>> itemToVariables,
            string tableName = null,
            string separator = "\n")
        {
            if (string.IsNullOrEmpty(templateKey))
            {
                Debug.LogWarning("[TemplateEngine] RenderList: templateKey is null or empty");
                return string.Empty;
            }
            
            if (items == null)
            {
                Debug.LogWarning("[TemplateEngine] RenderList: items is null");
                return string.Empty;
            }
            
            if (itemToVariables == null)
            {
                Debug.LogWarning("[TemplateEngine] RenderList: itemToVariables function is null");
                return string.Empty;
            }
            
            // Clear the string builder for reuse
            stringBuilder.Clear();
            
            bool isFirst = true;
            
            foreach (var item in items)
            {
                if (item == null)
                {
                    continue;
                }
                
                // Add separator before each item except the first
                if (!isFirst && !string.IsNullOrEmpty(separator))
                {
                    stringBuilder.Append(separator);
                }
                
                // Convert item to variables
                Dictionary<string, object> variables;
                try
                {
                    variables = itemToVariables(item);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TemplateEngine] Error converting item to variables: {ex.Message}");
                    continue;
                }
                
                // Get localized template and substitute variables
                string renderedItem = RenderSingleItem(templateKey, variables, tableName);
                stringBuilder.Append(renderedItem);
                
                isFirst = false;
            }
            
            // Pool the final result to reduce allocations for repeated renders
            string result = stringBuilder.ToString();
            return stringPool.GetOrAdd(result);
        }
        
        /// <summary>
        /// Render a single item using a template with variable substitution
        /// </summary>
        /// <param name="templateKey">Localization key for the template</param>
        /// <param name="variables">Variables to substitute in the template</param>
        /// <param name="tableName">Table name for localization lookup (optional)</param>
        /// <returns>Rendered string with variables substituted</returns>
        private string RenderSingleItem(string templateKey, Dictionary<string, object> variables, string tableName)
        {
            if (localizationManager == null)
            {
                Debug.LogError("[TemplateEngine] LocalizationManager is null");
                return $"[{templateKey}]";
            }
            
            // Get the localized template string
            string template = localizationManager.GetLocalizedString(templateKey, tableName, variables);
            
            return template;
        }
        
        /// <summary>
        /// Render multiple variable sets using the same template.
        /// Useful for rendering lists where each item has different variable values.
        /// </summary>
        /// <param name="templateKey">Localization key for the template</param>
        /// <param name="variableSets">Collection of variable dictionaries</param>
        /// <param name="tableName">Table name for localization lookup (optional)</param>
        /// <param name="separator">Separator between rendered items (default: newline)</param>
        /// <returns>Rendered string with all variable sets</returns>
        public string RenderTemplate(
            string templateKey,
            IEnumerable<Dictionary<string, object>> variableSets,
            string tableName = null,
            string separator = "\n")
        {
            if (string.IsNullOrEmpty(templateKey))
            {
                Debug.LogWarning("[TemplateEngine] RenderTemplate: templateKey is null or empty");
                return string.Empty;
            }
            
            if (variableSets == null)
            {
                Debug.LogWarning("[TemplateEngine] RenderTemplate: variableSets is null");
                return string.Empty;
            }
            
            // Clear the string builder for reuse
            stringBuilder.Clear();
            
            bool isFirst = true;
            
            foreach (var variables in variableSets)
            {
                if (variables == null)
                {
                    continue;
                }
                
                // Add separator before each item except the first
                if (!isFirst && !string.IsNullOrEmpty(separator))
                {
                    stringBuilder.Append(separator);
                }
                
                // Render this variable set
                string renderedItem = RenderSingleItem(templateKey, variables, tableName);
                stringBuilder.Append(renderedItem);
                
                isFirst = false;
            }
            
            // Pool the final result to reduce allocations for repeated renders
            string result = stringBuilder.ToString();
            return stringPool.GetOrAdd(result);
        }
        
        /// <summary>
        /// Substitute variables in a template string.
        /// Supports Unity Smart String format: "Hello {name}"
        /// </summary>
        /// <param name="template">Template string with {variable} placeholders</param>
        /// <param name="variables">Variables to substitute</param>
        /// <returns>String with variables substituted</returns>
        public string SubstituteVariables(string template, Dictionary<string, object> variables)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }
            
            if (variables == null || variables.Count == 0)
            {
                return template;
            }
            
            // Use the reusable StringBuilder for efficient string manipulation
            stringBuilder.Clear();
            stringBuilder.Append(template);
            
            foreach (var kvp in variables)
            {
                // Use string.Concat to avoid temporary allocations
                string placeholder = string.Concat("{", kvp.Key, "}");
                string value = kvp.Value?.ToString() ?? string.Empty;
                
                // Replace all occurrences of the placeholder
                stringBuilder.Replace(placeholder, value);
            }
            
            return stringBuilder.ToString();
        }
        
        /// <summary>
        /// Clear the string pool (called on locale change)
        /// </summary>
        public void ClearPool()
        {
            stringPool?.Clear();
        }
        
        /// <summary>
        /// Get string pool statistics
        /// </summary>
        /// <returns>Tuple containing hits, misses, and hit rate</returns>
        public (int hits, int misses, float hitRate) GetPoolStatistics()
        {
            return stringPool?.GetStatistics() ?? (0, 0, 0f);
        }

        /// <summary>
        /// Validation result for template validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; }
            public List<string> Warnings { get; set; }
            
            public ValidationResult()
            {
                Errors = new List<string>();
                Warnings = new List<string>();
                IsValid = true;
            }
        }
        
        /// <summary>
        /// Validate a template pattern and its variables.
        /// Checks for missing keys, invalid variable names, and template syntax.
        /// </summary>
        /// <param name="templateKey">Localization key for the template</param>
        /// <param name="requiredVariables">List of required variable names (optional)</param>
        /// <param name="tableName">Table name for localization lookup (optional)</param>
        /// <returns>ValidationResult with errors and warnings</returns>
        public ValidationResult ValidateTemplate(
            string templateKey, 
            List<string> requiredVariables = null,
            string tableName = null)
        {
            var result = new ValidationResult();
            
            // Validate template key
            if (string.IsNullOrWhiteSpace(templateKey))
            {
                result.IsValid = false;
                result.Errors.Add("Template key is null or empty");
                return result;
            }
            
            // Check if template key exists in localization
            if (localizationManager != null)
            {
                bool keyExists = localizationManager.KeyExists(templateKey, tableName);
                
                if (!keyExists)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Template key '{templateKey}' not found in table '{tableName ?? "default"}'");
                    return result;
                }
                
                // Get the template string to validate its format
                try
                {
                    string template = localizationManager.GetLocalizedString(templateKey, tableName);
                    
                    if (string.IsNullOrEmpty(template))
                    {
                        result.Warnings.Add($"Template key '{templateKey}' exists but returns empty string");
                    }
                    else
                    {
                        // Validate template syntax (check for balanced braces)
                        var syntaxValidation = ValidateTemplateSyntax(template);
                        if (!syntaxValidation.IsValid)
                        {
                            result.IsValid = false;
                            result.Errors.AddRange(syntaxValidation.Errors);
                        }
                        
                        // Check if required variables are present in template
                        if (requiredVariables != null && requiredVariables.Count > 0)
                        {
                            var missingVariables = CheckRequiredVariables(template, requiredVariables);
                            if (missingVariables.Count > 0)
                            {
                                result.Warnings.Add($"Template missing required variables: {string.Join(", ", missingVariables)}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Error validating template: {ex.Message}");
                }
            }
            else
            {
                result.Warnings.Add("LocalizationManager is null, cannot validate template existence");
            }
            
            return result;
        }
        
        /// <summary>
        /// Validate template syntax (check for balanced braces and valid placeholders)
        /// </summary>
        /// <param name="template">Template string to validate</param>
        /// <returns>ValidationResult with syntax errors</returns>
        private ValidationResult ValidateTemplateSyntax(string template)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(template))
            {
                return result;
            }
            
            int openBraces = 0;
            int closeBraces = 0;
            bool inPlaceholder = false;
            int placeholderStart = -1;
            
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                
                if (c == '{')
                {
                    openBraces++;
                    if (inPlaceholder)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Nested braces found at position {i}");
                    }
                    inPlaceholder = true;
                    placeholderStart = i;
                }
                else if (c == '}')
                {
                    closeBraces++;
                    if (!inPlaceholder)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Closing brace without opening brace at position {i}");
                    }
                    else
                    {
                        // Validate placeholder name
                        string placeholderName = template.Substring(placeholderStart + 1, i - placeholderStart - 1);
                        if (string.IsNullOrWhiteSpace(placeholderName))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Empty placeholder at position {placeholderStart}");
                        }
                        else if (!IsValidVariableName(placeholderName))
                        {
                            result.Warnings.Add($"Placeholder '{placeholderName}' contains unusual characters");
                        }
                    }
                    inPlaceholder = false;
                }
            }
            
            // Check for balanced braces
            if (openBraces != closeBraces)
            {
                result.IsValid = false;
                result.Errors.Add($"Unbalanced braces: {openBraces} opening, {closeBraces} closing");
            }
            
            if (inPlaceholder)
            {
                result.IsValid = false;
                result.Errors.Add("Unclosed placeholder at end of template");
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if a variable name is valid (alphanumeric and underscore)
        /// </summary>
        /// <param name="variableName">Variable name to check</param>
        /// <returns>True if valid, false otherwise</returns>
        private bool IsValidVariableName(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
            {
                return false;
            }
            
            // Allow alphanumeric, underscore, and hyphen
            foreach (char c in variableName)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Check which required variables are missing from the template
        /// </summary>
        /// <param name="template">Template string</param>
        /// <param name="requiredVariables">List of required variable names</param>
        /// <returns>List of missing variable names</returns>
        private List<string> CheckRequiredVariables(string template, List<string> requiredVariables)
        {
            var missingVariables = new List<string>();
            
            foreach (var variable in requiredVariables)
            {
                // Use string.Concat to avoid temporary allocations
                string placeholder = string.Concat("{", variable, "}");
                if (!template.Contains(placeholder))
                {
                    missingVariables.Add(variable);
                }
            }
            
            return missingVariables;
        }
        
        /// <summary>
        /// Render a list with fallback behavior for errors.
        /// If rendering fails, returns a fallback string instead of throwing.
        /// </summary>
        /// <typeparam name="T">Type of items in the list</typeparam>
        /// <param name="templateKey">Localization key for the template</param>
        /// <param name="items">List of items to render</param>
        /// <param name="itemToVariables">Function to convert each item to variables</param>
        /// <param name="tableName">Table name for localization lookup (optional)</param>
        /// <param name="separator">Separator between items (default: newline)</param>
        /// <param name="fallbackValue">Fallback value if rendering fails (optional)</param>
        /// <returns>Rendered string or fallback value</returns>
        public string RenderListWithFallback<T>(
            string templateKey,
            IEnumerable<T> items,
            Func<T, Dictionary<string, object>> itemToVariables,
            string tableName = null,
            string separator = "\n",
            string fallbackValue = null)
        {
            try
            {
                // Validate template first
                var validation = ValidateTemplate(templateKey, null, tableName);
                
                if (!validation.IsValid)
                {
                    Debug.LogWarning($"[TemplateEngine] Template validation failed for '{templateKey}': {string.Join(", ", validation.Errors)}");
                    
                    if (!string.IsNullOrEmpty(fallbackValue))
                    {
                        return fallbackValue;
                    }
                    
                    return $"[{templateKey}]";
                }
                
                // Render the list
                return RenderList(templateKey, items, itemToVariables, tableName, separator);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TemplateEngine] Error rendering list with template '{templateKey}': {ex.Message}");
                
                if (!string.IsNullOrEmpty(fallbackValue))
                {
                    return fallbackValue;
                }
                
                return $"[{templateKey}]";
            }
        }
        
        /// <summary>
        /// Render a template with fallback behavior for errors.
        /// If rendering fails, returns a fallback string instead of throwing.
        /// </summary>
        /// <param name="templateKey">Localization key for the template</param>
        /// <param name="variableSets">Collection of variable dictionaries</param>
        /// <param name="tableName">Table name for localization lookup (optional)</param>
        /// <param name="separator">Separator between items (default: newline)</param>
        /// <param name="fallbackValue">Fallback value if rendering fails (optional)</param>
        /// <returns>Rendered string or fallback value</returns>
        public string RenderTemplateWithFallback(
            string templateKey,
            IEnumerable<Dictionary<string, object>> variableSets,
            string tableName = null,
            string separator = "\n",
            string fallbackValue = null)
        {
            try
            {
                // Validate template first
                var validation = ValidateTemplate(templateKey, null, tableName);
                
                if (!validation.IsValid)
                {
                    Debug.LogWarning($"[TemplateEngine] Template validation failed for '{templateKey}': {string.Join(", ", validation.Errors)}");
                    
                    if (!string.IsNullOrEmpty(fallbackValue))
                    {
                        return fallbackValue;
                    }
                    
                    return $"[{templateKey}]";
                }
                
                // Render the template
                return RenderTemplate(templateKey, variableSets, tableName, separator);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TemplateEngine] Error rendering template '{templateKey}': {ex.Message}");
                
                if (!string.IsNullOrEmpty(fallbackValue))
                {
                    return fallbackValue;
                }
                
                return $"[{templateKey}]";
            }
        }
    }
}

using System.Collections.Generic;

namespace Base.Localization.Data
{
    /// <summary>
    /// Defines the mode of localization for a LocalizedText component
    /// </summary>
    public enum LocalizationMode
    {
        /// <summary>
        /// Simple key lookup with no variables
        /// </summary>
        Static,
        
        /// <summary>
        /// Key with variable placeholders (e.g., {amount})
        /// </summary>
        SmartString,
        
        /// <summary>
        /// Template with list of items for multi-line dynamic content
        /// </summary>
        DynamicList,
        
        /// <summary>
        /// Non-localized direct text (for fallback scenarios only)
        /// </summary>
        Direct
    }
    
    /// <summary>
    /// Stores the localization settings for a LocalizedText component
    /// Enhanced with template support and caching configuration
    /// </summary>
    public struct LocalizationSettings
    {
        /// <summary>
        /// Localization key
        /// </summary>
        public string Key;
        
        /// <summary>
        /// CSV localization domain (e.g., "UI", "Dialogues")
        /// </summary>
        public string TableName;
        
        /// <summary>
        /// Variables for SmartString mode
        /// </summary>
        public Dictionary<string, object> Variables;
        
        /// <summary>
        /// Whether this is using direct (non-localized) text
        /// </summary>
        public bool IsDirect;
        
        /// <summary>
        /// Direct content for Direct mode (for fallback scenarios only)
        /// </summary>
        public string DirectContent;
        
        // ===== NEW: Enhanced Fields for Refactor =====
        
        /// <summary>
        /// Current localization mode
        /// </summary>
        public LocalizationMode Mode;
        
        /// <summary>
        /// Template pattern for DynamicList mode (e.g., "ACP_Stat_{stat}")
        /// </summary>
        public string TemplatePattern;
        
        /// <summary>
        /// Template key for DynamicList mode (e.g., "ACP_Stat_Change")
        /// </summary>
        public string TemplateKey;
        
        /// <summary>
        /// Separator for multi-line content (default: newline)
        /// </summary>
        public string Separator;
        
        /// <summary>
        /// Stored data for DynamicList mode (for locale change handling)
        /// </summary>
        public object ListData;
        
        /// <summary>
        /// Enable caching for this component
        /// </summary>
        public bool EnableCaching;
        
        /// <summary>
        /// Cache duration in seconds (0 = cache until locale change)
        /// </summary>
        public float CacheDuration;
    }
}

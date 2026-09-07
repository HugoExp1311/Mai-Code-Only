using UnityEngine;
using UnityEngine.Serialization;

namespace Base.Localization
{
    /// <summary>
    /// Configuration for the Dynamic Localization System
    /// Create this asset in Resources folder for system-wide settings
    /// </summary>
    /// <summary>
    /// Behavior when a localization key is missing
    /// </summary>
    public enum MissingKeyBehavior
    {
        /// <summary>Display the key name in brackets: [Key_Name]</summary>
        ShowKey,
        
        /// <summary>Display a placeholder: ???</summary>
        ShowPlaceholder,
        
        /// <summary>Display empty string</summary>
        ShowEmpty,
        
        /// <summary>Log warning to console and show key</summary>
        LogWarning,
        
        /// <summary>Throw exception (for development/testing)</summary>
        ThrowException
    }

    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Localization/Config")]
    public class LocalizationConfig : ScriptableObject
    {
        [Header("Default Domains")]
        [Tooltip("Default CSV domain for UI elements (menus, buttons, items, etc.)")]
        [FormerlySerializedAs("defaultUITable")]
        public string defaultUITable = LocalizationDomains.UI;
        
        [Tooltip("Default CSV domain for dialogue and narrative text")]
        [FormerlySerializedAs("defaultDialogueTable")]
        public string defaultDialogueTable = LocalizationDomains.Dialogues;
        
        [Header("Fallback Chain")]
        [Tooltip("Languages to fall back to when a key is missing (in order)")]
        public SystemLanguage[] fallbackChain = { SystemLanguage.English };
        
        [Header("Performance")]
        [Tooltip("Keep runtime CSV domains loaded in memory for instant access")]
        public bool keepAllTablesLoaded = true;
        
        [Tooltip("Maximum number of operations to queue per domain")]
        public int maxQueueSize = 100;
        
        [Tooltip("Use frame-spread updates during locale changes to prevent UI freezing")]
        public bool useFrameSpreadUpdates = false;
        
        [Tooltip("Number of components to update per frame during frame-spread updates")]
        public int componentsPerFrame = 50;
        
        [Header("Debug")]
        [Tooltip("Enable verbose logging of all localization operations")]
        public bool debugMode = false;
        
        [Tooltip("Log warnings when localization keys are missing")]
        public bool logMissingKeys = true;
        
        [Tooltip("Log when falling back to English or other languages")]
        public bool logFallbacks = true;
        
        [Header("Error Handling")]
        [Tooltip("Behavior when a localization key is missing")]
        public MissingKeyBehavior missingKeyBehavior = MissingKeyBehavior.ShowKey;
        
        [Header("Editor Automation")]
        [Tooltip("Automatically offer to add LocalizedText when TextMeshPro is added")]
        public bool autoSuggestLocalizedText = true;
        
        [Tooltip("Automatically suggest keys based on GameObject hierarchy")]
        public bool autoSuggestKeys = true;
        
        [Tooltip("Validate localization on scene save")]
        public bool validateOnSave = true;
        
        [Tooltip("Automatically reload tables when localization assets change")]
        public bool autoReloadOnAssetChange = false;
    }
}

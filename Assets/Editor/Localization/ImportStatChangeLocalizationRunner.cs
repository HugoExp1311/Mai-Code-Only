using UnityEditor;
using UnityEngine;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Automated runner for importing stat change localization
    /// This can be called from command line or other scripts
    /// </summary>
    [InitializeOnLoad]
    public static class ImportStatChangeLocalizationRunner
    {
        // This will run automatically when Unity compiles scripts
        // Comment out the static constructor if you don't want auto-import
        /*
        static ImportStatChangeLocalizationRunner()
        {
            // Run import on next editor update
            EditorApplication.delayCall += () =>
            {
                ImportStatChangeLocalization.ImportStatChangeTemplates();
            };
        }
        */
        
        /// <summary>
        /// Manual import method that can be called from other scripts
        /// </summary>
        public static void RunImport()
        {
            ImportStatChangeLocalization.ImportStatChangeTemplates();
        }
    }
}

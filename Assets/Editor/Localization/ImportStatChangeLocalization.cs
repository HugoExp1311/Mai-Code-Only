using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Imports stat change template localization into runtime UI locale CSVs.
    /// Expected source format: Key,English,Vietnamese,Japanese.
    /// </summary>
    public static class ImportStatChangeLocalization
    {
        private const string DefaultCsvPath = "Assets/Resources/ACP_StatChange_Localization.csv";

        [MenuItem("Localization/UI CSV/Import Stat Change Templates")]
        public static void ImportStatChangeTemplates()
        {
            if (!File.Exists(DefaultCsvPath))
            {
                Debug.LogError($"[StatChangeImport] CSV file not found: {DefaultCsvPath}");
                EditorUtility.DisplayDialog("Error", $"File not found:\n{DefaultCsvPath}", "OK");
                return;
            }

            CSVExporter.ImportMultiLocaleCsv(LocalizationDomains.UI, DefaultCsvPath);
            EditorUtility.DisplayDialog(
                "Import Complete",
                "Stat change templates were imported into Assets/Resources/Localization/UI/{locale}.csv.",
                "OK");
        }
    }
}

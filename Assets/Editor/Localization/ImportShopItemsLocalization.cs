using System.IO;
using UnityEditor;
using UnityEngine;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Imports legacy shop/UI multi-locale CSVs into runtime UI locale files.
    /// Expected source format: Key,English,Vietnamese,Japanese.
    /// </summary>
    public static class UITableCSVManager
    {
        private const string DefaultLegacyCsvPath = "Assets/Resources/ShopItems_Localization.csv";

        [MenuItem("Localization/UI CSV/Import Shop Items")]
        public static void ImportShopItems()
        {
            if (!File.Exists(DefaultLegacyCsvPath))
            {
                Debug.LogError($"[UITableCSV] CSV file not found: {DefaultLegacyCsvPath}");
                EditorUtility.DisplayDialog("Error", $"File not found:\n{DefaultLegacyCsvPath}", "OK");
                return;
            }

            CSVExporter.ImportMultiLocaleCsv(LocalizationDomains.UI, DefaultLegacyCsvPath);
            EditorUtility.DisplayDialog(
                "Import Complete",
                "Shop item localization was imported into Assets/Resources/Localization/UI/{locale}.csv.",
                "OK");
        }

        [MenuItem("Localization/UI CSV/Import Multi-Locale CSV")]
        public static void ImportUITableFromCSV()
        {
            string path = EditorUtility.OpenFilePanel("Import Multi-Locale CSV to UI", "Assets/Resources", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            CSVExporter.ImportMultiLocaleCsv(LocalizationDomains.UI, path);
            EditorUtility.DisplayDialog("Import Complete", "UI runtime CSV files were updated.", "OK");
        }

        [MenuItem("Localization/UI CSV/Export Combined CSV")]
        public static void ExportUITableToCSV()
        {
            string path = EditorUtility.SaveFilePanel("Export UI Runtime CSV", "Assets/Resources", "UI_Localization.csv", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            CSVExporter.ExportDomain(LocalizationDomains.UI, path);
            EditorUtility.DisplayDialog("Export Complete", $"UI localization exported to:\n{path}", "OK");
        }
    }
}

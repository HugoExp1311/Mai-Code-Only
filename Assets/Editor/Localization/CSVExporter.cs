using System.Collections.Generic;
using System.IO;
using System.Text;
using Base.Localization.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Base.Localization.Editor
{
    /// <summary>
    /// CSV-only localization import/export utilities.
    /// Runtime locale files use Key,Text format at Assets/Resources/Localization/{Domain}/{Locale}.csv.
    /// </summary>
    public static class CSVExporter
    {
        private static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

        public static void ExportDomain(string domain, string outputPath)
        {
            var localeEntries = new Dictionary<string, Dictionary<string, string>>();
            var allKeys = new SortedSet<string>();

            foreach (string localeCode in RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes)
            {
                var entries = RuntimeCsvLocalizationEditorUtility.LoadDomain(domain, localeCode);
                localeEntries[localeCode] = entries;

                foreach (string key in entries.Keys)
                    allKeys.Add(key);
            }

            var csv = new StringBuilder();
            csv.AppendLine("Key,English,Vietnamese,Japanese");

            foreach (string key in allKeys)
            {
                localeEntries["en-US"].TryGetValue(key, out string english);
                localeEntries["vi-VN"].TryGetValue(key, out string vietnamese);
                localeEntries["ja-JP"].TryGetValue(key, out string japanese);

                csv.AppendLine($"{EscapeCSV(key)},{EscapeCSV(english)},{EscapeCSV(vietnamese)},{EscapeCSV(japanese)}");
            }

            File.WriteAllText(outputPath, csv.ToString(), Utf8WithBom);
            Debug.Log($"[CSVExporter] Exported {allKeys.Count} {domain} keys to {outputPath}");
        }

        public static void ImportMultiLocaleCsv(string domain, string inputPath)
        {
            if (!ValidateCSV(inputPath, out string errorMessage))
            {
                Debug.LogError($"[CSVExporter] CSV validation failed: {errorMessage}");
                return;
            }

            var localeData = new Dictionary<string, Dictionary<string, string>>
            {
                { "en-US", new Dictionary<string, string>() },
                { "vi-VN", new Dictionary<string, string>() },
                { "ja-JP", new Dictionary<string, string>() }
            };

            string[] lines = File.ReadAllLines(inputPath, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] cells = ParseCSVLine(lines[i]);
                if (cells.Length < 2)
                    continue;

                string key = cells[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                localeData["en-US"][key] = cells.Length > 1 ? cells[1] : string.Empty;
                localeData["vi-VN"][key] = cells.Length > 2 ? cells[2] : string.Empty;
                localeData["ja-JP"][key] = cells.Length > 3 ? cells[3] : string.Empty;
            }

            foreach (var locale in localeData)
                WriteLocaleFile(domain, locale.Key, locale.Value);

            AssetDatabase.Refresh();
            Debug.Log($"[CSVExporter] Imported {localeData["en-US"].Count} keys into runtime {domain} locale CSVs.");
        }

        public static bool ValidateCSV(string inputPath, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!File.Exists(inputPath))
            {
                errorMessage = "File not found.";
                return false;
            }

            string[] lines = File.ReadAllLines(inputPath, Encoding.UTF8);
            if (lines.Length < 2)
            {
                errorMessage = "CSV file must have a header row and at least one data row.";
                return false;
            }

            string[] header = ParseCSVLine(lines[0]);
            if (header.Length == 0 || !header[0].Trim().Equals("Key", System.StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "First CSV column must be Key.";
                return false;
            }

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] cells = ParseCSVLine(lines[i]);
                if (cells.Length == 0 || string.IsNullOrWhiteSpace(cells[0]))
                {
                    errorMessage = $"Line {i + 1}: Key is missing.";
                    return false;
                }
            }

            return true;
        }

        private static void WriteLocaleFile(string domain, string localeCode, Dictionary<string, string> entries)
        {
            string path = RuntimeCsvLocalizationEditorUtility.GetCsvPath(domain, localeCode);
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var csv = new StringBuilder();
            csv.AppendLine("Key,Text");

            foreach (var entry in entries)
                csv.AppendLine($"{EscapeCSV(entry.Key)},{EscapeCSV(entry.Value)}");

            File.WriteAllText(path, csv.ToString(), Utf8WithBom);
        }

        private static string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }

        private static string[] ParseCSVLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values.ToArray();
        }

        [MenuItem("Localization/CSV/Export UI Domain")]
        public static void MenuExportUIDomain()
        {
            string path = EditorUtility.SaveFilePanel("Export UI Domain", Application.dataPath, "UI_Localization.csv", "csv");
            if (!string.IsNullOrEmpty(path))
                ExportDomain(LocalizationDomains.UI, path);
        }

        [MenuItem("Localization/CSV/Export Dialogues Domain")]
        public static void MenuExportDialoguesDomain()
        {
            string path = EditorUtility.SaveFilePanel("Export Dialogues Domain", Application.dataPath, "Dialogues_Localization.csv", "csv");
            if (!string.IsNullOrEmpty(path))
                ExportDomain(LocalizationDomains.Dialogues, path);
        }

        [MenuItem("Localization/CSV/Import Multi-Locale CSV to UI")]
        public static void MenuImportUI()
        {
            string path = EditorUtility.OpenFilePanel("Import Multi-Locale CSV to UI", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
                ImportMultiLocaleCsv(LocalizationDomains.UI, path);
        }

        [MenuItem("Localization/CSV/Import Multi-Locale CSV to Dialogues")]
        public static void MenuImportDialogues()
        {
            string path = EditorUtility.OpenFilePanel("Import Multi-Locale CSV to Dialogues", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
                ImportMultiLocaleCsv(LocalizationDomains.Dialogues, path);
        }
    }
}

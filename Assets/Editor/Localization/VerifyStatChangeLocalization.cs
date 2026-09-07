using System.Collections.Generic;
using System.Text;
using Base.Localization.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Base.Localization.Editor
{
    /// <summary>
    /// Verifies that stat change localization entries exist in runtime UI locale CSVs.
    /// </summary>
    public static class VerifyStatChangeLocalization
    {
        [MenuItem("Localization/UI CSV/Verify Stat Change Templates")]
        public static void VerifyStatChangeTemplates()
        {
            var requiredKeys = new List<string>
            {
                "ACP_Stat_Change",
                "ACP_Stat_Energy",
                "ACP_Stat_Knowledge",
                "ACP_Stat_Love",
                "ACP_Stat_Charming",
                "ACP_Stat_Money",
                "ACP_Stat_MaxEnergy"
            };

            bool allFound = true;
            var results = new StringBuilder();
            results.AppendLine("=== Runtime CSV Stat Change Verification ===");

            foreach (string localeCode in RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes)
            {
                results.AppendLine();
                results.AppendLine($"--- {localeCode} ---");
                var entries = RuntimeCsvLocalizationEditorUtility.LoadDomain(LocalizationDomains.UI, localeCode);

                if (entries.Count == 0)
                {
                    results.AppendLine($"ERROR: Runtime UI CSV not found or empty for {localeCode}.");
                    allFound = false;
                    continue;
                }

                foreach (string key in requiredKeys)
                {
                    if (entries.TryGetValue(key, out string value))
                    {
                        results.AppendLine($"  OK: {key} = \"{value}\"");
                    }
                    else
                    {
                        results.AppendLine($"  MISSING: {key}");
                        allFound = false;
                    }
                }
            }

            if (allFound)
            {
                Debug.Log(results.ToString());
                EditorUtility.DisplayDialog(
                    "Verification Success",
                    "All stat change templates are present in all runtime UI locale CSVs.",
                    "OK");
            }
            else
            {
                Debug.LogWarning(results.ToString());
                EditorUtility.DisplayDialog(
                    "Verification Failed",
                    "Some stat change templates are missing. Check the Console for details.",
                    "OK");
            }
        }
    }
}

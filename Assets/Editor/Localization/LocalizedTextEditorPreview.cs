using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base.Localization.EditorTools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Base.Localization.Editor
{
    [InitializeOnLoad]
    public static class LocalizedTextEditorPreview
    {
        private const string SessionPrefix = "MLS.LocalizedTextEditorPreview.";
        private const string ActiveLocaleSessionKey = SessionPrefix + "ActiveLocale";
        private const string OriginalIdsSessionKey = SessionPrefix + "OriginalIds";
        private const string OriginalTextSessionKeyPrefix = SessionPrefix + "OriginalText.";

        private static readonly Dictionary<string, string> originalTextById = new Dictionary<string, string>();
        private static bool sessionLoaded;

        public static string ActiveLocaleCode => SessionState.GetString(ActiveLocaleSessionKey, string.Empty);
        public static bool IsActive => !string.IsNullOrEmpty(ActiveLocaleCode);

        static LocalizedTextEditorPreview()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += ClearPreview;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        [MenuItem("Localization/Editor Preview/Preview en-US")]
        private static void PreviewEnglish()
        {
            LogPreviewReport(PreviewAll("en-US"));
        }

        [MenuItem("Localization/Editor Preview/Preview vi-VN")]
        private static void PreviewVietnamese()
        {
            LogPreviewReport(PreviewAll("vi-VN"));
        }

        [MenuItem("Localization/Editor Preview/Preview ja-JP")]
        private static void PreviewJapanese()
        {
            LogPreviewReport(PreviewAll("ja-JP"));
        }

        [MenuItem("Localization/Editor Preview/Clear Preview")]
        public static void ClearPreviewMenu()
        {
            ClearPreview();
            Debug.Log("[LocalizedTextEditorPreview] Cleared editor localization preview.");
        }

        [MenuItem("Localization/Editor Preview/Scan Text Fit")]
        private static void ScanTextFitMenu()
        {
            string localeCode = IsActive ? ActiveLocaleCode : "en-US";
            LogFitScanReport(ScanTextFit(localeCode));
        }

        public static PreviewReport PreviewAll(string localeCode)
        {
            return Preview(localeCode, GetSceneLocalizedTexts(), false);
        }

        public static PreviewReport PreviewSelected(string localeCode)
        {
            return Preview(localeCode, GetSelectedLocalizedTexts(), false);
        }

        public static PreviewReport PreviewSelected(LocalizedText localizedText, string localeCode)
        {
            return Preview(localeCode, new[] { localizedText }, false);
        }

        public static void ClearPreview()
        {
            RestoreOriginals(true);
            RuntimeCsvLocalizationEditorUtility.ClearCache();
            QueueRepaint();
        }

        public static void Release(LocalizedText localizedText, bool restoreOriginal = true)
        {
            EnsureSessionLoaded();

            if (!TryGetObjectId(localizedText, out string id))
                return;

            if (restoreOriginal)
                RestoreOriginal(id);

            RemoveTrackedOriginal(id);
            SaveOriginalsToSession();
            QueueRepaint();
        }

        public static bool TryGetLocalizedText(
            LocalizedText localizedText,
            string localeCode,
            out string text,
            out string usedLocale,
            out string reason)
        {
            text = null;
            usedLocale = null;
            reason = null;

            if (!CanPreview(localizedText, out reason))
                return false;

            string domain = localizedText.EditorPreviewDomain;
            if (!RuntimeCsvLocalizationEditorUtility.TryGetText(localizedText.Key, domain, NormalizeLocaleCode(localeCode), out text, out usedLocale))
            {
                reason = $"Missing key '{localizedText.Key}' in domain '{domain}' for locale '{NormalizeLocaleCode(localeCode)}'.";
                return false;
            }

            return true;
        }

        public static bool TryGetFitResult(LocalizedText localizedText, string text, out TextFitResult result)
        {
            result = default;

            if (localizedText == null)
                return false;

            TMP_Text tmpText = localizedText.EditorTextComponent;
            if (tmpText == null || tmpText.rectTransform == null)
                return false;

            Rect rect = tmpText.rectTransform.rect;
            var available = new Vector2(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
            if (available.x <= 0.1f || available.y <= 0.1f)
                return false;

            string previewText = text ?? string.Empty;
            bool wrapsText = tmpText.textWrappingMode != TextWrappingModes.NoWrap;
            Vector2 preferred = wrapsText
                ? tmpText.GetPreferredValues(previewText, available.x, 0f)
                : tmpText.GetPreferredValues(previewText, 0f, 0f);

            const float tolerance = 1.5f;
            bool widthOverflow = !wrapsText && preferred.x > available.x + tolerance;
            bool heightOverflow = preferred.y > available.y + tolerance;

            result = new TextFitResult
            {
                Available = available,
                Preferred = preferred,
                WidthOverflow = widthOverflow,
                HeightOverflow = heightOverflow
            };
            return true;
        }

        public static FitScanReport ScanTextFit(string localeCode)
        {
            string normalizedLocale = NormalizeLocaleCode(localeCode);
            var report = new FitScanReport { LocaleCode = normalizedLocale };

            foreach (LocalizedText localizedText in GetSceneLocalizedTexts())
            {
                report.CandidateCount++;

                if (!CanPreview(localizedText, out string reason))
                {
                    report.Skipped.Add($"{GetHierarchyPath(localizedText)} | {reason}");
                    continue;
                }

                if (!TryGetLocalizedText(localizedText, normalizedLocale, out string text, out string usedLocale, out reason))
                {
                    report.Missing.Add($"{GetHierarchyPath(localizedText)} | {reason}");
                    continue;
                }

                if (!TryGetFitResult(localizedText, text, out TextFitResult fitResult))
                {
                    report.Skipped.Add($"{GetHierarchyPath(localizedText)} | No usable RectTransform text box.");
                    continue;
                }

                report.CheckedCount++;
                if (!fitResult.Fits)
                {
                    report.Oversized.Add(new OversizedTextInfo
                    {
                        Component = localizedText,
                        Path = GetHierarchyPath(localizedText),
                        Domain = localizedText.EditorPreviewDomain,
                        Key = localizedText.Key,
                        RequestedLocale = normalizedLocale,
                        UsedLocale = usedLocale,
                        Fit = fitResult
                    });
                }
            }

            if (report.Oversized.Count > 0)
            {
                Selection.objects = report.Oversized
                    .Select(item => item.Component.gameObject)
                    .Distinct()
                    .Cast<UnityEngine.Object>()
                    .ToArray();
            }

            return report;
        }

        private static PreviewReport Preview(string localeCode, IEnumerable<LocalizedText> components, bool clearExisting)
        {
            string normalizedLocale = NormalizeLocaleCode(localeCode);
            var report = new PreviewReport { LocaleCode = normalizedLocale };

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.Skipped.Add("Editor preview is only available in Edit Mode.");
                return report;
            }

            if (clearExisting)
                RestoreOriginals(true);

            EnsureSessionLoaded();
            RuntimeCsvLocalizationEditorUtility.ClearCache();

            foreach (LocalizedText localizedText in DistinctComponents(components))
            {
                report.CandidateCount++;

                if (!CanPreview(localizedText, out string reason))
                {
                    if (IsActive && TryGetObjectId(localizedText, out string skippedId))
                    {
                        RestoreOriginal(skippedId);
                    }

                    report.Skipped.Add($"{GetHierarchyPath(localizedText)} | {reason}");
                    continue;
                }

                if (!TryGetLocalizedText(localizedText, normalizedLocale, out string text, out string usedLocale, out reason))
                {
                    if (TryGetObjectId(localizedText, out string missingId))
                        RestoreOriginal(missingId);

                    report.Missing.Add($"{GetHierarchyPath(localizedText)} | {reason}");
                    continue;
                }

                TMP_Text tmpText = localizedText.EditorTextComponent;
                if (tmpText == null)
                {
                    report.Skipped.Add($"{GetHierarchyPath(localizedText)} | No TMP_Text component.");
                    continue;
                }

                if (!TryGetObjectId(localizedText, out string id))
                {
                    report.Skipped.Add($"{GetHierarchyPath(localizedText)} | Could not resolve a stable editor object id.");
                    continue;
                }

                if (!originalTextById.ContainsKey(id))
                    originalTextById[id] = tmpText.text ?? string.Empty;

                report.PreviewedCount++;
                if (tmpText.text == text)
                {
                    report.UnchangedCount++;
                    continue;
                }

                tmpText.text = text;
                report.Changed.Add($"{GetHierarchyPath(localizedText)} | {localizedText.EditorPreviewDomain}/{localizedText.Key} ({usedLocale})");
            }

            if (report.PreviewedCount > 0)
            {
                SessionState.SetString(ActiveLocaleSessionKey, normalizedLocale);
                SaveOriginalsToSession();
            }
            else if (clearExisting)
            {
                ClearStoredOriginals();
            }

            QueueRepaint();
            return report;
        }

        private static void RestoreOriginal(string id)
        {
            if (string.IsNullOrEmpty(id) || !originalTextById.TryGetValue(id, out string originalText))
                return;

            LocalizedText localizedText = ResolveLocalizedText(id);
            if (localizedText == null)
                return;

            TMP_Text tmpText = localizedText.EditorTextComponent;
            if (tmpText != null && tmpText.text != originalText)
                tmpText.text = originalText ?? string.Empty;
        }

        private static void RestoreOriginals(bool clearState)
        {
            EnsureSessionLoaded();

            foreach (KeyValuePair<string, string> entry in originalTextById.ToList())
            {
                LocalizedText localizedText = ResolveLocalizedText(entry.Key);
                if (localizedText == null)
                {
                    RemoveTrackedOriginal(entry.Key);
                    continue;
                }

                TMP_Text tmpText = localizedText.EditorTextComponent;
                if (tmpText != null && tmpText.text != entry.Value)
                    tmpText.text = entry.Value ?? string.Empty;
            }

            if (clearState)
                ClearStoredOriginals();
        }

        private static void ClearStoredOriginals()
        {
            EnsureSessionLoaded();

            foreach (string id in originalTextById.Keys.ToList())
                SessionState.EraseString(GetOriginalTextSessionKey(id));

            originalTextById.Clear();
            SessionState.EraseString(OriginalIdsSessionKey);
            SessionState.EraseString(ActiveLocaleSessionKey);
        }

        private static void RemoveTrackedOriginal(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            originalTextById.Remove(id);
            SessionState.EraseString(GetOriginalTextSessionKey(id));
        }

        private static void SaveOriginalsToSession()
        {
            EnsureSessionLoaded();

            if (originalTextById.Count == 0)
            {
                SessionState.EraseString(OriginalIdsSessionKey);
                SessionState.EraseString(ActiveLocaleSessionKey);
                return;
            }

            SessionState.SetString(OriginalIdsSessionKey, string.Join("\n", originalTextById.Keys));
            foreach (KeyValuePair<string, string> entry in originalTextById)
                SessionState.SetString(GetOriginalTextSessionKey(entry.Key), entry.Value ?? string.Empty);
        }

        private static void EnsureSessionLoaded()
        {
            if (sessionLoaded)
                return;

            sessionLoaded = true;
            originalTextById.Clear();

            string ids = SessionState.GetString(OriginalIdsSessionKey, string.Empty);
            if (string.IsNullOrEmpty(ids))
                return;

            foreach (string id in ids.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                originalTextById[id] = SessionState.GetString(GetOriginalTextSessionKey(id), string.Empty);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ClearPreview();
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (!IsActive)
                return;

            ClearPreview();
        }

        private static bool CanPreview(LocalizedText localizedText, out string reason)
        {
            reason = null;

            if (localizedText == null)
            {
                reason = "Component is null.";
                return false;
            }

            if (EditorUtility.IsPersistent(localizedText))
            {
                reason = "Persistent prefab/asset object is skipped.";
                return false;
            }

            if (!localizedText.gameObject.scene.IsValid() || !localizedText.gameObject.scene.isLoaded)
            {
                reason = "Object is not in a loaded scene.";
                return false;
            }

            if (!localizedText.EditorCanPreview)
            {
                reason = localizedText.ExcludeFromLocalization
                    ? "Excluded from localization."
                    : "No localization key.";
                return false;
            }

            string domain = localizedText.EditorPreviewDomain;
            if (!TableRegistry.TableExists(domain))
            {
                reason = $"Unsupported localization domain '{domain}'.";
                return false;
            }

            if (localizedText.EditorTextComponent == null)
            {
                reason = "No TMP_Text component.";
                return false;
            }

            return true;
        }

        private static IEnumerable<LocalizedText> GetSceneLocalizedTexts()
        {
            return Resources.FindObjectsOfTypeAll<LocalizedText>()
                .Where(localizedText =>
                    localizedText != null &&
                    !EditorUtility.IsPersistent(localizedText) &&
                    localizedText.gameObject.scene.IsValid() &&
                    localizedText.gameObject.scene.isLoaded);
        }

        private static IEnumerable<LocalizedText> GetSelectedLocalizedTexts()
        {
            var selected = new List<LocalizedText>();
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (obj is LocalizedText localizedText)
                {
                    selected.Add(localizedText);
                    continue;
                }

                if (obj is Component component)
                {
                    selected.AddRange(component.GetComponentsInChildren<LocalizedText>(true));
                    continue;
                }

                if (obj is GameObject gameObject)
                    selected.AddRange(gameObject.GetComponentsInChildren<LocalizedText>(true));
            }

            return selected;
        }

        private static IEnumerable<LocalizedText> DistinctComponents(IEnumerable<LocalizedText> components)
        {
            if (components == null)
                yield break;

            var seen = new HashSet<int>();
            foreach (LocalizedText component in components)
            {
                if (component == null)
                    continue;

                if (seen.Add(component.GetInstanceID()))
                    yield return component;
            }
        }

        private static bool TryGetObjectId(LocalizedText localizedText, out string id)
        {
            id = null;

            if (localizedText == null)
                return false;

            GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(localizedText);
            id = globalObjectId.ToString();
            return !string.IsNullOrEmpty(id);
        }

        private static LocalizedText ResolveLocalizedText(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (!GlobalObjectId.TryParse(id, out GlobalObjectId globalObjectId))
                return null;

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId) as LocalizedText;
        }

        private static string GetOriginalTextSessionKey(string id)
        {
            string encodedId = Convert.ToBase64String(Encoding.UTF8.GetBytes(id))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return OriginalTextSessionKeyPrefix + encodedId;
        }

        private static string NormalizeLocaleCode(string localeCode)
        {
            if (!string.IsNullOrEmpty(localeCode) &&
                RuntimeCsvLocalizationEditorUtility.SupportedLocaleCodes.Contains(localeCode))
            {
                return localeCode;
            }

            return "en-US";
        }

        public static string GetHierarchyPath(LocalizedText localizedText)
        {
            return localizedText == null ? "<null>" : GetHierarchyPath(localizedText.transform);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        public static void LogPreviewReport(PreviewReport report)
        {
            Debug.Log(
                $"[LocalizedTextEditorPreview] Preview locale={report.LocaleCode}. " +
                $"Changed {report.Changed.Count}/{report.PreviewedCount} previewed components, unchanged={report.UnchangedCount}, candidates={report.CandidateCount}. " +
                $"Missing={report.Missing.Count}, Skipped={report.Skipped.Count}.");

            if (report.Missing.Count > 0)
                Debug.LogWarning("[LocalizedTextEditorPreview] Missing keys:\n" + FormatList(report.Missing));
        }

        public static void LogFitScanReport(FitScanReport report)
        {
            if (report.Oversized.Count == 0)
            {
                Debug.Log(
                    $"[LocalizedTextEditorPreview] Text fit scan locale={report.LocaleCode}. " +
                    $"Checked {report.CheckedCount}/{report.CandidateCount}; no oversized text boxes found. " +
                    $"Missing={report.Missing.Count}, Skipped={report.Skipped.Count}.");
            }
            else
            {
                Debug.LogWarning(
                    $"[LocalizedTextEditorPreview] Text fit scan locale={report.LocaleCode}. " +
                    $"Found {report.Oversized.Count} oversized text boxes. " +
                    $"Checked={report.CheckedCount}, Missing={report.Missing.Count}, Skipped={report.Skipped.Count}.\n" +
                    FormatOversized(report.Oversized));
            }

            if (report.Missing.Count > 0)
                Debug.LogWarning("[LocalizedTextEditorPreview] Missing keys during fit scan:\n" + FormatList(report.Missing));
        }

        private static string FormatOversized(IReadOnlyList<OversizedTextInfo> oversized)
        {
            return string.Join(
                "\n",
                oversized.Take(50).Select(item =>
                    $"{item.Path} | {item.Domain}/{item.Key} | {item.UsedLocale} | " +
                    $"rect={item.Fit.Available.x:0.##}x{item.Fit.Available.y:0.##}, " +
                    $"preferred={item.Fit.Preferred.x:0.##}x{item.Fit.Preferred.y:0.##}"));
        }

        private static string FormatList(IReadOnlyList<string> values)
        {
            const int limit = 50;
            string formatted = string.Join("\n", values.Take(limit));
            if (values.Count > limit)
                formatted += $"\n...and {values.Count - limit} more.";

            return formatted;
        }

        private static void QueueRepaint()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }

    public sealed class PreviewReport
    {
        public string LocaleCode;
        public int CandidateCount;
        public int PreviewedCount;
        public int UnchangedCount;
        public readonly List<string> Changed = new List<string>();
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Skipped = new List<string>();
    }

    public sealed class FitScanReport
    {
        public string LocaleCode;
        public int CandidateCount;
        public int CheckedCount;
        public readonly List<string> Missing = new List<string>();
        public readonly List<string> Skipped = new List<string>();
        public readonly List<OversizedTextInfo> Oversized = new List<OversizedTextInfo>();
    }

    public sealed class OversizedTextInfo
    {
        public LocalizedText Component;
        public string Path;
        public string Domain;
        public string Key;
        public string RequestedLocale;
        public string UsedLocale;
        public TextFitResult Fit;
    }

    public struct TextFitResult
    {
        public Vector2 Available;
        public Vector2 Preferred;
        public bool WidthOverflow;
        public bool HeightOverflow;
        public bool Fits => !WidthOverflow && !HeightOverflow;
    }
}

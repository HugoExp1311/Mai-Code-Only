using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Base.Localization.Data;
using Base.Settings;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Base.Localization
{
    /// <summary>
    /// CSV-only localization service for UI and dialogue text.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        private const string DefaultLocaleCode = "en-US";
        private const string DialogueCsvResourceFolder = "Localization/Dialogues";
        private const string UICsvResourceFolder = "Localization/UI";

        private static LocalizationManager instance;

        public static LocalizationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<LocalizationManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("LocalizationManager");
                        instance = go.AddComponent<LocalizationManager>();
                        DontDestroyOnLoad(go);
                    }
                }

                return instance;
            }
        }

        public event Action<Language, string, CultureInfo> LanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> csvCurrentDomains = new();
        private readonly Dictionary<string, Dictionary<string, string>> csvFallbackDomains = new();
        private readonly Dictionary<string, LocalizedText> componentRegistry = new();
        private readonly List<LocalizedText> activeComponents = new();
        private readonly Dictionary<string, Queue<PendingOperation>> operationQueues = new();

        private LocalizationConfig config;
        private LocalizationCache cache;
        private TemplateEngine templateEngine;
        private Stopwatch localeChangeStopwatch;
        private readonly List<float> localeChangeTimes = new();
        private int stringAllocationsThisFrame;
        private int lastFrameCount;

        private Language currentLanguage = Language.English;
        private string currentLocaleCode = DefaultLocaleCode;
        private CultureInfo currentCulture = CultureInfo.GetCultureInfo(DefaultLocaleCode);
        private bool csvLocalizationLoaded;
        private bool cachingEnabled = true;

        public LocalizationConfig Config => config;
        public LocalizationCache Cache => cache;
        public TemplateEngine Templates => templateEngine ??= new TemplateEngine(this);
        public Language CurrentLanguage => currentLanguage;
        public string CurrentLocaleCode => currentLocaleCode;
        public CultureInfo CurrentCulture => currentCulture;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);

            cache = new LocalizationCache();
            templateEngine = new TemplateEngine(this);
            localeChangeStopwatch = new Stopwatch();

            config = Resources.Load<LocalizationConfig>("LocalizationConfig");
            if (config == null)
                Debug.LogWarning("[LocalizationManager] No LocalizationConfig found in Resources. Using defaults.");

            currentLanguage = LoadSavedLanguage();
            currentLocaleCode = GetLocaleCode(currentLanguage);
            currentCulture = CreateCulture(currentLocaleCode);

            LoadCsvLocalizationTables(currentLocaleCode, ShouldLoadCsvFromDiskInEditor(false));
        }

        private void OnApplicationQuit()
        {
            if (instance == this)
                instance = null;
        }

        private Language LoadSavedLanguage()
        {
            int savedValue = PlayerPrefs.GetInt(SettingsType.Language.ToString(), (int)DefaultSettings.DefaultLanguage);
            if (Enum.IsDefined(typeof(Language), savedValue))
                return (Language)savedValue;

            return DefaultSettings.DefaultLanguage;
        }

        public void SetLanguage(Language language, bool refresh = true)
        {
            SetLocaleCode(GetLocaleCode(language), refresh);
        }

        public void SetLocaleCode(string localeCode, bool refresh = true)
        {
            string normalizedLocale = NormalizeLocaleCode(localeCode);
            Language nextLanguage = GetLanguageFromLocaleCode(normalizedLocale);

            localeChangeStopwatch?.Restart();
            currentLanguage = nextLanguage;
            currentLocaleCode = normalizedLocale;
            currentCulture = CreateCulture(normalizedLocale);

            LoadCsvLocalizationTables(normalizedLocale, ShouldLoadCsvFromDiskInEditor(false));
            ClearRuntimeCaches();

            if (config != null && config.debugMode)
                Debug.Log($"[LocalizationManager] Language changed to {currentLanguage} ({currentLocaleCode})");

            if (refresh)
                RefreshAll();

            LanguageChanged?.Invoke(currentLanguage, currentLocaleCode, currentCulture);
            StopLocaleChangeTracking();
        }

        public void ReloadCsvLocalization(bool refresh = true, bool forceFromDiskInEditor = false)
        {
            bool loadFromDiskInEditor = ShouldLoadCsvFromDiskInEditor(forceFromDiskInEditor);
            LoadCsvLocalizationTables(currentLocaleCode, loadFromDiskInEditor);
            ClearRuntimeCaches();

            if (refresh)
                RefreshAll();

            LanguageChanged?.Invoke(currentLanguage, currentLocaleCode, currentCulture);
        }

        public void RegisterComponent(LocalizedText component)
        {
            if (component == null)
                return;

            string id = component.ComponentId;
            if (!string.IsNullOrEmpty(id))
                componentRegistry[id] = component;

            if (!activeComponents.Contains(component))
                activeComponents.Add(component);
        }

        public void UnregisterComponent(LocalizedText component)
        {
            if (component == null)
                return;

            activeComponents.Remove(component);

            string id = component.ComponentId;
            if (!string.IsNullOrEmpty(id) && componentRegistry.TryGetValue(id, out var registered) && registered == component)
                componentRegistry.Remove(id);
        }

        public LocalizedText GetComponentById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            componentRegistry.TryGetValue(id, out var component);
            return component;
        }

        public List<LocalizedText> GetActiveComponents()
        {
            activeComponents.RemoveAll(component => component == null);
            return new List<LocalizedText>(activeComponents);
        }

        public void RefreshAll()
        {
            activeComponents.RemoveAll(component => component == null);

            if (config != null && config.useFrameSpreadUpdates)
            {
                StartCoroutine(RefreshAllSpread());
                return;
            }

            foreach (var component in activeComponents)
                component.Refresh();
        }

        private System.Collections.IEnumerator RefreshAllSpread()
        {
            int perFrame = Mathf.Max(1, config != null ? config.componentsPerFrame : 50);
            int refreshedThisFrame = 0;

            foreach (var component in GetActiveComponents())
            {
                component.Refresh();
                refreshedThisFrame++;

                if (refreshedThisFrame >= perFrame)
                {
                    refreshedThisFrame = 0;
                    yield return null;
                }
            }
        }

        public void RefreshTable(string domain)
        {
            string normalizedDomain = NormalizeDomain(domain);
            activeComponents.RemoveAll(component => component == null);

            foreach (var component in activeComponents)
            {
                string componentDomain = NormalizeDomain(component.TableName);
                if (componentDomain == normalizedDomain)
                    component.Refresh();
            }
        }

        public void ReloadTable(string domain)
        {
            ReloadCsvLocalization(false, ShouldLoadCsvFromDiskInEditor(false));
            RefreshTable(domain);
        }

        public void ReloadAllTables()
        {
            ReloadCsvLocalization(true, ShouldLoadCsvFromDiskInEditor(false));
        }

        public void QueueOperation(PendingOperation operation)
        {
            string domain = NormalizeDomain(operation.TableName);
            if (!operationQueues.TryGetValue(domain, out var queue))
            {
                queue = new Queue<PendingOperation>();
                operationQueues[domain] = queue;
            }

            int maxQueueSize = config != null ? config.maxQueueSize : 100;
            while (queue.Count >= maxQueueSize)
                queue.Dequeue();

            operation.TableName = domain;
            queue.Enqueue(operation);
            ProcessQueue(domain);
        }

        public void ProcessQueue(string domain)
        {
            domain = NormalizeDomain(domain);
            if (!operationQueues.TryGetValue(domain, out var queue))
                return;

            while (queue.Count > 0)
            {
                var operation = queue.Dequeue();
                string text = GetLocalizedString(operation.Key, domain, operation.Variables);
                operation.Component?.SetText(text);
                operation.Callback?.Invoke(text);
            }
        }

        public void ProcessAllQueues()
        {
            foreach (string domain in new List<string>(operationQueues.Keys))
                ProcessQueue(domain);
        }

        public void EnableCaching(bool enable)
        {
            cachingEnabled = enable;
            if (!enable)
                cache?.InvalidateCache();
        }

        public void ClearCache()
        {
            cache?.InvalidateCache();
        }

        public (int hits, int misses, int totalEntries, float hitRate) GetCacheStatistics()
        {
            return cache != null ? cache.GetStatistics() : (0, 0, 0, 0f);
        }

        public bool TryGetLocalizedString(string key, string domain, Dictionary<string, object> variables, out string localizedText)
        {
            localizedText = null;

            if (string.IsNullOrEmpty(key))
                return false;

            domain = NormalizeDomain(domain);
            EnsureCsvLoaded();

            if (TryGetFromDomains(csvCurrentDomains, domain, key, variables, out localizedText))
                return true;

            if (TryGetFromDomains(csvFallbackDomains, domain, key, variables, out localizedText))
                return true;

            return false;
        }

        public bool TryGetCsvLocalizedString(string key, string tableName, Dictionary<string, object> variables, out string localizedText)
        {
            return TryGetLocalizedString(key, tableName, variables, out localizedText);
        }

        public string GetLocalizedString(string key, string domain = LocalizationDomains.UI, Dictionary<string, object> variables = null)
        {
            domain = NormalizeDomain(domain);

            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (cachingEnabled && cache != null && (variables == null || variables.Count == 0))
            {
                string cached = cache.GetCached(key, domain, currentLocaleCode);
                if (cached != null)
                    return cached;
            }

            if (TryGetLocalizedString(key, domain, variables, out string localizedText))
            {
                if (cachingEnabled && cache != null && (variables == null || variables.Count == 0))
                    cache.SetCached(key, domain, localizedText, currentLocaleCode);

                TrackStringAllocation();
                return localizedText;
            }

            return HandleMissingKey(key, domain);
        }

        public string GetLocalizedStringWithFallback(string key, string tableName = null, Dictionary<string, object> variables = null, string fallbackValue = null)
        {
            string result = GetLocalizedString(key, tableName, variables);
            if (IsMissingResult(result, key) && !string.IsNullOrEmpty(fallbackValue))
                return SubstituteVariables(fallbackValue, variables);

            return result;
        }

        public bool KeyExists(string key, string domain = null)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            domain = NormalizeDomain(domain);
            EnsureCsvLoaded();

            return DomainContains(csvCurrentDomains, domain, key) || DomainContains(csvFallbackDomains, domain, key);
        }

        public bool KeyExistsWithFallback(string key, string domain = null, SystemLanguage fallbackLanguage = SystemLanguage.English)
        {
            return KeyExists(key, domain);
        }

        public SystemLanguage GetCurrentLanguage()
        {
            return currentLanguage switch
            {
                Language.Vietnamese => SystemLanguage.Vietnamese,
                Language.Japanese => SystemLanguage.Japanese,
                _ => SystemLanguage.English
            };
        }

        public string ProcessTextWithFallbackAndVariables(string localizedText, string key, string tableName, Dictionary<string, object> variables)
        {
            string result = localizedText;
            if (IsMissingResult(result, key))
                result = GetLocalizedStringWithFallback(key, tableName, variables);

            return SubstituteVariables(result, variables);
        }

        public string GetFallbackText(string key, string tableName, SystemLanguage fallbackLanguage)
        {
            return GetLocalizedString(key, tableName);
        }

        public string GetTextInLanguage(string key, string tableName, SystemLanguage language)
        {
            string localeCode = language switch
            {
                SystemLanguage.Vietnamese => "vi-VN",
                SystemLanguage.Japanese => "ja-JP",
                _ => DefaultLocaleCode
            };

            string domain = NormalizeDomain(tableName);
            var table = LoadCsvTable(domain, GetCsvResourceFolder(domain), localeCode, ShouldLoadCsvFromDiskInEditor(false));
            if (table.TryGetValue(key, out string value))
                return value;

            return HandleMissingKey(key, domain);
        }

        public bool ValidateKey(string key, string domain = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                if (config == null || config.logMissingKeys)
                    Debug.LogWarning("[LocalizationManager] ValidateKey: key is null or empty");
                return false;
            }

            if (!Regex.IsMatch(key, @"^[\w\s.\-']+$"))
            {
                if (config == null || config.logMissingKeys)
                    Debug.LogWarning($"[LocalizationManager] ValidateKey: key '{key}' contains unusual characters.");
            }

            return KeyExists(key, domain);
        }

        public List<LocalizedText> GetComponentsWithPendingUpdates()
        {
            var pending = new List<LocalizedText>();
            activeComponents.RemoveAll(component => component == null);

            foreach (var component in activeComponents)
            {
                if (component.HasPendingUpdate)
                    pending.Add(component);
            }

            return pending;
        }

        public void ApplyPendingUpdates(GameObject root)
        {
            if (root == null)
                return;

            foreach (var localizedText in root.GetComponentsInChildren<LocalizedText>(true))
            {
                if (localizedText.HasPendingUpdate)
                    localizedText.ApplyPending();
            }
        }

        public bool CheckTranslationCompleteness(string domain, out List<string> missingKeys)
        {
            domain = NormalizeDomain(domain);
            EnsureCsvLoaded();
            missingKeys = new List<string>();

            if (!csvFallbackDomains.TryGetValue(domain, out var fallback))
                return false;

            csvCurrentDomains.TryGetValue(domain, out var current);
            foreach (string key in fallback.Keys)
            {
                if (current == null || !current.ContainsKey(key))
                    missingKeys.Add(key);
            }

            return missingKeys.Count == 0;
        }

        public string RenderTemplate(string templateKey, Dictionary<string, object> variables, string domain = null)
        {
            return Templates.RenderTemplate(templateKey, new[] { variables }, domain);
        }

        public string RenderList<T>(string templateKey, IEnumerable<T> items, Func<T, Dictionary<string, object>> itemToVariables, string domain = null, string separator = "\n")
        {
            return Templates.RenderList(templateKey, items, itemToVariables, domain, separator);
        }

        public TemplateEngine.ValidationResult ValidateTemplate(string templateKey, string domain = null, params string[] requiredVariables)
        {
            List<string> requiredVariableList = requiredVariables != null
                ? new List<string>(requiredVariables)
                : null;

            return Templates.ValidateTemplate(templateKey, requiredVariableList, domain);
        }

        public PerformanceStats GetPerformanceStats()
        {
            var stats = new PerformanceStats();
            var cacheStats = GetCacheStatistics();
            stats.TotalComponents = activeComponents.Count;
            stats.CachedLookups = cacheStats.hits;
            stats.CacheMisses = cacheStats.misses;
            stats.StringAllocations = stringAllocationsThisFrame;
            stats.MemoryUsage = GC.GetTotalMemory(false);

            float total = 0f;
            foreach (float time in localeChangeTimes)
                total += time;

            stats.AverageLocaleChangeTime = localeChangeTimes.Count > 0 ? total / localeChangeTimes.Count : 0f;
            return stats;
        }

        public void MonitorPerformance()
        {
            if (Time.frameCount != lastFrameCount)
            {
                lastFrameCount = Time.frameCount;
                stringAllocationsThisFrame = 0;
            }

            if (config == null || !config.debugMode)
                return;

            Debug.Log($"[LocalizationManager] Performance Summary: {GetPerformanceStats()}");
        }

        public IReadOnlyDictionary<string, string> GetDomainEntries(string domain, bool fallback = false)
        {
            domain = NormalizeDomain(domain);
            EnsureCsvLoaded();

            var source = fallback ? csvFallbackDomains : csvCurrentDomains;
            return source.TryGetValue(domain, out var entries) ? entries : new Dictionary<string, string>();
        }

        public IReadOnlyList<string> GetAvailableDomains()
        {
            return new[] { LocalizationDomains.UI, LocalizationDomains.Dialogues };
        }

        private void EnsureCsvLoaded()
        {
            if (!csvLocalizationLoaded)
                LoadCsvLocalizationTables(currentLocaleCode, ShouldLoadCsvFromDiskInEditor(false));
        }

        private void LoadCsvLocalizationTables(string localeCode, bool forceFromDiskInEditor)
        {
            csvCurrentDomains.Clear();
            csvFallbackDomains.Clear();

            LoadCsvDomain(LocalizationDomains.Dialogues, DefaultLocaleCode, csvFallbackDomains, forceFromDiskInEditor);
            LoadCsvDomain(LocalizationDomains.UI, DefaultLocaleCode, csvFallbackDomains, forceFromDiskInEditor);

            if (!string.Equals(localeCode, DefaultLocaleCode, StringComparison.OrdinalIgnoreCase))
            {
                LoadCsvDomain(LocalizationDomains.Dialogues, localeCode, csvCurrentDomains, forceFromDiskInEditor);
                LoadCsvDomain(LocalizationDomains.UI, localeCode, csvCurrentDomains, forceFromDiskInEditor);
            }

            csvLocalizationLoaded = true;
        }

        private void LoadCsvDomain(string domain, string localeCode, Dictionary<string, Dictionary<string, string>> target, bool forceFromDiskInEditor)
        {
            string folder = GetCsvResourceFolder(domain);
            var table = LoadCsvTable(domain, folder, localeCode, forceFromDiskInEditor);
            if (table.Count > 0)
                target[domain] = table;
        }

        private Dictionary<string, string> LoadCsvTable(string domain, string resourceFolder, string localeCode, bool forceFromDiskInEditor)
        {
            string csvText = null;
            string sourceKind = null;
            string sourceLocation = null;
            string resourcePath = $"{resourceFolder}/{localeCode}";
            string resourceLocation = $"Resources/{resourcePath}.csv";
            string diskPath = null;

#if UNITY_EDITOR
            if (forceFromDiskInEditor)
            {
                string projectPath = Application.dataPath;
                diskPath = Path.Combine(projectPath, "Resources", resourceFolder, $"{localeCode}.csv");
                if (File.Exists(diskPath))
                {
                    csvText = File.ReadAllText(diskPath, Encoding.UTF8);
                    sourceKind = "Disk";
                    sourceLocation = diskPath;
                }
            }
#endif

            if (csvText == null)
            {
                TextAsset csvAsset = Resources.Load<TextAsset>(resourcePath);
                if (csvAsset != null)
                {
                    csvText = csvAsset.text;
                    sourceKind = "Resources";
                    sourceLocation = resourceLocation;
                }
            }

            if (string.IsNullOrEmpty(csvText))
            {
                if (config == null || config.logMissingKeys)
                {
                    string expectedSource = forceFromDiskInEditor ? "Disk/Resources" : "Resources";
                    string expectedLocation = forceFromDiskInEditor && !string.IsNullOrEmpty(diskPath)
                        ? $"{diskPath} (fallback {resourceLocation})"
                        : resourceLocation;
                    Debug.LogWarning($"[LocalizationManager] CSV localization file not found or empty for domain '{domain}' locale '{localeCode}'. Source={sourceKind ?? expectedSource}; Path={sourceLocation ?? expectedLocation}");
                }
                return new Dictionary<string, string>();
            }

            var table = ParseCsvLocalization(csvText);
            if (table.Count == 0 && (config == null || config.logMissingKeys))
                Debug.LogWarning($"[LocalizationManager] CSV localization parsed 0 entries for domain '{domain}' locale '{localeCode}' from {sourceKind} '{sourceLocation}'. Expected 'Key,Text' header and at least one data row.");

            if ((config != null && config.debugMode) || string.Equals(sourceKind, "Disk", StringComparison.Ordinal))
                Debug.Log($"[LocalizationManager] Loaded {table.Count} CSV entries for domain '{domain}' locale '{localeCode}' from {sourceKind} '{sourceLocation}'.");

            return table;
        }

        private bool ShouldLoadCsvFromDiskInEditor(bool forceFromDiskInEditor)
        {
#if UNITY_EDITOR
            return forceFromDiskInEditor || Application.isEditor;
#else
            return false;
#endif
        }

        private Dictionary<string, string> ParseCsvLocalization(string csvText)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(csvText))
                return result;

            string[] lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                var cells = ParseCsvLine(lines[i]);
                if (cells.Count < 2)
                    continue;

                string key = cells[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                result[key] = cells[1];
            }

            return result;
        }

        private List<string> ParseCsvLine(string line)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            cells.Add(current.ToString());
            return cells;
        }

        private bool TryGetFromDomains(Dictionary<string, Dictionary<string, string>> domains, string domain, string key, Dictionary<string, object> variables, out string localizedText)
        {
            localizedText = null;
            if (domains.TryGetValue(domain, out var table) && table.TryGetValue(key, out string value))
            {
                localizedText = SubstituteVariables(value, variables);
                return true;
            }

            return false;
        }

        private bool DomainContains(Dictionary<string, Dictionary<string, string>> domains, string domain, string key)
        {
            return domains.TryGetValue(domain, out var table) && table.ContainsKey(key);
        }

        private string SubstituteVariables(string text, Dictionary<string, object> variables)
        {
            if (string.IsNullOrEmpty(text) || variables == null || variables.Count == 0)
                return text;

            string result = text;
            foreach (var kvp in variables)
            {
                string replacement = kvp.Value?.ToString() ?? string.Empty;
                result = result.Replace("{" + kvp.Key + "}", replacement);
            }

            return result;
        }

        private string HandleMissingKey(string key, string domain)
        {
            if (config != null && config.logMissingKeys)
                Debug.LogWarning($"[LocalizationManager] Missing localization key '{key}' in domain '{domain}' for locale '{currentLocaleCode}'");

            MissingKeyBehavior behavior = config != null ? config.missingKeyBehavior : MissingKeyBehavior.ShowKey;
            return behavior switch
            {
                MissingKeyBehavior.ShowPlaceholder => "???",
                MissingKeyBehavior.ShowEmpty => string.Empty,
                MissingKeyBehavior.LogWarning => $"[{key}]",
                MissingKeyBehavior.ThrowException => throw new Exception($"Missing localization key '{key}' in domain '{domain}'"),
                _ => $"[{key}]"
            };
        }

        private bool IsMissingResult(string result, string key)
        {
            return string.IsNullOrEmpty(result) || result == "???" || result == $"[{key}]";
        }

        private string NormalizeDomain(string domain)
        {
            return LocalizationDomains.Normalize(domain);
        }

        private string GetCsvResourceFolder(string domain)
        {
            return NormalizeDomain(domain) == LocalizationDomains.Dialogues
                ? DialogueCsvResourceFolder
                : UICsvResourceFolder;
        }

        private string NormalizeLocaleCode(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return DefaultLocaleCode;

            return localeCode.Trim() switch
            {
                "en" or "en_US" or "English" => "en-US",
                "vi" or "vi_VN" or "Vietnamese" => "vi-VN",
                "ja" or "jp" or "ja_JP" or "Japanese" => "ja-JP",
                "en-US" or "vi-VN" or "ja-JP" => localeCode.Trim(),
                _ => DefaultLocaleCode
            };
        }

        private string GetLocaleCode(Language language)
        {
            return language switch
            {
                Language.Vietnamese => "vi-VN",
                Language.Japanese => "ja-JP",
                _ => DefaultLocaleCode
            };
        }

        private Language GetLanguageFromLocaleCode(string localeCode)
        {
            return NormalizeLocaleCode(localeCode) switch
            {
                "vi-VN" => Language.Vietnamese,
                "ja-JP" => Language.Japanese,
                _ => Language.English
            };
        }

        private CultureInfo CreateCulture(string localeCode)
        {
            try
            {
                return CultureInfo.GetCultureInfo(NormalizeLocaleCode(localeCode));
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private void ClearRuntimeCaches()
        {
            cache?.InvalidateCache();
            templateEngine?.ClearPool();
        }

        private void StopLocaleChangeTracking()
        {
            if (localeChangeStopwatch == null || !localeChangeStopwatch.IsRunning)
                return;

            localeChangeStopwatch.Stop();
            localeChangeTimes.Add((float)localeChangeStopwatch.Elapsed.TotalMilliseconds);
            if (localeChangeTimes.Count > 20)
                localeChangeTimes.RemoveAt(0);
        }

        private void TrackStringAllocation()
        {
            if (Time.frameCount != lastFrameCount)
            {
                lastFrameCount = Time.frameCount;
                stringAllocationsThisFrame = 0;
            }

            stringAllocationsThisFrame++;
        }
    }
}

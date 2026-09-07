using System.Collections.Generic;
using System.IO;
using System.Text;
using Base.Localization;

namespace Base.Localization.EditorTools
{
    public static class RuntimeCsvLocalizationEditorUtility
    {
        public static readonly string[] SupportedLocaleCodes = { "en-US", "vi-VN", "ja-JP" };

        private static readonly Dictionary<string, CachedDomain> cachedDomains = new Dictionary<string, CachedDomain>();

        public static string GetCsvPath(string domain, string localeCode)
        {
            string normalizedDomain = LocalizationDomains.Normalize(domain);
            return $"Assets/Resources/Localization/{normalizedDomain}/{localeCode}.csv";
        }

        public static Dictionary<string, string> LoadDomain(string domain, string localeCode)
        {
            string normalizedDomain = LocalizationDomains.Normalize(domain);
            string normalizedLocale = NormalizeLocaleCode(localeCode);
            string cacheKey = $"{normalizedDomain}/{normalizedLocale}";
            string path = GetCsvPath(normalizedDomain, normalizedLocale);
            var fileInfo = new FileInfo(path);

            long lastWriteTicks = fileInfo.Exists ? fileInfo.LastWriteTimeUtc.Ticks : 0L;
            long length = fileInfo.Exists ? fileInfo.Length : -1L;

            if (cachedDomains.TryGetValue(cacheKey, out CachedDomain cached) &&
                cached.LastWriteTicks == lastWriteTicks &&
                cached.Length == length)
            {
                return cached.Entries;
            }

            var entries = new Dictionary<string, string>();

            if (!fileInfo.Exists)
            {
                cachedDomains[cacheKey] = new CachedDomain(entries, lastWriteTicks, length);
                return entries;
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] cells = ParseCsvLine(lines[i]);
                if (cells.Length < 2)
                    continue;

                string key = cells[0].Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                entries[key] = cells[1];
            }

            cachedDomains[cacheKey] = new CachedDomain(entries, lastWriteTicks, length);
            return entries;
        }

        public static void ClearCache()
        {
            cachedDomains.Clear();
        }

        public static bool TryGetText(string key, string domain, string localeCode, out string text, out string usedLocale)
        {
            text = null;
            localeCode = NormalizeLocaleCode(localeCode);
            usedLocale = localeCode;

            if (string.IsNullOrEmpty(key))
                return false;

            if (LoadDomain(domain, localeCode).TryGetValue(key, out text))
                return true;

            if (localeCode != "en-US" && LoadDomain(domain, "en-US").TryGetValue(key, out text))
            {
                usedLocale = "en-US";
                return true;
            }

            return false;
        }

        public static bool KeyExists(string key, string domain)
        {
            foreach (string localeCode in SupportedLocaleCodes)
            {
                if (LoadDomain(domain, localeCode).ContainsKey(key))
                    return true;
            }

            return false;
        }

        private static string NormalizeLocaleCode(string localeCode)
        {
            if (string.IsNullOrEmpty(localeCode))
                return "en-US";

            foreach (string supportedLocaleCode in SupportedLocaleCodes)
            {
                if (supportedLocaleCode == localeCode)
                    return localeCode;
            }

            return "en-US";
        }

        private sealed class CachedDomain
        {
            public CachedDomain(Dictionary<string, string> entries, long lastWriteTicks, long length)
            {
                Entries = entries;
                LastWriteTicks = lastWriteTicks;
                Length = length;
            }

            public Dictionary<string, string> Entries { get; }
            public long LastWriteTicks { get; }
            public long Length { get; }
        }

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}

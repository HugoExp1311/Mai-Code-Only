using System;
using System.Collections.Generic;
using UnityEngine;

namespace Base.Localization
{
    /// <summary>
    /// Caching layer for localized strings to improve performance
    /// Reduces repeated lookups and string allocations
    /// </summary>
    public class LocalizationCache
    {
        /// <summary>
        /// Cached entry containing localized text with metadata
        /// </summary>
        public struct CachedEntry
        {
            public string LocalizedText;
            public DateTime CachedAt;
            public string Locale;
            
            public CachedEntry(string localizedText, string locale)
            {
                LocalizedText = localizedText;
                CachedAt = DateTime.UtcNow;
                Locale = locale;
            }
        }
        
        // Cache storage: Key format is "tableName:key"
        private Dictionary<string, CachedEntry> cache = new Dictionary<string, CachedEntry>();
        
        // Statistics
        private int hitCount = 0;
        private int missCount = 0;
        
        /// <summary>
        /// Get cached localized string if available and valid for current locale
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="tableName">Table name</param>
        /// <param name="currentLocale">Current locale identifier</param>
        /// <returns>Cached text if valid, null otherwise</returns>
        public string GetCached(string key, string tableName, string currentLocale)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(tableName))
            {
                return null;
            }
            
            string cacheKey = BuildCacheKey(tableName, key);
            
            if (cache.TryGetValue(cacheKey, out CachedEntry entry))
            {
                // Validate locale matches
                if (entry.Locale == currentLocale)
                {
                    hitCount++;
                    return entry.LocalizedText;
                }
                else
                {
                    // Locale changed, invalidate this entry
                    cache.Remove(cacheKey);
                    missCount++;
                    return null;
                }
            }
            
            missCount++;
            return null;
        }
        
        /// <summary>
        /// Add or update a cached entry
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="tableName">Table name</param>
        /// <param name="localizedText">Localized text to cache</param>
        /// <param name="currentLocale">Current locale identifier</param>
        public void SetCached(string key, string tableName, string localizedText, string currentLocale)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(tableName))
            {
                return;
            }
            
            string cacheKey = BuildCacheKey(tableName, key);
            cache[cacheKey] = new CachedEntry(localizedText, currentLocale);
        }
        
        /// <summary>
        /// Invalidate entire cache (typically called on locale change)
        /// </summary>
        public void InvalidateCache()
        {
            cache.Clear();
            
            // Reset statistics on full invalidation
            hitCount = 0;
            missCount = 0;
        }
        
        /// <summary>
        /// Invalidate a specific key in a table
        /// </summary>
        /// <param name="key">Localization key</param>
        /// <param name="tableName">Table name</param>
        public void InvalidateKey(string key, string tableName)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(tableName))
            {
                return;
            }
            
            string cacheKey = BuildCacheKey(tableName, key);
            cache.Remove(cacheKey);
        }
        
        /// <summary>
        /// Invalidate all entries for a specific table
        /// </summary>
        /// <param name="tableName">Table name</param>
        public void InvalidateTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return;
            }
            
            // Remove all entries that start with the table name prefix
            var keysToRemove = new List<string>();
            string tablePrefix = tableName + ":";
            
            foreach (var key in cache.Keys)
            {
                if (key.StartsWith(tablePrefix))
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                cache.Remove(key);
            }
        }
        
        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int hits, int misses, int totalEntries, float hitRate) GetStatistics()
        {
            int total = hitCount + missCount;
            float hitRate = total > 0 ? (float)hitCount / total : 0f;
            
            return (hitCount, missCount, cache.Count, hitRate);
        }
        
        /// <summary>
        /// Build cache key from table name and localization key
        /// </summary>
        private string BuildCacheKey(string tableName, string key)
        {
            return $"{tableName}:{key}";
        }
        
        /// <summary>
        /// Get current cache size in entries
        /// </summary>
        public int GetCacheSize()
        {
            return cache.Count;
        }
        
        /// <summary>
        /// Check if cache contains a specific entry
        /// </summary>
        public bool Contains(string key, string tableName)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(tableName))
            {
                return false;
            }
            
            string cacheKey = BuildCacheKey(tableName, key);
            return cache.ContainsKey(cacheKey);
        }
    }
}

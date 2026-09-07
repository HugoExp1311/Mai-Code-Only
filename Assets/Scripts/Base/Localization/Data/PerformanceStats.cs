using System;

namespace Base.Localization.Data
{
    /// <summary>
    /// Performance statistics for the localization system
    /// </summary>
    [Serializable]
    public struct PerformanceStats
    {
        /// <summary>
        /// Total number of registered components
        /// </summary>
        public int TotalComponents;
        
        /// <summary>
        /// Number of cache hits
        /// </summary>
        public int CachedLookups;
        
        /// <summary>
        /// Number of cache misses
        /// </summary>
        public int CacheMisses;
        
        /// <summary>
        /// Average time for locale changes in milliseconds
        /// </summary>
        public float AverageLocaleChangeTime;
        
        /// <summary>
        /// Number of string allocations per frame
        /// </summary>
        public int StringAllocations;
        
        /// <summary>
        /// Memory usage in bytes
        /// </summary>
        public long MemoryUsage;
        
        /// <summary>
        /// Cache hit rate as a percentage (0-100)
        /// </summary>
        public float CacheHitRate
        {
            get
            {
                int total = CachedLookups + CacheMisses;
                if (total == 0) return 0f;
                return (CachedLookups / (float)total) * 100f;
            }
        }
        
        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void Reset()
        {
            TotalComponents = 0;
            CachedLookups = 0;
            CacheMisses = 0;
            AverageLocaleChangeTime = 0f;
            StringAllocations = 0;
            MemoryUsage = 0;
        }
        
        /// <summary>
        /// Get a formatted string representation of the stats
        /// </summary>
        public override string ToString()
        {
            return $"LocalizationStats: Components={TotalComponents}, " +
                   $"CacheHitRate={CacheHitRate:F1}%, " +
                   $"AvgLocaleChangeTime={AverageLocaleChangeTime:F2}ms, " +
                   $"Memory={MemoryUsage / 1024}KB";
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Base.Localization
{
    /// <summary>
    /// String pooling system for reducing memory allocations in localization.
    /// Reuses common strings to minimize garbage collection pressure.
    /// </summary>
    public class StringPool
    {
        private readonly Dictionary<string, string> pool;
        private int hitCount;
        private int missCount;
        
        /// <summary>
        /// Initialize the string pool with default capacity
        /// </summary>
        public StringPool() : this(100)
        {
        }
        
        /// <summary>
        /// Initialize the string pool with specified capacity
        /// </summary>
        /// <param name="capacity">Initial capacity for the pool</param>
        public StringPool(int capacity)
        {
            pool = new Dictionary<string, string>(capacity);
            hitCount = 0;
            missCount = 0;
        }
        
        /// <summary>
        /// Get a string from the pool or add it if not present.
        /// Returns the pooled instance to reduce allocations.
        /// </summary>
        /// <param name="value">String value to pool</param>
        /// <returns>Pooled string instance</returns>
        public string GetOrAdd(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            
            // Try to get from pool
            if (pool.TryGetValue(value, out var pooled))
            {
                hitCount++;
                return pooled;
            }
            
            // Add to pool
            pool[value] = value;
            missCount++;
            return value;
        }
        
        /// <summary>
        /// Clear all pooled strings and reset statistics
        /// </summary>
        public void Clear()
        {
            pool.Clear();
            hitCount = 0;
            missCount = 0;
        }
        
        /// <summary>
        /// Get the current size of the pool
        /// </summary>
        /// <returns>Number of pooled strings</returns>
        public int GetSize()
        {
            return pool.Count;
        }
        
        /// <summary>
        /// Get pool statistics
        /// </summary>
        /// <returns>Tuple containing hits, misses, and hit rate</returns>
        public (int hits, int misses, float hitRate) GetStatistics()
        {
            int total = hitCount + missCount;
            float hitRate = total > 0 ? (float)hitCount / total * 100f : 0f;
            return (hitCount, missCount, hitRate);
        }
        
        /// <summary>
        /// Check if a string is in the pool
        /// </summary>
        /// <param name="value">String to check</param>
        /// <returns>True if string is pooled, false otherwise</returns>
        public bool Contains(string value)
        {
            return !string.IsNullOrEmpty(value) && pool.ContainsKey(value);
        }
        
        /// <summary>
        /// Remove a specific string from the pool
        /// </summary>
        /// <param name="value">String to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool Remove(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            
            return pool.Remove(value);
        }
        
        /// <summary>
        /// Trim the pool to a maximum size by removing least recently used entries.
        /// Note: This is a simple implementation that clears the entire pool if over capacity.
        /// A more sophisticated LRU implementation could be added if needed.
        /// </summary>
        /// <param name="maxSize">Maximum number of strings to keep</param>
        public void TrimToSize(int maxSize)
        {
            if (pool.Count > maxSize)
            {
                // Simple approach: clear the pool when it gets too large
                // This prevents unbounded growth while maintaining performance
                Debug.LogWarning($"[StringPool] Pool size ({pool.Count}) exceeded max ({maxSize}). Clearing pool.");
                Clear();
            }
        }
    }
}

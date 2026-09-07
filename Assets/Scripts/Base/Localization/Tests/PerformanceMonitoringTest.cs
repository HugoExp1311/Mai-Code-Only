using UnityEngine;
using Base.Localization.Data;
using Base.Settings;

namespace Base.Localization.Tests
{
#if UNITY_EDITOR
    /// <summary>
    /// Test script to verify performance monitoring functionality
    /// Attach to a GameObject in the scene to test (Editor only)
    /// </summary>
    public class PerformanceMonitoringTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private bool logStatsEverySecond = false;
        
        private float nextLogTime = 0f;
        
        private void Start()
        {
            if (runOnStart)
            {
                TestPerformanceStats();
            }
        }
        
        private void Update()
        {
            if (logStatsEverySecond && Time.time >= nextLogTime)
            {
                LogCurrentStats();
                nextLogTime = Time.time + 1f;
            }
        }
        
        [ContextMenu("Test Performance Stats")]
        public void TestPerformanceStats()
        {
            Debug.Log("=== Performance Monitoring Test ===");
            
            // Get current stats
            var stats = LocalizationManager.Instance.GetPerformanceStats();
            
            Debug.Log($"Total Components: {stats.TotalComponents}");
            Debug.Log($"Cache Hits: {stats.CachedLookups}");
            Debug.Log($"Cache Misses: {stats.CacheMisses}");
            Debug.Log($"Cache Hit Rate: {stats.CacheHitRate:F1}%");
            Debug.Log($"Average Locale Change Time: {stats.AverageLocaleChangeTime:F2}ms");
            Debug.Log($"String Allocations This Frame: {stats.StringAllocations}");
            Debug.Log($"Memory Usage: {stats.MemoryUsage / 1024}KB");
            
            Debug.Log("=== End Test ===");
        }
        
        [ContextMenu("Monitor Performance")]
        public void TestMonitorPerformance()
        {
            Debug.Log("=== Running Performance Monitor ===");
            LocalizationManager.Instance.MonitorPerformance();
            Debug.Log("=== End Monitor ===");
        }
        
        [ContextMenu("Simulate Locale Change")]
        public void SimulateLocaleChange()
        {
            Debug.Log("=== Simulating Locale Change ===");

            var currentLanguage = LocalizationManager.Instance.CurrentLanguage;
            var nextLanguage = currentLanguage == Language.English ? Language.Vietnamese : Language.English;
            Debug.Log($"Switching from {currentLanguage} to {nextLanguage}");
            LocalizationManager.Instance.SetLanguage(nextLanguage);
        }
        
        [ContextMenu("Log Current Stats")]
        public void LogCurrentStats()
        {
            var stats = LocalizationManager.Instance.GetPerformanceStats();
            Debug.Log($"[PerformanceStats] {stats}");
        }
        
        [ContextMenu("Test Cache Performance")]
        public void TestCachePerformance()
        {
            Debug.Log("=== Testing Cache Performance ===");
            
            // Perform multiple lookups to test cache
            string testKey = "UI_Test";
            string testTable = LocalizationDomains.UI;
            
            for (int i = 0; i < 10; i++)
            {
                var result = LocalizationManager.Instance.GetLocalizedString(testKey, testTable);
                Debug.Log($"Lookup {i + 1}: {result}");
            }
            
            // Check stats after lookups
            var stats = LocalizationManager.Instance.GetPerformanceStats();
            Debug.Log($"After 10 lookups - Cache Hits: {stats.CachedLookups}, Misses: {stats.CacheMisses}, Hit Rate: {stats.CacheHitRate:F1}%");
            
            Debug.Log("=== End Cache Test ===");
        }
    }
#endif
}

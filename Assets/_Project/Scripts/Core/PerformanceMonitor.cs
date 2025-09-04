using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Performance monitoring system to track and optimize game performance
/// Monitors FPS, memory usage, and system performance metrics
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("Monitoring Settings")]
    [SerializeField] private bool enableMonitoring = true;
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private int frameHistorySize = 60;

    [Header("Performance Thresholds")]
    [SerializeField] private float targetFPS = 60f;
    [SerializeField] private float memoryThresholdMB = 500f;
    [SerializeField] private int gcThresholdKB = 10000;

    private float[] frameTimes;
    private int frameIndex;
    private float lastUpdateTime;
    private float averageFPS;
    private float minFPS;
    private float maxFPS;
    private long totalMemoryUsage;
    private long totalAllocatedMemory;
    private int totalGarbageCollections;

    private void Awake()
    {
        if (enableMonitoring)
        {
            InitializeMonitoring();
            ServiceLocator.RegisterService(this);
        }
    }

    private void InitializeMonitoring()
    {
        frameTimes = new float[frameHistorySize];
        frameIndex = 0;

        // Start monitoring
        InvokeRepeating(nameof(UpdatePerformanceMetrics), 0f, updateInterval);
    }

    private void Update()
    {
        if (!enableMonitoring) return;

        // Record frame time
        float deltaTime = Time.deltaTime;
        frameTimes[frameIndex] = deltaTime;
        frameIndex = (frameIndex + 1) % frameHistorySize;
    }

    private void UpdatePerformanceMetrics()
    {
        // Calculate FPS metrics
        CalculateFPSMetrics();

        // Monitor memory usage
        MonitorMemoryUsage();

        // Monitor garbage collection
        MonitorGarbageCollection();

        // Check performance thresholds
        CheckPerformanceThresholds();

        // Log performance data
        if (enableDebugLogging)
        {
            LogPerformanceData();
        }
    }

    private void CalculateFPSMetrics()
    {
        float totalTime = 0f;
        minFPS = float.MaxValue;
        maxFPS = 0f;

        for (int i = 0; i < frameHistorySize; i++)
        {
            float frameTime = frameTimes[i];
            if (frameTime > 0f)
            {
                float fps = 1f / frameTime;
                totalTime += frameTime;
                minFPS = Mathf.Min(minFPS, fps);
                maxFPS = Mathf.Max(maxFPS, fps);
            }
        }

        averageFPS = frameHistorySize / totalTime;
    }

    private void MonitorMemoryUsage()
    {
        totalMemoryUsage = System.GC.GetTotalMemory(false) / 1024 / 1024; // MB
        totalAllocatedMemory = System.GC.GetTotalMemory(true) / 1024 / 1024; // MB
    }

    private void MonitorGarbageCollection()
    {
        totalGarbageCollections = 0;
        for (int i = 0; i < System.GC.MaxGeneration; i++)
        {
            totalGarbageCollections += System.GC.CollectionCount(i);
        }
    }

    private void CheckPerformanceThresholds()
    {
        // Check FPS
        if (averageFPS < targetFPS * 0.8f)
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Low FPS detected: {0:F1} (Target: {1})", averageFPS, targetFPS));
            OptimizePerformance();
        }

        // Check memory usage
        if (totalMemoryUsage > memoryThresholdMB)
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ High memory usage: {0} MB (Threshold: {1} MB)", totalMemoryUsage, memoryThresholdMB));
            TriggerGarbageCollection();
        }
    }

    private void OptimizePerformance()
    {
        // Reduce quality settings if FPS is too low
        if (averageFPS < 30f)
        {
            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel() - 1, true);
            PerformanceUtils.Log("🔧 Reduced quality settings to improve performance");
        }

        // Disable expensive effects
        var particleSystem = ServiceLocator.GetService<UnifiedParticles>();
        if (particleSystem != null && averageFPS < targetFPS * 0.7f)
        {
            // Reduce particle effects
            PerformanceUtils.Log("🔧 Reducing particle effects for better performance");
        }
    }

    private void TriggerGarbageCollection()
    {
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🗑️ Triggered garbage collection and asset cleanup");
        }
    }

    private void LogPerformanceData()
    {
        string performanceData = PerformanceUtils.FormatString(
            "🎮 Performance - FPS: {0:F1} (Min: {1:F1}, Max: {2:F1}) | Memory: {3} MB | GC: {4}",
            averageFPS, minFPS, maxFPS, totalMemoryUsage, totalGarbageCollections
        );

        PerformanceUtils.Log(performanceData);
    }

    /// <summary>
    /// Get current FPS
    /// </summary>
    public float GetCurrentFPS()
    {
        return averageFPS;
    }

    /// <summary>
    /// Get minimum FPS in history
    /// </summary>
    public float GetMinFPS()
    {
        return minFPS;
    }

    /// <summary>
    /// Get maximum FPS in history
    /// </summary>
    public float GetMaxFPS()
    {
        return maxFPS;
    }

    /// <summary>
    /// Get memory usage in MB
    /// </summary>
    public long GetMemoryUsage()
    {
        return totalMemoryUsage;
    }

    /// <summary>
    /// Get allocated memory in MB
    /// </summary>
    public long GetAllocatedMemory()
    {
        return totalAllocatedMemory;
    }

    /// <summary>
    /// Get total garbage collection count
    /// </summary>
    public int GetGarbageCollectionCount()
    {
        return totalGarbageCollections;
    }

    /// <summary>
    /// Get performance report
    /// </summary>
    public string GetPerformanceReport()
    {
        string report = "=== Performance Report ===\n";
        report += PerformanceUtils.FormatString("Average FPS: {0:F1}\n", averageFPS);
        report += PerformanceUtils.FormatString("FPS Range: {0:F1} - {1:F1}\n", minFPS, maxFPS);
        report += PerformanceUtils.FormatString("Memory Usage: {0} MB\n", totalMemoryUsage);
        report += PerformanceUtils.FormatString("Allocated Memory: {0} MB\n", totalAllocatedMemory);
        report += PerformanceUtils.FormatString("Garbage Collections: {0}\n", totalGarbageCollections);
        report += PerformanceUtils.FormatString("Target FPS: {0}\n", targetFPS);
        report += PerformanceUtils.FormatString("Performance Status: {0}\n",
            averageFPS >= targetFPS * 0.9f ? "Excellent" :
            averageFPS >= targetFPS * 0.7f ? "Good" :
            averageFPS >= targetFPS * 0.5f ? "Poor" : "Critical");

        return report;
    }

    /// <summary>
    /// Enable/disable monitoring
    /// </summary>
    public void SetMonitoringEnabled(bool enabled)
    {
        enableMonitoring = enabled;
        if (enabled && frameTimes == null)
        {
            InitializeMonitoring();
        }
    }

    /// <summary>
    /// Set update interval
    /// </summary>
    public void SetUpdateInterval(float interval)
    {
        updateInterval = interval;
        if (enableMonitoring)
        {
            CancelInvoke(nameof(UpdatePerformanceMetrics));
            InvokeRepeating(nameof(UpdatePerformanceMetrics), 0f, updateInterval);
        }
    }

    /// <summary>
    /// Set performance thresholds
    /// </summary>
    public void SetThresholds(float fps, float memoryMB, int gcKB)
    {
        targetFPS = fps;
        memoryThresholdMB = memoryMB;
        gcThresholdKB = gcKB;
    }

    /// <summary>
    /// Force performance optimization
    /// </summary>
    public void ForceOptimization()
    {
        OptimizePerformance();
        TriggerGarbageCollection();
    }

    /// <summary>
    /// Reset performance metrics
    /// </summary>
    public void ResetMetrics()
    {
        frameIndex = 0;
        averageFPS = 0f;
        minFPS = float.MaxValue;
        maxFPS = 0f;
        totalMemoryUsage = 0;
        totalAllocatedMemory = 0;
        totalGarbageCollections = 0;

        System.Array.Clear(frameTimes, 0, frameTimes.Length);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🔄 Performance metrics reset");
        }
    }

    /// <summary>
    /// Get frame time history
    /// </summary>
    public float[] GetFrameTimeHistory()
    {
        return (float[])frameTimes.Clone();
    }

    /// <summary>
    /// Check if performance is within acceptable range
    /// </summary>
    public bool IsPerformanceAcceptable()
    {
        return averageFPS >= targetFPS * 0.8f && totalMemoryUsage <= memoryThresholdMB;
    }

    private void OnDestroy()
    {
        if (enableMonitoring)
        {
            CancelInvoke(nameof(UpdatePerformanceMetrics));
        }
    }
}

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Monitors RL system performance and applies graceful degradation
    /// Implements Requirements 6.2, 6.3 - performance constraints and monitoring
    /// Enhanced with adaptive batch sizing and comprehensive memory tracking
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Performance Thresholds")]
        [SerializeField] private float maxFrameTimeMs = 16f; // 60 FPS target
        [SerializeField] private float maxMemoryUsageMB = 100f;
        [SerializeField] private int maxActiveAgents = 50;
        [SerializeField] private float degradationThreshold = 0.8f; // Start degrading at 80% of limit
        
        [Header("Monitoring Settings")]
        [SerializeField] private float monitoringInterval = 1f; // Check every second
        [SerializeField] private int performanceHistorySize = 60; // Keep 60 seconds of history
        [SerializeField] private bool enableAutoDegradation = true;
        [SerializeField] private bool enableDetailedMemoryTracking = true;
        
        [Header("Adaptive Batch Sizing")]
        [SerializeField] private int baseBatchSize = 32;
        [SerializeField] private int minBatchSize = 4;
        [SerializeField] private int maxBatchSize = 128;
        [SerializeField] private float batchSizeAdjustmentRate = 0.1f;
        [SerializeField] private bool enableAdaptiveBatchSizing = true;
        
        private Queue<PerformanceSample> performanceHistory;
        private float lastMonitoringTime;
        private DegradationLevel currentDegradationLevel;
        private Dictionary<string, float> componentPerformance;
        private Dictionary<string, ComponentMemoryUsage> componentMemoryUsage;
        
        // Performance tracking
        private float currentFrameTime;
        private float currentMemoryUsage;
        private int currentActiveAgents;
        private float averageFrameTime;
        private float peakMemoryUsage;
        
        // Adaptive batch sizing
        private int currentBatchSize;
        private float batchSizePerformanceScore;
        private Queue<float> batchPerformanceHistory;
        private int batchSizeAdjustmentCooldown;
        
        // Degradation state
        private int originalBatchSize = 32;
        private int originalMaxAgentsPerFrame = 10;
        private float originalUpdateInterval = 0.1f;
        
        // Memory tracking
        private long lastGCMemory;
        private int gcCollectionCount;
        private float memoryGrowthRate;
        
        public event Action<DegradationLevel> OnDegradationLevelChanged;
        public event Action<PerformanceAlert> OnPerformanceAlert;
        public event Action<int> OnBatchSizeChanged;
        public event Action<MemoryAlert> OnMemoryAlert;
        
        public DegradationLevel CurrentDegradationLevel => currentDegradationLevel;
        public PerformanceMetrics CurrentMetrics => GetCurrentMetrics();
        public int CurrentBatchSize => currentBatchSize;
        public float AverageFrameTime => averageFrameTime;
        public float PeakMemoryUsage => peakMemoryUsage;

        void Awake()
        {
            performanceHistory = new Queue<PerformanceSample>();
            componentPerformance = new Dictionary<string, float>();
            componentMemoryUsage = new Dictionary<string, ComponentMemoryUsage>();
            batchPerformanceHistory = new Queue<float>();
            
            currentDegradationLevel = DegradationLevel.None;
            currentBatchSize = baseBatchSize;
            lastMonitoringTime = Time.time;
            
            // Initialize memory tracking
            lastGCMemory = GC.GetTotalMemory(false);
            gcCollectionCount = GC.CollectionCount(0);
            
            Debug.Log($"PerformanceMonitor initialized - Base batch size: {baseBatchSize}");
        }

        void Update()
        {
            // Monitor performance at regular intervals
            if (Time.time - lastMonitoringTime >= monitoringInterval)
            {
                MonitorPerformance();
                lastMonitoringTime = Time.time;
            }
        }

        /// <summary>
        /// Record performance metrics for a component
        /// </summary>
        public void RecordComponentPerformance(string componentName, float processingTimeMs)
        {
            try
            {
                componentPerformance[componentName] = processingTimeMs;
                
                // Check if this component is causing performance issues
                if (processingTimeMs > maxFrameTimeMs * 0.5f) // Component using more than 50% of frame budget
                {
                    var alert = new PerformanceAlert
                    {
                        timestamp = DateTime.Now,
                        component = componentName,
                        metric = "ProcessingTime",
                        value = processingTimeMs,
                        threshold = maxFrameTimeMs * 0.5f,
                        severity = processingTimeMs > maxFrameTimeMs ? AlertSeverity.Critical : AlertSeverity.Warning
                    };
                    
                    OnPerformanceAlert?.Invoke(alert);
                    ErrorHandler.LogPerformanceIssue(componentName, "ProcessingTime", processingTimeMs, maxFrameTimeMs * 0.5f);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("PerformanceMonitor", "RecordComponentPerformance", ex, componentName);
            }
        }

        /// <summary>
        /// Update current system metrics
        /// </summary>
        public void UpdateSystemMetrics(float frameTimeMs, float memoryUsageMB, int activeAgents)
        {
            currentFrameTime = frameTimeMs;
            currentMemoryUsage = memoryUsageMB;
            currentActiveAgents = activeAgents;
            
            // Update running averages
            float alpha = 0.1f;
            averageFrameTime = averageFrameTime * (1f - alpha) + frameTimeMs * alpha;
            
            // Track peak memory usage
            if (memoryUsageMB > peakMemoryUsage)
            {
                peakMemoryUsage = memoryUsageMB;
            }
            
            // Update adaptive batch sizing
            if (enableAdaptiveBatchSizing)
            {
                UpdateAdaptiveBatchSizing(frameTimeMs);
            }
            
            // Update detailed memory tracking
            if (enableDetailedMemoryTracking)
            {
                UpdateDetailedMemoryTracking();
            }
        }

        /// <summary>
        /// Record memory usage for a specific component
        /// </summary>
        public void RecordComponentMemoryUsage(string componentName, long memoryBytes, int objectCount = 0)
        {
            try
            {
                var usage = new ComponentMemoryUsage
                {
                    memoryBytes = memoryBytes,
                    objectCount = objectCount,
                    timestamp = DateTime.Now
                };
                
                componentMemoryUsage[componentName] = usage;
                
                // Check for memory leaks (rapid growth)
                if (componentMemoryUsage.ContainsKey(componentName))
                {
                    var previousUsage = componentMemoryUsage[componentName];
                    float memoryGrowthMB = (memoryBytes - previousUsage.memoryBytes) / (1024f * 1024f);
                    
                    if (memoryGrowthMB > 10f) // More than 10MB growth
                    {
                        var alert = new MemoryAlert
                        {
                            timestamp = DateTime.Now,
                            component = componentName,
                            currentMemoryMB = memoryBytes / (1024f * 1024f),
                            growthMB = memoryGrowthMB,
                            severity = memoryGrowthMB > 50f ? AlertSeverity.Critical : AlertSeverity.Warning
                        };
                        
                        OnMemoryAlert?.Invoke(alert);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("PerformanceMonitor", "RecordComponentMemoryUsage", ex, componentName);
            }
        }

        /// <summary>
        /// Get optimal batch size based on current performance
        /// </summary>
        public int GetOptimalBatchSize()
        {
            return currentBatchSize;
        }

        /// <summary>
        /// Force batch size adjustment
        /// </summary>
        public void SetBatchSize(int newBatchSize)
        {
            int clampedSize = Mathf.Clamp(newBatchSize, minBatchSize, maxBatchSize);
            if (clampedSize != currentBatchSize)
            {
                currentBatchSize = clampedSize;
                OnBatchSizeChanged?.Invoke(currentBatchSize);
                Debug.Log($"[PERFORMANCE] Batch size manually set to {currentBatchSize}");
            }
        }

        /// <summary>
        /// Force a performance check and potential degradation
        /// </summary>
        public void CheckPerformanceNow()
        {
            MonitorPerformance();
        }

        /// <summary>
        /// Apply manual degradation level
        /// </summary>
        public void SetDegradationLevel(DegradationLevel level)
        {
            if (currentDegradationLevel != level)
            {
                ApplyDegradation(level);
            }
        }

        /// <summary>
        /// Reset to optimal performance settings
        /// </summary>
        public void ResetToOptimalPerformance()
        {
            ApplyDegradation(DegradationLevel.None);
        }

        /// <summary>
        /// Get performance recommendations based on current state
        /// </summary>
        public List<string> GetPerformanceRecommendations()
        {
            var recommendations = new List<string>();
            
            if (currentFrameTime > maxFrameTimeMs * degradationThreshold)
            {
                recommendations.Add("Reduce batch size for neural network training");
                recommendations.Add("Limit number of agents updated per frame");
                recommendations.Add("Increase update interval between agent updates");
            }
            
            if (currentMemoryUsage > maxMemoryUsageMB * degradationThreshold)
            {
                recommendations.Add("Clear old experience replay buffers");
                recommendations.Add("Compress behavior profiles");
                recommendations.Add("Reduce neural network size");
            }
            
            if (currentActiveAgents > maxActiveAgents * degradationThreshold)
            {
                recommendations.Add("Implement agent pooling");
                recommendations.Add("Limit concurrent learning agents");
                recommendations.Add("Use fallback agents for some monsters");
            }
            
            return recommendations;
        }

        private void MonitorPerformance()
        {
            try
            {
                // Create performance sample
                var sample = new PerformanceSample
                {
                    timestamp = DateTime.Now,
                    frameTimeMs = currentFrameTime,
                    memoryUsageMB = currentMemoryUsage,
                    activeAgents = currentActiveAgents,
                    componentPerformance = new Dictionary<string, float>(componentPerformance)
                };
                
                // Add to history
                performanceHistory.Enqueue(sample);
                if (performanceHistory.Count > performanceHistorySize)
                {
                    performanceHistory.Dequeue();
                }
                
                // Check for performance issues
                CheckPerformanceThresholds(sample);
                
                // Apply automatic degradation if enabled
                if (enableAutoDegradation)
                {
                    DegradationLevel recommendedLevel = CalculateRecommendedDegradationLevel(sample);
                    if (recommendedLevel != currentDegradationLevel)
                    {
                        ApplyDegradation(recommendedLevel);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("PerformanceMonitor", "MonitorPerformance", ex);
            }
        }

        private void CheckPerformanceThresholds(PerformanceSample sample)
        {
            // Check frame time
            if (sample.frameTimeMs > maxFrameTimeMs)
            {
                var alert = new PerformanceAlert
                {
                    timestamp = sample.timestamp,
                    component = "RLSystem",
                    metric = "FrameTime",
                    value = sample.frameTimeMs,
                    threshold = maxFrameTimeMs,
                    severity = sample.frameTimeMs > maxFrameTimeMs * 1.5f ? AlertSeverity.Critical : AlertSeverity.Warning
                };
                OnPerformanceAlert?.Invoke(alert);
            }
            
            // Check memory usage
            if (sample.memoryUsageMB > maxMemoryUsageMB)
            {
                var alert = new PerformanceAlert
                {
                    timestamp = sample.timestamp,
                    component = "RLSystem",
                    metric = "MemoryUsage",
                    value = sample.memoryUsageMB,
                    threshold = maxMemoryUsageMB,
                    severity = sample.memoryUsageMB > maxMemoryUsageMB * 1.5f ? AlertSeverity.Critical : AlertSeverity.Warning
                };
                OnPerformanceAlert?.Invoke(alert);
            }
            
            // Check agent count
            if (sample.activeAgents > maxActiveAgents)
            {
                var alert = new PerformanceAlert
                {
                    timestamp = sample.timestamp,
                    component = "RLSystem",
                    metric = "ActiveAgents",
                    value = sample.activeAgents,
                    threshold = maxActiveAgents,
                    severity = AlertSeverity.Warning
                };
                OnPerformanceAlert?.Invoke(alert);
            }
        }

        private DegradationLevel CalculateRecommendedDegradationLevel(PerformanceSample sample)
        {
            float frameTimeRatio = sample.frameTimeMs / maxFrameTimeMs;
            float memoryRatio = sample.memoryUsageMB / maxMemoryUsageMB;
            float agentRatio = (float)sample.activeAgents / maxActiveAgents;
            
            float maxRatio = Mathf.Max(frameTimeRatio, memoryRatio, agentRatio);
            
            if (maxRatio >= 1.5f)
                return DegradationLevel.Severe;
            else if (maxRatio >= 1.2f)
                return DegradationLevel.High;
            else if (maxRatio >= 1.0f)
                return DegradationLevel.Medium;
            else if (maxRatio >= degradationThreshold)
                return DegradationLevel.Low;
            else
                return DegradationLevel.None;
        }

        private void ApplyDegradation(DegradationLevel level)
        {
            try
            {
                DegradationLevel previousLevel = currentDegradationLevel;
                currentDegradationLevel = level;
                
                // Apply degradation settings based on level
                switch (level)
                {
                    case DegradationLevel.None:
                        ApplyOptimalSettings();
                        break;
                    case DegradationLevel.Low:
                        ApplyLowDegradation();
                        break;
                    case DegradationLevel.Medium:
                        ApplyMediumDegradation();
                        break;
                    case DegradationLevel.High:
                        ApplyHighDegradation();
                        break;
                    case DegradationLevel.Severe:
                        ApplySevereDegradation();
                        break;
                }
                
                Debug.Log($"[PERFORMANCE] Degradation level changed from {previousLevel} to {level}");
                OnDegradationLevelChanged?.Invoke(level);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("PerformanceMonitor", "ApplyDegradation", ex, level.ToString());
            }
        }

        private void ApplyOptimalSettings()
        {
            // Reset to original optimal settings
            SetRLSystemSettings(originalBatchSize, originalMaxAgentsPerFrame, originalUpdateInterval);
        }

        private void ApplyLowDegradation()
        {
            // Slight reduction in processing
            SetRLSystemSettings(
                Mathf.Max(16, originalBatchSize - 8),
                Mathf.Max(5, originalMaxAgentsPerFrame - 2),
                originalUpdateInterval * 1.2f
            );
        }

        private void ApplyMediumDegradation()
        {
            // Moderate reduction in processing
            SetRLSystemSettings(
                Mathf.Max(8, originalBatchSize / 2),
                Mathf.Max(3, originalMaxAgentsPerFrame / 2),
                originalUpdateInterval * 1.5f
            );
        }

        private void ApplyHighDegradation()
        {
            // Significant reduction in processing
            SetRLSystemSettings(
                Mathf.Max(4, originalBatchSize / 4),
                Mathf.Max(2, originalMaxAgentsPerFrame / 3),
                originalUpdateInterval * 2f
            );
        }

        private void ApplySevereDegradation()
        {
            // Minimal processing to maintain stability
            SetRLSystemSettings(
                2, // Minimum batch size
                1, // One agent per frame
                originalUpdateInterval * 3f
            );
        }

        private void UpdateAdaptiveBatchSizing(float frameTimeMs)
        {
            // Skip adjustment if in cooldown
            if (batchSizeAdjustmentCooldown > 0)
            {
                batchSizeAdjustmentCooldown--;
                return;
            }
            
            // Calculate performance score (lower is better)
            float performanceScore = frameTimeMs / maxFrameTimeMs;
            
            // Add to history
            batchPerformanceHistory.Enqueue(performanceScore);
            if (batchPerformanceHistory.Count > 10) // Keep last 10 samples
            {
                batchPerformanceHistory.Dequeue();
            }
            
            // Only adjust if we have enough history
            if (batchPerformanceHistory.Count < 5) return;
            
            // Calculate average performance
            float avgPerformance = 0f;
            foreach (float score in batchPerformanceHistory)
            {
                avgPerformance += score;
            }
            avgPerformance /= batchPerformanceHistory.Count;
            
            // Determine if we should adjust batch size
            int newBatchSize = currentBatchSize;
            
            if (avgPerformance > 1.2f) // Performance is poor, reduce batch size
            {
                newBatchSize = Mathf.Max(minBatchSize, 
                    Mathf.RoundToInt(currentBatchSize * (1f - batchSizeAdjustmentRate)));
            }
            else if (avgPerformance < 0.6f) // Performance is good, try increasing batch size
            {
                newBatchSize = Mathf.Min(maxBatchSize, 
                    Mathf.RoundToInt(currentBatchSize * (1f + batchSizeAdjustmentRate)));
            }
            
            // Apply change if significant
            if (Mathf.Abs(newBatchSize - currentBatchSize) >= 2)
            {
                currentBatchSize = newBatchSize;
                batchSizeAdjustmentCooldown = 30; // Wait 30 frames before next adjustment
                OnBatchSizeChanged?.Invoke(currentBatchSize);
                
                Debug.Log($"[ADAPTIVE BATCH] Adjusted batch size to {currentBatchSize} (avg performance: {avgPerformance:F2})");
            }
        }

        private void UpdateDetailedMemoryTracking()
        {
            try
            {
                // Track GC memory
                long currentGCMemory = GC.GetTotalMemory(false);
                int currentGCCount = GC.CollectionCount(0);
                
                // Calculate memory growth rate
                float memoryDeltaMB = (currentGCMemory - lastGCMemory) / (1024f * 1024f);
                memoryGrowthRate = memoryGrowthRate * 0.9f + memoryDeltaMB * 0.1f; // Smooth the rate
                
                // Check for excessive GC
                if (currentGCCount > gcCollectionCount)
                {
                    int gcDelta = currentGCCount - gcCollectionCount;
                    if (gcDelta > 5) // More than 5 GC collections since last check
                    {
                        var alert = new MemoryAlert
                        {
                            timestamp = DateTime.Now,
                            component = "GarbageCollector",
                            currentMemoryMB = currentGCMemory / (1024f * 1024f),
                            growthMB = memoryDeltaMB,
                            severity = AlertSeverity.Warning,
                            additionalInfo = $"Excessive GC activity: {gcDelta} collections"
                        };
                        
                        OnMemoryAlert?.Invoke(alert);
                    }
                }
                
                // Update tracking variables
                lastGCMemory = currentGCMemory;
                gcCollectionCount = currentGCCount;
                
                // Check for memory leaks (sustained growth)
                if (memoryGrowthRate > 1f) // Growing by more than 1MB per monitoring interval
                {
                    var alert = new MemoryAlert
                    {
                        timestamp = DateTime.Now,
                        component = "SystemMemory",
                        currentMemoryMB = currentGCMemory / (1024f * 1024f),
                        growthMB = memoryGrowthRate,
                        severity = memoryGrowthRate > 5f ? AlertSeverity.Critical : AlertSeverity.Warning,
                        additionalInfo = $"Sustained memory growth rate: {memoryGrowthRate:F2} MB/interval"
                    };
                    
                    OnMemoryAlert?.Invoke(alert);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("PerformanceMonitor", "UpdateDetailedMemoryTracking", ex);
            }
        }

        private void SetRLSystemSettings(int batchSize, int maxAgentsPerFrame, float updateInterval)
        {
            // Update batch size through adaptive system
            if (enableAdaptiveBatchSizing && batchSize != currentBatchSize)
            {
                SetBatchSize(batchSize);
            }
            
            // This would integrate with the main RL system to apply other settings
            Debug.Log($"[PERFORMANCE] Applied settings: BatchSize={batchSize}, MaxAgentsPerFrame={maxAgentsPerFrame}, UpdateInterval={updateInterval:F2}s");
        }

        private PerformanceMetrics GetCurrentMetrics()
        {
            return new PerformanceMetrics
            {
                frameTimeMs = currentFrameTime,
                memoryUsageMB = currentMemoryUsage,
                activeAgents = currentActiveAgents,
                degradationLevel = currentDegradationLevel,
                componentPerformance = new Dictionary<string, float>(componentPerformance),
                currentBatchSize = currentBatchSize,
                averageFrameTime = averageFrameTime,
                peakMemoryUsage = peakMemoryUsage,
                memoryGrowthRate = memoryGrowthRate
            };
        }
    }

    /// <summary>
    /// Performance degradation levels
    /// </summary>
    public enum DegradationLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Severe = 4
    }

    /// <summary>
    /// Performance alert severity
    /// </summary>
    public enum AlertSeverity
    {
        Info = 0,
        Warning = 1,
        Critical = 2
    }

    /// <summary>
    /// Performance sample for monitoring
    /// </summary>
    [Serializable]
    public class PerformanceSample
    {
        public DateTime timestamp;
        public float frameTimeMs;
        public float memoryUsageMB;
        public int activeAgents;
        public Dictionary<string, float> componentPerformance;
    }

    /// <summary>
    /// Performance alert for notifications
    /// </summary>
    [Serializable]
    public class PerformanceAlert
    {
        public DateTime timestamp;
        public string component;
        public string metric;
        public float value;
        public float threshold;
        public AlertSeverity severity;
    }

    /// <summary>
    /// Current performance metrics
    /// </summary>
    [Serializable]
    public class PerformanceMetrics
    {
        public float frameTimeMs;
        public float memoryUsageMB;
        public int activeAgents;
        public DegradationLevel degradationLevel;
        public Dictionary<string, float> componentPerformance;
        public int currentBatchSize;
        public float averageFrameTime;
        public float peakMemoryUsage;
        public float memoryGrowthRate;
    }

    /// <summary>
    /// Component-specific memory usage tracking
    /// </summary>
    [Serializable]
    public class ComponentMemoryUsage
    {
        public long memoryBytes;
        public int objectCount;
        public DateTime timestamp;
        
        public float MemoryMB => memoryBytes / (1024f * 1024f);
    }

    /// <summary>
    /// Memory-specific alert for detailed tracking
    /// </summary>
    [Serializable]
    public class MemoryAlert
    {
        public DateTime timestamp;
        public string component;
        public float currentMemoryMB;
        public float growthMB;
        public AlertSeverity severity;
        public string additionalInfo;
    }
}
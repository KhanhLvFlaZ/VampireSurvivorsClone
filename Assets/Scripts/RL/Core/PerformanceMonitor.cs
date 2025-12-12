using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Monitors RL system performance and applies graceful degradation
    /// Implements Requirements 6.2, 6.3 - performance constraints and monitoring
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
        
        private Queue<PerformanceSample> performanceHistory;
        private float lastMonitoringTime;
        private DegradationLevel currentDegradationLevel;
        private Dictionary<string, float> componentPerformance;
        
        // Performance tracking
        private float currentFrameTime;
        private float currentMemoryUsage;
        private int currentActiveAgents;
        
        // Degradation state
        private int originalBatchSize = 32;
        private int originalMaxAgentsPerFrame = 10;
        private float originalUpdateInterval = 0.1f;
        
        public event Action<DegradationLevel> OnDegradationLevelChanged;
        public event Action<PerformanceAlert> OnPerformanceAlert;
        
        public DegradationLevel CurrentDegradationLevel => currentDegradationLevel;
        public PerformanceMetrics CurrentMetrics => GetCurrentMetrics();

        void Awake()
        {
            performanceHistory = new Queue<PerformanceSample>();
            componentPerformance = new Dictionary<string, float>();
            currentDegradationLevel = DegradationLevel.None;
            lastMonitoringTime = Time.time;
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

        private void SetRLSystemSettings(int batchSize, int maxAgentsPerFrame, float updateInterval)
        {
            // This would integrate with the main RL system to apply settings
            // For now, just log the changes
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
                componentPerformance = new Dictionary<string, float>(componentPerformance)
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
    }
}
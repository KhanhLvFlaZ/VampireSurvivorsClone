using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Comprehensive performance optimization manager for the RL system
    /// Implements Requirements 6.2, 6.3 - frame time monitoring, memory tracking, and adaptive batch sizing
    /// Coordinates between PerformanceMonitor, TrainingCoordinator, and individual agents
    /// </summary>
    public class PerformanceOptimizationManager : MonoBehaviour
    {
        [Header("Optimization Settings")]
        [SerializeField] private bool enableOptimization = true;
        [SerializeField] private float optimizationInterval = 2f; // Optimize every 2 seconds
        [SerializeField] private float emergencyThreshold = 1.5f; // Emergency action at 150% of limits
        
        [Header("Performance Targets")]
        [SerializeField] private float targetFrameTimeMs = 12f; // Target 12ms for 60 FPS with buffer
        [SerializeField] private float targetMemoryUsageMB = 80f; // Target 80MB to stay under 100MB limit
        [SerializeField] private int targetActiveAgents = 40; // Target 40 agents to stay under 50 limit
        
        [Header("Optimization Strategies")]
        [SerializeField] private bool enableBatchSizeOptimization = true;
        [SerializeField] private bool enableAgentThrottling = true;
        [SerializeField] private bool enableMemoryOptimization = true;
        [SerializeField] private bool enableEmergencyMeasures = true;
        
        // Core components
        private PerformanceMonitor performanceMonitor;
        private TrainingCoordinator trainingCoordinator;
        private RLSystem rlSystem;
        
        // Optimization state
        private float lastOptimizationTime;
        private OptimizationStrategy currentStrategy;
        private Dictionary<string, float> componentOptimizationScores;
        private Queue<PerformanceSnapshot> performanceHistory;
        
        // Emergency state
        private bool emergencyModeActive;
        private float emergencyModeStartTime;
        private int emergencyActionCount;
        
        // Events
        public event Action<OptimizationStrategy> OnOptimizationStrategyChanged;
        public event Action<bool> OnEmergencyModeChanged;
        public event Action<OptimizationResult> OnOptimizationCompleted;

        public bool IsOptimizationActive => enableOptimization;
        public OptimizationStrategy CurrentStrategy => currentStrategy;
        public bool IsEmergencyModeActive => emergencyModeActive;

        void Awake()
        {
            componentOptimizationScores = new Dictionary<string, float>();
            performanceHistory = new Queue<PerformanceSnapshot>();
            currentStrategy = OptimizationStrategy.Balanced;
            lastOptimizationTime = Time.time;
        }

        void Start()
        {
            InitializeComponents();
        }

        void Update()
        {
            if (!enableOptimization) return;
            
            // Check for emergency conditions every frame
            CheckEmergencyConditions();
            
            // Run optimization at regular intervals
            if (Time.time - lastOptimizationTime >= optimizationInterval)
            {
                RunOptimizationCycle();
                lastOptimizationTime = Time.time;
            }
        }

        private void InitializeComponents()
        {
            // Find or create required components
            rlSystem = FindObjectOfType<RLSystem>();
            if (rlSystem == null)
            {
                Debug.LogError("PerformanceOptimizationManager requires RLSystem to be present");
                return;
            }
            
            performanceMonitor = FindObjectOfType<PerformanceMonitor>();
            trainingCoordinator = FindObjectOfType<TrainingCoordinator>();
            
            if (performanceMonitor == null || trainingCoordinator == null)
            {
                Debug.LogWarning("Some RL components not found - optimization may be limited");
            }
            
            Debug.Log("Performance Optimization Manager initialized");
        }

        private void CheckEmergencyConditions()
        {
            if (!enableEmergencyMeasures || performanceMonitor == null) return;
            
            var metrics = performanceMonitor.CurrentMetrics;
            bool shouldActivateEmergency = false;
            
            // Check critical thresholds
            if (metrics.frameTimeMs > targetFrameTimeMs * emergencyThreshold ||
                metrics.memoryUsageMB > targetMemoryUsageMB * emergencyThreshold ||
                metrics.activeAgents > targetActiveAgents * emergencyThreshold)
            {
                shouldActivateEmergency = true;
            }
            
            // Activate or deactivate emergency mode
            if (shouldActivateEmergency && !emergencyModeActive)
            {
                ActivateEmergencyMode();
            }
            else if (!shouldActivateEmergency && emergencyModeActive)
            {
                DeactivateEmergencyMode();
            }
        }

        private void ActivateEmergencyMode()
        {
            emergencyModeActive = true;
            emergencyModeStartTime = Time.time;
            emergencyActionCount = 0;
            
            Debug.LogWarning("[EMERGENCY] Performance emergency mode activated");
            
            // Immediate emergency actions
            ApplyEmergencyOptimizations();
            
            OnEmergencyModeChanged?.Invoke(true);
        }

        private void DeactivateEmergencyMode()
        {
            emergencyModeActive = false;
            float emergencyDuration = Time.time - emergencyModeStartTime;
            
            Debug.Log($"[EMERGENCY] Emergency mode deactivated after {emergencyDuration:F1}s with {emergencyActionCount} actions");
            
            OnEmergencyModeChanged?.Invoke(false);
        }

        private void ApplyEmergencyOptimizations()
        {
            var result = new OptimizationResult { strategy = OptimizationStrategy.Emergency };
            
            // Drastically reduce batch sizes
            if (enableBatchSizeOptimization && performanceMonitor != null)
            {
                int emergencyBatchSize = Mathf.Max(2, performanceMonitor.CurrentBatchSize / 4);
                performanceMonitor.SetBatchSize(emergencyBatchSize);
                result.batchSizeChanged = true;
                result.newBatchSize = emergencyBatchSize;
                emergencyActionCount++;
            }
            
            // Severely limit agent processing
            if (enableAgentThrottling && trainingCoordinator != null)
            {
                // Force severe degradation
                performanceMonitor?.SetDegradationLevel(DegradationLevel.Severe);
                result.degradationLevelChanged = true;
                result.newDegradationLevel = DegradationLevel.Severe;
                emergencyActionCount++;
            }
            
            // Force garbage collection
            if (enableMemoryOptimization)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                result.memoryOptimized = true;
                emergencyActionCount++;
            }
            
            OnOptimizationCompleted?.Invoke(result);
        }

        private void RunOptimizationCycle()
        {
            if (performanceMonitor == null) return;
            
            // Capture current performance snapshot
            var snapshot = CapturePerformanceSnapshot();
            performanceHistory.Enqueue(snapshot);
            
            // Keep only recent history
            if (performanceHistory.Count > 30) // 1 minute of history at 2s intervals
            {
                performanceHistory.Dequeue();
            }
            
            // Skip optimization if in emergency mode (emergency handles its own optimization)
            if (emergencyModeActive) return;
            
            // Determine optimal strategy
            var newStrategy = DetermineOptimalStrategy(snapshot);
            if (newStrategy != currentStrategy)
            {
                currentStrategy = newStrategy;
                OnOptimizationStrategyChanged?.Invoke(currentStrategy);
            }
            
            // Apply optimizations based on strategy
            var result = ApplyOptimizationStrategy(currentStrategy, snapshot);
            OnOptimizationCompleted?.Invoke(result);
        }

        private PerformanceSnapshot CapturePerformanceSnapshot()
        {
            var metrics = performanceMonitor.CurrentMetrics;
            var trainingMetrics = trainingCoordinator?.GetTrainingPerformanceMetrics();
            
            return new PerformanceSnapshot
            {
                timestamp = DateTime.Now,
                frameTimeMs = metrics.frameTimeMs,
                averageFrameTime = metrics.averageFrameTime,
                memoryUsageMB = metrics.memoryUsageMB,
                peakMemoryUsage = metrics.peakMemoryUsage,
                activeAgents = metrics.activeAgents,
                currentBatchSize = metrics.currentBatchSize,
                degradationLevel = metrics.degradationLevel,
                trainingAgentCount = trainingMetrics?.trainingAgentCount ?? 0,
                performanceScore = CalculatePerformanceScore(metrics)
            };
        }

        private float CalculatePerformanceScore(PerformanceMetrics metrics)
        {
            // Calculate composite performance score (lower is better)
            float frameTimeScore = metrics.frameTimeMs / targetFrameTimeMs;
            float memoryScore = metrics.memoryUsageMB / targetMemoryUsageMB;
            float agentScore = (float)metrics.activeAgents / targetActiveAgents;
            
            return (frameTimeScore + memoryScore + agentScore) / 3f;
        }

        private OptimizationStrategy DetermineOptimalStrategy(PerformanceSnapshot snapshot)
        {
            float performanceScore = snapshot.performanceScore;
            
            if (performanceScore > 1.2f)
                return OptimizationStrategy.Aggressive;
            else if (performanceScore > 1.0f)
                return OptimizationStrategy.Conservative;
            else if (performanceScore < 0.7f)
                return OptimizationStrategy.Performance;
            else
                return OptimizationStrategy.Balanced;
        }

        private OptimizationResult ApplyOptimizationStrategy(OptimizationStrategy strategy, PerformanceSnapshot snapshot)
        {
            var result = new OptimizationResult { strategy = strategy };
            
            switch (strategy)
            {
                case OptimizationStrategy.Performance:
                    ApplyPerformanceOptimizations(result, snapshot);
                    break;
                case OptimizationStrategy.Balanced:
                    ApplyBalancedOptimizations(result, snapshot);
                    break;
                case OptimizationStrategy.Conservative:
                    ApplyConservativeOptimizations(result, snapshot);
                    break;
                case OptimizationStrategy.Aggressive:
                    ApplyAggressiveOptimizations(result, snapshot);
                    break;
            }
            
            return result;
        }

        private void ApplyPerformanceOptimizations(OptimizationResult result, PerformanceSnapshot snapshot)
        {
            // Increase batch size and processing for better performance
            if (enableBatchSizeOptimization && snapshot.currentBatchSize < 64)
            {
                int newBatchSize = Mathf.Min(64, snapshot.currentBatchSize + 8);
                performanceMonitor.SetBatchSize(newBatchSize);
                result.batchSizeChanged = true;
                result.newBatchSize = newBatchSize;
            }
            
            // Reduce degradation level
            if (snapshot.degradationLevel > DegradationLevel.None)
            {
                var newLevel = (DegradationLevel)Mathf.Max(0, (int)snapshot.degradationLevel - 1);
                performanceMonitor.SetDegradationLevel(newLevel);
                result.degradationLevelChanged = true;
                result.newDegradationLevel = newLevel;
            }
        }

        private void ApplyBalancedOptimizations(OptimizationResult result, PerformanceSnapshot snapshot)
        {
            // Make small adjustments to maintain balance
            if (enableBatchSizeOptimization)
            {
                int targetBatch = 32; // Balanced batch size
                if (Mathf.Abs(snapshot.currentBatchSize - targetBatch) > 4)
                {
                    int newBatchSize = snapshot.currentBatchSize < targetBatch ? 
                        snapshot.currentBatchSize + 2 : snapshot.currentBatchSize - 2;
                    performanceMonitor.SetBatchSize(newBatchSize);
                    result.batchSizeChanged = true;
                    result.newBatchSize = newBatchSize;
                }
            }
        }

        private void ApplyConservativeOptimizations(OptimizationResult result, PerformanceSnapshot snapshot)
        {
            // Reduce processing to improve performance
            if (enableBatchSizeOptimization && snapshot.currentBatchSize > 16)
            {
                int newBatchSize = Mathf.Max(16, snapshot.currentBatchSize - 4);
                performanceMonitor.SetBatchSize(newBatchSize);
                result.batchSizeChanged = true;
                result.newBatchSize = newBatchSize;
            }
            
            // Increase degradation level slightly
            if (snapshot.degradationLevel < DegradationLevel.Medium)
            {
                var newLevel = (DegradationLevel)Mathf.Min(2, (int)snapshot.degradationLevel + 1);
                performanceMonitor.SetDegradationLevel(newLevel);
                result.degradationLevelChanged = true;
                result.newDegradationLevel = newLevel;
            }
        }

        private void ApplyAggressiveOptimizations(OptimizationResult result, PerformanceSnapshot snapshot)
        {
            // Aggressively reduce processing
            if (enableBatchSizeOptimization)
            {
                int newBatchSize = Mathf.Max(8, snapshot.currentBatchSize / 2);
                performanceMonitor.SetBatchSize(newBatchSize);
                result.batchSizeChanged = true;
                result.newBatchSize = newBatchSize;
            }
            
            // Apply high degradation
            if (snapshot.degradationLevel < DegradationLevel.High)
            {
                performanceMonitor.SetDegradationLevel(DegradationLevel.High);
                result.degradationLevelChanged = true;
                result.newDegradationLevel = DegradationLevel.High;
            }
            
            // Force memory cleanup
            if (enableMemoryOptimization)
            {
                System.GC.Collect();
                result.memoryOptimized = true;
            }
        }

        /// <summary>
        /// Get comprehensive performance report
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            var report = new PerformanceReport
            {
                currentSnapshot = performanceHistory.LastOrDefault(),
                optimizationStrategy = currentStrategy,
                emergencyModeActive = emergencyModeActive,
                optimizationHistory = performanceHistory.ToList(),
                componentScores = new Dictionary<string, float>(componentOptimizationScores)
            };
            
            if (emergencyModeActive)
            {
                report.emergencyDuration = Time.time - emergencyModeStartTime;
                report.emergencyActionCount = emergencyActionCount;
            }
            
            return report;
        }

        /// <summary>
        /// Force immediate optimization
        /// </summary>
        public void ForceOptimization()
        {
            RunOptimizationCycle();
        }

        /// <summary>
        /// Reset optimization state
        /// </summary>
        public void ResetOptimization()
        {
            currentStrategy = OptimizationStrategy.Balanced;
            emergencyModeActive = false;
            performanceHistory.Clear();
            componentOptimizationScores.Clear();
            
            // Reset to default settings
            if (performanceMonitor != null)
            {
                performanceMonitor.ResetToOptimalPerformance();
            }
            
            Debug.Log("Performance optimization state reset");
        }
    }

    /// <summary>
    /// Optimization strategies for different performance scenarios
    /// </summary>
    public enum OptimizationStrategy
    {
        Performance,    // Prioritize performance over learning quality
        Balanced,       // Balance performance and learning
        Conservative,   // Slight performance reduction for stability
        Aggressive,     // Significant performance reduction for stability
        Emergency       // Emergency measures for critical performance issues
    }

    /// <summary>
    /// Snapshot of performance metrics at a point in time
    /// </summary>
    [Serializable]
    public class PerformanceSnapshot
    {
        public DateTime timestamp;
        public float frameTimeMs;
        public float averageFrameTime;
        public float memoryUsageMB;
        public float peakMemoryUsage;
        public int activeAgents;
        public int currentBatchSize;
        public DegradationLevel degradationLevel;
        public int trainingAgentCount;
        public float performanceScore;
    }

    /// <summary>
    /// Result of an optimization operation
    /// </summary>
    [Serializable]
    public class OptimizationResult
    {
        public OptimizationStrategy strategy;
        public bool batchSizeChanged;
        public int newBatchSize;
        public bool degradationLevelChanged;
        public DegradationLevel newDegradationLevel;
        public bool memoryOptimized;
        public DateTime timestamp = DateTime.Now;
    }

    /// <summary>
    /// Comprehensive performance report
    /// </summary>
    [Serializable]
    public class PerformanceReport
    {
        public PerformanceSnapshot currentSnapshot;
        public OptimizationStrategy optimizationStrategy;
        public bool emergencyModeActive;
        public float emergencyDuration;
        public int emergencyActionCount;
        public List<PerformanceSnapshot> optimizationHistory;
        public Dictionary<string, float> componentScores;
    }
}
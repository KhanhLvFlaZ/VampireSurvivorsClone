using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

namespace Vampire.RL
{
    /// <summary>
    /// Centralized error handling and recovery system for RL components
    /// Implements Requirements 3.5, 4.3 - fallback behavior and error recovery
    /// </summary>
    public static class ErrorHandler
    {
        private static readonly Dictionary<string, int> errorCounts = new Dictionary<string, int>();
        private static readonly Queue<ErrorLog> recentErrors = new Queue<ErrorLog>();
        private const int MAX_ERROR_HISTORY = 100;
        private const int MAX_RETRY_ATTEMPTS = 3;
        
        public static event Action<ErrorLog> OnErrorLogged;
        
        /// <summary>
        /// Log an error with context and automatic recovery suggestions
        /// </summary>
        public static void LogError(string component, string operation, Exception exception, object context = null)
        {
            var errorLog = new ErrorLog
            {
                timestamp = DateTime.Now,
                component = component,
                operation = operation,
                exception = exception,
                context = context?.ToString(),
                severity = DetermineSeverity(exception)
            };
            
            RecordError(errorLog);
            
            // Log to Unity console with appropriate level
            switch (errorLog.severity)
            {
                case ErrorSeverity.Critical:
                    Debug.LogError($"[RL CRITICAL] {component}.{operation}: {exception.Message}\nContext: {errorLog.context}");
                    break;
                case ErrorSeverity.High:
                    Debug.LogError($"[RL ERROR] {component}.{operation}: {exception.Message}");
                    break;
                case ErrorSeverity.Medium:
                    Debug.LogWarning($"[RL WARNING] {component}.{operation}: {exception.Message}");
                    break;
                case ErrorSeverity.Low:
                    Debug.Log($"[RL INFO] {component}.{operation}: {exception.Message}");
                    break;
            }
            
            // Trigger recovery if needed
            TriggerRecoveryIfNeeded(errorLog);
        }
        
        /// <summary>
        /// Log a performance issue with degradation suggestions
        /// </summary>
        public static void LogPerformanceIssue(string component, string metric, float actualValue, float expectedValue, string suggestion = null)
        {
            var performanceLog = new PerformanceIssue
            {
                timestamp = DateTime.Now,
                component = component,
                metric = metric,
                actualValue = actualValue,
                expectedValue = expectedValue,
                suggestion = suggestion ?? GeneratePerformanceSuggestion(metric, actualValue, expectedValue)
            };
            
            Debug.LogWarning($"[RL PERFORMANCE] {component}: {metric} = {actualValue:F2} (expected ≤ {expectedValue:F2}). Suggestion: {performanceLog.suggestion}");
            
            // Apply automatic performance degradation if needed
            ApplyPerformanceDegradation(performanceLog);
        }
        
        /// <summary>
        /// Attempt to recover from a corrupted behavior profile
        /// </summary>
        public static BehaviorProfile RecoverCorruptedProfile(MonsterType monsterType, string originalPath, Exception corruptionException)
        {
            LogError("BehaviorProfileManager", "LoadProfile", corruptionException, $"MonsterType: {monsterType}, Path: {originalPath}");
            
            // Try backup recovery first
            var backupProfile = TryLoadBackupProfile(monsterType, originalPath);
            if (backupProfile != null)
            {
                Debug.Log($"[RL RECOVERY] Successfully recovered {monsterType} profile from backup");
                return backupProfile;
            }
            
            // Try loading a different player's profile as template
            var templateProfile = TryLoadTemplateProfile(monsterType);
            if (templateProfile != null)
            {
                Debug.Log($"[RL RECOVERY] Using template profile for {monsterType}");
                return templateProfile;
            }
            
            // Fall back to default behavior profile
            var defaultProfile = CreateDefaultProfile(monsterType);
            Debug.Log($"[RL RECOVERY] Created default profile for {monsterType}");
            return defaultProfile;
        }
        
        /// <summary>
        /// Handle neural network failures with fallback strategies
        /// </summary>
        public static INeuralNetwork RecoverFailedNetwork(NetworkArchitecture architecture, int inputSize, int outputSize, int[] hiddenLayers, Exception networkException)
        {
            LogError("NeuralNetwork", "Initialize", networkException, $"Architecture: {architecture}, Input: {inputSize}, Output: {outputSize}");
            
            try
            {
                // Try simpler architecture first
                if (architecture != NetworkArchitecture.Simple)
                {
                    Debug.Log("[RL RECOVERY] Falling back to Simple network architecture");
                    var simpleNetwork = new SimpleNeuralNetwork();
                    simpleNetwork.Initialize(inputSize, outputSize, new int[] { 32, 16 }, NetworkArchitecture.Simple);
                    return simpleNetwork;
                }
                
                // Try even simpler network
                Debug.Log("[RL RECOVERY] Using minimal network configuration");
                var minimalNetwork = new SimpleNeuralNetwork();
                minimalNetwork.Initialize(inputSize, outputSize, new int[] { 16 }, NetworkArchitecture.Simple);
                return minimalNetwork;
            }
            catch (Exception fallbackException)
            {
                LogError("NeuralNetwork", "FallbackRecovery", fallbackException);
                
                // Last resort: create a dummy network that always returns zeros
                return new DummyNeuralNetwork(inputSize, outputSize);
            }
        }
        
        /// <summary>
        /// Handle agent initialization failures
        /// </summary>
        public static ILearningAgent RecoverFailedAgent(MonsterType monsterType, ActionSpace actionSpace, Exception agentException)
        {
            LogError("LearningAgent", "Initialize", agentException, $"MonsterType: {monsterType}");
            
            try
            {
                // Try creating a simplified agent
                var fallbackAgent = new FallbackLearningAgent();
                fallbackAgent.Initialize(monsterType, actionSpace);
                Debug.Log($"[RL RECOVERY] Created fallback agent for {monsterType}");
                return fallbackAgent;
            }
            catch (Exception fallbackException)
            {
                LogError("LearningAgent", "FallbackRecovery", fallbackException);
                
                // Return null - caller should handle with default scripted behavior
                return null;
            }
        }
        
        /// <summary>
        /// Check if a component should be disabled due to repeated failures
        /// </summary>
        public static bool ShouldDisableComponent(string componentName)
        {
            string key = $"component_failure_{componentName}";
            if (errorCounts.TryGetValue(key, out int count))
            {
                return count >= MAX_RETRY_ATTEMPTS;
            }
            return false;
        }
        
        /// <summary>
        /// Reset error count for a component (e.g., after successful recovery)
        /// </summary>
        public static void ResetComponentErrors(string componentName)
        {
            string key = $"component_failure_{componentName}";
            errorCounts.Remove(key);
            Debug.Log($"[RL RECOVERY] Reset error count for {componentName}");
        }
        
        /// <summary>
        /// Get recent error statistics for monitoring
        /// </summary>
        public static ErrorStatistics GetErrorStatistics()
        {
            var stats = new ErrorStatistics();
            
            foreach (var error in recentErrors)
            {
                stats.totalErrors++;
                switch (error.severity)
                {
                    case ErrorSeverity.Critical:
                        stats.criticalErrors++;
                        break;
                    case ErrorSeverity.High:
                        stats.highSeverityErrors++;
                        break;
                    case ErrorSeverity.Medium:
                        stats.mediumSeverityErrors++;
                        break;
                    case ErrorSeverity.Low:
                        stats.lowSeverityErrors++;
                        break;
                }
            }
            
            return stats;
        }
        
        // Private helper methods
        private static void RecordError(ErrorLog errorLog)
        {
            // Add to recent errors queue
            recentErrors.Enqueue(errorLog);
            if (recentErrors.Count > MAX_ERROR_HISTORY)
            {
                recentErrors.Dequeue();
            }
            
            // Update error counts
            string key = $"{errorLog.component}_{errorLog.operation}";
            errorCounts[key] = errorCounts.GetValueOrDefault(key, 0) + 1;
            
            // Component-level error tracking
            string componentKey = $"component_failure_{errorLog.component}";
            errorCounts[componentKey] = errorCounts.GetValueOrDefault(componentKey, 0) + 1;
            
            // Trigger event
            OnErrorLogged?.Invoke(errorLog);
        }
        
        private static ErrorSeverity DetermineSeverity(Exception exception)
        {
            switch (exception)
            {
                case OutOfMemoryException:
                case StackOverflowException:
                    return ErrorSeverity.Critical;
                case UnauthorizedAccessException:
                case DirectoryNotFoundException:
                case FileNotFoundException:
                    return ErrorSeverity.High;
                case ArgumentException:
                case InvalidOperationException:
                    return ErrorSeverity.Medium;
                default:
                    return ErrorSeverity.Low;
            }
        }
        
        private static void TriggerRecoveryIfNeeded(ErrorLog errorLog)
        {
            if (errorLog.severity >= ErrorSeverity.High)
            {
                string componentKey = $"component_failure_{errorLog.component}";
                int failureCount = errorCounts.GetValueOrDefault(componentKey, 0);
                
                if (failureCount >= MAX_RETRY_ATTEMPTS)
                {
                    Debug.LogError($"[RL RECOVERY] Component {errorLog.component} has failed {failureCount} times. Consider disabling.");
                }
            }
        }
        
        private static string GeneratePerformanceSuggestion(string metric, float actual, float expected)
        {
            switch (metric.ToLower())
            {
                case "frametime":
                case "processingtime":
                    return "Reduce batch size, limit agents per frame, or enable adaptive processing";
                case "memory":
                case "memoryusage":
                    return "Clear experience buffers, compress profiles, or reduce network size";
                case "agentcount":
                    return "Limit concurrent agents or use object pooling";
                default:
                    return "Consider reducing computational load or optimizing algorithms";
            }
        }
        
        private static void ApplyPerformanceDegradation(PerformanceIssue issue)
        {
            // This would integrate with the main RL system to apply degradation
            // For now, just log the suggestion
            Debug.Log($"[RL DEGRADATION] Applying performance degradation: {issue.suggestion}");
        }
        
        private static BehaviorProfile TryLoadBackupProfile(MonsterType monsterType, string originalPath)
        {
            try
            {
                string backupPath = originalPath + ".backup";
                if (File.Exists(backupPath))
                {
                    string json = File.ReadAllText(backupPath);
                    var profile = JsonUtility.FromJson<BehaviorProfile>(json);
                    if (profile != null && profile.IsValid())
                    {
                        return profile;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RL RECOVERY] Backup profile also corrupted: {ex.Message}");
            }
            
            return null;
        }
        
        private static BehaviorProfile TryLoadTemplateProfile(MonsterType monsterType)
        {
            try
            {
                // Try to find any valid profile for this monster type from other players
                string profileDir = Path.Combine(Application.persistentDataPath, "BehaviorProfiles");
                if (Directory.Exists(profileDir))
                {
                    var files = Directory.GetFiles(profileDir, $"{monsterType}_*.rlprofile");
                    foreach (string file in files)
                    {
                        try
                        {
                            string json = File.ReadAllText(file);
                            var profile = JsonUtility.FromJson<BehaviorProfile>(json);
                            if (profile != null && profile.IsValid())
                            {
                                // Reset player-specific data
                                profile.playerProfileId = "recovered";
                                profile.lastUpdated = DateTime.Now;
                                return profile;
                            }
                        }
                        catch
                        {
                            // Continue to next file
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RL RECOVERY] Template profile search failed: {ex.Message}");
            }
            
            return null;
        }
        
        private static BehaviorProfile CreateDefaultProfile(MonsterType monsterType)
        {
            var profile = BehaviorProfile.Create(monsterType, "default", NetworkArchitecture.Simple);
            
            // Initialize with reasonable default weights (small random values)
            int networkSize = 32 * 64 + 64 * 32 + 32 * 15; // Approximate network size
            profile.networkWeights = new float[networkSize];
            profile.networkBiases = new float[64 + 32 + 15]; // Approximate bias count
            
            // Small random initialization
            for (int i = 0; i < profile.networkWeights.Length; i++)
            {
                profile.networkWeights[i] = UnityEngine.Random.Range(-0.1f, 0.1f);
            }
            
            for (int i = 0; i < profile.networkBiases.Length; i++)
            {
                profile.networkBiases[i] = 0f;
            }
            
            profile.playerProfileId = "default";
            profile.lastUpdated = DateTime.Now;
            
            return profile;
        }
    }
    
    /// <summary>
    /// Error log entry for tracking and analysis
    /// </summary>
    [Serializable]
    public class ErrorLog
    {
        public DateTime timestamp;
        public string component;
        public string operation;
        public Exception exception;
        public string context;
        public ErrorSeverity severity;
    }
    
    /// <summary>
    /// Performance issue log for monitoring and degradation
    /// </summary>
    [Serializable]
    public class PerformanceIssue
    {
        public DateTime timestamp;
        public string component;
        public string metric;
        public float actualValue;
        public float expectedValue;
        public string suggestion;
    }
    
    /// <summary>
    /// Error severity levels for prioritization
    /// </summary>
    public enum ErrorSeverity
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
    
    /// <summary>
    /// Error statistics for monitoring system health
    /// </summary>
    [Serializable]
    public class ErrorStatistics
    {
        public int totalErrors;
        public int criticalErrors;
        public int highSeverityErrors;
        public int mediumSeverityErrors;
        public int lowSeverityErrors;
        
        public float CriticalErrorRate => totalErrors > 0 ? (float)criticalErrors / totalErrors : 0f;
        public float HighSeverityErrorRate => totalErrors > 0 ? (float)highSeverityErrors / totalErrors : 0f;
    }
}
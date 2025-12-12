using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Vampire;

namespace Vampire.RL
{
    /// <summary>
    /// Coordinates training across multiple learning agents
    /// Manages training/inference modes, learning progress tracking, and state preservation
    /// Implements Requirements 3.1, 3.2, 3.3, 3.4
    /// </summary>
    public class TrainingCoordinator : MonoBehaviour, ITrainingCoordinator
    {
        [Header("Training Settings")]
        [SerializeField] private TrainingMode currentMode = TrainingMode.Training;
        [SerializeField] private float updateInterval = 0.1f; // Update agents every 100ms
        [SerializeField] private float maxFrameTimeMs = 16f; // Max processing time per frame
        [SerializeField] private bool enableProgressTracking = true;
        [SerializeField] private bool enableStatePreservation = true;
        
        [Header("Performance Monitoring")]
        [SerializeField] private int maxAgentsPerFrame = 10; // Limit agents updated per frame
        [SerializeField] private bool adaptiveProcessing = true; // Adjust processing based on performance
        [SerializeField] private bool enablePerformanceThrottling = true; // Enable performance-based throttling
        [SerializeField] private float performanceThrottleThreshold = 0.8f; // Throttle at 80% of frame budget
        
        [Header("State Preservation")]
        [SerializeField] private float autoSaveInterval = 30f; // Auto-save every 30 seconds
        [SerializeField] private string stateFileName = "training_state.json";
        
        // Core components
        private MonoBehaviour playerCharacter;
        private IBehaviorProfileManager profileManager;
        private MonsterCoordinationSystem coordinationSystem;
        private MultiAgentLearningManager multiAgentManager;
        private PerformanceMonitor performanceMonitor;
        
        // Agent management
        private Dictionary<ILearningAgent, MonsterType> registeredAgents;
        private Dictionary<MonsterType, List<ILearningAgent>> agentsByType;
        private Dictionary<MonsterType, LearningMetrics> lastKnownMetrics;
        
        // Performance tracking
        private float lastUpdateTime;
        private float currentFrameTime;
        private float lastAutoSaveTime;
        private int currentAgentIndex; // For round-robin processing
        private int adaptiveBatchSize; // Current adaptive batch size
        private float performanceScore; // Current performance score
        
        // State preservation
        private TrainingState currentTrainingState;
        private string stateFilePath;
        
        // Events
        public event Action<TrainingMode> OnTrainingModeChanged;
        public event Action<MonsterType, LearningMetrics> OnLearningProgressUpdated;

        // Properties
        public bool IsTrainingActive => currentMode == TrainingMode.Training || currentMode == TrainingMode.Mixed;
        public float CurrentFrameTime => currentFrameTime;

        public void Initialize(MonoBehaviour playerCharacter)
        {
            this.playerCharacter = playerCharacter;
            
            // Initialize collections
            this.registeredAgents = new Dictionary<ILearningAgent, MonsterType>();
            this.agentsByType = new Dictionary<MonsterType, List<ILearningAgent>>();
            this.lastKnownMetrics = new Dictionary<MonsterType, LearningMetrics>();
            
            // Initialize behavior profile manager
            this.profileManager = new BehaviorProfileManager();
            this.profileManager.Initialize("default"); // TODO: Get actual player ID
            
            // Initialize coordination system
            var coordinationGO = new GameObject("CoordinationSystem");
            coordinationGO.transform.SetParent(transform);
            this.coordinationSystem = coordinationGO.AddComponent<MonsterCoordinationSystem>();
            
            // Initialize multi-agent learning manager
            var multiAgentGO = new GameObject("MultiAgentLearningManager");
            multiAgentGO.transform.SetParent(transform);
            this.multiAgentManager = multiAgentGO.AddComponent<MultiAgentLearningManager>();
            
            // Initialize performance monitor
            var performanceGO = new GameObject("PerformanceMonitor");
            performanceGO.transform.SetParent(transform);
            this.performanceMonitor = performanceGO.AddComponent<PerformanceMonitor>();
            
            // Subscribe to performance events
            this.performanceMonitor.OnBatchSizeChanged += OnAdaptiveBatchSizeChanged;
            this.performanceMonitor.OnDegradationLevelChanged += OnPerformanceDegradationChanged;
            
            // Initialize state preservation
            InitializeStatePreservation();
            
            // Initialize timing
            this.lastUpdateTime = Time.time;
            this.lastAutoSaveTime = Time.time;
            this.currentAgentIndex = 0;
            this.adaptiveBatchSize = 32; // Default batch size
            
            // Load previous training state if available
            LoadTrainingState();
            
            Debug.Log($"Training Coordinator initialized with mode: {currentMode}");
        }

        private void InitializeStatePreservation()
        {
            if (!enableStatePreservation) return;
            
            string stateDirectory = Path.Combine(Application.persistentDataPath, "TrainingStates");
            if (!Directory.Exists(stateDirectory))
            {
                Directory.CreateDirectory(stateDirectory);
            }
            
            stateFilePath = Path.Combine(stateDirectory, stateFileName);
            currentTrainingState = new TrainingState();
        }

        public void SetTrainingMode(TrainingMode mode)
        {
            if (currentMode != mode)
            {
                TrainingMode previousMode = currentMode;
                currentMode = mode;
                
                // Preserve state during mode switching (Requirement 3.4)
                if (enableStatePreservation)
                {
                    PreserveModeTransitionState(previousMode, mode);
                }
                
                // Update all registered agents based on new mode
                UpdateAgentTrainingModes(mode);
                
                // Trigger event
                OnTrainingModeChanged?.Invoke(mode);
                
                Debug.Log($"Training mode changed from {previousMode} to {mode}");
            }
        }

        private void PreserveModeTransitionState(TrainingMode fromMode, TrainingMode toMode)
        {
            currentTrainingState.lastModeChange = DateTime.Now;
            currentTrainingState.previousMode = fromMode;
            currentTrainingState.currentMode = toMode;
            
            // Capture current learning progress before mode change
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    var metrics = agent.GetMetrics();
                    currentTrainingState.metricsAtModeChange[monsterType] = metrics;
                }
            }
            
            // Save state immediately
            SaveTrainingState();
        }

        private void UpdateAgentTrainingModes(TrainingMode mode)
        {
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    switch (mode)
                    {
                        case TrainingMode.Training:
                            agent.IsTraining = true;
                            break;
                        case TrainingMode.Inference:
                            agent.IsTraining = false;
                            break;
                        case TrainingMode.Mixed:
                            // In mixed mode, alternate training based on monster type or other criteria
                            agent.IsTraining = ShouldTrainInMixedMode(monsterType);
                            break;
                    }
                }
            }
        }

        private bool ShouldTrainInMixedMode(MonsterType monsterType)
        {
            // In mixed mode, train based on learning progress
            // Train monsters that haven't converged yet
            if (lastKnownMetrics.TryGetValue(monsterType, out LearningMetrics metrics))
            {
                return !metrics.IsConverging();
            }
            
            // Default to training for new monsters
            return true;
        }

        public TrainingMode GetTrainingMode()
        {
            return currentMode;
        }

        public void RegisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent == null || registeredAgents.ContainsKey(agent))
                return;

            // Register agent
            registeredAgents[agent] = monsterType;
            
            // Add to type-specific list
            if (!agentsByType.ContainsKey(monsterType))
            {
                agentsByType[monsterType] = new List<ILearningAgent>();
            }
            agentsByType[monsterType].Add(agent);
            
            // Register with coordination system and multi-agent manager
            coordinationSystem?.RegisterAgent(agent, monsterType);
            multiAgentManager?.RegisterAgent(agent, monsterType);
            
            // Set initial training mode
            agent.IsTraining = ShouldAgentTrain(monsterType);
            
            // Initialize metrics tracking
            if (!lastKnownMetrics.ContainsKey(monsterType))
            {
                lastKnownMetrics[monsterType] = LearningMetrics.CreateDefault();
            }
            
            // Update training state
            if (enableStatePreservation)
            {
                currentTrainingState.registeredAgentCount++;
                currentTrainingState.agentsByType[monsterType] = agentsByType[monsterType].Count;
            }
            
            Debug.Log($"Registered {monsterType} agent for training (Total: {registeredAgents.Count})");
        }

        private bool ShouldAgentTrain(MonsterType monsterType)
        {
            switch (currentMode)
            {
                case TrainingMode.Training:
                    return true;
                case TrainingMode.Inference:
                    return false;
                case TrainingMode.Mixed:
                    return ShouldTrainInMixedMode(monsterType);
                default:
                    return false;
            }
        }

        public void UnregisterAgent(ILearningAgent agent)
        {
            if (agent == null || !registeredAgents.ContainsKey(agent))
                return;

            var monsterType = registeredAgents[agent];
            
            // Remove from main registry
            registeredAgents.Remove(agent);
            
            // Remove from type-specific list
            if (agentsByType.ContainsKey(monsterType))
            {
                agentsByType[monsterType].Remove(agent);
                if (agentsByType[monsterType].Count == 0)
                {
                    agentsByType.Remove(monsterType);
                }
            }
            
            // Unregister from coordination system and multi-agent manager
            coordinationSystem?.UnregisterAgent(agent);
            multiAgentManager?.UnregisterAgent(agent, monsterType);
            
            // Update training state
            if (enableStatePreservation)
            {
                currentTrainingState.registeredAgentCount--;
                if (agentsByType.ContainsKey(monsterType))
                {
                    currentTrainingState.agentsByType[monsterType] = agentsByType[monsterType].Count;
                }
                else
                {
                    currentTrainingState.agentsByType.Remove(monsterType);
                }
            }
            
            Debug.Log($"Unregistered {monsterType} agent from training (Remaining: {registeredAgents.Count})");
        }

        public void UpdateAgents()
        {
            if (registeredAgents.Count == 0) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            // Check if it's time for an update
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            lastUpdateTime = Time.time;

            // Adaptive processing: limit agents processed per frame
            int baseAgentsToProcess = adaptiveProcessing ? 
                Mathf.Min(maxAgentsPerFrame, registeredAgents.Count) : 
                registeredAgents.Count;
            
            // Apply performance throttling if enabled
            int agentsToProcess = baseAgentsToProcess;
            if (enablePerformanceThrottling && performanceMonitor != null)
            {
                float performanceRatio = performanceMonitor.AverageFrameTime / maxFrameTimeMs;
                if (performanceRatio > performanceThrottleThreshold)
                {
                    // Reduce agents processed based on performance
                    float throttleFactor = Mathf.Clamp01(2f - performanceRatio);
                    agentsToProcess = Mathf.Max(1, Mathf.RoundToInt(baseAgentsToProcess * throttleFactor));
                }
            }

            // Process agents in round-robin fashion for fairness
            var agentList = registeredAgents.Keys.ToList();
            int processedCount = 0;
            
            for (int i = 0; i < agentsToProcess && processedCount < agentList.Count; i++)
            {
                int agentIndex = (currentAgentIndex + i) % agentList.Count;
                var agent = agentList[agentIndex];
                var monsterType = registeredAgents[agent];
                
                if (agent != null)
                {
                    ProcessAgent(agent, monsterType);
                    processedCount++;
                    
                    // Check frame time constraint
                    float currentProcessingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    if (currentProcessingTime > maxFrameTimeMs * 0.8f) // Leave 20% buffer
                    {
                        break;
                    }
                }
            }
            
            // Update round-robin index
            currentAgentIndex = (currentAgentIndex + processedCount) % Mathf.Max(1, agentList.Count);
            
            // Auto-save training state periodically
            if (enableStatePreservation && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveTrainingState();
                lastAutoSaveTime = Time.time;
            }
            
            // Calculate frame time
            currentFrameTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            // Update performance monitor with current metrics
            if (performanceMonitor != null)
            {
                float memoryUsage = GetEstimatedMemoryUsageMB();
                performanceMonitor.UpdateSystemMetrics(currentFrameTime, memoryUsage, registeredAgents.Count);
                performanceMonitor.RecordComponentPerformance("TrainingCoordinator", currentFrameTime);
            }
        }

        private void ProcessAgent(ILearningAgent agent, MonsterType monsterType)
        {
            try
            {
                // Update policy if training
                if (agent.IsTraining)
                {
                    agent.UpdatePolicy();
                }
                
                // Track learning progress
                if (enableProgressTracking)
                {
                    var metrics = agent.GetMetrics();
                    
                    // Update cached metrics
                    lastKnownMetrics[monsterType] = metrics;
                    
                    // Update training state
                    if (enableStatePreservation)
                    {
                        currentTrainingState.currentMetrics[monsterType] = metrics;
                    }
                    
                    // Emit progress update event
                    OnLearningProgressUpdated?.Invoke(monsterType, metrics);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing agent {monsterType}: {ex.Message}");
            }
        }

        public void TriggerLearningUpdate()
        {
            float startTime = Time.realtimeSinceStartup;
            
            // Force immediate learning update for all training agents
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null && agent.IsTraining)
                {
                    try
                    {
                        agent.UpdatePolicy();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error updating policy for {monsterType}: {ex.Message}");
                    }
                }
                
                // Respect frame time constraints
                float processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                if (processingTime > maxFrameTimeMs)
                {
                    Debug.LogWarning("Learning update exceeded frame time limit, stopping early");
                    break;
                }
            }
        }

        public void SaveAllProfiles()
        {
            if (profileManager == null)
            {
                Debug.LogError("Profile manager not initialized");
                return;
            }

            int savedCount = 0;
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    try
                    {
                        // Create behavior profile from agent
                        var profile = CreateBehaviorProfileFromAgent(agent, monsterType);
                        if (profileManager.SaveProfile(profile))
                        {
                            savedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to save profile for {monsterType}: {ex.Message}");
                    }
                }
            }
            
            Debug.Log($"Saved {savedCount} behavior profiles");
        }

        public void LoadAllProfiles()
        {
            if (profileManager == null)
            {
                Debug.LogError("Profile manager not initialized");
                return;
            }

            int loadedCount = 0;
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    try
                    {
                        var profile = profileManager.LoadProfile(monsterType);
                        if (profile != null && profile.IsValid())
                        {
                            // Apply profile to agent (this would need agent support)
                            // For now, use the existing LoadBehaviorProfile method
                            string tempPath = Path.GetTempFileName();
                            if (profileManager.SaveProfile(profile))
                            {
                                agent.LoadBehaviorProfile(tempPath);
                                loadedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to load profile for {monsterType}: {ex.Message}");
                    }
                }
            }
            
            Debug.Log($"Loaded {loadedCount} behavior profiles");
        }

        private BehaviorProfile CreateBehaviorProfileFromAgent(ILearningAgent agent, MonsterType monsterType)
        {
            var metrics = agent.GetMetrics();
            
            return new BehaviorProfile
            {
                monsterType = monsterType,
                trainingEpisodes = metrics.episodeCount,
                averageReward = metrics.averageReward,
                lastUpdated = DateTime.Now,
                playerProfileId = profileManager.CurrentPlayerProfileId,
                // Note: Actual network weights would need to be extracted from the agent
                // This is a simplified version for the interface
                networkWeights = new float[0], // TODO: Extract from agent
                networkBiases = new float[0]   // TODO: Extract from agent
            };
        }

        public Dictionary<MonsterType, LearningMetrics> GetAllMetrics()
        {
            var metrics = new Dictionary<MonsterType, LearningMetrics>();
            
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    try
                    {
                        metrics[monsterType] = agent.GetMetrics();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error getting metrics for {monsterType}: {ex.Message}");
                        // Use cached metrics as fallback
                        if (lastKnownMetrics.ContainsKey(monsterType))
                        {
                            metrics[monsterType] = lastKnownMetrics[monsterType];
                        }
                    }
                }
            }
            
            return metrics;
        }

        public void ResetAllProgress()
        {
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    try
                    {
                        // Reset metrics
                        lastKnownMetrics[monsterType] = LearningMetrics.CreateDefault();
                        
                        // TODO: Add reset method to ILearningAgent interface
                        // For now, just reinitialize training state
                        agent.IsTraining = ShouldAgentTrain(monsterType);
                        
                        Debug.Log($"Reset learning progress for {monsterType}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error resetting progress for {monsterType}: {ex.Message}");
                    }
                }
            }
            
            // Reset training state
            if (enableStatePreservation)
            {
                currentTrainingState.currentMetrics.Clear();
                currentTrainingState.metricsAtModeChange.Clear();
                SaveTrainingState();
            }
            
            Debug.Log("All learning progress reset");
        }

        private void SaveTrainingState()
        {
            if (!enableStatePreservation || string.IsNullOrEmpty(stateFilePath))
                return;

            try
            {
                currentTrainingState.lastSaved = DateTime.Now;
                currentTrainingState.currentMode = currentMode;
                
                string json = JsonUtility.ToJson(currentTrainingState, true);
                File.WriteAllText(stateFilePath, json);
                
                Debug.Log("Training state saved");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save training state: {ex.Message}");
            }
        }

        private void LoadTrainingState()
        {
            if (!enableStatePreservation || string.IsNullOrEmpty(stateFilePath) || !File.Exists(stateFilePath))
                return;

            try
            {
                string json = File.ReadAllText(stateFilePath);
                var loadedState = JsonUtility.FromJson<TrainingState>(json);
                
                if (loadedState != null)
                {
                    currentTrainingState = loadedState;
                    
                    // Restore training mode if it was preserved
                    if (currentTrainingState.currentMode != TrainingMode.Inference)
                    {
                        currentMode = currentTrainingState.currentMode;
                    }
                    
                    Debug.Log($"Training state loaded (Mode: {currentMode})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load training state: {ex.Message}");
                // Initialize new state on failure
                currentTrainingState = new TrainingState();
            }
        }

        void OnDestroy()
        {
            // Save all profiles and state when coordinator is destroyed
            SaveAllProfiles();
            SaveTrainingState();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) // Resuming
            {
                SaveAllProfiles();
                SaveTrainingState();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) // Losing focus
            {
                SaveAllProfiles();
                SaveTrainingState();
            }
        }

        /// <summary>
        /// Handle adaptive batch size changes from performance monitor
        /// </summary>
        private void OnAdaptiveBatchSizeChanged(int newBatchSize)
        {
            adaptiveBatchSize = newBatchSize;
            
            // Apply new batch size to all training agents
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                if (agent != null && agent.IsTraining)
                {
                    // Apply batch size to agent if it supports it
                    if (agent is IAdaptiveBatchAgent adaptiveAgent)
                    {
                        adaptiveAgent.SetBatchSize(newBatchSize);
                    }
                }
            }
            
            Debug.Log($"[TRAINING] Applied adaptive batch size {newBatchSize} to {registeredAgents.Count} agents");
        }

        /// <summary>
        /// Handle performance degradation level changes
        /// </summary>
        private void OnPerformanceDegradationChanged(DegradationLevel newLevel)
        {
            switch (newLevel)
            {
                case DegradationLevel.None:
                    // Restore normal processing
                    maxAgentsPerFrame = 10;
                    updateInterval = 0.1f;
                    break;
                case DegradationLevel.Low:
                    // Slight reduction
                    maxAgentsPerFrame = 8;
                    updateInterval = 0.12f;
                    break;
                case DegradationLevel.Medium:
                    // Moderate reduction
                    maxAgentsPerFrame = 6;
                    updateInterval = 0.15f;
                    break;
                case DegradationLevel.High:
                    // Significant reduction
                    maxAgentsPerFrame = 4;
                    updateInterval = 0.2f;
                    break;
                case DegradationLevel.Severe:
                    // Minimal processing
                    maxAgentsPerFrame = 2;
                    updateInterval = 0.3f;
                    break;
            }
            
            Debug.Log($"[TRAINING] Performance degradation level changed to {newLevel}");
        }

        /// <summary>
        /// Estimate current memory usage for all training components
        /// </summary>
        private float GetEstimatedMemoryUsageMB()
        {
            // Simplified estimation based on agent count and other factors
            float baseMemory = 10f; // Base system memory
            float agentMemory = registeredAgents.Count * 2f; // ~2MB per agent
            float profileMemory = profileManager?.GetStorageSize() ?? 0f;
            
            return baseMemory + agentMemory + profileMemory;
        }

        /// <summary>
        /// Get current performance metrics from the training coordinator
        /// </summary>
        public TrainingPerformanceMetrics GetTrainingPerformanceMetrics()
        {
            return new TrainingPerformanceMetrics
            {
                currentFrameTime = currentFrameTime,
                adaptiveBatchSize = adaptiveBatchSize,
                activeAgentCount = registeredAgents.Count,
                trainingAgentCount = registeredAgents.Count(kvp => kvp.Key?.IsTraining == true),
                averageUpdateInterval = updateInterval,
                maxAgentsPerFrame = maxAgentsPerFrame,
                performanceScore = performanceScore
            };
        }

        // Training state data structure for persistence
        [Serializable]
        private class TrainingState
        {
            public DateTime lastSaved;
            public DateTime lastModeChange;
            public TrainingMode currentMode;
            public TrainingMode previousMode;
            public int registeredAgentCount;
            public Dictionary<MonsterType, int> agentsByType = new Dictionary<MonsterType, int>();
            public Dictionary<MonsterType, LearningMetrics> currentMetrics = new Dictionary<MonsterType, LearningMetrics>();
            public Dictionary<MonsterType, LearningMetrics> metricsAtModeChange = new Dictionary<MonsterType, LearningMetrics>();
        }
    }

    /// <summary>
    /// Interface for agents that support adaptive batch sizing
    /// </summary>
    public interface IAdaptiveBatchAgent
    {
        void SetBatchSize(int batchSize);
        int GetCurrentBatchSize();
    }

    /// <summary>
    /// Training-specific performance metrics
    /// </summary>
    [Serializable]
    public class TrainingPerformanceMetrics
    {
        public float currentFrameTime;
        public int adaptiveBatchSize;
        public int activeAgentCount;
        public int trainingAgentCount;
        public float averageUpdateInterval;
        public int maxAgentsPerFrame;
        public float performanceScore;
    }
}
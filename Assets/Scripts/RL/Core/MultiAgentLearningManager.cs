using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Manages independent learning for multiple monster types
    /// Implements Requirements 1.3, 2.5 - Independent learning per monster type
    /// Ensures learning isolation and progress tracking per type
    /// </summary>
    public class MultiAgentLearningManager : MonoBehaviour
    {
        [Header("Learning Settings")]
        [SerializeField] private bool enableIndependentLearning = true;
        [SerializeField] private float learningIsolationFactor = 0.9f; // How much to isolate learning between types
        [SerializeField] private int maxAgentsPerType = 10;
        [SerializeField] private float learningUpdateInterval = 0.1f;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxLearningUpdatesPerFrame = 5;
        [SerializeField] private bool adaptiveLearningRate = true;
        [SerializeField] private float baseLearningRate = 0.001f;
        
        // Learning management per monster type
        private Dictionary<MonsterType, TypeLearningManager> typeLearningManagers;
        private Dictionary<MonsterType, LearningMetrics> typeMetrics;
        private Dictionary<MonsterType, float> typeLearningRates;
        
        // Cross-type learning coordination
        private Dictionary<MonsterType, Dictionary<MonsterType, float>> crossTypeInfluence;
        private float lastLearningUpdate;
        
        // Events
        public event Action<MonsterType, LearningMetrics> OnTypeLearningUpdated;
        public event Action<MonsterType, float> OnLearningRateAdjusted;

        public bool IsLearningActive => enableIndependentLearning;
        public Dictionary<MonsterType, int> AgentCountsByType => 
            typeLearningManagers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AgentCount) ?? 
            new Dictionary<MonsterType, int>();

        void Awake()
        {
            InitializeLearningManager();
        }

        private void InitializeLearningManager()
        {
            typeLearningManagers = new Dictionary<MonsterType, TypeLearningManager>();
            typeMetrics = new Dictionary<MonsterType, LearningMetrics>();
            typeLearningRates = new Dictionary<MonsterType, float>();
            crossTypeInfluence = new Dictionary<MonsterType, Dictionary<MonsterType, float>>();
            
            // Initialize learning managers for each monster type
            foreach (MonsterType monsterType in Enum.GetValues(typeof(MonsterType)))
            {
                if (monsterType == MonsterType.None) continue;
                
                InitializeTypeManager(monsterType);
            }
            
            lastLearningUpdate = Time.time;
            
            Debug.Log("Multi-Agent Learning Manager initialized");
        }

        private void InitializeTypeManager(MonsterType monsterType)
        {
            // Create type-specific learning manager
            var managerGO = new GameObject($"TypeLearningManager_{monsterType}");
            managerGO.transform.SetParent(transform);
            
            var typeManager = managerGO.AddComponent<TypeLearningManager>();
            typeManager.Initialize(monsterType, maxAgentsPerType, learningIsolationFactor);
            
            typeLearningManagers[monsterType] = typeManager;
            typeMetrics[monsterType] = LearningMetrics.CreateDefault();
            typeLearningRates[monsterType] = baseLearningRate;
            
            // Initialize cross-type influence matrix
            crossTypeInfluence[monsterType] = new Dictionary<MonsterType, float>();
            foreach (MonsterType otherType in Enum.GetValues(typeof(MonsterType)))
            {
                if (otherType != MonsterType.None)
                {
                    crossTypeInfluence[monsterType][otherType] = CalculateTypeInfluence(monsterType, otherType);
                }
            }
        }

        private float CalculateTypeInfluence(MonsterType sourceType, MonsterType targetType)
        {
            if (sourceType == targetType) return 1f; // Full self-influence
            
            // Define influence relationships between monster types
            var influences = new Dictionary<(MonsterType, MonsterType), float>
            {
                // Melee monsters can learn from each other's positioning
                { (MonsterType.Melee, MonsterType.Melee), 0.8f },
                { (MonsterType.Melee, MonsterType.Boss), 0.3f },
                
                // Ranged monsters share targeting strategies
                { (MonsterType.Ranged, MonsterType.Ranged), 0.9f },
                { (MonsterType.Ranged, MonsterType.Throwing), 0.6f },
                { (MonsterType.Ranged, MonsterType.Boomerang), 0.4f },
                
                // Throwing monsters have unique mechanics
                { (MonsterType.Throwing, MonsterType.Throwing), 0.9f },
                { (MonsterType.Throwing, MonsterType.Ranged), 0.5f },
                
                // Boomerang monsters are specialized
                { (MonsterType.Boomerang, MonsterType.Boomerang), 0.95f },
                { (MonsterType.Boomerang, MonsterType.Throwing), 0.3f },
                
                // Boss monsters are unique but can influence others
                { (MonsterType.Boss, MonsterType.Boss), 1f },
                { (MonsterType.Boss, MonsterType.Melee), 0.2f },
                { (MonsterType.Boss, MonsterType.Ranged), 0.2f }
            };
            
            return influences.ContainsKey((sourceType, targetType)) ? 
                influences[(sourceType, targetType)] * (1f - learningIsolationFactor) : 
                0.1f * (1f - learningIsolationFactor); // Minimal cross-influence for unrelated types
        }

        /// <summary>
        /// Register an agent for independent learning
        /// </summary>
        public void RegisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent == null || monsterType == MonsterType.None) return;
            
            if (typeLearningManagers.ContainsKey(monsterType))
            {
                typeLearningManagers[monsterType].RegisterAgent(agent);
                Debug.Log($"Registered {monsterType} agent for independent learning");
            }
        }

        /// <summary>
        /// Unregister an agent from learning
        /// </summary>
        public void UnregisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent == null || monsterType == MonsterType.None) return;
            
            if (typeLearningManagers.ContainsKey(monsterType))
            {
                typeLearningManagers[monsterType].UnregisterAgent(agent);
                Debug.Log($"Unregistered {monsterType} agent from independent learning");
            }
        }

        void Update()
        {
            if (!enableIndependentLearning) return;
            
            if (Time.time - lastLearningUpdate < learningUpdateInterval) return;
            
            lastLearningUpdate = Time.time;
            
            // Update learning for each monster type
            UpdateTypeLearning();
            
            // Apply cross-type learning influence
            ApplyCrossTypeLearning();
            
            // Adjust learning rates if adaptive learning is enabled
            if (adaptiveLearningRate)
            {
                AdjustLearningRates();
            }
        }

        private void UpdateTypeLearning()
        {
            int updatesThisFrame = 0;
            
            foreach (var kvp in typeLearningManagers)
            {
                if (updatesThisFrame >= maxLearningUpdatesPerFrame) break;
                
                var monsterType = kvp.Key;
                var typeManager = kvp.Value;
                
                // Update learning for this type
                var metrics = typeManager.UpdateLearning();
                if (metrics.episodeCount > 0)
                {
                    typeMetrics[monsterType] = metrics;
                    OnTypeLearningUpdated?.Invoke(monsterType, metrics);
                    updatesThisFrame++;
                }
            }
        }

        private void ApplyCrossTypeLearning()
        {
            if (learningIsolationFactor >= 1f) return; // No cross-type learning
            
            // Apply learning influence between types
            foreach (var sourceType in typeLearningManagers.Keys)
            {
                var sourceMetrics = typeMetrics[sourceType];
                
                foreach (var targetType in typeLearningManagers.Keys)
                {
                    if (sourceType == targetType) continue;
                    
                    var influence = crossTypeInfluence[sourceType][targetType];
                    if (influence > 0.1f)
                    {
                        ApplyLearningInfluence(sourceType, targetType, sourceMetrics, influence);
                    }
                }
            }
        }

        private void ApplyLearningInfluence(MonsterType sourceType, MonsterType targetType, 
            LearningMetrics sourceMetrics, float influence)
        {
            var targetManager = typeLearningManagers[targetType];
            
            // Apply influence based on source type's learning success
            if (sourceMetrics.averageReward > typeMetrics[targetType].averageReward)
            {
                // Source type is performing better, share some learning
                var learningBoost = sourceMetrics.averageReward * influence * 0.1f;
                targetManager.ApplyLearningBoost(learningBoost);
            }
        }

        private void AdjustLearningRates()
        {
            foreach (var kvp in typeMetrics)
            {
                var monsterType = kvp.Key;
                var metrics = kvp.Value;
                
                // Adjust learning rate based on performance
                float currentRate = typeLearningRates[monsterType];
                float targetRate = CalculateOptimalLearningRate(metrics);
                
                // Smooth adjustment
                float newRate = Mathf.Lerp(currentRate, targetRate, 0.1f);
                
                if (Mathf.Abs(newRate - currentRate) > 0.0001f)
                {
                    typeLearningRates[monsterType] = newRate;
                    typeLearningManagers[monsterType].SetLearningRate(newRate);
                    OnLearningRateAdjusted?.Invoke(monsterType, newRate);
                }
            }
        }

        private float CalculateOptimalLearningRate(LearningMetrics metrics)
        {
            // Adjust learning rate based on convergence and performance
            float baseRate = baseLearningRate;
            
            // Reduce learning rate if converging (stable performance)
            if (metrics.IsConverging())
            {
                baseRate *= 0.5f;
            }
            
            // Increase learning rate if performance is poor
            if (metrics.averageReward < 10f)
            {
                baseRate *= 1.5f;
            }
            
            // Reduce learning rate if loss is increasing (instability)
            if (metrics.lossValue > 1f)
            {
                baseRate *= 0.7f;
            }
            
            return Mathf.Clamp(baseRate, 0.0001f, 0.01f);
        }

        /// <summary>
        /// Get learning metrics for a specific monster type
        /// </summary>
        public LearningMetrics GetTypeMetrics(MonsterType monsterType)
        {
            return typeMetrics.ContainsKey(monsterType) ? 
                typeMetrics[monsterType] : LearningMetrics.CreateDefault();
        }

        /// <summary>
        /// Get learning metrics for all monster types
        /// </summary>
        public Dictionary<MonsterType, LearningMetrics> GetAllTypeMetrics()
        {
            return new Dictionary<MonsterType, LearningMetrics>(typeMetrics);
        }

        /// <summary>
        /// Get current learning rate for a monster type
        /// </summary>
        public float GetLearningRate(MonsterType monsterType)
        {
            return typeLearningRates.ContainsKey(monsterType) ? 
                typeLearningRates[monsterType] : baseLearningRate;
        }

        /// <summary>
        /// Set learning rate for a specific monster type
        /// </summary>
        public void SetLearningRate(MonsterType monsterType, float learningRate)
        {
            if (typeLearningManagers.ContainsKey(monsterType))
            {
                typeLearningRates[monsterType] = learningRate;
                typeLearningManagers[monsterType].SetLearningRate(learningRate);
                OnLearningRateAdjusted?.Invoke(monsterType, learningRate);
            }
        }

        /// <summary>
        /// Reset learning progress for a specific monster type
        /// </summary>
        public void ResetTypeProgress(MonsterType monsterType)
        {
            if (typeLearningManagers.ContainsKey(monsterType))
            {
                typeLearningManagers[monsterType].ResetProgress();
                typeMetrics[monsterType] = LearningMetrics.CreateDefault();
                typeLearningRates[monsterType] = baseLearningRate;
                
                Debug.Log($"Reset learning progress for {monsterType}");
            }
        }

        /// <summary>
        /// Reset learning progress for all monster types
        /// </summary>
        public void ResetAllProgress()
        {
            foreach (var monsterType in typeLearningManagers.Keys)
            {
                ResetTypeProgress(monsterType);
            }
            
            Debug.Log("Reset learning progress for all monster types");
        }

        /// <summary>
        /// Get agent count for a specific monster type
        /// </summary>
        public int GetAgentCount(MonsterType monsterType)
        {
            return typeLearningManagers.ContainsKey(monsterType) ? 
                typeLearningManagers[monsterType].AgentCount : 0;
        }

        /// <summary>
        /// Check if a monster type has reached learning capacity
        /// </summary>
        public bool IsAtCapacity(MonsterType monsterType)
        {
            return GetAgentCount(monsterType) >= maxAgentsPerType;
        }

        /// <summary>
        /// Set learning isolation factor (0 = full cross-learning, 1 = complete isolation)
        /// </summary>
        public void SetLearningIsolation(float isolationFactor)
        {
            learningIsolationFactor = Mathf.Clamp01(isolationFactor);
            
            // Update cross-type influence matrix
            foreach (var sourceType in crossTypeInfluence.Keys)
            {
                foreach (var targetType in crossTypeInfluence[sourceType].Keys.ToList())
                {
                    crossTypeInfluence[sourceType][targetType] = 
                        CalculateTypeInfluence(sourceType, targetType);
                }
            }
            
            Debug.Log($"Learning isolation factor set to {learningIsolationFactor}");
        }

        /// <summary>
        /// Update learning for all monster types
        /// </summary>
        public void UpdateLearning()
        {
            if (!enableIndependentLearning) return;
            
            UpdateTypeLearning();
            ApplyCrossTypeLearning();
            
            if (adaptiveLearningRate)
            {
                AdjustLearningRates();
            }
        }

        /// <summary>
        /// Get performance summary for all monster types
        /// </summary>
        public string GetPerformanceSummary()
        {
            var summary = "Multi-Agent Learning Performance:\n";
            
            foreach (var kvp in typeMetrics)
            {
                var type = kvp.Key;
                var metrics = kvp.Value;
                var agentCount = GetAgentCount(type);
                var learningRate = GetLearningRate(type);
                
                summary += $"{type}: {agentCount} agents, " +
                          $"Avg Reward: {metrics.averageReward:F2}, " +
                          $"Learning Rate: {learningRate:F4}, " +
                          $"Episodes: {metrics.episodeCount}\n";
            }
            
            return summary;
        }

        void OnDestroy()
        {
            // Clean up type learning managers
            foreach (var typeManager in typeLearningManagers.Values)
            {
                if (typeManager != null)
                {
                    Destroy(typeManager.gameObject);
                }
            }
        }
    }
}
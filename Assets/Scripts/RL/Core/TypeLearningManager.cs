using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Manages learning for a specific monster type
    /// Ensures independent learning progress and isolation
    /// </summary>
    public class TypeLearningManager : MonoBehaviour
    {
        [Header("Type Settings")]
        [SerializeField] private MonsterType monsterType;
        [SerializeField] private int maxAgents = 10;
        [SerializeField] private float isolationFactor = 0.9f;
        
        [Header("Learning Parameters")]
        [SerializeField] private float learningRate = 0.001f;
        [SerializeField] private float experienceShareRate = 0.1f;
        [SerializeField] private bool enableExperienceSharing = true;
        
        // Agent management
        private List<ILearningAgent> registeredAgents;
        private Dictionary<ILearningAgent, AgentLearningState> agentStates;
        
        // Learning metrics
        private LearningMetrics aggregatedMetrics;
        private float lastMetricsUpdate;
        private float metricsUpdateInterval = 1f;
        
        // Experience sharing
        private Queue<SharedExperience> sharedExperiencePool;
        private int maxSharedExperiences = 1000;
        
        public int AgentCount => registeredAgents?.Count ?? 0;
        public MonsterType Type => monsterType;
        public LearningMetrics CurrentMetrics => aggregatedMetrics;

        public void Initialize(MonsterType type, int maxAgentCount, float isolation)
        {
            monsterType = type;
            maxAgents = maxAgentCount;
            isolationFactor = isolation;
            
            registeredAgents = new List<ILearningAgent>();
            agentStates = new Dictionary<ILearningAgent, AgentLearningState>();
            sharedExperiencePool = new Queue<SharedExperience>();
            aggregatedMetrics = LearningMetrics.CreateDefault();
            lastMetricsUpdate = Time.time;
            
            Debug.Log($"Type Learning Manager initialized for {monsterType}");
        }

        /// <summary>
        /// Register an agent for type-specific learning
        /// </summary>
        public bool RegisterAgent(ILearningAgent agent)
        {
            if (agent == null || registeredAgents.Contains(agent)) return false;
            
            if (registeredAgents.Count >= maxAgents)
            {
                Debug.LogWarning($"Cannot register agent: {monsterType} type at capacity ({maxAgents})");
                return false;
            }
            
            registeredAgents.Add(agent);
            agentStates[agent] = new AgentLearningState
            {
                agent = agent,
                registrationTime = Time.time,
                totalExperiences = 0,
                sharedExperiences = 0,
                learningProgress = 0f,
                lastUpdateTime = Time.time
            };
            
            Debug.Log($"Registered agent for {monsterType} learning (Total: {registeredAgents.Count})");
            return true;
        }

        /// <summary>
        /// Unregister an agent from learning
        /// </summary>
        public void UnregisterAgent(ILearningAgent agent)
        {
            if (agent == null || !registeredAgents.Contains(agent)) return;
            
            registeredAgents.Remove(agent);
            agentStates.Remove(agent);
            
            Debug.Log($"Unregistered agent from {monsterType} learning (Remaining: {registeredAgents.Count})");
        }

        /// <summary>
        /// Update learning for all agents of this type
        /// </summary>
        public LearningMetrics UpdateLearning()
        {
            if (registeredAgents.Count == 0) return aggregatedMetrics;
            
            // Update individual agent learning
            UpdateAgentLearning();
            
            // Share experiences between agents if enabled
            if (enableExperienceSharing)
            {
                ShareExperiences();
            }
            
            // Update aggregated metrics
            if (Time.time - lastMetricsUpdate >= metricsUpdateInterval)
            {
                UpdateAggregatedMetrics();
                lastMetricsUpdate = Time.time;
            }
            
            return aggregatedMetrics;
        }

        private void UpdateAgentLearning()
        {
            foreach (var agent in registeredAgents.ToList()) // ToList to avoid modification during iteration
            {
                if (agent == null)
                {
                    registeredAgents.Remove(agent);
                    continue;
                }
                
                if (!agentStates.ContainsKey(agent)) continue;
                
                try
                {
                    // Update agent policy
                    if (agent.IsTraining)
                    {
                        agent.UpdatePolicy();
                    }
                    
                    // Update agent state
                    var state = agentStates[agent];
                    state.lastUpdateTime = Time.time;
                    state.learningProgress = CalculateLearningProgress(agent);
                    agentStates[agent] = state;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error updating {monsterType} agent learning: {ex.Message}");
                }
            }
        }

        private float CalculateLearningProgress(ILearningAgent agent)
        {
            try
            {
                var metrics = agent.GetMetrics();
                
                // Calculate progress based on multiple factors
                float rewardProgress = Mathf.Clamp01(metrics.averageReward / 100f);
                float episodeProgress = Mathf.Clamp01(metrics.episodeCount / 1000f);
                float lossProgress = metrics.lossValue > 0 ? Mathf.Clamp01(1f - metrics.lossValue) : 0f;
                
                return (rewardProgress + episodeProgress + lossProgress) / 3f;
            }
            catch
            {
                return 0f;
            }
        }

        private void ShareExperiences()
        {
            if (registeredAgents.Count < 2) return;
            
            // Collect experiences from high-performing agents
            CollectSharedExperiences();
            
            // Distribute experiences to learning agents
            DistributeSharedExperiences();
        }

        private void CollectSharedExperiences()
        {
            foreach (var agent in registeredAgents)
            {
                if (!agentStates.ContainsKey(agent)) continue;
                
                var state = agentStates[agent];
                var metrics = agent.GetMetrics();
                
                // Share experiences from agents with above-average performance
                if (metrics.averageReward > aggregatedMetrics.averageReward * 1.1f)
                {
                    var experience = CreateSharedExperience(agent, metrics);
                    if (experience != null)
                    {
                        AddSharedExperience(experience);
                        state.sharedExperiences++;
                        agentStates[agent] = state;
                    }
                }
            }
        }

        private SharedExperience CreateSharedExperience(ILearningAgent agent, LearningMetrics metrics)
        {
            // Create a simplified shared experience
            // In a full implementation, this would extract actual experience data
            return new SharedExperience
            {
                sourceAgentId = agent.GetHashCode().ToString(),
                monsterType = monsterType,
                reward = metrics.averageReward,
                success = metrics.averageReward > 0,
                timestamp = Time.time,
                experienceWeight = CalculateExperienceWeight(metrics)
            };
        }

        private float CalculateExperienceWeight(LearningMetrics metrics)
        {
            // Weight experiences based on performance and recency
            float performanceWeight = Mathf.Clamp01(metrics.averageReward / 100f);
            float episodeWeight = Mathf.Clamp01(metrics.episodeCount / 1000f);
            
            return (performanceWeight + episodeWeight) / 2f;
        }

        private void AddSharedExperience(SharedExperience experience)
        {
            sharedExperiencePool.Enqueue(experience);
            
            // Maintain pool size limit
            while (sharedExperiencePool.Count > maxSharedExperiences)
            {
                sharedExperiencePool.Dequeue();
            }
        }

        private void DistributeSharedExperiences()
        {
            if (sharedExperiencePool.Count == 0) return;
            
            var experiencesToShare = Mathf.Min(sharedExperiencePool.Count, 
                registeredAgents.Count * 2); // Limit distribution per frame
            
            for (int i = 0; i < experiencesToShare; i++)
            {
                if (sharedExperiencePool.Count == 0) break;
                
                var experience = sharedExperiencePool.Dequeue();
                var targetAgent = SelectTargetAgent(experience);
                
                if (targetAgent != null)
                {
                    ApplySharedExperience(targetAgent, experience);
                }
            }
        }

        private ILearningAgent SelectTargetAgent(SharedExperience experience)
        {
            // Select agents that could benefit from this experience
            var candidateAgents = registeredAgents.Where(agent =>
            {
                if (!agentStates.ContainsKey(agent)) return false;
                
                var metrics = agent.GetMetrics();
                return metrics.averageReward < experience.reward * 0.8f; // Agents performing below this experience level
            }).ToList();
            
            if (candidateAgents.Count == 0) return null;
            
            // Select randomly from candidates
            return candidateAgents[UnityEngine.Random.Range(0, candidateAgents.Count)];
        }

        private void ApplySharedExperience(ILearningAgent agent, SharedExperience experience)
        {
            // Apply shared experience as a learning boost
            // In a full implementation, this would inject the experience into the agent's replay buffer
            
            if (agentStates.ContainsKey(agent))
            {
                var state = agentStates[agent];
                state.totalExperiences++;
                
                // Apply a small learning boost based on the shared experience
                var learningBoost = experience.experienceWeight * experienceShareRate;
                ApplyLearningBoost(agent, learningBoost);
                
                agentStates[agent] = state;
            }
        }

        private void ApplyLearningBoost(ILearningAgent agent, float boost)
        {
            // This is a simplified learning boost
            // In a full implementation, this would modify the agent's learning parameters
            // For now, we just track it in the agent state
            
            if (agentStates.ContainsKey(agent))
            {
                var state = agentStates[agent];
                state.learningProgress = Mathf.Min(1f, state.learningProgress + boost);
                agentStates[agent] = state;
            }
        }

        private void UpdateAggregatedMetrics()
        {
            if (registeredAgents.Count == 0)
            {
                aggregatedMetrics = LearningMetrics.CreateDefault();
                return;
            }
            
            float totalReward = 0f;
            float totalLoss = 0f;
            int totalEpisodes = 0;
            float totalExploration = 0f;
            int validAgents = 0;
            
            foreach (var agent in registeredAgents)
            {
                try
                {
                    var metrics = agent.GetMetrics();
                    totalReward += metrics.averageReward;
                    totalLoss += metrics.lossValue;
                    totalEpisodes += metrics.episodeCount;
                    totalExploration += metrics.explorationRate;
                    validAgents++;
                }
                catch
                {
                    // Skip invalid agents
                }
            }
            
            if (validAgents > 0)
            {
                aggregatedMetrics = new LearningMetrics
                {
                    averageReward = totalReward / validAgents,
                    lossValue = totalLoss / validAgents,
                    episodeCount = totalEpisodes / validAgents, // Average episodes per agent
                    explorationRate = totalExploration / validAgents
                };
            }
        }

        /// <summary>
        /// Apply a learning boost to all agents of this type
        /// </summary>
        public void ApplyLearningBoost(float boost)
        {
            foreach (var agent in registeredAgents)
            {
                ApplyLearningBoost(agent, boost);
            }
        }

        /// <summary>
        /// Set learning rate for all agents of this type
        /// </summary>
        public void SetLearningRate(float rate)
        {
            learningRate = rate;
            
            // Apply to all agents (would need agent interface support)
            // For now, just store the rate
        }

        /// <summary>
        /// Reset learning progress for all agents of this type
        /// </summary>
        public void ResetProgress()
        {
            foreach (var agent in registeredAgents)
            {
                if (agentStates.ContainsKey(agent))
                {
                    var state = agentStates[agent];
                    state.totalExperiences = 0;
                    state.sharedExperiences = 0;
                    state.learningProgress = 0f;
                    agentStates[agent] = state;
                }
            }
            
            // Clear shared experience pool
            sharedExperiencePool.Clear();
            
            // Reset aggregated metrics
            aggregatedMetrics = LearningMetrics.CreateDefault();
            
            Debug.Log($"Reset learning progress for {monsterType} type");
        }

        /// <summary>
        /// Get learning state for a specific agent
        /// </summary>
        public AgentLearningState? GetAgentState(ILearningAgent agent)
        {
            return agentStates.ContainsKey(agent) ? agentStates[agent] : null;
        }

        /// <summary>
        /// Get performance summary for this monster type
        /// </summary>
        public string GetPerformanceSummary()
        {
            var summary = $"{monsterType} Learning Summary:\n";
            summary += $"Agents: {registeredAgents.Count}/{maxAgents}\n";
            summary += $"Avg Reward: {aggregatedMetrics.averageReward:F2}\n";
            summary += $"Avg Loss: {aggregatedMetrics.lossValue:F4}\n";
            summary += $"Shared Experiences: {sharedExperiencePool.Count}\n";
            
            if (agentStates.Count > 0)
            {
                var avgProgress = agentStates.Values.Average(s => s.learningProgress);
                summary += $"Avg Learning Progress: {avgProgress:F2}\n";
            }
            
            return summary;
        }
    }

    /// <summary>
    /// Learning state for an individual agent within a type
    /// </summary>
    [Serializable]
    public struct AgentLearningState
    {
        public ILearningAgent agent;
        public float registrationTime;
        public int totalExperiences;
        public int sharedExperiences;
        public float learningProgress; // 0-1 progress indicator
        public float lastUpdateTime;
        
        public float TimeRegistered => Time.time - registrationTime;
        public float TimeSinceUpdate => Time.time - lastUpdateTime;
        public float ExperienceShareRatio => totalExperiences > 0 ? 
            (float)sharedExperiences / totalExperiences : 0f;
    }

    /// <summary>
    /// Shared experience data for cross-agent learning
    /// </summary>
    [Serializable]
    public class SharedExperience
    {
        public string sourceAgentId;
        public MonsterType monsterType;
        public float reward;
        public bool success;
        public float timestamp;
        public float experienceWeight; // 0-1 weight for importance
        
        public float Age => Time.time - timestamp;
        public bool IsValid => Age < 60f; // Experiences valid for 60 seconds
    }
}
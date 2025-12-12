using UnityEngine;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Metrics for tracking learning progress
    /// </summary>
    [Serializable]
    public struct LearningMetrics
    {
        [Header("Training Progress")]
        public int episodeCount;
        public float averageReward;
        public float bestReward;
        public float recentAverageReward; // Average over last 100 episodes
        
        [Header("Performance Metrics")]
        public float averageEpisodeLength;
        public float playerDamageDealt;
        public float damageTaken;
        public float survivalRate;
        
        [Header("Learning Statistics")]
        public float explorationRate; // Current epsilon for epsilon-greedy
        public float learningRate;
        public int totalSteps;
        public float lossValue; // Neural network loss
        
        [Header("Behavioral Metrics")]
        public int coordinatedActions;
        public int successfulAttacks;
        public int retreatActions;
        public float averageDistanceToPlayer;

        /// <summary>
        /// Create default metrics
        /// </summary>
        public static LearningMetrics CreateDefault()
        {
            return new LearningMetrics
            {
                episodeCount = 0,
                averageReward = 0f,
                bestReward = float.MinValue,
                recentAverageReward = 0f,
                averageEpisodeLength = 0f,
                playerDamageDealt = 0f,
                damageTaken = 0f,
                survivalRate = 0f,
                explorationRate = 1f,
                learningRate = 0.001f,
                totalSteps = 0,
                lossValue = 0f,
                coordinatedActions = 0,
                successfulAttacks = 0,
                retreatActions = 0,
                averageDistanceToPlayer = 0f
            };
        }

        /// <summary>
        /// Update metrics after an episode
        /// </summary>
        public void UpdateAfterEpisode(float episodeReward, float episodeLength, ActionOutcome finalOutcome)
        {
            episodeCount++;
            
            // Update reward metrics
            float alpha = 0.01f; // Learning rate for running averages
            averageReward = averageReward * (1f - alpha) + episodeReward * alpha;
            if (episodeReward > bestReward)
                bestReward = episodeReward;
            
            // Update recent average (last 100 episodes)
            float recentAlpha = Mathf.Min(1f / Mathf.Min(episodeCount, 100), 1f);
            recentAverageReward = recentAverageReward * (1f - recentAlpha) + episodeReward * recentAlpha;
            
            // Update performance metrics
            averageEpisodeLength = averageEpisodeLength * (1f - alpha) + episodeLength * alpha;
            playerDamageDealt = playerDamageDealt * (1f - alpha) + finalOutcome.damageDealt * alpha;
            damageTaken = damageTaken * (1f - alpha) + finalOutcome.damageTaken * alpha;
            
            // Update survival rate (1 if survived, 0 if died)
            float survived = finalOutcome.damageTaken > 0 ? 0f : 1f;
            survivalRate = survivalRate * (1f - alpha) + survived * alpha;
        }

        /// <summary>
        /// Check if learning is converging
        /// </summary>
        public bool IsConverging()
        {
            return episodeCount > 100 && 
                   Mathf.Abs(averageReward - recentAverageReward) < 0.1f &&
                   explorationRate < 0.1f;
        }

        /// <summary>
        /// Get learning progress as percentage (0-100)
        /// </summary>
        public float GetProgressPercentage()
        {
            if (episodeCount < 10) return 0f;
            
            // Base progress on exploration decay and reward stability
            float explorationProgress = (1f - explorationRate) * 50f;
            float stabilityProgress = IsConverging() ? 50f : 0f;
            
            return Mathf.Clamp(explorationProgress + stabilityProgress, 0f, 100f);
        }
    }
}
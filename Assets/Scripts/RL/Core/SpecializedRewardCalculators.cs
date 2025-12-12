using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// Specialized reward calculators for different reward function types
    /// </summary>
    public static class SpecializedRewardCalculators
    {
        /// <summary>
        /// Sparse reward calculator - only provides rewards at episode termination
        /// </summary>
        public class SparseRewardCalculator : IRewardCalculator
        {
            private RewardConfig rewardConfig;
            private MonsterRLConfig monsterConfig;

            public SparseRewardCalculator(RewardConfig rewardConfig, MonsterRLConfig monsterConfig)
            {
                this.rewardConfig = rewardConfig;
                this.monsterConfig = monsterConfig;
            }

            public float CalculateReward(RLGameState previousState, MonsterAction action, RLGameState currentState, ActionOutcome actionOutcome)
            {
                // Only provide immediate reward for hitting player
                if (actionOutcome.hitPlayer)
                {
                    return rewardConfig.hitReward + (actionOutcome.damageDealt * rewardConfig.damageRewardMultiplier);
                }
                
                return 0f; // No intermediate rewards
            }

            public float CalculateTerminalReward(RLGameState finalState, float episodeLength, bool killedByPlayer)
            {
                float reward = 0f;
                
                if (killedByPlayer)
                {
                    reward += rewardConfig.deathPenalty;
                }
                
                if (finalState.playerHealth <= 0f)
                {
                    reward += rewardConfig.killPlayerReward;
                }
                
                return reward;
            }

            public float ShapeReward(float baseReward, RLGameState state)
            {
                return baseReward; // No shaping for sparse rewards
            }
        }

        /// <summary>
        /// Curiosity-driven reward calculator - provides intrinsic motivation
        /// </summary>
        public class CuriosityRewardCalculator : IRewardCalculator
        {
            private RewardConfig rewardConfig;
            private MonsterRLConfig monsterConfig;
            private Vector2 lastPlayerPosition;
            private float lastPlayerHealth;
            private bool isInitialized;

            public CuriosityRewardCalculator(RewardConfig rewardConfig, MonsterRLConfig monsterConfig)
            {
                this.rewardConfig = rewardConfig;
                this.monsterConfig = monsterConfig;
                this.isInitialized = false;
            }

            public float CalculateReward(RLGameState previousState, MonsterAction action, RLGameState currentState, ActionOutcome actionOutcome)
            {
                float extrinsicReward = CalculateExtrinsicReward(actionOutcome);
                float intrinsicReward = CalculateIntrinsicReward(previousState, currentState);
                
                return extrinsicReward + intrinsicReward;
            }

            public float CalculateTerminalReward(RLGameState finalState, float episodeLength, bool killedByPlayer)
            {
                float reward = 0f;
                
                if (killedByPlayer)
                {
                    reward += rewardConfig.deathPenalty;
                }
                
                if (finalState.playerHealth <= 0f)
                {
                    reward += rewardConfig.killPlayerReward;
                }
                
                // Curiosity bonus for exploration
                reward += episodeLength * 0.1f; // Small bonus for longer episodes
                
                return reward;
            }

            public float ShapeReward(float baseReward, RLGameState state)
            {
                return baseReward; // Curiosity provides its own shaping
            }

            private float CalculateExtrinsicReward(ActionOutcome actionOutcome)
            {
                float reward = 0f;
                
                if (actionOutcome.hitPlayer)
                {
                    reward += rewardConfig.hitReward;
                    reward += actionOutcome.damageDealt * rewardConfig.damageRewardMultiplier;
                }
                
                return reward;
            }

            private float CalculateIntrinsicReward(RLGameState previousState, RLGameState currentState)
            {
                if (!isInitialized)
                {
                    lastPlayerPosition = currentState.playerPosition;
                    lastPlayerHealth = currentState.playerHealth;
                    isInitialized = true;
                    return 0f;
                }

                float intrinsicReward = 0f;

                // Reward for causing player state changes (curiosity about effect on environment)
                float playerMovement = Vector2.Distance(lastPlayerPosition, currentState.playerPosition);
                float playerHealthChange = Mathf.Abs(lastPlayerHealth - currentState.playerHealth);
                
                intrinsicReward += playerMovement * 0.1f; // Small reward for causing player to move
                intrinsicReward += playerHealthChange * 0.5f; // Reward for affecting player health

                // Update tracking variables
                lastPlayerPosition = currentState.playerPosition;
                lastPlayerHealth = currentState.playerHealth;

                return intrinsicReward;
            }
        }

        /// <summary>
        /// Adaptive reward calculator - adjusts rewards based on performance
        /// </summary>
        public class AdaptiveRewardCalculator : IRewardCalculator
        {
            private RewardConfig rewardConfig;
            private MonsterRLConfig monsterConfig;
            private float performanceHistory;
            private int episodeCount;
            private const float adaptationRate = 0.1f;

            public AdaptiveRewardCalculator(RewardConfig rewardConfig, MonsterRLConfig monsterConfig)
            {
                this.rewardConfig = rewardConfig;
                this.monsterConfig = monsterConfig;
                this.performanceHistory = 0f;
                this.episodeCount = 0;
            }

            public float CalculateReward(RLGameState previousState, MonsterAction action, RLGameState currentState, ActionOutcome actionOutcome)
            {
                float baseReward = CalculateBaseReward(actionOutcome);
                
                // Adapt reward based on recent performance
                float adaptationMultiplier = CalculateAdaptationMultiplier();
                
                return baseReward * adaptationMultiplier;
            }

            public float CalculateTerminalReward(RLGameState finalState, float episodeLength, bool killedByPlayer)
            {
                float reward = 0f;
                
                if (killedByPlayer)
                {
                    reward += rewardConfig.deathPenalty;
                    UpdatePerformance(-1f); // Poor performance
                }
                else
                {
                    UpdatePerformance(0.5f); // Neutral performance
                }
                
                if (finalState.playerHealth <= 0f)
                {
                    reward += rewardConfig.killPlayerReward;
                    UpdatePerformance(1f); // Excellent performance
                }
                
                episodeCount++;
                return reward;
            }

            public float ShapeReward(float baseReward, RLGameState state)
            {
                // Standard shaping with adaptation
                float shapedReward = baseReward;
                
                float distance = state.DistanceToPlayer;
                if (distance <= rewardConfig.optimalDistance)
                {
                    shapedReward += rewardConfig.optimalDistanceReward;
                }
                
                return shapedReward;
            }

            private float CalculateBaseReward(ActionOutcome actionOutcome)
            {
                float reward = 0f;
                
                if (actionOutcome.hitPlayer)
                {
                    reward += rewardConfig.hitReward;
                    reward += actionOutcome.damageDealt * rewardConfig.damageRewardMultiplier;
                }
                
                if (actionOutcome.coordinated)
                {
                    reward += rewardConfig.coordinationReward;
                }
                
                return reward;
            }

            private float CalculateAdaptationMultiplier()
            {
                if (episodeCount < 10) return 1f; // No adaptation for first few episodes
                
                // Increase rewards if performance is poor, decrease if too good
                float avgPerformance = performanceHistory / episodeCount;
                
                if (avgPerformance < -0.5f) // Poor performance
                {
                    return 1.5f; // Increase rewards to encourage learning
                }
                else if (avgPerformance > 0.5f) // Good performance
                {
                    return 0.8f; // Decrease rewards to maintain challenge
                }
                
                return 1f; // Normal rewards
            }

            private void UpdatePerformance(float performance)
            {
                performanceHistory = Mathf.Lerp(performanceHistory, performance, adaptationRate);
            }
        }

        /// <summary>
        /// Create specialized reward calculator based on type
        /// </summary>
        public static IRewardCalculator CreateSpecializedCalculator(RewardFunctionType type, RewardConfig rewardConfig, MonsterRLConfig monsterConfig)
        {
            switch (type)
            {
                case RewardFunctionType.Sparse:
                    return new SparseRewardCalculator(rewardConfig, monsterConfig);
                case RewardFunctionType.Curiosity:
                    return new CuriosityRewardCalculator(rewardConfig, monsterConfig);
                case RewardFunctionType.Dense:
                case RewardFunctionType.Shaped:
                default:
                    return new RewardCalculator(rewardConfig, monsterConfig);
            }
        }

        /// <summary>
        /// Create adaptive reward calculator
        /// </summary>
        public static AdaptiveRewardCalculator CreateAdaptiveCalculator(RewardConfig rewardConfig, MonsterRLConfig monsterConfig)
        {
            return new AdaptiveRewardCalculator(rewardConfig, monsterConfig);
        }
    }
}
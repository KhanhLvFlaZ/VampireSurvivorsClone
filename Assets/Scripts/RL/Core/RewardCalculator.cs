using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// Main implementation of reward calculation for monster RL system
    /// Supports configurable reward functions through ScriptableObjects
    /// </summary>
    public class RewardCalculator : IRewardCalculator
    {
        private RewardConfig rewardConfig;
        private MonsterRLConfig monsterConfig;

        /// <summary>
        /// Initialize the reward calculator with configuration
        /// </summary>
        public RewardCalculator(RewardConfig rewardConfig, MonsterRLConfig monsterConfig)
        {
            this.rewardConfig = rewardConfig ?? throw new System.ArgumentNullException(nameof(rewardConfig));
            this.monsterConfig = monsterConfig ?? throw new System.ArgumentNullException(nameof(monsterConfig));
        }

        /// <summary>
        /// Calculate reward for a monster action
        /// </summary>
        public float CalculateReward(RLGameState previousState, MonsterAction action, RLGameState currentState, ActionOutcome actionOutcome)
        {
            float totalReward = 0f;

            // Immediate action rewards
            totalReward += CalculateActionReward(action, actionOutcome);
            
            // State-based rewards
            totalReward += CalculateStateReward(previousState, currentState);
            
            // Coordination rewards
            if (actionOutcome.coordinated)
            {
                totalReward += rewardConfig.coordinationReward;
            }

            // Apply reward shaping
            totalReward = ShapeReward(totalReward, currentState);

            return totalReward;
        }

        /// <summary>
        /// Calculate terminal reward when monster dies or episode ends
        /// </summary>
        public float CalculateTerminalReward(RLGameState finalState, float episodeLength, bool killedByPlayer)
        {
            float terminalReward = 0f;

            if (killedByPlayer)
            {
                // Death penalty
                terminalReward += rewardConfig.deathPenalty;
            }
            else
            {
                // Survival bonus based on episode length
                terminalReward += CalculateSurvivalBonus(episodeLength);
            }

            // Bonus for contributing to player death (if player health is 0)
            if (finalState.playerHealth <= 0f)
            {
                terminalReward += rewardConfig.killPlayerReward;
            }

            return terminalReward;
        }

        /// <summary>
        /// Apply reward shaping for better learning convergence
        /// </summary>
        public float ShapeReward(float baseReward, RLGameState state)
        {
            if (rewardConfig.rewardFunctionType != RewardFunctionType.Shaped)
            {
                return baseReward;
            }

            float shapedReward = baseReward;

            // Distance-based shaping
            shapedReward += CalculateDistanceShaping(state);
            
            // Health-based shaping
            shapedReward += CalculateHealthShaping(state);
            
            // Time-based shaping
            shapedReward += CalculateTimeShaping(state);

            return shapedReward;
        }

        /// <summary>
        /// Calculate reward based on the specific action taken
        /// </summary>
        private float CalculateActionReward(MonsterAction action, ActionOutcome actionOutcome)
        {
            float reward = 0f;

            // Damage rewards
            if (actionOutcome.hitPlayer)
            {
                reward += rewardConfig.hitReward;
                reward += actionOutcome.damageDealt * rewardConfig.damageRewardMultiplier;
            }

            // Damage penalties
            if (actionOutcome.tookDamage)
            {
                reward -= actionOutcome.damageTaken * rewardConfig.damagePenaltyMultiplier;
            }

            // Action-specific rewards
            switch (action.actionType)
            {
                case ActionType.Attack:
                    reward += rewardConfig.attackAttemptReward;
                    break;
                case ActionType.SpecialAttack:
                    reward += rewardConfig.specialAttackReward;
                    break;
                case ActionType.Retreat:
                    if (actionOutcome.tookDamage)
                        reward += rewardConfig.tacticalRetreatReward; // Reward smart retreats
                    break;
                case ActionType.Coordinate:
                    reward += rewardConfig.coordinationAttemptReward;
                    break;
                case ActionType.Ambush:
                    if (actionOutcome.hitPlayer)
                        reward += rewardConfig.ambushSuccessReward;
                    break;
            }

            return reward;
        }

        /// <summary>
        /// Calculate reward based on state changes
        /// </summary>
        private float CalculateStateReward(RLGameState previousState, RLGameState currentState)
        {
            float reward = 0f;

            // Survival reward per frame
            reward += rewardConfig.survivalReward * Time.fixedDeltaTime;

            // Position improvement reward
            float previousDistance = previousState.DistanceToPlayer;
            float currentDistance = currentState.DistanceToPlayer;
            
            if (currentDistance < previousDistance && currentDistance <= rewardConfig.optimalDistance)
            {
                reward += rewardConfig.positionImprovementReward;
            }

            return reward;
        }

        /// <summary>
        /// Calculate survival bonus based on episode length
        /// </summary>
        private float CalculateSurvivalBonus(float episodeLength)
        {
            // Logarithmic survival bonus to prevent infinite rewards
            return rewardConfig.survivalBonusMultiplier * Mathf.Log(1f + episodeLength);
        }

        /// <summary>
        /// Calculate distance-based reward shaping
        /// </summary>
        private float CalculateDistanceShaping(RLGameState state)
        {
            float distance = state.DistanceToPlayer;
            float optimalDistance = rewardConfig.optimalDistance;

            // Reward being at optimal distance, penalize being too far or too close
            if (distance <= optimalDistance)
            {
                return rewardConfig.optimalDistanceReward * (1f - distance / optimalDistance);
            }
            else
            {
                float penalty = (distance - optimalDistance) / optimalDistance;
                return -rewardConfig.distancePenalty * Mathf.Min(penalty, 1f);
            }
        }

        /// <summary>
        /// Calculate health-based reward shaping
        /// </summary>
        private float CalculateHealthShaping(RLGameState state)
        {
            float reward = 0f;

            // Reward maintaining health
            float healthRatio = state.monsterHealth / 100f; // Assuming max health is 100
            reward += rewardConfig.healthMaintenanceReward * healthRatio;

            // Reward when player health is low
            float playerHealthRatio = state.playerHealth / 100f;
            if (playerHealthRatio < 0.3f) // Player below 30% health
            {
                reward += rewardConfig.playerLowHealthBonus;
            }

            return reward;
        }

        /// <summary>
        /// Calculate time-based reward shaping
        /// </summary>
        private float CalculateTimeShaping(RLGameState state)
        {
            float reward = 0f;

            // Reward recent damage to player
            if (state.timeSincePlayerDamage < rewardConfig.recentDamageWindow)
            {
                float recency = 1f - (state.timeSincePlayerDamage / rewardConfig.recentDamageWindow);
                reward += rewardConfig.recentDamageBonus * recency;
            }

            return reward;
        }
    }
}
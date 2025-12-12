using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// ScriptableObject configuration for reward functions
    /// Allows designers to configure reward parameters per monster type or globally
    /// </summary>
    [CreateAssetMenu(fileName = "RewardConfig", menuName = "Vampire/RL/Reward Config")]
    public class RewardConfig : ScriptableObject
    {
        [Header("Basic Rewards")]
        [Tooltip("Reward for successfully hitting the player")]
        [Range(1f, 100f)]
        public float hitReward = 25f;
        
        [Tooltip("Multiplier for damage dealt to player")]
        [Range(0.1f, 10f)]
        public float damageRewardMultiplier = 1f;
        
        [Tooltip("Reward for surviving per second")]
        [Range(0.1f, 10f)]
        public float survivalReward = 1f;
        
        [Tooltip("Reward for coordinating with other monsters")]
        [Range(1f, 50f)]
        public float coordinationReward = 15f;

        [Header("Terminal Rewards")]
        [Tooltip("Penalty for dying")]
        [Range(-500f, -10f)]
        public float deathPenalty = -100f;
        
        [Tooltip("Reward for contributing to player death")]
        [Range(50f, 1000f)]
        public float killPlayerReward = 200f;
        
        [Tooltip("Multiplier for survival bonus based on episode length")]
        [Range(1f, 20f)]
        public float survivalBonusMultiplier = 5f;

        [Header("Action-Specific Rewards")]
        [Tooltip("Small reward for attempting attacks (encourages aggression)")]
        [Range(0f, 5f)]
        public float attackAttemptReward = 1f;
        
        [Tooltip("Reward for using special attacks")]
        [Range(1f, 20f)]
        public float specialAttackReward = 5f;
        
        [Tooltip("Reward for tactical retreats when taking damage")]
        [Range(1f, 15f)]
        public float tacticalRetreatReward = 3f;
        
        [Tooltip("Reward for attempting coordination")]
        [Range(0f, 10f)]
        public float coordinationAttemptReward = 2f;
        
        [Tooltip("Bonus reward for successful ambush attacks")]
        [Range(5f, 30f)]
        public float ambushSuccessReward = 10f;

        [Header("Penalties")]
        [Tooltip("Multiplier for damage taken penalty")]
        [Range(0.1f, 5f)]
        public float damagePenaltyMultiplier = 0.5f;

        [Header("Reward Shaping Parameters")]
        [Tooltip("Type of reward function to use")]
        public RewardFunctionType rewardFunctionType = RewardFunctionType.Dense;
        
        [Tooltip("Optimal distance from player for positioning")]
        [Range(1f, 10f)]
        public float optimalDistance = 3f;
        
        [Tooltip("Reward for being at optimal distance")]
        [Range(0.1f, 5f)]
        public float optimalDistanceReward = 1f;
        
        [Tooltip("Penalty for being too far from optimal distance")]
        [Range(0.1f, 5f)]
        public float distancePenalty = 0.5f;
        
        [Tooltip("Reward for maintaining health")]
        [Range(0.1f, 5f)]
        public float healthMaintenanceReward = 0.5f;
        
        [Tooltip("Bonus when player health is low")]
        [Range(1f, 20f)]
        public float playerLowHealthBonus = 5f;
        
        [Tooltip("Bonus for recent damage to player")]
        [Range(1f, 15f)]
        public float recentDamageBonus = 3f;
        
        [Tooltip("Time window for recent damage bonus (seconds)")]
        [Range(1f, 10f)]
        public float recentDamageWindow = 3f;

        [Header("Position Improvement")]
        [Tooltip("Reward for improving position relative to player")]
        [Range(0.1f, 5f)]
        public float positionImprovementReward = 1f;

        /// <summary>
        /// Validate the reward configuration
        /// </summary>
        public bool IsValid()
        {
            return hitReward > 0 && 
                   damageRewardMultiplier > 0 && 
                   survivalReward > 0 && 
                   deathPenalty < 0 && 
                   killPlayerReward > 0 &&
                   optimalDistance > 0;
        }

        /// <summary>
        /// Create default reward configuration
        /// </summary>
        public static RewardConfig CreateDefault()
        {
            var config = CreateInstance<RewardConfig>();
            config.name = "Default Reward Config";
            
            // Set default values (already set in field declarations)
            return config;
        }

        /// <summary>
        /// Create aggressive reward configuration (encourages attacking)
        /// </summary>
        public static RewardConfig CreateAggressive()
        {
            var config = CreateDefault();
            config.name = "Aggressive Reward Config";
            
            config.hitReward = 40f;
            config.damageRewardMultiplier = 2f;
            config.attackAttemptReward = 3f;
            config.specialAttackReward = 10f;
            config.optimalDistance = 2f; // Closer to player
            config.rewardFunctionType = RewardFunctionType.Dense;
            
            return config;
        }

        /// <summary>
        /// Create defensive reward configuration (encourages survival)
        /// </summary>
        public static RewardConfig CreateDefensive()
        {
            var config = CreateDefault();
            config.name = "Defensive Reward Config";
            
            config.survivalReward = 3f;
            config.tacticalRetreatReward = 8f;
            config.healthMaintenanceReward = 2f;
            config.optimalDistance = 5f; // Further from player
            config.damagePenaltyMultiplier = 2f; // Higher penalty for taking damage
            config.rewardFunctionType = RewardFunctionType.Shaped;
            
            return config;
        }

        /// <summary>
        /// Create coordination-focused reward configuration
        /// </summary>
        public static RewardConfig CreateCoordination()
        {
            var config = CreateDefault();
            config.name = "Coordination Reward Config";
            
            config.coordinationReward = 30f;
            config.coordinationAttemptReward = 5f;
            config.ambushSuccessReward = 20f;
            config.rewardFunctionType = RewardFunctionType.Dense;
            
            return config;
        }

        /// <summary>
        /// Create sparse reward configuration (only terminal rewards)
        /// </summary>
        public static RewardConfig CreateSparse()
        {
            var config = CreateDefault();
            config.name = "Sparse Reward Config";
            
            config.rewardFunctionType = RewardFunctionType.Sparse;
            
            // Zero out intermediate rewards
            config.survivalReward = 0f;
            config.attackAttemptReward = 0f;
            config.coordinationAttemptReward = 0f;
            config.optimalDistanceReward = 0f;
            config.healthMaintenanceReward = 0f;
            config.positionImprovementReward = 0f;
            
            // Increase terminal rewards
            config.hitReward = 50f;
            config.killPlayerReward = 500f;
            config.deathPenalty = -200f;
            
            return config;
        }

        /// <summary>
        /// Get reward configuration based on monster type
        /// </summary>
        public static RewardConfig GetConfigForMonsterType(MonsterType monsterType)
        {
            switch (monsterType)
            {
                case MonsterType.Melee:
                    return CreateAggressive();
                case MonsterType.Ranged:
                    return CreateDefensive();
                case MonsterType.Throwing:
                    return CreateCoordination();
                case MonsterType.Boomerang:
                    return CreateDefault();
                case MonsterType.Boss:
                    var bossConfig = CreateAggressive();
                    bossConfig.killPlayerReward = 1000f;
                    bossConfig.hitReward = 60f;
                    bossConfig.specialAttackReward = 25f;
                    return bossConfig;
                default:
                    return CreateDefault();
            }
        }

        /// <summary>
        /// Apply difficulty scaling to rewards
        /// </summary>
        public void ApplyDifficultyScaling(float difficultyMultiplier)
        {
            // Scale positive rewards up with difficulty
            hitReward *= difficultyMultiplier;
            damageRewardMultiplier *= difficultyMultiplier;
            killPlayerReward *= difficultyMultiplier;
            specialAttackReward *= difficultyMultiplier;
            ambushSuccessReward *= difficultyMultiplier;
            
            // Scale penalties down with difficulty (make it easier)
            deathPenalty /= difficultyMultiplier;
            damagePenaltyMultiplier /= difficultyMultiplier;
        }
    }
}
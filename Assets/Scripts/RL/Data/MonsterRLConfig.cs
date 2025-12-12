using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// ScriptableObject configuration for monster RL behavior
    /// Allows designers to configure action spaces and reward functions per monster type
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterRLConfig", menuName = "Vampire/RL/Monster RL Config")]
    public class MonsterRLConfig : ScriptableObject
    {
        [Header("Monster Configuration")]
        public MonsterType monsterType = MonsterType.Melee;
        public string displayName = "Basic Monster";
        
        [Header("Action Space Configuration")]
        public ActionSpace actionSpace = new ActionSpace();
        
        [Header("Learning Parameters")]
        [Range(0.001f, 0.1f)]
        public float learningRate = 0.01f;
        
        [Range(0.1f, 1.0f)]
        public float discountFactor = 0.99f;
        
        [Range(0.01f, 1.0f)]
        public float explorationRate = 0.1f;
        
        [Range(0.001f, 0.1f)]
        public float explorationDecay = 0.995f;
        
        [Header("Reward Configuration")]
        public RewardFunctionType rewardFunctionType = RewardFunctionType.Dense;
        
        [Range(1f, 100f)]
        public float damageRewardMultiplier = 10f;
        
        [Range(1f, 50f)]
        public float hitReward = 25f;
        
        [Range(1f, 20f)]
        public float survivalReward = 5f;
        
        [Range(10f, 200f)]
        public float killPlayerReward = 200f;
        
        [Range(-200f, -50f)]
        public float deathPenalty = -100f;
        
        [Range(5f, 50f)]
        public float coordinationReward = 15f;
        
        [Header("Network Architecture")]
        public NetworkArchitecture networkArchitecture = NetworkArchitecture.Simple;
        
        [Range(16, 512)]
        public int hiddenLayerSize = 64;
        
        [Range(1, 4)]
        public int hiddenLayerCount = 2;
        
        [Header("Training Configuration")]
        [Range(100, 10000)]
        public int experienceBufferSize = 1000;
        
        [Range(8, 128)]
        public int batchSize = 32;
        
        [Range(1, 100)]
        public int updateFrequency = 10;
        
        [Range(100, 10000)]
        public int targetNetworkUpdateFrequency = 1000;

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public bool IsValid()
        {
            return monsterType != MonsterType.None && 
                   actionSpace != null && 
                   learningRate > 0 && 
                   discountFactor > 0 && 
                   hiddenLayerSize > 0;
        }

        /// <summary>
        /// Create default configuration for a monster type
        /// </summary>
        public static MonsterRLConfig CreateDefault(MonsterType monsterType)
        {
            var config = CreateInstance<MonsterRLConfig>();
            config.monsterType = monsterType;
            config.displayName = monsterType.ToString();
            
            switch (monsterType)
            {
                case MonsterType.Melee:
                    config.actionSpace = ActionSpace.CreateDefault();
                    config.displayName = "Melee Monster";
                    break;
                    
                case MonsterType.Ranged:
                    config.actionSpace = CreateRangedActionSpace();
                    config.displayName = "Ranged Monster";
                    config.hitReward = 30f; // Higher reward for ranged hits
                    break;
                    
                case MonsterType.Throwing:
                    config.actionSpace = CreateThrowingActionSpace();
                    config.displayName = "Throwing Monster";
                    config.coordinationReward = 20f; // Better at coordination
                    break;
                    
                case MonsterType.Boomerang:
                    config.actionSpace = CreateBoomerangActionSpace();
                    config.displayName = "Boomerang Monster";
                    config.explorationRate = 0.15f; // More exploration for complex behavior
                    break;
                    
                case MonsterType.Boss:
                    config.actionSpace = ActionSpace.CreateAdvanced();
                    config.displayName = "Boss Monster";
                    config.hiddenLayerSize = 128; // Larger network for boss
                    config.hiddenLayerCount = 3;
                    config.killPlayerReward = 500f; // Much higher reward
                    break;
            }
            
            return config;
        }

        /// <summary>
        /// Create action space for ranged monsters
        /// </summary>
        private static ActionSpace CreateRangedActionSpace()
        {
            return new ActionSpace
            {
                canMove = true,
                movementDirections = 8,
                canAttack = true,
                canSpecialAttack = true, // Ranged special attacks
                canDefend = false,
                canRetreat = true,
                canCoordinate = false,
                canAmbush = true, // Good at ambush tactics
                canWait = true,
                minActionInterval = 0.2f, // Slower attack rate
                maxActionRange = 8f // Longer range
            };
        }

        /// <summary>
        /// Create action space for throwing monsters
        /// </summary>
        private static ActionSpace CreateThrowingActionSpace()
        {
            return new ActionSpace
            {
                canMove = true,
                movementDirections = 8,
                canAttack = true,
                canSpecialAttack = false,
                canDefend = false,
                canRetreat = true,
                canCoordinate = true, // Good at coordination
                canAmbush = false,
                canWait = true,
                minActionInterval = 0.15f,
                maxActionRange = 6f
            };
        }

        /// <summary>
        /// Create action space for boomerang monsters
        /// </summary>
        private static ActionSpace CreateBoomerangActionSpace()
        {
            return new ActionSpace
            {
                canMove = true,
                movementDirections = 8,
                canAttack = true,
                canSpecialAttack = true, // Boomerang special mechanics
                canDefend = true, // Can use boomerang defensively
                canRetreat = true,
                canCoordinate = false,
                canAmbush = true,
                canWait = true,
                minActionInterval = 0.3f, // Slower due to boomerang return time
                maxActionRange = 7f
            };
        }
    }
}
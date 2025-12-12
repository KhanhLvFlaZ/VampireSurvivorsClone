using UnityEngine.Pool;
using UnityEngine;
using Vampire.RL;

namespace Vampire
{
    /// <summary>
    /// Specialized monster pool for RL monsters that handles proper state reset
    /// Ensures RL components are properly initialized and reset when pooled
    /// Implements Requirements 6.1, 6.4
    /// </summary>
    public class RLMonsterPool : MonsterPool
    {
        [Header("RL Pool Settings")]
        [SerializeField] private bool enableRLByDefault = true;
        [SerializeField] private TrainingMode defaultTrainingMode = TrainingMode.Training;
        
        // RL System integration
        private RLSystem rlSystem;
        private TrainingCoordinator trainingCoordinator;
        
        public override void Init(EntityManager entityManager, Character playerCharacter, GameObject prefab, bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)
        {
            // Initialize base properties without calling base.Init() to avoid double pool creation
            this.entityManager = entityManager;
            this.playerCharacter = playerCharacter;
            this.prefab = prefab;
            this.collectionCheck = collectionCheck;
            this.defaultCapacity = defaultCapacity;
            this.maxSize = maxSize;
            
            // Create ObjectPool with RL-specific callbacks
            pool = new ObjectPool<Monster>(CreateRLPooledItem, OnRLMonsterTakeFromPool, OnRLMonsterReturnedToPool, OnDestroyPooledItem, collectionCheck, defaultCapacity, maxSize);
            
            // Find RL system components
            rlSystem = FindObjectOfType<RLSystem>();
            if (rlSystem != null)
            {
                trainingCoordinator = rlSystem.GetComponent<TrainingCoordinator>();
            }
            
            Debug.Log($"RLMonsterPool initialized for prefab: {prefab.name}");
        }

        protected Monster CreateRLPooledItem()
        {
            Monster monster = Instantiate(prefab, transform).GetComponent<Monster>();
            monster.Init(entityManager, playerCharacter);
            
            // Initialize RL components if this is an RL monster
            if (monster is RLMonster rlMonster)
            {
                InitializeRLMonster(rlMonster);
            }
            
            return monster;
        }

        protected void OnRLMonsterTakeFromPool(Monster monster)
        {
            // Activate the monster (base functionality)
            monster.gameObject.SetActive(true);
            
            // Reset RL state when taking from pool
            if (monster is RLMonster rlMonster)
            {
                ResetRLMonsterState(rlMonster);
            }
        }

        protected void OnRLMonsterReturnedToPool(Monster monster)
        {
            // Clean up RL state before returning to pool
            if (monster is RLMonster rlMonster)
            {
                CleanupRLMonsterState(rlMonster);
            }
            
            // Call base functionality
            monster.gameObject.SetActive(false);
        }

        /// <summary>
        /// Initialize RL components for a newly created RL monster
        /// </summary>
        private void InitializeRLMonster(RLMonster rlMonster)
        {
            try
            {
                // Set default RL configuration
                rlMonster.SetRLEnabled(enableRLByDefault);
                rlMonster.SetTrainingMode(defaultTrainingMode == TrainingMode.Training);
                
                // Register with RL system if available
                if (rlSystem != null && rlSystem.IsEnabled)
                {
                    var learningAgent = rlMonster.GetComponent<ILearningAgent>();
                    if (learningAgent != null)
                    {
                        // The monster type should be determined from the monster's configuration
                        MonsterType monsterType = DetermineMonsterType(rlMonster);
                        rlSystem.RegisterAgent(learningAgent, monsterType);
                    }
                }
                
                Debug.Log($"RL Monster initialized: {rlMonster.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize RL monster: {ex.Message}");
                // Fallback: disable RL if initialization fails
                rlMonster.SetRLEnabled(false);
            }
        }

        /// <summary>
        /// Reset RL monster state when taking from pool
        /// Ensures clean state for reuse while preserving learned behavior
        /// </summary>
        private void ResetRLMonsterState(RLMonster rlMonster)
        {
            try
            {
                // Reset episode state but preserve learned weights
                rlMonster.ForceStartNewEpisode();
                
                // Re-register with training coordinator if needed
                if (trainingCoordinator != null)
                {
                    var learningAgent = rlMonster.GetComponent<ILearningAgent>();
                    if (learningAgent != null)
                    {
                        MonsterType monsterType = DetermineMonsterType(rlMonster);
                        trainingCoordinator.RegisterAgent(learningAgent, monsterType);
                    }
                }
                
                // Ensure RL is enabled based on current system state
                if (rlSystem != null)
                {
                    rlMonster.SetRLEnabled(rlSystem.IsEnabled);
                    rlMonster.SetTrainingMode(rlSystem.CurrentTrainingMode == TrainingMode.Training);
                }
                
                Debug.Log($"RL Monster state reset: {rlMonster.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to reset RL monster state: {ex.Message}");
                // Fallback: disable RL if reset fails
                rlMonster.SetRLEnabled(false);
            }
        }

        /// <summary>
        /// Clean up RL monster state before returning to pool
        /// Ensures proper cleanup of learning components
        /// </summary>
        private void CleanupRLMonsterState(RLMonster rlMonster)
        {
            try
            {
                // Unregister from training coordinator
                if (trainingCoordinator != null)
                {
                    var learningAgent = rlMonster.GetComponent<ILearningAgent>();
                    if (learningAgent != null)
                    {
                        trainingCoordinator.UnregisterAgent(learningAgent);
                    }
                }
                
                // Unregister from RL system
                if (rlSystem != null)
                {
                    var learningAgent = rlMonster.GetComponent<ILearningAgent>();
                    if (learningAgent != null)
                    {
                        rlSystem.UnregisterAgent(learningAgent);
                    }
                }
                
                Debug.Log($"RL Monster state cleaned up: {rlMonster.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to cleanup RL monster state: {ex.Message}");
            }
        }

        /// <summary>
        /// Determine monster type from RL monster configuration
        /// </summary>
        private MonsterType DetermineMonsterType(RLMonster rlMonster)
        {
            // Try to get monster type from the RL monster's configuration
            // This is a simplified approach - in a real implementation, this would
            // be determined from the monster's blueprint or configuration
            
            // Check the monster's name or prefab to determine type
            string monsterName = rlMonster.name.ToLower();
            
            if (monsterName.Contains("melee") || monsterName.Contains("warrior"))
                return MonsterType.Melee;
            else if (monsterName.Contains("ranged") || monsterName.Contains("archer"))
                return MonsterType.Ranged;
            else if (monsterName.Contains("boss"))
                return MonsterType.Boss;
            else if (monsterName.Contains("throwing"))
                return MonsterType.Throwing;
            else if (monsterName.Contains("boomerang"))
                return MonsterType.Boomerang;
            
            // Default to Melee if type cannot be determined
            return MonsterType.Melee;
        }

        /// <summary>
        /// Get RL-specific pool statistics
        /// </summary>
        public RLPoolStatistics GetRLStatistics()
        {
            var stats = new RLPoolStatistics();
            
            // Count active RL monsters
            var activeMonsters = GetComponentsInChildren<RLMonster>();
            foreach (var monster in activeMonsters)
            {
                if (monster.gameObject.activeInHierarchy)
                {
                    stats.activeRLMonsters++;
                    
                    if (monster.GetComponent<ILearningAgent>()?.IsTraining == true)
                    {
                        stats.trainingMonsters++;
                    }
                    else
                    {
                        stats.inferenceMonsters++;
                    }
                }
            }
            
            stats.totalPooledMonsters = activeMonsters.Length;
            
            return stats;
        }
    }

    /// <summary>
    /// Statistics for RL monster pool
    /// </summary>
    [System.Serializable]
    public class RLPoolStatistics
    {
        public int activeRLMonsters;
        public int trainingMonsters;
        public int inferenceMonsters;
        public int totalPooledMonsters;
        
        public override string ToString()
        {
            return $"RL Pool Stats - Active: {activeRLMonsters}, Training: {trainingMonsters}, " +
                   $"Inference: {inferenceMonsters}, Total: {totalPooledMonsters}";
        }
    }
}
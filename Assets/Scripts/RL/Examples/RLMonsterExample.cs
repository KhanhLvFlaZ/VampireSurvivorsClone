using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Examples
{
    /// <summary>
    /// Example script showing how to set up and use RLMonster components
    /// Demonstrates integration with existing Monster system
    /// </summary>
    public class RLMonsterExample : MonoBehaviour
    {
        [Header("Example Configuration")]
        [SerializeField] private GameObject baseMonsterPrefab;
        [SerializeField] private Character player;
        [SerializeField] private EntityManager entityManager;
        [SerializeField] private MonsterRLConfig rlConfig;
        
        [Header("Spawn Settings")]
        [SerializeField] private int numberOfMonsters = 5;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private bool enableTraining = true;
        
        [Header("Runtime Controls")]
        [SerializeField] private bool showDebugInfo = true;
        
        private RLMonster[] spawnedMonsters;

        void Start()
        {
            if (baseMonsterPrefab != null && player != null && entityManager != null)
            {
                SpawnRLMonsters();
            }
            else
            {
                Debug.LogWarning("RLMonsterExample: Missing required references. Please assign in inspector.");
            }
        }

        void Update()
        {
            if (showDebugInfo && spawnedMonsters != null)
            {
                DisplayDebugInfo();
            }
        }

        /// <summary>
        /// Spawn multiple RL monsters for demonstration
        /// </summary>
        [ContextMenu("Spawn RL Monsters")]
        public void SpawnRLMonsters()
        {
            spawnedMonsters = new RLMonster[numberOfMonsters];
            
            for (int i = 0; i < numberOfMonsters; i++)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();
                GameObject monsterObj = CreateRLMonster(spawnPosition, i);
                spawnedMonsters[i] = monsterObj.GetComponent<RLMonster>();
            }
            
            Debug.Log($"Spawned {numberOfMonsters} RL monsters");
        }

        /// <summary>
        /// Create a single RL monster at the specified position
        /// </summary>
        private GameObject CreateRLMonster(Vector2 position, int index)
        {
            // Instantiate base monster prefab
            GameObject monsterObj = Instantiate(baseMonsterPrefab, position, Quaternion.identity);
            monsterObj.name = $"RLMonster_{index}";
            
            // Add RL components if not already present
            RLMonster rlMonster = monsterObj.GetComponent<RLMonster>();
            if (rlMonster == null)
            {
                rlMonster = monsterObj.AddComponent<RLMonster>();
            }
            
            DQNLearningAgent learningAgent = monsterObj.GetComponent<DQNLearningAgent>();
            if (learningAgent == null)
            {
                learningAgent = monsterObj.AddComponent<DQNLearningAgent>();
            }
            
            // Configure RL settings
            rlMonster.SetRLEnabled(true);
            rlMonster.SetTrainingMode(enableTraining);
            
            // Initialize with game systems
            rlMonster.Init(entityManager, player);
            
            // Setup with monster blueprint (would normally come from spawn system)
            var blueprint = CreateExampleBlueprint();
            rlMonster.Setup(index, position, blueprint);
            
            return monsterObj;
        }

        /// <summary>
        /// Get a random spawn position around the player
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            Vector2 playerPos = player.transform.position;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(spawnRadius * 0.5f, spawnRadius);
            
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            return playerPos + offset;
        }

        /// <summary>
        /// Create an example monster blueprint for testing
        /// </summary>
        private MonsterBlueprint CreateExampleBlueprint()
        {
            var blueprint = ScriptableObject.CreateInstance<MonsterBlueprint>();
            
            // Set example values (in a real game, these would be loaded from assets)
            // Note: This is a simplified example - actual blueprints have many more fields
            
            return blueprint;
        }

        /// <summary>
        /// Display debug information about spawned monsters
        /// </summary>
        private void DisplayDebugInfo()
        {
            if (spawnedMonsters == null) return;
            
            int aliveCount = 0;
            float totalReward = 0f;
            int trainingCount = 0;
            
            foreach (var monster in spawnedMonsters)
            {
                if (monster != null && monster.gameObject.activeInHierarchy)
                {
                    aliveCount++;
                    var metrics = monster.GetLearningMetrics();
                    totalReward += metrics.averageReward;
                    
                    if (monster.GetLearningMetrics().explorationRate > 0.1f)
                        trainingCount++;
                }
            }
            
            // Display info in scene view (you could also use UI)
            string debugText = $"RL Monsters: {aliveCount}/{numberOfMonsters} alive, " +
                              $"Avg Reward: {(aliveCount > 0 ? totalReward / aliveCount : 0):F1}, " +
                              $"Training: {trainingCount}";
            
            Debug.Log(debugText);
        }

        /// <summary>
        /// Toggle training mode for all monsters
        /// </summary>
        [ContextMenu("Toggle Training Mode")]
        public void ToggleTrainingMode()
        {
            enableTraining = !enableTraining;
            
            if (spawnedMonsters != null)
            {
                foreach (var monster in spawnedMonsters)
                {
                    if (monster != null)
                    {
                        monster.SetTrainingMode(enableTraining);
                    }
                }
            }
            
            Debug.Log($"Training mode: {(enableTraining ? "Enabled" : "Disabled")}");
        }

        /// <summary>
        /// Save behavior profiles for all monsters
        /// </summary>
        [ContextMenu("Save Behavior Profiles")]
        public void SaveBehaviorProfiles()
        {
            if (spawnedMonsters == null) return;
            
            string saveDirectory = Application.persistentDataPath + "/RL_Profiles/";
            System.IO.Directory.CreateDirectory(saveDirectory);
            
            for (int i = 0; i < spawnedMonsters.Length; i++)
            {
                var monster = spawnedMonsters[i];
                if (monster != null)
                {
                    string filePath = saveDirectory + $"monster_{i}_profile.json";
                    monster.SaveBehaviorProfile(filePath);
                }
            }
            
            Debug.Log($"Saved behavior profiles to: {saveDirectory}");
        }

        /// <summary>
        /// Load behavior profiles for all monsters
        /// </summary>
        [ContextMenu("Load Behavior Profiles")]
        public void LoadBehaviorProfiles()
        {
            if (spawnedMonsters == null) return;
            
            string saveDirectory = Application.persistentDataPath + "/RL_Profiles/";
            
            for (int i = 0; i < spawnedMonsters.Length; i++)
            {
                var monster = spawnedMonsters[i];
                if (monster != null)
                {
                    string filePath = saveDirectory + $"monster_{i}_profile.json";
                    if (System.IO.File.Exists(filePath))
                    {
                        monster.LoadBehaviorProfile(filePath);
                    }
                }
            }
            
            Debug.Log("Loaded behavior profiles");
        }

        /// <summary>
        /// Clean up spawned monsters
        /// </summary>
        [ContextMenu("Cleanup Monsters")]
        public void CleanupMonsters()
        {
            if (spawnedMonsters != null)
            {
                foreach (var monster in spawnedMonsters)
                {
                    if (monster != null && monster.gameObject != null)
                    {
                        DestroyImmediate(monster.gameObject);
                    }
                }
                spawnedMonsters = null;
            }
            
            Debug.Log("Cleaned up RL monsters");
        }

        void OnDrawGizmosSelected()
        {
            if (player != null)
            {
                // Draw spawn radius using multiple wire spheres to simulate a circle
                Gizmos.color = Color.yellow;
                Vector3 playerPos = player.transform.position;
                
                // Draw a circle by drawing multiple points around the circumference
                int segments = 32;
                for (int i = 0; i < segments; i++)
                {
                    float angle1 = (float)i / segments * 2f * Mathf.PI;
                    float angle2 = (float)(i + 1) / segments * 2f * Mathf.PI;
                    
                    Vector3 point1 = playerPos + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * spawnRadius;
                    Vector3 point2 = playerPos + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * spawnRadius;
                    
                    Gizmos.DrawLine(point1, point2);
                }
                
                // Draw monster positions
                if (spawnedMonsters != null)
                {
                    Gizmos.color = Color.red;
                    foreach (var monster in spawnedMonsters)
                    {
                        if (monster != null)
                        {
                            Gizmos.DrawWireSphere(monster.transform.position, 0.5f);
                        }
                    }
                }
            }
        }
    }
}
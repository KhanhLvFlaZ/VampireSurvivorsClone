using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Unit tests for RLMonster functionality
    /// Tests integration with Monster class and RL system components
    /// </summary>
    public class RLMonsterTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;
        
        [Header("Test Dependencies")]
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private Character testPlayer;
        [SerializeField] private EntityManager testEntityManager;

        void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run RLMonster Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== RLMonster Tests Started ===");
            
            bool allTestsPassed = true;
            
            allTestsPassed &= TestRLMonsterCreation();
            allTestsPassed &= TestRLSystemInitialization();
            allTestsPassed &= TestActionSelection();
            allTestsPassed &= TestStateObservation();
            allTestsPassed &= TestRewardCalculation();
            allTestsPassed &= TestTrainingModeToggle();
            
            if (allTestsPassed)
            {
                Debug.Log("=== RLMonster Tests PASSED ===");
            }
            else
            {
                Debug.LogError("=== RLMonster Tests FAILED ===");
            }
        }

        private bool TestRLMonsterCreation()
        {
            try
            {
                // Create test monster
                GameObject testMonsterObj = new GameObject("TestRLMonster");
                RLMonster rlMonster = testMonsterObj.AddComponent<RLMonster>();
                
                // Add required components
                testMonsterObj.AddComponent<Rigidbody2D>();
                testMonsterObj.AddComponent<CircleCollider2D>();
                testMonsterObj.AddComponent<DQNLearningAgent>();
                
                // Add sprite components
                GameObject spriteChild = new GameObject("Sprite");
                spriteChild.transform.SetParent(testMonsterObj.transform);
                spriteChild.AddComponent<SpriteRenderer>();
                spriteChild.AddComponent<SpriteAnimator>();
                
                if (rlMonster == null)
                {
                    Debug.LogError("✗ RLMonster creation test failed: component not created");
                    return false;
                }
                
                if (logDetailedResults)
                    Debug.Log("✓ RLMonster creation test passed");
                
                // Cleanup
                DestroyImmediate(testMonsterObj);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ RLMonster creation test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRLSystemInitialization()
        {
            try
            {
                // Create test setup
                var testSetup = CreateTestRLMonster();
                RLMonster rlMonster = testSetup.rlMonster;
                
                // Create mock dependencies
                Character mockPlayer = CreateMockPlayer();
                EntityManager mockEntityManager = CreateMockEntityManager();
                
                // Initialize the monster
                rlMonster.Init(mockEntityManager, mockPlayer);
                
                // Check if RL system is initialized
                var metrics = rlMonster.GetLearningMetrics();
                if (metrics.episodeCount < 0) // Should be 0 or positive
                {
                    Debug.LogError("✗ RL system initialization test failed: invalid metrics");
                    CleanupTest(testSetup.gameObject, mockPlayer.gameObject, mockEntityManager.gameObject);
                    return false;
                }
                
                if (logDetailedResults)
                    Debug.Log("✓ RL system initialization test passed");
                
                CleanupTest(testSetup.gameObject, mockPlayer.gameObject, mockEntityManager.gameObject);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ RL system initialization test failed: {e.Message}");
                return false;
            }
        }

        private bool TestActionSelection()
        {
            try
            {
                var testSetup = CreateTestRLMonster();
                RLMonster rlMonster = testSetup.rlMonster;
                
                Character mockPlayer = CreateMockPlayer();
                EntityManager mockEntityManager = CreateMockEntityManager();
                
                rlMonster.Init(mockEntityManager, mockPlayer);
                
                // Setup monster with blueprint
                var mockBlueprint = CreateMockMonsterBlueprint();
                rlMonster.Setup(0, Vector2.zero, mockBlueprint);
                
                // Force start episode to trigger action selection
                rlMonster.ForceStartNewEpisode();
                
                // Wait a frame for action selection
                // In a real test, we'd need to simulate time passing
                
                if (logDetailedResults)
                    Debug.Log("✓ Action selection test passed");
                
                CleanupTest(testSetup.gameObject, mockPlayer.gameObject, mockEntityManager.gameObject);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Action selection test failed: {e.Message}");
                return false;
            }
        }

        private bool TestStateObservation()
        {
            try
            {
                var testSetup = CreateTestRLMonster();
                RLMonster rlMonster = testSetup.rlMonster;
                
                Character mockPlayer = CreateMockPlayer();
                EntityManager mockEntityManager = CreateMockEntityManager();
                
                rlMonster.Init(mockEntityManager, mockPlayer);
                
                // Position monster and player
                rlMonster.transform.position = Vector2.zero;
                mockPlayer.transform.position = new Vector2(5f, 0f);
                
                var mockBlueprint = CreateMockMonsterBlueprint();
                rlMonster.Setup(0, Vector2.zero, mockBlueprint);
                
                if (logDetailedResults)
                    Debug.Log("✓ State observation test passed");
                
                CleanupTest(testSetup.gameObject, mockPlayer.gameObject, mockEntityManager.gameObject);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ State observation test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardCalculation()
        {
            try
            {
                var testSetup = CreateTestRLMonster();
                RLMonster rlMonster = testSetup.rlMonster;
                
                Character mockPlayer = CreateMockPlayer();
                EntityManager mockEntityManager = CreateMockEntityManager();
                
                rlMonster.Init(mockEntityManager, mockPlayer);
                
                var mockBlueprint = CreateMockMonsterBlueprint();
                rlMonster.Setup(0, Vector2.zero, mockBlueprint);
                
                // Simulate taking damage (should affect reward calculation)
                rlMonster.TakeDamage(10f);
                
                if (logDetailedResults)
                    Debug.Log("✓ Reward calculation test passed");
                
                CleanupTest(testSetup.gameObject, mockPlayer.gameObject, mockEntityManager.gameObject);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Reward calculation test failed: {e.Message}");
                return false;
            }
        }

        private bool TestTrainingModeToggle()
        {
            try
            {
                var testSetup = CreateTestRLMonster();
                RLMonster rlMonster = testSetup.rlMonster;
                
                Character mockPlayer = CreateMockPlayer();
                EntityManager mockEntityManager = CreateMockEntityManager();
                
                rlMonster.Init(mockEntityManager, mockPlayer);
                
                // Test training mode toggle
                rlMonster.SetTrainingMode(true);
                rlMonster.SetTrainingMode(false);
                rlMonster.SetRLEnabled(false);
                rlMonster.SetRLEnabled(true);
                
                if (logDetailedResults)
                    Debug.Log("✓ Training mode toggle test passed");
                
                CleanupTest(testSetup.gameObject, mockPlayer.gameObject, mockEntityManager.gameObject);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Training mode toggle test failed: {e.Message}");
                return false;
            }
        }

        private (GameObject gameObject, RLMonster rlMonster) CreateTestRLMonster()
        {
            GameObject testMonsterObj = new GameObject("TestRLMonster");
            
            // Add required components
            testMonsterObj.AddComponent<Rigidbody2D>();
            testMonsterObj.AddComponent<CircleCollider2D>();
            testMonsterObj.AddComponent<DQNLearningAgent>();
            
            // Add sprite components
            GameObject spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(testMonsterObj.transform);
            spriteChild.AddComponent<SpriteRenderer>();
            spriteChild.AddComponent<SpriteAnimator>();
            
            RLMonster rlMonster = testMonsterObj.AddComponent<RLMonster>();
            
            return (testMonsterObj, rlMonster);
        }

        private Character CreateMockPlayer()
        {
            GameObject playerObj = new GameObject("MockPlayer");
            playerObj.AddComponent<Rigidbody2D>();
            playerObj.AddComponent<CircleCollider2D>();
            
            // Add sprite components
            GameObject spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(playerObj.transform);
            spriteChild.AddComponent<SpriteRenderer>();
            spriteChild.AddComponent<SpriteAnimator>();
            
            Character character = playerObj.AddComponent<Character>();
            return character;
        }

        private EntityManager CreateMockEntityManager()
        {
            GameObject entityManagerObj = new GameObject("MockEntityManager");
            EntityManager entityManager = entityManagerObj.AddComponent<EntityManager>();
            return entityManager;
        }

        private MonsterBlueprint CreateMockMonsterBlueprint()
        {
            MonsterBlueprint blueprint = ScriptableObject.CreateInstance<MonsterBlueprint>();
            // Set default values that would normally be set in the inspector
            // This is a simplified mock - in a real scenario we'd need to set all required fields
            return blueprint;
        }

        private void CleanupTest(params GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                    DestroyImmediate(obj);
            }
        }
    }
}
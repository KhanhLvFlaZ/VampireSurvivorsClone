using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Simple integration test for the RL system foundation
    /// Verifies that all core components can be instantiated and work together
    /// </summary>
    public class RLSystemIntegrationTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        void Start()
        {
            if (runTestOnStart)
            {
                RunIntegrationTest();
            }
        }

        [ContextMenu("Run RL System Integration Test")]
        public void RunIntegrationTest()
        {
            Debug.Log("=== RL System Integration Test Started ===");
            
            bool allTestsPassed = true;
            
            // Test 1: Core Data Structures
            allTestsPassed &= TestDataStructures();
            
            // Test 2: Neural Network
            allTestsPassed &= TestNeuralNetwork();
            
            // Test 3: Learning Agent
            allTestsPassed &= TestLearningAgent();
            
            // Test 4: Action Space
            allTestsPassed &= TestActionSpace();
            
            // Test 5: State Encoder
            allTestsPassed &= TestStateEncoder();
            
            // Test 6: Behavior Profile
            allTestsPassed &= TestBehaviorProfile();

            // Final result
            if (allTestsPassed)
            {
                Debug.Log("=== RL System Integration Test PASSED ===");
            }
            else
            {
                Debug.LogError("=== RL System Integration Test FAILED ===");
            }
        }

        private bool TestDataStructures()
        {
            try
            {
                // Test RLGameState
                var gameState = RLGameState.CreateDefault();
                if (logDetailedResults)
                    Debug.Log($"✓ RLGameState created: Player at {gameState.playerPosition}, Monster at {gameState.monsterPosition}");

                // Test MonsterAction
                var moveAction = MonsterAction.CreateMovement(Vector2.up);
                var attackAction = MonsterAction.CreateAttack(0.8f);
                if (logDetailedResults)
                    Debug.Log($"✓ MonsterActions created: Move={moveAction.actionType}, Attack={attackAction.actionType}");

                // Test ActionOutcome
                var outcome = ActionOutcome.CreateDefault();
                if (logDetailedResults)
                    Debug.Log($"✓ ActionOutcome created: Hit={outcome.hitPlayer}, Damage={outcome.damageDealt}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Data Structures test failed: {e.Message}");
                return false;
            }
        }

        private bool TestNeuralNetwork()
        {
            try
            {
                var network = new SimpleNeuralNetwork();
                network.Initialize(32, 15, new int[] { 64, 32 }, NetworkArchitecture.Simple);
                
                // Test forward pass
                float[] input = new float[32];
                for (int i = 0; i < input.Length; i++)
                    input[i] = Random.Range(-1f, 1f);
                
                float[] output = network.Forward(input);
                
                if (output.Length != 15)
                {
                    Debug.LogError($"✗ Neural Network output size mismatch: expected 15, got {output.Length}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Neural Network test passed: Input[32] -> Output[{output.Length}]");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Neural Network test failed: {e.Message}");
                return false;
            }
        }

        private bool TestLearningAgent()
        {
            try
            {
                var agentGO = new GameObject("TestAgent");
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                var actionSpace = ActionSpace.CreateDefault();
                
                agent.Initialize(MonsterType.Melee, actionSpace);
                
                // Test action selection
                var gameState = RLGameState.CreateDefault();
                int action = agent.SelectAction(gameState, true);
                
                if (action < 0 || action >= actionSpace.GetTotalActionCount())
                {
                    Debug.LogError($"✗ Learning Agent returned invalid action: {action}");
                    DestroyImmediate(agentGO);
                    return false;
                }

                // Test experience storage
                agent.StoreExperience(gameState, action, 1.0f, gameState, false);
                
                // Test metrics
                var metrics = agent.GetMetrics();
                
                if (logDetailedResults)
                    Debug.Log($"✓ Learning Agent test passed: Action={action}, Exploration={metrics.explorationRate:F3}");

                DestroyImmediate(agentGO);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Learning Agent test failed: {e.Message}");
                return false;
            }
        }

        private bool TestActionSpace()
        {
            try
            {
                var defaultSpace = ActionSpace.CreateDefault();
                var advancedSpace = ActionSpace.CreateAdvanced();
                
                int defaultActions = defaultSpace.GetTotalActionCount();
                int advancedActions = advancedSpace.GetTotalActionCount();
                
                if (defaultActions <= 0 || advancedActions <= 0)
                {
                    Debug.LogError($"✗ Action Space returned invalid action counts: Default={defaultActions}, Advanced={advancedActions}");
                    return false;
                }

                if (advancedActions <= defaultActions)
                {
                    Debug.LogError($"✗ Advanced action space should have more actions than default: Advanced={advancedActions}, Default={defaultActions}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Action Space test passed: Default={defaultActions} actions, Advanced={advancedActions} actions");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Action Space test failed: {e.Message}");
                return false;
            }
        }

        private bool TestStateEncoder()
        {
            try
            {
                var stateEncoder = new StateEncoder();
                
                // Test state size
                int stateSize = stateEncoder.GetStateSize();
                if (stateSize <= 0)
                {
                    Debug.LogError($"✗ State Encoder returned invalid state size: {stateSize}");
                    return false;
                }

                // Test encoding with default state
                var gameState = RLGameState.CreateDefault();
                float[] encodedState = stateEncoder.EncodeState(gameState);
                
                if (encodedState == null || encodedState.Length != stateSize)
                {
                    Debug.LogError($"✗ State Encoder returned invalid encoded state: expected {stateSize}, got {encodedState?.Length ?? 0}");
                    return false;
                }

                // Test normalization - all values should be between -1 and 1
                foreach (float value in encodedState)
                {
                    if (value < -1f || value > 1f)
                    {
                        Debug.LogError($"✗ State Encoder normalization failed: value {value} outside [-1, 1] range");
                        return false;
                    }
                }

                // Test with populated state
                gameState.playerPosition = new Vector2(10f, -5f);
                gameState.playerVelocity = new Vector2(3f, 2f);
                gameState.playerHealth = 75f;
                gameState.monsterPosition = new Vector2(-8f, 12f);
                gameState.monsterHealth = 50f;
                gameState.timeAlive = 30f;

                float[] encodedState2 = stateEncoder.EncodeState(gameState);
                
                if (encodedState2 == null || encodedState2.Length != stateSize)
                {
                    Debug.LogError($"✗ State Encoder failed with populated state");
                    return false;
                }

                // Verify that different states produce different encodings
                bool statesAreDifferent = false;
                for (int i = 0; i < encodedState.Length; i++)
                {
                    if (Mathf.Abs(encodedState[i] - encodedState2[i]) > 0.001f)
                    {
                        statesAreDifferent = true;
                        break;
                    }
                }

                if (!statesAreDifferent)
                {
                    Debug.LogError("✗ State Encoder produced identical encodings for different states");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ State Encoder test passed: StateSize={stateSize}, Normalization=OK, Differentiation=OK");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ State Encoder test failed: {e.Message}");
                return false;
            }
        }

        private bool TestBehaviorProfile()
        {
            try
            {
                var profile = BehaviorProfile.Create(MonsterType.Melee, "test_player", NetworkArchitecture.Simple);
                
                if (!profile.IsValid())
                {
                    Debug.LogError("✗ Behavior Profile validation failed");
                    return false;
                }

                // Test weight compression
                profile.networkWeights = new float[] { 1.0f, -0.5f, 0.3f, -0.8f, 0.1f };
                profile.CompressWeights();
                
                if (profile.compressedWeights == null || profile.compressedWeights.Length == 0)
                {
                    Debug.LogError("✗ Behavior Profile compression failed");
                    return false;
                }

                // Test decompression
                profile.DecompressWeights();
                
                if (profile.networkWeights == null || profile.networkWeights.Length != 5)
                {
                    Debug.LogError("✗ Behavior Profile decompression failed");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Behavior Profile test passed: ID={profile.profileId}, Type={profile.monsterType}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Behavior Profile test failed: {e.Message}");
                return false;
            }
        }
    }
}
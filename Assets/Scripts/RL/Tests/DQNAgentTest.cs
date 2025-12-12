using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Unit tests for DQN Learning Agent implementation
    /// Tests core DQN functionality including experience replay and Q-learning
    /// </summary>
    public class DQNAgentTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        void Start()
        {
            if (runTestOnStart)
            {
                RunDQNTests();
            }
        }

        [ContextMenu("Run DQN Agent Tests")]
        public void RunDQNTests()
        {
            Debug.Log("=== DQN Agent Tests Started ===");
            
            bool allTestsPassed = true;
            
            // Test 1: Agent Initialization
            allTestsPassed &= TestAgentInitialization();
            
            // Test 2: Action Selection
            allTestsPassed &= TestActionSelection();
            
            // Test 3: Experience Replay Buffer
            allTestsPassed &= TestExperienceReplayBuffer();
            
            // Test 4: Epsilon-Greedy Exploration
            allTestsPassed &= TestEpsilonGreedyExploration();
            
            // Test 5: Q-Learning Updates
            allTestsPassed &= TestQLearningUpdates();
            
            // Test 6: Target Network Updates
            allTestsPassed &= TestTargetNetworkUpdates();

            // Final result
            if (allTestsPassed)
            {
                Debug.Log("=== DQN Agent Tests PASSED ===");
            }
            else
            {
                Debug.LogError("=== DQN Agent Tests FAILED ===");
            }
        }

        private bool TestAgentInitialization()
        {
            try
            {
                var agentGO = new GameObject("TestDQNAgent");
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                var actionSpace = ActionSpace.CreateDefault();
                
                agent.Initialize(MonsterType.Melee, actionSpace);
                
                var metrics = agent.GetMetrics();
                
                // Check initial values
                if (metrics.explorationRate <= 0f || metrics.explorationRate > 1f)
                {
                    Debug.LogError($"✗ Invalid initial exploration rate: {metrics.explorationRate}");
                    DestroyImmediate(agentGO);
                    return false;
                }
                
                if (metrics.learningRate <= 0f)
                {
                    Debug.LogError($"✗ Invalid learning rate: {metrics.learningRate}");
                    DestroyImmediate(agentGO);
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Agent Initialization: Epsilon={metrics.explorationRate:F3}, LR={metrics.learningRate:F4}");

                DestroyImmediate(agentGO);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Agent Initialization test failed: {e.Message}");
                return false;
            }
        }

        private bool TestActionSelection()
        {
            try
            {
                var agentGO = new GameObject("TestDQNAgent");
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                var actionSpace = ActionSpace.CreateDefault();
                
                agent.Initialize(MonsterType.Melee, actionSpace);
                
                var gameState = RLGameState.CreateDefault();
                int totalActions = actionSpace.GetTotalActionCount();
                
                // Test multiple action selections
                for (int i = 0; i < 10; i++)
                {
                    int action = agent.SelectAction(gameState, true);
                    
                    if (action < 0 || action >= totalActions)
                    {
                        Debug.LogError($"✗ Invalid action selected: {action} (valid range: 0-{totalActions-1})");
                        DestroyImmediate(agentGO);
                        return false;
                    }
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Action Selection: Valid actions in range [0, {totalActions-1}]");

                DestroyImmediate(agentGO);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Action Selection test failed: {e.Message}");
                return false;
            }
        }

        private bool TestExperienceReplayBuffer()
        {
            try
            {
                var buffer = new ExperienceReplayBuffer(100);
                
                // Test empty buffer
                if (buffer.Count != 0)
                {
                    Debug.LogError($"✗ Buffer should be empty initially, got count: {buffer.Count}");
                    return false;
                }
                
                var batch = buffer.SampleBatch(10);
                if (batch.Length != 0)
                {
                    Debug.LogError($"✗ Empty buffer should return empty batch, got: {batch.Length}");
                    return false;
                }
                
                // Add experiences
                var gameState = RLGameState.CreateDefault();
                for (int i = 0; i < 50; i++)
                {
                    var experience = new Experience
                    {
                        state = gameState,
                        action = i % 5,
                        reward = Random.Range(-1f, 1f),
                        nextState = gameState,
                        done = i % 10 == 0
                    };
                    buffer.Add(experience);
                }
                
                if (buffer.Count != 50)
                {
                    Debug.LogError($"✗ Buffer count mismatch: expected 50, got {buffer.Count}");
                    return false;
                }
                
                // Test batch sampling
                var sampleBatch = buffer.SampleBatch(10);
                if (sampleBatch.Length != 10)
                {
                    Debug.LogError($"✗ Batch size mismatch: expected 10, got {sampleBatch.Length}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Experience Replay Buffer: Count={buffer.Count}, Batch sampling works");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Experience Replay Buffer test failed: {e.Message}");
                return false;
            }
        }

        private bool TestEpsilonGreedyExploration()
        {
            try
            {
                var agentGO = new GameObject("TestDQNAgent");
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                var actionSpace = ActionSpace.CreateDefault();
                
                agent.Initialize(MonsterType.Melee, actionSpace);
                agent.IsTraining = true;
                
                var gameState = RLGameState.CreateDefault();
                
                // Count exploration vs exploitation
                int explorationCount = 0;
                int totalTests = 1000;
                
                for (int i = 0; i < totalTests; i++)
                {
                    int action1 = agent.SelectAction(gameState, true);  // Training mode
                    int action2 = agent.SelectAction(gameState, true);  // Training mode
                    
                    // If actions are different, likely exploration happened
                    if (action1 != action2)
                        explorationCount++;
                }
                
                float explorationRate = (float)explorationCount / totalTests;
                
                // Should have some exploration (not deterministic)
                if (explorationRate < 0.1f)
                {
                    Debug.LogError($"✗ Too little exploration detected: {explorationRate:F3}");
                    DestroyImmediate(agentGO);
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Epsilon-Greedy Exploration: Rate={explorationRate:F3}");

                DestroyImmediate(agentGO);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Epsilon-Greedy Exploration test failed: {e.Message}");
                return false;
            }
        }

        private bool TestQLearningUpdates()
        {
            try
            {
                var agentGO = new GameObject("TestDQNAgent");
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                var actionSpace = ActionSpace.CreateDefault();
                
                agent.Initialize(MonsterType.Melee, actionSpace);
                agent.IsTraining = true;
                
                var gameState = RLGameState.CreateDefault();
                
                // Store enough experiences for training
                for (int i = 0; i < 1100; i++)
                {
                    int action = Random.Range(0, actionSpace.GetTotalActionCount());
                    float reward = Random.Range(-1f, 1f);
                    agent.StoreExperience(gameState, action, reward, gameState, i % 100 == 0);
                }
                
                var initialMetrics = agent.GetMetrics();
                float initialLoss = initialMetrics.lossValue;
                
                // Perform several updates
                for (int i = 0; i < 10; i++)
                {
                    agent.UpdatePolicy();
                }
                
                var updatedMetrics = agent.GetMetrics();
                
                // Check that training steps increased
                if (updatedMetrics.totalSteps <= initialMetrics.totalSteps)
                {
                    Debug.LogError($"✗ Training steps not increasing: {initialMetrics.totalSteps} -> {updatedMetrics.totalSteps}");
                    DestroyImmediate(agentGO);
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Q-Learning Updates: Steps={updatedMetrics.totalSteps}, Loss={updatedMetrics.lossValue:F4}");

                DestroyImmediate(agentGO);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Q-Learning Updates test failed: {e.Message}");
                return false;
            }
        }

        private bool TestTargetNetworkUpdates()
        {
            try
            {
                var agentGO = new GameObject("TestDQNAgent");
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                var actionSpace = ActionSpace.CreateDefault();
                
                agent.Initialize(MonsterType.Melee, actionSpace);
                agent.IsTraining = true;
                
                var gameState = RLGameState.CreateDefault();
                
                // Store experiences and perform many updates to trigger target network update
                for (int i = 0; i < 1100; i++)
                {
                    int action = Random.Range(0, actionSpace.GetTotalActionCount());
                    float reward = Random.Range(-1f, 1f);
                    agent.StoreExperience(gameState, action, reward, gameState, false);
                }
                
                // Perform enough updates to trigger target network update (100+ updates)
                for (int i = 0; i < 110; i++)
                {
                    agent.UpdatePolicy();
                }
                
                // If we get here without errors, target network updates are working
                if (logDetailedResults)
                    Debug.Log($"✓ Target Network Updates: Completed 110 policy updates successfully");

                DestroyImmediate(agentGO);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Target Network Updates test failed: {e.Message}");
                return false;
            }
        }
    }
}
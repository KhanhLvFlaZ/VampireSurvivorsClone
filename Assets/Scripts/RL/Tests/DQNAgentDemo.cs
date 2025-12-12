using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Demo script showing DQN agent learning in action
    /// Simulates a simple learning scenario to demonstrate DQN functionality
    /// </summary>
    public class DQNAgentDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool runDemoOnStart = false;
        [SerializeField] private int episodesToRun = 100;
        [SerializeField] private int stepsPerEpisode = 50;
        [SerializeField] private bool logProgress = true;

        private DQNLearningAgent agent;
        private ActionSpace actionSpace;

        void Start()
        {
            if (runDemoOnStart)
            {
                StartDemo();
            }
        }

        [ContextMenu("Start DQN Learning Demo")]
        public void StartDemo()
        {
            Debug.Log("=== DQN Learning Demo Started ===");
            
            // Initialize agent
            SetupAgent();
            
            // Run learning episodes
            RunLearningEpisodes();
            
            Debug.Log("=== DQN Learning Demo Completed ===");
        }

        private void SetupAgent()
        {
            // Create agent
            var agentGO = new GameObject("DemoAgent");
            agent = agentGO.AddComponent<DQNLearningAgent>();
            actionSpace = ActionSpace.CreateDefault();
            
            agent.Initialize(MonsterType.Melee, actionSpace);
            agent.IsTraining = true;
            
            Debug.Log($"Agent initialized with {actionSpace.GetTotalActionCount()} actions");
        }

        private void RunLearningEpisodes()
        {
            for (int episode = 0; episode < episodesToRun; episode++)
            {
                RunSingleEpisode(episode);
                
                // Log progress every 10 episodes
                if (logProgress && episode % 10 == 0)
                {
                    var metrics = agent.GetMetrics();
                    Debug.Log($"Episode {episode}: {agent.GetLearningStatus()}");
                }
            }
        }

        private void RunSingleEpisode(int episodeNumber)
        {
            var gameState = CreateRandomGameState();
            float episodeReward = 0f;
            
            for (int step = 0; step < stepsPerEpisode; step++)
            {
                // Agent selects action
                int action = agent.SelectAction(gameState, true);
                
                // Simulate environment response
                var nextState = SimulateEnvironmentStep(gameState, action);
                float reward = CalculateReward(gameState, action, nextState);
                bool done = step == stepsPerEpisode - 1 || Random.Range(0f, 1f) < 0.1f;
                
                // Store experience
                agent.StoreExperience(gameState, action, reward, nextState, done);
                
                // Update policy
                agent.UpdatePolicy();
                
                episodeReward += reward;
                gameState = nextState;
                
                if (done) break;
            }
            
            // Update episode metrics
            var metrics = agent.GetMetrics();
            var outcome = ActionOutcome.CreateDefault();
            outcome.damageDealt = Mathf.Max(0f, episodeReward);
            outcome.damageTaken = Mathf.Max(0f, -episodeReward);
            
            // Manually update metrics (normally done by the game system)
            // This is a simplified version for demo purposes
        }

        private RLGameState CreateRandomGameState()
        {
            var state = RLGameState.CreateDefault();
            
            // Randomize positions
            state.playerPosition = new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
            state.monsterPosition = new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
            
            // Randomize other properties
            state.playerHealth = Random.Range(50f, 100f);
            state.monsterHealth = Random.Range(30f, 100f);
            state.playerVelocity = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            
            return state;
        }

        private RLGameState SimulateEnvironmentStep(RLGameState currentState, int action)
        {
            var nextState = currentState;
            
            // Simple simulation: monster moves based on action
            Vector2 movement = Vector2.zero;
            
            // Decode action into movement (simplified)
            int moveAction = action % 9; // 9 movement directions
            switch (moveAction)
            {
                case 0: movement = Vector2.up; break;
                case 1: movement = Vector2.down; break;
                case 2: movement = Vector2.left; break;
                case 3: movement = Vector2.right; break;
                case 4: movement = Vector2.up + Vector2.right; break;
                case 5: movement = Vector2.up + Vector2.left; break;
                case 6: movement = Vector2.down + Vector2.right; break;
                case 7: movement = Vector2.down + Vector2.left; break;
                case 8: movement = Vector2.zero; break; // Stay still
            }
            
            nextState.monsterPosition += movement.normalized * 2f;
            
            // Simulate player movement (random)
            nextState.playerPosition += new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            
            // Update time
            nextState.timeAlive += 1f;
            
            return nextState;
        }

        private float CalculateReward(RLGameState state, int action, RLGameState nextState)
        {
            float reward = 0f;
            
            // Reward for getting closer to player
            float currentDistance = Vector2.Distance(state.monsterPosition, state.playerPosition);
            float nextDistance = Vector2.Distance(nextState.monsterPosition, nextState.playerPosition);
            
            if (nextDistance < currentDistance)
                reward += 0.1f; // Small reward for getting closer
            else
                reward -= 0.05f; // Small penalty for moving away
            
            // Reward for being close to player
            if (nextDistance < 3f)
                reward += 0.5f; // Good reward for being close
            
            // Penalty for being too far
            if (nextDistance > 15f)
                reward -= 0.2f;
            
            // Random noise to make learning more interesting
            reward += Random.Range(-0.1f, 0.1f);
            
            return reward;
        }

        void OnDestroy()
        {
            // Clean up agent if demo is destroyed
            if (agent != null && agent.gameObject != null)
            {
                DestroyImmediate(agent.gameObject);
            }
        }
    }
}
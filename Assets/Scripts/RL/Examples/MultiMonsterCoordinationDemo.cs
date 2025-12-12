using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL.Examples
{
    /// <summary>
    /// Demo showcasing multi-monster learning and coordination capabilities
    /// Demonstrates Requirements 1.3, 2.5, 5.2
    /// </summary>
    public class MultiMonsterCoordinationDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private int meleeAgentCount = 5;
        [SerializeField] private int rangedAgentCount = 3;
        [SerializeField] private int throwingAgentCount = 2;
        [SerializeField] private float spawnRadius = 15f;
        [SerializeField] private bool enableCoordination = true;
        [SerializeField] private bool enableIndependentLearning = true;
        
        [Header("Demo Controls")]
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private KeyCode toggleCoordinationKey = KeyCode.C;
        [SerializeField] private KeyCode toggleLearningKey = KeyCode.L;
        [SerializeField] private KeyCode showStatsKey = KeyCode.S;
        
        // Core systems
        private MonsterCoordinationSystem coordinationSystem;
        private MultiAgentLearningManager multiAgentManager;
        private TrainingCoordinator trainingCoordinator;
        
        // Demo agents
        private List<DemoAgent> demoAgents;
        private Dictionary<MonsterType, List<DemoAgent>> agentsByType;
        
        // UI and visualization
        private bool showStats = false;
        private GUIStyle statsStyle;

        void Start()
        {
            InitializeDemo();
            CreateDemoAgents();
            StartCoordinationDemo();
        }

        private void InitializeDemo()
        {
            // Initialize coordination system
            var coordGO = new GameObject("CoordinationSystem");
            coordGO.transform.SetParent(transform);
            coordinationSystem = coordGO.AddComponent<MonsterCoordinationSystem>();
            
            // Initialize multi-agent learning manager
            var multiAgentGO = new GameObject("MultiAgentLearningManager");
            multiAgentGO.transform.SetParent(transform);
            multiAgentManager = multiAgentGO.AddComponent<MultiAgentLearningManager>();
            
            // Initialize training coordinator
            var trainingGO = new GameObject("TrainingCoordinator");
            trainingGO.transform.SetParent(transform);
            trainingCoordinator = trainingGO.AddComponent<TrainingCoordinator>();
            
            // Create mock dependencies
            var mockEntityManager = gameObject.AddComponent<MockEntityManager>();
            var mockPlayer = gameObject.AddComponent<MockPlayer>();
            
            trainingCoordinator.Initialize(mockPlayer);
            
            // Initialize collections
            demoAgents = new List<DemoAgent>();
            agentsByType = new Dictionary<MonsterType, List<DemoAgent>>();
            
            Debug.Log("Multi-Monster Coordination Demo initialized");
        }

        private void CreateDemoAgents()
        {
            // Create melee agents
            CreateAgentsOfType(MonsterType.Melee, meleeAgentCount, Color.red);
            
            // Create ranged agents
            CreateAgentsOfType(MonsterType.Ranged, rangedAgentCount, Color.blue);
            
            // Create throwing agents
            CreateAgentsOfType(MonsterType.Throwing, throwingAgentCount, Color.green);
            
            Debug.Log($"Created {demoAgents.Count} demo agents");
        }

        private void CreateAgentsOfType(MonsterType monsterType, int count, Color color)
        {
            if (!agentsByType.ContainsKey(monsterType))
            {
                agentsByType[monsterType] = new List<DemoAgent>();
            }
            
            for (int i = 0; i < count; i++)
            {
                // Random position within spawn radius
                Vector2 position = Random.insideUnitCircle * spawnRadius;
                
                var agentGO = new GameObject($"DemoAgent_{monsterType}_{i}");
                agentGO.transform.position = position;
                agentGO.transform.SetParent(transform);
                
                // Add visual representation
                var renderer = agentGO.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateCircleSprite();
                renderer.color = color;
                
                // Add demo agent component
                var demoAgent = agentGO.AddComponent<DemoAgent>();
                demoAgent.Initialize(monsterType, ActionSpace.CreateDefault());
                
                // Register with systems
                if (enableCoordination)
                {
                    coordinationSystem.RegisterAgent(demoAgent, monsterType);
                }
                
                if (enableIndependentLearning)
                {
                    multiAgentManager.RegisterAgent(demoAgent, monsterType);
                }
                
                trainingCoordinator.RegisterAgent(demoAgent, monsterType);
                
                demoAgents.Add(demoAgent);
                agentsByType[monsterType].Add(demoAgent);
            }
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple circle sprite for visualization
            var texture = new Texture2D(32, 32);
            var center = new Vector2(16, 16);
            
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    Color color = distance <= 15 ? Color.white : Color.clear;
                    texture.SetPixel(x, y, color);
                }
            }
            
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }

        private void StartCoordinationDemo()
        {
            // Set training mode
            trainingCoordinator.SetTrainingMode(TrainingMode.Training);
            
            // Start demo behaviors
            InvokeRepeating(nameof(UpdateDemoAgentBehaviors), 1f, 0.5f);
            InvokeRepeating(nameof(SimulateCoordinationEvents), 2f, 3f);
            
            Debug.Log("Coordination demo started");
        }

        private void UpdateDemoAgentBehaviors()
        {
            foreach (var agent in demoAgents)
            {
                if (agent != null)
                {
                    agent.UpdateDemoBehavior();
                }
            }
        }

        private void SimulateCoordinationEvents()
        {
            // Simulate successful coordination events
            foreach (var typeGroup in agentsByType)
            {
                var agents = typeGroup.Value;
                if (agents.Count >= 2)
                {
                    // Pick random agents for coordination
                    var agent1 = agents[Random.Range(0, agents.Count)];
                    var agent2 = agents[Random.Range(0, agents.Count)];
                    
                    if (agent1 != agent2)
                    {
                        bool success = Random.value > 0.3f; // 70% success rate
                        float reward = success ? Random.Range(10f, 25f) : Random.Range(-5f, 5f);
                        
                        coordinationSystem.RecordCoordinationSuccess(agent1, success, reward);
                        coordinationSystem.RecordCoordinationSuccess(agent2, success, reward);
                    }
                }
            }
        }

        void Update()
        {
            HandleInput();
            
            // Update demo visualization
            UpdateAgentVisualization();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(resetKey))
            {
                ResetDemo();
            }
            
            if (Input.GetKeyDown(toggleCoordinationKey))
            {
                ToggleCoordination();
            }
            
            if (Input.GetKeyDown(toggleLearningKey))
            {
                ToggleLearning();
            }
            
            if (Input.GetKeyDown(showStatsKey))
            {
                showStats = !showStats;
            }
        }

        private void UpdateAgentVisualization()
        {
            foreach (var agent in demoAgents)
            {
                if (agent == null) continue;
                
                var renderer = agent.GetComponent<SpriteRenderer>();
                if (renderer == null) continue;
                
                // Get coordination info
                var coordInfo = coordinationSystem.GetCoordinationInfo(agent);
                
                // Adjust size based on group membership
                float scale = coordInfo.isInGroup ? 1.5f : 1f;
                agent.transform.localScale = Vector3.one * scale;
                
                // Add glow effect for coordinating agents
                if (coordInfo.isInGroup)
                {
                    renderer.color = Color.Lerp(renderer.color, Color.white, 0.3f);
                }
            }
        }

        private void ResetDemo()
        {
            // Reset all learning progress
            multiAgentManager.ResetAllProgress();
            trainingCoordinator.ResetAllProgress();
            
            // Respawn agents in new positions
            foreach (var agent in demoAgents)
            {
                if (agent != null)
                {
                    Vector2 newPosition = Random.insideUnitCircle * spawnRadius;
                    agent.transform.position = newPosition;
                    agent.ResetDemoState();
                }
            }
            
            Debug.Log("Demo reset");
        }

        private void ToggleCoordination()
        {
            enableCoordination = !enableCoordination;
            
            if (enableCoordination)
            {
                // Re-register agents for coordination
                foreach (var agent in demoAgents)
                {
                    if (agent != null)
                    {
                        coordinationSystem.RegisterAgent(agent, agent.MonsterType);
                    }
                }
            }
            else
            {
                // Unregister agents from coordination
                foreach (var agent in demoAgents)
                {
                    if (agent != null)
                    {
                        coordinationSystem.UnregisterAgent(agent);
                    }
                }
            }
            
            Debug.Log($"Coordination {(enableCoordination ? "enabled" : "disabled")}");
        }

        private void ToggleLearning()
        {
            enableIndependentLearning = !enableIndependentLearning;
            
            var mode = enableIndependentLearning ? TrainingMode.Training : TrainingMode.Inference;
            trainingCoordinator.SetTrainingMode(mode);
            
            Debug.Log($"Learning {(enableIndependentLearning ? "enabled" : "disabled")}");
        }

        void OnGUI()
        {
            if (statsStyle == null)
            {
                statsStyle = new GUIStyle(GUI.skin.box);
                statsStyle.alignment = TextAnchor.UpperLeft;
                statsStyle.fontSize = 12;
            }
            
            // Show controls
            GUI.Box(new Rect(10, 10, 300, 120), 
                $"Multi-Monster Coordination Demo\n\n" +
                $"Controls:\n" +
                $"{resetKey} - Reset Demo\n" +
                $"{toggleCoordinationKey} - Toggle Coordination ({(enableCoordination ? "ON" : "OFF")})\n" +
                $"{toggleLearningKey} - Toggle Learning ({(enableIndependentLearning ? "ON" : "OFF")})\n" +
                $"{showStatsKey} - Toggle Stats ({(showStats ? "ON" : "OFF")})", 
                statsStyle);
            
            if (showStats)
            {
                ShowDetailedStats();
            }
        }

        private void ShowDetailedStats()
        {
            var stats = "Learning Statistics:\n\n";
            
            // Multi-agent learning stats
            var typeMetrics = multiAgentManager.GetAllTypeMetrics();
            foreach (var kvp in typeMetrics)
            {
                var type = kvp.Key;
                var metrics = kvp.Value;
                var agentCount = multiAgentManager.GetAgentCount(type);
                
                stats += $"{type}: {agentCount} agents\n";
                stats += $"  Avg Reward: {metrics.averageReward:F1}\n";
                stats += $"  Episodes: {metrics.episodeCount}\n";
                stats += $"  Loss: {metrics.lossValue:F3}\n\n";
            }
            
            // Coordination stats
            var groupMetrics = coordinationSystem.GetAllGroupMetrics();
            stats += $"Coordination Groups: {coordinationSystem.ActiveGroupCount}\n\n";
            
            foreach (var kvp in groupMetrics)
            {
                var type = kvp.Key;
                var metrics = kvp.Value;
                
                stats += $"{type} Groups:\n";
                stats += $"  Success Rate: {metrics.CoordinationSuccessRate:F2}\n";
                stats += $"  Avg Group Size: {metrics.averageGroupSize:F1}\n";
                stats += $"  Total Reward: {metrics.totalGroupReward:F1}\n\n";
            }
            
            GUI.Box(new Rect(Screen.width - 320, 10, 310, 400), stats, statsStyle);
        }

        void OnDestroy()
        {
            CancelInvoke();
        }
    }

    /// <summary>
    /// Demo agent that implements ILearningAgent for testing
    /// </summary>
    public class DemoAgent : MonoBehaviour, ILearningAgent
    {
        private MonsterType monsterType;
        private ActionSpace actionSpace;
        private LearningMetrics metrics;
        private bool isTraining = true;
        
        // Demo state
        private float lastActionTime;
        private Vector2 targetPosition;
        private float moveSpeed = 2f;

        public MonsterType MonsterType => monsterType;
        public bool IsTraining { get => isTraining; set => isTraining = value; }

        public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
        {
            this.monsterType = monsterType;
            this.actionSpace = actionSpace;
            this.metrics = LearningMetrics.CreateDefault();
            this.targetPosition = transform.position;
        }

        public int SelectAction(RLGameState state, bool isTraining)
        {
            // Simple demo action selection
            return Random.Range(0, actionSpace.actionCount);
        }

        public void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done)
        {
            // Update metrics based on experience
            metrics.averageReward = Mathf.Lerp(metrics.averageReward, reward, 0.1f);
            metrics.episodeCount++;
        }

        public void UpdatePolicy()
        {
            if (!isTraining) return;
            
            // Simulate learning progress
            metrics.averageReward += Random.Range(-0.5f, 1f);
            metrics.lossValue = Mathf.Max(0f, metrics.lossValue - 0.001f);
            metrics.explorationRate = Mathf.Max(0.01f, metrics.explorationRate - 0.0001f);
        }

        public void SaveBehaviorProfile(string filePath)
        {
            // Demo implementation
        }

        public void LoadBehaviorProfile(string filePath)
        {
            // Demo implementation
        }

        public LearningMetrics GetMetrics()
        {
            return metrics;
        }

        public void UpdateDemoBehavior()
        {
            // Simple movement behavior for visualization
            if (Vector2.Distance(transform.position, targetPosition) < 0.5f)
            {
                targetPosition = (Vector2)transform.position + Random.insideUnitCircle * 5f;
            }
            
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }

        public void ResetDemoState()
        {
            metrics = LearningMetrics.CreateDefault();
            targetPosition = transform.position;
        }
    }

    /// <summary>
    /// Mock entity manager for demo
    /// </summary>
    public class MockEntityManager : MonoBehaviour
    {
        // Mock implementation
    }

    /// <summary>
    /// Mock player for demo
    /// </summary>
    public class MockPlayer : MonoBehaviour
    {
        // Mock implementation
    }
}
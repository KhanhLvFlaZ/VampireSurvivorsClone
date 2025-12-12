using UnityEngine;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Main RL system manager that coordinates all RL components
    /// Integrates with existing game systems
    /// </summary>
    public class RLSystem : MonoBehaviour
    {
        [Header("RL System Settings")]
        [SerializeField] private bool enableRL = true;
        [SerializeField] private TrainingMode defaultTrainingMode = TrainingMode.Training;
        [SerializeField] private float maxFrameTimeMs = 16f; // Max 16ms per frame for 60 FPS
        [SerializeField] private int maxMemoryUsageMB = 100; // Max 100MB for RL components
        
        [Header("Network Settings")]
        [SerializeField] private NetworkArchitecture defaultArchitecture = NetworkArchitecture.Simple;
        [SerializeField] private int[] defaultHiddenLayers = new int[] { 64, 32 };
        [SerializeField] private LearningAlgorithm defaultAlgorithm = LearningAlgorithm.DQN;
        
        [Header("Dependencies")]
        [SerializeField] private EntityManager entityManager;
        [SerializeField] private Character playerCharacter;

        // Core RL components
        private ITrainingCoordinator trainingCoordinator;
        private IBehaviorProfileManager profileManager;
        private Dictionary<MonsterType, ActionSpace> actionSpaces;
        private Dictionary<MonsterType, ILearningAgent> agentTemplates;
        
        // Performance monitoring
        private float frameStartTime;
        private float totalRLProcessingTime;
        private int activeAgentCount;
        
        // System state
        private bool isInitialized = false;
        private string currentPlayerProfileId;

        public bool IsEnabled => enableRL && isInitialized;
        public TrainingMode CurrentTrainingMode => trainingCoordinator?.GetTrainingMode() ?? TrainingMode.Inference;
        public float CurrentFrameTime => totalRLProcessingTime;
        public int ActiveAgentCount => activeAgentCount;

        /// <summary>
        /// Initialize the RL system
        /// </summary>
        public void Initialize(EntityManager entityManager, Character playerCharacter, string playerProfileId = null)
        {
            if (isInitialized) return;

            this.entityManager = entityManager;
            this.playerCharacter = playerCharacter;
            this.currentPlayerProfileId = playerProfileId ?? "default";

            InitializeComponents();
            InitializeActionSpaces();
            InitializeAgentTemplates();

            isInitialized = true;
            
            Debug.Log($"RL System initialized with training mode: {defaultTrainingMode}");
        }

        private void InitializeComponents()
        {
            // Initialize training coordinator
            var coordinatorGO = new GameObject("TrainingCoordinator");
            coordinatorGO.transform.SetParent(transform);
            trainingCoordinator = coordinatorGO.AddComponent<TrainingCoordinator>();
            trainingCoordinator.Initialize(entityManager, playerCharacter);
            trainingCoordinator.SetTrainingMode(defaultTrainingMode);

            // Initialize profile manager
            var profileManagerGO = new GameObject("BehaviorProfileManager");
            profileManagerGO.transform.SetParent(transform);
            profileManager = profileManagerGO.AddComponent<BehaviorProfileManager>();
            profileManager.Initialize(currentPlayerProfileId);
        }

        private void InitializeActionSpaces()
        {
            actionSpaces = new Dictionary<MonsterType, ActionSpace>
            {
                { MonsterType.Melee, ActionSpace.CreateDefault() },
                { MonsterType.Ranged, CreateRangedActionSpace() },
                { MonsterType.Throwing, CreateThrowingActionSpace() },
                { MonsterType.Boomerang, CreateBoomerangActionSpace() },
                { MonsterType.Boss, ActionSpace.CreateAdvanced() }
            };
        }

        private void InitializeAgentTemplates()
        {
            agentTemplates = new Dictionary<MonsterType, ILearningAgent>();
            
            foreach (var monsterType in System.Enum.GetValues(typeof(MonsterType)))
            {
                if ((MonsterType)monsterType == MonsterType.None) continue;
                
                var agentGO = new GameObject($"AgentTemplate_{monsterType}");
                agentGO.transform.SetParent(transform);
                agentGO.SetActive(false); // Templates are inactive
                
                var agent = agentGO.AddComponent<DQNLearningAgent>();
                agent.Initialize((MonsterType)monsterType, actionSpaces[(MonsterType)monsterType]);
                
                agentTemplates[(MonsterType)monsterType] = agent;
            }
        }

        private ActionSpace CreateRangedActionSpace()
        {
            var actionSpace = ActionSpace.CreateDefault();
            actionSpace.canSpecialAttack = true; // Ranged monsters can use special attacks
            actionSpace.maxActionRange = 8f; // Longer range for ranged monsters
            return actionSpace;
        }

        private ActionSpace CreateThrowingActionSpace()
        {
            var actionSpace = ActionSpace.CreateDefault();
            actionSpace.canSpecialAttack = true;
            actionSpace.canAmbush = true; // Throwing monsters can ambush
            actionSpace.maxActionRange = 6f;
            return actionSpace;
        }

        private ActionSpace CreateBoomerangActionSpace()
        {
            var actionSpace = ActionSpace.CreateDefault();
            actionSpace.canSpecialAttack = true;
            actionSpace.canCoordinate = true; // Boomerang monsters can coordinate
            actionSpace.maxActionRange = 7f;
            return actionSpace;
        }

        void Update()
        {
            if (!IsEnabled) return;

            frameStartTime = Time.realtimeSinceStartup;
            
            // Update training coordinator
            trainingCoordinator?.UpdateAgents();
            
            // Monitor performance
            totalRLProcessingTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // Convert to ms
            
            // Check performance constraints
            if (totalRLProcessingTime > maxFrameTimeMs)
            {
                Debug.LogWarning($"RL processing exceeded frame time limit: {totalRLProcessingTime:F2}ms > {maxFrameTimeMs}ms");
            }
        }

        /// <summary>
        /// Create a learning agent for a specific monster
        /// </summary>
        public ILearningAgent CreateAgentForMonster(MonsterType monsterType)
        {
            if (!IsEnabled || !agentTemplates.ContainsKey(monsterType))
                return null;

            var templateAgent = agentTemplates[monsterType];
            var agentGO = new GameObject($"LearningAgent_{monsterType}_{System.Guid.NewGuid()}");
            
            var newAgent = agentGO.AddComponent<DQNLearningAgent>();
            newAgent.Initialize(monsterType, actionSpaces[monsterType]);
            
            // Load existing behavior profile if available
            var profile = profileManager.LoadProfile(monsterType);
            if (profile != null && profile.IsValid())
            {
                newAgent.LoadBehaviorProfile(GetProfilePath(profile));
            }

            // Register with training coordinator
            trainingCoordinator.RegisterAgent(newAgent, monsterType);
            activeAgentCount++;

            return newAgent;
        }

        /// <summary>
        /// Destroy a learning agent
        /// </summary>
        public void DestroyAgent(ILearningAgent agent)
        {
            if (agent == null) return;

            trainingCoordinator?.UnregisterAgent(agent);
            activeAgentCount = Mathf.Max(0, activeAgentCount - 1);

            if (agent is MonoBehaviour agentMono)
            {
                Destroy(agentMono.gameObject);
            }
        }

        /// <summary>
        /// Set training mode for all agents
        /// </summary>
        public void SetTrainingMode(TrainingMode mode)
        {
            trainingCoordinator?.SetTrainingMode(mode);
        }

        /// <summary>
        /// Save all behavior profiles
        /// </summary>
        public void SaveAllProfiles()
        {
            trainingCoordinator?.SaveAllProfiles();
        }

        /// <summary>
        /// Load all behavior profiles
        /// </summary>
        public void LoadAllProfiles()
        {
            trainingCoordinator?.LoadAllProfiles();
        }

        /// <summary>
        /// Get learning metrics for all monster types
        /// </summary>
        public Dictionary<MonsterType, LearningMetrics> GetAllMetrics()
        {
            return trainingCoordinator?.GetAllMetrics() ?? new Dictionary<MonsterType, LearningMetrics>();
        }

        /// <summary>
        /// Reset all learning progress
        /// </summary>
        public void ResetAllProgress()
        {
            trainingCoordinator?.ResetAllProgress();
        }

        /// <summary>
        /// Get action space for monster type
        /// </summary>
        public ActionSpace GetActionSpace(MonsterType monsterType)
        {
            return actionSpaces.ContainsKey(monsterType) ? actionSpaces[monsterType] : ActionSpace.CreateDefault();
        }

        private string GetProfilePath(BehaviorProfile profile)
        {
            return $"{profileManager.ProfileDirectory}/{profile.profileId}.json";
        }

        /// <summary>
        /// Check if system meets performance constraints
        /// </summary>
        public bool MeetsPerformanceConstraints()
        {
            return totalRLProcessingTime <= maxFrameTimeMs && 
                   GetMemoryUsageMB() <= maxMemoryUsageMB;
        }

        private float GetMemoryUsageMB()
        {
            // Simplified memory usage calculation
            // In production, use Unity Profiler API for accurate measurement
            return (activeAgentCount * 10f) + (profileManager?.GetStorageSize() ?? 0) / (1024f * 1024f);
        }

        void OnDestroy()
        {
            if (isInitialized)
            {
                SaveAllProfiles();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus && isInitialized)
            {
                SaveAllProfiles();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && isInitialized)
            {
                SaveAllProfiles();
            }
        }
    }
}
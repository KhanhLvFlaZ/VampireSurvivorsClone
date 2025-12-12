using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Manages independent learning per monster type and coordinates multi-monster learning
    /// Implements Requirements 1.3, 2.5, 5.2
    /// Integrates with existing TrainingCoordinator and MonsterCoordinationSystem
    /// </summary>
    public class MultiMonsterLearningManager : MonoBehaviour
    {
        [Header("Multi-Monster Learning Settings")]
        [SerializeField] private bool enableIndependentLearning = true;
        [SerializeField] private bool enableGroupLearning = true;
        [SerializeField] private float learningUpdateInterval = 0.5f;
        [SerializeField] private int maxConcurrentLearners = 20;
        
        [Header("Type-Specific Learning Rates")]
        [SerializeField] private float meleeBaseLearningRate = 0.001f;
        [SerializeField] private float rangedBaseLearningRate = 0.0015f;
        [SerializeField] private float throwingBaseLearningRate = 0.002f;
        [SerializeField] private float boomerangBaseLearningRate = 0.0012f;
        [SerializeField] private float bossBaseLearningRate = 0.0008f;
        
        // Core components
        private TrainingCoordinator trainingCoordinator;
        private MonsterCoordinationSystem coordinationSystem;
        private Dictionary<MonsterType, TypeLearningManager> typeLearningManagers;
        private Dictionary<MonsterType, List<IGroupLearningAgent>> coordinatedAgents;
        
        // Learning state tracking
        private Dictionary<MonsterType, LearningProgress> typeProgress;
        private Dictionary<string, GroupLearningSession> activeLearningGroups;
        private float lastLearningUpdate;
        
        // Performance monitoring
        private Dictionary<MonsterType, float> typePerformanceScores;
        private Dictionary<MonsterType, int> typeAgentCounts;
        
        // Events
        public event Action<MonsterType, LearningProgress> OnTypeLearningProgressUpdated;
        public event Action<string, GroupLearningSession> OnGroupLearningSessionStarted;
        public event Action<string, GroupLearningSession> OnGroupLearningSessionCompleted;

        public bool IsLearningActive => enableIndependentLearning || enableGroupLearning;
        public int TotalActiveAgents => coordinatedAgents?.Values.Sum(list => list.Count) ?? 0;

        void Awake()
        {
            InitializeMultiMonsterLearning();
        }

        private void InitializeMultiMonsterLearning()
        {
            // Initialize collections
            typeLearningManagers = new Dictionary<MonsterType, TypeLearningManager>();
            coordinatedAgents = new Dictionary<MonsterType, List<IGroupLearningAgent>>();
            typeProgress = new Dictionary<MonsterType, LearningProgress>();
            activeLearningGroups = new Dictionary<string, GroupLearningSession>();
            typePerformanceScores = new Dictionary<MonsterType, float>();
            typeAgentCounts = new Dictionary<MonsterType, int>();
            
            // Initialize for each monster type
            foreach (MonsterType monsterType in Enum.GetValues(typeof(MonsterType)))
            {
                if (monsterType == MonsterType.None) continue;
                
                InitializeTypeSpecificLearning(monsterType);
            }
            
            lastLearningUpdate = Time.time;
            
            Debug.Log("Multi-Monster Learning Manager initialized");
        }

        private void InitializeTypeSpecificLearning(MonsterType monsterType)
        {
            // Create type-specific learning manager
            var managerGO = new GameObject($"TypeLearningManager_{monsterType}");
            managerGO.transform.SetParent(transform);
            
            var typeManager = managerGO.AddComponent<TypeLearningManager>();
            typeManager.Initialize(monsterType, maxConcurrentLearners / 5, 0.8f); // Distribute capacity
            
            typeLearningManagers[monsterType] = typeManager;
            coordinatedAgents[monsterType] = new List<IGroupLearningAgent>();
            typeProgress[monsterType] = LearningProgress.CreateDefault();
            typePerformanceScores[monsterType] = 0f;
            typeAgentCounts[monsterType] = 0;
        }

        /// <summary>
        /// Initialize with existing systems
        /// </summary>
        public void Initialize(TrainingCoordinator coordinator, MonsterCoordinationSystem coordination)
        {
            trainingCoordinator = coordinator;
            coordinationSystem = coordination;
            
            // Subscribe to coordination events
            if (coordinationSystem != null)
            {
                coordinationSystem.OnGroupFormed += OnCoordinationGroupFormed;
                coordinationSystem.OnGroupDisbanded += OnCoordinationGroupDisbanded;
            }
        }

        /// <summary>
        /// Register an agent for multi-monster learning
        /// </summary>
        public void RegisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent == null || monsterType == MonsterType.None) return;
            
            // Register with type-specific manager
            if (typeLearningManagers.ContainsKey(monsterType))
            {
                typeLearningManagers[monsterType].RegisterAgent(agent);
                typeAgentCounts[monsterType]++;
            }
            
            // If agent supports coordination, add to coordinated agents
            if (agent is IGroupLearningAgent coordAgent)
            {
                coordinatedAgents[monsterType].Add(coordAgent);
            }
            
            Debug.Log($"Registered {monsterType} agent for multi-monster learning");
        }

        /// <summary>
        /// Unregister an agent from learning
        /// </summary>
        public void UnregisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent == null || monsterType == MonsterType.None) return;
            
            // Unregister from type-specific manager
            if (typeLearningManagers.ContainsKey(monsterType))
            {
                typeLearningManagers[monsterType].UnregisterAgent(agent);
                typeAgentCounts[monsterType] = Mathf.Max(0, typeAgentCounts[monsterType] - 1);
            }
            
            // Remove from coordinated agents if applicable
            if (agent is IGroupLearningAgent coordAgent && coordinatedAgents.ContainsKey(monsterType))
            {
                coordinatedAgents[monsterType].Remove(coordAgent);
            }
            
            Debug.Log($"Unregistered {monsterType} agent from multi-monster learning");
        }

        void Update()
        {
            if (!IsLearningActive) return;
            
            if (Time.time - lastLearningUpdate < learningUpdateInterval) return;
            
            lastLearningUpdate = Time.time;
            
            // Update independent learning per type
            if (enableIndependentLearning)
            {
                UpdateIndependentLearning();
            }
            
            // Update group learning sessions
            if (enableGroupLearning)
            {
                UpdateGroupLearning();
            }
            
            // Update performance tracking
            UpdatePerformanceTracking();
        }

        private void UpdateIndependentLearning()
        {
            foreach (var kvp in typeLearningManagers)
            {
                var monsterType = kvp.Key;
                var typeManager = kvp.Value;
                
                // Update learning for this type
                var metrics = typeManager.UpdateLearning();
                if (metrics.episodeCount > 0)
                {
                    // Update progress tracking
                    var progress = typeProgress[monsterType];
                    progress.UpdateFromMetrics(metrics);
                    typeProgress[monsterType] = progress;
                    
                    OnTypeLearningProgressUpdated?.Invoke(monsterType, progress);
                }
            }
        }

        private void UpdateGroupLearning()
        {
            // Update active group learning sessions
            var completedSessions = new List<string>();
            
            foreach (var kvp in activeLearningGroups)
            {
                var sessionId = kvp.Key;
                var session = kvp.Value;
                
                session.Update();
                
                if (session.IsCompleted)
                {
                    completedSessions.Add(sessionId);
                    OnGroupLearningSessionCompleted?.Invoke(sessionId, session);
                }
            }
            
            // Remove completed sessions
            foreach (var sessionId in completedSessions)
            {
                activeLearningGroups.Remove(sessionId);
            }
        }

        private void UpdatePerformanceTracking()
        {
            foreach (var monsterType in typeProgress.Keys)
            {
                var progress = typeProgress[monsterType];
                var agentCount = typeAgentCounts[monsterType];
                
                // Calculate performance score
                float performanceScore = CalculateTypePerformanceScore(progress, agentCount);
                typePerformanceScores[monsterType] = performanceScore;
            }
        }

        private float CalculateTypePerformanceScore(LearningProgress progress, int agentCount)
        {
            if (agentCount == 0) return 0f;
            
            // Combine multiple factors for performance score
            float learningEfficiency = progress.learningEfficiency;
            float coordinationSuccess = progress.coordinationSuccessRate;
            float adaptationRate = progress.adaptationRate;
            
            return (learningEfficiency + coordinationSuccess + adaptationRate) / 3f;
        }

        private void OnCoordinationGroupFormed(CoordinationGroup group)
        {
            if (!enableGroupLearning) return;
            
            // Start a group learning session
            var session = new GroupLearningSession
            {
                sessionId = Guid.NewGuid().ToString(),
                group = group,
                startTime = Time.time,
                participantAgents = GetCoordinatedAgentsInGroup(group),
                learningObjectives = DetermineLearningObjectives(group)
            };
            
            activeLearningGroups[session.sessionId] = session;
            OnGroupLearningSessionStarted?.Invoke(session.sessionId, session);
            
            Debug.Log($"Started group learning session for {group.monsterType} group");
        }

        private void OnCoordinationGroupDisbanded(CoordinationGroup group)
        {
            // Find and complete any active learning sessions for this group
            var sessionsToComplete = activeLearningGroups.Values
                .Where(s => s.group.groupId == group.groupId)
                .ToList();
            
            foreach (var session in sessionsToComplete)
            {
                session.CompleteSession();
                OnGroupLearningSessionCompleted?.Invoke(session.sessionId, session);
                activeLearningGroups.Remove(session.sessionId);
            }
        }

        private List<IGroupLearningAgent> GetCoordinatedAgentsInGroup(CoordinationGroup group)
        {
            var agents = new List<IGroupLearningAgent>();
            
            if (coordinatedAgents.ContainsKey(group.monsterType))
            {
                foreach (var agent in coordinatedAgents[group.monsterType])
                {
                    if (group.agents.Contains(agent as ILearningAgent))
                    {
                        agents.Add(agent);
                    }
                }
            }
            
            return agents;
        }

        private List<string> DetermineLearningObjectives(CoordinationGroup group)
        {
            var objectives = new List<string>();
            
            switch (group.coordinationStrategy)
            {
                case CoordinationStrategy.Surround:
                    objectives.Add("Learn optimal positioning for encirclement");
                    objectives.Add("Coordinate movement timing");
                    break;
                case CoordinationStrategy.CrossFire:
                    objectives.Add("Learn crossfire positioning");
                    objectives.Add("Coordinate attack timing");
                    break;
                case CoordinationStrategy.SequentialAttack:
                    objectives.Add("Learn attack sequence optimization");
                    objectives.Add("Coordinate attack intervals");
                    break;
                default:
                    objectives.Add("Learn basic group coordination");
                    break;
            }
            
            return objectives;
        }

        /// <summary>
        /// Get learning progress for a specific monster type
        /// </summary>
        public LearningProgress GetTypeProgress(MonsterType monsterType)
        {
            return typeProgress.ContainsKey(monsterType) ? 
                typeProgress[monsterType] : LearningProgress.CreateDefault();
        }

        /// <summary>
        /// Get performance score for a monster type
        /// </summary>
        public float GetTypePerformanceScore(MonsterType monsterType)
        {
            return typePerformanceScores.ContainsKey(monsterType) ? 
                typePerformanceScores[monsterType] : 0f;
        }

        /// <summary>
        /// Get agent count for a monster type
        /// </summary>
        public int GetAgentCount(MonsterType monsterType)
        {
            return typeAgentCounts.ContainsKey(monsterType) ? 
                typeAgentCounts[monsterType] : 0;
        }

        /// <summary>
        /// Reset learning progress for all types
        /// </summary>
        public void ResetAllProgress()
        {
            foreach (var typeManager in typeLearningManagers.Values)
            {
                typeManager.ResetProgress();
            }
            
            foreach (var monsterType in typeProgress.Keys.ToList())
            {
                typeProgress[monsterType] = LearningProgress.CreateDefault();
                typePerformanceScores[monsterType] = 0f;
            }
            
            activeLearningGroups.Clear();
            
            Debug.Log("Reset all multi-monster learning progress");
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (coordinationSystem != null)
            {
                coordinationSystem.OnGroupFormed -= OnCoordinationGroupFormed;
                coordinationSystem.OnGroupDisbanded -= OnCoordinationGroupDisbanded;
            }
            
            // Clean up type learning managers
            foreach (var typeManager in typeLearningManagers.Values)
            {
                if (typeManager != null)
                {
                    Destroy(typeManager.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Interface for agents that support group learning sessions
    /// </summary>
    public interface IGroupLearningAgent : ILearningAgent
    {
        void StartGroupLearning(GroupLearningSession session);
        void UpdateGroupLearning(GroupLearningSession session);
        void CompleteGroupLearning(GroupLearningSession session);
    }

    /// <summary>
    /// Tracks learning progress for a specific monster type
    /// </summary>
    [Serializable]
    public struct LearningProgress
    {
        public float learningEfficiency; // 0-1 how efficiently the type is learning
        public float coordinationSuccessRate; // 0-1 success rate in coordination
        public float adaptationRate; // 0-1 how quickly the type adapts to new strategies
        public int totalEpisodes;
        public float averageReward;
        public DateTime lastUpdated;
        
        public static LearningProgress CreateDefault()
        {
            return new LearningProgress
            {
                learningEfficiency = 0f,
                coordinationSuccessRate = 0f,
                adaptationRate = 0f,
                totalEpisodes = 0,
                averageReward = 0f,
                lastUpdated = DateTime.Now
            };
        }
        
        public void UpdateFromMetrics(LearningMetrics metrics)
        {
            learningEfficiency = Mathf.Clamp01(metrics.averageReward / 100f);
            totalEpisodes = metrics.episodeCount;
            averageReward = metrics.averageReward;
            lastUpdated = DateTime.Now;
            
            // Calculate adaptation rate based on recent performance changes
            adaptationRate = Mathf.Clamp01(1f - metrics.lossValue);
        }
    }

    /// <summary>
    /// Represents a group learning session
    /// </summary>
    [Serializable]
    public class GroupLearningSession
    {
        public string sessionId;
        public CoordinationGroup group;
        public float startTime;
        public List<IGroupLearningAgent> participantAgents;
        public List<string> learningObjectives;
        public Dictionary<string, float> objectiveProgress;
        public bool isCompleted;
        
        public float SessionDuration => Time.time - startTime;
        public bool IsCompleted => isCompleted;
        
        public void Update()
        {
            if (isCompleted) return;
            
            // Update objective progress
            UpdateObjectiveProgress();
            
            // Check completion conditions
            CheckCompletionConditions();
        }
        
        private void UpdateObjectiveProgress()
        {
            if (objectiveProgress == null)
            {
                objectiveProgress = new Dictionary<string, float>();
                foreach (var objective in learningObjectives)
                {
                    objectiveProgress[objective] = 0f;
                }
            }
            
            // Simulate progress based on group performance
            foreach (var objective in learningObjectives)
            {
                float currentProgress = objectiveProgress[objective];
                float progressIncrement = UnityEngine.Random.Range(0.01f, 0.05f);
                objectiveProgress[objective] = Mathf.Min(1f, currentProgress + progressIncrement);
            }
        }
        
        private void CheckCompletionConditions()
        {
            // Complete if all objectives are sufficiently progressed or session is too long
            bool allObjectivesComplete = objectiveProgress.Values.All(progress => progress >= 0.8f);
            bool sessionTooLong = SessionDuration > 300f; // 5 minutes max
            
            if (allObjectivesComplete || sessionTooLong)
            {
                CompleteSession();
            }
        }
        
        public void CompleteSession()
        {
            isCompleted = true;
            
            // Apply learning results to participant agents
            foreach (var agent in participantAgents)
            {
                agent?.CompleteGroupLearning(this);
            }
        }
    }
}
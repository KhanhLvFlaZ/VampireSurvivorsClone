using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Vampire;

namespace Vampire.RL
{
    /// <summary>
    /// Manages coordination between multiple RL monsters
    /// Implements Requirements 1.3, 2.5, 5.2
    /// Provides independent learning per monster type and group behavior coordination
    /// </summary>
    public class MonsterCoordinationSystem : MonoBehaviour
    {
        [Header("Coordination Settings")]
        [SerializeField] private float coordinationRadius = 10f;
        [SerializeField] private int maxCoordinationGroup = 5;
        [SerializeField] private float coordinationUpdateInterval = 0.2f;
        [SerializeField] private bool enableGroupLearning = true;
        
        [Header("Learning Independence")]
        [SerializeField] private bool enableIndependentLearning = true;
        [SerializeField] private float learningIsolationFactor = 0.8f; // How much to isolate learning between types
        
        // Core components
        private Dictionary<MonsterType, List<ILearningAgent>> agentsByType;
        private Dictionary<ILearningAgent, CoordinationState> coordinationStates;
        private Dictionary<MonsterType, GroupLearningMetrics> groupMetrics;
        private Dictionary<CoordinationGroup, GroupBehaviorProfile> groupProfiles;
        
        // Coordination tracking
        private List<CoordinationGroup> activeGroups;
        private float lastCoordinationUpdate;
        
        // Events for coordination
        public event Action<CoordinationGroup> OnGroupFormed;
        public event Action<CoordinationGroup> OnGroupDisbanded;
        public event Action<MonsterType, GroupLearningMetrics> OnGroupLearningUpdated;

        public int ActiveGroupCount => activeGroups?.Count ?? 0;
        public Dictionary<MonsterType, int> AgentCountsByType => 
            agentsByType?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count) ?? 
            new Dictionary<MonsterType, int>();

        void Awake()
        {
            InitializeCoordinationSystem();
        }

        private void InitializeCoordinationSystem()
        {
            agentsByType = new Dictionary<MonsterType, List<ILearningAgent>>();
            coordinationStates = new Dictionary<ILearningAgent, CoordinationState>();
            groupMetrics = new Dictionary<MonsterType, GroupLearningMetrics>();
            groupProfiles = new Dictionary<CoordinationGroup, GroupBehaviorProfile>();
            activeGroups = new List<CoordinationGroup>();
            
            // Initialize group metrics for each monster type
            foreach (MonsterType monsterType in Enum.GetValues(typeof(MonsterType)))
            {
                if (monsterType != MonsterType.None)
                {
                    groupMetrics[monsterType] = GroupLearningMetrics.CreateDefault();
                }
            }
            
            lastCoordinationUpdate = Time.time;
            
            Debug.Log("Monster Coordination System initialized");
        }

        /// <summary>
        /// Register an agent for coordination (Requirement 1.3 - Independent learning per monster type)
        /// </summary>
        public void RegisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent == null || monsterType == MonsterType.None) return;

            // Add to type-specific list for independent learning
            if (!agentsByType.ContainsKey(monsterType))
            {
                agentsByType[monsterType] = new List<ILearningAgent>();
            }
            
            if (!agentsByType[monsterType].Contains(agent))
            {
                agentsByType[monsterType].Add(agent);
                
                // Initialize coordination state
                coordinationStates[agent] = new CoordinationState
                {
                    agent = agent,
                    monsterType = monsterType,
                    currentGroup = null,
                    lastCoordinationTime = Time.time,
                    coordinationSuccess = 0f,
                    independentLearningProgress = 0f
                };
                
                Debug.Log($"Registered {monsterType} agent for coordination (Total {monsterType}: {agentsByType[monsterType].Count})");
            }
        }

        /// <summary>
        /// Unregister an agent from coordination
        /// </summary>
        public void UnregisterAgent(ILearningAgent agent)
        {
            if (agent == null || !coordinationStates.ContainsKey(agent)) return;

            var state = coordinationStates[agent];
            var monsterType = state.monsterType;
            
            // Remove from type-specific list
            if (agentsByType.ContainsKey(monsterType))
            {
                agentsByType[monsterType].Remove(agent);
                if (agentsByType[monsterType].Count == 0)
                {
                    agentsByType.Remove(monsterType);
                }
            }
            
            // Remove from any coordination group
            if (state.currentGroup != null)
            {
                RemoveAgentFromGroup(agent, state.currentGroup);
            }
            
            // Remove coordination state
            coordinationStates.Remove(agent);
            
            Debug.Log($"Unregistered {monsterType} agent from coordination");
        }

        void Update()
        {
            if (Time.time - lastCoordinationUpdate < coordinationUpdateInterval) return;
            
            lastCoordinationUpdate = Time.time;
            
            // Update coordination groups
            UpdateCoordinationGroups();
            
            // Update group learning if enabled
            if (enableGroupLearning)
            {
                UpdateGroupLearning();
            }
            
            // Update independent learning metrics
            if (enableIndependentLearning)
            {
                UpdateIndependentLearningMetrics();
            }
        }

        /// <summary>
        /// Update coordination groups based on proximity and behavior (Requirement 5.2)
        /// </summary>
        public void UpdateCoordinationGroups()
        {
            // Dissolve groups that are no longer valid
            for (int i = activeGroups.Count - 1; i >= 0; i--)
            {
                var group = activeGroups[i];
                if (!IsGroupValid(group))
                {
                    DisbandGroup(group);
                }
            }
            
            // Form new groups based on proximity and coordination potential
            foreach (var typeAgents in agentsByType)
            {
                var monsterType = typeAgents.Key;
                var agents = typeAgents.Value;
                
                // Find agents that could coordinate
                var availableAgents = agents.Where(a => 
                    coordinationStates.ContainsKey(a) && 
                    coordinationStates[a].currentGroup == null).ToList();
                
                if (availableAgents.Count >= 2)
                {
                    FormCoordinationGroups(availableAgents, monsterType);
                }
            }
        }

        private void FormCoordinationGroups(List<ILearningAgent> availableAgents, MonsterType monsterType)
        {
            // Group agents by proximity
            var proximityGroups = new List<List<ILearningAgent>>();
            
            foreach (var agent in availableAgents)
            {
                if (!coordinationStates.ContainsKey(agent)) continue;
                
                var agentState = coordinationStates[agent];
                var agentPosition = GetAgentPosition(agent);
                
                // Find existing group within coordination radius
                List<ILearningAgent> targetGroup = null;
                foreach (var group in proximityGroups)
                {
                    if (group.Count >= maxCoordinationGroup) continue;
                    
                    bool withinRadius = group.Any(groupAgent =>
                    {
                        var groupAgentPos = GetAgentPosition(groupAgent);
                        return Vector2.Distance(agentPosition, groupAgentPos) <= coordinationRadius;
                    });
                    
                    if (withinRadius)
                    {
                        targetGroup = group;
                        break;
                    }
                }
                
                // Create new group if no suitable group found
                if (targetGroup == null)
                {
                    targetGroup = new List<ILearningAgent>();
                    proximityGroups.Add(targetGroup);
                }
                
                targetGroup.Add(agent);
            }
            
            // Create coordination groups from proximity groups
            foreach (var proximityGroup in proximityGroups)
            {
                if (proximityGroup.Count >= 2)
                {
                    CreateCoordinationGroup(proximityGroup, monsterType);
                }
            }
        }

        private void CreateCoordinationGroup(List<ILearningAgent> agents, MonsterType monsterType)
        {
            var group = new CoordinationGroup
            {
                groupId = Guid.NewGuid().ToString(),
                monsterType = monsterType,
                agents = new List<ILearningAgent>(agents),
                formationTime = Time.time,
                coordinationStrategy = DetermineCoordinationStrategy(agents, monsterType),
                groupReward = 0f,
                successfulCoordinations = 0
            };
            
            // Assign agents to group
            foreach (var agent in agents)
            {
                if (coordinationStates.ContainsKey(agent))
                {
                    var state = coordinationStates[agent];
                    state.currentGroup = group;
                    coordinationStates[agent] = state;
                }
            }
            
            activeGroups.Add(group);
            
            // Initialize group behavior profile
            if (enableGroupLearning)
            {
                groupProfiles[group] = GroupBehaviorProfile.CreateDefault(group);
            }
            
            OnGroupFormed?.Invoke(group);
            
            Debug.Log($"Formed coordination group for {monsterType} with {agents.Count} agents");
        }

        private CoordinationStrategy DetermineCoordinationStrategy(List<ILearningAgent> agents, MonsterType monsterType)
        {
            // Determine strategy based on monster type and group size
            switch (monsterType)
            {
                case MonsterType.Melee:
                    return agents.Count >= 3 ? CoordinationStrategy.Surround : CoordinationStrategy.Flank;
                case MonsterType.Ranged:
                    return CoordinationStrategy.CrossFire;
                case MonsterType.Throwing:
                    return CoordinationStrategy.SequentialAttack;
                case MonsterType.Boomerang:
                    return CoordinationStrategy.ZoneControl;
                case MonsterType.Boss:
                    return CoordinationStrategy.Overwhelm;
                default:
                    return CoordinationStrategy.Basic;
            }
        }

        private bool IsGroupValid(CoordinationGroup group)
        {
            if (group == null || group.agents == null) return false;
            
            // Remove null or invalid agents
            group.agents.RemoveAll(a => a == null || !coordinationStates.ContainsKey(a));
            
            // Group needs at least 2 agents
            if (group.agents.Count < 2) return false;
            
            // Check if agents are still within coordination radius
            var positions = group.agents.Select(GetAgentPosition).ToList();
            var centerPosition = positions.Aggregate(Vector2.zero, (sum, pos) => sum + pos) / positions.Count;
            
            return positions.All(pos => Vector2.Distance(pos, centerPosition) <= coordinationRadius * 1.5f);
        }

        private void RemoveAgentFromGroup(ILearningAgent agent, CoordinationGroup group)
        {
            if (group == null || agent == null) return;
            
            // Remove agent from group
            group.agents.Remove(agent);
            
            // Clear agent's group reference
            if (coordinationStates.ContainsKey(agent))
            {
                var state = coordinationStates[agent];
                state.currentGroup = null;
                coordinationStates[agent] = state;
            }
            
            // If group becomes too small, disband it
            if (group.agents.Count < 2)
            {
                DisbandGroup(group);
            }
            
            Debug.Log($"Removed agent from coordination group for {group.monsterType}");
        }

        private void DisbandGroup(CoordinationGroup group)
        {
            if (group == null) return;
            
            // Remove agents from group
            foreach (var agent in group.agents)
            {
                if (coordinationStates.ContainsKey(agent))
                {
                    var state = coordinationStates[agent];
                    state.currentGroup = null;
                    coordinationStates[agent] = state;
                }
            }
            
            // Remove group from active list
            activeGroups.Remove(group);
            
            // Remove group profile
            if (groupProfiles.ContainsKey(group))
            {
                groupProfiles.Remove(group);
            }
            
            OnGroupDisbanded?.Invoke(group);
            
            Debug.Log($"Disbanded coordination group for {group.monsterType}");
        }

        /// <summary>
        /// Update group learning metrics and behavior (Requirement 5.2 - Group behavior learning)
        /// </summary>
        private void UpdateGroupLearning()
        {
            foreach (var group in activeGroups)
            {
                if (!groupProfiles.ContainsKey(group)) continue;
                
                var profile = groupProfiles[group];
                
                // Update group coordination success
                float groupSuccess = CalculateGroupCoordinationSuccess(group);
                profile.coordinationSuccess = Mathf.Lerp(profile.coordinationSuccess, groupSuccess, 0.1f);
                
                // Update group learning metrics
                if (groupMetrics.ContainsKey(group.monsterType))
                {
                    var metrics = groupMetrics[group.monsterType];
                    metrics.groupCoordinationSuccess = profile.coordinationSuccess;
                    metrics.activeGroupCount = activeGroups.Count(g => g.monsterType == group.monsterType);
                    metrics.averageGroupSize = (float)activeGroups
                        .Where(g => g.monsterType == group.monsterType)
                        .Average(g => g.agents.Count);
                    
                    groupMetrics[group.monsterType] = metrics;
                    
                    OnGroupLearningUpdated?.Invoke(group.monsterType, metrics);
                }
                
                // Apply group learning to individual agents
                ApplyGroupLearningToAgents(group, profile);
            }
        }

        private float CalculateGroupCoordinationSuccess(CoordinationGroup group)
        {
            if (group.agents.Count == 0) return 0f;
            
            float totalSuccess = 0f;
            int validAgents = 0;
            
            foreach (var agent in group.agents)
            {
                if (coordinationStates.ContainsKey(agent))
                {
                    totalSuccess += coordinationStates[agent].coordinationSuccess;
                    validAgents++;
                }
            }
            
            return validAgents > 0 ? totalSuccess / validAgents : 0f;
        }

        private void ApplyGroupLearningToAgents(CoordinationGroup group, GroupBehaviorProfile profile)
        {
            // Share successful coordination patterns among group members
            foreach (var agent in group.agents)
            {
                if (!coordinationStates.ContainsKey(agent)) continue;
                
                var state = coordinationStates[agent];
                
                // Boost individual learning based on group success
                if (profile.coordinationSuccess > 0.7f)
                {
                    state.coordinationSuccess = Mathf.Min(1f, state.coordinationSuccess + 0.05f);
                }
                
                coordinationStates[agent] = state;
            }
        }

        /// <summary>
        /// Update independent learning metrics per monster type (Requirement 1.3, 2.5)
        /// </summary>
        private void UpdateIndependentLearningMetrics()
        {
            foreach (var typeAgents in agentsByType)
            {
                var monsterType = typeAgents.Key;
                var agents = typeAgents.Value;
                
                if (agents.Count == 0) continue;
                
                // Calculate independent learning progress for this monster type
                float totalProgress = 0f;
                int validAgents = 0;
                
                foreach (var agent in agents)
                {
                    if (coordinationStates.ContainsKey(agent))
                    {
                        var metrics = agent.GetMetrics();
                        var progress = CalculateIndependentLearningProgress(metrics);
                        
                        coordinationStates[agent] = new CoordinationState
                        {
                            agent = coordinationStates[agent].agent,
                            monsterType = coordinationStates[agent].monsterType,
                            currentGroup = coordinationStates[agent].currentGroup,
                            lastCoordinationTime = coordinationStates[agent].lastCoordinationTime,
                            coordinationSuccess = coordinationStates[agent].coordinationSuccess,
                            independentLearningProgress = progress
                        };
                        
                        totalProgress += progress;
                        validAgents++;
                    }
                }
                
                // Update group metrics with independent learning data
                if (validAgents > 0 && groupMetrics.ContainsKey(monsterType))
                {
                    var metrics = groupMetrics[monsterType];
                    metrics.independentLearningProgress = totalProgress / validAgents;
                    metrics.agentCount = validAgents;
                    groupMetrics[monsterType] = metrics;
                }
            }
        }

        private float CalculateIndependentLearningProgress(LearningMetrics metrics)
        {
            // Calculate learning progress based on multiple factors
            float rewardProgress = Mathf.Clamp01(metrics.averageReward / 100f); // Normalize to 0-1
            float episodeProgress = Mathf.Clamp01(metrics.episodeCount / 1000f); // Normalize to 0-1
            float lossProgress = metrics.lossValue > 0 ? Mathf.Clamp01(1f - metrics.lossValue) : 0f;
            
            return (rewardProgress + episodeProgress + lossProgress) / 3f;
        }

        /// <summary>
        /// Get coordination information for an agent's decision making
        /// </summary>
        public CoordinationInfo GetCoordinationInfo(ILearningAgent agent)
        {
            if (!coordinationStates.ContainsKey(agent))
                return CoordinationInfo.CreateEmpty();
            
            var state = coordinationStates[agent];
            var info = new CoordinationInfo
            {
                isInGroup = state.currentGroup != null,
                groupSize = state.currentGroup?.agents.Count ?? 0,
                coordinationStrategy = state.currentGroup?.coordinationStrategy ?? CoordinationStrategy.None,
                nearbyAllies = GetNearbyAllies(agent),
                groupTarget = GetGroupTarget(state.currentGroup),
                coordinationSuccess = state.coordinationSuccess
            };
            
            return info;
        }

        private List<Vector2> GetNearbyAllies(ILearningAgent agent)
        {
            var allies = new List<Vector2>();
            var agentPosition = GetAgentPosition(agent);
            
            if (!coordinationStates.ContainsKey(agent)) return allies;
            
            var monsterType = coordinationStates[agent].monsterType;
            
            if (agentsByType.ContainsKey(monsterType))
            {
                foreach (var ally in agentsByType[monsterType])
                {
                    if (ally == agent) continue;
                    
                    var allyPosition = GetAgentPosition(ally);
                    if (Vector2.Distance(agentPosition, allyPosition) <= coordinationRadius)
                    {
                        allies.Add(allyPosition);
                    }
                }
            }
            
            return allies;
        }

        private Vector2 GetGroupTarget(CoordinationGroup group)
        {
            if (group == null) return Vector2.zero;
            
            // For now, return player position as the primary target
            // In a full implementation, this could be more sophisticated
            var player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }

        private Vector2 GetAgentPosition(ILearningAgent agent)
        {
            // Get position from the agent's MonoBehaviour component
            if (agent is MonoBehaviour agentMono)
            {
                return agentMono.transform.position;
            }
            
            return Vector2.zero;
        }

        /// <summary>
        /// Record coordination success for reward calculation
        /// </summary>
        public void RecordCoordinationSuccess(ILearningAgent agent, bool success, float reward = 0f)
        {
            if (!coordinationStates.ContainsKey(agent)) return;
            
            var state = coordinationStates[agent];
            
            // Update coordination success rate
            float successValue = success ? 1f : 0f;
            state.coordinationSuccess = Mathf.Lerp(state.coordinationSuccess, successValue, 0.2f);
            state.lastCoordinationTime = Time.time;
            
            // Update group reward if in a group
            if (state.currentGroup != null)
            {
                state.currentGroup.groupReward += reward;
                if (success)
                {
                    state.currentGroup.successfulCoordinations++;
                }
            }
            
            coordinationStates[agent] = state;
        }

        /// <summary>
        /// Get learning metrics for all monster types
        /// </summary>
        public Dictionary<MonsterType, GroupLearningMetrics> GetAllGroupMetrics()
        {
            return new Dictionary<MonsterType, GroupLearningMetrics>(groupMetrics);
        }

        /// <summary>
        /// Get coordination state for an agent
        /// </summary>
        public CoordinationState? GetCoordinationState(ILearningAgent agent)
        {
            return coordinationStates.ContainsKey(agent) ? coordinationStates[agent] : null;
        }

        /// <summary>
        /// Force coordination between specific agents (for testing/debugging)
        /// </summary>
        public void ForceCoordination(List<ILearningAgent> agents, MonsterType monsterType)
        {
            if (agents == null || agents.Count < 2) return;
            
            CreateCoordinationGroup(agents, monsterType);
        }

        void OnDestroy()
        {
            // Clean up all groups
            foreach (var group in activeGroups.ToList())
            {
                DisbandGroup(group);
            }
        }
    }
}
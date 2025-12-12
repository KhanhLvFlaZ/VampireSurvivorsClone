using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Represents a coordination group of monsters working together
    /// </summary>
    [Serializable]
    public class CoordinationGroup
    {
        public string groupId;
        public MonsterType monsterType;
        public List<ILearningAgent> agents;
        public float formationTime;
        public CoordinationStrategy coordinationStrategy;
        public float groupReward;
        public int successfulCoordinations;
        
        public int GroupSize => agents?.Count ?? 0;
        public float GroupAge => Time.time - formationTime;
        public float AverageSuccessRate => successfulCoordinations > 0 ? groupReward / successfulCoordinations : 0f;
    }

    /// <summary>
    /// Coordination strategies that groups can employ
    /// </summary>
    public enum CoordinationStrategy
    {
        None = 0,
        Basic = 1,          // Simple follow-the-leader
        Flank = 2,          // Attack from multiple sides
        Surround = 3,       // Encircle the target
        CrossFire = 4,      // Ranged units create crossfire
        SequentialAttack = 5, // Attack in sequence
        ZoneControl = 6,    // Control specific areas
        Overwhelm = 7       // Mass coordinated assault
    }

    /// <summary>
    /// Current coordination state of an individual agent
    /// </summary>
    [Serializable]
    public struct CoordinationState
    {
        public ILearningAgent agent;
        public MonsterType monsterType;
        public CoordinationGroup currentGroup;
        public float lastCoordinationTime;
        public float coordinationSuccess; // 0-1 success rate
        public float independentLearningProgress; // 0-1 learning progress
        
        public bool IsInGroup => currentGroup != null;
        public float TimeSinceLastCoordination => Time.time - lastCoordinationTime;
    }

    /// <summary>
    /// Information about coordination opportunities for decision making
    /// </summary>
    [Serializable]
    public struct CoordinationInfo
    {
        public bool isInGroup;
        public int groupSize;
        public CoordinationStrategy coordinationStrategy;
        public List<Vector2> nearbyAllies;
        public Vector2 groupTarget;
        public float coordinationSuccess;
        
        public static CoordinationInfo CreateEmpty()
        {
            return new CoordinationInfo
            {
                isInGroup = false,
                groupSize = 0,
                coordinationStrategy = CoordinationStrategy.None,
                nearbyAllies = new List<Vector2>(),
                groupTarget = Vector2.zero,
                coordinationSuccess = 0f
            };
        }
        
        public bool HasNearbyAllies => nearbyAllies != null && nearbyAllies.Count > 0;
        public int AllyCount => nearbyAllies?.Count ?? 0;
    }

    /// <summary>
    /// Learning metrics specific to group behavior and coordination
    /// </summary>
    [Serializable]
    public struct GroupLearningMetrics
    {
        public float independentLearningProgress; // 0-1 progress for this monster type
        public float groupCoordinationSuccess; // 0-1 success rate for group coordination
        public int agentCount; // Number of agents of this type
        public int activeGroupCount; // Number of active coordination groups
        public float averageGroupSize; // Average size of coordination groups
        public float totalGroupReward; // Total reward earned through coordination
        public int totalCoordinations; // Total coordination attempts
        public int successfulCoordinations; // Successful coordination attempts
        
        public static GroupLearningMetrics CreateDefault()
        {
            return new GroupLearningMetrics
            {
                independentLearningProgress = 0f,
                groupCoordinationSuccess = 0f,
                agentCount = 0,
                activeGroupCount = 0,
                averageGroupSize = 0f,
                totalGroupReward = 0f,
                totalCoordinations = 0,
                successfulCoordinations = 0
            };
        }
        
        public float CoordinationSuccessRate => totalCoordinations > 0 ? 
            (float)successfulCoordinations / totalCoordinations : 0f;
        
        public float AverageRewardPerCoordination => successfulCoordinations > 0 ? 
            totalGroupReward / successfulCoordinations : 0f;
    }

    /// <summary>
    /// Behavior profile for a coordination group
    /// </summary>
    [Serializable]
    public class GroupBehaviorProfile
    {
        public string groupId;
        public MonsterType monsterType;
        public CoordinationStrategy strategy;
        public float coordinationSuccess;
        public float[] strategyWeights; // Weights for different coordination actions
        public DateTime creationTime;
        public DateTime lastUpdated;
        public int coordinationAttempts;
        public int successfulCoordinations;
        
        public static GroupBehaviorProfile CreateDefault(CoordinationGroup group)
        {
            return new GroupBehaviorProfile
            {
                groupId = group.groupId,
                monsterType = group.monsterType,
                strategy = group.coordinationStrategy,
                coordinationSuccess = 0f,
                strategyWeights = new float[8], // One for each coordination action type
                creationTime = DateTime.Now,
                lastUpdated = DateTime.Now,
                coordinationAttempts = 0,
                successfulCoordinations = 0
            };
        }
        
        public float SuccessRate => coordinationAttempts > 0 ? 
            (float)successfulCoordinations / coordinationAttempts : 0f;
    }

    /// <summary>
    /// Coordination action types for group behavior
    /// </summary>
    public enum CoordinationActionType
    {
        None = 0,
        FormGroup = 1,      // Form a coordination group
        JoinGroup = 2,      // Join an existing group
        LeaveGroup = 3,     // Leave current group
        FollowLeader = 4,   // Follow the group leader
        FlankTarget = 5,    // Move to flank position
        SurroundTarget = 6, // Move to surround position
        SynchronizedAttack = 7, // Attack in coordination with group
        DefendAlly = 8      // Defend a group member
    }

    /// <summary>
    /// Group formation types for coordinated positioning
    /// </summary>
    public enum GroupFormation
    {
        None = 0,
        Cluster = 1,        // Tight cluster formation
        Line = 2,           // Linear formation
        Arc = 3,            // Arc/semicircle formation
        Surround = 4,       // Surrounding formation
        Scatter = 5,        // Scattered formation
        Support = 6,        // Support formation (for boss monsters)
        Wedge = 7,          // Wedge/triangle formation
        Diamond = 8         // Diamond formation
    }

    /// <summary>
    /// Represents a learned group behavior pattern
    /// </summary>
    [Serializable]
    public class GroupBehaviorPattern
    {
        public string patternId;
        public MonsterType monsterType;
        public GroupFormation formation;
        public CoordinationStrategy strategy;
        public List<Vector2> positions; // Relative positions for the pattern
        public float successRate;
        public int timesUsed;
        public DateTime lastUsed;
        public Dictionary<string, float> contextConditions; // Conditions when this pattern works best
        
        public static GroupBehaviorPattern CreateDefault(MonsterType monsterType, GroupFormation formation)
        {
            return new GroupBehaviorPattern
            {
                patternId = System.Guid.NewGuid().ToString(),
                monsterType = monsterType,
                formation = formation,
                strategy = CoordinationStrategy.Basic,
                positions = new List<Vector2>(),
                successRate = 0f,
                timesUsed = 0,
                lastUsed = DateTime.Now,
                contextConditions = new Dictionary<string, float>()
            };
        }
        
        public bool IsEffective => successRate > 0.6f && timesUsed > 5;
        public float RecentUsage => (float)(DateTime.Now - lastUsed).TotalMinutes;
    }

    /// <summary>
    /// Extended game state that includes coordination information
    /// </summary>
    [Serializable]
    public struct CoordinationGameState
    {
        public RLGameState baseState;
        public CoordinationInfo coordinationInfo;
        public GroupLearningMetrics groupMetrics;
        
        public static CoordinationGameState CreateFromBase(RLGameState baseState, 
            CoordinationInfo coordinationInfo, GroupLearningMetrics groupMetrics)
        {
            return new CoordinationGameState
            {
                baseState = baseState,
                coordinationInfo = coordinationInfo,
                groupMetrics = groupMetrics
            };
        }
    }
}
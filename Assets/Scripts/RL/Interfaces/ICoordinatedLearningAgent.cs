using UnityEngine;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Extended interface for learning agents that support coordination
    /// Builds upon ILearningAgent to add coordination capabilities
    /// </summary>
    public interface ICoordinatedLearningAgent : ILearningAgent
    {
        /// <summary>
        /// Current coordination group this agent belongs to
        /// </summary>
        CoordinationGroup CurrentGroup { get; set; }
        
        /// <summary>
        /// Whether this agent can participate in coordination
        /// </summary>
        bool CanCoordinate { get; }
        
        /// <summary>
        /// Select action considering coordination with other agents
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <param name="coordinationData">Information about nearby coordinating agents</param>
        /// <param name="isTraining">Whether in training mode</param>
        /// <returns>Action index to execute</returns>
        int SelectCoordinatedAction(RLGameState state, CoordinationContext coordinationData, bool isTraining);
        
        /// <summary>
        /// Store coordination experience for group learning
        /// </summary>
        /// <param name="state">Previous state</param>
        /// <param name="action">Action taken</param>
        /// <param name="coordinationReward">Reward from coordination</param>
        /// <param name="nextState">Resulting state</param>
        /// <param name="groupOutcome">Outcome of group action</param>
        void StoreCoordinationExperience(RLGameState state, int action, float coordinationReward, 
            RLGameState nextState, GroupActionOutcome groupOutcome);
        
        /// <summary>
        /// Update policy considering group learning
        /// </summary>
        /// <param name="groupFeedback">Feedback from group coordination</param>
        void UpdateCoordinatedPolicy(GroupLearningFeedback groupFeedback);
        
        /// <summary>
        /// Get coordination-specific metrics
        /// </summary>
        CoordinationMetrics GetCoordinationMetrics();
        
        /// <summary>
        /// Set coordination parameters for this agent
        /// </summary>
        /// <param name="parameters">Coordination parameters</param>
        void SetCoordinationParameters(CoordinationParameters parameters);
    }

    /// <summary>
    /// Context information for coordination decisions
    /// </summary>
    public struct CoordinationContext
    {
        public List<NearbyCoordinatingAgent> nearbyAgents;
        public GroupFormation currentFormation;
        public Vector2 groupCenter;
        public float groupCoordinationScore;
        public CoordinationActionType suggestedAction;
        
        public static CoordinationContext CreateEmpty()
        {
            return new CoordinationContext
            {
                nearbyAgents = new List<NearbyCoordinatingAgent>(),
                currentFormation = GroupFormation.Cluster,
                groupCenter = Vector2.zero,
                groupCoordinationScore = 0f,
                suggestedAction = CoordinationActionType.None
            };
        }
    }

    /// <summary>
    /// Information about nearby coordinating agents
    /// </summary>
    public struct NearbyCoordinatingAgent
    {
        public Vector2 position;
        public MonsterType monsterType;
        public int currentAction;
        public float coordinationScore;
        public bool isInSameGroup;
        
        public static NearbyCoordinatingAgent Create(Vector2 pos, MonsterType type, int action, float score, bool sameGroup)
        {
            return new NearbyCoordinatingAgent
            {
                position = pos,
                monsterType = type,
                currentAction = action,
                coordinationScore = score,
                isInSameGroup = sameGroup
            };
        }
    }

    /// <summary>
    /// Outcome of a group action for learning
    /// </summary>
    public struct GroupActionOutcome
    {
        public bool groupActionSucceeded;
        public float groupReward;
        public int participatingAgents;
        public float synchronizationScore;
        public bool formationMaintained;
        
        public static GroupActionOutcome CreateSuccess(float reward, int agents, float sync)
        {
            return new GroupActionOutcome
            {
                groupActionSucceeded = true,
                groupReward = reward,
                participatingAgents = agents,
                synchronizationScore = sync,
                formationMaintained = true
            };
        }
        
        public static GroupActionOutcome CreateFailure(int agents)
        {
            return new GroupActionOutcome
            {
                groupActionSucceeded = false,
                groupReward = -10f,
                participatingAgents = agents,
                synchronizationScore = 0f,
                formationMaintained = false
            };
        }
    }

    /// <summary>
    /// Feedback for group learning
    /// </summary>
    public struct GroupLearningFeedback
    {
        public float groupPerformance;
        public Dictionary<string, float> formationEffectiveness;
        public float averageCoordinationScore;
        public List<string> successfulPatterns;
        public List<string> failedPatterns;
        
        public static GroupLearningFeedback CreateDefault()
        {
            return new GroupLearningFeedback
            {
                groupPerformance = 0f,
                formationEffectiveness = new Dictionary<string, float>(),
                averageCoordinationScore = 0f,
                successfulPatterns = new List<string>(),
                failedPatterns = new List<string>()
            };
        }
    }

    /// <summary>
    /// Coordination-specific learning metrics
    /// </summary>
    public struct CoordinationMetrics
    {
        public float coordinationSuccessRate;
        public float averageGroupReward;
        public int groupActionsPerformed;
        public float formationAdherence;
        public float synchronizationAccuracy;
        public Dictionary<GroupFormation, float> formationPreferences;
        
        public static CoordinationMetrics CreateDefault()
        {
            return new CoordinationMetrics
            {
                coordinationSuccessRate = 0f,
                averageGroupReward = 0f,
                groupActionsPerformed = 0,
                formationAdherence = 0f,
                synchronizationAccuracy = 0f,
                formationPreferences = new Dictionary<GroupFormation, float>()
            };
        }
    }

    /// <summary>
    /// Parameters for coordination behavior
    /// </summary>
    public struct CoordinationParameters
    {
        public float coordinationWeight; // How much to weight coordination vs individual actions
        public float formationTolerance; // How strictly to maintain formation
        public float synchronizationWindow; // Time window for synchronized actions
        public bool enableGroupLearning; // Whether to learn from group experiences
        public GroupFormation preferredFormation; // Preferred formation type
        
        public static CoordinationParameters CreateDefault(MonsterType monsterType)
        {
            var parameters = new CoordinationParameters
            {
                coordinationWeight = 0.3f,
                formationTolerance = 2f,
                synchronizationWindow = 0.5f,
                enableGroupLearning = true
            };
            
            // Set type-specific preferences
            switch (monsterType)
            {
                case MonsterType.Melee:
                    parameters.preferredFormation = GroupFormation.Surround;
                    parameters.coordinationWeight = 0.4f;
                    break;
                case MonsterType.Ranged:
                    parameters.preferredFormation = GroupFormation.Line;
                    parameters.coordinationWeight = 0.5f;
                    break;
                case MonsterType.Throwing:
                    parameters.preferredFormation = GroupFormation.Arc;
                    parameters.coordinationWeight = 0.35f;
                    break;
                case MonsterType.Boomerang:
                    parameters.preferredFormation = GroupFormation.Scatter;
                    parameters.coordinationWeight = 0.25f;
                    break;
                case MonsterType.Boss:
                    parameters.preferredFormation = GroupFormation.Support;
                    parameters.coordinationWeight = 0.6f;
                    break;
            }
            
            return parameters;
        }
    }
}
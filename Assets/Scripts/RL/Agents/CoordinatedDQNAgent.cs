using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// DQN Learning Agent with coordination capabilities
    /// Extends DQNLearningAgent to support multi-monster coordination
    /// Implements Requirements 1.3, 2.5, 5.2
    /// </summary>
    public class CoordinatedDQNAgent : DQNLearningAgent, ICoordinatedLearningAgent, IGroupLearningAgent
    {
        [Header("Coordination Settings")]
        [SerializeField] private bool enableCoordination = true;
        [SerializeField] private float coordinationLearningRate = 0.001f;
        [SerializeField] private int coordinationBufferSize = 2000;
        
        // Coordination properties
        public CoordinationGroup CurrentGroup { get; set; }
        public bool CanCoordinate => enableCoordination && coordinationNetwork != null;
        
        // Local copy of monster type for coordination logic
        private MonsterType localMonsterType;
        
        // Local copy of action space for coordination logic
        private ActionSpace localActionSpace;
        
        // Coordination-specific hyperparameters
        [SerializeField] private float coordinationDiscountFactor = 0.99f;
        [SerializeField] private int coordinationBatchSize = 32;
        [SerializeField] private float coordinationExplorationRate = 0.1f;
        
        // Coordination learning components
        private INeuralNetwork coordinationNetwork;
        private ExperienceReplayBuffer coordinationBuffer;
        private CoordinationParameters coordinationParams;
        private CoordinationMetrics coordinationMetrics;
        private StateEncoder stateEncoder;
        
        // Coordination state tracking
        private Dictionary<GroupFormation, float> formationRewards;
        private List<GroupBehaviorPattern> learnedPatterns;
        private float lastCoordinationAction;
        
        protected void Awake()
        {
            InitializeCoordination();
        }

        private void InitializeCoordination()
        {
            // Initialize state encoder
            stateEncoder = new StateEncoder();
            
            formationRewards = new Dictionary<GroupFormation, float>();
            learnedPatterns = new List<GroupBehaviorPattern>();
            coordinationMetrics = CoordinationMetrics.CreateDefault();
            
            // Initialize formation preferences
            foreach (GroupFormation formation in System.Enum.GetValues(typeof(GroupFormation)))
            {
                formationRewards[formation] = 0f;
                coordinationMetrics.formationPreferences[formation] = 0f;
            }
        }

        public new void Initialize(MonsterType monsterType, ActionSpace actionSpace)
        {
            // Store monster type and action space locally for coordination logic
            localMonsterType = monsterType;
            localActionSpace = actionSpace;
            
            base.Initialize(monsterType, actionSpace);
            
            // Initialize coordination-specific components
            InitializeCoordinationNetwork();
            InitializeCoordinationBuffer();
            SetCoordinationParameters(CoordinationParameters.CreateDefault(monsterType));
            
            Debug.Log($"Coordinated DQN Agent initialized for {monsterType}");
        }

        private void InitializeCoordinationNetwork()
        {
            try
            {
                // Create coordination network with extended input size
                // Base state (32) + coordination context (16) = 48 inputs
                int coordinationInputSize = 48;
                int coordinationOutputSize = localActionSpace.actionCount + 6; // Base actions + coordination actions
                int[] hiddenLayers = new int[] { 64, 32, 16 };
                
                coordinationNetwork = new SimpleNeuralNetwork();
                coordinationNetwork.Initialize(
                    coordinationInputSize, 
                    coordinationOutputSize, 
                    hiddenLayers, 
                    NetworkArchitecture.Simple);
                
                Debug.Log($"Coordination network initialized: {coordinationInputSize} -> {coordinationOutputSize}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize coordination network: {ex.Message}");
                enableCoordination = false;
            }
        }

        private void InitializeCoordinationBuffer()
        {
            coordinationBuffer = new ExperienceReplayBuffer(coordinationBufferSize);
        }

        public int SelectCoordinatedAction(RLGameState state, CoordinationContext coordinationData, bool isTraining)
        {
            if (!CanCoordinate)
            {
                return SelectAction(state, isTraining);
            }

            try
            {
                // Encode state with coordination context
                float[] coordinationInput = EncodeCoordinationState(state, coordinationData);
                
                // Get Q-values from coordination network
                float[] qValues = coordinationNetwork.Forward(coordinationInput);
                
                // Select action using epsilon-greedy with coordination considerations
                int selectedAction = SelectCoordinatedActionFromQValues(qValues, isTraining, coordinationData);
                
                // Track coordination action
                lastCoordinationAction = Time.time;
                
                return selectedAction;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in coordinated action selection: {ex.Message}");
                return SelectAction(state, isTraining); // Fallback to base action selection
            }
        }

        private float[] EncodeCoordinationState(RLGameState state, CoordinationContext coordinationData)
        {
            // Base state encoding (32 dimensions)
            float[] baseState = stateEncoder.EncodeState(state);
            
            // Coordination context encoding (16 dimensions)
            float[] coordinationContext = new float[16];
            
            // Group information (4 dimensions)
            coordinationContext[0] = coordinationData.nearbyAgents.Count / 5f; // Normalized agent count
            coordinationContext[1] = coordinationData.groupCoordinationScore;
            coordinationContext[2] = Vector2.Distance(state.monsterPosition, coordinationData.groupCenter) / 20f; // Normalized distance
            coordinationContext[3] = (float)coordinationData.currentFormation / System.Enum.GetValues(typeof(GroupFormation)).Length;
            
            // Nearby agents information (12 dimensions - up to 3 agents, 4 dims each)
            for (int i = 0; i < 3 && i < coordinationData.nearbyAgents.Count; i++)
            {
                var agent = coordinationData.nearbyAgents[i];
                int baseIndex = 4 + i * 4;
                
                coordinationContext[baseIndex] = (agent.position.x - state.monsterPosition.x) / 20f; // Relative X
                coordinationContext[baseIndex + 1] = (agent.position.y - state.monsterPosition.y) / 20f; // Relative Y
                coordinationContext[baseIndex + 2] = agent.coordinationScore;
                coordinationContext[baseIndex + 3] = agent.isInSameGroup ? 1f : 0f;
            }
            
            // Combine base state and coordination context
            float[] fullState = new float[baseState.Length + coordinationContext.Length];
            System.Array.Copy(baseState, 0, fullState, 0, baseState.Length);
            System.Array.Copy(coordinationContext, 0, fullState, baseState.Length, coordinationContext.Length);
            
            return fullState;
        }

        private int SelectCoordinatedActionFromQValues(float[] qValues, bool isTraining, CoordinationContext coordinationData)
        {
            if (isTraining && UnityEngine.Random.Range(0f, 1f) < coordinationExplorationRate)
            {
                // Exploration: prefer coordination actions if in a group
                if (CurrentGroup != null && CurrentGroup.GroupSize > 1)
                {
                    // Higher chance of selecting coordination actions
                    int coordinationActionStart = localActionSpace.actionCount;
                    if (UnityEngine.Random.Range(0f, 1f) < coordinationParams.coordinationWeight)
                    {
                        return UnityEngine.Random.Range(coordinationActionStart, qValues.Length);
                    }
                }
                
                return UnityEngine.Random.Range(0, qValues.Length);
            }
            else
            {
                // Exploitation: select best action considering coordination
                return GetBestCoordinatedAction(qValues, coordinationData);
            }
        }

        private int GetBestCoordinatedAction(float[] qValues, CoordinationContext coordinationData)
        {
            // Weight Q-values based on coordination context
            float[] weightedQValues = new float[qValues.Length];
            
            for (int i = 0; i < qValues.Length; i++)
            {
                weightedQValues[i] = qValues[i];
                
                // Boost coordination actions if in a group
                if (i >= localActionSpace.actionCount && CurrentGroup != null)
                {
                    weightedQValues[i] += coordinationParams.coordinationWeight * coordinationData.groupCoordinationScore;
                }
                
                // Boost actions that align with suggested group action
                if (coordinationData.suggestedAction != CoordinationActionType.None)
                {
                    // Apply formation-specific bonuses
                    ApplyFormationBonus(ref weightedQValues[i], i, coordinationData.currentFormation);
                }
            }
            
            // Return action with highest weighted Q-value
            return System.Array.IndexOf(weightedQValues, weightedQValues.Max());
        }

        private void ApplyFormationBonus(ref float qValue, int actionIndex, GroupFormation formation)
        {
            // Apply learned formation preferences
            if (formationRewards.ContainsKey(formation))
            {
                float formationBonus = formationRewards[formation] * 0.1f;
                qValue += formationBonus;
            }
        }

        public void StoreCoordinationExperience(RLGameState state, int action, float coordinationReward, 
            RLGameState nextState, GroupActionOutcome groupOutcome)
        {
            if (!CanCoordinate) return;

            try
            {
                // Create coordination experience
                var experience = new CoordinationExperience
                {
                    state = state,
                    action = action,
                    reward = coordinationReward,
                    nextState = nextState,
                    done = false, // Coordination experiences are typically not terminal
                    groupOutcome = groupOutcome,
                    timestamp = Time.time
                };
                
                // Store in coordination buffer
                coordinationBuffer.Add(experience.ToExperience());
                
                // Update coordination metrics
                UpdateCoordinationMetrics(groupOutcome, coordinationReward);
                
                // Learn from group patterns
                if (groupOutcome.groupActionSucceeded)
                {
                    LearnFromSuccessfulCoordination(state, action, groupOutcome);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error storing coordination experience: {ex.Message}");
            }
        }

        private void UpdateCoordinationMetrics(GroupActionOutcome outcome, float reward)
        {
            coordinationMetrics.groupActionsPerformed++;
            
            if (outcome.groupActionSucceeded)
            {
                coordinationMetrics.coordinationSuccessRate = 
                    (coordinationMetrics.coordinationSuccessRate * (coordinationMetrics.groupActionsPerformed - 1) + 1f) / 
                    coordinationMetrics.groupActionsPerformed;
            }
            else
            {
                coordinationMetrics.coordinationSuccessRate = 
                    (coordinationMetrics.coordinationSuccessRate * (coordinationMetrics.groupActionsPerformed - 1) + 0f) / 
                    coordinationMetrics.groupActionsPerformed;
            }
            
            coordinationMetrics.averageGroupReward = 
                (coordinationMetrics.averageGroupReward * (coordinationMetrics.groupActionsPerformed - 1) + reward) / 
                coordinationMetrics.groupActionsPerformed;
            
            coordinationMetrics.synchronizationAccuracy = outcome.synchronizationScore;
            coordinationMetrics.formationAdherence = outcome.formationMaintained ? 1f : 0f;
        }

        private void LearnFromSuccessfulCoordination(RLGameState state, int action, GroupActionOutcome outcome)
        {
            if (CurrentGroup == null) return;
            
            // Update formation preferences
            var formation = MapStrategyToFormation(CurrentGroup.coordinationStrategy);
            if (formationRewards.ContainsKey(formation))
            {
                formationRewards[formation] = Mathf.Lerp(formationRewards[formation], outcome.groupReward, 0.1f);
                coordinationMetrics.formationPreferences[formation] = formationRewards[formation];
            }
            
            // Create or update behavior pattern
            UpdateBehaviorPattern(state, action, outcome);
        }

        private void UpdateBehaviorPattern(RLGameState state, int action, GroupActionOutcome outcome)
        {
            // Find or create behavior pattern
            var pattern = learnedPatterns.FirstOrDefault(p => p.formation == MapStrategyToFormation(CurrentGroup.coordinationStrategy));
            
            if (pattern == null)
            {
                pattern = new GroupBehaviorPattern
                {
                    patternId = System.Guid.NewGuid().ToString(),
                    formation = MapStrategyToFormation(CurrentGroup.coordinationStrategy)
                };
                learnedPatterns.Add(pattern);
            }
            
            // Record success by updating pattern statistics
            pattern.timesUsed++;
            pattern.lastUsed = DateTime.Now;
            
            // Update success rate using exponential moving average
            float alpha = 0.1f; // Learning rate for success rate
            float currentSuccess = outcome.groupReward > 0 ? 1f : 0f;
            pattern.successRate = pattern.successRate * (1f - alpha) + currentSuccess * alpha;
        }

        public void UpdateCoordinatedPolicy(GroupLearningFeedback groupFeedback)
        {
            if (!CanCoordinate) return;

            try
            {
                // Update base policy
                UpdatePolicy();
                
                // Update coordination network if we have enough experiences
                if (coordinationBuffer.Count >= coordinationBatchSize)
                {
                    UpdateCoordinationNetwork();
                }
                
                // Apply group feedback
                ApplyGroupFeedback(groupFeedback);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error updating coordinated policy: {ex.Message}");
            }
        }

        private void UpdateCoordinationNetwork()
        {
            if (coordinationBuffer.Count < coordinationBatchSize) return;
            
            // Sample batch from coordination buffer
            var batch = coordinationBuffer.SampleBatch(coordinationBatchSize);
            
            // Prepare training data
            float[][] inputs = new float[batch.Length][];
            float[][] targets = new float[batch.Length][];
            
            for (int i = 0; i < batch.Length; i++)
            {
                var experience = batch[i];
                
                // Create coordination context for this experience
                var coordinationContext = CoordinationContext.CreateEmpty(); // Simplified for now
                
                inputs[i] = EncodeCoordinationState(experience.state, coordinationContext);
                
                // Calculate target Q-values
                float[] currentQValues = coordinationNetwork.Forward(inputs[i]);
                float[] nextQValues = coordinationNetwork.Forward(
                    EncodeCoordinationState(experience.nextState, coordinationContext));
                
                targets[i] = (float[])currentQValues.Clone();
                
                float targetValue = experience.reward;
                if (!experience.done)
                {
                    targetValue += coordinationDiscountFactor * nextQValues.Max();
                }
                
                targets[i][experience.action] = targetValue;
            }
            
            // Train coordination network using backward pass
            for (int i = 0; i < inputs.Length; i++)
            {
                coordinationNetwork.Backward(inputs[i], targets[i], coordinationLearningRate);
            }
        }

        private void ApplyGroupFeedback(GroupLearningFeedback feedback)
        {
            // Update formation effectiveness based on group feedback
            foreach (var kvp in feedback.formationEffectiveness)
            {
                if (System.Enum.TryParse<GroupFormation>(kvp.Key, out GroupFormation formation))
                {
                    if (formationRewards.ContainsKey(formation))
                    {
                        formationRewards[formation] = Mathf.Lerp(formationRewards[formation], kvp.Value, 0.2f);
                        coordinationMetrics.formationPreferences[formation] = formationRewards[formation];
                    }
                }
            }
            
            // Adjust coordination weight based on group performance
            if (feedback.groupPerformance > 0.7f)
            {
                coordinationParams.coordinationWeight = Mathf.Min(1f, coordinationParams.coordinationWeight * 1.05f);
            }
            else if (feedback.groupPerformance < 0.3f)
            {
                coordinationParams.coordinationWeight = Mathf.Max(0.1f, coordinationParams.coordinationWeight * 0.95f);
            }
        }

        public CoordinationMetrics GetCoordinationMetrics()
        {
            return coordinationMetrics;
        }

        public void SetCoordinationParameters(CoordinationParameters parameters)
        {
            coordinationParams = parameters;
            
            // Apply parameters to coordination behavior
            coordinationLearningRate = Mathf.Clamp(coordinationLearningRate * parameters.coordinationWeight, 0.0001f, 0.01f);
            
            Debug.Log($"Coordination parameters set for {localMonsterType}: weight={parameters.coordinationWeight}, formation={parameters.preferredFormation}");
        }

        protected void OnDestroy()
        {
            // Clean up coordination resources
            coordinationNetwork = null; // Release reference
            coordinationBuffer?.Clear();
        }

        private GroupFormation MapStrategyToFormation(CoordinationStrategy strategy)
        {
            // Map coordination strategy to group formation
            switch (strategy)
            {
                case CoordinationStrategy.Basic:
                    return GroupFormation.Cluster;
                case CoordinationStrategy.Flank:
                    return GroupFormation.Arc;
                case CoordinationStrategy.Surround:
                    return GroupFormation.Surround;
                case CoordinationStrategy.CrossFire:
                    return GroupFormation.Line;
                case CoordinationStrategy.SequentialAttack:
                    return GroupFormation.Wedge;
                case CoordinationStrategy.ZoneControl:
                    return GroupFormation.Scatter;
                case CoordinationStrategy.Overwhelm:
                    return GroupFormation.Diamond;
                default:
                    return GroupFormation.Cluster;
            }
        }

        // Implementation of ICoordinatedLearningAgent methods from MultiMonsterLearningManager
        public void StartGroupLearning(GroupLearningSession session)
        {
            if (!enableCoordination || !CanCoordinate)
                return;

            Debug.Log($"Starting group learning session: {session.sessionId}");
            
            // Initialize group learning state
            // This could involve setting up shared learning parameters
        }

        public void UpdateGroupLearning(GroupLearningSession session)
        {
            if (!enableCoordination || !CanCoordinate)
                return;

            // Update learning based on group session progress
            // This could involve adjusting learning rates or sharing experiences
        }

        public void CompleteGroupLearning(GroupLearningSession session)
        {
            if (!enableCoordination || !CanCoordinate)
                return;

            Debug.Log($"Completing group learning session: {session.sessionId}");
            
            // Finalize group learning
            // This could involve consolidating learned behaviors or updating group policies
        }
    }

    /// <summary>
    /// Extended experience for coordination learning
    /// </summary>
    public struct CoordinationExperience
    {
        public RLGameState state;
        public int action;
        public float reward;
        public RLGameState nextState;
        public bool done;
        public GroupActionOutcome groupOutcome;
        public float timestamp;
        
        public Experience ToExperience()
        {
            return new Experience
            {
                state = state,
                action = action,
                reward = reward,
                nextState = nextState,
                done = done
            };
        }
    }
}
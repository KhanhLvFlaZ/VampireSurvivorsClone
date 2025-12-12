using UnityEngine;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Fallback learning agent that provides basic scripted behavior
    /// Used when main RL agents fail to initialize or become corrupted
    /// Implements Requirements 3.5, 4.3 - fallback to default behavior
    /// </summary>
    public class FallbackLearningAgent : MonoBehaviour, ILearningAgent
    {
        [Header("Fallback Agent Settings")]
        [SerializeField] private MonsterType monsterType;
        [SerializeField] private bool isTraining = false; // Fallback agents don't train
        
        private ActionSpace actionSpace;
        private LearningMetrics metrics;
        private float lastActionTime;
        private int lastSelectedAction;
        
        // Simple behavior parameters
        private float aggressionLevel = 0.5f;
        private float cautionLevel = 0.3f;
        private float randomnessLevel = 0.2f;
        
        public bool IsTraining 
        { 
            get => false; // Fallback agents never train
            set { } // Ignore training mode changes
        }

        public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
        {
            this.monsterType = monsterType;
            this.actionSpace = actionSpace;
            this.metrics = LearningMetrics.CreateDefault();
            this.lastActionTime = Time.time;
            this.lastSelectedAction = 0;
            
            // Set behavior parameters based on monster type
            ConfigureBehaviorForMonsterType(monsterType);
            
            Debug.Log($"[FALLBACK] Initialized fallback agent for {monsterType}");
        }

        public int SelectAction(RLGameState state, bool isTraining)
        {
            // Simple rule-based action selection
            try
            {
                int action = SelectActionBasedOnRules(state);
                lastSelectedAction = action;
                lastActionTime = Time.time;
                
                // Update basic metrics
                metrics.totalSteps++;
                
                return action;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("FallbackLearningAgent", "SelectAction", ex, $"MonsterType: {monsterType}");
                
                // Return safe default action (usually "do nothing" or basic movement)
                return 0;
            }
        }

        public void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done)
        {
            // Fallback agents don't learn from experience
            // But we can track basic statistics
            if (done)
            {
                metrics.episodeCount++;
                
                // Simple reward tracking
                if (reward > 0)
                {
                    metrics.averageReward = (metrics.averageReward * 0.9f) + (reward * 0.1f);
                }
            }
        }

        public void UpdatePolicy()
        {
            // Fallback agents don't update their policy
            // But we can adjust behavior parameters slightly over time
            try
            {
                AdaptBehaviorParameters();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("FallbackLearningAgent", "UpdatePolicy", ex);
            }
        }

        public void SaveBehaviorProfile(string filePath)
        {
            try
            {
                // Create a simple profile representing the fallback behavior
                var profile = BehaviorProfile.Create(monsterType, "fallback", NetworkArchitecture.Simple);
                profile.networkWeights = new float[] { aggressionLevel, cautionLevel, randomnessLevel };
                profile.networkBiases = new float[0];
                profile.metrics = metrics;
                profile.playerProfileId = "fallback";
                
                string json = JsonUtility.ToJson(profile, true);
                System.IO.File.WriteAllText(filePath, json);
                
                Debug.Log($"[FALLBACK] Saved fallback profile for {monsterType}");
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("FallbackLearningAgent", "SaveBehaviorProfile", ex, filePath);
            }
        }

        public void LoadBehaviorProfile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    var profile = JsonUtility.FromJson<BehaviorProfile>(json);
                    
                    if (profile != null && profile.networkWeights != null && profile.networkWeights.Length >= 3)
                    {
                        aggressionLevel = Mathf.Clamp01(profile.networkWeights[0]);
                        cautionLevel = Mathf.Clamp01(profile.networkWeights[1]);
                        randomnessLevel = Mathf.Clamp01(profile.networkWeights[2]);
                        
                        metrics = profile.metrics;
                        
                        Debug.Log($"[FALLBACK] Loaded fallback profile for {monsterType}");
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("FallbackLearningAgent", "LoadBehaviorProfile", ex, filePath);
                // Continue with default parameters
            }
        }

        public LearningMetrics GetMetrics()
        {
            return metrics;
        }

        private void ConfigureBehaviorForMonsterType(MonsterType monsterType)
        {
            // Configure behavior parameters based on monster type
            switch (monsterType)
            {
                case MonsterType.Melee:
                    aggressionLevel = 0.8f;
                    cautionLevel = 0.2f;
                    randomnessLevel = 0.1f;
                    break;
                    
                case MonsterType.Ranged:
                    aggressionLevel = 0.6f;
                    cautionLevel = 0.4f;
                    randomnessLevel = 0.2f;
                    break;
                    
                case MonsterType.Throwing:
                    aggressionLevel = 0.7f;
                    cautionLevel = 0.3f;
                    randomnessLevel = 0.2f;
                    break;
                    
                case MonsterType.Boomerang:
                    aggressionLevel = 0.5f;
                    cautionLevel = 0.5f;
                    randomnessLevel = 0.3f;
                    break;
                    
                case MonsterType.Boss:
                    aggressionLevel = 0.9f;
                    cautionLevel = 0.1f;
                    randomnessLevel = 0.1f;
                    break;
                    
                default:
                    aggressionLevel = 0.5f;
                    cautionLevel = 0.3f;
                    randomnessLevel = 0.2f;
                    break;
            }
        }

        private int SelectActionBasedOnRules(RLGameState state)
        {
            float distanceToPlayer = state.DistanceToPlayer;
            float playerHealth = state.playerHealth;
            float monsterHealth = state.monsterHealth;
            
            // Calculate action probabilities based on simple rules
            float[] actionProbabilities = new float[actionSpace.GetTotalActionCount()];
            
            // Movement actions (0-8: 8 directions + stop)
            if (distanceToPlayer > 5f)
            {
                // Move towards player when far away
                Vector2 direction = (state.playerPosition - state.monsterPosition).normalized;
                int bestMoveAction = GetBestMovementAction(direction);
                actionProbabilities[bestMoveAction] = aggressionLevel;
            }
            else if (distanceToPlayer < 2f && monsterHealth < 50f)
            {
                // Retreat when close and low health
                Vector2 direction = (state.monsterPosition - state.playerPosition).normalized;
                int retreatAction = GetBestMovementAction(direction);
                actionProbabilities[retreatAction] = cautionLevel;
            }
            else
            {
                // Circle around player at medium distance
                Vector2 perpendicular = Vector2.Perpendicular((state.playerPosition - state.monsterPosition).normalized);
                int circleAction = GetBestMovementAction(perpendicular);
                actionProbabilities[circleAction] = 0.5f;
            }
            
            // Attack actions (9-11: primary, special, defensive)
            if (distanceToPlayer < 3f)
            {
                actionProbabilities[9] = aggressionLevel * 0.8f; // Primary attack
                actionProbabilities[10] = aggressionLevel * 0.3f; // Special attack
            }
            
            if (playerHealth > 80f && monsterHealth < 30f)
            {
                actionProbabilities[11] = cautionLevel; // Defensive stance
            }
            
            // Tactical actions (12-14: retreat, coordinate, ambush)
            if (monsterHealth < 25f)
            {
                actionProbabilities[12] = cautionLevel * 1.2f; // Retreat
            }
            
            // Add some randomness
            for (int i = 0; i < actionProbabilities.Length; i++)
            {
                actionProbabilities[i] += UnityEngine.Random.Range(0f, randomnessLevel);
            }
            
            // Select action with highest probability
            int bestAction = 0;
            float bestProbability = actionProbabilities[0];
            
            for (int i = 1; i < actionProbabilities.Length; i++)
            {
                if (actionProbabilities[i] > bestProbability)
                {
                    bestProbability = actionProbabilities[i];
                    bestAction = i;
                }
            }
            
            return bestAction;
        }

        private int GetBestMovementAction(Vector2 direction)
        {
            // Convert direction to one of 8 movement actions (0-7)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            
            // Map angle to 8 directions
            int directionIndex = Mathf.RoundToInt(angle / 45f) % 8;
            return directionIndex;
        }

        private void AdaptBehaviorParameters()
        {
            // Slightly adjust behavior parameters over time based on performance
            if (metrics.averageReward > 10f)
            {
                // Increase aggression if doing well
                aggressionLevel = Mathf.Min(1f, aggressionLevel + 0.001f);
            }
            else if (metrics.averageReward < -10f)
            {
                // Increase caution if doing poorly
                cautionLevel = Mathf.Min(1f, cautionLevel + 0.001f);
                aggressionLevel = Mathf.Max(0f, aggressionLevel - 0.001f);
            }
            
            // Gradually reduce randomness over time (simulating "learning")
            randomnessLevel = Mathf.Max(0.05f, randomnessLevel - 0.0001f);
        }
    }
}
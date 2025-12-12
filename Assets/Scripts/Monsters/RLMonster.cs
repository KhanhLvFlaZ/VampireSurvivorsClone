using UnityEngine;
using System.Collections;
using Vampire.RL;

namespace Vampire
{
    /// <summary>
    /// Monster component that extends base Monster class with reinforcement learning capabilities
    /// Integrates LearningAgent into monster behavior for adaptive AI
    /// </summary>
    [RequireComponent(typeof(DQNLearningAgent))]
    public class RLMonster : Monster
    {
        [Header("RL Configuration")]
        [SerializeField] private MonsterRLConfig rlConfig;
        [SerializeField] private bool enableRL = true;
        [SerializeField] private bool isTrainingMode = true;
        
        [Header("RL State")]
        [SerializeField] private MonsterType monsterType = MonsterType.Melee;
        [SerializeField] private float actionInterval = 0.2f;
        [SerializeField] private float lastActionTime = 0f;
        
        // RL Components
        private ILearningAgent learningAgent;
        private IRewardCalculator rewardCalculator;
        private RLGameState previousState;
        private RLGameState currentState;
        private MonsterAction lastAction;
        private ActionOutcome lastActionOutcome;
        
        // State tracking
        private float episodeStartTime;
        private float totalEpisodeReward;
        private bool episodeActive = false;
        
        // Action execution
        private Vector2 currentMovementDirection;
        private bool isExecutingAction = false;
        private float actionExecutionTime = 0f;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Get or add learning agent component
            learningAgent = GetComponent<DQNLearningAgent>();
            if (learningAgent == null)
            {
                learningAgent = gameObject.AddComponent<DQNLearningAgent>();
            }
        }

        public override void Init(EntityManager entityManager, Character playerCharacter)
        {
            base.Init(entityManager, playerCharacter);
            
            // Initialize RL components if enabled
            if (enableRL)
            {
                InitializeRLSystem();
            }
        }

        public override void Setup(int monsterIndex, Vector2 position, MonsterBlueprint monsterBlueprint, float hpBuff = 0)
        {
            base.Setup(monsterIndex, position, monsterBlueprint, hpBuff);
            
            if (enableRL)
            {
                StartNewEpisode();
            }
        }

        protected override void Update()
        {
            base.Update();
            
            if (enableRL && alive && episodeActive)
            {
                UpdateRLBehavior();
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            
            if (enableRL && alive && episodeActive)
            {
                ExecuteCurrentAction();
            }
        }

        /// <summary>
        /// Initialize the reinforcement learning system
        /// </summary>
        private void InitializeRLSystem()
        {
            // Load or create RL configuration
            if (rlConfig == null)
            {
                rlConfig = MonsterRLConfig.CreateDefault(monsterType);
            }
            
            // Initialize learning agent
            learningAgent.Initialize(monsterType, rlConfig.actionSpace);
            learningAgent.IsTraining = isTrainingMode;
            
            // Initialize reward calculator
            var rewardConfig = RewardConfig.GetConfigForMonsterType(monsterType);
            rewardCalculator = new RewardCalculator(rewardConfig, rlConfig);
            
            // Initialize state
            currentState = RLGameState.CreateDefault();
            previousState = currentState;
            lastAction = MonsterAction.CreateWait();
            lastActionOutcome = ActionOutcome.CreateDefault();
            
            Debug.Log($"RL System initialized for {monsterType} monster");
        }

        /// <summary>
        /// Start a new learning episode
        /// </summary>
        private void StartNewEpisode()
        {
            episodeStartTime = Time.time;
            totalEpisodeReward = 0f;
            episodeActive = true;
            lastActionTime = 0f;
            
            // Reset action state
            currentMovementDirection = Vector2.zero;
            isExecutingAction = false;
            actionExecutionTime = 0f;
            
            // Build initial game state
            UpdateGameState();
            previousState = currentState;
            
            Debug.Log($"Started new RL episode for {monsterType} monster");
        }

        /// <summary>
        /// Update RL behavior - main decision making loop
        /// </summary>
        private void UpdateRLBehavior()
        {
            // Update game state
            UpdateGameState();
            
            // Calculate reward for previous action
            if (Time.time > episodeStartTime + 0.1f) // Skip first frame
            {
                float reward = rewardCalculator.CalculateReward(previousState, lastAction, currentState, lastActionOutcome);
                totalEpisodeReward += reward;
                
                // Store experience for learning
                if (learningAgent.IsTraining)
                {
                    learningAgent.StoreExperience(previousState, GetActionIndex(lastAction), reward, currentState, !alive);
                }
            }
            
            // Select and execute new action if enough time has passed
            if (Time.time - lastActionTime >= actionInterval)
            {
                SelectAndExecuteAction();
                lastActionTime = Time.time;
            }
            
            // Update previous state
            previousState = currentState;
            
            // Update learning policy periodically
            if (learningAgent.IsTraining && Time.fixedTime % 0.5f < Time.fixedDeltaTime)
            {
                learningAgent.UpdatePolicy();
            }
        }

        /// <summary>
        /// Select and execute the next action using the learning agent
        /// </summary>
        private void SelectAndExecuteAction()
        {
            // Get action from learning agent
            int actionIndex = learningAgent.SelectAction(currentState, learningAgent.IsTraining);
            
            // Convert action index to MonsterAction
            lastAction = ConvertIndexToAction(actionIndex);
            
            // Reset action outcome
            lastActionOutcome = ActionOutcome.CreateDefault();
            
            // Start executing the action
            StartActionExecution(lastAction);
        }

        /// <summary>
        /// Start executing a specific action
        /// </summary>
        private void StartActionExecution(MonsterAction action)
        {
            isExecutingAction = true;
            actionExecutionTime = 0f;
            
            switch (action.actionType)
            {
                case ActionType.Move:
                    currentMovementDirection = action.direction;
                    break;
                    
                case ActionType.Attack:
                    ExecuteAttack(action.intensity);
                    break;
                    
                case ActionType.Retreat:
                    currentMovementDirection = action.direction;
                    break;
                    
                case ActionType.Wait:
                    currentMovementDirection = Vector2.zero;
                    break;
                    
                case ActionType.SpecialAttack:
                    ExecuteSpecialAttack(action.intensity);
                    break;
                    
                case ActionType.DefensiveStance:
                    ExecuteDefensiveStance();
                    break;
                    
                case ActionType.Coordinate:
                    ExecuteCoordination(action.targetIndex);
                    break;
                    
                case ActionType.Ambush:
                    ExecuteAmbush();
                    break;
            }
        }

        /// <summary>
        /// Execute the current action (called in FixedUpdate)
        /// </summary>
        private void ExecuteCurrentAction()
        {
            if (!isExecutingAction) return;
            
            actionExecutionTime += Time.fixedDeltaTime;
            
            // Execute movement if there's a movement direction
            if (currentMovementDirection != Vector2.zero)
            {
                ExecuteMovement(currentMovementDirection);
            }
            
            // Stop action execution after a certain time
            if (actionExecutionTime >= actionInterval)
            {
                isExecutingAction = false;
                currentMovementDirection = Vector2.zero;
            }
        }

        /// <summary>
        /// Execute movement in the specified direction
        /// </summary>
        private void ExecuteMovement(Vector2 direction)
        {
            if (knockedBack) return;
            
            // Apply movement force
            Vector2 targetVelocity = direction * monsterBlueprint.movespeed;
            Vector2 velocityChange = targetVelocity - rb.velocity;
            Vector2 force = velocityChange * monsterBlueprint.acceleration;
            
            rb.AddForce(force, ForceMode2D.Force);
            
            // Update action outcome
            lastActionOutcome.distanceToPlayer = Vector2.Distance(transform.position, playerCharacter.transform.position);
        }

        /// <summary>
        /// Execute attack action
        /// </summary>
        private void ExecuteAttack(float intensity)
        {
            // Check if player is in range
            float distanceToPlayer = Vector2.Distance(transform.position, playerCharacter.transform.position);
            float attackRange = monsterBlueprint.movespeed * 0.5f; // Basic attack range
            
            if (distanceToPlayer <= attackRange)
            {
                // Deal damage to player
                float damage = monsterBlueprint.atk * intensity;
                Vector2 knockback = (playerCharacter.transform.position - transform.position).normalized * 2f;
                
                // This would need to be implemented based on the actual damage system
                // For now, just update the action outcome
                lastActionOutcome.hitPlayer = true;
                lastActionOutcome.damageDealt = damage;
                
                Debug.Log($"RL Monster attacked player for {damage} damage");
            }
        }

        /// <summary>
        /// Execute special attack action
        /// </summary>
        private void ExecuteSpecialAttack(float intensity)
        {
            // Placeholder for special attack implementation
            // This would depend on the specific monster type and abilities
            ExecuteAttack(intensity * 1.5f); // More powerful attack
        }

        /// <summary>
        /// Execute defensive stance
        /// </summary>
        private void ExecuteDefensiveStance()
        {
            // Reduce movement and increase defense
            currentMovementDirection = Vector2.zero;
            // This would need integration with the actual defense system
        }

        /// <summary>
        /// Execute coordination with other monsters
        /// </summary>
        private void ExecuteCoordination(int targetIndex)
        {
            // Find nearby monsters and coordinate
            var nearbyMonsters = entityManager.LivingMonsters;
            if (targetIndex >= 0 && targetIndex < nearbyMonsters.Count)
            {
                // Coordinate with specific monster
                lastActionOutcome.coordinated = true;
            }
        }

        /// <summary>
        /// Execute ambush action
        /// </summary>
        private void ExecuteAmbush()
        {
            // Wait for optimal moment then attack
            if (currentState.IsPlayerApproaching)
            {
                ExecuteAttack(1.5f); // Surprise attack bonus
            }
        }

        /// <summary>
        /// Update the current game state
        /// </summary>
        private void UpdateGameState()
        {
            currentState = GameStateBuilder.BuildGameState(
                this, 
                playerCharacter, 
                entityManager,
                maxNearbyMonsters: 5,
                maxNearbyCollectibles: 10,
                searchRadius: 15f
            );
        }

        /// <summary>
        /// Convert action index to MonsterAction
        /// </summary>
        private MonsterAction ConvertIndexToAction(int actionIndex)
        {
            // This is a simplified conversion - would need to match the action space configuration
            var availableActions = rlConfig.actionSpace.GetAvailableActions();
            
            if (actionIndex < 0 || actionIndex >= availableActions.Count)
            {
                return MonsterAction.CreateWait();
            }
            
            var actionType = availableActions[actionIndex % availableActions.Count];
            
            switch (actionType)
            {
                case ActionType.Move:
                    // Convert to 8-directional movement
                    int dirIndex = actionIndex % 8;
                    float angle = dirIndex * 45f * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    return MonsterAction.CreateMovement(direction);
                    
                case ActionType.Attack:
                    return MonsterAction.CreateAttack();
                    
                case ActionType.Retreat:
                    // Retreat away from player
                    Vector2 retreatDir = (transform.position - playerCharacter.transform.position).normalized;
                    return MonsterAction.CreateRetreat(retreatDir);
                    
                case ActionType.Coordinate:
                    return MonsterAction.CreateCoordinate(0);
                    
                default:
                    return MonsterAction.CreateWait();
            }
        }

        /// <summary>
        /// Convert MonsterAction to action index
        /// </summary>
        private int GetActionIndex(MonsterAction action)
        {
            // Simplified conversion - would need to match the action space configuration
            switch (action.actionType)
            {
                case ActionType.Move:
                    return 0;
                case ActionType.Attack:
                    return 1;
                case ActionType.Retreat:
                    return 2;
                case ActionType.Coordinate:
                    return 3;
                case ActionType.Wait:
                    return 4;
                default:
                    return 4; // Default to wait
            }
        }

        /// <summary>
        /// Handle monster death - end episode and calculate terminal reward
        /// </summary>
        public override IEnumerator Killed(bool killedByPlayer = true)
        {
            if (enableRL && episodeActive)
            {
                EndEpisode(killedByPlayer);
            }
            
            return base.Killed(killedByPlayer);
        }

        /// <summary>
        /// End the current learning episode
        /// </summary>
        private void EndEpisode(bool killedByPlayer)
        {
            if (!episodeActive) return;
            
            episodeActive = false;
            float episodeLength = Time.time - episodeStartTime;
            
            // Calculate terminal reward
            float terminalReward = rewardCalculator.CalculateTerminalReward(currentState, episodeLength, killedByPlayer);
            totalEpisodeReward += terminalReward;
            
            // Store final experience
            if (learningAgent.IsTraining)
            {
                learningAgent.StoreExperience(previousState, GetActionIndex(lastAction), terminalReward, currentState, true);
                learningAgent.UpdatePolicy();
            }
            
            Debug.Log($"RL Episode ended for {monsterType}: Length={episodeLength:F1}s, Total Reward={totalEpisodeReward:F1}");
        }

        /// <summary>
        /// Override TakeDamage to track damage for reward calculation
        /// </summary>
        public override void TakeDamage(float damage, Vector2 knockback = default(Vector2))
        {
            if (enableRL)
            {
                lastActionOutcome.tookDamage = true;
                lastActionOutcome.damageTaken += damage;
            }
            
            base.TakeDamage(damage, knockback);
        }

        /// <summary>
        /// Enable or disable RL behavior
        /// </summary>
        public void SetRLEnabled(bool enabled)
        {
            enableRL = enabled;
            if (learningAgent != null)
            {
                learningAgent.IsTraining = enabled && isTrainingMode;
            }
        }

        /// <summary>
        /// Set training mode
        /// </summary>
        public void SetTrainingMode(bool training)
        {
            isTrainingMode = training;
            if (learningAgent != null)
            {
                learningAgent.IsTraining = training && enableRL;
            }
        }

        /// <summary>
        /// Get current learning metrics
        /// </summary>
        public LearningMetrics GetLearningMetrics()
        {
            return learningAgent?.GetMetrics() ?? LearningMetrics.CreateDefault();
        }

        /// <summary>
        /// Save behavior profile
        /// </summary>
        public void SaveBehaviorProfile(string filePath)
        {
            learningAgent?.SaveBehaviorProfile(filePath);
        }

        /// <summary>
        /// Load behavior profile
        /// </summary>
        public void LoadBehaviorProfile(string filePath)
        {
            learningAgent?.LoadBehaviorProfile(filePath);
        }

        /// <summary>
        /// Get current RL status for debugging
        /// </summary>
        public string GetRLStatus()
        {
            if (!enableRL) return "RL Disabled";
            
            var metrics = GetLearningMetrics();
            return $"Episodes: {metrics.episodeCount}, Avg Reward: {metrics.averageReward:F1}, " +
                   $"Exploration: {metrics.explorationRate:F2}, Training: {learningAgent.IsTraining}";
        }

        /// <summary>
        /// Force start a new episode (for testing)
        /// </summary>
        public void ForceStartNewEpisode()
        {
            if (enableRL)
            {
                StartNewEpisode();
            }
        }
    }
}
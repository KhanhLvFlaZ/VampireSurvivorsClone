using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Demo script showing how to use the RewardCalculator system
    /// </summary>
    public class RewardCalculatorDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private MonsterType demoMonsterType = MonsterType.Melee;
        [SerializeField] private bool runDemoOnStart = false;
        [SerializeField] private bool showDetailedOutput = true;

        void Start()
        {
            if (runDemoOnStart)
            {
                RunRewardCalculatorDemo();
            }
        }

        [ContextMenu("Run Reward Calculator Demo")]
        public void RunRewardCalculatorDemo()
        {
            Debug.Log("=== Reward Calculator Demo Started ===");
            
            // Demo 1: Basic Usage
            DemoBasicUsage();
            
            // Demo 2: Different Monster Types
            DemoDifferentMonsterTypes();
            
            // Demo 3: Reward Shaping
            DemoRewardShaping();
            
            // Demo 4: Specialized Calculators
            DemoSpecializedCalculators();
            
            Debug.Log("=== Reward Calculator Demo Completed ===");
        }

        private void DemoBasicUsage()
        {
            Debug.Log("--- Demo 1: Basic Usage ---");
            
            // Get a reward calculator for melee monsters
            var calculator = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);
            
            // Create a game scenario
            var previousState = RLGameState.CreateDefault();
            previousState.playerPosition = new Vector2(0f, 0f);
            previousState.playerHealth = 100f;
            previousState.monsterPosition = new Vector2(3f, 0f);
            previousState.monsterHealth = 100f;
            
            var currentState = previousState;
            currentState.monsterPosition = new Vector2(2f, 0f); // Monster moved closer
            
            // Test different action outcomes
            var hitOutcome = new ActionOutcome
            {
                hitPlayer = true,
                damageDealt = 25f,
                tookDamage = false,
                coordinated = false,
                distanceToPlayer = 2f
            };
            
            var missOutcome = new ActionOutcome
            {
                hitPlayer = false,
                damageDealt = 0f,
                tookDamage = true,
                damageTaken = 10f,
                coordinated = false,
                distanceToPlayer = 2f
            };
            
            var attackAction = MonsterAction.CreateAttack();
            
            float hitReward = calculator.CalculateReward(previousState, attackAction, currentState, hitOutcome);
            float missReward = calculator.CalculateReward(previousState, attackAction, currentState, missOutcome);
            
            Debug.Log($"Attack Hit Reward: {hitReward:F2}");
            Debug.Log($"Attack Miss Reward: {missReward:F2}");
            
            // Test terminal rewards
            var deathState = currentState;
            deathState.monsterHealth = 0f;
            float deathReward = calculator.CalculateTerminalReward(deathState, 15f, true);
            
            var victoryState = currentState;
            victoryState.playerHealth = 0f;
            float victoryReward = calculator.CalculateTerminalReward(victoryState, 20f, false);
            
            Debug.Log($"Death Penalty: {deathReward:F2}");
            Debug.Log($"Victory Reward: {victoryReward:F2}");
        }

        private void DemoDifferentMonsterTypes()
        {
            Debug.Log("--- Demo 2: Different Monster Types ---");
            
            var monsterTypes = new MonsterType[] { MonsterType.Melee, MonsterType.Ranged, MonsterType.Throwing, MonsterType.Boss };
            
            var gameState = RLGameState.CreateDefault();
            var action = MonsterAction.CreateAttack();
            var outcome = new ActionOutcome
            {
                hitPlayer = true,
                damageDealt = 20f,
                tookDamage = false,
                coordinated = false
            };
            
            foreach (var monsterType in monsterTypes)
            {
                var calculator = RewardCalculatorFactory.GetRewardCalculator(monsterType);
                float reward = calculator.CalculateReward(gameState, action, gameState, outcome);
                
                Debug.Log($"{monsterType} Monster Hit Reward: {reward:F2}");
            }
        }

        private void DemoRewardShaping()
        {
            Debug.Log("--- Demo 3: Reward Shaping ---");
            
            // Create configs with and without shaping
            var shapedConfig = RewardConfig.CreateDefault();
            shapedConfig.rewardFunctionType = RewardFunctionType.Shaped;
            shapedConfig.optimalDistance = 3f;
            shapedConfig.optimalDistanceReward = 2f;
            
            var denseConfig = RewardConfig.CreateDefault();
            denseConfig.rewardFunctionType = RewardFunctionType.Dense;
            
            var monsterConfig = MonsterRLConfig.CreateDefault(MonsterType.Melee);
            
            var shapedCalculator = new RewardCalculator(shapedConfig, monsterConfig);
            var denseCalculator = new RewardCalculator(denseConfig, monsterConfig);
            
            // Test at different distances
            var distances = new float[] { 1f, 3f, 5f, 8f };
            
            foreach (var distance in distances)
            {
                var state = RLGameState.CreateDefault();
                state.playerPosition = Vector2.zero;
                state.monsterPosition = new Vector2(distance, 0f);
                
                float baseReward = 10f;
                float shapedReward = shapedCalculator.ShapeReward(baseReward, state);
                float denseReward = denseCalculator.ShapeReward(baseReward, state);
                
                Debug.Log($"Distance {distance:F1}m - Shaped: {shapedReward:F2}, Dense: {denseReward:F2}");
            }
        }

        private void DemoSpecializedCalculators()
        {
            Debug.Log("--- Demo 4: Specialized Calculators ---");
            
            var rewardConfig = RewardConfig.CreateDefault();
            var monsterConfig = MonsterRLConfig.CreateDefault(MonsterType.Melee);
            
            // Create different calculator types
            var normalCalculator = new RewardCalculator(rewardConfig, monsterConfig);
            var sparseCalculator = SpecializedRewardCalculators.CreateSpecializedCalculator(
                RewardFunctionType.Sparse, rewardConfig, monsterConfig);
            var curiosityCalculator = SpecializedRewardCalculators.CreateSpecializedCalculator(
                RewardFunctionType.Curiosity, rewardConfig, monsterConfig);
            var adaptiveCalculator = SpecializedRewardCalculators.CreateAdaptiveCalculator(rewardConfig, monsterConfig);
            
            // Test scenario: Monster misses attack
            var state = RLGameState.CreateDefault();
            var action = MonsterAction.CreateAttack();
            var missOutcome = ActionOutcome.CreateDefault(); // No hit, no damage
            
            float normalReward = normalCalculator.CalculateReward(state, action, state, missOutcome);
            float sparseReward = sparseCalculator.CalculateReward(state, action, state, missOutcome);
            float curiosityReward = curiosityCalculator.CalculateReward(state, action, state, missOutcome);
            float adaptiveReward = adaptiveCalculator.CalculateReward(state, action, state, missOutcome);
            
            Debug.Log($"Miss Attack Rewards:");
            Debug.Log($"  Normal: {normalReward:F2}");
            Debug.Log($"  Sparse: {sparseReward:F2}");
            Debug.Log($"  Curiosity: {curiosityReward:F2}");
            Debug.Log($"  Adaptive: {adaptiveReward:F2}");
            
            // Test scenario: Monster hits player
            var hitOutcome = new ActionOutcome
            {
                hitPlayer = true,
                damageDealt = 15f,
                tookDamage = false,
                coordinated = false
            };
            
            float normalHitReward = normalCalculator.CalculateReward(state, action, state, hitOutcome);
            float sparseHitReward = sparseCalculator.CalculateReward(state, action, state, hitOutcome);
            float curiosityHitReward = curiosityCalculator.CalculateReward(state, action, state, hitOutcome);
            float adaptiveHitReward = adaptiveCalculator.CalculateReward(state, action, state, hitOutcome);
            
            Debug.Log($"Hit Attack Rewards:");
            Debug.Log($"  Normal: {normalHitReward:F2}");
            Debug.Log($"  Sparse: {sparseHitReward:F2}");
            Debug.Log($"  Curiosity: {curiosityHitReward:F2}");
            Debug.Log($"  Adaptive: {adaptiveHitReward:F2}");
        }
    }
}
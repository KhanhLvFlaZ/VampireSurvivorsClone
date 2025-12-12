using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Integration test for the RewardCalculator system
    /// Verifies that reward calculation works correctly with different configurations
    /// </summary>
    public class RewardCalculatorIntegrationTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        void Start()
        {
            if (runTestOnStart)
            {
                RunRewardCalculatorTest();
            }
        }

        [ContextMenu("Run Reward Calculator Test")]
        public void RunRewardCalculatorTest()
        {
            Debug.Log("=== Reward Calculator Integration Test Started ===");
            
            bool allTestsPassed = true;
            
            // Test 1: Basic Reward Calculation
            allTestsPassed &= TestBasicRewardCalculation();
            
            // Test 2: Terminal Rewards
            allTestsPassed &= TestTerminalRewards();
            
            // Test 3: Reward Shaping
            allTestsPassed &= TestRewardShaping();
            
            // Test 4: Factory Pattern
            allTestsPassed &= TestRewardCalculatorFactory();
            
            // Test 5: Different Monster Types
            allTestsPassed &= TestDifferentMonsterTypes();
            
            // Test 6: Specialized Calculators
            allTestsPassed &= TestSpecializedCalculators();

            // Final result
            if (allTestsPassed)
            {
                Debug.Log("=== Reward Calculator Integration Test PASSED ===");
            }
            else
            {
                Debug.LogError("=== Reward Calculator Integration Test FAILED ===");
            }
        }

        private bool TestBasicRewardCalculation()
        {
            try
            {
                // Create test configuration
                var rewardConfig = RewardConfig.CreateDefault();
                var monsterConfig = MonsterRLConfig.CreateDefault(MonsterType.Melee);
                var calculator = new RewardCalculator(rewardConfig, monsterConfig);

                // Test hit reward
                var previousState = RLGameState.CreateDefault();
                var currentState = RLGameState.CreateDefault();
                var action = MonsterAction.CreateAttack();
                var hitOutcome = new ActionOutcome
                {
                    hitPlayer = true,
                    damageDealt = 15f,
                    tookDamage = false,
                    coordinated = false
                };

                float hitReward = calculator.CalculateReward(previousState, action, currentState, hitOutcome);
                
                if (hitReward <= 0f)
                {
                    Debug.LogError($"✗ Hit reward should be positive, got: {hitReward}");
                    return false;
                }

                // Test miss (no reward)
                var missOutcome = ActionOutcome.CreateDefault();
                float missReward = calculator.CalculateReward(previousState, action, currentState, missOutcome);
                
                if (missReward < 0f)
                {
                    Debug.LogError($"✗ Miss reward should not be negative, got: {missReward}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Basic Reward Calculation: Hit={hitReward:F2}, Miss={missReward:F2}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Basic Reward Calculation test failed: {e.Message}");
                return false;
            }
        }

        private bool TestTerminalRewards()
        {
            try
            {
                var rewardConfig = RewardConfig.CreateDefault();
                var monsterConfig = MonsterRLConfig.CreateDefault(MonsterType.Melee);
                var calculator = new RewardCalculator(rewardConfig, monsterConfig);

                // Test death penalty
                var deathState = RLGameState.CreateDefault();
                deathState.playerHealth = 50f; // Player still alive
                float deathReward = calculator.CalculateTerminalReward(deathState, 10f, true);
                
                if (deathReward >= 0f)
                {
                    Debug.LogError($"✗ Death reward should be negative, got: {deathReward}");
                    return false;
                }

                // Test kill player reward
                var killState = RLGameState.CreateDefault();
                killState.playerHealth = 0f; // Player dead
                float killReward = calculator.CalculateTerminalReward(killState, 15f, false);
                
                if (killReward <= 0f)
                {
                    Debug.LogError($"✗ Kill player reward should be positive, got: {killReward}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Terminal Rewards: Death={deathReward:F2}, Kill={killReward:F2}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Terminal Rewards test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardShaping()
        {
            try
            {
                var rewardConfig = RewardConfig.CreateDefault();
                rewardConfig.rewardFunctionType = RewardFunctionType.Shaped;
                var monsterConfig = MonsterRLConfig.CreateDefault(MonsterType.Melee);
                var calculator = new RewardCalculator(rewardConfig, monsterConfig);

                // Test distance shaping
                var state = RLGameState.CreateDefault();
                state.playerPosition = Vector2.zero;
                state.monsterPosition = new Vector2(2f, 0f); // Within optimal distance
                
                float baseReward = 10f;
                float shapedReward = calculator.ShapeReward(baseReward, state);
                
                if (shapedReward == baseReward)
                {
                    Debug.LogError("✗ Reward shaping should modify the base reward");
                    return false;
                }

                // Test without shaping
                rewardConfig.rewardFunctionType = RewardFunctionType.Dense;
                float unshapedReward = calculator.ShapeReward(baseReward, state);
                
                if (unshapedReward != baseReward)
                {
                    Debug.LogError($"✗ Dense rewards should not be shaped, expected {baseReward}, got {unshapedReward}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Reward Shaping: Base={baseReward:F2}, Shaped={shapedReward:F2}, Unshaped={unshapedReward:F2}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Reward Shaping test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardCalculatorFactory()
        {
            try
            {
                // Clear cache first
                RewardCalculatorFactory.ClearCache();

                // Test getting calculator for same monster type returns same instance
                var calculator1 = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);
                var calculator2 = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);
                
                if (calculator1 != calculator2)
                {
                    Debug.LogError("✗ Factory should return same instance for same monster type");
                    return false;
                }

                // Test different monster types return different instances
                var meleeCalculator = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);
                var rangedCalculator = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Ranged);
                
                if (meleeCalculator == rangedCalculator)
                {
                    Debug.LogError("✗ Factory should return different instances for different monster types");
                    return false;
                }

                // Test validation
                bool allValid = RewardCalculatorFactory.ValidateAllConfigurations();
                if (!allValid)
                {
                    Debug.LogError("✗ Some reward configurations are invalid");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log("✓ Reward Calculator Factory: Caching=OK, Validation=OK");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Reward Calculator Factory test failed: {e.Message}");
                return false;
            }
        }

        private bool TestDifferentMonsterTypes()
        {
            try
            {
                var monsterTypes = new MonsterType[] { MonsterType.Melee, MonsterType.Ranged, MonsterType.Throwing, MonsterType.Boss };
                
                foreach (var monsterType in monsterTypes)
                {
                    var config = RewardConfig.GetConfigForMonsterType(monsterType);
                    if (!config.IsValid())
                    {
                        Debug.LogError($"✗ Invalid config for monster type: {monsterType}");
                        return false;
                    }

                    var calculator = RewardCalculatorFactory.GetRewardCalculator(monsterType);
                    if (calculator == null)
                    {
                        Debug.LogError($"✗ Failed to create calculator for monster type: {monsterType}");
                        return false;
                    }
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Different Monster Types: Tested {monsterTypes.Length} types successfully");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Different Monster Types test failed: {e.Message}");
                return false;
            }
        }

        private bool TestSpecializedCalculators()
        {
            try
            {
                var rewardConfig = RewardConfig.CreateDefault();
                var monsterConfig = MonsterRLConfig.CreateDefault(MonsterType.Melee);

                // Test sparse calculator
                var sparseCalculator = SpecializedRewardCalculators.CreateSpecializedCalculator(
                    RewardFunctionType.Sparse, rewardConfig, monsterConfig);
                
                if (sparseCalculator == null)
                {
                    Debug.LogError("✗ Failed to create sparse calculator");
                    return false;
                }

                // Test curiosity calculator
                var curiosityCalculator = SpecializedRewardCalculators.CreateSpecializedCalculator(
                    RewardFunctionType.Curiosity, rewardConfig, monsterConfig);
                
                if (curiosityCalculator == null)
                {
                    Debug.LogError("✗ Failed to create curiosity calculator");
                    return false;
                }

                // Test adaptive calculator
                var adaptiveCalculator = SpecializedRewardCalculators.CreateAdaptiveCalculator(rewardConfig, monsterConfig);
                
                if (adaptiveCalculator == null)
                {
                    Debug.LogError("✗ Failed to create adaptive calculator");
                    return false;
                }

                // Test that sparse calculator gives different results
                var state = RLGameState.CreateDefault();
                var action = MonsterAction.CreateAttack();
                var missOutcome = ActionOutcome.CreateDefault();
                
                float sparseReward = sparseCalculator.CalculateReward(state, action, state, missOutcome);
                var normalCalculator = new RewardCalculator(rewardConfig, monsterConfig);
                float normalReward = normalCalculator.CalculateReward(state, action, state, missOutcome);
                
                // Sparse should give 0 for misses, normal might give small positive reward
                if (sparseReward != 0f)
                {
                    Debug.LogError($"✗ Sparse calculator should give 0 for miss, got: {sparseReward}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Specialized Calculators: Sparse={sparseReward:F2}, Normal={normalReward:F2}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Specialized Calculators test failed: {e.Message}");
                return false;
            }
        }
    }
}
using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Unit tests for RewardCalculator system
    /// </summary>
    public class RewardCalculatorTests : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        private RewardConfig testRewardConfig;
        private MonsterRLConfig testMonsterConfig;
        private RewardCalculator rewardCalculator;

        void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run Reward Calculator Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== Reward Calculator Tests Started ===");
            
            SetUp();
            
            bool allTestsPassed = true;
            
            allTestsPassed &= TestCalculateReward_HitPlayer_ReturnsPositiveReward();
            allTestsPassed &= TestCalculateReward_MissPlayer_ReturnsSmallReward();
            allTestsPassed &= TestCalculateReward_CoordinatedAction_ReceivesCoordinationBonus();
            allTestsPassed &= TestCalculateTerminalReward_MonsterDies_ReturnsNegativeReward();
            allTestsPassed &= TestCalculateTerminalReward_PlayerDies_ReturnsLargePositiveReward();
            allTestsPassed &= TestShapeReward_OptimalDistance_ReceivesDistanceBonus();
            allTestsPassed &= TestShapeReward_NoShaping_ReturnsBaseReward();
            allTestsPassed &= TestRewardCalculatorFactory_GetRewardCalculator_ReturnsSameInstance();
            allTestsPassed &= TestRewardCalculatorFactory_DifferentMonsterTypes_ReturnsDifferentInstances();
            allTestsPassed &= TestRewardConfig_CreateDefault_IsValid();
            allTestsPassed &= TestRewardConfig_CreateAggressive_HasHigherAttackRewards();
            allTestsPassed &= TestRewardConfig_CreateDefensive_HasHigherSurvivalRewards();
            allTestsPassed &= TestSpecializedRewardCalculators_SparseCalculator_OnlyRewardsHits();
            allTestsPassed &= TestRewardCalculator_TakeDamage_ReceivesPenalty();
            
            if (allTestsPassed)
            {
                Debug.Log("=== Reward Calculator Tests PASSED ===");
            }
            else
            {
                Debug.LogError("=== Reward Calculator Tests FAILED ===");
            }
        }

        private void SetUp()
        {
            // Create test configurations
            testRewardConfig = ScriptableObject.CreateInstance<RewardConfig>();
            testRewardConfig.hitReward = 25f;
            testRewardConfig.damageRewardMultiplier = 1f;
            testRewardConfig.survivalReward = 1f;
            testRewardConfig.coordinationReward = 15f;
            testRewardConfig.deathPenalty = -100f;
            testRewardConfig.killPlayerReward = 200f;
            testRewardConfig.rewardFunctionType = RewardFunctionType.Dense;
            testRewardConfig.optimalDistance = 3f;
            testRewardConfig.optimalDistanceReward = 1f;

            testMonsterConfig = ScriptableObject.CreateInstance<MonsterRLConfig>();
            testMonsterConfig.monsterType = MonsterType.Melee;

            rewardCalculator = new RewardCalculator(testRewardConfig, testMonsterConfig);
        }

        private void TearDown()
        {
            if (testRewardConfig != null)
                DestroyImmediate(testRewardConfig);
            if (testMonsterConfig != null)
                DestroyImmediate(testMonsterConfig);
        }

        private bool TestCalculateReward_HitPlayer_ReturnsPositiveReward()
        {
            try
            {
                // Arrange
                var previousState = RLGameState.CreateDefault();
                var currentState = RLGameState.CreateDefault();
                var action = MonsterAction.CreateAttack();
                var outcome = new ActionOutcome
                {
                    hitPlayer = true,
                    damageDealt = 10f,
                    tookDamage = false,
                    coordinated = false
                };

                // Act
                float reward = rewardCalculator.CalculateReward(previousState, action, currentState, outcome);

                // Assert
                if (reward <= 0f)
                {
                    Debug.LogError("✗ Should receive positive reward for hitting player");
                    return false;
                }

                float expectedReward = testRewardConfig.hitReward + (10f * testRewardConfig.damageRewardMultiplier);
                if (Mathf.Abs(reward - expectedReward) > 0.1f)
                {
                    Debug.LogError($"✗ Expected reward {expectedReward:F2}, got {reward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Hit player reward test passed: {reward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Hit player reward test failed: {e.Message}");
                return false;
            }
        }

        private bool TestCalculateReward_MissPlayer_ReturnsSmallReward()
        {
            try
            {
                // Arrange
                var previousState = RLGameState.CreateDefault();
                var currentState = RLGameState.CreateDefault();
                var action = MonsterAction.CreateAttack();
                var outcome = new ActionOutcome
                {
                    hitPlayer = false,
                    damageDealt = 0f,
                    tookDamage = false,
                    coordinated = false
                };

                // Act
                float reward = rewardCalculator.CalculateReward(previousState, action, currentState, outcome);

                // Assert
                if (reward < 0f)
                {
                    Debug.LogError($"✗ Should not receive negative reward for missing, got {reward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Miss player reward test passed: {reward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Miss player reward test failed: {e.Message}");
                return false;
            }
        }

        private bool TestCalculateReward_CoordinatedAction_ReceivesCoordinationBonus()
        {
            try
            {
                // Arrange
                var previousState = RLGameState.CreateDefault();
                var currentState = RLGameState.CreateDefault();
                var action = MonsterAction.CreateCoordinate(0);
                var outcome = new ActionOutcome
                {
                    hitPlayer = false,
                    coordinated = true
                };

                // Act
                float reward = rewardCalculator.CalculateReward(previousState, action, currentState, outcome);

                // Assert
                if (reward < testRewardConfig.coordinationReward)
                {
                    Debug.LogError($"✗ Should receive coordination reward of at least {testRewardConfig.coordinationReward}, got {reward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Coordination reward test passed: {reward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Coordination reward test failed: {e.Message}");
                return false;
            }
        }

        private bool TestCalculateTerminalReward_MonsterDies_ReturnsNegativeReward()
        {
            try
            {
                // Arrange
                var finalState = RLGameState.CreateDefault();
                finalState.playerHealth = 50f; // Player still alive
                float episodeLength = 10f;
                bool killedByPlayer = true;

                // Act
                float reward = rewardCalculator.CalculateTerminalReward(finalState, episodeLength, killedByPlayer);

                // Assert
                if (reward >= 0f)
                {
                    Debug.LogError($"✗ Should receive negative reward for dying, got {reward:F2}");
                    return false;
                }

                if (reward > testRewardConfig.deathPenalty)
                {
                    Debug.LogError($"✗ Death penalty should be at least {testRewardConfig.deathPenalty}, got {reward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Monster death penalty test passed: {reward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Monster death penalty test failed: {e.Message}");
                return false;
            }
        }

        private bool TestCalculateTerminalReward_PlayerDies_ReturnsLargePositiveReward()
        {
            try
            {
                // Arrange
                var finalState = RLGameState.CreateDefault();
                finalState.playerHealth = 0f; // Player dead
                float episodeLength = 15f;
                bool killedByPlayer = false;

                // Act
                float reward = rewardCalculator.CalculateTerminalReward(finalState, episodeLength, killedByPlayer);

                // Assert
                if (reward <= 0f)
                {
                    Debug.LogError($"✗ Should receive positive reward for killing player, got {reward:F2}");
                    return false;
                }

                if (reward < testRewardConfig.killPlayerReward)
                {
                    Debug.LogError($"✗ Kill player reward should be at least {testRewardConfig.killPlayerReward}, got {reward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Player death reward test passed: {reward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Player death reward test failed: {e.Message}");
                return false;
            }
        }

        private bool TestShapeReward_OptimalDistance_ReceivesDistanceBonus()
        {
            try
            {
                // Arrange
                testRewardConfig.rewardFunctionType = RewardFunctionType.Shaped;
                var state = RLGameState.CreateDefault();
                state.playerPosition = Vector2.zero;
                state.monsterPosition = new Vector2(2f, 0f); // Within optimal distance
                float baseReward = 10f;

                // Act
                float shapedReward = rewardCalculator.ShapeReward(baseReward, state);

                // Assert
                if (shapedReward <= baseReward)
                {
                    Debug.LogError($"✗ Should receive distance shaping bonus, base: {baseReward:F2}, shaped: {shapedReward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Distance shaping test passed: {baseReward:F2} -> {shapedReward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Distance shaping test failed: {e.Message}");
                return false;
            }
        }

        private bool TestShapeReward_NoShaping_ReturnsBaseReward()
        {
            try
            {
                // Arrange
                testRewardConfig.rewardFunctionType = RewardFunctionType.Dense;
                var state = RLGameState.CreateDefault();
                float baseReward = 10f;

                // Act
                float shapedReward = rewardCalculator.ShapeReward(baseReward, state);

                // Assert
                if (Mathf.Abs(shapedReward - baseReward) > 0.01f)
                {
                    Debug.LogError($"✗ Should return base reward when shaping disabled, expected {baseReward:F2}, got {shapedReward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ No shaping test passed: {shapedReward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ No shaping test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardCalculatorFactory_GetRewardCalculator_ReturnsSameInstance()
        {
            try
            {
                // Act
                var calculator1 = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);
                var calculator2 = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);

                // Assert
                if (calculator1 != calculator2)
                {
                    Debug.LogError("✗ Factory should return cached instance");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log("✓ Factory caching test passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Factory caching test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardCalculatorFactory_DifferentMonsterTypes_ReturnsDifferentInstances()
        {
            try
            {
                // Act
                var meleeCalculator = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Melee);
                var rangedCalculator = RewardCalculatorFactory.GetRewardCalculator(MonsterType.Ranged);

                // Assert
                if (meleeCalculator == rangedCalculator)
                {
                    Debug.LogError("✗ Different monster types should have different calculators");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log("✓ Factory different types test passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Factory different types test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardConfig_CreateDefault_IsValid()
        {
            try
            {
                // Act
                var config = RewardConfig.CreateDefault();

                // Assert
                if (!config.IsValid())
                {
                    Debug.LogError("✗ Default config should be valid");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log("✓ Default config validation test passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Default config validation test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardConfig_CreateAggressive_HasHigherAttackRewards()
        {
            try
            {
                // Act
                var defaultConfig = RewardConfig.CreateDefault();
                var aggressiveConfig = RewardConfig.CreateAggressive();

                // Assert
                if (aggressiveConfig.hitReward <= defaultConfig.hitReward)
                {
                    Debug.LogError($"✗ Aggressive config should have higher hit reward: {aggressiveConfig.hitReward} vs {defaultConfig.hitReward}");
                    return false;
                }

                if (aggressiveConfig.damageRewardMultiplier <= defaultConfig.damageRewardMultiplier)
                {
                    Debug.LogError($"✗ Aggressive config should have higher damage multiplier: {aggressiveConfig.damageRewardMultiplier} vs {defaultConfig.damageRewardMultiplier}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Aggressive config test passed: Hit {aggressiveConfig.hitReward} vs {defaultConfig.hitReward}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Aggressive config test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardConfig_CreateDefensive_HasHigherSurvivalRewards()
        {
            try
            {
                // Act
                var defaultConfig = RewardConfig.CreateDefault();
                var defensiveConfig = RewardConfig.CreateDefensive();

                // Assert
                if (defensiveConfig.survivalReward <= defaultConfig.survivalReward)
                {
                    Debug.LogError($"✗ Defensive config should have higher survival reward: {defensiveConfig.survivalReward} vs {defaultConfig.survivalReward}");
                    return false;
                }

                if (defensiveConfig.tacticalRetreatReward <= defaultConfig.tacticalRetreatReward)
                {
                    Debug.LogError($"✗ Defensive config should have higher retreat reward: {defensiveConfig.tacticalRetreatReward} vs {defaultConfig.tacticalRetreatReward}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Defensive config test passed: Survival {defensiveConfig.survivalReward} vs {defaultConfig.survivalReward}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Defensive config test failed: {e.Message}");
                return false;
            }
        }

        private bool TestSpecializedRewardCalculators_SparseCalculator_OnlyRewardsHits()
        {
            try
            {
                // Arrange
                var sparseCalculator = new SpecializedRewardCalculators.SparseRewardCalculator(testRewardConfig, testMonsterConfig);
                var previousState = RLGameState.CreateDefault();
                var currentState = RLGameState.CreateDefault();
                var missAction = MonsterAction.CreateAttack();
                var missOutcome = ActionOutcome.CreateDefault();

                // Act
                float missReward = sparseCalculator.CalculateReward(previousState, missAction, currentState, missOutcome);

                // Assert
                if (Mathf.Abs(missReward - 0f) > 0.001f)
                {
                    Debug.LogError($"✗ Sparse calculator should not reward misses, got {missReward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log("✓ Sparse calculator test passed");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Sparse calculator test failed: {e.Message}");
                return false;
            }
        }

        private bool TestRewardCalculator_TakeDamage_ReceivesPenalty()
        {
            try
            {
                // Arrange
                testRewardConfig.damagePenaltyMultiplier = 1f;
                var previousState = RLGameState.CreateDefault();
                var currentState = RLGameState.CreateDefault();
                var action = MonsterAction.CreateAttack();
                var outcome = new ActionOutcome
                {
                    hitPlayer = false,
                    tookDamage = true,
                    damageTaken = 20f
                };

                // Act
                float reward = rewardCalculator.CalculateReward(previousState, action, currentState, outcome);

                // Assert
                if (reward >= 0f)
                {
                    Debug.LogError($"✗ Should receive penalty for taking damage, got {reward:F2}");
                    return false;
                }

                if (logDetailedResults)
                    Debug.Log($"✓ Damage penalty test passed: {reward:F2}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Damage penalty test failed: {e.Message}");
                return false;
            }
        }
    }
}
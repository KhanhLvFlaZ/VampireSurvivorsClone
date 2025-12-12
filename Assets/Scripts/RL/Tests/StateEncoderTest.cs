using UnityEngine;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Unit tests for StateEncoder functionality
    /// Tests encoding and normalization of game state
    /// </summary>
    public class StateEncoderTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        private StateEncoder stateEncoder;
        private RLGameState testGameState;

        void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run StateEncoder Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== StateEncoder Tests Started ===");
            
            SetUp();
            
            bool allTestsPassed = true;
            
            allTestsPassed &= TestStateSize();
            allTestsPassed &= TestEncodeStateReturnsCorrectArraySize();
            allTestsPassed &= TestEncodeStateNormalizesValues();
            allTestsPassed &= TestEncodeStatePreservesPlayerPosition();
            allTestsPassed &= TestEncodeStateHandlesEmptyArrays();
            allTestsPassed &= TestNormalizeStateClampsExtremeValues();
            allTestsPassed &= TestEncodeStateConsistentResults();
            
            if (allTestsPassed)
            {
                Debug.Log("=== StateEncoder Tests PASSED ===");
            }
            else
            {
                Debug.LogError("=== StateEncoder Tests FAILED ===");
            }
        }

        private void SetUp()
        {
            stateEncoder = new StateEncoder();
            
            // Create a test game state with known values
            testGameState = new RLGameState
            {
                playerPosition = new Vector2(10f, 5f),
                playerVelocity = new Vector2(2f, -1f),
                playerHealth = 80f,
                activeAbilities = 0b1010, // Binary: 10 (decimal)
                
                monsterPosition = new Vector2(-5f, 8f),
                monsterHealth = 60f,
                currentAction = 3,
                timeSinceLastAction = 1.5f,
                timeAlive = 45f,
                timeSincePlayerDamage = 12f,
                
                nearbyMonsters = new NearbyMonster[]
                {
                    new NearbyMonster { position = new Vector2(1f, 2f), monsterType = MonsterType.Melee, health = 50f, currentAction = 1 },
                    new NearbyMonster { position = new Vector2(3f, 4f), monsterType = MonsterType.Ranged, health = 30f, currentAction = 2 }
                },
                
                nearbyCollectibles = new CollectibleInfo[]
                {
                    new CollectibleInfo { position = new Vector2(2f, 3f), collectibleType = CollectibleType.ExpGem, value = 10f },
                    new CollectibleInfo { position = new Vector2(4f, 1f), collectibleType = CollectibleType.Coin, value = 5f }
                }
            };
        }

        private bool TestStateSize()
        {
            try
            {
                // Expected size: 7 (player) + 6 (monster) + 20 (nearby monsters) + 30 (collectibles) + 1 (temporal) = 64
                int expectedSize = 64;
                int actualSize = stateEncoder.GetStateSize();
                
                if (actualSize != expectedSize)
                {
                    Debug.LogError($"✗ State size test failed: expected {expectedSize}, got {actualSize}");
                    return false;
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ State size test passed: {actualSize}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ State size test failed: {e.Message}");
                return false;
            }
        }

        private bool TestEncodeStateReturnsCorrectArraySize()
        {
            try
            {
                float[] encodedState = stateEncoder.EncodeState(testGameState);
                
                if (encodedState.Length != stateEncoder.GetStateSize())
                {
                    Debug.LogError($"✗ Encode state array size test failed: expected {stateEncoder.GetStateSize()}, got {encodedState.Length}");
                    return false;
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ Encode state array size test passed: {encodedState.Length}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Encode state array size test failed: {e.Message}");
                return false;
            }
        }

        private bool TestEncodeStateNormalizesValues()
        {
            try
            {
                float[] encodedState = stateEncoder.EncodeState(testGameState);
                
                // Check that all values are within normalized range [-1, 1]
                foreach (float value in encodedState)
                {
                    if (value < -1f || value > 1f)
                    {
                        Debug.LogError($"✗ Normalization test failed: value {value} outside [-1, 1] range");
                        return false;
                    }
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ Normalization test passed: all {encodedState.Length} values within [-1, 1]");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Normalization test failed: {e.Message}");
                return false;
            }
        }

        private bool TestEncodeStatePreservesPlayerPosition()
        {
            try
            {
                float[] encodedState = stateEncoder.EncodeState(testGameState);
                
                // Player position should be the first two values (normalized)
                float expectedX = Mathf.Clamp(testGameState.playerPosition.x / 50f, -1f, 1f);
                float expectedY = Mathf.Clamp(testGameState.playerPosition.y / 50f, -1f, 1f);
                
                if (Mathf.Abs(encodedState[0] - expectedX) > 0.001f)
                {
                    Debug.LogError($"✗ Player X position test failed: expected {expectedX}, got {encodedState[0]}");
                    return false;
                }
                
                if (Mathf.Abs(encodedState[1] - expectedY) > 0.001f)
                {
                    Debug.LogError($"✗ Player Y position test failed: expected {expectedY}, got {encodedState[1]}");
                    return false;
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ Player position preservation test passed: [{encodedState[0]:F3}, {encodedState[1]:F3}]");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Player position preservation test failed: {e.Message}");
                return false;
            }
        }

        private bool TestEncodeStateHandlesEmptyArrays()
        {
            try
            {
                // Test with empty nearby monsters and collectibles
                var emptyGameState = RLGameState.CreateDefault();
                
                float[] encodedState = stateEncoder.EncodeState(emptyGameState);
                
                if (encodedState.Length != stateEncoder.GetStateSize())
                {
                    Debug.LogError($"✗ Empty arrays test failed: wrong array size");
                    return false;
                }
                
                // All values should still be normalized
                foreach (float value in encodedState)
                {
                    if (value < -1f || value > 1f)
                    {
                        Debug.LogError($"✗ Empty arrays test failed: value {value} outside [-1, 1] range");
                        return false;
                    }
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ Empty arrays test passed: handled gracefully");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Empty arrays test failed: {e.Message}");
                return false;
            }
        }

        private bool TestNormalizeStateClampsExtremeValues()
        {
            try
            {
                // Create state with extreme values
                float[] extremeValues = new float[stateEncoder.GetStateSize()];
                for (int i = 0; i < extremeValues.Length; i++)
                {
                    extremeValues[i] = (i % 2 == 0) ? 1000f : -1000f; // Alternate between extreme positive and negative
                }
                
                float[] normalizedState = stateEncoder.NormalizeState(extremeValues);
                
                // All values should be clamped to [-1, 1] range
                foreach (float value in normalizedState)
                {
                    if (value < -1f || value > 1f)
                    {
                        Debug.LogError($"✗ Extreme values clamping test failed: value {value} outside [-1, 1] range");
                        return false;
                    }
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ Extreme values clamping test passed: all values properly clamped");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Extreme values clamping test failed: {e.Message}");
                return false;
            }
        }

        private bool TestEncodeStateConsistentResults()
        {
            try
            {
                // Encoding the same state multiple times should produce identical results
                float[] firstEncoding = stateEncoder.EncodeState(testGameState);
                float[] secondEncoding = stateEncoder.EncodeState(testGameState);
                
                if (firstEncoding.Length != secondEncoding.Length)
                {
                    Debug.LogError($"✗ Consistency test failed: different array lengths");
                    return false;
                }
                
                for (int i = 0; i < firstEncoding.Length; i++)
                {
                    if (Mathf.Abs(firstEncoding[i] - secondEncoding[i]) > 0.0001f)
                    {
                        Debug.LogError($"✗ Consistency test failed at index {i}: {firstEncoding[i]} != {secondEncoding[i]}");
                        return false;
                    }
                }
                
                if (logDetailedResults)
                    Debug.Log($"✓ Consistency test passed: identical results across multiple encodings");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Consistency test failed: {e.Message}");
                return false;
            }
        }
    }
}
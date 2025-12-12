using UnityEngine;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Demonstration script showing StateEncoder usage
    /// Can be attached to a GameObject to test state encoding in the Unity editor
    /// </summary>
    public class StateEncoderDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool runDemoOnStart = false;
        [SerializeField] private bool logEncodedValues = false;

        [Header("Test State Values")]
        [SerializeField] private Vector2 playerPosition = new Vector2(10f, 5f);
        [SerializeField] private Vector2 playerVelocity = new Vector2(2f, -1f);
        [SerializeField] private float playerHealth = 80f;
        [SerializeField] private Vector2 monsterPosition = new Vector2(-5f, 8f);
        [SerializeField] private float monsterHealth = 60f;

        private StateEncoder stateEncoder;

        void Start()
        {
            stateEncoder = new StateEncoder();
            
            if (runDemoOnStart)
            {
                RunStateEncodingDemo();
            }
        }

        [ContextMenu("Run State Encoding Demo")]
        public void RunStateEncodingDemo()
        {
            Debug.Log("=== State Encoder Demo Started ===");

            // Create test game state
            var gameState = CreateTestGameState();
            
            // Encode the state
            float[] encodedState = stateEncoder.EncodeState(gameState);
            
            Debug.Log($"State Size: {stateEncoder.GetStateSize()}");
            Debug.Log($"Encoded State Length: {encodedState.Length}");
            Debug.Log($"Player Distance: {gameState.DistanceToPlayer:F2}");
            Debug.Log($"Player Approaching: {gameState.IsPlayerApproaching}");

            if (logEncodedValues)
            {
                LogEncodedStateDetails(encodedState);
            }

            // Test with different positions
            TestPositionVariations();

            Debug.Log("=== State Encoder Demo Completed ===");
        }

        private RLGameState CreateTestGameState()
        {
            var gameState = new RLGameState
            {
                playerPosition = playerPosition,
                playerVelocity = playerVelocity,
                playerHealth = playerHealth,
                activeAbilities = 0b1010, // Some test abilities
                
                monsterPosition = monsterPosition,
                monsterHealth = monsterHealth,
                currentAction = 2,
                timeSinceLastAction = 1.5f,
                timeAlive = 30f,
                timeSincePlayerDamage = 5f,
                
                nearbyMonsters = new NearbyMonster[]
                {
                    new NearbyMonster 
                    { 
                        position = new Vector2(1f, 2f), 
                        monsterType = MonsterType.Melee, 
                        health = 50f, 
                        currentAction = 1 
                    },
                    new NearbyMonster 
                    { 
                        position = new Vector2(3f, 4f), 
                        monsterType = MonsterType.Ranged, 
                        health = 30f, 
                        currentAction = 0 
                    }
                },
                
                nearbyCollectibles = new CollectibleInfo[]
                {
                    new CollectibleInfo 
                    { 
                        position = new Vector2(2f, 3f), 
                        collectibleType = CollectibleType.ExpGem, 
                        value = 10f 
                    }
                }
            };

            return gameState;
        }

        private void LogEncodedStateDetails(float[] encodedState)
        {
            Debug.Log("=== Encoded State Details ===");
            
            // Log first few values (player state)
            Debug.Log($"Player Position (normalized): [{encodedState[0]:F3}, {encodedState[1]:F3}]");
            Debug.Log($"Player Velocity (normalized): [{encodedState[2]:F3}, {encodedState[3]:F3}]");
            Debug.Log($"Player Health (normalized): {encodedState[4]:F3}");
            
            // Log monster state
            Debug.Log($"Monster Position (normalized): [{encodedState[7]:F3}, {encodedState[8]:F3}]");
            Debug.Log($"Monster Health (normalized): {encodedState[9]:F3}");
            
            // Check normalization
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            
            foreach (float value in encodedState)
            {
                if (value < minValue) minValue = value;
                if (value > maxValue) maxValue = value;
            }
            
            Debug.Log($"Value Range: [{minValue:F3}, {maxValue:F3}] (should be within [-1, 1])");
        }

        private void TestPositionVariations()
        {
            Debug.Log("=== Testing Position Variations ===");
            
            Vector2[] testPositions = new Vector2[]
            {
                new Vector2(0f, 0f),      // Origin
                new Vector2(25f, 25f),    // Far positive
                new Vector2(-25f, -25f),  // Far negative
                new Vector2(100f, 100f)   // Very far (should be clamped)
            };

            foreach (var pos in testPositions)
            {
                var testState = CreateTestGameState();
                testState.playerPosition = pos;
                
                float[] encoded = stateEncoder.EncodeState(testState);
                
                Debug.Log($"Position {pos} -> Normalized: [{encoded[0]:F3}, {encoded[1]:F3}]");
            }
        }

        [ContextMenu("Test State Builder Integration")]
        public void TestStateBuilderIntegration()
        {
            Debug.Log("=== Testing GameStateBuilder Integration ===");
            
            // Create a test state using GameStateBuilder
            var testState = GameStateBuilder.CreateTestGameState(
                playerPosition, 
                monsterPosition, 
                playerHealth, 
                monsterHealth
            );
            
            // Encode it
            float[] encoded = stateEncoder.EncodeState(testState);
            
            Debug.Log($"GameStateBuilder + StateEncoder: Created state with {encoded.Length} features");
            Debug.Log($"Distance to player: {testState.DistanceToPlayer:F2}");
            Debug.Log($"Direction to player: {testState.DirectionToPlayer}");
        }
    }
}
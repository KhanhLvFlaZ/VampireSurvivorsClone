using UnityEngine;
using System;
using System.IO;

namespace Vampire.RL.Examples
{
    /// <summary>
    /// Demo script to test error handling and fallback systems
    /// Shows how the RL system gracefully handles various failure scenarios
    /// </summary>
    public class ErrorHandlingDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool enableLogging = true;
        
        void Start()
        {
            if (runOnStart)
            {
                RunErrorHandlingDemo();
            }
        }
        
        [ContextMenu("Run Error Handling Demo")]
        public void RunErrorHandlingDemo()
        {
            Debug.Log("=== Error Handling Demo Started ===");
            
            // Test 1: Error logging and statistics
            TestErrorLogging();
            
            // Test 2: Corrupted profile recovery
            TestCorruptedProfileRecovery();
            
            // Test 3: Failed network recovery
            TestFailedNetworkRecovery();
            
            // Test 4: Failed agent recovery
            TestFailedAgentRecovery();
            
            // Test 5: Component failure tracking
            TestComponentFailureTracking();
            
            // Test 6: Performance monitoring
            TestPerformanceMonitoring();
            
            // Test 7: Fallback agent functionality
            TestFallbackAgent();
            
            // Test 8: Dummy network functionality
            TestDummyNetwork();
            
            Debug.Log("=== Error Handling Demo Completed ===");
        }
        
        private void TestErrorLogging()
        {
            Debug.Log("--- Testing Error Logging ---");
            
            // Subscribe to error events
            ErrorHandler.OnErrorLogged += OnErrorLogged;
            
            // Log different types of errors
            ErrorHandler.LogError("TestComponent", "TestOperation", 
                new InvalidOperationException("Test error"), "Demo context");
            
            ErrorHandler.LogError("TestComponent", "CriticalOperation", 
                new OutOfMemoryException("Critical test error"), "Critical demo context");
            
            // Get error statistics
            var stats = ErrorHandler.GetErrorStatistics();
            Debug.Log($"Error Statistics: Total={stats.totalErrors}, Critical={stats.criticalErrors}, " +
                     $"High={stats.highSeverityErrors}, CriticalRate={stats.CriticalErrorRate:P}");
            
            // Unsubscribe
            ErrorHandler.OnErrorLogged -= OnErrorLogged;
        }
        
        private void TestCorruptedProfileRecovery()
        {
            Debug.Log("--- Testing Corrupted Profile Recovery ---");
            
            var corruptionException = new InvalidDataException("Simulated profile corruption");
            var recoveredProfile = ErrorHandler.RecoverCorruptedProfile(
                MonsterType.Ranged, "fake_corrupted_path.json", corruptionException);
            
            if (recoveredProfile != null)
            {
                Debug.Log($"Successfully recovered profile for {recoveredProfile.monsterType}. " +
                         $"Valid: {recoveredProfile.IsValid()}, PlayerID: {recoveredProfile.playerProfileId}");
            }
            else
            {
                Debug.LogError("Failed to recover corrupted profile");
            }
        }
        
        private void TestFailedNetworkRecovery()
        {
            Debug.Log("--- Testing Failed Network Recovery ---");
            
            var networkException = new OutOfMemoryException("Simulated network initialization failure");
            var recoveredNetwork = ErrorHandler.RecoverFailedNetwork(
                NetworkArchitecture.Simple, 32, 15, new int[] { 64, 32 }, networkException);
            
            if (recoveredNetwork != null)
            {
                Debug.Log($"Successfully recovered network: {recoveredNetwork.Architecture}, " +
                         $"Input={recoveredNetwork.InputSize}, Output={recoveredNetwork.OutputSize}, " +
                         $"Parameters={recoveredNetwork.GetParameterCount()}");
                
                // Test the recovered network
                var testInput = new float[32];
                for (int i = 0; i < testInput.Length; i++)
                {
                    testInput[i] = UnityEngine.Random.Range(-1f, 1f);
                }
                
                var output = recoveredNetwork.Forward(testInput);
                Debug.Log($"Network test output length: {output?.Length ?? 0}");
            }
            else
            {
                Debug.LogError("Failed to recover network");
            }
        }
        
        private void TestFailedAgentRecovery()
        {
            Debug.Log("--- Testing Failed Agent Recovery ---");
            
            var agentException = new InvalidOperationException("Simulated agent initialization failure");
            var actionSpace = ActionSpace.CreateDefault();
            var recoveredAgent = ErrorHandler.RecoverFailedAgent(MonsterType.Throwing, actionSpace, agentException);
            
            if (recoveredAgent != null)
            {
                Debug.Log($"Successfully recovered agent. Training: {recoveredAgent.IsTraining}");
                
                // Test the recovered agent
                var testState = new RLGameState
                {
                    playerPosition = Vector2.zero,
                    monsterPosition = new Vector2(3f, 0f),
                    playerHealth = 100f,
                    monsterHealth = 80f
                };
                
                int action = recoveredAgent.SelectAction(testState, false);
                Debug.Log($"Agent selected action: {action}");
                
                var metrics = recoveredAgent.GetMetrics();
                Debug.Log($"Agent metrics - Episodes: {metrics.episodeCount}, Steps: {metrics.totalSteps}");
            }
            else
            {
                Debug.LogError("Failed to recover agent");
            }
        }
        
        private void TestComponentFailureTracking()
        {
            Debug.Log("--- Testing Component Failure Tracking ---");
            
            string testComponent = "DemoComponent";
            
            // Simulate multiple failures
            for (int i = 0; i < 4; i++)
            {
                ErrorHandler.LogError(testComponent, "TestOperation", 
                    new Exception($"Failure {i + 1}"), "Demo failure");
            }
            
            bool shouldDisable = ErrorHandler.ShouldDisableComponent(testComponent);
            Debug.Log($"Component {testComponent} should be disabled: {shouldDisable}");
            
            // Reset and test again
            ErrorHandler.ResetComponentErrors(testComponent);
            bool shouldDisableAfterReset = ErrorHandler.ShouldDisableComponent(testComponent);
            Debug.Log($"Component {testComponent} should be disabled after reset: {shouldDisableAfterReset}");
        }
        
        private void TestPerformanceMonitoring()
        {
            Debug.Log("--- Testing Performance Monitoring ---");
            
            // Create performance monitor
            var monitorGO = new GameObject("DemoPerformanceMonitor");
            var monitor = monitorGO.AddComponent<PerformanceMonitor>();
            
            // Subscribe to events
            monitor.OnDegradationLevelChanged += OnDegradationLevelChanged;
            monitor.OnPerformanceAlert += OnPerformanceAlert;
            
            // Simulate performance issues
            monitor.UpdateSystemMetrics(25f, 120f, 60); // Over limits
            monitor.RecordComponentPerformance("DemoComponent", 20f);
            
            // Test degradation
            monitor.SetDegradationLevel(DegradationLevel.High);
            Debug.Log($"Current degradation level: {monitor.CurrentDegradationLevel}");
            
            // Get recommendations
            var recommendations = monitor.GetPerformanceRecommendations();
            Debug.Log($"Performance recommendations: {string.Join(", ", recommendations)}");
            
            // Cleanup
            monitor.OnDegradationLevelChanged -= OnDegradationLevelChanged;
            monitor.OnPerformanceAlert -= OnPerformanceAlert;
            DestroyImmediate(monitorGO);
        }
        
        private void TestFallbackAgent()
        {
            Debug.Log("--- Testing Fallback Agent ---");
            
            var agentGO = new GameObject("DemoFallbackAgent");
            var fallbackAgent = agentGO.AddComponent<FallbackLearningAgent>();
            var actionSpace = ActionSpace.CreateDefault();
            
            fallbackAgent.Initialize(MonsterType.Melee, actionSpace);
            
            // Test action selection
            var testState = new RLGameState
            {
                playerPosition = Vector2.zero,
                monsterPosition = new Vector2(5f, 5f),
                playerHealth = 75f,
                monsterHealth = 50f
            };
            
            int action = fallbackAgent.SelectAction(testState, false);
            Debug.Log($"Fallback agent selected action: {action}");
            
            // Test experience storage (should not crash)
            fallbackAgent.StoreExperience(testState, action, 10f, testState, false);
            
            // Test policy update (should not crash)
            fallbackAgent.UpdatePolicy();
            
            var metrics = fallbackAgent.GetMetrics();
            Debug.Log($"Fallback agent metrics - Episodes: {metrics.episodeCount}, Steps: {metrics.totalSteps}");
            
            // Cleanup
            DestroyImmediate(agentGO);
        }
        
        private void TestDummyNetwork()
        {
            Debug.Log("--- Testing Dummy Network ---");
            
            var dummyNetwork = new DummyNeuralNetwork(10, 5);
            
            Debug.Log($"Dummy network - Input: {dummyNetwork.InputSize}, Output: {dummyNetwork.OutputSize}, " +
                     $"Parameters: {dummyNetwork.GetParameterCount()}, Supports Training: {dummyNetwork.SupportsTraining}");
            
            // Test with valid input
            var validInput = new float[10];
            for (int i = 0; i < validInput.Length; i++)
            {
                validInput[i] = UnityEngine.Random.Range(-1f, 1f);
            }
            
            var output = dummyNetwork.Forward(validInput);
            Debug.Log($"Dummy network output length: {output.Length}");
            
            // Test with invalid input (should not crash)
            var invalidOutput1 = dummyNetwork.Forward(null);
            var invalidOutput2 = dummyNetwork.Forward(new float[5]); // Wrong size
            
            Debug.Log($"Dummy network handled invalid inputs gracefully: " +
                     $"null={invalidOutput1?.Length ?? 0}, wrong_size={invalidOutput2?.Length ?? 0}");
            
            // Test training operations (should not crash)
            float loss = dummyNetwork.Backward(validInput, new float[5], 0.01f);
            Debug.Log($"Dummy network training loss: {loss}");
        }
        
        // Event handlers
        private void OnErrorLogged(ErrorLog errorLog)
        {
            if (enableLogging)
            {
                Debug.Log($"[ERROR EVENT] {errorLog.component}.{errorLog.operation}: {errorLog.exception.Message} " +
                         $"(Severity: {errorLog.severity})");
            }
        }
        
        private void OnDegradationLevelChanged(DegradationLevel level)
        {
            if (enableLogging)
            {
                Debug.Log($"[PERFORMANCE EVENT] Degradation level changed to: {level}");
            }
        }
        
        private void OnPerformanceAlert(PerformanceAlert alert)
        {
            if (enableLogging)
            {
                Debug.Log($"[PERFORMANCE ALERT] {alert.component}.{alert.metric}: {alert.value:F2} > {alert.threshold:F2} " +
                         $"(Severity: {alert.severity})");
            }
        }
    }
}
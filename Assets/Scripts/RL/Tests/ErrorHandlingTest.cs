using UnityEngine;
using System;
using System.Collections;
using System.IO;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Tests for error handling and fallback systems
    /// Validates Requirements 3.5, 4.3 - fallback behavior and error recovery
    /// </summary>
    public class ErrorHandlingTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;
        
        private string testProfilePath;
        private BehaviorProfileManager profileManager;
        
        void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        [ContextMenu("Run All Error Handling Tests")]
        public void RunAllTestsMenu()
        {
            StartCoroutine(RunAllTests());
        }
        
        private IEnumerator RunAllTests()
        {
            Debug.Log("=== Error Handling Tests Started ===");
            
            Setup();
            
            yield return StartCoroutine(TestErrorLogging());
            yield return StartCoroutine(TestCorruptedProfileRecovery());
            yield return StartCoroutine(TestFailedNetworkRecovery());
            yield return StartCoroutine(TestFailedAgentRecovery());
            yield return StartCoroutine(TestComponentFailureTracking());
            yield return StartCoroutine(TestErrorStatistics());
            yield return StartCoroutine(TestFallbackAgent());
            yield return StartCoroutine(TestDummyNetwork());
            yield return StartCoroutine(TestPerformanceMonitor());
            
            TearDown();
            
            Debug.Log("=== Error Handling Tests Completed ===");
        }
        
        private void Setup()
        {
            testProfilePath = Path.Combine(Application.temporaryCachePath, "test_profiles");
            if (Directory.Exists(testProfilePath))
            {
                Directory.Delete(testProfilePath, true);
            }
            Directory.CreateDirectory(testProfilePath);
            
            profileManager = new BehaviorProfileManager();
            profileManager.Initialize("test_player");
        }
        
        private void TearDown()
        {
            if (Directory.Exists(testProfilePath))
            {
                Directory.Delete(testProfilePath, true);
            }
        }

        private IEnumerator TestErrorLogging()
        {
            Debug.Log("--- Testing Error Logging ---");
            
            // Arrange
            var testException = new InvalidOperationException("Test error");
            bool eventTriggered = false;
            ErrorLog capturedLog = null;
            
            ErrorHandler.OnErrorLogged += (log) => {
                eventTriggered = true;
                capturedLog = log;
            };
            
            // Act
            ErrorHandler.LogError("TestComponent", "TestOperation", testException, "Test context");
            
            yield return null; // Wait one frame
            
            // Assert
            bool success = eventTriggered && capturedLog != null && 
                          capturedLog.component == "TestComponent" &&
                          capturedLog.operation == "TestOperation" &&
                          capturedLog.exception == testException &&
                          capturedLog.context == "Test context";
            
            if (logDetailedResults)
            {
                Debug.Log($"Error Logging Test: {(success ? "PASSED" : "FAILED")}");
                if (success)
                {
                    Debug.Log($"  - Event triggered: {eventTriggered}");
                    Debug.Log($"  - Component: {capturedLog.component}");
                    Debug.Log($"  - Operation: {capturedLog.operation}");
                    Debug.Log($"  - Severity: {capturedLog.severity}");
                }
            }
            
            // Cleanup
            ErrorHandler.OnErrorLogged -= (log) => {
                eventTriggered = true;
                capturedLog = log;
            };
        }

        private IEnumerator TestCorruptedProfileRecovery()
        {
            Debug.Log("--- Testing Corrupted Profile Recovery ---");
            
            // Arrange
            var corruptionException = new InvalidDataException("Corrupted profile");
            
            // Act
            var recoveredProfile = ErrorHandler.RecoverCorruptedProfile(MonsterType.Ranged, "fake_path", corruptionException);
            
            yield return null;
            
            // Assert
            bool success = recoveredProfile != null && 
                          recoveredProfile.monsterType == MonsterType.Ranged &&
                          recoveredProfile.IsValid() &&
                          recoveredProfile.playerProfileId == "default";
            
            if (logDetailedResults)
            {
                Debug.Log($"Corrupted Profile Recovery Test: {(success ? "PASSED" : "FAILED")}");
                if (success)
                {
                    Debug.Log($"  - Profile recovered for: {recoveredProfile.monsterType}");
                    Debug.Log($"  - Profile valid: {recoveredProfile.IsValid()}");
                    Debug.Log($"  - Player ID: {recoveredProfile.playerProfileId}");
                }
            }
        }

        private IEnumerator TestFailedNetworkRecovery()
        {
            Debug.Log("--- Testing Failed Network Recovery ---");
            
            // Arrange
            var networkException = new OutOfMemoryException("Network initialization failed");
            
            // Act
            var recoveredNetwork = ErrorHandler.RecoverFailedNetwork(
                NetworkArchitecture.Simple, 32, 15, new int[] { 64, 32 }, networkException);
            
            yield return null;
            
            // Assert
            bool success = recoveredNetwork != null &&
                          recoveredNetwork.InputSize == 32 &&
                          recoveredNetwork.OutputSize == 15 &&
                          recoveredNetwork.Architecture == NetworkArchitecture.Simple;
            
            if (logDetailedResults)
            {
                Debug.Log($"Failed Network Recovery Test: {(success ? "PASSED" : "FAILED")}");
                if (success)
                {
                    Debug.Log($"  - Network recovered: {recoveredNetwork.Architecture}");
                    Debug.Log($"  - Input size: {recoveredNetwork.InputSize}");
                    Debug.Log($"  - Output size: {recoveredNetwork.OutputSize}");
                    Debug.Log($"  - Parameters: {recoveredNetwork.GetParameterCount()}");
                }
            }
        }

        private IEnumerator TestFailedAgentRecovery()
        {
            Debug.Log("--- Testing Failed Agent Recovery ---");
            
            // Arrange
            var agentException = new InvalidOperationException("Agent initialization failed");
            var actionSpace = ActionSpace.CreateDefault();
            
            // Act
            var recoveredAgent = ErrorHandler.RecoverFailedAgent(MonsterType.Throwing, actionSpace, agentException);
            
            yield return null;
            
            // Assert
            bool success = recoveredAgent != null && !recoveredAgent.IsTraining;
            
            if (logDetailedResults)
            {
                Debug.Log($"Failed Agent Recovery Test: {(success ? "PASSED" : "FAILED")}");
                if (success)
                {
                    Debug.Log($"  - Agent recovered: {recoveredAgent != null}");
                    Debug.Log($"  - Training mode: {recoveredAgent.IsTraining}");
                }
            }
        }

        private IEnumerator TestComponentFailureTracking()
        {
            Debug.Log("--- Testing Component Failure Tracking ---");
            
            // Arrange
            string componentName = "TestComponent_" + UnityEngine.Random.Range(1000, 9999);
            var testException = new Exception("Test failure");
            
            // Act - Trigger multiple failures
            for (int i = 0; i < 4; i++) // More than MAX_RETRY_ATTEMPTS (3)
            {
                ErrorHandler.LogError(componentName, "TestOperation", testException);
            }
            
            yield return null;
            
            // Assert
            bool shouldDisable = ErrorHandler.ShouldDisableComponent(componentName);
            
            // Test reset functionality
            ErrorHandler.ResetComponentErrors(componentName);
            bool shouldDisableAfterReset = ErrorHandler.ShouldDisableComponent(componentName);
            
            bool success = shouldDisable && !shouldDisableAfterReset;
            
            if (logDetailedResults)
            {
                Debug.Log($"Component Failure Tracking Test: {(success ? "PASSED" : "FAILED")}");
                Debug.Log($"  - Should disable after failures: {shouldDisable}");
                Debug.Log($"  - Should not disable after reset: {!shouldDisableAfterReset}");
            }
        }

        private IEnumerator TestErrorStatistics()
        {
            Debug.Log("--- Testing Error Statistics ---");
            
            // Arrange & Act
            ErrorHandler.LogError("Test", "Op1", new Exception("Low severity"));
            ErrorHandler.LogError("Test", "Op2", new OutOfMemoryException("Critical"));
            ErrorHandler.LogError("Test", "Op3", new FileNotFoundException("High severity"));
            
            yield return null;
            
            // Assert
            var stats = ErrorHandler.GetErrorStatistics();
            bool success = stats.totalErrors >= 3 && stats.criticalErrors >= 1 && stats.highSeverityErrors >= 1;
            
            if (logDetailedResults)
            {
                Debug.Log($"Error Statistics Test: {(success ? "PASSED" : "FAILED")}");
                Debug.Log($"  - Total errors: {stats.totalErrors}");
                Debug.Log($"  - Critical errors: {stats.criticalErrors}");
                Debug.Log($"  - High severity errors: {stats.highSeverityErrors}");
                Debug.Log($"  - Critical error rate: {stats.CriticalErrorRate:P}");
            }
        }

        private IEnumerator TestFallbackAgent()
        {
            Debug.Log("--- Testing Fallback Agent ---");
            
            // Arrange
            var gameObject = new GameObject("TestFallbackAgent");
            var fallbackAgent = gameObject.AddComponent<FallbackLearningAgent>();
            var actionSpace = ActionSpace.CreateDefault();
            
            // Act
            fallbackAgent.Initialize(MonsterType.Melee, actionSpace);
            
            yield return null;
            
            var testState = new RLGameState
            {
                playerPosition = Vector2.zero,
                monsterPosition = new Vector2(5f, 0f),
                playerHealth = 100f,
                monsterHealth = 80f
            };
            
            int selectedAction = fallbackAgent.SelectAction(testState, false);
            
            // Assert
            var metrics = fallbackAgent.GetMetrics();
            bool success = !fallbackAgent.IsTraining && 
                          metrics.totalSteps >= 0 && // Struct is always valid, check a field instead
                          selectedAction >= 0 && 
                          selectedAction < actionSpace.GetTotalActionCount();
            
            if (logDetailedResults)
            {
                Debug.Log($"Fallback Agent Test: {(success ? "PASSED" : "FAILED")}");
                Debug.Log($"  - Training mode: {fallbackAgent.IsTraining}");
                Debug.Log($"  - Has metrics: {metrics.totalSteps >= 0}"); // Check if metrics are valid
                Debug.Log($"  - Selected action: {selectedAction}");
                Debug.Log($"  - Action in valid range: {selectedAction >= 0 && selectedAction < actionSpace.GetTotalActionCount()}");
            }
            
            // Cleanup
            DestroyImmediate(gameObject);
        }

        private IEnumerator TestDummyNetwork()
        {
            Debug.Log("--- Testing Dummy Network ---");
            
            // Arrange
            var dummyNetwork = new DummyNeuralNetwork(10, 5);
            
            // Act & Assert - Should not throw exceptions
            var result1 = dummyNetwork.Forward(null);
            var result2 = dummyNetwork.Forward(new float[5]); // Wrong size
            var result3 = dummyNetwork.Forward(new float[10]); // Correct size
            
            yield return null;
            
            bool success = result1 != null && result1.Length == 5 &&
                          result2 != null && result2.Length == 5 &&
                          result3 != null && result3.Length == 5 &&
                          dummyNetwork.GetParameterCount() == 0;
            
            if (logDetailedResults)
            {
                Debug.Log($"Dummy Network Test: {(success ? "PASSED" : "FAILED")}");
                Debug.Log($"  - Handles null input: {result1 != null && result1.Length == 5}");
                Debug.Log($"  - Handles wrong size input: {result2 != null && result2.Length == 5}");
                Debug.Log($"  - Handles correct input: {result3 != null && result3.Length == 5}");
                Debug.Log($"  - Parameter count: {dummyNetwork.GetParameterCount()}");
            }
            
            // Test training operations (should not crash)
            float loss = dummyNetwork.Backward(new float[10], new float[5], 0.01f);
            dummyNetwork.SetWeights(new float[] { 1f, 2f, 3f });
            dummyNetwork.SetBiases(new float[] { 0.1f, 0.2f });
            dummyNetwork.AddNoise(0.1f);
            dummyNetwork.Reset();
            
            if (logDetailedResults)
            {
                Debug.Log($"  - Training operations completed without errors, loss: {loss}");
            }
        }

        private IEnumerator TestPerformanceMonitor()
        {
            Debug.Log("--- Testing Performance Monitor ---");
            
            // Arrange
            var gameObject = new GameObject("TestPerformanceMonitor");
            var monitor = gameObject.AddComponent<PerformanceMonitor>();
            bool degradationChanged = false;
            bool alertTriggered = false;
            
            monitor.OnDegradationLevelChanged += (level) => {
                degradationChanged = true;
            };
            
            monitor.OnPerformanceAlert += (alert) => {
                alertTriggered = true;
            };
            
            yield return null; // Wait for initialization
            
            // Act
            monitor.UpdateSystemMetrics(20f, 50f, 10); // Over frame time limit
            monitor.RecordComponentPerformance("TestComponent", 15f);
            monitor.SetDegradationLevel(DegradationLevel.High);
            
            yield return null;
            
            // Assert
            var metrics = monitor.CurrentMetrics;
            bool success = metrics.frameTimeMs == 20f &&
                          metrics.memoryUsageMB == 50f &&
                          metrics.activeAgents == 10 &&
                          degradationChanged &&
                          monitor.CurrentDegradationLevel == DegradationLevel.High;
            
            if (logDetailedResults)
            {
                Debug.Log($"Performance Monitor Test: {(success ? "PASSED" : "FAILED")}");
                Debug.Log($"  - Frame time: {metrics.frameTimeMs}ms");
                Debug.Log($"  - Memory usage: {metrics.memoryUsageMB}MB");
                Debug.Log($"  - Active agents: {metrics.activeAgents}");
                Debug.Log($"  - Degradation changed: {degradationChanged}");
                Debug.Log($"  - Current degradation: {monitor.CurrentDegradationLevel}");
            }
            
            // Cleanup
            DestroyImmediate(gameObject);
        }


    }
}
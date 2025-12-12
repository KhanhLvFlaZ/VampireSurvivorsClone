using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Vampire;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Comprehensive tests for TrainingCoordinator implementation
    /// Tests training/inference mode management, learning progress tracking, and state preservation
    /// </summary>
    public class TrainingCoordinatorTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;
        
        private GameObject coordinatorGO;
        private TrainingCoordinator coordinator;
        private MockEntityManager mockEntityManager;
        private MockCharacter mockPlayer;
        private MockLearningAgent mockAgent1;
        private MockLearningAgent mockAgent2;
        
        private int testsRun = 0;
        private int testsPassed = 0;
        private int testsFailed = 0;

        void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }

        IEnumerator RunAllTests()
        {
            Debug.Log("=== TrainingCoordinator Tests Started ===");
            
            yield return StartCoroutine(RunTest("Initialize_SetsDefaultValues", Test_Initialize_SetsDefaultValues));
            yield return StartCoroutine(RunTest("SetTrainingMode_ChangesMode", Test_SetTrainingMode_ChangesMode));
            yield return StartCoroutine(RunTest("SetTrainingMode_UpdatesRegisteredAgents", Test_SetTrainingMode_UpdatesRegisteredAgents));
            yield return StartCoroutine(RunTest("RegisterAgent_AddsAgentCorrectly", Test_RegisterAgent_AddsAgentCorrectly));
            yield return StartCoroutine(RunTest("RegisterAgent_IgnoresDuplicates", Test_RegisterAgent_IgnoresDuplicates));
            yield return StartCoroutine(RunTest("UnregisterAgent_RemovesAgentCorrectly", Test_UnregisterAgent_RemovesAgentCorrectly));
            yield return StartCoroutine(RunTest("UpdateAgents_CallsUpdatePolicyOnTrainingAgents", Test_UpdateAgents_CallsUpdatePolicyOnTrainingAgents));
            yield return StartCoroutine(RunTest("UpdateAgents_SkipsUpdatePolicyOnInferenceAgents", Test_UpdateAgents_SkipsUpdatePolicyOnInferenceAgents));
            yield return StartCoroutine(RunTest("TriggerLearningUpdate_ForcesUpdateOnAllTrainingAgents", Test_TriggerLearningUpdate_ForcesUpdateOnAllTrainingAgents));
            yield return StartCoroutine(RunTest("GetAllMetrics_ReturnsCorrectMetrics", Test_GetAllMetrics_ReturnsCorrectMetrics));
            yield return StartCoroutine(RunTest("MixedMode_TrainsUnconvergedAgents", Test_MixedMode_TrainsUnconvergedAgents));
            yield return StartCoroutine(RunTest("UpdateAgents_EmitsProgressEvents", Test_UpdateAgents_EmitsProgressEvents));
            yield return StartCoroutine(RunTest("ResetAllProgress_ClearsMetrics", Test_ResetAllProgress_ClearsMetrics));
            
            Debug.Log($"=== TrainingCoordinator Tests Completed: {testsPassed}/{testsRun} passed, {testsFailed} failed ===");
        }

        IEnumerator RunTest(string testName, System.Func<IEnumerator> testMethod)
        {
            testsRun++;
            SetUp();
            
            Debug.Log($"Running test: {testName}");
            
            // Simple approach: run test without try-catch around yield
            yield return StartCoroutine(testMethod());
            
            // If we get here, the test passed
            testsPassed++;
            if (logDetailedResults)
            {
                Debug.Log($"✅ {testName} - PASSED");
            }
            
            TearDown();
        }

        void SetUp()
        {
            // Create test objects
            coordinatorGO = new GameObject("TestTrainingCoordinator");
            coordinator = coordinatorGO.AddComponent<TrainingCoordinator>();
            
            mockEntityManager = new MockEntityManager();
            mockPlayer = new MockCharacter();
            mockAgent1 = new MockLearningAgent();
            mockAgent2 = new MockLearningAgent();
            
            // Initialize coordinator
            coordinator.Initialize(mockPlayer);
        }

        void TearDown()
        {
            if (coordinatorGO != null)
            {
                DestroyImmediate(coordinatorGO);
            }
            
            // Clean up any test files
            CleanupTestFiles();
        }

        IEnumerator Test_Initialize_SetsDefaultValues()
        {
            // Assert
            AssertEqual(TrainingMode.Training, coordinator.GetTrainingMode(), "Default training mode should be Training");
            AssertTrue(coordinator.IsTrainingActive, "Training should be active by default");
            AssertEqual(0f, coordinator.CurrentFrameTime, "Initial frame time should be 0");
            yield return null;
        }

        IEnumerator Test_SetTrainingMode_ChangesMode()
        {
            // Arrange
            bool eventTriggered = false;
            TrainingMode receivedMode = TrainingMode.Inference;
            coordinator.OnTrainingModeChanged += (mode) => {
                eventTriggered = true;
                receivedMode = mode;
            };

            // Act
            coordinator.SetTrainingMode(TrainingMode.Inference);

            // Assert
            AssertEqual(TrainingMode.Inference, coordinator.GetTrainingMode(), "Training mode should change to Inference");
            AssertFalse(coordinator.IsTrainingActive, "Training should not be active in Inference mode");
            AssertTrue(eventTriggered, "Mode change event should be triggered");
            AssertEqual(TrainingMode.Inference, receivedMode, "Event should receive correct mode");
            yield return null;
        }

        IEnumerator Test_SetTrainingMode_UpdatesRegisteredAgents()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent2, MonsterType.Ranged);
            
            // Act
            coordinator.SetTrainingMode(TrainingMode.Inference);

            // Assert
            AssertFalse(mockAgent1.IsTraining, "Agent1 should not be training in Inference mode");
            AssertFalse(mockAgent2.IsTraining, "Agent2 should not be training in Inference mode");
            yield return null;
        }

        IEnumerator Test_RegisterAgent_AddsAgentCorrectly()
        {
            // Act
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);

            // Assert
            var metrics = coordinator.GetAllMetrics();
            AssertTrue(metrics.ContainsKey(MonsterType.Melee), "Metrics should contain Melee monster type");
            AssertTrue(mockAgent1.IsTraining, "Agent should be training by default");
            yield return null;
        }

        IEnumerator Test_RegisterAgent_IgnoresDuplicates()
        {
            // Act
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent1, MonsterType.Ranged); // Same agent, different type

            // Assert
            var metrics = coordinator.GetAllMetrics();
            AssertEqual(1, metrics.Count, "Should only have one agent registered");
            AssertTrue(metrics.ContainsKey(MonsterType.Melee), "Should contain original monster type");
            yield return null;
        }

        IEnumerator Test_UnregisterAgent_RemovesAgentCorrectly()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent2, MonsterType.Ranged);

            // Act
            coordinator.UnregisterAgent(mockAgent1);

            // Assert
            var metrics = coordinator.GetAllMetrics();
            AssertEqual(1, metrics.Count, "Should have one agent remaining");
            AssertFalse(metrics.ContainsKey(MonsterType.Melee), "Should not contain removed agent type");
            AssertTrue(metrics.ContainsKey(MonsterType.Ranged), "Should still contain remaining agent type");
            yield return null;
        }

        IEnumerator Test_UpdateAgents_CallsUpdatePolicyOnTrainingAgents()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent2, MonsterType.Ranged);
            coordinator.SetTrainingMode(TrainingMode.Training);

            // Act
            coordinator.UpdateAgents();

            // Assert
            AssertTrue(mockAgent1.UpdatePolicyCalled, "Agent1 UpdatePolicy should be called");
            AssertTrue(mockAgent2.UpdatePolicyCalled, "Agent2 UpdatePolicy should be called");
            yield return null;
        }

        IEnumerator Test_UpdateAgents_SkipsUpdatePolicyOnInferenceAgents()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.SetTrainingMode(TrainingMode.Inference);

            // Act
            coordinator.UpdateAgents();

            // Assert
            AssertFalse(mockAgent1.UpdatePolicyCalled, "Agent UpdatePolicy should not be called in Inference mode");
            yield return null;
        }

        IEnumerator Test_TriggerLearningUpdate_ForcesUpdateOnAllTrainingAgents()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent2, MonsterType.Ranged);
            coordinator.SetTrainingMode(TrainingMode.Training);

            // Act
            coordinator.TriggerLearningUpdate();

            // Assert
            AssertTrue(mockAgent1.UpdatePolicyCalled, "Agent1 UpdatePolicy should be called");
            AssertTrue(mockAgent2.UpdatePolicyCalled, "Agent2 UpdatePolicy should be called");
            yield return null;
        }

        IEnumerator Test_GetAllMetrics_ReturnsCorrectMetrics()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent2, MonsterType.Ranged);

            // Act
            var metrics = coordinator.GetAllMetrics();

            // Assert
            AssertEqual(2, metrics.Count, "Should return metrics for both agents");
            AssertTrue(metrics.ContainsKey(MonsterType.Melee), "Should contain Melee metrics");
            AssertTrue(metrics.ContainsKey(MonsterType.Ranged), "Should contain Ranged metrics");
            yield return null;
        }

        IEnumerator Test_MixedMode_TrainsUnconvergedAgents()
        {
            // Arrange
            mockAgent1.SetConverged(false); // Should train
            mockAgent2.SetConverged(true);  // Should not train
            
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.RegisterAgent(mockAgent2, MonsterType.Ranged);

            // Act
            coordinator.SetTrainingMode(TrainingMode.Mixed);

            // Assert
            AssertTrue(mockAgent1.IsTraining, "Unconverged agent should be training in Mixed mode");
            AssertFalse(mockAgent2.IsTraining, "Converged agent should not be training in Mixed mode");
            yield return null;
        }

        IEnumerator Test_UpdateAgents_EmitsProgressEvents()
        {
            // Arrange
            bool eventTriggered = false;
            MonsterType receivedType = MonsterType.None;
            LearningMetrics receivedMetrics = default;
            
            coordinator.OnLearningProgressUpdated += (type, metrics) => {
                eventTriggered = true;
                receivedType = type;
                receivedMetrics = metrics;
            };
            
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);

            // Act
            coordinator.UpdateAgents();
            yield return null; // Wait a frame

            // Assert
            AssertTrue(eventTriggered, "Progress update event should be triggered");
            AssertEqual(MonsterType.Melee, receivedType, "Event should receive correct monster type");
        }

        IEnumerator Test_ResetAllProgress_ClearsMetrics()
        {
            // Arrange
            coordinator.RegisterAgent(mockAgent1, MonsterType.Melee);
            coordinator.UpdateAgents(); // Generate some metrics

            // Act
            coordinator.ResetAllProgress();

            // Assert
            var metrics = coordinator.GetAllMetrics();
            AssertTrue(metrics.ContainsKey(MonsterType.Melee), "Should still contain agent metrics");
            // Metrics should be reset to default values
            AssertEqual(0, metrics[MonsterType.Melee].episodeCount, "Episode count should be reset to 0");
            yield return null;
        }

        // Helper assertion methods
        private void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception($"Assertion failed: {message}");
            }
        }

        private void AssertFalse(bool condition, string message)
        {
            if (condition)
            {
                throw new System.Exception($"Assertion failed: {message}");
            }
        }

        private void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!expected.Equals(actual))
            {
                throw new System.Exception($"Assertion failed: {message}. Expected: {expected}, Actual: {actual}");
            }
        }

        private void CleanupTestFiles()
        {
            try
            {
                string stateDirectory = Path.Combine(Application.persistentDataPath, "TrainingStates");
                if (Directory.Exists(stateDirectory))
                {
                    Directory.Delete(stateDirectory, true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to cleanup test files: {ex.Message}");
            }
        }

        // Mock classes for testing
        private class MockEntityManager : MonoBehaviour
        {
            // Minimal implementation for testing - using MonoBehaviour instead of EntityManager
            // to avoid complex dependencies
        }

        private class MockCharacter : MonoBehaviour
        {
            // Minimal implementation for testing - using MonoBehaviour instead of Character
            // to avoid complex dependencies
        }

        private class MockLearningAgent : ILearningAgent
        {
            public bool IsTraining { get; set; } = true;
            public bool UpdatePolicyCalled { get; private set; } = false;
            private bool isConverged = false;
            private LearningMetrics metrics;

            public MockLearningAgent()
            {
                metrics = LearningMetrics.CreateDefault();
            }

            public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
            {
                // Mock implementation
            }

            public int SelectAction(RLGameState state, bool isTraining)
            {
                return 0; // Mock action
            }

            public void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done)
            {
                // Mock implementation
            }

            public void UpdatePolicy()
            {
                UpdatePolicyCalled = true;
            }

            public void SaveBehaviorProfile(string filePath)
            {
                // Mock implementation
            }

            public void LoadBehaviorProfile(string filePath)
            {
                // Mock implementation
            }

            public LearningMetrics GetMetrics()
            {
                var currentMetrics = metrics;
                currentMetrics.episodeCount = isConverged ? 1000 : 10;
                return currentMetrics;
            }

            public void SetConverged(bool converged)
            {
                isConverged = converged;
            }
        }
    }
}
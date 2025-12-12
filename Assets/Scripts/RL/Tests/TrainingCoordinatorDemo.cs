using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vampire;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Demo script showing TrainingCoordinator functionality
    /// Demonstrates training/inference mode management, learning progress tracking, and state preservation
    /// </summary>
    public class TrainingCoordinatorDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool runDemo = false;
        [SerializeField] private float demoInterval = 2f;
        [SerializeField] private bool showDebugInfo = true;
        
        private TrainingCoordinator coordinator;
        private List<DemoLearningAgent> demoAgents;
        private float lastDemoTime;
        private int demoStep = 0;

        void Start()
        {
            if (runDemo)
            {
                StartCoroutine(RunDemo());
            }
        }

        IEnumerator RunDemo()
        {
            Debug.Log("=== TrainingCoordinator Demo Started ===");
            
            // Step 1: Initialize TrainingCoordinator
            yield return StartCoroutine(DemoStep1_Initialize());
            
            // Step 2: Register agents
            yield return StartCoroutine(DemoStep2_RegisterAgents());
            
            // Step 3: Demonstrate training mode
            yield return StartCoroutine(DemoStep3_TrainingMode());
            
            // Step 4: Switch to inference mode
            yield return StartCoroutine(DemoStep4_InferenceMode());
            
            // Step 5: Demonstrate mixed mode
            yield return StartCoroutine(DemoStep5_MixedMode());
            
            // Step 6: Show learning progress tracking
            yield return StartCoroutine(DemoStep6_ProgressTracking());
            
            // Step 7: Demonstrate state preservation
            yield return StartCoroutine(DemoStep7_StatePreservation());
            
            Debug.Log("=== TrainingCoordinator Demo Completed ===");
        }

        IEnumerator DemoStep1_Initialize()
        {
            Debug.Log("Step 1: Initializing TrainingCoordinator");
            
            // Create coordinator
            var coordinatorGO = new GameObject("DemoTrainingCoordinator");
            coordinator = coordinatorGO.AddComponent<TrainingCoordinator>();
            
            // Create mock dependencies
            var mockEntityManager = new GameObject("MockEntityManager").AddComponent<MockEntityManager>();
            var mockPlayer = new GameObject("MockPlayer").AddComponent<MockCharacter>();
            
            // Initialize
            coordinator.Initialize(mockPlayer);
            
            // Subscribe to events
            coordinator.OnTrainingModeChanged += OnTrainingModeChanged;
            coordinator.OnLearningProgressUpdated += OnLearningProgressUpdated;
            
            Debug.Log($"Initial training mode: {coordinator.GetTrainingMode()}");
            Debug.Log($"Is training active: {coordinator.IsTrainingActive}");
            
            yield return new WaitForSeconds(demoInterval);
        }

        IEnumerator DemoStep2_RegisterAgents()
        {
            Debug.Log("Step 2: Registering learning agents");
            
            demoAgents = new List<DemoLearningAgent>();
            
            // Create and register different monster types
            var monsterTypes = new[] { MonsterType.Melee, MonsterType.Ranged, MonsterType.Throwing };
            
            foreach (var monsterType in monsterTypes)
            {
                var agentGO = new GameObject($"DemoAgent_{monsterType}");
                var agent = agentGO.AddComponent<DemoLearningAgent>();
                agent.Initialize(monsterType, ActionSpace.CreateDefault());
                
                coordinator.RegisterAgent(agent, monsterType);
                demoAgents.Add(agent);
                
                Debug.Log($"Registered {monsterType} agent");
            }
            
            var metrics = coordinator.GetAllMetrics();
            Debug.Log($"Total registered agents: {metrics.Count}");
            
            yield return new WaitForSeconds(demoInterval);
        }

        IEnumerator DemoStep3_TrainingMode()
        {
            Debug.Log("Step 3: Demonstrating Training Mode");
            
            coordinator.SetTrainingMode(TrainingMode.Training);
            
            // Simulate some training updates
            for (int i = 0; i < 3; i++)
            {
                coordinator.UpdateAgents();
                coordinator.TriggerLearningUpdate();
                
                Debug.Log($"Training update {i + 1} completed");
                Debug.Log($"Frame time: {coordinator.CurrentFrameTime:F2}ms");
                
                yield return new WaitForSeconds(0.5f);
            }
            
            yield return new WaitForSeconds(demoInterval);
        }

        IEnumerator DemoStep4_InferenceMode()
        {
            Debug.Log("Step 4: Switching to Inference Mode");
            
            coordinator.SetTrainingMode(TrainingMode.Inference);
            
            // Verify agents are no longer training
            foreach (var agent in demoAgents)
            {
                Debug.Log($"{agent.MonsterType} agent training: {agent.IsTraining}");
            }
            
            // Simulate inference updates
            for (int i = 0; i < 2; i++)
            {
                coordinator.UpdateAgents();
                Debug.Log($"Inference update {i + 1} completed");
                yield return new WaitForSeconds(0.5f);
            }
            
            yield return new WaitForSeconds(demoInterval);
        }

        IEnumerator DemoStep5_MixedMode()
        {
            Debug.Log("Step 5: Demonstrating Mixed Mode");
            
            // Set some agents as converged, others as not converged
            if (demoAgents.Count >= 2)
            {
                demoAgents[0].SetConverged(true);  // This one should not train
                demoAgents[1].SetConverged(false); // This one should train
            }
            
            coordinator.SetTrainingMode(TrainingMode.Mixed);
            
            // Check which agents are training
            foreach (var agent in demoAgents)
            {
                Debug.Log($"{agent.MonsterType} agent training in mixed mode: {agent.IsTraining}");
            }
            
            yield return new WaitForSeconds(demoInterval);
        }

        IEnumerator DemoStep6_ProgressTracking()
        {
            Debug.Log("Step 6: Demonstrating Learning Progress Tracking");
            
            coordinator.SetTrainingMode(TrainingMode.Training);
            
            // Simulate learning progress
            foreach (var agent in demoAgents)
            {
                agent.SimulateProgress();
            }
            
            // Update and show metrics
            coordinator.UpdateAgents();
            
            var allMetrics = coordinator.GetAllMetrics();
            foreach (var kvp in allMetrics)
            {
                var type = kvp.Key;
                var metrics = kvp.Value;
                Debug.Log($"{type} - Episodes: {metrics.episodeCount}, Avg Reward: {metrics.averageReward:F2}, Progress: {metrics.GetProgressPercentage():F1}%");
            }
            
            yield return new WaitForSeconds(demoInterval);
        }

        IEnumerator DemoStep7_StatePreservation()
        {
            Debug.Log("Step 7: Demonstrating State Preservation");
            
            // Save current state
            coordinator.SaveAllProfiles();
            Debug.Log("Behavior profiles saved");
            
            // Reset progress
            coordinator.ResetAllProgress();
            Debug.Log("Learning progress reset");
            
            // Load state back
            coordinator.LoadAllProfiles();
            Debug.Log("Behavior profiles loaded");
            
            yield return new WaitForSeconds(demoInterval);
        }

        private void OnTrainingModeChanged(TrainingMode newMode)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[EVENT] Training mode changed to: {newMode}");
            }
        }

        private void OnLearningProgressUpdated(MonsterType monsterType, LearningMetrics metrics)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[EVENT] Progress update for {monsterType}: Episodes={metrics.episodeCount}, Reward={metrics.averageReward:F2}");
            }
        }

        void OnDestroy()
        {
            if (coordinator != null)
            {
                coordinator.OnTrainingModeChanged -= OnTrainingModeChanged;
                coordinator.OnLearningProgressUpdated -= OnLearningProgressUpdated;
            }
        }

        // Mock classes for demo
        public class MockEntityManager : MonoBehaviour
        {
            // Minimal implementation for demo - using MonoBehaviour instead of EntityManager
            // to avoid complex dependencies
        }

        public class MockCharacter : MonoBehaviour
        {
            // Minimal implementation for demo - using MonoBehaviour instead of Character
            // to avoid complex dependencies
        }

        public class DemoLearningAgent : MonoBehaviour, ILearningAgent
        {
            public bool IsTraining { get; set; } = true;
            public MonsterType MonsterType { get; private set; }
            
            private LearningMetrics metrics;
            private bool isConverged = false;
            private int updateCount = 0;

            public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
            {
                MonsterType = monsterType;
                metrics = LearningMetrics.CreateDefault();
            }

            public int SelectAction(RLGameState state, bool isTraining)
            {
                return Random.Range(0, 5); // Random action for demo
            }

            public void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done)
            {
                // Demo implementation
            }

            public void UpdatePolicy()
            {
                updateCount++;
                
                // Simulate learning progress
                if (IsTraining)
                {
                    metrics.episodeCount++;
                    metrics.averageReward += Random.Range(-0.1f, 0.1f);
                    metrics.explorationRate = Mathf.Max(0.01f, metrics.explorationRate - 0.01f);
                }
            }

            public void SaveBehaviorProfile(string filePath)
            {
                // Demo implementation
                Debug.Log($"Saving profile for {MonsterType} to {filePath}");
            }

            public void LoadBehaviorProfile(string filePath)
            {
                // Demo implementation
                Debug.Log($"Loading profile for {MonsterType} from {filePath}");
            }

            public LearningMetrics GetMetrics()
            {
                return metrics;
            }

            public void SetConverged(bool converged)
            {
                isConverged = converged;
                if (converged)
                {
                    metrics.explorationRate = 0.01f;
                    metrics.episodeCount = 1000;
                }
            }

            public void SimulateProgress()
            {
                metrics.episodeCount += Random.Range(10, 50);
                metrics.averageReward += Random.Range(-1f, 2f);
                metrics.explorationRate = Mathf.Max(0.01f, metrics.explorationRate - Random.Range(0.01f, 0.05f));
            }
        }
    }
}
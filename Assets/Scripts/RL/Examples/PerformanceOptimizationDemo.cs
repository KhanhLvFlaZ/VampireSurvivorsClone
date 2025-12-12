using UnityEngine;
using System.Collections;

namespace Vampire.RL.Examples
{
    /// <summary>
    /// Demonstration of the performance optimization and monitoring system
    /// Shows how the system adapts to different performance conditions
    /// </summary>
    public class PerformanceOptimizationDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool runDemo = false;
        [SerializeField] private float demoInterval = 5f;
        [SerializeField] private bool showDebugUI = true;
        
        [Header("Performance Simulation")]
        [SerializeField] private bool simulateHighFrameTime = false;
        [SerializeField] private bool simulateHighMemoryUsage = false;
        [SerializeField] private bool simulateHighAgentCount = false;
        
        private PerformanceMonitor performanceMonitor;
        private PerformanceOptimizationManager optimizationManager;
        private TrainingCoordinator trainingCoordinator;
        private RLSystem rlSystem;
        
        // Demo state
        private float lastDemoTime;
        private int demoPhase = 0;
        private bool demoRunning = false;

        void Start()
        {
            InitializeComponents();
            
            if (runDemo)
            {
                StartCoroutine(RunPerformanceDemo());
            }
        }

        void Update()
        {
            if (showDebugUI)
            {
                UpdatePerformanceSimulation();
            }
        }

        void OnGUI()
        {
            if (!showDebugUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 600));
            GUILayout.Label("Performance Optimization Demo", GUI.skin.box);
            
            // Demo controls
            GUILayout.Space(10);
            GUILayout.Label("Demo Controls:", GUI.skin.box);
            
            if (GUILayout.Button(demoRunning ? "Stop Demo" : "Start Demo"))
            {
                if (demoRunning)
                {
                    StopCoroutine(RunPerformanceDemo());
                    demoRunning = false;
                }
                else
                {
                    StartCoroutine(RunPerformanceDemo());
                }
            }
            
            // Performance simulation controls
            GUILayout.Space(10);
            GUILayout.Label("Performance Simulation:", GUI.skin.box);
            
            simulateHighFrameTime = GUILayout.Toggle(simulateHighFrameTime, "Simulate High Frame Time");
            simulateHighMemoryUsage = GUILayout.Toggle(simulateHighMemoryUsage, "Simulate High Memory Usage");
            simulateHighAgentCount = GUILayout.Toggle(simulateHighAgentCount, "Simulate High Agent Count");
            
            // Current performance metrics
            if (performanceMonitor != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Current Performance:", GUI.skin.box);
                
                var metrics = performanceMonitor.CurrentMetrics;
                GUILayout.Label($"Frame Time: {metrics.frameTimeMs:F1}ms");
                GUILayout.Label($"Memory Usage: {metrics.memoryUsageMB:F1}MB");
                GUILayout.Label($"Active Agents: {metrics.activeAgents}");
                GUILayout.Label($"Batch Size: {metrics.currentBatchSize}");
                GUILayout.Label($"Degradation: {metrics.degradationLevel}");
                GUILayout.Label($"Avg Frame Time: {metrics.averageFrameTime:F1}ms");
                GUILayout.Label($"Peak Memory: {metrics.peakMemoryUsage:F1}MB");
            }
            
            // Optimization status
            if (optimizationManager != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Optimization Status:", GUI.skin.box);
                
                GUILayout.Label($"Strategy: {optimizationManager.CurrentStrategy}");
                GUILayout.Label($"Emergency Mode: {(optimizationManager.IsEmergencyModeActive ? "ACTIVE" : "Inactive")}");
                
                if (GUILayout.Button("Force Optimization"))
                {
                    optimizationManager.ForceOptimization();
                }
                
                if (GUILayout.Button("Reset Optimization"))
                {
                    optimizationManager.ResetOptimization();
                }
            }
            
            // System status
            if (rlSystem != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("RL System Status:", GUI.skin.box);
                
                string status = rlSystem.GetOptimizationStatus();
                string[] statusLines = status.Split(',');
                foreach (string line in statusLines)
                {
                    GUILayout.Label(line.Trim());
                }
            }
            
            GUILayout.EndArea();
        }

        private void InitializeComponents()
        {
            // Find existing components
            rlSystem = FindObjectOfType<RLSystem>();
            performanceMonitor = FindObjectOfType<PerformanceMonitor>();
            optimizationManager = FindObjectOfType<PerformanceOptimizationManager>();
            trainingCoordinator = FindObjectOfType<TrainingCoordinator>();
            
            // Create components if they don't exist
            if (rlSystem == null)
            {
                var rlGO = new GameObject("RLSystem");
                rlSystem = rlGO.AddComponent<RLSystem>();
                rlSystem.Initialize(this, "demo_player");
            }
            
            if (performanceMonitor == null)
            {
                var monitorGO = new GameObject("PerformanceMonitor");
                performanceMonitor = monitorGO.AddComponent<PerformanceMonitor>();
            }
            
            if (optimizationManager == null)
            {
                var optimizationGO = new GameObject("PerformanceOptimizationManager");
                optimizationManager = optimizationGO.AddComponent<PerformanceOptimizationManager>();
            }
            
            if (trainingCoordinator == null)
            {
                var coordinatorGO = new GameObject("TrainingCoordinator");
                trainingCoordinator = coordinatorGO.AddComponent<TrainingCoordinator>();
                trainingCoordinator.Initialize(this);
            }
            
            Debug.Log("Performance Optimization Demo initialized");
        }

        private void UpdatePerformanceSimulation()
        {
            if (performanceMonitor == null) return;
            
            // Simulate different performance conditions
            float frameTime = simulateHighFrameTime ? Random.Range(18f, 25f) : Random.Range(8f, 14f);
            float memoryUsage = simulateHighMemoryUsage ? Random.Range(85f, 120f) : Random.Range(30f, 70f);
            int agentCount = simulateHighAgentCount ? Random.Range(45, 60) : Random.Range(10, 30);
            
            performanceMonitor.UpdateSystemMetrics(frameTime, memoryUsage, agentCount);
            
            // Simulate component performance
            performanceMonitor.RecordComponentPerformance("DemoComponent", Random.Range(2f, 8f));
            performanceMonitor.RecordComponentPerformance("SimulatedAgent", Random.Range(1f, 5f));
        }

        private IEnumerator RunPerformanceDemo()
        {
            demoRunning = true;
            demoPhase = 0;
            
            Debug.Log("Starting Performance Optimization Demo");
            
            while (demoRunning)
            {
                switch (demoPhase)
                {
                    case 0:
                        yield return StartCoroutine(DemoPhase_NormalPerformance());
                        break;
                    case 1:
                        yield return StartCoroutine(DemoPhase_HighFrameTime());
                        break;
                    case 2:
                        yield return StartCoroutine(DemoPhase_HighMemoryUsage());
                        break;
                    case 3:
                        yield return StartCoroutine(DemoPhase_EmergencyConditions());
                        break;
                    case 4:
                        yield return StartCoroutine(DemoPhase_Recovery());
                        break;
                    default:
                        demoPhase = 0;
                        continue;
                }
                
                demoPhase++;
                yield return new WaitForSeconds(1f); // Brief pause between phases
            }
            
            Debug.Log("Performance Optimization Demo completed");
        }

        private IEnumerator DemoPhase_NormalPerformance()
        {
            Debug.Log("Demo Phase 1: Normal Performance");
            
            simulateHighFrameTime = false;
            simulateHighMemoryUsage = false;
            simulateHighAgentCount = false;
            
            yield return new WaitForSeconds(demoInterval);
        }

        private IEnumerator DemoPhase_HighFrameTime()
        {
            Debug.Log("Demo Phase 2: High Frame Time");
            
            simulateHighFrameTime = true;
            simulateHighMemoryUsage = false;
            simulateHighAgentCount = false;
            
            yield return new WaitForSeconds(demoInterval);
        }

        private IEnumerator DemoPhase_HighMemoryUsage()
        {
            Debug.Log("Demo Phase 3: High Memory Usage");
            
            simulateHighFrameTime = false;
            simulateHighMemoryUsage = true;
            simulateHighAgentCount = false;
            
            yield return new WaitForSeconds(demoInterval);
        }

        private IEnumerator DemoPhase_EmergencyConditions()
        {
            Debug.Log("Demo Phase 4: Emergency Conditions");
            
            simulateHighFrameTime = true;
            simulateHighMemoryUsage = true;
            simulateHighAgentCount = true;
            
            yield return new WaitForSeconds(demoInterval);
        }

        private IEnumerator DemoPhase_Recovery()
        {
            Debug.Log("Demo Phase 5: Recovery");
            
            simulateHighFrameTime = false;
            simulateHighMemoryUsage = false;
            simulateHighAgentCount = false;
            
            // Reset optimization to demonstrate recovery
            if (optimizationManager != null)
            {
                optimizationManager.ResetOptimization();
            }
            
            yield return new WaitForSeconds(demoInterval);
        }

        void OnDestroy()
        {
            if (demoRunning)
            {
                StopCoroutine(RunPerformanceDemo());
            }
        }
    }
}
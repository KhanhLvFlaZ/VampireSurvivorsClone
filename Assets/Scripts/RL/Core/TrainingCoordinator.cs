using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Coordinates training across multiple learning agents
    /// Placeholder implementation for task 1 - will be fully implemented in task 8
    /// </summary>
    public class TrainingCoordinator : MonoBehaviour, ITrainingCoordinator
    {
        [Header("Training Settings")]
        [SerializeField] private TrainingMode currentMode = TrainingMode.Training;
        [SerializeField] private float updateInterval = 0.1f; // Update agents every 100ms
        
        private EntityManager entityManager;
        private Character playerCharacter;
        private Dictionary<ILearningAgent, MonsterType> registeredAgents;
        private float lastUpdateTime;
        private float currentFrameTime;

        public event Action<TrainingMode> OnTrainingModeChanged;
        public event Action<MonsterType, LearningMetrics> OnLearningProgressUpdated;

        public bool IsTrainingActive => currentMode == TrainingMode.Training || currentMode == TrainingMode.Mixed;
        public float CurrentFrameTime => currentFrameTime;

        public void Initialize(EntityManager entityManager, Character playerCharacter)
        {
            this.entityManager = entityManager;
            this.playerCharacter = playerCharacter;
            this.registeredAgents = new Dictionary<ILearningAgent, MonsterType>();
            this.lastUpdateTime = Time.time;
            
            Debug.Log("Training Coordinator initialized");
        }

        public void SetTrainingMode(TrainingMode mode)
        {
            if (currentMode != mode)
            {
                currentMode = mode;
                OnTrainingModeChanged?.Invoke(mode);
                
                // Update all registered agents
                foreach (var agent in registeredAgents.Keys)
                {
                    agent.IsTraining = (mode == TrainingMode.Training);
                }
                
                Debug.Log($"Training mode changed to: {mode}");
            }
        }

        public TrainingMode GetTrainingMode()
        {
            return currentMode;
        }

        public void RegisterAgent(ILearningAgent agent, MonsterType monsterType)
        {
            if (agent != null && !registeredAgents.ContainsKey(agent))
            {
                registeredAgents[agent] = monsterType;
                agent.IsTraining = IsTrainingActive;
                Debug.Log($"Registered {monsterType} agent for training");
            }
        }

        public void UnregisterAgent(ILearningAgent agent)
        {
            if (agent != null && registeredAgents.ContainsKey(agent))
            {
                var monsterType = registeredAgents[agent];
                registeredAgents.Remove(agent);
                Debug.Log($"Unregistered {monsterType} agent from training");
            }
        }

        public void UpdateAgents()
        {
            float startTime = Time.realtimeSinceStartup;
            
            // Only update at specified intervals to manage performance
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            lastUpdateTime = Time.time;

            // Update all registered agents
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    // Trigger policy update if training
                    if (agent.IsTraining)
                    {
                        agent.UpdatePolicy();
                    }
                    
                    // Emit progress update event
                    var metrics = agent.GetMetrics();
                    OnLearningProgressUpdated?.Invoke(monsterType, metrics);
                }
            }

            currentFrameTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
        }

        public void TriggerLearningUpdate()
        {
            // Force immediate learning update for all agents
            foreach (var agent in registeredAgents.Keys)
            {
                if (agent != null && agent.IsTraining)
                {
                    agent.UpdatePolicy();
                }
            }
        }

        public void SaveAllProfiles()
        {
            // Placeholder implementation - will be fully implemented in task 7
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    string filePath = GetProfilePath(monsterType);
                    agent.SaveBehaviorProfile(filePath);
                }
            }
            
            Debug.Log("All behavior profiles saved");
        }

        public void LoadAllProfiles()
        {
            // Placeholder implementation - will be fully implemented in task 7
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    string filePath = GetProfilePath(monsterType);
                    if (System.IO.File.Exists(filePath))
                    {
                        agent.LoadBehaviorProfile(filePath);
                    }
                }
            }
            
            Debug.Log("All behavior profiles loaded");
        }

        public Dictionary<MonsterType, LearningMetrics> GetAllMetrics()
        {
            var metrics = new Dictionary<MonsterType, LearningMetrics>();
            
            foreach (var kvp in registeredAgents)
            {
                var agent = kvp.Key;
                var monsterType = kvp.Value;
                
                if (agent != null)
                {
                    metrics[monsterType] = agent.GetMetrics();
                }
            }
            
            return metrics;
        }

        public void ResetAllProgress()
        {
            foreach (var agent in registeredAgents.Keys)
            {
                if (agent != null)
                {
                    // Reset agent (this would need to be implemented in the agent)
                    // For now, just log the action
                    Debug.Log("Reset learning progress (placeholder)");
                }
            }
        }

        private string GetProfilePath(MonsterType monsterType)
        {
            string directory = Application.persistentDataPath + "/BehaviorProfiles";
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            return $"{directory}/{monsterType}_profile.json";
        }

        void OnDestroy()
        {
            // Save all profiles when coordinator is destroyed
            SaveAllProfiles();
        }
    }
}
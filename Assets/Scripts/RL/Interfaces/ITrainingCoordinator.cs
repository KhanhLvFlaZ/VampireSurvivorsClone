using UnityEngine;
using System;
using Vampire;

namespace Vampire.RL
{
    /// <summary>
    /// Interface for coordinating the training process across multiple monsters
    /// </summary>
    public interface ITrainingCoordinator
    {
        /// <summary>
        /// Initialize the training coordinator
        /// </summary>
        /// <param name="playerCharacter">Reference to player character</param>
        void Initialize(MonoBehaviour playerCharacter);

        /// <summary>
        /// Set the current training mode
        /// </summary>
        /// <param name="mode">Training mode to use</param>
        void SetTrainingMode(TrainingMode mode);

        /// <summary>
        /// Get the current training mode
        /// </summary>
        TrainingMode GetTrainingMode();

        /// <summary>
        /// Register a learning agent for coordination
        /// </summary>
        /// <param name="agent">Agent to register</param>
        /// <param name="monsterType">Type of monster this agent controls</param>
        void RegisterAgent(ILearningAgent agent, MonsterType monsterType);

        /// <summary>
        /// Unregister a learning agent
        /// </summary>
        /// <param name="agent">Agent to unregister</param>
        void UnregisterAgent(ILearningAgent agent);

        /// <summary>
        /// Update all registered agents (called each frame)
        /// </summary>
        void UpdateAgents();

        /// <summary>
        /// Trigger learning update for all agents
        /// </summary>
        void TriggerLearningUpdate();

        /// <summary>
        /// Save all behavior profiles
        /// </summary>
        void SaveAllProfiles();

        /// <summary>
        /// Load all behavior profiles
        /// </summary>
        void LoadAllProfiles();

        /// <summary>
        /// Get learning metrics for all agents
        /// </summary>
        /// <returns>Dictionary of metrics by monster type</returns>
        System.Collections.Generic.Dictionary<MonsterType, LearningMetrics> GetAllMetrics();

        /// <summary>
        /// Reset all learning progress
        /// </summary>
        void ResetAllProgress();

        /// <summary>
        /// Event triggered when training mode changes
        /// </summary>
        event Action<TrainingMode> OnTrainingModeChanged;

        /// <summary>
        /// Event triggered when learning progress updates
        /// </summary>
        event Action<MonsterType, LearningMetrics> OnLearningProgressUpdated;

        /// <summary>
        /// Whether training is currently active
        /// </summary>
        bool IsTrainingActive { get; }

        /// <summary>
        /// Current frame processing time for performance monitoring
        /// </summary>
        float CurrentFrameTime { get; }
    }
}
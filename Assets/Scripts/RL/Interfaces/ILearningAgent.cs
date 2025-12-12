using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// Core interface for reinforcement learning agents that can learn and make decisions
    /// </summary>
    public interface ILearningAgent
    {
        /// <summary>
        /// Initialize the learning agent with monster type and action space configuration
        /// </summary>
        /// <param name="monsterType">Type of monster this agent controls</param>
        /// <param name="actionSpace">Available actions for this monster type</param>
        void Initialize(MonsterType monsterType, ActionSpace actionSpace);

        /// <summary>
        /// Select an action based on current game state
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <param name="isTraining">Whether the agent is in training mode</param>
        /// <returns>Action index to execute</returns>
        int SelectAction(RLGameState state, bool isTraining);

        /// <summary>
        /// Store experience for learning
        /// </summary>
        /// <param name="state">Previous state</param>
        /// <param name="action">Action taken</param>
        /// <param name="reward">Reward received</param>
        /// <param name="nextState">Resulting state</param>
        /// <param name="done">Whether episode is complete</param>
        void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done);

        /// <summary>
        /// Update the agent's policy based on stored experiences
        /// </summary>
        void UpdatePolicy();

        /// <summary>
        /// Save learned behavior to file
        /// </summary>
        /// <param name="filePath">Path to save behavior profile</param>
        void SaveBehaviorProfile(string filePath);

        /// <summary>
        /// Load learned behavior from file
        /// </summary>
        /// <param name="filePath">Path to load behavior profile</param>
        void LoadBehaviorProfile(string filePath);

        /// <summary>
        /// Whether the agent is currently in training mode
        /// </summary>
        bool IsTraining { get; set; }

        /// <summary>
        /// Current learning progress metrics
        /// </summary>
        LearningMetrics GetMetrics();
    }
}
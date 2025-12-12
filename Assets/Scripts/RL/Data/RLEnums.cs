using System;

namespace Vampire.RL
{
    /// <summary>
    /// Types of monsters for RL system
    /// </summary>
    public enum MonsterType
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Throwing = 3,
        Boomerang = 4,
        Boss = 5
    }

    /// <summary>
    /// Types of collectibles in the game
    /// </summary>
    public enum CollectibleType
    {
        None = 0,
        ExpGem = 1,
        Coin = 2,
        Chest = 3,
        PowerUp = 4
    }

    /// <summary>
    /// Training modes for the RL system
    /// </summary>
    public enum TrainingMode
    {
        Inference = 0,  // Use learned behavior without updating
        Training = 1,   // Active learning and behavior updates
        Mixed = 2       // Some monsters train, others use inference
    }

    /// <summary>
    /// Learning algorithms supported by the system
    /// </summary>
    public enum LearningAlgorithm
    {
        DQN = 0,        // Deep Q-Network
        DoubleDQN = 1,  // Double Deep Q-Network
        DuelingDQN = 2, // Dueling Deep Q-Network
        A3C = 3,        // Asynchronous Actor-Critic
        PPO = 4         // Proximal Policy Optimization
    }

    /// <summary>
    /// Network architecture types
    /// </summary>
    public enum NetworkArchitecture
    {
        Simple = 0,     // Basic feedforward network
        LSTM = 1,       // Long Short-Term Memory
        CNN = 2,        // Convolutional Neural Network
        Attention = 3   // Attention-based architecture
    }

    /// <summary>
    /// Reward function types
    /// </summary>
    public enum RewardFunctionType
    {
        Sparse = 0,     // Only terminal rewards
        Dense = 1,      // Frequent intermediate rewards
        Shaped = 2,     // Carefully designed reward shaping
        Curiosity = 3   // Intrinsic motivation based rewards
    }
}
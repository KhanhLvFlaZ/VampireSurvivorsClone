using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// Interface for encoding game state into neural network input format
    /// </summary>
    public interface IStateEncoder
    {
        /// <summary>
        /// Encode game state into neural network input vector
        /// </summary>
        /// <param name="gameState">Current game state</param>
        /// <returns>Normalized input vector for neural network</returns>
        float[] EncodeState(RLGameState gameState);

        /// <summary>
        /// Get the size of the encoded state vector
        /// </summary>
        int GetStateSize();

        /// <summary>
        /// Normalize state values for neural network input
        /// </summary>
        /// <param name="rawState">Raw game state values</param>
        /// <returns>Normalized state values</returns>
        float[] NormalizeState(float[] rawState);
    }
}
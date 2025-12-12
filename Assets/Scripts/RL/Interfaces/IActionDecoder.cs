using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// Interface for decoding neural network output into game actions
    /// </summary>
    public interface IActionDecoder
    {
        /// <summary>
        /// Decode neural network output into game action
        /// </summary>
        /// <param name="networkOutput">Output from neural network</param>
        /// <param name="currentState">Current game state for action masking</param>
        /// <returns>Action to execute</returns>
        MonsterAction DecodeAction(float[] networkOutput, RLGameState currentState);

        /// <summary>
        /// Get valid actions for current state (action masking)
        /// </summary>
        /// <param name="currentState">Current game state</param>
        /// <returns>Mask indicating valid actions</returns>
        bool[] GetValidActionMask(RLGameState currentState);

        /// <summary>
        /// Get the total number of possible actions
        /// </summary>
        int GetActionCount();

        /// <summary>
        /// Convert action index to MonsterAction
        /// </summary>
        /// <param name="actionIndex">Index of action to convert</param>
        /// <returns>Corresponding MonsterAction</returns>
        MonsterAction IndexToAction(int actionIndex);
    }
}
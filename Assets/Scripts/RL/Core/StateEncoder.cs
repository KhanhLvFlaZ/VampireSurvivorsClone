using UnityEngine;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Encodes game state into neural network input format
    /// Converts RLGameState into normalized float array for neural network processing
    /// </summary>
    public class StateEncoder : IStateEncoder
    {
        // State size constants based on design document
        private const int PLAYER_STATE_SIZE = 7;  // position(2) + velocity(2) + health(1) + abilities(2 for bit flags)
        private const int MONSTER_STATE_SIZE = 6; // position(2) + health(1) + action(1) + timeSinceAction(1) + timeAlive(1)
        private const int NEARBY_MONSTERS_SIZE = 5 * 4; // 5 monsters * (position(2) + type(1) + health(1))
        private const int NEARBY_COLLECTIBLES_SIZE = 10 * 3; // 10 collectibles * (position(2) + type(1))
        private const int TEMPORAL_STATE_SIZE = 1; // timeSincePlayerDamage(1)
        
        private const int TOTAL_STATE_SIZE = PLAYER_STATE_SIZE + MONSTER_STATE_SIZE + 
                                           NEARBY_MONSTERS_SIZE + NEARBY_COLLECTIBLES_SIZE + TEMPORAL_STATE_SIZE;

        // Normalization constants
        private const float MAX_POSITION_RANGE = 50f; // Assume game world is roughly 100x100 units
        private const float MAX_VELOCITY = 20f; // Maximum expected velocity
        private const float MAX_HEALTH = 200f; // Maximum expected health
        private const float MAX_TIME = 300f; // Maximum expected time values (5 minutes)
        private const float MAX_ABILITIES = 32f; // Maximum number of ability bits

        /// <summary>
        /// Encode game state into neural network input vector
        /// </summary>
        /// <param name="gameState">Current game state</param>
        /// <returns>Normalized input vector for neural network</returns>
        public float[] EncodeState(RLGameState gameState)
        {
            float[] encodedState = new float[TOTAL_STATE_SIZE];
            int index = 0;

            // Encode player state
            index = EncodePlayerState(gameState, encodedState, index);
            
            // Encode monster state
            index = EncodeMonsterState(gameState, encodedState, index);
            
            // Encode nearby monsters
            index = EncodeNearbyMonsters(gameState, encodedState, index);
            
            // Encode nearby collectibles
            index = EncodeNearbyCollectibles(gameState, encodedState, index);
            
            // Encode temporal state
            index = EncodeTemporalState(gameState, encodedState, index);

            // Normalize the entire state
            return NormalizeState(encodedState);
        }

        /// <summary>
        /// Get the size of the encoded state vector
        /// </summary>
        public int GetStateSize()
        {
            return TOTAL_STATE_SIZE;
        }

        /// <summary>
        /// Normalize state values for neural network input
        /// </summary>
        /// <param name="rawState">Raw game state values</param>
        /// <returns>Normalized state values between -1 and 1</returns>
        public float[] NormalizeState(float[] rawState)
        {
            float[] normalizedState = new float[rawState.Length];
            int index = 0;

            // Normalize player state
            index = NormalizePlayerState(rawState, normalizedState, index);
            
            // Normalize monster state
            index = NormalizeMonsterState(rawState, normalizedState, index);
            
            // Normalize nearby monsters
            index = NormalizeNearbyMonsters(rawState, normalizedState, index);
            
            // Normalize nearby collectibles
            index = NormalizeNearbyCollectibles(rawState, normalizedState, index);
            
            // Normalize temporal state
            index = NormalizeTemporalState(rawState, normalizedState, index);

            return normalizedState;
        }

        #region Private Encoding Methods

        private int EncodePlayerState(RLGameState gameState, float[] encodedState, int startIndex)
        {
            int index = startIndex;
            
            // Player position (2D)
            encodedState[index++] = gameState.playerPosition.x;
            encodedState[index++] = gameState.playerPosition.y;
            
            // Player velocity (2D)
            encodedState[index++] = gameState.playerVelocity.x;
            encodedState[index++] = gameState.playerVelocity.y;
            
            // Player health (1D)
            encodedState[index++] = gameState.playerHealth;
            
            // Active abilities (2D - split 32-bit uint into two 16-bit values for better representation)
            encodedState[index++] = (float)(gameState.activeAbilities & 0xFFFF); // Lower 16 bits
            encodedState[index++] = (float)((gameState.activeAbilities >> 16) & 0xFFFF); // Upper 16 bits
            
            return index;
        }

        private int EncodeMonsterState(RLGameState gameState, float[] encodedState, int startIndex)
        {
            int index = startIndex;
            
            // Monster position (2D)
            encodedState[index++] = gameState.monsterPosition.x;
            encodedState[index++] = gameState.monsterPosition.y;
            
            // Monster health (1D)
            encodedState[index++] = gameState.monsterHealth;
            
            // Current action (1D)
            encodedState[index++] = gameState.currentAction;
            
            // Time since last action (1D)
            encodedState[index++] = gameState.timeSinceLastAction;
            
            // Time alive (1D)
            encodedState[index++] = gameState.timeAlive;
            
            return index;
        }

        private int EncodeNearbyMonsters(RLGameState gameState, float[] encodedState, int startIndex)
        {
            int index = startIndex;
            
            for (int i = 0; i < 5; i++)
            {
                if (i < gameState.nearbyMonsters.Length && gameState.nearbyMonsters[i].monsterType != MonsterType.None)
                {
                    var monster = gameState.nearbyMonsters[i];
                    
                    // Monster position (2D)
                    encodedState[index++] = monster.position.x;
                    encodedState[index++] = monster.position.y;
                    
                    // Monster type (1D)
                    encodedState[index++] = (float)monster.monsterType;
                    
                    // Monster health (1D)
                    encodedState[index++] = monster.health;
                }
                else
                {
                    // Empty slot - fill with zeros
                    encodedState[index++] = 0f; // position.x
                    encodedState[index++] = 0f; // position.y
                    encodedState[index++] = 0f; // type
                    encodedState[index++] = 0f; // health
                }
            }
            
            return index;
        }

        private int EncodeNearbyCollectibles(RLGameState gameState, float[] encodedState, int startIndex)
        {
            int index = startIndex;
            
            for (int i = 0; i < 10; i++)
            {
                if (i < gameState.nearbyCollectibles.Length && gameState.nearbyCollectibles[i].collectibleType != CollectibleType.None)
                {
                    var collectible = gameState.nearbyCollectibles[i];
                    
                    // Collectible position (2D)
                    encodedState[index++] = collectible.position.x;
                    encodedState[index++] = collectible.position.y;
                    
                    // Collectible type (1D)
                    encodedState[index++] = (float)collectible.collectibleType;
                }
                else
                {
                    // Empty slot - fill with zeros
                    encodedState[index++] = 0f; // position.x
                    encodedState[index++] = 0f; // position.y
                    encodedState[index++] = 0f; // type
                }
            }
            
            return index;
        }

        private int EncodeTemporalState(RLGameState gameState, float[] encodedState, int startIndex)
        {
            int index = startIndex;
            
            // Time since player damage (1D)
            encodedState[index++] = gameState.timeSincePlayerDamage;
            
            return index;
        }

        #endregion

        #region Private Normalization Methods

        private int NormalizePlayerState(float[] rawState, float[] normalizedState, int startIndex)
        {
            int index = startIndex;
            
            // Normalize player position (-1 to 1 based on MAX_POSITION_RANGE)
            normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
            index++;
            normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
            index++;
            
            // Normalize player velocity (-1 to 1 based on MAX_VELOCITY)
            normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_VELOCITY, -1f, 1f);
            index++;
            normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_VELOCITY, -1f, 1f);
            index++;
            
            // Normalize player health (0 to 1)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / MAX_HEALTH);
            index++;
            
            // Normalize abilities (0 to 1)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / (MAX_ABILITIES / 2)); // Lower 16 bits
            index++;
            normalizedState[index] = Mathf.Clamp01(rawState[index] / (MAX_ABILITIES / 2)); // Upper 16 bits
            index++;
            
            return index;
        }

        private int NormalizeMonsterState(float[] rawState, float[] normalizedState, int startIndex)
        {
            int index = startIndex;
            
            // Normalize monster position (-1 to 1)
            normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
            index++;
            normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
            index++;
            
            // Normalize monster health (0 to 1)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / MAX_HEALTH);
            index++;
            
            // Normalize current action (0 to 1, assuming max 15 actions)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / 15f);
            index++;
            
            // Normalize time since last action (0 to 1)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / MAX_TIME);
            index++;
            
            // Normalize time alive (0 to 1)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / MAX_TIME);
            index++;
            
            return index;
        }

        private int NormalizeNearbyMonsters(float[] rawState, float[] normalizedState, int startIndex)
        {
            int index = startIndex;
            
            for (int i = 0; i < 5; i++)
            {
                // Normalize position (-1 to 1)
                normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
                index++;
                normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
                index++;
                
                // Normalize monster type (0 to 1, assuming max 5 types)
                normalizedState[index] = Mathf.Clamp01(rawState[index] / 5f);
                index++;
                
                // Normalize health (0 to 1)
                normalizedState[index] = Mathf.Clamp01(rawState[index] / MAX_HEALTH);
                index++;
            }
            
            return index;
        }

        private int NormalizeNearbyCollectibles(float[] rawState, float[] normalizedState, int startIndex)
        {
            int index = startIndex;
            
            for (int i = 0; i < 10; i++)
            {
                // Normalize position (-1 to 1)
                normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
                index++;
                normalizedState[index] = Mathf.Clamp(rawState[index] / MAX_POSITION_RANGE, -1f, 1f);
                index++;
                
                // Normalize collectible type (0 to 1, assuming max 4 types)
                normalizedState[index] = Mathf.Clamp01(rawState[index] / 4f);
                index++;
            }
            
            return index;
        }

        private int NormalizeTemporalState(float[] rawState, float[] normalizedState, int startIndex)
        {
            int index = startIndex;
            
            // Normalize time since player damage (0 to 1)
            normalizedState[index] = Mathf.Clamp01(rawState[index] / MAX_TIME);
            index++;
            
            return index;
        }

        #endregion
    }
}
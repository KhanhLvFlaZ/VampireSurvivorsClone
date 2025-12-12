using UnityEngine;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Represents the game state for reinforcement learning
    /// Contains all information needed for monster decision making
    /// </summary>
    [Serializable]
    public struct RLGameState
    {
        [Header("Player State")]
        public Vector2 playerPosition;
        public Vector2 playerVelocity;
        public float playerHealth;
        public uint activeAbilities; // Bit flags for active player abilities
        
        [Header("Monster State")]
        public Vector2 monsterPosition;
        public float monsterHealth;
        public int currentAction;
        public float timeSinceLastAction;
        
        [Header("Environment State")]
        public NearbyMonster[] nearbyMonsters; // Max 5 nearby monsters
        public CollectibleInfo[] nearbyCollectibles; // Max 10 nearby collectibles
        
        [Header("Temporal State")]
        public float timeAlive; // How long this monster has been alive
        public float timeSincePlayerDamage; // Time since monster last damaged player

        /// <summary>
        /// Create a new game state with default values
        /// </summary>
        public static RLGameState CreateDefault()
        {
            return new RLGameState
            {
                playerPosition = Vector2.zero,
                playerVelocity = Vector2.zero,
                playerHealth = 100f,
                activeAbilities = 0,
                monsterPosition = Vector2.zero,
                monsterHealth = 100f,
                currentAction = 0,
                timeSinceLastAction = 0f,
                nearbyMonsters = new NearbyMonster[5],
                nearbyCollectibles = new CollectibleInfo[10],
                timeAlive = 0f,
                timeSincePlayerDamage = float.MaxValue
            };
        }

        /// <summary>
        /// Get distance to player
        /// </summary>
        public float DistanceToPlayer => Vector2.Distance(monsterPosition, playerPosition);

        /// <summary>
        /// Get direction to player (normalized)
        /// </summary>
        public Vector2 DirectionToPlayer => (playerPosition - monsterPosition).normalized;

        /// <summary>
        /// Check if player is moving towards this monster
        /// </summary>
        public bool IsPlayerApproaching => Vector2.Dot(playerVelocity.normalized, DirectionToPlayer) > 0.5f;
    }

    /// <summary>
    /// Information about nearby monsters for coordination
    /// </summary>
    [Serializable]
    public struct NearbyMonster
    {
        public Vector2 position;
        public MonsterType monsterType;
        public float health;
        public int currentAction;

        public static NearbyMonster CreateEmpty()
        {
            return new NearbyMonster
            {
                position = Vector2.zero,
                monsterType = MonsterType.None,
                health = 0f,
                currentAction = -1
            };
        }
    }

    /// <summary>
    /// Information about nearby collectibles
    /// </summary>
    [Serializable]
    public struct CollectibleInfo
    {
        public Vector2 position;
        public CollectibleType collectibleType;
        public float value;

        public static CollectibleInfo CreateEmpty()
        {
            return new CollectibleInfo
            {
                position = Vector2.zero,
                collectibleType = CollectibleType.None,
                value = 0f
            };
        }
    }
}
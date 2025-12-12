using UnityEngine;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Represents an action that a monster can take
    /// </summary>
    [Serializable]
    public struct MonsterAction
    {
        public ActionType actionType;
        public Vector2 direction; // For movement actions
        public float intensity; // For variable intensity actions (0-1)
        public int targetIndex; // For coordination actions

        /// <summary>
        /// Create a movement action
        /// </summary>
        public static MonsterAction CreateMovement(Vector2 direction)
        {
            return new MonsterAction
            {
                actionType = ActionType.Move,
                direction = direction.normalized,
                intensity = 1f,
                targetIndex = -1
            };
        }

        /// <summary>
        /// Create an attack action
        /// </summary>
        public static MonsterAction CreateAttack(float intensity = 1f)
        {
            return new MonsterAction
            {
                actionType = ActionType.Attack,
                direction = Vector2.zero,
                intensity = Mathf.Clamp01(intensity),
                targetIndex = -1
            };
        }

        /// <summary>
        /// Create a retreat action
        /// </summary>
        public static MonsterAction CreateRetreat(Vector2 direction)
        {
            return new MonsterAction
            {
                actionType = ActionType.Retreat,
                direction = direction.normalized,
                intensity = 1f,
                targetIndex = -1
            };
        }

        /// <summary>
        /// Create a coordinate action with another monster
        /// </summary>
        public static MonsterAction CreateCoordinate(int targetMonsterIndex)
        {
            return new MonsterAction
            {
                actionType = ActionType.Coordinate,
                direction = Vector2.zero,
                intensity = 1f,
                targetIndex = targetMonsterIndex
            };
        }

        /// <summary>
        /// Create a wait/idle action
        /// </summary>
        public static MonsterAction CreateWait()
        {
            return new MonsterAction
            {
                actionType = ActionType.Wait,
                direction = Vector2.zero,
                intensity = 0f,
                targetIndex = -1
            };
        }
    }

    /// <summary>
    /// Types of actions monsters can perform
    /// </summary>
    public enum ActionType
    {
        Move = 0,
        Attack = 1,
        Retreat = 2,
        Coordinate = 3,
        Wait = 4,
        SpecialAttack = 5,
        DefensiveStance = 6,
        Ambush = 7
    }

    /// <summary>
    /// Outcome of an action for reward calculation
    /// </summary>
    [Serializable]
    public struct ActionOutcome
    {
        public bool hitPlayer;
        public float damageDealt;
        public bool tookDamage;
        public float damageTaken;
        public bool coordinated; // Successfully coordinated with other monsters
        public float distanceToPlayer; // Final distance to player after action

        public static ActionOutcome CreateDefault()
        {
            return new ActionOutcome
            {
                hitPlayer = false,
                damageDealt = 0f,
                tookDamage = false,
                damageTaken = 0f,
                coordinated = false,
                distanceToPlayer = float.MaxValue
            };
        }
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Defines the action space for a specific monster type
    /// Configurable through ScriptableObjects
    /// </summary>
    [Serializable]
    public class ActionSpace
    {
        [Header("Movement Actions")]
        public bool canMove = true;
        public int movementDirections = 8; // 8-directional movement
        
        [Header("Combat Actions")]
        public bool canAttack = true;
        public bool canSpecialAttack = false;
        public bool canDefend = false;
        
        [Header("Tactical Actions")]
        public bool canRetreat = true;
        public bool canCoordinate = false;
        public bool canAmbush = false;
        public bool canWait = true;

        [Header("Action Constraints")]
        public float minActionInterval = 0.1f; // Minimum time between actions
        public float maxActionRange = 5f; // Maximum range for actions
        
        /// <summary>
        /// Total number of actions in this action space
        /// </summary>
        public int actionCount => GetTotalActionCount();

        /// <summary>
        /// Get all available actions for this action space
        /// </summary>
        public List<ActionType> GetAvailableActions()
        {
            List<ActionType> actions = new List<ActionType>();
            
            if (canMove) actions.Add(ActionType.Move);
            if (canAttack) actions.Add(ActionType.Attack);
            if (canSpecialAttack) actions.Add(ActionType.SpecialAttack);
            if (canDefend) actions.Add(ActionType.DefensiveStance);
            if (canRetreat) actions.Add(ActionType.Retreat);
            if (canCoordinate) actions.Add(ActionType.Coordinate);
            if (canAmbush) actions.Add(ActionType.Ambush);
            if (canWait) actions.Add(ActionType.Wait);
            
            return actions;
        }

        /// <summary>
        /// Get total number of discrete actions
        /// </summary>
        public int GetTotalActionCount()
        {
            int count = 0;
            
            if (canMove) count += movementDirections + 1; // +1 for stop
            if (canAttack) count += 1;
            if (canSpecialAttack) count += 1;
            if (canDefend) count += 1;
            if (canRetreat) count += movementDirections;
            if (canCoordinate) count += 1;
            if (canAmbush) count += 1;
            if (canWait) count += 1;
            
            return count;
        }

        /// <summary>
        /// Create default action space for basic monsters
        /// </summary>
        public static ActionSpace CreateDefault()
        {
            return new ActionSpace
            {
                canMove = true,
                movementDirections = 8,
                canAttack = true,
                canSpecialAttack = false,
                canDefend = false,
                canRetreat = true,
                canCoordinate = false,
                canAmbush = false,
                canWait = true,
                minActionInterval = 0.1f,
                maxActionRange = 5f
            };
        }

        /// <summary>
        /// Create advanced action space for boss monsters
        /// </summary>
        public static ActionSpace CreateAdvanced()
        {
            return new ActionSpace
            {
                canMove = true,
                movementDirections = 8,
                canAttack = true,
                canSpecialAttack = true,
                canDefend = true,
                canRetreat = true,
                canCoordinate = true,
                canAmbush = true,
                canWait = true,
                minActionInterval = 0.05f,
                maxActionRange = 10f
            };
        }
    }
}
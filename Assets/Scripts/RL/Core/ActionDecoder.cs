using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Decodes neural network output into game actions for monsters
    /// Implements action masking to prevent invalid actions
    /// </summary>
    public class ActionDecoder : IActionDecoder
    {
        private ActionSpace actionSpace;
        private MonsterType monsterType;
        private List<ActionMapping> actionMappings;
        
        /// <summary>
        /// Mapping between action indices and actual actions
        /// </summary>
        private struct ActionMapping
        {
            public int index;
            public ActionType actionType;
            public Vector2 direction;
            public float intensity;
            
            public ActionMapping(int index, ActionType actionType, Vector2 direction = default, float intensity = 1f)
            {
                this.index = index;
                this.actionType = actionType;
                this.direction = direction;
                this.intensity = intensity;
            }
        }

        /// <summary>
        /// Initialize the action decoder with specific action space
        /// </summary>
        public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
        {
            this.monsterType = monsterType;
            this.actionSpace = actionSpace;
            BuildActionMappings();
        }

        /// <summary>
        /// Decode neural network output into game action
        /// </summary>
        public MonsterAction DecodeAction(float[] networkOutput, RLGameState currentState)
        {
            if (networkOutput == null || networkOutput.Length == 0)
            {
                return MonsterAction.CreateWait();
            }

            // Get valid action mask
            bool[] validMask = GetValidActionMask(currentState);
            
            // Apply action masking - set invalid actions to very low values
            float[] maskedOutput = new float[networkOutput.Length];
            for (int i = 0; i < networkOutput.Length && i < validMask.Length; i++)
            {
                maskedOutput[i] = validMask[i] ? networkOutput[i] : float.MinValue;
            }

            // Find action with highest value among valid actions
            int selectedIndex = GetMaxIndex(maskedOutput);
            
            // Convert index to action
            return IndexToAction(selectedIndex);
        }

        /// <summary>
        /// Get valid actions for current state (action masking)
        /// </summary>
        public bool[] GetValidActionMask(RLGameState currentState)
        {
            bool[] mask = new bool[GetActionCount()];
            
            for (int i = 0; i < actionMappings.Count && i < mask.Length; i++)
            {
                mask[i] = IsActionValid(actionMappings[i], currentState);
            }
            
            return mask;
        }

        /// <summary>
        /// Get the total number of possible actions
        /// </summary>
        public int GetActionCount()
        {
            return actionMappings?.Count ?? 0;
        }

        /// <summary>
        /// Convert action index to MonsterAction
        /// </summary>
        public MonsterAction IndexToAction(int actionIndex)
        {
            if (actionIndex < 0 || actionIndex >= actionMappings.Count)
            {
                return MonsterAction.CreateWait();
            }

            var mapping = actionMappings[actionIndex];
            
            switch (mapping.actionType)
            {
                case ActionType.Move:
                    return MonsterAction.CreateMovement(mapping.direction);
                    
                case ActionType.Attack:
                    return MonsterAction.CreateAttack(mapping.intensity);
                    
                case ActionType.SpecialAttack:
                    return new MonsterAction
                    {
                        actionType = ActionType.SpecialAttack,
                        direction = Vector2.zero,
                        intensity = mapping.intensity,
                        targetIndex = -1
                    };
                    
                case ActionType.DefensiveStance:
                    return new MonsterAction
                    {
                        actionType = ActionType.DefensiveStance,
                        direction = Vector2.zero,
                        intensity = 1f,
                        targetIndex = -1
                    };
                    
                case ActionType.Retreat:
                    return MonsterAction.CreateRetreat(mapping.direction);
                    
                case ActionType.Coordinate:
                    return MonsterAction.CreateCoordinate(0); // Will be set by coordination system
                    
                case ActionType.Ambush:
                    return new MonsterAction
                    {
                        actionType = ActionType.Ambush,
                        direction = mapping.direction,
                        intensity = 1f,
                        targetIndex = -1
                    };
                    
                case ActionType.Wait:
                default:
                    return MonsterAction.CreateWait();
            }
        }

        /// <summary>
        /// Build the mapping between action indices and actual actions
        /// </summary>
        private void BuildActionMappings()
        {
            actionMappings = new List<ActionMapping>();
            int currentIndex = 0;

            // Movement actions (8 directions + stop)
            if (actionSpace.canMove)
            {
                // 8 directional movement
                for (int i = 0; i < actionSpace.movementDirections; i++)
                {
                    float angle = (i * 360f / actionSpace.movementDirections) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Move, direction));
                }
                
                // Stop/idle movement
                actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Wait));
            }

            // Attack actions
            if (actionSpace.canAttack)
            {
                actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Attack));
            }

            if (actionSpace.canSpecialAttack)
            {
                actionMappings.Add(new ActionMapping(currentIndex++, ActionType.SpecialAttack));
            }

            if (actionSpace.canDefend)
            {
                actionMappings.Add(new ActionMapping(currentIndex++, ActionType.DefensiveStance));
            }

            // Retreat actions (same directions as movement)
            if (actionSpace.canRetreat)
            {
                for (int i = 0; i < actionSpace.movementDirections; i++)
                {
                    float angle = (i * 360f / actionSpace.movementDirections) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Retreat, direction));
                }
            }

            // Tactical actions
            if (actionSpace.canCoordinate)
            {
                actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Coordinate));
            }

            if (actionSpace.canAmbush)
            {
                // Ambush in different directions
                for (int i = 0; i < 4; i++) // 4 cardinal directions for ambush
                {
                    float angle = (i * 90f) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Ambush, direction));
                }
            }

            if (actionSpace.canWait)
            {
                actionMappings.Add(new ActionMapping(currentIndex++, ActionType.Wait));
            }
        }

        /// <summary>
        /// Check if an action is valid in the current state
        /// </summary>
        private bool IsActionValid(ActionMapping mapping, RLGameState currentState)
        {
            switch (mapping.actionType)
            {
                case ActionType.Move:
                    // Always valid unless monster is stunned or in defensive stance
                    return currentState.monsterHealth > 0;

                case ActionType.Attack:
                case ActionType.SpecialAttack:
                    // Valid if player is within range and monster can attack
                    float distanceToPlayer = Vector2.Distance(currentState.monsterPosition, currentState.playerPosition);
                    return distanceToPlayer <= actionSpace.maxActionRange && currentState.monsterHealth > 0;

                case ActionType.DefensiveStance:
                    // Valid if monster is under threat (player nearby or low health)
                    float threatDistance = Vector2.Distance(currentState.monsterPosition, currentState.playerPosition);
                    return threatDistance <= actionSpace.maxActionRange * 1.5f || currentState.monsterHealth < 0.5f;

                case ActionType.Retreat:
                    // Valid if monster is in danger (low health or player very close)
                    float dangerDistance = Vector2.Distance(currentState.monsterPosition, currentState.playerPosition);
                    return dangerDistance <= actionSpace.maxActionRange * 0.5f || currentState.monsterHealth < 0.3f;

                case ActionType.Coordinate:
                    // Valid if there are other monsters nearby
                    return currentState.nearbyMonsters != null && currentState.nearbyMonsters.Length > 0;

                case ActionType.Ambush:
                    // Valid if monster has been alive for a while and player is at medium distance
                    float ambushDistance = Vector2.Distance(currentState.monsterPosition, currentState.playerPosition);
                    return currentState.timeAlive > 2f && 
                           ambushDistance > actionSpace.maxActionRange * 0.5f && 
                           ambushDistance <= actionSpace.maxActionRange * 2f;

                case ActionType.Wait:
                    // Always valid as fallback
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Get index of maximum value in array
        /// </summary>
        private int GetMaxIndex(float[] values)
        {
            if (values == null || values.Length == 0)
                return 0;

            int maxIndex = 0;
            float maxValue = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > maxValue)
                {
                    maxValue = values[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }
    }
}
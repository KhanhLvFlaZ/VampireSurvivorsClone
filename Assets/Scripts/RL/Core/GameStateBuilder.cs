using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Utility class for building RLGameState from actual game objects
    /// Converts Unity game objects and components into RL-compatible data structures
    /// </summary>
    public static class GameStateBuilder
    {
        /// <summary>
        /// Build an RLGameState from current game objects
        /// </summary>
        /// <param name="monster">The monster requesting the state</param>
        /// <param name="player">The player character</param>
        /// <param name="entityManager">Entity manager for finding nearby objects</param>
        /// <param name="maxNearbyMonsters">Maximum number of nearby monsters to include</param>
        /// <param name="maxNearbyCollectibles">Maximum number of nearby collectibles to include</param>
        /// <param name="searchRadius">Radius to search for nearby objects</param>
        /// <returns>Complete RLGameState for the requesting monster</returns>
        public static RLGameState BuildGameState(
            Monster monster, 
            Character player, 
            EntityManager entityManager,
            int maxNearbyMonsters = 5,
            int maxNearbyCollectibles = 10,
            float searchRadius = 15f)
        {
            var gameState = new RLGameState();

            // Build player state
            gameState.playerPosition = player.transform.position;
            gameState.playerVelocity = player.Velocity;
            gameState.playerHealth = player.HP;
            gameState.activeAbilities = GetActiveAbilities(player);

            // Build monster state
            gameState.monsterPosition = monster.transform.position;
            gameState.monsterHealth = monster.HP;
            gameState.currentAction = GetCurrentMonsterAction(monster);
            gameState.timeSinceLastAction = GetTimeSinceLastAction(monster);
            gameState.timeAlive = GetMonsterTimeAlive(monster);
            gameState.timeSincePlayerDamage = GetTimeSincePlayerDamage(monster);

            // Build environment state
            gameState.nearbyMonsters = GetNearbyMonsters(monster, entityManager, maxNearbyMonsters, searchRadius);
            gameState.nearbyCollectibles = GetNearbyCollectibles(monster, entityManager, maxNearbyCollectibles, searchRadius);

            return gameState;
        }

        /// <summary>
        /// Get active abilities as bit flags
        /// </summary>
        private static uint GetActiveAbilities(Character player)
        {
            // This would need to be implemented based on the actual ability system
            // For now, return a placeholder value
            // TODO: Implement actual ability detection when ability system is integrated
            return 0;
        }

        /// <summary>
        /// Get the current action index for a monster
        /// </summary>
        private static int GetCurrentMonsterAction(Monster monster)
        {
            // This would need to be implemented based on the actual monster behavior system
            // For now, return a default action
            // TODO: Implement actual action detection when monster action system is integrated
            return 0;
        }

        /// <summary>
        /// Get time since the monster's last action
        /// </summary>
        private static float GetTimeSinceLastAction(Monster monster)
        {
            // This would need to be implemented based on the actual monster behavior system
            // For now, return a placeholder value
            // TODO: Implement actual timing tracking when monster action system is integrated
            return 0f;
        }

        /// <summary>
        /// Get how long the monster has been alive
        /// </summary>
        private static float GetMonsterTimeAlive(Monster monster)
        {
            // This would need to be implemented based on monster spawn tracking
            // For now, return a placeholder value
            // TODO: Implement actual lifetime tracking when monster lifecycle is integrated
            return 0f;
        }

        /// <summary>
        /// Get time since the monster last damaged the player
        /// </summary>
        private static float GetTimeSincePlayerDamage(Monster monster)
        {
            // This would need to be implemented based on damage tracking
            // For now, return a large value indicating no recent damage
            // TODO: Implement actual damage timing tracking when combat system is integrated
            return float.MaxValue;
        }

        /// <summary>
        /// Get nearby monsters within search radius
        /// </summary>
        private static NearbyMonster[] GetNearbyMonsters(Monster requestingMonster, EntityManager entityManager, int maxCount, float searchRadius)
        {
            var nearbyMonsters = new List<NearbyMonster>();
            Vector2 monsterPos = requestingMonster.transform.position;

            // Get all living monsters from entity manager
            var allMonsters = entityManager.LivingMonsters;
            
            // Find nearby monsters (excluding the requesting monster)
            var nearbyList = allMonsters
                .Where(m => m != requestingMonster)
                .Where(m => Vector2.Distance(m.transform.position, monsterPos) <= searchRadius)
                .OrderBy(m => Vector2.Distance(m.transform.position, monsterPos))
                .Take(maxCount);

            foreach (var monster in nearbyList)
            {
                nearbyMonsters.Add(new NearbyMonster
                {
                    position = monster.transform.position,
                    monsterType = GetMonsterType(monster),
                    health = monster.HP,
                    currentAction = GetCurrentMonsterAction(monster)
                });
            }

            // Fill remaining slots with empty entries
            while (nearbyMonsters.Count < maxCount)
            {
                nearbyMonsters.Add(NearbyMonster.CreateEmpty());
            }

            return nearbyMonsters.ToArray();
        }

        /// <summary>
        /// Get nearby collectibles within search radius
        /// </summary>
        private static CollectibleInfo[] GetNearbyCollectibles(Monster requestingMonster, EntityManager entityManager, int maxCount, float searchRadius)
        {
            var nearbyCollectibles = new List<CollectibleInfo>();
            Vector2 monsterPos = requestingMonster.transform.position;

            // TODO: Implement actual collectible detection when collectible system is integrated
            // For now, create empty collectibles
            // This would need to query the entity manager for active collectibles (gems, coins, etc.)

            // Fill with empty entries for now
            while (nearbyCollectibles.Count < maxCount)
            {
                nearbyCollectibles.Add(CollectibleInfo.CreateEmpty());
            }

            return nearbyCollectibles.ToArray();
        }

        /// <summary>
        /// Determine monster type from monster instance
        /// </summary>
        private static MonsterType GetMonsterType(Monster monster)
        {
            // This would need to be implemented based on the actual monster classification system
            // For now, return a default type
            // TODO: Implement actual monster type detection when monster type system is integrated
            
            // Could be based on monster blueprint, component types, or naming conventions
            string monsterName = monster.name.ToLower();
            
            if (monsterName.Contains("melee"))
                return MonsterType.Melee;
            else if (monsterName.Contains("ranged"))
                return MonsterType.Ranged;
            else if (monsterName.Contains("throwing"))
                return MonsterType.Throwing;
            else if (monsterName.Contains("boomerang"))
                return MonsterType.Boomerang;
            else if (monsterName.Contains("boss"))
                return MonsterType.Boss;
            else
                return MonsterType.Melee; // Default fallback
        }

        /// <summary>
        /// Create a test game state for development and testing
        /// </summary>
        public static RLGameState CreateTestGameState(Vector2 playerPos, Vector2 monsterPos, float playerHealth = 100f, float monsterHealth = 100f)
        {
            return new RLGameState
            {
                playerPosition = playerPos,
                playerVelocity = Vector2.zero,
                playerHealth = playerHealth,
                activeAbilities = 0,
                
                monsterPosition = monsterPos,
                monsterHealth = monsterHealth,
                currentAction = 0,
                timeSinceLastAction = 0f,
                timeAlive = 0f,
                timeSincePlayerDamage = float.MaxValue,
                
                nearbyMonsters = new NearbyMonster[5],
                nearbyCollectibles = new CollectibleInfo[10]
            };
        }
    }
}
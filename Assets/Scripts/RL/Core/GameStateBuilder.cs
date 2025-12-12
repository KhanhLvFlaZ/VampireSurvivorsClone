using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Vampire;

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
            MonoBehaviour monster, 
            MonoBehaviour player, 
            int maxNearbyMonsters = 5,
            int maxNearbyCollectibles = 10,
            float searchRadius = 15f)
        {
            var gameState = new RLGameState();

            // Build player state
            gameState.playerPosition = player.transform.position;
            // gameState.playerVelocity = player.Velocity; // Commented out - property may not exist
            // gameState.playerHealth = player.HP; // Commented out - property may not exist
            gameState.activeAbilities = GetActiveAbilities(player);

            // Build monster state
            gameState.monsterPosition = monster.transform.position;
            // gameState.monsterHealth = monster.HP; // Commented out - property may not exist
            gameState.currentAction = 0; // Default action
            gameState.timeSinceLastAction = 0f; // Default value
            gameState.timeAlive = 0f; // Default value
            gameState.timeSincePlayerDamage = float.MaxValue; // Default value

            // Build environment state
            gameState.nearbyMonsters = GetNearbyMonsters(monster, maxNearbyMonsters, searchRadius);
            gameState.nearbyCollectibles = GetNearbyCollectibles(monster, maxNearbyCollectibles, searchRadius);

            return gameState;
        }

        /// <summary>
        /// Get active abilities as bit flags
        /// </summary>
        private static uint GetActiveAbilities(MonoBehaviour player)
        {
            // This would need to be implemented based on the actual ability system
            // For now, return a placeholder value
            // TODO: Implement actual ability detection when ability system is integrated
            return 0;
        }









        /// <summary>
        /// Get nearby monsters within search radius
        /// </summary>
        private static NearbyMonster[] GetNearbyMonsters(MonoBehaviour requestingMonster, int maxCount, float searchRadius)
        {
            var nearbyMonsters = new List<NearbyMonster>();
            Vector2 monsterPos = requestingMonster.transform.position;

            // Get all monsters using FindObjectsOfType (simplified approach)
            var allMonsters = Object.FindObjectsOfType<MonoBehaviour>()
                .Where(obj => obj.gameObject.CompareTag("Monster") && obj != requestingMonster);
            
            // Find nearby monsters (excluding the requesting monster)
            var nearbyList = allMonsters
                .Where(m => Vector2.Distance(m.transform.position, monsterPos) <= searchRadius)
                .OrderBy(m => Vector2.Distance(m.transform.position, monsterPos))
                .Take(maxCount);

            foreach (var monster in nearbyList)
            {
                nearbyMonsters.Add(new NearbyMonster
                {
                    position = monster.transform.position,
                    monsterType = MonsterType.Melee, // Default type - would need actual monster type detection
                    health = 100f, // Default health - would need actual monster health detection
                    currentAction = 0 // Default action - would need actual monster action detection
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
        private static CollectibleInfo[] GetNearbyCollectibles(MonoBehaviour requestingMonster, int maxCount, float searchRadius)
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
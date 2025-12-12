using UnityEngine;
using UnityEngine.Pool;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Vampire.RL;

namespace Vampire
{
    /// <summary>
    /// Manager class for monsters, monster-dependent objects such as exp gems and coins, chests etc.
    /// </summary>
    public class EntityManager : MonoBehaviour
    {
        [Header("Monster Spawning Settings")]
        [SerializeField] private float monsterSpawnBufferDistance;  // Extra distance outside of the screen view at which monsters should be spawned
        [SerializeField] private float playerDirectionSpawnWeight;  // How much do we weight the player's movement direction in the spawning of monsters
        [Header("Chest Spawning Settings")]
        [SerializeField] private  float chestSpawnRange = 5;
        [Header("Object Pool Settings")]
        [SerializeField] private GameObject monsterPoolParent;
        private MonsterPool[] monsterPools;
        [Header("RL Monster Settings")]
        [SerializeField] private bool enableRLMonsters = true;
        [SerializeField] private RLSystem rlSystem;
        private Dictionary<int, bool> isRLMonsterPool;
        [SerializeField] private GameObject projectilePoolParent;
        private List<ProjectilePool> projectilePools;
        private Dictionary<GameObject, int> projectileIndexByPrefab;
        [SerializeField] private GameObject throwablePoolParent;
        private List<ThrowablePool> throwablePools;
        private Dictionary<GameObject, int> throwableIndexByPrefab;
        [SerializeField] private GameObject boomerangPoolParent;
        private List<BoomerangPool> boomerangPools;
        private Dictionary<GameObject, int> boomerangIndexByPrefab;
        [SerializeField] private GameObject expGemPrefab;
        [SerializeField] private ExpGemPool expGemPool;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private CoinPool coinPool;
        [SerializeField] private GameObject chestPrefab;
        [SerializeField] private ChestPool chestPool;
        [SerializeField] private GameObject textPrefab;
        [SerializeField] private DamageTextPool textPool;
        [Header("Spatial Hash Grid Settings")]
        [SerializeField] private Vector2 gridSize;
        [SerializeField] private Vector2Int gridDimensions;
        [Header("Dependencies")]
        [SerializeField] private SpriteRenderer flashSpriteRenderer;
        [SerializeField] private Camera playerCamera;  // 攝像頭
        private Character playerCharacter;  // 玩家的角色
        private StatsManager statsManager;
        private Inventory inventory;
        private InfiniteBackground infiniteBackground;
        private FastList<Monster> livingMonsters;
        private FastList<Collectable> magneticCollectables;
        public FastList<Chest> chests; 
        private float timeSinceLastMonsterSpawned;
        private float timeSinceLastChestSpawned;
        private float screenWidthWorldSpace, screenHeightWorldSpace, screenDiagonalWorldSpace;
        private float minSpawnDistance;
        private Coroutine flashCoroutine;
        private Coroutine shockwave;
        private SpatialHashGrid grid;
        public FastList<Monster> LivingMonsters { get => livingMonsters; }
        public FastList<Collectable> MagneticCollectables { get => magneticCollectables; }
        public Inventory Inventory { get => inventory; }
        public AbilitySelectionDialog AbilitySelectionDialog { get; private set; }
        public SpatialHashGrid Grid { get => grid; }

        public void Init(LevelBlueprint levelBlueprint, Character character, Inventory inventory, StatsManager statsManager, InfiniteBackground infiniteBackground, AbilitySelectionDialog abilitySelectionDialog)
        {
            this.playerCharacter = character;
            this.inventory = inventory;
            this.infiniteBackground = infiniteBackground;
            this.statsManager = statsManager;
            AbilitySelectionDialog = abilitySelectionDialog;

            // Determine the screen size in world space so that we can spawn enemies outside of it
            Vector2 bottomLeft = playerCamera.ViewportToWorldPoint(new Vector3(0, 0, playerCamera.nearClipPlane));
            Vector2 topRight = playerCamera.ViewportToWorldPoint(new Vector3(1, 1, playerCamera.nearClipPlane));
            screenWidthWorldSpace = topRight.x - bottomLeft.x;
            screenHeightWorldSpace = topRight.y - bottomLeft.y;
            screenDiagonalWorldSpace = (topRight - bottomLeft).magnitude;
            minSpawnDistance = screenDiagonalWorldSpace/2;

            // Init fast lists
            livingMonsters = new FastList<Monster>();
            magneticCollectables = new FastList<Collectable>();
            chests = new FastList<Chest>();
            
            // Initialize RL system if not provided
            if (enableRLMonsters && rlSystem == null)
            {
                rlSystem = FindObjectOfType<RLSystem>();
            }
            
            // Initialize a monster pool for each monster prefab
            monsterPools = new MonsterPool[levelBlueprint.monsters.Length + 1];
            isRLMonsterPool = new Dictionary<int, bool>();
            
            for (int i = 0; i < levelBlueprint.monsters.Length; i++)
            {
                InitializeMonsterPool(i, levelBlueprint.monsters[i].monstersPrefab);
            }
            
            // Initialize boss pool
            InitializeMonsterPool(monsterPools.Length - 1, levelBlueprint.finalBoss.bossPrefab);
            // Initialize a projectile pool for each ranged projectile type
            projectileIndexByPrefab = new Dictionary<GameObject, int>();
            projectilePools = new List<ProjectilePool>();
            // Initialize a throwable pool for each throwable type
            throwableIndexByPrefab = new Dictionary<GameObject, int>();
            throwablePools = new List<ThrowablePool>();
            // Initialize a boomerang pool for each boomerang type
            boomerangIndexByPrefab = new Dictionary<GameObject, int>();
            boomerangPools = new List<BoomerangPool>();
            // Initialize remaining one-off object pools
            expGemPool.Init(this, playerCharacter, expGemPrefab);
            coinPool.Init(this, playerCharacter, coinPrefab);
            chestPool.Init(this, playerCharacter, chestPrefab);
            textPool.Init(this, playerCharacter, textPrefab);

            // Init spatial hash grid
            Vector2[] bounds = new Vector2[] { (Vector2)playerCharacter.transform.position - gridSize/2, (Vector2)playerCharacter.transform.position + gridSize/2 };
            grid = new SpatialHashGrid(bounds, gridDimensions);
        }

        void Update()
        {
            // Rebuild the grid if the player gets close to the edge
            if (grid.CloseToEdge(playerCharacter))
            {
                grid.Rebuild(playerCharacter.transform.position);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Special Functions
        ////////////////////////////////////////////////////////////////////////////////
        public void CollectAllCoinsAndGems()
        {
            if (shockwave != null) StopCoroutine(shockwave);
            shockwave = StartCoroutine(infiniteBackground.Shockwave(screenDiagonalWorldSpace/2));
            foreach (Collectable collectable in magneticCollectables.ToList())
            {
                collectable.Collect();
            }
        }

        public void DamageAllVisibileEnemies(float damage)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(Flash());
            foreach (Monster monster in livingMonsters.ToList() )
            {
                if (TransformOnScreen(monster.transform, Vector2.one))
                    monster.TakeDamage(damage, Vector2.zero);
            }
        }

        public void KillAllMonsters()
        {
            foreach (Monster monster in livingMonsters.ToList() )
            {
                if (!(monster as BossMonster))
                    StartCoroutine(monster.Killed(false));
            }
        }

        private IEnumerator Flash()
        {
            flashSpriteRenderer.enabled = true;
            float t = 0;
            while (t < 1)
            {
                flashSpriteRenderer.color = new Color(1, 1, 1, 1-EasingUtils.EaseOutQuart(t));
                t += Time.unscaledDeltaTime * 4;
                yield return null;
            }
            flashSpriteRenderer.enabled = false;
        }

        public bool TransformOnScreen(Transform transform, Vector2 buffer = default(Vector2))
        {
            return (
                transform.position.x > playerCharacter.transform.position.x - screenWidthWorldSpace/2 - buffer.x &&
                transform.position.x < playerCharacter.transform.position.x + screenWidthWorldSpace/2 + buffer.x &&
                transform.position.y > playerCharacter.transform.position.y - screenHeightWorldSpace/2 - buffer.y &&
                transform.position.y < playerCharacter.transform.position.y + screenHeightWorldSpace/2 + buffer.y
            );
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// RL Monster Pool Management
        ////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// Initialize a monster pool, choosing between regular and RL pool based on prefab
        /// </summary>
        private void InitializeMonsterPool(int poolIndex, GameObject monsterPrefab)
        {
            // Check if this prefab has RL components
            bool isRLMonster = IsRLMonsterPrefab(monsterPrefab);
            isRLMonsterPool[poolIndex] = isRLMonster;
            
            if (isRLMonster && enableRLMonsters)
            {
                // Use specialized RL monster pool
                monsterPools[poolIndex] = monsterPoolParent.AddComponent<RLMonsterPool>();
                Debug.Log($"Created RL monster pool for: {monsterPrefab.name}");
            }
            else
            {
                // Use regular monster pool
                monsterPools[poolIndex] = monsterPoolParent.AddComponent<MonsterPool>();
                Debug.Log($"Created regular monster pool for: {monsterPrefab.name}");
            }
            
            monsterPools[poolIndex].Init(this, playerCharacter, monsterPrefab);
        }
        
        /// <summary>
        /// Check if a monster prefab has RL components
        /// </summary>
        private bool IsRLMonsterPrefab(GameObject prefab)
        {
            if (prefab == null) return false;
            
            // Check for RLMonster component
            var rlMonster = prefab.GetComponent<RLMonster>();
            if (rlMonster != null) return true;
            
            // Check for learning agent components
            var learningAgent = prefab.GetComponent<ILearningAgent>();
            if (learningAgent != null) return true;
            
            // Check for DQN learning agent specifically
            var dqnAgent = prefab.GetComponent<DQNLearningAgent>();
            if (dqnAgent != null) return true;
            
            return false;
        }
        
        /// <summary>
        /// Get RL system reference for monster pools
        /// </summary>
        public RLSystem GetRLSystem()
        {
            return rlSystem;
        }
        
        /// <summary>
        /// Enable or disable RL monsters system-wide
        /// </summary>
        public void SetRLMonstersEnabled(bool enabled)
        {
            enableRLMonsters = enabled;
            
            // Update existing RL monsters
            foreach (Monster monster in livingMonsters.ToList())
            {
                if (monster is RLMonster rlMonster)
                {
                    rlMonster.SetRLEnabled(enabled);
                }
            }
            
            Debug.Log($"RL Monsters {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get statistics for all RL monster pools
        /// </summary>
        public Dictionary<int, RLPoolStatistics> GetRLPoolStatistics()
        {
            var stats = new Dictionary<int, RLPoolStatistics>();
            
            for (int i = 0; i < monsterPools.Length; i++)
            {
                if (isRLMonsterPool.ContainsKey(i) && isRLMonsterPool[i])
                {
                    if (monsterPools[i] is RLMonsterPool rlPool)
                    {
                        stats[i] = rlPool.GetRLStatistics();
                    }
                }
            }
            
            return stats;
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Monster Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public Monster SpawnMonsterRandomPosition(int monsterPoolIndex, MonsterBlueprint monsterBlueprint, float hpBuff = 0)
        {
            // Find a random position offscreen


            Vector2 spawnPosition = (playerCharacter.Velocity != Vector2.zero) ? GetRandomMonsterSpawnPositionPlayerVelocity() : GetRandomMonsterSpawnPosition();
            // Vector2 spawnDirection = Random.insideUnitCircle.normalized;
            // Vector2 spawnPosition = (Vector2)playerCharacter.transform.position + spawnDirection * (minSpawnDistance + monsterSpawnBufferDistance);
            // Spawn the monster
            return SpawnMonster(monsterPoolIndex, spawnPosition, monsterBlueprint, hpBuff);
        }

        public Monster SpawnMonster(int monsterPoolIndex, Vector2 position, MonsterBlueprint monsterBlueprint, float hpBuff = 0)
        {
            Monster newMonster = monsterPools[monsterPoolIndex].Get();
            newMonster.Setup(monsterPoolIndex, position, monsterBlueprint, hpBuff);
            grid.InsertClient(newMonster);
            
            // Additional setup for RL monsters
            if (newMonster is RLMonster rlMonster && enableRLMonsters)
            {
                SetupRLMonster(rlMonster);
            }
            
            return newMonster;
        }
        
        /// <summary>
        /// Spawn an RL monster with specific training configuration
        /// </summary>
        public RLMonster SpawnRLMonster(int monsterPoolIndex, Vector2 position, MonsterBlueprint monsterBlueprint, 
            bool enableTraining = true, float hpBuff = 0)
        {
            if (!enableRLMonsters)
            {
                Debug.LogWarning("RL monsters are disabled, spawning regular monster instead");
                return SpawnMonster(monsterPoolIndex, position, monsterBlueprint, hpBuff) as RLMonster;
            }
            
            Monster monster = SpawnMonster(monsterPoolIndex, position, monsterBlueprint, hpBuff);
            
            if (monster is RLMonster rlMonster)
            {
                // Configure RL-specific settings
                rlMonster.SetTrainingMode(enableTraining);
                return rlMonster;
            }
            else
            {
                Debug.LogWarning($"Monster pool {monsterPoolIndex} does not contain RL monsters");
                return null;
            }
        }
        
        /// <summary>
        /// Setup RL monster with current system configuration
        /// </summary>
        private void SetupRLMonster(RLMonster rlMonster)
        {
            try
            {
                // Ensure RL system is available
                if (rlSystem == null)
                {
                    rlSystem = FindObjectOfType<RLSystem>();
                }
                
                if (rlSystem != null && rlSystem.IsEnabled)
                {
                    // Set training mode based on current system state
                    rlMonster.SetTrainingMode(rlSystem.CurrentTrainingMode == TrainingMode.Training);
                    
                    // Register with RL system if not already registered
                    var learningAgent = rlMonster.GetComponent<ILearningAgent>();
                    if (learningAgent != null)
                    {
                        // Determine monster type (this could be enhanced with better type detection)
                        MonsterType monsterType = DetermineMonsterTypeFromBlueprint(rlMonster);
                        rlSystem.RegisterAgent(learningAgent, monsterType);
                    }
                }
                else
                {
                    // Fallback to inference mode if RL system is not available
                    rlMonster.SetRLEnabled(false);
                    Debug.LogWarning("RL System not available, RL monster will use default behavior");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to setup RL monster: {ex.Message}");
                rlMonster.SetRLEnabled(false);
            }
        }
        
        /// <summary>
        /// Determine monster type from monster blueprint or configuration
        /// </summary>
        private MonsterType DetermineMonsterTypeFromBlueprint(RLMonster rlMonster)
        {
            // This is a simplified approach - in a real implementation, this would
            // be determined from the monster's blueprint or a dedicated configuration
            
            string monsterName = rlMonster.name.ToLower();
            
            if (monsterName.Contains("melee") || monsterName.Contains("warrior"))
                return MonsterType.Melee;
            else if (monsterName.Contains("ranged") || monsterName.Contains("archer"))
                return MonsterType.Ranged;
            else if (monsterName.Contains("boss"))
                return MonsterType.Boss;
            else if (monsterName.Contains("throwing"))
                return MonsterType.Throwing;
            else if (monsterName.Contains("boomerang"))
                return MonsterType.Boomerang;
            
            // Default to Melee if type cannot be determined
            return MonsterType.Melee;
        }

        public void DespawnMonster(int monsterPoolIndex, Monster monster, bool killedByPlayer = true)
        {
            if (killedByPlayer)
            {
                statsManager.IncrementMonstersKilled();
            }
            
            // Handle RL monster cleanup before despawning
            if (monster is RLMonster rlMonster && enableRLMonsters)
            {
                CleanupRLMonster(rlMonster);
            }
            
            grid.RemoveClient(monster);
            monsterPools[monsterPoolIndex].Release(monster);
        }
        
        /// <summary>
        /// Cleanup RL monster before despawning
        /// </summary>
        private void CleanupRLMonster(RLMonster rlMonster)
        {
            try
            {
                // The actual cleanup is handled by the RLMonsterPool
                // This method is here for any additional cleanup that might be needed
                // at the EntityManager level
                
                Debug.Log($"Cleaning up RL monster: {rlMonster.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to cleanup RL monster: {ex.Message}");
            }
        }

        private Vector2 GetRandomMonsterSpawnPosition()
        {
            Vector2[] sideDirections = new Vector2[] { Vector2.left, Vector2.up, Vector2.right, Vector2.down };
            int sideIndex = Random.Range(0,4);
            Vector2 spawnPosition;
            if (sideIndex % 2 == 0)
            {
                spawnPosition = (Vector2)playerCharacter.transform.position + sideDirections[sideIndex] * (screenWidthWorldSpace/2+monsterSpawnBufferDistance) + Vector2.up * Random.Range(-screenHeightWorldSpace/2-monsterSpawnBufferDistance, screenHeightWorldSpace/2+monsterSpawnBufferDistance);
            }
            else
            {
                spawnPosition = (Vector2)playerCharacter.transform.position + sideDirections[sideIndex] * (screenHeightWorldSpace/2+monsterSpawnBufferDistance) + Vector2.right * Random.Range(-screenWidthWorldSpace/2-monsterSpawnBufferDistance, screenWidthWorldSpace/2+monsterSpawnBufferDistance);
            }
            return spawnPosition;
        }

        private Vector2 GetRandomMonsterSpawnPositionPlayerVelocity()
        {
            Vector2[] sideDirections = new Vector2[] { Vector2.left, Vector2.up, Vector2.right, Vector2.down };

            float[] sideWeights = new float[]
            {
                Vector2.Dot(playerCharacter.Velocity.normalized, sideDirections[0]),
                Vector2.Dot(playerCharacter.Velocity.normalized, sideDirections[1]),
                Vector2.Dot(playerCharacter.Velocity.normalized, sideDirections[2]),
                Vector2.Dot(playerCharacter.Velocity.normalized, sideDirections[3])
            };
            float extraWeight = sideWeights.Sum()/playerDirectionSpawnWeight;
            int badSideCount = sideWeights.Where(x => x <= 0).Count();
            for (int i = 0; i < sideWeights.Length; i++)
            {
                if (sideWeights[i] <= 0)
                    sideWeights[i] = extraWeight / badSideCount; 
            }
            float totalSideWeight = sideWeights.Sum();

            float rand = Random.Range(0f, totalSideWeight);
            float cumulative = 0;
            int sideIndex = -1;
            for (int i = 0; i < sideWeights.Length; i++)
            {
                cumulative += sideWeights[i];
                if (rand < cumulative)
                {
                    sideIndex = i;
                    break;
                }
            }

            Vector2 spawnPosition;
            if (sideIndex % 2 == 0)
            {
                spawnPosition = (Vector2)playerCharacter.transform.position + sideDirections[sideIndex] * (screenWidthWorldSpace/2+monsterSpawnBufferDistance) + Vector2.up * Random.Range(-screenHeightWorldSpace/2-monsterSpawnBufferDistance, screenHeightWorldSpace/2+monsterSpawnBufferDistance);
            }
            else
            {
                spawnPosition = (Vector2)playerCharacter.transform.position + sideDirections[sideIndex] * (screenHeightWorldSpace/2+monsterSpawnBufferDistance) + Vector2.right * Random.Range(-screenWidthWorldSpace/2-monsterSpawnBufferDistance, screenWidthWorldSpace/2+monsterSpawnBufferDistance);
            }
            return spawnPosition;
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Exp Gem Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public ExpGem SpawnExpGem(Vector2 position, GemType gemType = GemType.White1, bool spawnAnimation = true)
        {
            ExpGem newGem = expGemPool.Get();
            newGem.Setup(position, gemType, spawnAnimation);
            return newGem;
        }

        public void DespawnGem(ExpGem gem)
        {
            expGemPool.Release(gem);
        }

        public void SpawnGemsAroundPlayer(int gemCount, GemType gemType = GemType.White1)
        {
            for (int i = 0; i < gemCount; i++)
            {
                Vector2 spawnDirection = Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = (Vector2)playerCharacter.transform.position + spawnDirection * Mathf.Sqrt(Random.Range(1, Mathf.Pow(minSpawnDistance, 2)));
                SpawnExpGem(spawnPosition, gemType, false);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Coin Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public Coin SpawnCoin(Vector2 position, CoinType coinType = CoinType.Bronze1, bool spawnAnimation = true)
        {
            Coin newCoin = coinPool.Get();
            newCoin.Setup(position, coinType, spawnAnimation);
            return newCoin;
        }

        public void DespawnCoin(Coin coin, bool pickedUpByPlayer = true)
        {
            if (pickedUpByPlayer)
            {
                statsManager.IncreaseCoinsGained((int)coin.CoinType);
            }
            coinPool.Release(coin);
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Chest Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public Chest SpawnChest(ChestBlueprint chestBlueprint)
        {
            Chest newChest = chestPool.Get();
            newChest.Setup(chestBlueprint);
            // Ensure the chest is not spawned on top of another chest
            bool overlapsOtherChest = false;
            int tries = 0;
            do
            {
                Vector2 spawnDirection = Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = (Vector2)playerCharacter.transform.position + spawnDirection * (minSpawnDistance + monsterSpawnBufferDistance + Random.Range(0, chestSpawnRange));
                newChest.transform.position = spawnPosition;
                overlapsOtherChest = false;
                foreach (Chest chest in chests)
                {
                    if (Vector2.Distance(chest.transform.position, spawnPosition) < 0.5f)
                    {
                        overlapsOtherChest = true;
                        break;
                    }
                }
            } while (overlapsOtherChest && tries++ < 100);
            chests.Add(newChest);
            return newChest;
        }

        public Chest SpawnChest(ChestBlueprint chestBlueprint, Vector2 position)
        {
            Chest newChest = chestPool.Get();
            newChest.transform.position = position;
            newChest.Setup(chestBlueprint);
            chests.Add(newChest);
            return newChest;
        }

        public void DespawnChest(Chest chest)
        {
            chests.Remove(chest);
            chestPool.Release(chest);
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Text Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public DamageText SpawnDamageText(Vector2 position, float damage)
        {
            DamageText newText = textPool.Get();
            newText.Setup(position, damage);
            return newText;
        }

        public void DespawnDamageText(DamageText text)
        {
            textPool.Release(text);
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Projectile Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public Projectile SpawnProjectile(int projectileIndex, Vector2 position, float damage, float knockback, float speed, LayerMask targetLayer)
        {
            Projectile projectile = projectilePools[projectileIndex].Get();
            projectile.Setup(projectileIndex, position, damage, knockback, speed, targetLayer);
            return projectile;
        }
        
        public void DespawnProjectile(int projectileIndex, Projectile projectile)
        {
            projectilePools[projectileIndex].Release(projectile);
        }

        public int AddPoolForProjectile(GameObject projectilePrefab)
        {
            if (!projectileIndexByPrefab.ContainsKey(projectilePrefab))
            {
                projectileIndexByPrefab[projectilePrefab] = projectilePools.Count;
                ProjectilePool projectilePool = projectilePoolParent.AddComponent<ProjectilePool>();
                projectilePool.Init(this, playerCharacter, projectilePrefab);
                projectilePools.Add(projectilePool);
                return projectilePools.Count - 1;
            }
            return projectileIndexByPrefab[projectilePrefab];
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Throwable Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public Throwable SpawnThrowable(int throwableIndex, Vector2 position, float damage, float knockback, float speed, LayerMask targetLayer)
        {
            Throwable throwable = throwablePools[throwableIndex].Get();
            throwable.Setup(throwableIndex, position, damage, knockback, speed, targetLayer);
            return throwable;
        }

        public void DespawnThrowable(int throwableIndex, Throwable throwable)
        {
            throwablePools[throwableIndex].Release(throwable);
        }

        public int AddPoolForThrowable(GameObject throwablePrefab)
        {
            if (!throwableIndexByPrefab.ContainsKey(throwablePrefab))
            {
                throwableIndexByPrefab[throwablePrefab] = throwablePools.Count;
                ThrowablePool throwablePool = throwablePoolParent.AddComponent<ThrowablePool>();
                throwablePool.Init(this, playerCharacter, throwablePrefab);
                throwablePools.Add(throwablePool);
                return throwablePools.Count - 1;
            }
            return throwableIndexByPrefab[throwablePrefab];
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// Boomerang Spawning
        ////////////////////////////////////////////////////////////////////////////////
        public Boomerang SpawnBoomerang(int boomerangIndex, Vector2 position, float damage, float knockback, float throwDistance, float throwTime, LayerMask targetLayer)
        {
            Boomerang boomerang = boomerangPools[boomerangIndex].Get();
            boomerang.Setup(boomerangIndex, position, damage, knockback, throwDistance, throwTime, targetLayer);
            return boomerang;
        }

        public void DespawnBoomerang(int boomerangIndex, Boomerang boomerang)
        {
            boomerangPools[boomerangIndex].Release(boomerang);
        }

        public int AddPoolForBoomerang(GameObject boomerangPrefab)
        {
            if (!boomerangIndexByPrefab.ContainsKey(boomerangPrefab))
            {
                boomerangIndexByPrefab[boomerangPrefab] = boomerangPools.Count;
                BoomerangPool boomerangPool = boomerangPoolParent.AddComponent<BoomerangPool>();
                boomerangPool.Init(this, playerCharacter, boomerangPrefab);
                boomerangPools.Add(boomerangPool);
                return boomerangPools.Count - 1;
            }
            return boomerangIndexByPrefab[boomerangPrefab];
        }

        ////////////////////////////////////////////////////////////////////////////////
        /// RL Monster Management Utilities
        ////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>
        /// Get all active RL monsters
        /// </summary>
        public List<RLMonster> GetActiveRLMonsters()
        {
            var rlMonsters = new List<RLMonster>();
            
            foreach (Monster monster in livingMonsters)
            {
                if (monster is RLMonster rlMonster)
                {
                    rlMonsters.Add(rlMonster);
                }
            }
            
            return rlMonsters;
        }
        
        /// <summary>
        /// Get count of active RL monsters by type
        /// </summary>
        public Dictionary<MonsterType, int> GetRLMonsterCountByType()
        {
            var counts = new Dictionary<MonsterType, int>();
            
            foreach (var rlMonster in GetActiveRLMonsters())
            {
                MonsterType type = DetermineMonsterTypeFromBlueprint(rlMonster);
                counts[type] = counts.ContainsKey(type) ? counts[type] + 1 : 1;
            }
            
            return counts;
        }
        
        /// <summary>
        /// Set training mode for all active RL monsters
        /// </summary>
        public void SetAllRLMonstersTrainingMode(bool training)
        {
            foreach (var rlMonster in GetActiveRLMonsters())
            {
                rlMonster.SetTrainingMode(training);
            }
            
            Debug.Log($"Set training mode to {training} for {GetActiveRLMonsters().Count} RL monsters");
        }
        
        /// <summary>
        /// Get learning metrics for all active RL monsters
        /// </summary>
        public Dictionary<MonsterType, List<LearningMetrics>> GetAllRLMonsterMetrics()
        {
            var metricsByType = new Dictionary<MonsterType, List<LearningMetrics>>();
            
            foreach (var rlMonster in GetActiveRLMonsters())
            {
                MonsterType type = DetermineMonsterTypeFromBlueprint(rlMonster);
                
                if (!metricsByType.ContainsKey(type))
                {
                    metricsByType[type] = new List<LearningMetrics>();
                }
                
                metricsByType[type].Add(rlMonster.GetLearningMetrics());
            }
            
            return metricsByType;
        }
        
        /// <summary>
        /// Force save all RL monster behavior profiles
        /// </summary>
        public void SaveAllRLMonsterProfiles()
        {
            if (rlSystem != null)
            {
                rlSystem.SaveAllProfiles();
            }
            else
            {
                Debug.LogWarning("RL System not available for saving profiles");
            }
        }
        
        /// <summary>
        /// Get comprehensive RL system status
        /// </summary>
        public string GetRLSystemStatus()
        {
            if (!enableRLMonsters)
            {
                return "RL Monsters: Disabled";
            }
            
            if (rlSystem == null)
            {
                return "RL Monsters: Enabled, but RL System not found";
            }
            
            var activeRLMonsters = GetActiveRLMonsters();
            var poolStats = GetRLPoolStatistics();
            
            return $"RL System: {(rlSystem.IsEnabled ? "Active" : "Inactive")}, " +
                   $"Mode: {rlSystem.CurrentTrainingMode}, " +
                   $"Active RL Monsters: {activeRLMonsters.Count}, " +
                   $"RL Pools: {poolStats.Count}";
        }
    }
}

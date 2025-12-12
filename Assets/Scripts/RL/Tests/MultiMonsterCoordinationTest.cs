using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Comprehensive test suite for multi-monster learning and coordination
    /// Tests Requirements 1.3, 2.5, 5.2
    /// </summary>
    public class MultiMonsterCoordinationTest
    {
        private GameObject testGameObject;
        private MonsterCoordinationSystem coordinationSystem;
        private MultiAgentLearningManager multiAgentManager;
        private TrainingCoordinator trainingCoordinator;
        private List<MockLearningAgent> testAgents;

        [SetUp]
        public void SetUp()
        {
            // Create test game object
            testGameObject = new GameObject("MultiMonsterCoordinationTest");
            
            // Initialize coordination system
            coordinationSystem = testGameObject.AddComponent<MonsterCoordinationSystem>();
            
            // Initialize multi-agent learning manager
            multiAgentManager = testGameObject.AddComponent<MultiAgentLearningManager>();
            
            // Initialize training coordinator
            trainingCoordinator = testGameObject.AddComponent<TrainingCoordinator>();
            
            // Create mock entity manager and player
            var mockEntityManager = testGameObject.AddComponent<MockEntityManager>();
            var mockPlayer = testGameObject.AddComponent<MockCharacter>();
            
            trainingCoordinator.Initialize(mockPlayer);
            
            // Create test agents
            testAgents = new List<MockLearningAgent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            testAgents?.Clear();
        }

        /// <summary>
        /// Test independent learning per monster type (Requirement 1.3)
        /// </summary>
        [Test]
        public void TestIndependentLearningPerMonsterType()
        {
            // Create agents of different types
            var meleeAgent1 = CreateMockAgent(MonsterType.Melee, Vector2.zero);
            var meleeAgent2 = CreateMockAgent(MonsterType.Melee, Vector2.one);
            var rangedAgent1 = CreateMockAgent(MonsterType.Ranged, Vector2.up);
            var rangedAgent2 = CreateMockAgent(MonsterType.Ranged, Vector2.right);
            
            // Register agents
            multiAgentManager.RegisterAgent(meleeAgent1, MonsterType.Melee);
            multiAgentManager.RegisterAgent(meleeAgent2, MonsterType.Melee);
            multiAgentManager.RegisterAgent(rangedAgent1, MonsterType.Ranged);
            multiAgentManager.RegisterAgent(rangedAgent2, MonsterType.Ranged);
            
            // Verify independent tracking
            Assert.AreEqual(2, multiAgentManager.GetAgentCount(MonsterType.Melee));
            Assert.AreEqual(2, multiAgentManager.GetAgentCount(MonsterType.Ranged));
            Assert.AreEqual(0, multiAgentManager.GetAgentCount(MonsterType.Boss));
            
            // Simulate learning progress for melee agents
            meleeAgent1.SetMockMetrics(new LearningMetrics 
            { 
                averageReward = 50f, 
                episodeCount = 100,
                lossValue = 0.5f
            });
            meleeAgent2.SetMockMetrics(new LearningMetrics 
            { 
                averageReward = 60f, 
                episodeCount = 120,
                lossValue = 0.4f
            });
            
            // Simulate different learning progress for ranged agents
            rangedAgent1.SetMockMetrics(new LearningMetrics 
            { 
                averageReward = 30f, 
                episodeCount = 80,
                lossValue = 0.7f
            });
            rangedAgent2.SetMockMetrics(new LearningMetrics 
            { 
                averageReward = 35f, 
                episodeCount = 90,
                lossValue = 0.6f
            });
            
            // Update learning
            multiAgentManager.UpdateLearning();
            
            // Verify independent metrics
            var meleeMetrics = multiAgentManager.GetTypeMetrics(MonsterType.Melee);
            var rangedMetrics = multiAgentManager.GetTypeMetrics(MonsterType.Ranged);
            
            Assert.Greater(meleeMetrics.averageReward, rangedMetrics.averageReward);
            Assert.AreNotEqual(meleeMetrics.averageReward, rangedMetrics.averageReward);
            
            Debug.Log($"Melee avg reward: {meleeMetrics.averageReward}, Ranged avg reward: {rangedMetrics.averageReward}");
        }

        /// <summary>
        /// Test coordination group formation (Requirement 5.2)
        /// </summary>
        [Test]
        public void TestCoordinationGroupFormation()
        {
            // Create agents close to each other for coordination
            var agent1 = CreateMockAgent(MonsterType.Melee, Vector2.zero);
            var agent2 = CreateMockAgent(MonsterType.Melee, Vector2.one * 2f); // Within coordination radius
            var agent3 = CreateMockAgent(MonsterType.Melee, Vector2.one * 20f); // Outside coordination radius
            
            // Register agents
            coordinationSystem.RegisterAgent(agent1, MonsterType.Melee);
            coordinationSystem.RegisterAgent(agent2, MonsterType.Melee);
            coordinationSystem.RegisterAgent(agent3, MonsterType.Melee);
            
            // Force coordination update
            coordinationSystem.UpdateCoordinationGroups();
            
            // Verify group formation
            Assert.Greater(coordinationSystem.ActiveGroupCount, 0);
            
            // Check coordination info for agents
            var coord1 = coordinationSystem.GetCoordinationInfo(agent1);
            var coord2 = coordinationSystem.GetCoordinationInfo(agent2);
            var coord3 = coordinationSystem.GetCoordinationInfo(agent3);
            
            // Agents 1 and 2 should be in a group, agent 3 should not
            Assert.IsTrue(coord1.isInGroup);
            Assert.IsTrue(coord2.isInGroup);
            Assert.IsFalse(coord3.isInGroup);
            
            Assert.AreEqual(2, coord1.groupSize);
            Assert.AreEqual(2, coord2.groupSize);
            
            Debug.Log($"Active groups: {coordinationSystem.ActiveGroupCount}");
        }

        /// <summary>
        /// Test group behavior learning capabilities (Requirement 5.2)
        /// </summary>
        [UnityTest]
        public IEnumerator TestGroupBehaviorLearning()
        {
            // Create a coordination group
            var agent1 = CreateMockAgent(MonsterType.Melee, Vector2.zero);
            var agent2 = CreateMockAgent(MonsterType.Melee, Vector2.one);
            
            coordinationSystem.RegisterAgent(agent1, MonsterType.Melee);
            coordinationSystem.RegisterAgent(agent2, MonsterType.Melee);
            
            // Force group formation
            coordinationSystem.ForceCoordination(new List<ILearningAgent> { agent1, agent2 }, MonsterType.Melee);
            
            // Simulate successful coordination
            coordinationSystem.RecordCoordinationSuccess(agent1, true, 10f);
            coordinationSystem.RecordCoordinationSuccess(agent2, true, 15f);
            
            // Wait for learning update
            yield return new WaitForSeconds(0.5f);
            
            // Verify group learning metrics
            var groupMetrics = coordinationSystem.GetAllGroupMetrics();
            Assert.IsTrue(groupMetrics.ContainsKey(MonsterType.Melee));
            
            var meleeGroupMetrics = groupMetrics[MonsterType.Melee];
            Assert.Greater(meleeGroupMetrics.groupCoordinationSuccess, 0f);
            Assert.AreEqual(1, meleeGroupMetrics.activeGroupCount);
            Assert.AreEqual(2f, meleeGroupMetrics.averageGroupSize);
            
            Debug.Log($"Group coordination success: {meleeGroupMetrics.groupCoordinationSuccess}");
        }

        /// <summary>
        /// Test learning isolation between monster types (Requirement 2.5)
        /// </summary>
        [Test]
        public void TestLearningIsolation()
        {
            // Create agents of different types
            var meleeAgent = CreateMockAgent(MonsterType.Melee, Vector2.zero);
            var rangedAgent = CreateMockAgent(MonsterType.Ranged, Vector2.zero);
            
            multiAgentManager.RegisterAgent(meleeAgent, MonsterType.Melee);
            multiAgentManager.RegisterAgent(rangedAgent, MonsterType.Ranged);
            
            // Set high isolation factor
            multiAgentManager.SetLearningIsolation(0.95f);
            
            // Give melee agent high performance
            meleeAgent.SetMockMetrics(new LearningMetrics 
            { 
                averageReward = 100f, 
                episodeCount = 500,
                lossValue = 0.1f
            });
            
            // Give ranged agent low performance
            rangedAgent.SetMockMetrics(new LearningMetrics 
            { 
                averageReward = 10f, 
                episodeCount = 50,
                lossValue = 1.0f
            });
            
            // Update learning multiple times
            for (int i = 0; i < 10; i++)
            {
                multiAgentManager.UpdateLearning();
            }
            
            // Verify isolation - ranged agent should not benefit much from melee success
            var rangedMetrics = multiAgentManager.GetTypeMetrics(MonsterType.Ranged);
            Assert.Less(rangedMetrics.averageReward, 20f); // Should not have improved significantly
            
            Debug.Log($"Ranged metrics after isolation: {rangedMetrics.averageReward}");
        }

        /// <summary>
        /// Test coordination strategy selection
        /// </summary>
        [Test]
        public void TestCoordinationStrategySelection()
        {
            // Test different monster types get appropriate strategies
            var meleeAgents = new List<ILearningAgent>
            {
                CreateMockAgent(MonsterType.Melee, Vector2.zero),
                CreateMockAgent(MonsterType.Melee, Vector2.one),
                CreateMockAgent(MonsterType.Melee, Vector2.up)
            };
            
            var rangedAgents = new List<ILearningAgent>
            {
                CreateMockAgent(MonsterType.Ranged, Vector2.zero),
                CreateMockAgent(MonsterType.Ranged, Vector2.right)
            };
            
            // Register agents
            foreach (var agent in meleeAgents)
            {
                coordinationSystem.RegisterAgent(agent, MonsterType.Melee);
            }
            
            foreach (var agent in rangedAgents)
            {
                coordinationSystem.RegisterAgent(agent, MonsterType.Ranged);
            }
            
            // Force coordination
            coordinationSystem.ForceCoordination(meleeAgents, MonsterType.Melee);
            coordinationSystem.ForceCoordination(rangedAgents, MonsterType.Ranged);
            
            // Check coordination strategies
            var meleeCoord = coordinationSystem.GetCoordinationInfo(meleeAgents[0]);
            var rangedCoord = coordinationSystem.GetCoordinationInfo(rangedAgents[0]);
            
            // Melee with 3 agents should use Surround strategy
            Assert.AreEqual(CoordinationStrategy.Surround, meleeCoord.coordinationStrategy);
            
            // Ranged should use CrossFire strategy
            Assert.AreEqual(CoordinationStrategy.CrossFire, rangedCoord.coordinationStrategy);
            
            Debug.Log($"Melee strategy: {meleeCoord.coordinationStrategy}, Ranged strategy: {rangedCoord.coordinationStrategy}");
        }

        /// <summary>
        /// Test performance constraints with multiple agents
        /// </summary>
        [Test]
        public void TestPerformanceConstraints()
        {
            // Create many agents to test performance limits
            var agents = new List<ILearningAgent>();
            
            for (int i = 0; i < 20; i++)
            {
                var agent = CreateMockAgent(MonsterType.Melee, Vector2.one * i);
                agents.Add(agent);
                multiAgentManager.RegisterAgent(agent, MonsterType.Melee);
                coordinationSystem.RegisterAgent(agent, MonsterType.Melee);
            }
            
            // Measure update time
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 10; i++)
            {
                multiAgentManager.UpdateLearning();
            }
            
            float updateTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
            
            // Should complete within reasonable time (less than 16ms for 60 FPS)
            Assert.Less(updateTime, 100f); // Allow 100ms for test environment
            
            Debug.Log($"Update time for 20 agents: {updateTime}ms");
        }

        private MockLearningAgent CreateMockAgent(MonsterType monsterType, Vector2 position)
        {
            var agentGO = new GameObject($"MockAgent_{monsterType}_{testAgents.Count}");
            agentGO.transform.position = position;
            agentGO.transform.SetParent(testGameObject.transform);
            
            var agent = agentGO.AddComponent<MockLearningAgent>();
            agent.Initialize(monsterType, ActionSpace.CreateDefault());
            
            testAgents.Add(agent);
            return agent;
        }
    }

    /// <summary>
    /// Mock learning agent for testing
    /// </summary>
    public class MockLearningAgent : MonoBehaviour, ILearningAgent
    {
        private MonsterType monsterType;
        private ActionSpace actionSpace;
        private LearningMetrics mockMetrics;
        private bool isTraining = true;

        public bool IsTraining { get => isTraining; set => isTraining = value; }

        public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
        {
            this.monsterType = monsterType;
            this.actionSpace = actionSpace;
            this.mockMetrics = LearningMetrics.CreateDefault();
        }

        public int SelectAction(RLGameState state, bool isTraining)
        {
            return Random.Range(0, actionSpace.actionCount);
        }

        public void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done)
        {
            // Mock implementation
        }

        public void UpdatePolicy()
        {
            // Mock implementation - slightly improve metrics
            mockMetrics.episodeCount++;
            mockMetrics.averageReward += Random.Range(-1f, 2f);
            mockMetrics.lossValue = Mathf.Max(0f, mockMetrics.lossValue - 0.01f);
        }

        public void SaveBehaviorProfile(string filePath)
        {
            // Mock implementation
        }

        public void LoadBehaviorProfile(string filePath)
        {
            // Mock implementation
        }

        public LearningMetrics GetMetrics()
        {
            return mockMetrics;
        }

        public void SetMockMetrics(LearningMetrics metrics)
        {
            mockMetrics = metrics;
        }
    }

    /// <summary>
    /// Mock entity manager for testing
    /// </summary>
    public class MockEntityManager : MonoBehaviour
    {
        // Mock implementation
    }

    /// <summary>
    /// Mock character for testing
    /// </summary>
    public class MockCharacter : MonoBehaviour
    {
        // Mock implementation
    }
}
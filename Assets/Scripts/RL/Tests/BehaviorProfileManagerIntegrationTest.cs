using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Integration test for BehaviorProfileManager with the RL system
    /// Verifies Requirements 4.1, 4.2, 4.4, 4.5 work with existing components
    /// </summary>
    public class BehaviorProfileManagerIntegrationTest : MonoBehaviour
    {
        [Header("Integration Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        private BehaviorProfileManager profileManager;
        private DQNLearningAgent testAgent;

        void Start()
        {
            if (runTestOnStart)
            {
                RunIntegrationTest();
            }
        }

        [ContextMenu("Run Integration Test")]
        public void RunIntegrationTest()
        {
            Debug.Log("=== Behavior Profile Manager Integration Test Started ===");
            
            SetUp();
            
            bool allTestsPassed = true;
            
            allTestsPassed &= TestProfileManagerBasicFunctionality();
            allTestsPassed &= TestProfileManagerWithDQNAgent();
            allTestsPassed &= TestSaveLoadAgentWeights();
            allTestsPassed &= TestMultipleAgentProfiles();
            
            TearDown();
            
            if (allTestsPassed)
            {
                Debug.Log("✅ All integration tests PASSED!");
            }
            else
            {
                Debug.LogError("❌ Some integration tests FAILED!");
            }
            
            Debug.Log("=== Integration Test Completed ===");
        }

        private void SetUp()
        {
            // Initialize profile manager (main focus of integration test)
            profileManager = new BehaviorProfileManager();
            profileManager.Initialize("integration_test_player");
            
            // Create test agent
            testAgent = new DQNLearningAgent();
            testAgent.Initialize(MonsterType.Melee, ActionSpace.CreateDefault());
            
            // Note: RLSystem initialization skipped in test as it requires EntityManager and Character
            // which are not needed for testing BehaviorProfileManager integration
        }

        private void TearDown()
        {
            // Clean up test data
            if (profileManager != null)
            {
                profileManager.CleanupProfiles(0); // Remove all test profiles
            }
        }

        private bool TestProfileManagerBasicFunctionality()
        {
            try
            {
                // Test basic profile manager functionality with realistic data
                var profile = BehaviorProfile.Create(MonsterType.Melee, "integration_test", NetworkArchitecture.Simple);
                
                // Add some realistic data
                profile.networkWeights = new float[100];
                for (int i = 0; i < profile.networkWeights.Length; i++)
                {
                    profile.networkWeights[i] = Random.Range(-1f, 1f);
                }
                
                profile.networkBiases = new float[20];
                for (int i = 0; i < profile.networkBiases.Length; i++)
                {
                    profile.networkBiases[i] = Random.Range(-0.5f, 0.5f);
                }
                
                // Save through profile manager
                bool saveResult = profileManager.SaveProfile(profile);
                
                // Load through profile manager
                var loadedProfile = profileManager.LoadProfile(MonsterType.Melee);
                
                bool result = saveResult && loadedProfile != null && 
                             loadedProfile.networkWeights.Length == profile.networkWeights.Length;
                
                LogTestResult("Profile Manager Basic Functionality", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Profile Manager with RL System", false, ex.Message);
                return false;
            }
        }

        private bool TestProfileManagerWithDQNAgent()
        {
            try
            {
                // Test that profile manager can save/load DQN agent weights
                var gameState = RLGameState.CreateDefault();
                
                // Train agent for a few steps to get some weights
                for (int i = 0; i < 10; i++)
                {
                    int action = testAgent.SelectAction(gameState, true);
                    float reward = Random.Range(-10f, 10f);
                    testAgent.StoreExperience(gameState, action, reward, gameState, false);
                    testAgent.UpdatePolicy();
                }
                
                // Create profile from agent using the profile manager
                var profile = BehaviorProfile.Create(MonsterType.Melee, "dqn_test", NetworkArchitecture.Simple);
                
                // Use a temporary file to get the agent's weights through its save method
                string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "temp_agent_profile.json");
                testAgent.SaveBehaviorProfile(tempPath);
                
                // Load the saved profile to get the weights
                if (System.IO.File.Exists(tempPath))
                {
                    string json = System.IO.File.ReadAllText(tempPath);
                    var tempProfile = JsonUtility.FromJson<BehaviorProfile>(json);
                    profile.networkWeights = tempProfile.networkWeights;
                    profile.networkBiases = tempProfile.networkBiases;
                    profile.trainingEpisodes = 10;
                    profile.averageReward = 5.0f;
                    
                    // Clean up temp file
                    System.IO.File.Delete(tempPath);
                }
                
                // Save profile through profile manager
                bool saveResult = profileManager.SaveProfile(profile);
                
                // Load profile
                var loadedProfile = profileManager.LoadProfile(MonsterType.Melee);
                
                // Verify profile was loaded correctly and has valid weights
                bool result = saveResult && loadedProfile != null && 
                             loadedProfile.networkWeights != null && 
                             loadedProfile.networkWeights.Length > 0 &&
                             loadedProfile.networkBiases != null &&
                             loadedProfile.networkBiases.Length > 0;
                LogTestResult("Profile Manager with DQN Agent", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Profile Manager with DQN Agent", false, ex.Message);
                return false;
            }
        }

        private bool TestSaveLoadAgentWeights()
        {
            try
            {
                // Create two agents
                var agent1 = new DQNLearningAgent();
                agent1.Initialize(MonsterType.Ranged, ActionSpace.CreateDefault());
                
                var agent2 = new DQNLearningAgent();
                agent2.Initialize(MonsterType.Ranged, ActionSpace.CreateDefault());
                
                // Train agent1
                var gameState = RLGameState.CreateDefault();
                for (int i = 0; i < 20; i++)
                {
                    int action = agent1.SelectAction(gameState, true);
                    float reward = Random.Range(-5f, 15f);
                    agent1.StoreExperience(gameState, action, reward, gameState, false);
                    agent1.UpdatePolicy();
                }
                
                // Save agent1's weights through profile manager
                var profile = BehaviorProfile.Create(MonsterType.Ranged, "weight_test", NetworkArchitecture.Simple);
                
                // Use temporary file to extract weights from agent1
                string tempPath1 = System.IO.Path.Combine(Application.temporaryCachePath, "temp_agent1_profile.json");
                agent1.SaveBehaviorProfile(tempPath1);
                
                if (System.IO.File.Exists(tempPath1))
                {
                    string json = System.IO.File.ReadAllText(tempPath1);
                    var tempProfile = JsonUtility.FromJson<BehaviorProfile>(json);
                    profile.networkWeights = tempProfile.networkWeights;
                    profile.networkBiases = tempProfile.networkBiases;
                    System.IO.File.Delete(tempPath1);
                }
                
                profileManager.SaveProfile(profile);
                
                // Load weights into agent2 through profile manager and agent's load method
                var loadedProfile = profileManager.LoadProfile(MonsterType.Ranged);
                string tempPath2 = System.IO.Path.Combine(Application.temporaryCachePath, "temp_agent2_profile.json");
                string profileJson = JsonUtility.ToJson(loadedProfile, true);
                System.IO.File.WriteAllText(tempPath2, profileJson);
                agent2.LoadBehaviorProfile(tempPath2);
                System.IO.File.Delete(tempPath2);
                
                // Test that both agents now behave similarly
                int similarActions = 0;
                for (int i = 0; i < 10; i++)
                {
                    int action1 = agent1.SelectAction(gameState, false); // No exploration
                    int action2 = agent2.SelectAction(gameState, false); // No exploration
                    
                    if (action1 == action2)
                    {
                        similarActions++;
                    }
                }
                
                bool result = similarActions >= 7; // At least 70% similar actions
                LogTestResult("Save/Load Agent Weights", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Save/Load Agent Weights", false, ex.Message);
                return false;
            }
        }

        private bool TestMultipleAgentProfiles()
        {
            try
            {
                // Create profiles for different monster types
                var monsterTypes = new MonsterType[] 
                { 
                    MonsterType.Melee, 
                    MonsterType.Ranged, 
                    MonsterType.Throwing 
                };
                
                // Create and save profiles for each type
                foreach (var monsterType in monsterTypes)
                {
                    var agent = new DQNLearningAgent();
                    agent.Initialize(monsterType, ActionSpace.CreateDefault());
                    
                    // Train briefly
                    var gameState = RLGameState.CreateDefault();
                    for (int i = 0; i < 5; i++)
                    {
                        int action = agent.SelectAction(gameState, true);
                        float reward = Random.Range(0f, 10f);
                        agent.StoreExperience(gameState, action, reward, gameState, false);
                        agent.UpdatePolicy();
                    }
                    
                    // Save profile through profile manager
                    var profile = BehaviorProfile.Create(monsterType, "multi_test", NetworkArchitecture.Simple);
                    
                    // Use temporary file to extract weights from agent
                    string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"temp_{monsterType}_profile.json");
                    agent.SaveBehaviorProfile(tempPath);
                    
                    if (System.IO.File.Exists(tempPath))
                    {
                        string json = System.IO.File.ReadAllText(tempPath);
                        var tempProfile = JsonUtility.FromJson<BehaviorProfile>(json);
                        profile.networkWeights = tempProfile.networkWeights;
                        profile.networkBiases = tempProfile.networkBiases;
                        profile.trainingEpisodes = 5;
                        System.IO.File.Delete(tempPath);
                    }
                    
                    profileManager.SaveProfile(profile);
                }
                
                // Verify all profiles exist and are different
                var allProfiles = profileManager.GetAllProfiles();
                bool result = allProfiles.Count == 3;
                
                // Check that profiles have different weights (indicating they're separate)
                if (result && allProfiles.Count >= 2)
                {
                    var profile1 = allProfiles[0];
                    var profile2 = allProfiles[1];
                    
                    bool weightsAreDifferent = false;
                    for (int i = 0; i < Mathf.Min(profile1.networkWeights.Length, profile2.networkWeights.Length); i++)
                    {
                        if (Mathf.Abs(profile1.networkWeights[i] - profile2.networkWeights[i]) > 0.001f)
                        {
                            weightsAreDifferent = true;
                            break;
                        }
                    }
                    
                    result = weightsAreDifferent;
                }
                
                LogTestResult("Multiple Agent Profiles", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Multiple Agent Profiles", false, ex.Message);
                return false;
            }
        }

        private void LogTestResult(string testName, bool passed, string errorMessage = null)
        {
            if (logDetailedResults)
            {
                if (passed)
                {
                    Debug.Log($"✅ {testName}: PASSED");
                }
                else
                {
                    Debug.LogError($"❌ {testName}: FAILED" + (errorMessage != null ? $" - {errorMessage}" : ""));
                }
            }
        }
    }
}
using UnityEngine;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Interactive demo for BehaviorProfileManager functionality
    /// Shows save/load, compression, and multi-player profile management
    /// </summary>
    public class BehaviorProfileManagerDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private string playerProfileId = "demo_player";
        [SerializeField] private MonsterType demoMonsterType = MonsterType.Melee;
        [SerializeField] private bool autoRunDemo = false;

        private BehaviorProfileManager profileManager;

        void Start()
        {
            profileManager = new BehaviorProfileManager();
            profileManager.Initialize(playerProfileId);
            
            if (autoRunDemo)
            {
                RunDemo();
            }
        }

        [ContextMenu("Run Profile Manager Demo")]
        public void RunDemo()
        {
            Debug.Log("=== Behavior Profile Manager Demo Started ===");
            
            // Demo 1: Create and save a profile
            Debug.Log("\n--- Demo 1: Creating and Saving Profile ---");
            var profile = CreateDemoProfile();
            bool saveResult = profileManager.SaveProfile(profile);
            Debug.Log($"Profile saved: {saveResult}");
            Debug.Log($"Profile size: {profile.GetSizeInBytes()} bytes");
            
            // Demo 2: Load the profile
            Debug.Log("\n--- Demo 2: Loading Profile ---");
            var loadedProfile = profileManager.LoadProfile(demoMonsterType);
            if (loadedProfile != null)
            {
                Debug.Log($"Profile loaded successfully!");
                Debug.Log($"Monster Type: {loadedProfile.monsterType}");
                Debug.Log($"Training Episodes: {loadedProfile.trainingEpisodes}");
                Debug.Log($"Average Reward: {loadedProfile.averageReward}");
                Debug.Log($"Player Profile ID: {loadedProfile.playerProfileId}");
            }
            
            // Demo 3: Profile compression
            Debug.Log("\n--- Demo 3: Profile Compression ---");
            long spaceSaved = profileManager.CompressAllProfiles();
            Debug.Log($"Space saved by compression: {spaceSaved} bytes");
            
            // Demo 4: Multiple profiles
            Debug.Log("\n--- Demo 4: Multiple Profiles ---");
            CreateMultipleProfiles();
            var allProfiles = profileManager.GetAllProfiles();
            Debug.Log($"Total profiles for {playerProfileId}: {allProfiles.Count}");
            foreach (var p in allProfiles)
            {
                Debug.Log($"  - {p.monsterType}: {p.trainingEpisodes} episodes, {p.averageReward:F1} avg reward");
            }
            
            // Demo 5: Storage management
            Debug.Log("\n--- Demo 5: Storage Management ---");
            long totalSize = profileManager.GetStorageSize();
            Debug.Log($"Total storage used: {totalSize} bytes");
            
            // Demo 6: Backup and restore
            Debug.Log("\n--- Demo 6: Backup and Restore ---");
            string backupPath = System.IO.Path.Combine(Application.temporaryCachePath, "demo_backup");
            bool backupResult = profileManager.CreateBackup(backupPath);
            Debug.Log($"Backup created: {backupResult}");
            
            Debug.Log("\n=== Demo Completed ===");
        }

        [ContextMenu("Create Demo Profile")]
        public void CreateAndSaveDemoProfile()
        {
            var profile = CreateDemoProfile();
            bool result = profileManager.SaveProfile(profile);
            Debug.Log($"Demo profile created and saved: {result}");
        }

        [ContextMenu("Load Demo Profile")]
        public void LoadDemoProfile()
        {
            var profile = profileManager.LoadProfile(demoMonsterType);
            if (profile != null)
            {
                Debug.Log($"Loaded profile for {profile.monsterType} with {profile.trainingEpisodes} episodes");
            }
            else
            {
                Debug.Log($"No profile found for {demoMonsterType}");
            }
        }

        [ContextMenu("Show All Profiles")]
        public void ShowAllProfiles()
        {
            var profiles = profileManager.GetAllProfiles();
            Debug.Log($"Found {profiles.Count} profiles:");
            foreach (var profile in profiles)
            {
                Debug.Log($"  {profile.monsterType}: {profile.trainingEpisodes} episodes, {profile.averageReward:F1} reward");
            }
        }

        [ContextMenu("Clean Up Profiles")]
        public void CleanUpProfiles()
        {
            int cleaned = profileManager.CleanupProfiles(0); // Clean all for demo
            Debug.Log($"Cleaned up {cleaned} profiles");
        }

        private BehaviorProfile CreateDemoProfile()
        {
            var profile = BehaviorProfile.Create(demoMonsterType, playerProfileId, NetworkArchitecture.Simple);
            
            // Simulate some training data
            profile.networkWeights = GenerateRandomWeights(100);
            profile.networkBiases = GenerateRandomWeights(20);
            profile.layerSizes = new int[] { 32, 64, 32, 15 };
            profile.inputSize = 32;
            profile.outputSize = 15;
            profile.trainingEpisodes = Random.Range(50, 500);
            profile.averageReward = Random.Range(10f, 100f);
            profile.bestReward = profile.averageReward + Random.Range(10f, 50f);
            profile.explorationRate = Random.Range(0.01f, 0.3f);
            
            return profile;
        }

        private void CreateMultipleProfiles()
        {
            var monsterTypes = new MonsterType[] 
            { 
                MonsterType.Melee, 
                MonsterType.Ranged, 
                MonsterType.Throwing, 
                MonsterType.Boomerang 
            };
            
            foreach (var monsterType in monsterTypes)
            {
                if (!profileManager.ProfileExists(monsterType))
                {
                    var profile = BehaviorProfile.Create(monsterType, playerProfileId, NetworkArchitecture.Simple);
                    profile.networkWeights = GenerateRandomWeights(50);
                    profile.networkBiases = GenerateRandomWeights(10);
                    profile.trainingEpisodes = Random.Range(20, 200);
                    profile.averageReward = Random.Range(5f, 80f);
                    profile.bestReward = profile.averageReward + Random.Range(5f, 30f);
                    
                    profileManager.SaveProfile(profile);
                }
            }
        }

        private float[] GenerateRandomWeights(int count)
        {
            float[] weights = new float[count];
            for (int i = 0; i < count; i++)
            {
                weights[i] = Random.Range(-1f, 1f);
            }
            return weights;
        }

        void OnGUI()
        {
            if (profileManager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("Behavior Profile Manager Demo");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Run Full Demo"))
            {
                RunDemo();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create Demo Profile"))
            {
                CreateAndSaveDemoProfile();
            }
            
            if (GUILayout.Button("Load Demo Profile"))
            {
                LoadDemoProfile();
            }
            
            if (GUILayout.Button("Show All Profiles"))
            {
                ShowAllProfiles();
            }
            
            if (GUILayout.Button("Clean Up Profiles"))
            {
                CleanUpProfiles();
            }
            
            GUILayout.Space(10);
            
            GUILayout.Label($"Current Player: {profileManager.CurrentPlayerProfileId}");
            GUILayout.Label($"Storage Size: {profileManager.GetStorageSize()} bytes");
            
            GUILayout.EndArea();
        }
    }
}
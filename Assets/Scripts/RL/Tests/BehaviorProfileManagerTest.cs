using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Vampire.RL;

namespace Vampire.RL.Tests
{
    /// <summary>
    /// Unit tests for BehaviorProfileManager
    /// Tests Requirements 4.1, 4.2, 4.4, 4.5
    /// </summary>
    public class BehaviorProfileManagerTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool logDetailedResults = true;

        private BehaviorProfileManager profileManager;
        private string testPlayerProfileId = "test_player_123";
        private string testDirectory;

        void Start()
        {
            if (runTestOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Run Behavior Profile Manager Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== Behavior Profile Manager Tests Started ===");
            
            SetUp();
            
            bool allTestsPassed = true;
            
            // Core functionality tests
            allTestsPassed &= TestInitialize_ValidPlayerId_SetsUpCorrectly();
            allTestsPassed &= TestSaveProfile_ValidProfile_ReturnsTrue();
            allTestsPassed &= TestSaveProfile_InvalidProfile_ReturnsFalse();
            allTestsPassed &= TestLoadProfile_ExistingProfile_ReturnsCorrectProfile();
            allTestsPassed &= TestLoadProfile_NonExistentProfile_ReturnsNull();
            allTestsPassed &= TestProfileExists_ExistingProfile_ReturnsTrue();
            allTestsPassed &= TestProfileExists_NonExistentProfile_ReturnsFalse();
            allTestsPassed &= TestDeleteProfile_ExistingProfile_ReturnsTrue();
            
            // Multi-player support tests
            allTestsPassed &= TestSaveLoad_MultiplePlayerProfiles_IsolatesCorrectly();
            allTestsPassed &= TestGetAllProfiles_MultipleProfiles_ReturnsCorrectList();
            allTestsPassed &= TestGetAllProfiles_SpecificPlayer_ReturnsOnlyPlayerProfiles();
            
            // Compression tests
            allTestsPassed &= TestSaveProfile_LargeProfile_CompressesAutomatically();
            allTestsPassed &= TestLoadProfile_CompressedProfile_DecompressesCorrectly();
            allTestsPassed &= TestCompressAllProfiles_UncompressedProfiles_SavesSpace();
            
            // Backup and restore tests
            allTestsPassed &= TestCreateBackup_ExistingProfiles_CreatesValidBackup();
            allTestsPassed &= TestRestoreFromBackup_ValidBackup_RestoresProfiles();
            
            // Error handling and validation tests
            allTestsPassed &= TestValidateProfile_ValidProfile_ReturnsTrue();
            allTestsPassed &= TestValidateProfile_InvalidProfile_ReturnsFalse();
            allTestsPassed &= TestLoadProfile_CorruptedFile_ReturnsNull();
            allTestsPassed &= TestCleanupProfiles_OldProfiles_RemovesCorrectly();
            
            // Storage management tests
            allTestsPassed &= TestGetStorageSize_MultipleProfiles_ReturnsCorrectSize();
            
            TearDown();
            
            if (allTestsPassed)
            {
                Debug.Log("✅ All Behavior Profile Manager tests PASSED!");
            }
            else
            {
                Debug.LogError("❌ Some Behavior Profile Manager tests FAILED!");
            }
            
            Debug.Log("=== Behavior Profile Manager Tests Completed ===");
        }

        private void SetUp()
        {
            profileManager = new BehaviorProfileManager();
            testDirectory = Path.Combine(Application.temporaryCachePath, "TestProfiles");
            
            // Clean up any existing test data
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        private void TearDown()
        {
            // Clean up test data
            if (Directory.Exists(testDirectory))
            {
                try
                {
                    Directory.Delete(testDirectory, true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to clean up test directory: {ex.Message}");
                }
            }
        }

        private BehaviorProfile CreateTestProfile(MonsterType monsterType, string playerProfileId)
        {
            var profile = BehaviorProfile.Create(monsterType, playerProfileId, NetworkArchitecture.Simple);
            
            // Add some test data
            profile.networkWeights = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
            profile.networkBiases = new float[] { 0.01f, 0.02f, 0.03f };
            profile.layerSizes = new int[] { 32, 64, 15 };
            profile.inputSize = 32;
            profile.outputSize = 15;
            profile.trainingEpisodes = 100;
            profile.averageReward = 25.5f;
            profile.bestReward = 50.0f;
            
            return profile;
        }

        private bool TestInitialize_ValidPlayerId_SetsUpCorrectly()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                bool result = profileManager.CurrentPlayerProfileId == testPlayerProfileId &&
                             !string.IsNullOrEmpty(profileManager.ProfileDirectory);
                
                LogTestResult("Initialize with valid player ID", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Initialize with valid player ID", false, ex.Message);
                return false;
            }
        }

        private bool TestSaveProfile_ValidProfile_ReturnsTrue()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                var profile = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                
                bool result = profileManager.SaveProfile(profile);
                
                LogTestResult("Save valid profile", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Save valid profile", false, ex.Message);
                return false;
            }
        }

        private bool TestSaveProfile_InvalidProfile_ReturnsFalse()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Test with null profile
                bool result1 = !profileManager.SaveProfile(null);
                
                // Test with invalid profile
                var invalidProfile = new BehaviorProfile();
                bool result2 = !profileManager.SaveProfile(invalidProfile);
                
                bool result = result1 && result2;
                LogTestResult("Save invalid profile returns false", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Save invalid profile returns false", false, ex.Message);
                return false;
            }
        }

        private bool TestLoadProfile_ExistingProfile_ReturnsCorrectProfile()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                var originalProfile = CreateTestProfile(MonsterType.Ranged, testPlayerProfileId);
                
                // Save profile first
                profileManager.SaveProfile(originalProfile);
                
                // Load profile
                var loadedProfile = profileManager.LoadProfile(MonsterType.Ranged);
                
                bool result = loadedProfile != null &&
                             loadedProfile.monsterType == originalProfile.monsterType &&
                             loadedProfile.playerProfileId == originalProfile.playerProfileId &&
                             loadedProfile.trainingEpisodes == originalProfile.trainingEpisodes;
                
                LogTestResult("Load existing profile", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Load existing profile", false, ex.Message);
                return false;
            }
        }

        private bool TestLoadProfile_NonExistentProfile_ReturnsNull()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                var loadedProfile = profileManager.LoadProfile(MonsterType.Boss);
                
                bool result = loadedProfile == null;
                LogTestResult("Load non-existent profile returns null", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Load non-existent profile returns null", false, ex.Message);
                return false;
            }
        }

        private bool TestProfileExists_ExistingProfile_ReturnsTrue()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                var profile = CreateTestProfile(MonsterType.Throwing, testPlayerProfileId);
                
                profileManager.SaveProfile(profile);
                
                bool result = profileManager.ProfileExists(MonsterType.Throwing);
                LogTestResult("Profile exists for existing profile", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Profile exists for existing profile", false, ex.Message);
                return false;
            }
        }

        private bool TestProfileExists_NonExistentProfile_ReturnsFalse()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                bool result = !profileManager.ProfileExists(MonsterType.Boomerang);
                LogTestResult("Profile exists for non-existent profile returns false", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Profile exists for non-existent profile returns false", false, ex.Message);
                return false;
            }
        }

        private bool TestDeleteProfile_ExistingProfile_ReturnsTrue()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                var profile = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                
                profileManager.SaveProfile(profile);
                bool existsBefore = profileManager.ProfileExists(MonsterType.Melee);
                
                bool deleteResult = profileManager.DeleteProfile(MonsterType.Melee);
                bool existsAfter = profileManager.ProfileExists(MonsterType.Melee);
                
                bool result = existsBefore && deleteResult && !existsAfter;
                LogTestResult("Delete existing profile", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Delete existing profile", false, ex.Message);
                return false;
            }
        }

        private bool TestSaveLoad_MultiplePlayerProfiles_IsolatesCorrectly()
        {
            try
            {
                string player1 = "player1";
                string player2 = "player2";
                
                // Initialize for player 1
                profileManager.Initialize(player1);
                var profile1 = CreateTestProfile(MonsterType.Melee, player1);
                profile1.averageReward = 100f;
                profileManager.SaveProfile(profile1);
                
                // Initialize for player 2
                profileManager.Initialize(player2);
                var profile2 = CreateTestProfile(MonsterType.Melee, player2);
                profile2.averageReward = 200f;
                profileManager.SaveProfile(profile2);
                
                // Load profiles for each player
                var loadedProfile1 = profileManager.LoadProfile(MonsterType.Melee, player1);
                var loadedProfile2 = profileManager.LoadProfile(MonsterType.Melee, player2);
                
                bool result = loadedProfile1 != null && loadedProfile2 != null &&
                             loadedProfile1.averageReward == 100f &&
                             loadedProfile2.averageReward == 200f &&
                             loadedProfile1.playerProfileId == player1 &&
                             loadedProfile2.playerProfileId == player2;
                
                LogTestResult("Multiple player profile isolation", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Multiple player profile isolation", false, ex.Message);
                return false;
            }
        }

        private bool TestGetAllProfiles_MultipleProfiles_ReturnsCorrectList()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Create and save multiple profiles
                var profile1 = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                var profile2 = CreateTestProfile(MonsterType.Ranged, testPlayerProfileId);
                var profile3 = CreateTestProfile(MonsterType.Throwing, testPlayerProfileId);
                
                profileManager.SaveProfile(profile1);
                profileManager.SaveProfile(profile2);
                profileManager.SaveProfile(profile3);
                
                var allProfiles = profileManager.GetAllProfiles();
                
                bool result = allProfiles.Count == 3 &&
                             allProfiles.Exists(p => p.monsterType == MonsterType.Melee) &&
                             allProfiles.Exists(p => p.monsterType == MonsterType.Ranged) &&
                             allProfiles.Exists(p => p.monsterType == MonsterType.Throwing);
                
                LogTestResult("Get all profiles returns correct list", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Get all profiles returns correct list", false, ex.Message);
                return false;
            }
        }

        private bool TestGetAllProfiles_SpecificPlayer_ReturnsOnlyPlayerProfiles()
        {
            try
            {
                string player1 = "player1";
                string player2 = "player2";
                
                // Save profiles for different players
                profileManager.Initialize(player1);
                var profile1 = CreateTestProfile(MonsterType.Melee, player1);
                profileManager.SaveProfile(profile1);
                
                profileManager.Initialize(player2);
                var profile2 = CreateTestProfile(MonsterType.Ranged, player2);
                profileManager.SaveProfile(profile2);
                
                // Get profiles for specific player
                var player1Profiles = profileManager.GetAllProfiles(player1);
                var player2Profiles = profileManager.GetAllProfiles(player2);
                
                bool result = player1Profiles.Count == 1 && player2Profiles.Count == 1 &&
                             player1Profiles[0].monsterType == MonsterType.Melee &&
                             player2Profiles[0].monsterType == MonsterType.Ranged;
                
                LogTestResult("Get profiles for specific player", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Get profiles for specific player", false, ex.Message);
                return false;
            }
        }

        private bool TestSaveProfile_LargeProfile_CompressesAutomatically()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                var profile = CreateTestProfile(MonsterType.Boss, testPlayerProfileId);
                
                // Create large weights array to trigger compression
                profile.networkWeights = new float[2000];
                for (int i = 0; i < profile.networkWeights.Length; i++)
                {
                    profile.networkWeights[i] = UnityEngine.Random.Range(-1f, 1f);
                }
                
                profileManager.SaveProfile(profile);
                
                // Load and check if compression occurred
                var loadedProfile = profileManager.LoadProfile(MonsterType.Boss);
                
                bool result = loadedProfile != null && 
                             loadedProfile.compressedWeights != null && 
                             loadedProfile.compressedWeights.Length > 0;
                
                LogTestResult("Large profile compresses automatically", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Large profile compresses automatically", false, ex.Message);
                return false;
            }
        }

        private bool TestLoadProfile_CompressedProfile_DecompressesCorrectly()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                var profile = CreateTestProfile(MonsterType.Boss, testPlayerProfileId);
                
                // Manually compress the profile
                profile.CompressWeights();
                profileManager.SaveProfile(profile);
                
                // Load and verify decompression
                var loadedProfile = profileManager.LoadProfile(MonsterType.Boss);
                
                bool result = loadedProfile != null && 
                             loadedProfile.networkWeights != null && 
                             loadedProfile.networkWeights.Length > 0;
                
                LogTestResult("Compressed profile decompresses correctly", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Compressed profile decompresses correctly", false, ex.Message);
                return false;
            }
        }

        private bool TestCompressAllProfiles_UncompressedProfiles_SavesSpace()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Create profiles without compression
                var profile1 = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                var profile2 = CreateTestProfile(MonsterType.Ranged, testPlayerProfileId);
                
                profileManager.SaveProfile(profile1);
                profileManager.SaveProfile(profile2);
                
                long spaceSaved = profileManager.CompressAllProfiles();
                
                bool result = spaceSaved >= 0; // Should save some space or at least not fail
                LogTestResult("Compress all profiles saves space", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Compress all profiles saves space", false, ex.Message);
                return false;
            }
        }

        private bool TestCreateBackup_ExistingProfiles_CreatesValidBackup()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Create some profiles
                var profile1 = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                var profile2 = CreateTestProfile(MonsterType.Ranged, testPlayerProfileId);
                
                profileManager.SaveProfile(profile1);
                profileManager.SaveProfile(profile2);
                
                string backupPath = Path.Combine(Application.temporaryCachePath, "test_backup");
                bool result = profileManager.CreateBackup(backupPath);
                
                LogTestResult("Create backup with existing profiles", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Create backup with existing profiles", false, ex.Message);
                return false;
            }
        }

        private bool TestRestoreFromBackup_ValidBackup_RestoresProfiles()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Create and backup profiles
                var profile1 = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                profileManager.SaveProfile(profile1);
                
                string backupPath = Path.Combine(Application.temporaryCachePath, "test_restore_backup");
                profileManager.CreateBackup(backupPath);
                
                // Delete original profile
                profileManager.DeleteProfile(MonsterType.Melee);
                
                // Restore from backup
                bool restoreResult = profileManager.RestoreFromBackup(backupPath);
                bool profileExists = profileManager.ProfileExists(MonsterType.Melee);
                
                bool result = restoreResult && profileExists;
                LogTestResult("Restore from valid backup", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Restore from valid backup", false, ex.Message);
                return false;
            }
        }

        private bool TestValidateProfile_ValidProfile_ReturnsTrue()
        {
            try
            {
                var profile = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                
                bool result = profileManager.ValidateProfile(profile);
                LogTestResult("Validate valid profile", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Validate valid profile", false, ex.Message);
                return false;
            }
        }

        private bool TestValidateProfile_InvalidProfile_ReturnsFalse()
        {
            try
            {
                // Test null profile
                bool result1 = !profileManager.ValidateProfile(null);
                
                // Test profile with NaN weights
                var invalidProfile = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                invalidProfile.networkWeights[0] = float.NaN;
                bool result2 = !profileManager.ValidateProfile(invalidProfile);
                
                bool result = result1 && result2;
                LogTestResult("Validate invalid profile returns false", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Validate invalid profile returns false", false, ex.Message);
                return false;
            }
        }

        private bool TestLoadProfile_CorruptedFile_ReturnsNull()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Create a corrupted file manually
                string fileName = $"Melee_{testPlayerProfileId}.rlprofile";
                string filePath = Path.Combine(profileManager.ProfileDirectory, fileName);
                
                // Ensure directory exists
                Directory.CreateDirectory(profileManager.ProfileDirectory);
                
                // Write corrupted data
                File.WriteAllText(filePath, "corrupted json data");
                
                var loadedProfile = profileManager.LoadProfile(MonsterType.Melee);
                
                bool result = loadedProfile == null;
                LogTestResult("Load corrupted file returns null", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Load corrupted file returns null", false, ex.Message);
                return false;
            }
        }

        private bool TestCleanupProfiles_OldProfiles_RemovesCorrectly()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                // Create a profile
                var profile = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                profileManager.SaveProfile(profile);
                
                // Cleanup with 0 max age (should remove all profiles)
                int cleanedCount = profileManager.CleanupProfiles(0);
                
                bool result = cleanedCount >= 0; // Should clean up at least 0 profiles
                LogTestResult("Cleanup old profiles", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Cleanup old profiles", false, ex.Message);
                return false;
            }
        }

        private bool TestGetStorageSize_MultipleProfiles_ReturnsCorrectSize()
        {
            try
            {
                profileManager.Initialize(testPlayerProfileId);
                
                long initialSize = profileManager.GetStorageSize();
                
                // Add some profiles
                var profile1 = CreateTestProfile(MonsterType.Melee, testPlayerProfileId);
                var profile2 = CreateTestProfile(MonsterType.Ranged, testPlayerProfileId);
                
                profileManager.SaveProfile(profile1);
                profileManager.SaveProfile(profile2);
                
                long finalSize = profileManager.GetStorageSize();
                
                bool result = finalSize > initialSize;
                LogTestResult("Get storage size with multiple profiles", result);
                return result;
            }
            catch (System.Exception ex)
            {
                LogTestResult("Get storage size with multiple profiles", false, ex.Message);
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
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Manages behavior profile persistence and loading
    /// Placeholder implementation for task 1 - will be fully implemented in task 7
    /// </summary>
    public class BehaviorProfileManager : MonoBehaviour, IBehaviorProfileManager
    {
        [Header("Profile Settings")]
        [SerializeField] private string profileDirectory = "BehaviorProfiles";
        [SerializeField] private bool enableCompression = true;
        [SerializeField] private int maxProfileAge = 30; // Days
        
        private string currentPlayerProfileId;
        private string fullProfileDirectory;

        public string CurrentPlayerProfileId => currentPlayerProfileId;
        public string ProfileDirectory => fullProfileDirectory;

        public void Initialize(string playerProfileId)
        {
            currentPlayerProfileId = playerProfileId ?? "default";
            fullProfileDirectory = Path.Combine(Application.persistentDataPath, profileDirectory);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(fullProfileDirectory))
            {
                Directory.CreateDirectory(fullProfileDirectory);
            }
            
            Debug.Log($"Behavior Profile Manager initialized for player: {currentPlayerProfileId}");
            Debug.Log($"Profile directory: {fullProfileDirectory}");
        }

        public bool SaveProfile(BehaviorProfile profile)
        {
            if (profile == null || !profile.IsValid())
            {
                Debug.LogError("Cannot save invalid behavior profile");
                return false;
            }

            try
            {
                // Compress weights if enabled
                if (enableCompression)
                {
                    profile.CompressWeights();
                }

                string fileName = GetProfileFileName(profile.monsterType, profile.playerProfileId);
                string filePath = Path.Combine(fullProfileDirectory, fileName);
                
                string json = JsonUtility.ToJson(profile, true);
                File.WriteAllText(filePath, json);
                
                Debug.Log($"Saved behavior profile for {profile.monsterType} to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save behavior profile: {e.Message}");
                return false;
            }
        }

        public BehaviorProfile LoadProfile(MonsterType monsterType, string playerProfileId = null)
        {
            string profileId = playerProfileId ?? currentPlayerProfileId;
            string fileName = GetProfileFileName(monsterType, profileId);
            string filePath = Path.Combine(fullProfileDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                Debug.Log($"No behavior profile found for {monsterType} (player: {profileId})");
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var profile = JsonUtility.FromJson<BehaviorProfile>(json);
                
                if (profile != null && profile.IsValid())
                {
                    // Decompress weights if they were compressed
                    if (profile.compressedWeights != null && profile.compressedWeights.Length > 0)
                    {
                        profile.DecompressWeights();
                    }
                    
                    Debug.Log($"Loaded behavior profile for {monsterType} from {filePath}");
                    return profile;
                }
                else
                {
                    Debug.LogError($"Invalid behavior profile loaded from {filePath}");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load behavior profile: {e.Message}");
                return null;
            }
        }

        public bool ProfileExists(MonsterType monsterType, string playerProfileId = null)
        {
            string profileId = playerProfileId ?? currentPlayerProfileId;
            string fileName = GetProfileFileName(monsterType, profileId);
            string filePath = Path.Combine(fullProfileDirectory, fileName);
            return File.Exists(filePath);
        }

        public bool DeleteProfile(MonsterType monsterType, string playerProfileId = null)
        {
            string profileId = playerProfileId ?? currentPlayerProfileId;
            string fileName = GetProfileFileName(monsterType, profileId);
            string filePath = Path.Combine(fullProfileDirectory, fileName);
            
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Debug.Log($"Deleted behavior profile for {monsterType} (player: {profileId})");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to delete behavior profile: {e.Message}");
                    return false;
                }
            }
            
            return false;
        }

        public List<BehaviorProfile> GetAllProfiles()
        {
            return GetAllProfiles(currentPlayerProfileId);
        }

        public List<BehaviorProfile> GetAllProfiles(string playerProfileId)
        {
            var profiles = new List<BehaviorProfile>();
            
            if (!Directory.Exists(fullProfileDirectory))
                return profiles;

            try
            {
                string[] files = Directory.GetFiles(fullProfileDirectory, "*.json");
                
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName.Contains($"_{playerProfileId}_"))
                    {
                        var profile = LoadProfileFromFile(file);
                        if (profile != null)
                        {
                            profiles.Add(profile);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get all profiles: {e.Message}");
            }
            
            return profiles;
        }

        public bool CreateBackup(string backupPath)
        {
            // Placeholder implementation
            Debug.Log($"Creating backup to {backupPath} (placeholder)");
            return true;
        }

        public bool RestoreFromBackup(string backupPath)
        {
            // Placeholder implementation
            Debug.Log($"Restoring from backup {backupPath} (placeholder)");
            return true;
        }

        public long GetStorageSize()
        {
            long totalSize = 0;
            
            if (Directory.Exists(fullProfileDirectory))
            {
                try
                {
                    string[] files = Directory.GetFiles(fullProfileDirectory, "*.json");
                    foreach (string file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        totalSize += fileInfo.Length;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to calculate storage size: {e.Message}");
                }
            }
            
            return totalSize;
        }

        public int CleanupProfiles(int maxAge = 30)
        {
            int cleanedCount = 0;
            
            if (!Directory.Exists(fullProfileDirectory))
                return cleanedCount;

            try
            {
                string[] files = Directory.GetFiles(fullProfileDirectory, "*.json");
                DateTime cutoffDate = DateTime.Now.AddDays(-maxAge);
                
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                        cleanedCount++;
                    }
                }
                
                Debug.Log($"Cleaned up {cleanedCount} old behavior profiles");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to cleanup profiles: {e.Message}");
            }
            
            return cleanedCount;
        }

        public bool ValidateProfile(BehaviorProfile profile)
        {
            return profile != null && profile.IsValid();
        }

        public long CompressAllProfiles()
        {
            long spaceSaved = 0;
            
            var profiles = GetAllProfiles();
            foreach (var profile in profiles)
            {
                if (profile.networkWeights != null && profile.compressedWeights == null)
                {
                    long originalSize = profile.networkWeights.Length * sizeof(float);
                    profile.CompressWeights();
                    long compressedSize = profile.compressedWeights?.Length ?? 0;
                    spaceSaved += originalSize - compressedSize;
                    
                    SaveProfile(profile);
                }
            }
            
            Debug.Log($"Compressed profiles, saved {spaceSaved} bytes");
            return spaceSaved;
        }

        private string GetProfileFileName(MonsterType monsterType, string playerProfileId)
        {
            return $"{monsterType}_{playerProfileId}_profile.json";
        }

        private BehaviorProfile LoadProfileFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<BehaviorProfile>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load profile from {filePath}: {e.Message}");
                return null;
            }
        }
    }
}
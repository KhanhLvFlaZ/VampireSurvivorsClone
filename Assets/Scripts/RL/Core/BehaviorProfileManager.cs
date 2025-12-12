using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Vampire.RL
{
    /// <summary>
    /// Manages behavior profile persistence, compression, and multi-player support
    /// Implements Requirements 4.1, 4.2, 4.4, 4.5
    /// </summary>
    public class BehaviorProfileManager : IBehaviorProfileManager
    {
        private const string PROFILE_DIRECTORY = "BehaviorProfiles";
        private const string PROFILE_EXTENSION = ".rlprofile";
        private const string BACKUP_EXTENSION = ".backup";
        private const int COMPRESSION_THRESHOLD = 1024; // Compress profiles larger than 1KB
        
        private string currentPlayerProfileId;
        private string profileDirectory;
        private Dictionary<string, BehaviorProfile> profileCache;
        
        public string CurrentPlayerProfileId => currentPlayerProfileId;
        public string ProfileDirectory => profileDirectory;

        public BehaviorProfileManager()
        {
            profileCache = new Dictionary<string, BehaviorProfile>();
        }

        public void Initialize(string playerProfileId)
        {
            currentPlayerProfileId = playerProfileId ?? "default";
            profileDirectory = Path.Combine(Application.persistentDataPath, PROFILE_DIRECTORY);
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(profileDirectory))
            {
                Directory.CreateDirectory(profileDirectory);
                Debug.Log($"Created behavior profile directory: {profileDirectory}");
            }
            
            // Clear cache on initialization
            profileCache.Clear();
            
            Debug.Log($"BehaviorProfileManager initialized for player: {currentPlayerProfileId}");
        }

        public bool SaveProfile(BehaviorProfile profile)
        {
            if (profile == null || !profile.IsValid())
            {
                ErrorHandler.LogError("BehaviorProfileManager", "SaveProfile", 
                    new ArgumentException("Cannot save invalid behavior profile"), profile?.monsterType.ToString());
                return false;
            }

            try
            {
                // Update profile metadata
                profile.lastUpdated = DateTime.Now;
                profile.playerProfileId = currentPlayerProfileId;

                // Compress weights if profile is large enough
                if (profile.GetSizeInBytes() > COMPRESSION_THRESHOLD)
                {
                    profile.CompressWeights();
                }

                // Generate file path
                string fileName = GetProfileFileName(profile.monsterType, currentPlayerProfileId);
                string filePath = Path.Combine(profileDirectory, fileName);

                // Serialize to JSON
                string jsonData = JsonUtility.ToJson(profile, true);
                
                // Add checksum for integrity validation
                string checksum = CalculateChecksum(jsonData);
                var profileData = new SerializedProfileData
                {
                    profileJson = jsonData,
                    checksum = checksum,
                    version = "1.0",
                    savedAt = DateTime.Now
                };

                string finalJson = JsonUtility.ToJson(profileData, true);
                
                // Write to file with backup
                WriteFileWithBackup(filePath, finalJson);
                
                // Update cache
                string cacheKey = GetCacheKey(profile.monsterType, currentPlayerProfileId);
                profileCache[cacheKey] = profile;
                
                Debug.Log($"Saved behavior profile for {profile.monsterType} (Player: {currentPlayerProfileId})");
                return true;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("BehaviorProfileManager", "SaveProfile", ex, $"MonsterType: {profile?.monsterType}, Player: {currentPlayerProfileId}");
                return false;
            }
        }

        public BehaviorProfile LoadProfile(MonsterType monsterType, string playerProfileId = null)
        {
            string targetPlayerId = playerProfileId ?? currentPlayerProfileId;
            string cacheKey = GetCacheKey(monsterType, targetPlayerId);
            
            // Check cache first
            if (profileCache.TryGetValue(cacheKey, out BehaviorProfile cachedProfile))
            {
                return cachedProfile;
            }

            try
            {
                string fileName = GetProfileFileName(monsterType, targetPlayerId);
                string filePath = Path.Combine(profileDirectory, fileName);
                
                if (!File.Exists(filePath))
                {
                    Debug.Log($"No behavior profile found for {monsterType} (Player: {targetPlayerId})");
                    return null;
                }

                string fileContent = File.ReadAllText(filePath);
                var profileData = JsonUtility.FromJson<SerializedProfileData>(fileContent);
                
                // Validate checksum
                if (!ValidateChecksum(profileData.profileJson, profileData.checksum))
                {
                    var checksumException = new InvalidDataException($"Corrupted behavior profile detected for {monsterType}. Checksum mismatch.");
                    return ErrorHandler.RecoverCorruptedProfile(monsterType, filePath, checksumException);
                }

                var profile = JsonUtility.FromJson<BehaviorProfile>(profileData.profileJson);
                
                // Decompress weights if needed
                if (profile.compressedWeights != null && profile.compressedWeights.Length > 0)
                {
                    profile.DecompressWeights();
                }

                // Validate profile
                if (!ValidateProfile(profile))
                {
                    var validationException = new InvalidDataException($"Invalid behavior profile loaded for {monsterType}");
                    return ErrorHandler.RecoverCorruptedProfile(monsterType, filePath, validationException);
                }

                // Cache the loaded profile
                profileCache[cacheKey] = profile;
                
                Debug.Log($"Loaded behavior profile for {monsterType} (Player: {targetPlayerId})");
                return profile;
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("BehaviorProfileManager", "LoadProfile", ex, $"MonsterType: {monsterType}, Player: {targetPlayerId}");
                return ErrorHandler.RecoverCorruptedProfile(monsterType, GetProfileFileName(monsterType, targetPlayerId), ex);
            }
        }

        public bool ProfileExists(MonsterType monsterType, string playerProfileId = null)
        {
            string targetPlayerId = playerProfileId ?? currentPlayerProfileId;
            string cacheKey = GetCacheKey(monsterType, targetPlayerId);
            
            // Check cache first
            if (profileCache.ContainsKey(cacheKey))
            {
                return true;
            }

            // Check file system
            string fileName = GetProfileFileName(monsterType, targetPlayerId);
            string filePath = Path.Combine(profileDirectory, fileName);
            return File.Exists(filePath);
        }

        public bool DeleteProfile(MonsterType monsterType, string playerProfileId = null)
        {
            string targetPlayerId = playerProfileId ?? currentPlayerProfileId;
            
            try
            {
                string fileName = GetProfileFileName(monsterType, targetPlayerId);
                string filePath = Path.Combine(profileDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Remove from cache
                string cacheKey = GetCacheKey(monsterType, targetPlayerId);
                profileCache.Remove(cacheKey);
                
                Debug.Log($"Deleted behavior profile for {monsterType} (Player: {targetPlayerId})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete behavior profile: {ex.Message}");
                return false;
            }
        }

        public List<BehaviorProfile> GetAllProfiles()
        {
            return GetAllProfiles(currentPlayerProfileId);
        }

        public List<BehaviorProfile> GetAllProfiles(string playerProfileId)
        {
            var profiles = new List<BehaviorProfile>();
            
            try
            {
                if (!Directory.Exists(profileDirectory))
                {
                    return profiles;
                }

                var files = Directory.GetFiles(profileDirectory, $"*{playerProfileId}*{PROFILE_EXTENSION}");
                
                foreach (string filePath in files)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        var parts = fileName.Split('_');
                        
                        if (parts.Length >= 2 && Enum.TryParse<MonsterType>(parts[0], out MonsterType monsterType))
                        {
                            var profile = LoadProfile(monsterType, playerProfileId);
                            if (profile != null)
                            {
                                profiles.Add(profile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to load profile from {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get all profiles: {ex.Message}");
            }
            
            return profiles;
        }

        public bool CreateBackup(string backupPath)
        {
            try
            {
                if (!Directory.Exists(profileDirectory))
                {
                    Debug.LogWarning("No profiles directory to backup");
                    return true;
                }

                string backupDir = Path.GetDirectoryName(backupPath);
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Create a zip-like backup by copying all profile files
                var files = Directory.GetFiles(profileDirectory, $"*{PROFILE_EXTENSION}");
                var backupData = new BackupData
                {
                    createdAt = DateTime.Now,
                    profileCount = files.Length,
                    profiles = new List<string>()
                };

                foreach (string file in files)
                {
                    string content = File.ReadAllText(file);
                    backupData.profiles.Add(content);
                }

                string backupJson = JsonUtility.ToJson(backupData, true);
                File.WriteAllText(backupPath + BACKUP_EXTENSION, backupJson);
                
                Debug.Log($"Created backup with {files.Length} profiles at {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create backup: {ex.Message}");
                return false;
            }
        }

        public bool RestoreFromBackup(string backupPath)
        {
            try
            {
                string fullBackupPath = backupPath + BACKUP_EXTENSION;
                if (!File.Exists(fullBackupPath))
                {
                    Debug.LogError($"Backup file not found: {fullBackupPath}");
                    return false;
                }

                string backupJson = File.ReadAllText(fullBackupPath);
                var backupData = JsonUtility.FromJson<BackupData>(backupJson);
                
                int restoredCount = 0;
                foreach (string profileJson in backupData.profiles)
                {
                    try
                    {
                        var profileData = JsonUtility.FromJson<SerializedProfileData>(profileJson);
                        var profile = JsonUtility.FromJson<BehaviorProfile>(profileData.profileJson);
                        
                        if (SaveProfile(profile))
                        {
                            restoredCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to restore individual profile: {ex.Message}");
                    }
                }
                
                Debug.Log($"Restored {restoredCount} profiles from backup");
                return restoredCount > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to restore from backup: {ex.Message}");
                return false;
            }
        }

        public long GetStorageSize()
        {
            try
            {
                if (!Directory.Exists(profileDirectory))
                {
                    return 0;
                }

                var files = Directory.GetFiles(profileDirectory, $"*{PROFILE_EXTENSION}");
                long totalSize = 0;
                
                foreach (string file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;
                }
                
                return totalSize;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to calculate storage size: {ex.Message}");
                return 0;
            }
        }

        public int CleanupProfiles(int maxAge = 30)
        {
            int cleanedCount = 0;
            
            try
            {
                if (!Directory.Exists(profileDirectory))
                {
                    return 0;
                }

                var files = Directory.GetFiles(profileDirectory, $"*{PROFILE_EXTENSION}");
                DateTime cutoffDate = DateTime.Now.AddDays(-maxAge);
                
                foreach (string filePath in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        
                        // Check file age
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            File.Delete(filePath);
                            cleanedCount++;
                            continue;
                        }

                        // Check if profile is corrupted
                        string content = File.ReadAllText(filePath);
                        var profileData = JsonUtility.FromJson<SerializedProfileData>(content);
                        
                        if (!ValidateChecksum(profileData.profileJson, profileData.checksum))
                        {
                            File.Delete(filePath);
                            cleanedCount++;
                        }
                    }
                    catch (Exception)
                    {
                        // If we can't read the file, it's probably corrupted
                        try
                        {
                            File.Delete(filePath);
                            cleanedCount++;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to delete corrupted profile {filePath}: {ex.Message}");
                        }
                    }
                }
                
                // Clear cache for cleaned profiles
                profileCache.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cleanup profiles: {ex.Message}");
            }
            
            Debug.Log($"Cleaned up {cleanedCount} old/corrupted profiles");
            return cleanedCount;
        }

        public bool ValidateProfile(BehaviorProfile profile)
        {
            if (profile == null)
                return false;

            // Use the profile's built-in validation
            if (!profile.IsValid())
                return false;

            // Additional validation checks
            if (profile.networkWeights != null && profile.networkWeights.Any(w => float.IsNaN(w) || float.IsInfinity(w)))
            {
                Debug.LogError("Profile contains invalid weight values (NaN or Infinity)");
                return false;
            }

            if (profile.networkBiases != null && profile.networkBiases.Any(b => float.IsNaN(b) || float.IsInfinity(b)))
            {
                Debug.LogError("Profile contains invalid bias values (NaN or Infinity)");
                return false;
            }

            return true;
        }

        public long CompressAllProfiles()
        {
            long spaceSaved = 0;
            
            try
            {
                var allProfiles = GetAllProfiles();
                
                foreach (var profile in allProfiles)
                {
                    long originalSize = profile.GetSizeInBytes();
                    
                    if (profile.compressedWeights == null || profile.compressedWeights.Length == 0)
                    {
                        profile.CompressWeights();
                        SaveProfile(profile);
                        
                        long newSize = profile.GetSizeInBytes();
                        spaceSaved += originalSize - newSize;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to compress profiles: {ex.Message}");
            }
            
            Debug.Log($"Compression saved {spaceSaved} bytes across all profiles");
            return spaceSaved;
        }

        // Helper methods
        private string GetProfileFileName(MonsterType monsterType, string playerProfileId)
        {
            return $"{monsterType}_{playerProfileId}{PROFILE_EXTENSION}";
        }

        private string GetCacheKey(MonsterType monsterType, string playerProfileId)
        {
            return $"{monsterType}_{playerProfileId}";
        }

        private string CalculateChecksum(string data)
        {
            // Simple checksum using hash code
            return data.GetHashCode().ToString("X8");
        }

        private bool ValidateChecksum(string data, string expectedChecksum)
        {
            string actualChecksum = CalculateChecksum(data);
            return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }

        private void WriteFileWithBackup(string filePath, string content)
        {
            string backupPath = filePath + ".tmp";
            
            // Write to temporary file first
            File.WriteAllText(backupPath, content);
            
            // If original exists, delete it
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            // Move temporary file to final location
            File.Move(backupPath, filePath);
        }

        // Serialization helper classes
        [Serializable]
        private class SerializedProfileData
        {
            public string profileJson;
            public string checksum;
            public string version;
            public DateTime savedAt;
        }

        [Serializable]
        private class BackupData
        {
            public DateTime createdAt;
            public int profileCount;
            public List<string> profiles;
        }
    }
}
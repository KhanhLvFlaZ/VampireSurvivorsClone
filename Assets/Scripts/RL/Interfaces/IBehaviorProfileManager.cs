using UnityEngine;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Interface for managing behavior profile persistence and loading
    /// </summary>
    public interface IBehaviorProfileManager
    {
        /// <summary>
        /// Initialize the profile manager
        /// </summary>
        /// <param name="playerProfileId">ID of the current player profile</param>
        void Initialize(string playerProfileId);

        /// <summary>
        /// Save a behavior profile to persistent storage
        /// </summary>
        /// <param name="profile">Profile to save</param>
        /// <returns>True if save was successful</returns>
        bool SaveProfile(BehaviorProfile profile);

        /// <summary>
        /// Load a behavior profile from persistent storage
        /// </summary>
        /// <param name="monsterType">Type of monster to load profile for</param>
        /// <param name="playerProfileId">Player profile ID (optional, uses current if null)</param>
        /// <returns>Loaded profile or null if not found</returns>
        BehaviorProfile LoadProfile(MonsterType monsterType, string playerProfileId = null);

        /// <summary>
        /// Check if a profile exists for the given monster type
        /// </summary>
        /// <param name="monsterType">Type of monster to check</param>
        /// <param name="playerProfileId">Player profile ID (optional, uses current if null)</param>
        /// <returns>True if profile exists</returns>
        bool ProfileExists(MonsterType monsterType, string playerProfileId = null);

        /// <summary>
        /// Delete a behavior profile
        /// </summary>
        /// <param name="monsterType">Type of monster profile to delete</param>
        /// <param name="playerProfileId">Player profile ID (optional, uses current if null)</param>
        /// <returns>True if deletion was successful</returns>
        bool DeleteProfile(MonsterType monsterType, string playerProfileId = null);

        /// <summary>
        /// Get all available profiles for current player
        /// </summary>
        /// <returns>List of available behavior profiles</returns>
        List<BehaviorProfile> GetAllProfiles();

        /// <summary>
        /// Get all available profiles for specific player
        /// </summary>
        /// <param name="playerProfileId">Player profile ID</param>
        /// <returns>List of available behavior profiles</returns>
        List<BehaviorProfile> GetAllProfiles(string playerProfileId);

        /// <summary>
        /// Create a backup of all profiles
        /// </summary>
        /// <param name="backupPath">Path to save backup</param>
        /// <returns>True if backup was successful</returns>
        bool CreateBackup(string backupPath);

        /// <summary>
        /// Restore profiles from backup
        /// </summary>
        /// <param name="backupPath">Path to restore from</param>
        /// <returns>True if restore was successful</returns>
        bool RestoreFromBackup(string backupPath);

        /// <summary>
        /// Get total storage size used by profiles
        /// </summary>
        /// <returns>Size in bytes</returns>
        long GetStorageSize();

        /// <summary>
        /// Clean up old or corrupted profiles
        /// </summary>
        /// <param name="maxAge">Maximum age in days (profiles older than this will be deleted)</param>
        /// <returns>Number of profiles cleaned up</returns>
        int CleanupProfiles(int maxAge = 30);

        /// <summary>
        /// Validate profile integrity
        /// </summary>
        /// <param name="profile">Profile to validate</param>
        /// <returns>True if profile is valid</returns>
        bool ValidateProfile(BehaviorProfile profile);

        /// <summary>
        /// Compress all profiles to save storage space
        /// </summary>
        /// <returns>Amount of space saved in bytes</returns>
        long CompressAllProfiles();

        /// <summary>
        /// Current player profile ID
        /// </summary>
        string CurrentPlayerProfileId { get; }

        /// <summary>
        /// Base directory for profile storage
        /// </summary>
        string ProfileDirectory { get; }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Factory for creating ActionDecoders for different monster types
    /// </summary>
    public static class ActionDecoderFactory
    {
        private static Dictionary<MonsterType, ActionDecoder> decoderCache = new Dictionary<MonsterType, ActionDecoder>();

        /// <summary>
        /// Create or get cached ActionDecoder for a monster type
        /// </summary>
        public static ActionDecoder CreateDecoder(MonsterType monsterType, ActionSpace actionSpace)
        {
            // Check cache first
            if (decoderCache.ContainsKey(monsterType))
            {
                return decoderCache[monsterType];
            }

            // Create new decoder
            var decoder = new ActionDecoder();
            decoder.Initialize(monsterType, actionSpace);
            
            // Cache for reuse
            decoderCache[monsterType] = decoder;
            
            return decoder;
        }

        /// <summary>
        /// Create ActionDecoder from MonsterRLConfig
        /// </summary>
        public static ActionDecoder CreateDecoder(MonsterRLConfig config)
        {
            if (config == null || !config.IsValid())
            {
                Debug.LogWarning("Invalid MonsterRLConfig provided, using default configuration");
                return CreateDecoder(MonsterType.Melee, ActionSpace.CreateDefault());
            }

            return CreateDecoder(config.monsterType, config.actionSpace);
        }

        /// <summary>
        /// Clear the decoder cache (useful for testing or configuration changes)
        /// </summary>
        public static void ClearCache()
        {
            decoderCache.Clear();
        }

        /// <summary>
        /// Get all cached decoders
        /// </summary>
        public static Dictionary<MonsterType, ActionDecoder> GetAllDecoders()
        {
            return new Dictionary<MonsterType, ActionDecoder>(decoderCache);
        }

        /// <summary>
        /// Validate that all monster types have decoders
        /// </summary>
        public static bool ValidateAllDecoders()
        {
            foreach (MonsterType monsterType in System.Enum.GetValues(typeof(MonsterType)))
            {
                if (monsterType == MonsterType.None) continue;
                
                if (!decoderCache.ContainsKey(monsterType))
                {
                    Debug.LogWarning($"No ActionDecoder found for monster type: {monsterType}");
                    return false;
                }
            }
            
            return true;
        }
    }
}
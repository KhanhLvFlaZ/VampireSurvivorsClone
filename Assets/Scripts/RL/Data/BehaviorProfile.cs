using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Serializable behavior profile containing learned neural network weights and metadata
    /// </summary>
    [Serializable]
    public class BehaviorProfile
    {
        [Header("Profile Information")]
        public string profileId;
        public string monsterTypeName;
        public MonsterType monsterType;
        public DateTime createdDate;
        public DateTime lastUpdated;
        public string playerProfileId; // Links to specific player profile
        
        [Header("Network Architecture")]
        public NetworkArchitecture architecture;
        public int[] layerSizes;
        public int inputSize;
        public int outputSize;
        
        [Header("Learned Parameters")]
        public float[] networkWeights;
        public float[] networkBiases;
        public byte[] compressedWeights; // Compressed version for storage
        
        [Header("Training Metadata")]
        public int trainingEpisodes;
        public float averageReward;
        public float bestReward;
        public LearningAlgorithm algorithm;
        public LearningMetrics metrics;
        
        [Header("Configuration")]
        public ActionSpace actionSpace;
        public RewardFunctionType rewardType;
        public float explorationRate;

        /// <summary>
        /// Create a new behavior profile
        /// </summary>
        public static BehaviorProfile Create(MonsterType monsterType, string playerProfileId, NetworkArchitecture architecture)
        {
            return new BehaviorProfile
            {
                profileId = Guid.NewGuid().ToString(),
                monsterTypeName = monsterType.ToString(),
                monsterType = monsterType,
                createdDate = DateTime.Now,
                lastUpdated = DateTime.Now,
                playerProfileId = playerProfileId,
                architecture = architecture,
                trainingEpisodes = 0,
                averageReward = 0f,
                bestReward = float.MinValue,
                algorithm = LearningAlgorithm.DQN,
                metrics = LearningMetrics.CreateDefault(),
                actionSpace = ActionSpace.CreateDefault(),
                rewardType = RewardFunctionType.Dense,
                explorationRate = 1f
            };
        }

        /// <summary>
        /// Update profile after training session
        /// </summary>
        public void UpdateAfterTraining(float[] weights, float[] biases, LearningMetrics newMetrics)
        {
            networkWeights = weights;
            networkBiases = biases;
            metrics = newMetrics;
            trainingEpisodes = newMetrics.episodeCount;
            averageReward = newMetrics.averageReward;
            bestReward = newMetrics.bestReward;
            explorationRate = newMetrics.explorationRate;
            lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Compress weights for efficient storage
        /// </summary>
        public void CompressWeights()
        {
            if (networkWeights != null && networkWeights.Length > 0)
            {
                // Simple compression: convert floats to bytes with reduced precision
                List<byte> compressed = new List<byte>();
                
                foreach (float weight in networkWeights)
                {
                    // Quantize to 8-bit precision
                    float normalized = Mathf.Clamp((weight + 1f) / 2f, 0f, 1f);
                    byte quantized = (byte)(normalized * 255f);
                    compressed.Add(quantized);
                }
                
                compressedWeights = compressed.ToArray();
            }
        }

        /// <summary>
        /// Decompress weights from storage
        /// </summary>
        public void DecompressWeights()
        {
            if (compressedWeights != null && compressedWeights.Length > 0)
            {
                networkWeights = new float[compressedWeights.Length];
                
                for (int i = 0; i < compressedWeights.Length; i++)
                {
                    // Dequantize from 8-bit precision
                    float normalized = compressedWeights[i] / 255f;
                    networkWeights[i] = normalized * 2f - 1f;
                }
            }
        }

        /// <summary>
        /// Validate profile integrity
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(profileId) &&
                   monsterType != MonsterType.None &&
                   (networkWeights != null || compressedWeights != null) &&
                   actionSpace != null;
        }

        /// <summary>
        /// Get profile size in bytes (for storage management)
        /// </summary>
        public int GetSizeInBytes()
        {
            int size = 0;
            
            // String data (approximate)
            size += (profileId?.Length ?? 0) * 2;
            size += (monsterTypeName?.Length ?? 0) * 2;
            size += (playerProfileId?.Length ?? 0) * 2;
            
            // Array data
            size += (networkWeights?.Length ?? 0) * sizeof(float);
            size += (networkBiases?.Length ?? 0) * sizeof(float);
            size += (compressedWeights?.Length ?? 0) * sizeof(byte);
            size += (layerSizes?.Length ?? 0) * sizeof(int);
            
            // Metadata (approximate)
            size += sizeof(int) * 10; // Various int fields
            size += sizeof(float) * 10; // Various float fields
            
            return size;
        }

        /// <summary>
        /// Create a copy of this profile
        /// </summary>
        public BehaviorProfile Clone()
        {
            var clone = new BehaviorProfile
            {
                profileId = Guid.NewGuid().ToString(), // New ID for clone
                monsterTypeName = monsterTypeName,
                monsterType = monsterType,
                createdDate = DateTime.Now,
                lastUpdated = DateTime.Now,
                playerProfileId = playerProfileId,
                architecture = architecture,
                inputSize = inputSize,
                outputSize = outputSize,
                trainingEpisodes = trainingEpisodes,
                averageReward = averageReward,
                bestReward = bestReward,
                algorithm = algorithm,
                metrics = metrics,
                rewardType = rewardType,
                explorationRate = explorationRate
            };

            // Deep copy arrays
            if (layerSizes != null)
                clone.layerSizes = (int[])layerSizes.Clone();
            if (networkWeights != null)
                clone.networkWeights = (float[])networkWeights.Clone();
            if (networkBiases != null)
                clone.networkBiases = (float[])networkBiases.Clone();
            if (compressedWeights != null)
                clone.compressedWeights = (byte[])compressedWeights.Clone();

            // Clone action space
            if (actionSpace != null)
            {
                clone.actionSpace = new ActionSpace
                {
                    canMove = actionSpace.canMove,
                    movementDirections = actionSpace.movementDirections,
                    canAttack = actionSpace.canAttack,
                    canSpecialAttack = actionSpace.canSpecialAttack,
                    canDefend = actionSpace.canDefend,
                    canRetreat = actionSpace.canRetreat,
                    canCoordinate = actionSpace.canCoordinate,
                    canAmbush = actionSpace.canAmbush,
                    canWait = actionSpace.canWait,
                    minActionInterval = actionSpace.minActionInterval,
                    maxActionRange = actionSpace.maxActionRange
                };
            }

            return clone;
        }
    }
}
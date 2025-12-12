using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vampire.RL
{
    /// <summary>
    /// Deep Q-Network learning agent implementation
    /// Complete DQN algorithm with experience replay and target network
    /// </summary>
    public class DQNLearningAgent : MonoBehaviour, ILearningAgent
    {
        [Header("Agent Configuration")]
        [SerializeField] private MonsterType monsterType;
        [SerializeField] private bool isTraining = true;
        
        [Header("DQN Hyperparameters")]
        [SerializeField] private float learningRate = 0.001f;
        [SerializeField] private float discountFactor = 0.99f;
        [SerializeField] private float initialEpsilon = 1.0f;
        [SerializeField] private float finalEpsilon = 0.01f;
        [SerializeField] private float epsilonDecayRate = 0.995f;
        [SerializeField] private int batchSize = 32;
        [SerializeField] private int targetUpdateFrequency = 100;
        [SerializeField] private int minExperiencesBeforeTraining = 1000;
        
        private ActionSpace actionSpace;
        private INeuralNetwork mainNetwork;
        private INeuralNetwork targetNetwork;
        private LearningMetrics metrics;
        private ExperienceReplayBuffer experienceBuffer;
        private int updateCounter = 0;
        
        private const int BUFFER_SIZE = 10000;

        public bool IsTraining 
        { 
            get => isTraining; 
            set => isTraining = value; 
        }

        public void Initialize(MonsterType monsterType, ActionSpace actionSpace)
        {
            this.monsterType = monsterType;
            this.actionSpace = actionSpace;
            this.metrics = LearningMetrics.CreateDefault();
            this.metrics.explorationRate = initialEpsilon;
            this.metrics.learningRate = learningRate;
            
            // Initialize experience replay buffer
            this.experienceBuffer = new ExperienceReplayBuffer(BUFFER_SIZE);

            // Initialize main and target networks
            int inputSize = 32; // From design document
            int outputSize = actionSpace.GetTotalActionCount();
            int[] hiddenLayers = new int[] { 128, 64, 32 }; // Deeper network for better learning
            
            mainNetwork = new SimpleNeuralNetwork();
            mainNetwork.Initialize(inputSize, outputSize, hiddenLayers, NetworkArchitecture.Simple);
            
            targetNetwork = new SimpleNeuralNetwork();
            targetNetwork.Initialize(inputSize, outputSize, hiddenLayers, NetworkArchitecture.Simple);
            
            // Copy initial weights to target network
            targetNetwork.CopyWeightsFrom(mainNetwork);
            
            Debug.Log($"DQN Agent initialized for {monsterType} with {outputSize} actions, {mainNetwork.GetParameterCount()} parameters");
        }

        public int SelectAction(RLGameState state, bool isTraining)
        {
            if (mainNetwork == null) return 0;

            // Encode state
            float[] encodedState = EncodeState(state);
            
            // Get Q-values from main network
            float[] qValues = mainNetwork.Forward(encodedState);
            
            // Epsilon-greedy action selection
            if (isTraining && Random.Range(0f, 1f) < metrics.explorationRate)
            {
                // Random action (exploration)
                int randomAction = Random.Range(0, qValues.Length);
                return randomAction;
            }
            else
            {
                // Best action (exploitation)
                int bestAction = GetBestAction(qValues);
                return bestAction;
            }
        }
        
        private int GetBestAction(float[] qValues)
        {
            int bestAction = 0;
            float bestValue = qValues[0];
            
            for (int i = 1; i < qValues.Length; i++)
            {
                if (qValues[i] > bestValue)
                {
                    bestValue = qValues[i];
                    bestAction = i;
                }
            }
            
            return bestAction;
        }

        public void StoreExperience(RLGameState state, int action, float reward, RLGameState nextState, bool done)
        {
            if (!isTraining) return;
            
            // Store experience in replay buffer
            var experience = new Experience
            {
                state = state,
                action = action,
                reward = reward,
                nextState = nextState,
                done = done
            };

            experienceBuffer.Add(experience);
        }

        public void UpdatePolicy()
        {
            if (!isTraining) return;
            
            metrics.totalSteps++;
            
            // Only train if we have enough experiences
            if (experienceBuffer.Count < minExperiencesBeforeTraining) return;
            
            // Sample a batch of experiences
            var batch = experienceBuffer.SampleBatch(batchSize);
            if (batch.Length == 0) return;
            
            // Perform DQN training step
            float loss = TrainOnBatch(batch);
            metrics.lossValue = loss;
            
            // Update target network periodically
            updateCounter++;
            if (updateCounter >= targetUpdateFrequency)
            {
                targetNetwork.CopyWeightsFrom(mainNetwork);
                updateCounter = 0;
                Debug.Log($"Target network updated for {monsterType}");
            }
            
            // Decay exploration rate
            DecayEpsilon();
        }
        
        private float TrainOnBatch(Experience[] batch)
        {
            float totalLoss = 0f;
            
            foreach (var experience in batch)
            {
                // Encode states
                float[] currentState = EncodeState(experience.state);
                float[] nextState = EncodeState(experience.nextState);
                
                // Get current Q-values
                float[] currentQValues = mainNetwork.Forward(currentState);
                
                // Calculate target Q-value using Bellman equation
                float targetQValue;
                if (experience.done)
                {
                    // Terminal state - no future reward
                    targetQValue = experience.reward;
                }
                else
                {
                    // Non-terminal state - use target network for stability
                    float[] nextQValues = targetNetwork.Forward(nextState);
                    float maxNextQ = nextQValues.Max();
                    targetQValue = experience.reward + discountFactor * maxNextQ;
                }
                
                // Create target vector (only update the action that was taken)
                float[] targetVector = (float[])currentQValues.Clone();
                targetVector[experience.action] = targetQValue;
                
                // Train the network
                float loss = mainNetwork.Backward(currentState, targetVector, learningRate);
                totalLoss += loss;
            }
            
            return totalLoss / batch.Length;
        }
        
        private void DecayEpsilon()
        {
            if (metrics.explorationRate > finalEpsilon)
            {
                metrics.explorationRate = Mathf.Max(finalEpsilon, metrics.explorationRate * epsilonDecayRate);
            }
        }

        public void SaveBehaviorProfile(string filePath)
        {
            if (mainNetwork == null) return;

            var profile = BehaviorProfile.Create(monsterType, "dqn", NetworkArchitecture.Simple);
            profile.networkWeights = mainNetwork.GetWeights();
            profile.networkBiases = mainNetwork.GetBiases();
            profile.metrics = metrics;
            
            // Save to file (simplified)
            string json = JsonUtility.ToJson(profile, true);
            System.IO.File.WriteAllText(filePath, json);
            
            Debug.Log($"DQN Behavior profile saved for {monsterType} to {filePath}");
        }

        public void LoadBehaviorProfile(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return;

            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                var profile = JsonUtility.FromJson<BehaviorProfile>(json);
                
                if (profile != null && profile.IsValid())
                {
                    if (mainNetwork != null && profile.networkWeights != null)
                    {
                        mainNetwork.SetWeights(profile.networkWeights);
                        if (profile.networkBiases != null)
                            mainNetwork.SetBiases(profile.networkBiases);
                        
                        // Also update target network
                        targetNetwork.CopyWeightsFrom(mainNetwork);
                    }
                    
                    metrics = profile.metrics;
                    Debug.Log($"DQN Behavior profile loaded for {monsterType} from {filePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load DQN behavior profile: {e.Message}");
            }
        }

        public LearningMetrics GetMetrics()
        {
            return metrics;
        }

        private float[] EncodeState(RLGameState state)
        {
            // Placeholder state encoding - will be fully implemented in task 2
            float[] encoded = new float[32];
            
            // Basic encoding
            encoded[0] = state.playerPosition.x / 10f; // Normalize position
            encoded[1] = state.playerPosition.y / 10f;
            encoded[2] = state.playerVelocity.x / 5f; // Normalize velocity
            encoded[3] = state.playerVelocity.y / 5f;
            encoded[4] = state.playerHealth / 100f; // Normalize health
            
            encoded[5] = state.monsterPosition.x / 10f;
            encoded[6] = state.monsterPosition.y / 10f;
            encoded[7] = state.monsterHealth / 100f;
            encoded[8] = state.DistanceToPlayer / 10f; // Normalize distance
            
            // Fill remaining with zeros for now
            for (int i = 9; i < encoded.Length; i++)
            {
                encoded[i] = 0f;
            }
            
            return encoded;
        }

        /// <summary>
        /// Get current learning progress and statistics
        /// </summary>
        public string GetLearningStatus()
        {
            return $"Episodes: {metrics.episodeCount}, " +
                   $"Avg Reward: {metrics.averageReward:F2}, " +
                   $"Epsilon: {metrics.explorationRate:F3}, " +
                   $"Loss: {metrics.lossValue:F4}, " +
                   $"Buffer: {experienceBuffer.Count}/{BUFFER_SIZE}";
        }
        
        /// <summary>
        /// Reset the agent for a new episode
        /// </summary>
        public void ResetEpisode()
        {
            // Reset any episode-specific state if needed
            // The experience buffer and networks persist across episodes
        }
        
        /// <summary>
        /// Check if the agent is ready to start training
        /// </summary>
        public bool IsReadyToTrain()
        {
            return experienceBuffer.Count >= minExperiencesBeforeTraining;
        }
    }
    
    /// <summary>
    /// Experience replay buffer for DQN training
    /// </summary>
    public class ExperienceReplayBuffer
    {
        private Experience[] buffer;
        private int capacity;
        private int size;
        private int index;
        
        public int Count => size;
        
        public ExperienceReplayBuffer(int capacity)
        {
            this.capacity = capacity;
            this.buffer = new Experience[capacity];
            this.size = 0;
            this.index = 0;
        }
        
        public void Add(Experience experience)
        {
            buffer[index] = experience;
            index = (index + 1) % capacity;
            size = Mathf.Min(size + 1, capacity);
        }
        
        public Experience[] SampleBatch(int batchSize)
        {
            if (size < batchSize) return new Experience[0];
            
            var batch = new Experience[batchSize];
            var usedIndices = new HashSet<int>();
            
            for (int i = 0; i < batchSize; i++)
            {
                int randomIndex;
                do
                {
                    randomIndex = Random.Range(0, size);
                } while (usedIndices.Contains(randomIndex));
                
                usedIndices.Add(randomIndex);
                batch[i] = buffer[randomIndex];
            }
            
            return batch;
        }
        
        public void Clear()
        {
            size = 0;
            index = 0;
        }
    }
    
    /// <summary>
    /// Experience tuple for DQN training
    /// </summary>
    [System.Serializable]
    public struct Experience
    {
        public RLGameState state;
        public int action;
        public float reward;
        public RLGameState nextState;
        public bool done;
    }
}
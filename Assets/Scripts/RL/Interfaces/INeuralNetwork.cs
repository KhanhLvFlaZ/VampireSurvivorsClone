using UnityEngine;

namespace Vampire.RL
{
    /// <summary>
    /// Interface for neural network implementations
    /// Supports both Unity ML-Agents and custom implementations
    /// </summary>
    public interface INeuralNetwork
    {
        /// <summary>
        /// Initialize the neural network with specified architecture
        /// </summary>
        /// <param name="inputSize">Size of input layer</param>
        /// <param name="outputSize">Size of output layer</param>
        /// <param name="hiddenLayers">Sizes of hidden layers</param>
        /// <param name="architecture">Network architecture type</param>
        void Initialize(int inputSize, int outputSize, int[] hiddenLayers, NetworkArchitecture architecture);

        /// <summary>
        /// Forward pass through the network
        /// </summary>
        /// <param name="input">Input vector</param>
        /// <returns>Output vector</returns>
        float[] Forward(float[] input);

        /// <summary>
        /// Backward pass for training (if supported)
        /// </summary>
        /// <param name="input">Input vector</param>
        /// <param name="target">Target output</param>
        /// <param name="learningRate">Learning rate</param>
        /// <returns>Loss value</returns>
        float Backward(float[] input, float[] target, float learningRate);

        /// <summary>
        /// Get network weights for serialization
        /// </summary>
        /// <returns>Flattened weight array</returns>
        float[] GetWeights();

        /// <summary>
        /// Set network weights from serialization
        /// </summary>
        /// <param name="weights">Flattened weight array</param>
        void SetWeights(float[] weights);

        /// <summary>
        /// Get network biases for serialization
        /// </summary>
        /// <returns>Flattened bias array</returns>
        float[] GetBiases();

        /// <summary>
        /// Set network biases from serialization
        /// </summary>
        /// <param name="biases">Flattened bias array</param>
        void SetBiases(float[] biases);

        /// <summary>
        /// Get total number of parameters (weights + biases)
        /// </summary>
        int GetParameterCount();

        /// <summary>
        /// Copy weights from another network
        /// </summary>
        /// <param name="sourceNetwork">Network to copy from</param>
        void CopyWeightsFrom(INeuralNetwork sourceNetwork);

        /// <summary>
        /// Add noise to weights for exploration
        /// </summary>
        /// <param name="noiseScale">Scale of noise to add</param>
        void AddNoise(float noiseScale);

        /// <summary>
        /// Reset network to random initialization
        /// </summary>
        void Reset();

        /// <summary>
        /// Whether this network supports training
        /// </summary>
        bool SupportsTraining { get; }

        /// <summary>
        /// Current network architecture
        /// </summary>
        NetworkArchitecture Architecture { get; }

        /// <summary>
        /// Input layer size
        /// </summary>
        int InputSize { get; }

        /// <summary>
        /// Output layer size
        /// </summary>
        int OutputSize { get; }
    }
}
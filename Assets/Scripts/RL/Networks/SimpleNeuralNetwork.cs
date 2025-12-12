using UnityEngine;
using System;
using System.Collections.Generic;

namespace Vampire.RL
{
    /// <summary>
    /// Simple feedforward neural network implementation
    /// Used as fallback when ML-Agents is not available
    /// </summary>
    public class SimpleNeuralNetwork : INeuralNetwork
    {
        private int inputSize;
        private int outputSize;
        private int[] layerSizes;
        private float[][] weights;
        private float[][] biases;
        private float[][] activations;
        private NetworkArchitecture architecture;

        public bool SupportsTraining => true;
        public NetworkArchitecture Architecture => architecture;
        public int InputSize => inputSize;
        public int OutputSize => outputSize;

        public void Initialize(int inputSize, int outputSize, int[] hiddenLayers, NetworkArchitecture architecture)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            this.architecture = architecture;

            // Build layer sizes array
            List<int> layers = new List<int> { inputSize };
            if (hiddenLayers != null)
                layers.AddRange(hiddenLayers);
            layers.Add(outputSize);
            layerSizes = layers.ToArray();

            // Initialize weights and biases
            InitializeWeightsAndBiases();
            
            // Initialize activation arrays
            activations = new float[layerSizes.Length][];
            for (int i = 0; i < layerSizes.Length; i++)
            {
                activations[i] = new float[layerSizes[i]];
            }
        }

        private void InitializeWeightsAndBiases()
        {
            weights = new float[layerSizes.Length - 1][];
            biases = new float[layerSizes.Length - 1][];

            for (int i = 0; i < weights.Length; i++)
            {
                int inputCount = layerSizes[i];
                int outputCount = layerSizes[i + 1];

                weights[i] = new float[inputCount * outputCount];
                biases[i] = new float[outputCount];

                // Xavier initialization
                float scale = Mathf.Sqrt(2f / (inputCount + outputCount));
                for (int j = 0; j < weights[i].Length; j++)
                {
                    weights[i][j] = UnityEngine.Random.Range(-scale, scale);
                }

                for (int j = 0; j < biases[i].Length; j++)
                {
                    biases[i][j] = 0f;
                }
            }
        }

        public float[] Forward(float[] input)
        {
            if (input.Length != inputSize)
                throw new ArgumentException($"Input size mismatch. Expected {inputSize}, got {input.Length}");

            // Copy input to first activation layer
            Array.Copy(input, activations[0], inputSize);

            // Forward pass through each layer
            for (int layer = 0; layer < weights.Length; layer++)
            {
                int inputCount = layerSizes[layer];
                int outputCount = layerSizes[layer + 1];

                for (int j = 0; j < outputCount; j++)
                {
                    float sum = biases[layer][j];
                    for (int i = 0; i < inputCount; i++)
                    {
                        sum += activations[layer][i] * weights[layer][i * outputCount + j];
                    }

                    // Apply activation function
                    if (layer == weights.Length - 1)
                    {
                        // Output layer - no activation for Q-values
                        activations[layer + 1][j] = sum;
                    }
                    else
                    {
                        // Hidden layers - ReLU activation
                        activations[layer + 1][j] = Mathf.Max(0f, sum);
                    }
                }
            }

            // Return output layer
            float[] output = new float[outputSize];
            Array.Copy(activations[activations.Length - 1], output, outputSize);
            return output;
        }

        public float Backward(float[] input, float[] target, float learningRate)
        {
            // Simple gradient descent implementation
            float[] output = Forward(input);
            
            // Calculate loss (MSE)
            float loss = 0f;
            float[] outputError = new float[outputSize];
            for (int i = 0; i < outputSize; i++)
            {
                float error = target[i] - output[i];
                outputError[i] = error;
                loss += error * error;
            }
            loss /= outputSize;

            // Backward pass (simplified)
            BackwardPass(outputError, learningRate);

            return loss;
        }

        private void BackwardPass(float[] outputError, float learningRate)
        {
            // This is a simplified backward pass
            // For production use, consider using a proper ML framework
            
            float[][] errors = new float[layerSizes.Length][];
            for (int i = 0; i < layerSizes.Length; i++)
            {
                errors[i] = new float[layerSizes[i]];
            }

            // Start with output error
            Array.Copy(outputError, errors[errors.Length - 1], outputSize);

            // Propagate errors backward
            for (int layer = weights.Length - 1; layer >= 0; layer--)
            {
                int inputCount = layerSizes[layer];
                int outputCount = layerSizes[layer + 1];

                // Calculate input errors
                for (int i = 0; i < inputCount; i++)
                {
                    float error = 0f;
                    for (int j = 0; j < outputCount; j++)
                    {
                        error += errors[layer + 1][j] * weights[layer][i * outputCount + j];
                    }
                    errors[layer][i] = error;
                }

                // Update weights and biases
                for (int i = 0; i < inputCount; i++)
                {
                    for (int j = 0; j < outputCount; j++)
                    {
                        float gradient = errors[layer + 1][j] * activations[layer][i];
                        weights[layer][i * outputCount + j] += learningRate * gradient;
                    }
                }

                for (int j = 0; j < outputCount; j++)
                {
                    biases[layer][j] += learningRate * errors[layer + 1][j];
                }
            }
        }

        public float[] GetWeights()
        {
            List<float> allWeights = new List<float>();
            foreach (var layerWeights in weights)
            {
                allWeights.AddRange(layerWeights);
            }
            return allWeights.ToArray();
        }

        public void SetWeights(float[] weights)
        {
            int index = 0;
            for (int layer = 0; layer < this.weights.Length; layer++)
            {
                for (int i = 0; i < this.weights[layer].Length; i++)
                {
                    if (index < weights.Length)
                        this.weights[layer][i] = weights[index++];
                }
            }
        }

        public float[] GetBiases()
        {
            List<float> allBiases = new List<float>();
            foreach (var layerBiases in biases)
            {
                allBiases.AddRange(layerBiases);
            }
            return allBiases.ToArray();
        }

        public void SetBiases(float[] biases)
        {
            int index = 0;
            for (int layer = 0; layer < this.biases.Length; layer++)
            {
                for (int i = 0; i < this.biases[layer].Length; i++)
                {
                    if (index < biases.Length)
                        this.biases[layer][i] = biases[index++];
                }
            }
        }

        public int GetParameterCount()
        {
            int count = 0;
            foreach (var layerWeights in weights)
                count += layerWeights.Length;
            foreach (var layerBiases in biases)
                count += layerBiases.Length;
            return count;
        }

        public void CopyWeightsFrom(INeuralNetwork sourceNetwork)
        {
            SetWeights(sourceNetwork.GetWeights());
            SetBiases(sourceNetwork.GetBiases());
        }

        public void AddNoise(float noiseScale)
        {
            foreach (var layerWeights in weights)
            {
                for (int i = 0; i < layerWeights.Length; i++)
                {
                    layerWeights[i] += UnityEngine.Random.Range(-noiseScale, noiseScale);
                }
            }
        }

        public void Reset()
        {
            InitializeWeightsAndBiases();
        }
    }
}
using UnityEngine;
using System;

namespace Vampire.RL
{
    /// <summary>
    /// Dummy neural network that provides safe fallback behavior
    /// Used when real neural networks fail to initialize
    /// Always returns safe default outputs to prevent crashes
    /// </summary>
    public class DummyNeuralNetwork : INeuralNetwork
    {
        private int inputSize;
        private int outputSize;
        private float[] defaultOutput;
        
        public bool SupportsTraining => false;
        public NetworkArchitecture Architecture => NetworkArchitecture.Simple;
        public int InputSize => inputSize;
        public int OutputSize => outputSize;

        public DummyNeuralNetwork(int inputSize, int outputSize)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            
            // Initialize default output (all zeros, which typically means "do nothing")
            this.defaultOutput = new float[outputSize];
            for (int i = 0; i < outputSize; i++)
            {
                this.defaultOutput[i] = 0f;
            }
            
            Debug.LogWarning($"[DUMMY NETWORK] Created dummy neural network ({inputSize} -> {outputSize}). This provides safe fallback behavior only.");
        }

        public void Initialize(int inputSize, int outputSize, int[] hiddenLayers, NetworkArchitecture architecture)
        {
            this.inputSize = inputSize;
            this.outputSize = outputSize;
            
            // Reinitialize default output
            this.defaultOutput = new float[outputSize];
            for (int i = 0; i < outputSize; i++)
            {
                this.defaultOutput[i] = 0f;
            }
        }

        public float[] Forward(float[] input)
        {
            try
            {
                // Validate input
                if (input == null || input.Length != inputSize)
                {
                    ErrorHandler.LogError("DummyNeuralNetwork", "Forward", 
                        new ArgumentException($"Invalid input size. Expected {inputSize}, got {input?.Length ?? 0}"));
                    return (float[])defaultOutput.Clone();
                }
                
                // Return safe default output
                return (float[])defaultOutput.Clone();
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError("DummyNeuralNetwork", "Forward", ex);
                return (float[])defaultOutput.Clone();
            }
        }

        public float Backward(float[] input, float[] target, float learningRate)
        {
            // Dummy network doesn't support training
            // Return zero loss to indicate "no learning"
            return 0f;
        }

        public float[] GetWeights()
        {
            // Return empty weights array
            return new float[0];
        }

        public void SetWeights(float[] weights)
        {
            // Dummy network ignores weight updates
            // This prevents crashes when trying to load corrupted profiles
        }

        public float[] GetBiases()
        {
            // Return empty biases array
            return new float[0];
        }

        public void SetBiases(float[] biases)
        {
            // Dummy network ignores bias updates
        }

        public int GetParameterCount()
        {
            // Dummy network has no parameters
            return 0;
        }

        public void CopyWeightsFrom(INeuralNetwork sourceNetwork)
        {
            // Dummy network can't copy weights
            // This is safe - it just continues with default behavior
        }

        public void AddNoise(float noiseScale)
        {
            // Dummy network ignores noise addition
        }

        public void Reset()
        {
            // Reset to default output
            for (int i = 0; i < defaultOutput.Length; i++)
            {
                defaultOutput[i] = 0f;
            }
        }
    }
}
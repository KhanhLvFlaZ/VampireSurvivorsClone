# Monster Reinforcement Learning System

## Overview

This directory contains the foundation for the Monster Reinforcement Learning system in the Vampire Survivors Clone. The system enables monsters to learn and adapt their behavior based on player actions and game state.

## Task 1 Implementation Status: ✅ COMPLETED
## Task 4 Implementation Status: ✅ COMPLETED

### Core Interfaces Created

- **ILearningAgent**: Main interface for RL agents that can learn and make decisions
- **IStateEncoder**: Interface for encoding game state into neural network input
- **IActionDecoder**: Interface for decoding neural network output into game actions  
- **IRewardCalculator**: Interface for calculating rewards based on monster actions
- **INeuralNetwork**: Interface for neural network implementations
- **ITrainingCoordinator**: Interface for coordinating training across multiple monsters
- **IBehaviorProfileManager**: Interface for managing behavior profile persistence

### Core Data Structures

- **RLGameState**: Complete game state representation for RL (32 dimensions)
- **MonsterAction**: Action representation with type, direction, intensity, and target
- **ActionSpace**: Configurable action space for different monster types
- **BehaviorProfile**: Serializable learned behavior with compression support
- **LearningMetrics**: Comprehensive learning progress tracking
- **ActionOutcome**: Action result data for reward calculation

### Neural Network Framework

- **SimpleNeuralNetwork**: Basic feedforward network implementation
- **NetworkArchitecture**: Support for different network types (Simple, LSTM, CNN, Attention)
- **Weight serialization/deserialization**: For behavior profile persistence
- **Xavier initialization**: Proper weight initialization for training stability

### System Integration

- **RLSystem**: Main system manager that coordinates all RL components
- **TrainingCoordinator**: Manages training process across multiple agents (placeholder)
- **BehaviorProfileManager**: Handles save/load of learned behaviors (placeholder)
- **DQNLearningAgent**: Complete Deep Q-Network agent implementation with experience replay

### Performance Considerations

- **Frame time monitoring**: Ensures RL processing stays under 16ms per frame
- **Memory usage tracking**: Limits RL components to 100MB total usage
- **Adaptive processing**: Can throttle updates to maintain 60 FPS
- **Profile compression**: Reduces storage space for behavior profiles

### Integration Points

- **EntityManager compatibility**: Designed to work with existing monster spawning
- **Monster class extension**: Can be added to existing Monster hierarchy
- **ScriptableObject configuration**: Uses existing configuration patterns
- **Object pooling support**: Compatible with current pooling system

### Testing

- **RLSystemIntegrationTest**: Comprehensive test suite verifying all components work together
- **Unit test coverage**: Tests data structures, neural networks, agents, and profiles
- **Error handling**: Graceful fallbacks for corrupted profiles and network failures

## Requirements Satisfied

✅ **Requirement 2.1**: Common interface that works with existing Monster classes  
✅ **Requirement 6.5**: Extends rather than replaces current Monster class functionality  
✅ **Requirement 1.4**: Reward system provides positive reinforcement for successful damage  
✅ **Requirement 1.5**: Reward system provides negative reinforcement for monster death  
✅ **Requirement 2.3**: Reward functions are configurable through ScriptableObjects  
✅ **Requirement 4.1**: Learning agent serializes behavior profiles to persistent storage  
✅ **Requirement 4.2**: Learning agent loads existing behavior profiles from storage  
✅ **Requirement 4.4**: Learning agent compresses behavior profiles using efficient serialization  
✅ **Requirement 4.5**: Learning agent maintains separate behavior profiles per player

### RewardCalculator System (Task 4)

- **RewardCalculator**: Main implementation with configurable reward functions and shaping
- **RewardConfig**: ScriptableObject for designer-friendly reward configuration
- **RewardCalculatorFactory**: Factory pattern for creating and caching calculators
- **SpecializedRewardCalculators**: Sparse, curiosity-driven, and adaptive reward calculators
- **Comprehensive Testing**: Unit tests and integration tests for all reward functionality

### Reward System Features

- **Configurable Rewards**: ScriptableObject-based configuration for different monster types
- **Reward Shaping**: Distance-based, health-based, and time-based reward shaping for better learning
- **Multiple Reward Types**: Dense, sparse, shaped, and curiosity-driven reward functions
- **Factory Pattern**: Efficient creation and caching of reward calculators
- **Specialized Calculators**: Different reward strategies for different learning approaches
- **Difficulty Scaling**: Automatic reward adjustment based on difficulty settings

## Task 5 Implementation Status: ✅ COMPLETED
## Task 7 Implementation Status: ✅ COMPLETED
## Task 8 Implementation Status: ✅ COMPLETED

### DQN Learning Agent (Task 5)

- **Complete DQN Algorithm**: Full Deep Q-Network implementation with dual networks
- **Experience Replay Buffer**: Efficient circular buffer for storing and sampling experiences
- **Epsilon-Greedy Exploration**: Configurable exploration strategy with decay
- **Q-Learning Updates**: Bellman equation implementation with target network stability
- **Target Network**: Separate target network updated periodically for training stability
- **Hyperparameter Configuration**: Configurable learning rate, discount factor, batch size, etc.
- **Training Management**: Proper training/inference mode switching and progress tracking

### DQN Features

- **Dual Network Architecture**: Main network for action selection, target network for stable Q-value targets
- **Experience Replay**: Stores up to 10,000 experiences with random batch sampling
- **Exploration Decay**: Epsilon starts at 1.0 and decays to 0.01 over time
- **Batch Training**: Configurable batch size (default 32) for efficient learning
- **Target Updates**: Target network updated every 100 training steps
- **Performance Monitoring**: Tracks loss, exploration rate, and learning progress
- **Save/Load Support**: Compatible with behavior profile persistence system

### Testing

- **DQNAgentTest**: Comprehensive unit tests for all DQN functionality
- **DQNAgentDemo**: Interactive demo showing learning in action
- **Integration Tests**: Verified compatibility with existing RL system components

### BehaviorProfileManager System (Task 7)

- **Complete Persistence System**: Full implementation of behavior profile save/load functionality
- **Multi-Player Support**: Isolated profiles per player with proper file naming and caching
- **Compression System**: Automatic compression for large profiles to save storage space
- **Backup and Restore**: Complete backup system for profile data protection
- **Error Handling**: Robust error handling with checksum validation and corruption detection
- **Storage Management**: Profile cleanup, size monitoring, and space optimization
- **Comprehensive Testing**: Full unit test suite covering all functionality and edge cases

### BehaviorProfileManager Features

- **Serialization System**: JSON-based serialization with checksum integrity validation
- **File Management**: Safe file operations with temporary file backup during writes
- **Profile Caching**: In-memory caching for improved performance and reduced I/O
- **Compression**: Automatic weight compression for profiles larger than 1KB threshold
- **Multi-Player Isolation**: Separate profile storage per player with proper isolation
- **Backup System**: Complete backup and restore functionality for data protection
- **Storage Optimization**: Profile cleanup, compression, and size monitoring tools
- **Validation**: Comprehensive profile validation including NaN/Infinity detection

### TrainingCoordinator System (Task 8)

- **Complete Training Management**: Full implementation of training/inference mode management with state preservation
- **Mode Switching**: Seamless switching between Training, Inference, and Mixed modes with agent state updates
- **Learning Progress Tracking**: Comprehensive tracking of learning metrics with event-based progress updates
- **State Preservation**: Automatic saving and loading of training state during mode transitions (Requirement 3.4)
- **Performance Optimization**: Adaptive processing with frame time constraints and round-robin agent updates
- **Multi-Agent Coordination**: Efficient management of multiple learning agents with type-based organization
- **Auto-Save Functionality**: Periodic automatic saving of behavior profiles and training state
- **Error Handling**: Robust error handling with graceful degradation and recovery mechanisms

### TrainingCoordinator Features

- **Training Mode Management**: Full support for Training, Inference, and Mixed modes with intelligent agent selection
- **State Preservation**: Complete state preservation during mode transitions with JSON-based serialization
- **Progress Tracking**: Real-time learning progress tracking with metrics aggregation and event notifications
- **Performance Monitoring**: Frame time monitoring with adaptive processing to maintain 60 FPS performance
- **Agent Lifecycle**: Complete agent registration/unregistration with proper cleanup and resource management
- **Batch Operations**: Efficient batch operations for saving/loading profiles and resetting progress
- **Event System**: Comprehensive event system for mode changes and progress updates
- **Mixed Mode Intelligence**: Smart agent selection in mixed mode based on learning convergence status

### Testing

- **TrainingCoordinatorTest**: Comprehensive unit tests covering all functionality and edge cases
- **TrainingCoordinatorDemo**: Interactive demo showing all features in action with mock agents
- **Integration Tests**: Verified compatibility with existing RL system components and BehaviorProfileManager

## Next Steps

The foundation is now ready for implementing the specific components in subsequent tasks:

- Task 2: StateEncoder and game state representation ✅ COMPLETED
- Task 3: ActionDecoder and action space management ✅ COMPLETED  
- Task 4: RewardCalculator system ✅ COMPLETED
- Task 5: Complete DQN algorithm implementation ✅ COMPLETED
- Task 6: RLMonster component integration
- Task 7: Full BehaviorProfileManager implementation ✅ COMPLETED
- Task 8: Complete TrainingCoordinator implementation ✅ COMPLETED

## Architecture Notes

The system follows a modular design where each component can be developed and tested independently. All interfaces are designed to support both Unity ML-Agents integration and custom implementations, providing flexibility for future enhancements.

The performance monitoring and constraints ensure the RL system won't impact game performance, maintaining the 60 FPS target while providing intelligent monster behavior.
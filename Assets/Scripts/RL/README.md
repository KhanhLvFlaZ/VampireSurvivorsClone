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
- **DQNLearningAgent**: Deep Q-Network agent implementation (placeholder)

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

## Next Steps

The foundation is now ready for implementing the specific components in subsequent tasks:

- Task 2: StateEncoder and game state representation ✅ COMPLETED
- Task 3: ActionDecoder and action space management ✅ COMPLETED  
- Task 4: RewardCalculator system ✅ COMPLETED
- Task 5: Complete DQN algorithm implementation
- Task 6: RLMonster component integration
- Task 7: Full BehaviorProfileManager implementation
- Task 8: Complete TrainingCoordinator implementation

## Architecture Notes

The system follows a modular design where each component can be developed and tested independently. All interfaces are designed to support both Unity ML-Agents integration and custom implementations, providing flexibility for future enhancements.

The performance monitoring and constraints ensure the RL system won't impact game performance, maintaining the 60 FPS target while providing intelligent monster behavior.
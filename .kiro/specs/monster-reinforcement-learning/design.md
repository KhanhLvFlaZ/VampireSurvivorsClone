# Monster Reinforcement Learning System Design

## Overview

The Monster Reinforcement Learning System enhances the existing monster AI in the Vampire Survivors Clone by integrating adaptive learning capabilities. This system uses Deep Q-Network (DQN) algorithms to enable monsters to learn optimal strategies for engaging players, making the game progressively more challenging and engaging.

The system is designed as a modular extension to the existing Monster class hierarchy, ensuring compatibility with current game mechanics while adding sophisticated AI behaviors. Monsters will learn from player actions, adapt their strategies, and coordinate with other monsters to create dynamic, evolving gameplay experiences.

## Architecture

### Core Components

```
RLMonsterSystem
├── LearningAgent (DQN Implementation)
├── StateEncoder (Game State → Neural Network Input)
├── ActionDecoder (Neural Network Output → Game Actions)
├── RewardCalculator (Experience → Learning Signals)
├── BehaviorProfileManager (Save/Load Learned Behaviors)
└── TrainingCoordinator (Manages Learning Process)
```

### Integration with Existing Systems

The RL system integrates with existing components:
- **Monster Class**: Extended with `RLMonster` component
- **EntityManager**: Enhanced to support RL monster spawning and management
- **LevelManager**: Modified to handle training/inference mode switching
- **GameState**: Extended to track RL-specific metrics and settings

## Components and Interfaces

### 1. LearningAgent Interface

```csharp
public interface ILearningAgent
{
    void Initialize(MonsterType monsterType, ActionSpace actionSpace);
    int SelectAction(GameState state, bool isTraining);
    void StoreExperience(GameState state, int action, float reward, GameState nextState, bool done);
    void UpdatePolicy();
    void SaveBehaviorProfile(string filePath);
    void LoadBehaviorProfile(string filePath);
}
```

### 2. StateEncoder

Converts game state into neural network input:
- **Player State**: Position (2D), velocity (2D), health (1D), active abilities (bit vector)
- **Monster State**: Position (2D), health (1D), current action (1D), time since last action (1D)
- **Environment State**: Nearby monsters (up to 5, each with position + type), collectibles in range (count + types)
- **Temporal State**: Time since spawn (1D), time since last player damage (1D)

Total input size: ~32 dimensions per monster

### 3. ActionDecoder

Maps neural network outputs to game actions:
- **Movement Actions**: 8 directional movements + stop (9 actions)
- **Attack Actions**: Primary attack, special attack, defensive stance (3 actions)
- **Tactical Actions**: Retreat, coordinate with nearby monsters, ambush (3 actions)

Total action space: 15 discrete actions per monster type

### 4. RewardCalculator

Calculates learning rewards based on monster performance:

**Immediate Rewards:**
- Damage dealt to player: +10 to +50 (based on damage amount)
- Successful hit on player: +25
- Avoiding player damage: +5 per second
- Coordinated attack with other monsters: +15

**Terminal Rewards:**
- Monster death: -100
- Player death (monster contributed): +200
- Survival beyond average lifespan: +50

**Shaped Rewards:**
- Distance-based: Reward for optimal positioning relative to player
- Timing-based: Reward for well-timed attacks and retreats

## Data Models

### GameState Structure
```csharp
public struct RLGameState
{
    public Vector2 playerPosition;
    public Vector2 playerVelocity;
    public float playerHealth;
    public uint activeAbilities; // Bit flags
    
    public Vector2 monsterPosition;
    public float monsterHealth;
    public int currentAction;
    public float timeSinceLastAction;
    
    public NearbyMonster[] nearbyMonsters; // Max 5
    public CollectibleInfo[] nearbyCollectibles; // Max 10
    
    public float timeAlive;
    public float timeSincePlayerDamage;
}
```

### BehaviorProfile Structure
```csharp
[Serializable]
public class BehaviorProfile
{
    public string monsterType;
    public float[] networkWeights;
    public float[] networkBiases;
    public int trainingEpisodes;
    public float averageReward;
    public DateTime lastUpdated;
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*
### Property Reflection

After reviewing all identified properties, several can be consolidated to eliminate redundancy:

**Consolidations:**
- Properties 1.4 and 1.5 (reward system responses) can be combined into a comprehensive reward system property
- Properties 2.4 and 2.5 (learning stability and independence) can be merged into a learning isolation property
- Properties 3.2 and 3.3 (training vs inference modes) can be combined into a mode-specific behavior property
- Properties 4.1 and 4.2 (save/load functionality) can be merged into a persistence round-trip property
- Properties 6.2 and 6.3 (performance constraints) can be combined into a performance limits property

**Final Properties:**

Property 1: Strategy adaptation learning
*For any* repeated player strategy, monsters should adapt their behavior to counter that strategy within a reasonable timeframe
**Validates: Requirements 1.1, 1.2**

Property 2: Individual learning progress tracking
*For any* set of multiple RL monsters, each monster type should maintain independent learning progress without interference
**Validates: Requirements 1.3, 2.5**

Property 3: Reward system correctness
*For any* monster action outcome (damage dealt, death, survival), the reward system should provide appropriate positive or negative reinforcement
**Validates: Requirements 1.4, 1.5**

Property 4: Interface compatibility
*For any* new monster type integration, the learning agent should work with existing Monster class interfaces without breaking functionality
**Validates: Requirements 2.1, 6.5**

Property 5: Configuration flexibility
*For any* monster type, action spaces and reward functions should be configurable through ScriptableObjects
**Validates: Requirements 2.2, 2.3**

Property 6: Learning stability
*For any* monster that reaches learning capacity, behavior should remain stable without performance degradation
**Validates: Requirements 2.4**

Property 7: Mode-specific behavior
*For any* game mode (training/inference), monsters should exhibit appropriate learning behavior - active learning in training mode, static behavior in inference mode
**Validates: Requirements 3.2, 3.3**

Property 8: State persistence during mode switching
*For any* mode change operation, current learning progress should be preserved
**Validates: Requirements 3.4**

Property 9: Fallback behavior
*For any* scenario where pre-trained models are unavailable or corrupted, monsters should use default scripted behavior
**Validates: Requirements 3.5, 4.3**

Property 10: Persistence round-trip
*For any* behavior profile, saving then loading should preserve the learned behavior
**Validates: Requirements 4.1, 4.2**

Property 11: Profile compression and isolation
*For any* storage or multi-player scenario, behavior profiles should be efficiently compressed and maintained separately per player
**Validates: Requirements 4.4, 4.5**

Property 12: Coordination learning
*For any* group of monsters, learned coordination behaviors should result in synchronized tactical actions
**Validates: Requirements 5.2**

Property 13: Avoidance adaptation
*For any* previously effective player strategy, monsters should learn to avoid or counter that strategy
**Validates: Requirements 5.3**

Property 14: Difficulty scaling
*For any* difficulty level, monsters should demonstrate progressively more sophisticated learned behaviors
**Validates: Requirements 5.5**

Property 15: System integration compatibility
*For any* interaction with existing systems (EntityManager, object pooling), RL monsters should maintain full compatibility
**Validates: Requirements 6.1, 6.4**

Property 16: Performance constraints
*For any* frame update, RL processing should not exceed 16ms execution time and total memory usage should stay under 100MB
**Validates: Requirements 6.2, 6.3**

## Error Handling

### Learning Failures
- **Network Convergence Issues**: Implement learning rate decay and early stopping
- **Memory Overflow**: Automatic experience buffer pruning and model compression
- **Invalid Actions**: Action masking and fallback to valid actions

### Data Corruption
- **Profile Corruption**: Checksum validation and automatic fallback to default behavior
- **Save/Load Errors**: Graceful degradation with error logging
- **Network Architecture Mismatches**: Version compatibility checks

### Performance Degradation
- **Frame Rate Drops**: Adaptive batch sizing and processing throttling
- **Memory Leaks**: Automatic garbage collection and resource cleanup
- **Training Instability**: Curriculum learning and reward clipping

## Testing Strategy

### Unit Testing Approach
Unit tests will focus on:
- Individual component functionality (StateEncoder, ActionDecoder, RewardCalculator)
- Data serialization/deserialization correctness
- Interface compatibility with existing Monster classes
- Error handling and edge cases

### Property-Based Testing Approach
Property-based tests will use **Unity Test Framework** with custom generators to verify:
- Learning convergence properties across different scenarios
- Behavioral adaptation under various player strategies
- System performance under stress conditions
- Data persistence and integrity across save/load cycles

**Configuration**: Each property-based test will run a minimum of 100 iterations to ensure statistical significance.

**Test Tagging**: Each property-based test will be tagged with comments explicitly referencing the correctness property using the format: '**Feature: monster-reinforcement-learning, Property {number}: {property_text}**'

### Integration Testing
- Full gameplay scenarios with RL monsters
- Performance benchmarking under various load conditions
- Compatibility testing with existing game systems
- Multi-session learning persistence validation

### Testing Tools
- **Primary Framework**: Unity Test Framework for C# testing
- **Performance Profiling**: Unity Profiler for memory and CPU monitoring
- **Custom Generators**: Procedural game state generation for property testing
- **Mock Systems**: Simulated player behaviors for consistent testing

The dual testing approach ensures both concrete functionality verification through unit tests and general correctness validation through property-based testing, providing comprehensive coverage of the RL system's behavior.
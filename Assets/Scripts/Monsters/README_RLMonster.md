# RLMonster Component

The `RLMonster` component extends the base `Monster` class with reinforcement learning capabilities, enabling monsters to adapt their behavior based on player actions and game state.

## Overview

The RLMonster integrates a Deep Q-Network (DQN) learning agent into the existing monster behavior system, allowing monsters to:
- Learn optimal strategies for attacking, positioning, and coordinating
- Adapt to player tactics over time
- Switch between training and inference modes
- Save and load learned behaviors

## Key Features

### 1. **Adaptive Behavior**
- Monsters learn from experience and adapt their strategies
- Counter-adaptation to repeated player tactics
- Individual learning progress per monster type

### 2. **Flexible Configuration**
- Configurable action spaces per monster type
- Customizable reward functions through ScriptableObjects
- Training/inference mode switching

### 3. **Seamless Integration**
- Extends existing Monster class without breaking compatibility
- Works with current EntityManager and object pooling
- Maintains performance requirements (16ms per frame)

## Usage

### Basic Setup

1. **Add RLMonster Component**
   ```csharp
   // Add to existing monster prefab or create new one
   RLMonster rlMonster = monsterGameObject.AddComponent<RLMonster>();
   ```

2. **Configure RL Settings**
   ```csharp
   rlMonster.SetRLEnabled(true);
   rlMonster.SetTrainingMode(true); // or false for inference only
   ```

3. **Initialize with Game Systems**
   ```csharp
   rlMonster.Init(entityManager, playerCharacter);
   rlMonster.Setup(monsterIndex, spawnPosition, monsterBlueprint);
   ```

### Configuration

#### MonsterRLConfig ScriptableObject
Create and configure monster-specific RL settings:
- Action space configuration (movement, combat, tactical actions)
- Learning parameters (learning rate, exploration rate)
- Reward function settings
- Network architecture options

#### Training vs Inference Mode
- **Training Mode**: Monsters actively learn and update behavior
- **Inference Mode**: Monsters use pre-trained behavior without learning

### Action Types

The RLMonster supports various action types:
- **Movement**: 8-directional movement with variable intensity
- **Attack**: Basic and special attacks with timing
- **Retreat**: Tactical withdrawal from dangerous situations
- **Coordinate**: Group tactics with other monsters
- **Defensive**: Defensive stances and damage mitigation
- **Ambush**: Surprise attacks and positioning
- **Wait**: Strategic pausing and observation

### State Observation

The monster observes:
- **Player State**: Position, velocity, health, active abilities
- **Monster State**: Own position, health, current action
- **Environment**: Nearby monsters, collectibles, obstacles
- **Temporal**: Time alive, time since last player damage

### Reward System

Configurable reward functions include:
- **Immediate Rewards**: Damage dealt, successful hits, survival
- **Terminal Rewards**: Death penalty, player kill bonus
- **Shaped Rewards**: Distance optimization, health maintenance
- **Coordination Rewards**: Successful group tactics

## API Reference

### Core Methods

```csharp
// Enable/disable RL behavior
void SetRLEnabled(bool enabled)

// Set training mode
void SetTrainingMode(bool training)

// Get current learning metrics
LearningMetrics GetLearningMetrics()

// Save/load behavior profiles
void SaveBehaviorProfile(string filePath)
void LoadBehaviorProfile(string filePath)

// Force start new episode (for testing)
void ForceStartNewEpisode()

// Get RL status for debugging
string GetRLStatus()
```

### Configuration Properties

```csharp
[SerializeField] private MonsterRLConfig rlConfig;
[SerializeField] private bool enableRL = true;
[SerializeField] private bool isTrainingMode = true;
[SerializeField] private MonsterType monsterType = MonsterType.Melee;
[SerializeField] private float actionInterval = 0.2f;
```

## Integration Examples

### Example 1: Basic RL Monster Setup
```csharp
public class RLMonsterSpawner : MonoBehaviour
{
    public void SpawnRLMonster(Vector2 position)
    {
        GameObject monsterObj = Instantiate(monsterPrefab, position, Quaternion.identity);
        RLMonster rlMonster = monsterObj.GetComponent<RLMonster>();
        
        rlMonster.SetRLEnabled(true);
        rlMonster.SetTrainingMode(GameSettings.IsTrainingMode);
        rlMonster.Init(entityManager, player);
        rlMonster.Setup(0, position, monsterBlueprint);
    }
}
```

### Example 2: Training Mode Management
```csharp
public class RLTrainingManager : MonoBehaviour
{
    public void ToggleTrainingMode(bool training)
    {
        var rlMonsters = FindObjectsOfType<RLMonster>();
        foreach (var monster in rlMonsters)
        {
            monster.SetTrainingMode(training);
        }
    }
    
    public void SaveAllProfiles()
    {
        var rlMonsters = FindObjectsOfType<RLMonster>();
        for (int i = 0; i < rlMonsters.Length; i++)
        {
            string path = $"{Application.persistentDataPath}/monster_{i}.json";
            rlMonsters[i].SaveBehaviorProfile(path);
        }
    }
}
```

## Performance Considerations

- **Frame Time**: RL processing is limited to 16ms per frame
- **Memory Usage**: Total RL memory consumption stays under 100MB
- **Batch Processing**: Learning updates are batched for efficiency
- **Adaptive Throttling**: Processing scales based on frame rate

## Debugging

### Debug Information
```csharp
// Get learning status
string status = rlMonster.GetRLStatus();
Debug.Log(status); // "Episodes: 150, Avg Reward: 23.5, Exploration: 0.15, Training: True"

// Get detailed metrics
LearningMetrics metrics = rlMonster.GetLearningMetrics();
Debug.Log($"Episodes: {metrics.episodeCount}, Best Reward: {metrics.bestReward}");
```

### Visual Debugging
- Use Scene view gizmos to visualize monster states
- Monitor learning progress through metrics
- Debug UI for real-time RL statistics

## Requirements Validation

The RLMonster component satisfies the following requirements:
- **2.1**: Uses common interface compatible with existing Monster classes
- **6.1**: Maintains compatibility with EntityManager functionality  
- **6.5**: Extends rather than replaces current Monster class functionality

## Testing

Unit tests are available in `Assets/Scripts/RL/Tests/RLMonsterTest.cs`:
- Component creation and initialization
- RL system integration
- Action selection and execution
- State observation and reward calculation
- Training mode functionality

## Troubleshooting

### Common Issues

1. **Monster not learning**: Check that training mode is enabled and RL config is assigned
2. **Performance issues**: Verify action interval settings and batch sizes
3. **Behavior not saving**: Ensure write permissions for save directory
4. **Integration conflicts**: Check for missing required components (Rigidbody2D, Colliders)

### Error Messages

- `"RL System not initialized"`: Call Init() before Setup()
- `"Invalid action space"`: Check MonsterRLConfig action space configuration
- `"Learning agent not found"`: Ensure DQNLearningAgent component is present
# [Component Name] - Component Context

> **Template for Tier 2 (Component-Level) Documentation**
> 
> **Use this template when creating component-level CONTEXT.md files**
> 
> Copy this to your component folder (e.g., `Assets/Scripts/CONTEXT.md`) and fill in the sections.

## Purpose

[Brief description of what this component does in the project]

**Example**:
> This component contains all core game scripts including managers, controllers, and fundamental game systems for Mai's Love Story.

## Architecture

[High-level design and organizational structure]

**Example**:
> The component is organized into subsystems:
> - **Core/**: Singleton managers (GameManager, SceneManager)
> - **DialogueSystem/**: Dialogue and story logic
> - **Characters/**: Character controllers and management
> - **UI/**: UI controllers and components

### Component Diagram (Optional)

```
[Component]
├── [Subsystem 1]
├── [Subsystem 2]
└── [Subsystem 3]
```

## Key Classes

List the most important classes with their responsibilities.

### [ClassName]
- **Purpose**: [What this class does]
- **Pattern**: [Design pattern used, if applicable]
- **Responsibilities**: 
  - [Responsibility 1]
  - [Responsibility 2]

**Example**:
> ### GameManager
> - **Purpose**: Central game state and coordination
> - **Pattern**: Singleton with DontDestroyOnLoad
> - **Responsibilities**:
>   - Game initialization and shutdown
>   - Global state management
>   - System coordination

### [Another ClassName]
[Continue for 3-5 most important classes]

## Public APIs

Document public methods and properties that other systems use.

### [ClassName]

```csharp
// Key public methods
public static [ClassName] Instance { get; }
public void MethodName(params)
public PropertyType PropertyName { get; set; }
```

**Example**:
> ### DialogueManager
> ```csharp
> public static DialogueManager Instance { get; }
> public void LoadDialogue(string dialogueId)
> public void AdvanceDialogue()
> public bool IsDialogueActive { get; }
> public event Action<string> OnDialogueUpdated;
> ```

## Integration Points

How this component integrates with other systems in the project.

**Example**:
> - **FMOD Audio**: Dialogue triggers sound events via FMODUnity.RuntimeManager
> - **Live2D Characters**: Dialogue controls character expressions through CubismExpressionController
> - **Localization**: Text retrieved from Unity Localization based on current locale
> - **UI System**: Updates dialogue display elements via events

### Integration Diagram (Optional)

```
[This Component]
    ↓
  [System A]
    ↓
  [System B]
```

## Common Patterns

Design patterns and coding conventions used within this component.

**Example**:
> - **Singleton Pattern**: Used for all manager classes (GameManager, DialogueManager, AudioManager)
> - **Event-Driven**: Systems communicate via C# events and UnityEvents
> - **ScriptableObjects**: Configuration stored as ScriptableObject assets
> - **Async/Await**: Addressables loading uses async/await pattern

## Configuration

Settings, ScriptableObjects, or configuration files used by this component.

**Example**:
> - **Game Settings**: `Assets/Settings/GameSettings.asset` - Core game configuration
> - **Dialogue Settings**: `Assets/Settings/DialogueSettings.asset` - Dialogue speed, display options
> - **Character Data**: `Assets/Resources/CharacterData/` - Character ScriptableObjects

## Usage Examples

Common usage patterns for this component.

### Example: [Common Task]

```csharp
// Example code showing how to use this component
var instance = ComponentName.Instance;
instance.MethodName(params);
```

**Example**:
> ### Example: Loading Dialogue
> ```csharp
> // Load and display dialogue
> var dialogueManager = DialogueManager.Instance;
> dialogueManager.LoadDialogue("DLG_001");
> 
> // Subscribe to dialogue events
> dialogueManager.OnDialogueUpdated += HandleDialogueUpdate;
> ```

## Dependencies

External libraries, Unity packages, or other components this depends on.

**Example**:
> - Unity Addressables
> - Unity Localization
> - FMOD Unity Integration
> - Live2D Cubism SDK
> - TextMesh Pro

## File Structure

```
[Component Folder]/
├── [Subfolder 1]/
│   ├── [File1].cs
│   └── [File2].cs
├── [Subfolder 2]/
└── [Main Script].cs
```

**Example**:
> ```
> Assets/Scripts/
> ├── Core/
> │   ├── GameManager.cs
> │   └── SceneManager.cs
> ├── DialogueSystem/
> │   ├── DialogueManager.cs
> │   ├── DialogueData.cs
> │   └── DialogueUI.cs
> └── Utilities/
>     └── Extensions.cs
> ```

## Performance Considerations

Any performance-related notes specific to this component.

**Example**:
> - Cache component references in Awake() to avoid GetComponent() in Update()
> - Use object pooling for frequently instantiated UI elements
> - Addressables loaded asynchronously to prevent frame drops

## Known Issues / Limitations

Current known issues or limitations (if any).

**Example**:
> - Dialogue system doesn't support branching choices yet (planned)
> - Character expression transitions can be jarring with very fast dialogue

## Future Improvements

Planned enhancements or refactoring.

**Example**:
> - Add dialogue branching system
> - Implement dialogue history/log
> - Add voice playback synchronization
> - Refactor DialogueManager to separate UI concerns

## Related Documentation

Links to related documentation files.

**Example**:
> - **Tier 3 Feature Docs**: 
>   - `Assets/Scripts/DialogueSystem/CONTEXT.md` - Dialogue implementation details
>   - `Assets/Scripts/SaveSystem/CONTEXT.md` - Save system details
> - **Integration Patterns**: `docs/ai-context/system-integration.md`
> - **Coding Standards**: `MCP-ASSISTANT-RULES.md`

---

**Last Updated**: [Date]  
**Maintained By**: [Your Name / Team]










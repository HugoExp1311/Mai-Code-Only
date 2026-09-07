# [Feature Name] - Feature Context

> **Template for Tier 3 (Feature-Level) Documentation**
> 
> **Use this template when creating feature-specific CONTEXT.md files**
> 
> Copy this to your feature folder (e.g., `Assets/Scripts/DialogueSystem/CONTEXT.md`) and fill in the sections.

## Overview

[Brief description of what this feature does]

**Example**:
> The Dialogue System manages story progression, dialogue display, character integration, and player choices in Mai's Love Story.

## Implementation

[How the feature is implemented at a high level]

**Example**:
> The dialogue system uses a CSV-based data structure for story content, with support for three languages. It coordinates with the Live2D character system for expressions, FMOD for audio, and the Unity Localization package for multi-language text.

## Code Structure

### Main Files

| File | Purpose |
|------|---------|
| [Filename.cs] | [What this file does] |
| [Filename2.cs] | [What this file does] |

**Example**:
> | File | Purpose |
> |------|---------|
> | DialogueManager.cs | Core dialogue flow management |
> | DialogueData.cs | Data model for dialogue entries |
> | DialogueUI.cs | UI presentation and text display |
> | CSVParser.cs | Parses dialogue CSV files |

### Class Diagram (Optional)

```
[MainClass]
    ↓
[HelperClass1]
    ↓
[HelperClass2]
```

## Key Classes

### [ClassName]

**Purpose**: [What this class does]

**Key Methods**:
```csharp
public void MethodName(params)
private void HelperMethod(params)
```

**Responsibilities**:
- [Responsibility 1]
- [Responsibility 2]

**Example**:
> ### DialogueManager
> 
> **Purpose**: Manages dialogue flow, CSV loading, and event coordination
> 
> **Key Methods**:
> ```csharp
> public void LoadDialogue(string dialogueId)
> public void AdvanceDialogue()
> private void ParseCSVLine(string line)
> private void TriggerCharacterExpression(string expression)
> ```
> 
> **Responsibilities**:
> - Load and parse dialogue CSV
> - Manage dialogue progression
> - Coordinate with character system
> - Trigger audio events
> - Emit UI update events

## Data Structures

### [StructName / ClassName]

```csharp
[Serializable]
public class DataStructure
{
    public Type fieldName;
}
```

**Purpose**: [What this data structure represents]

**Example**:
> ### DialogueEntry
> 
> ```csharp
> [Serializable]
> public class DialogueEntry
> {
>     public string id;
>     public string characterName;
>     public string englishText;
>     public string vietnameseText;
>     public string japaneseText;
>     public string expression;
>     public string audioEvent;
>     public string nextDialogueId;
> }
> ```
> 
> **Purpose**: Represents a single dialogue line with all language variants and metadata

## Algorithms & Logic

### [Algorithm Name]

**Purpose**: [What this algorithm does]

**Pseudocode** (or actual code):
```
1. Step one
2. Step two
3. Step three
```

**Example**:
> ### Dialogue Text Typewriter Effect
> 
> **Purpose**: Displays dialogue text character-by-character for visual novel effect
> 
> **Algorithm**:
> ```csharp
> IEnumerator TypewriterEffect(string text, float speed)
> {
>     dialogueText.text = "";
>     foreach (char c in text)
>     {
>         dialogueText.text += c;
>         PlayTextBeepSound();
>         yield return new WaitForSeconds(speed);
>     }
>     isTyping = false;
> }
> ```

## Integration

### With [Other System]

**How**: [Description of integration]

**Code Example**:
```csharp
// Example code
```

**Example**:
> ### With Live2D Character System
> 
> **How**: Dialogue manager triggers character expression changes via CubismExpressionController
> 
> **Code Example**:
> ```csharp
> private void UpdateCharacterExpression(string expression)
> {
>     if (currentCharacter != null)
>     {
>         var expressionController = currentCharacter.GetComponent<CubismExpressionController>();
>         expressionController.StartExpression(expression);
>     }
> }
> ```

## Edge Cases

### [Edge Case Description]

**Problem**: [What can go wrong]  
**Solution**: [How it's handled]

**Example**:
> ### Missing Dialogue ID
> 
> **Problem**: Requesting a dialogue ID that doesn't exist in CSV
> **Solution**: Log error and display fallback message; don't crash
> 
> ```csharp
> public bool LoadDialogue(string id)
> {
>     if (!dialogueData.ContainsKey(id))
>     {
>         Debug.LogError($"Dialogue ID '{id}' not found");
>         DisplayFallbackMessage();
>         return false;
>     }
>     // ... normal loading
> }
> ```

### [Another Edge Case]
[Continue for important edge cases]

## Configuration

### Settings

[List configuration options and where they're defined]

**Example**:
> ### Dialogue Settings (ScriptableObject)
> 
> Location: `Assets/Settings/DialogueSettings.asset`
> 
> - `textSpeed`: Characters per second for typewriter effect
> - `autoAdvanceDelay`: Seconds before auto-advance (if enabled)
> - `skipEnabled`: Allow dialogue skipping
> - `textBeepEvent`: FMOD event path for text beep sound

### Data Files

[List data files used by this feature]

**Example**:
> - **Dialogue CSV**: `Assets/Resources/Dialogues_3Language.csv`
>   - Format: ID, Character, English, Vietnamese, Japanese, Expression, Audio Event
> - **Character Data**: `Assets/Resources/CharacterData/*.asset`

## Performance Notes

[Performance considerations specific to this feature]

**Example**:
> - CSV is loaded once at startup and cached in memory (trade-off: memory vs. load time)
> - Typewriter coroutine is lightweight but avoid running multiple simultaneously
> - Addressables used for character sprites to manage memory
> - FMOD events are lightweight one-shots for text beeps

## Testing

### Test Scenarios

- [ ] [Test scenario 1]
- [ ] [Test scenario 2]

**Example**:
> ### Test Scenarios
> 
> - [ ] Load dialogue with valid ID
> - [ ] Handle missing dialogue ID gracefully
> - [ ] Switch languages mid-dialogue
> - [ ] Character expressions change correctly
> - [ ] Audio events trigger at right time
> - [ ] Typewriter effect skippable
> - [ ] Dialogue progression works with input

### Known Test Gaps

[Areas that need more testing]

**Example**:
> - Edge case: Extremely long dialogue lines (>1000 characters)
> - Performance: Rapidly skipping through dialogue
> - Integration: Character change during mid-dialogue

## Code Examples

### [Common Use Case]

```csharp
// Code example
```

**Example**:
> ### Loading and Displaying Dialogue
> 
> ```csharp
> // Subscribe to events
> DialogueManager.Instance.OnDialogueUpdated += (text) => {
>     dialogueTextUI.text = text;
> };
> 
> DialogueManager.Instance.OnCharacterChanged += (character) => {
>     characterNameUI.text = character;
> };
> 
> // Load dialogue sequence
> DialogueManager.Instance.LoadDialogue("DLG_INTRO_001");
> 
> // Advance on input
> if (Input.GetKeyDown(KeyCode.Space))
> {
>     DialogueManager.Instance.AdvanceDialogue();
> }
> ```

## Debugging

### Common Issues

**Issue**: [Problem description]  
**Cause**: [What causes it]  
**Fix**: [How to resolve]

**Example**:
> ### Common Issues
> 
> **Issue**: Text not displaying in certain languages  
> **Cause**: Missing font characters for Vietnamese/Japanese  
> **Fix**: Ensure TextMesh Pro font asset includes required character sets
> 
> **Issue**: Character expression not changing  
> **Cause**: Expression name mismatch  
> **Fix**: Verify expression names match Live2D model definitions

### Debug Commands

[Console commands or tools for debugging]

**Example**:
> ```csharp
> [ContextMenu("Debug: List All Dialogues")]
> private void DebugListDialogues()
> {
>     foreach (var id in dialogueData.Keys)
>     {
>         Debug.Log($"Dialogue: {id}");
>     }
> }
> ```

## Future Improvements

- [ ] [Planned improvement 1]
- [ ] [Planned improvement 2]

**Example**:
> - [ ] Implement dialogue branching with player choices
> - [ ] Add dialogue history/log feature
> - [ ] Support for character voice acting
> - [ ] Visual effects during dialogue (screen shake, flash, etc.)
> - [ ] Refactor to separate UI concerns from logic

## Dependencies

### Required

- [Dependency 1]
- [Dependency 2]

**Example**:
> ### Required
> 
> - Unity Localization package
> - FMOD Unity Integration
> - Live2D Cubism SDK
> - TextMesh Pro

### Optional

- [Optional dependency with benefit]

**Example**:
> ### Optional
> 
> - DOTween (for smoother UI animations) - currently using Unity built-in

## Migration Notes

[If this replaced an older system, note migration steps]

**Example**:
> Migrated from Unity's built-in audio to FMOD:
> - All `AudioSource.PlayClipAtPoint()` calls replaced with `FMODUnity.RuntimeManager.PlayOneShot()`
> - Audio clips converted to FMOD events
> - Old audio assets archived in `Assets/Audio_OLD/`

## Related Documentation

**Example**:
> - **Parent Component**: `Assets/Scripts/CONTEXT.md`
> - **Integration Patterns**: `docs/ai-context/system-integration.md`
> - **Live2D Integration**: `Assets/Live2D/CONTEXT.md`
> - **Audio System**: `Audio System/CONTEXT.md`
> - **Coding Standards**: `MCP-ASSISTANT-RULES.md`

---

**Last Updated**: [Date]  
**Maintained By**: [Your Name / Team]










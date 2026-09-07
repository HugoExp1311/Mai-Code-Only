# System Integration Patterns

This document describes how the major systems in Mai's Love Story integrate and communicate with each other.

## Core System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Game Manager                            │
│              (Central coordination, game state)              │
└───────────┬──────────────────────────────────┬──────────────┘
            │                                  │
    ┌───────▼────────┐                 ┌──────▼──────────┐
    │ Scene Manager  │                 │ Audio Manager   │
    │  (Transitions) │                 │  (FMOD control) │
    └───────┬────────┘                 └──────┬──────────┘
            │                                  │
    ┌───────▼─────────────────────────────────▼──────────┐
    │           Dialogue Manager                          │
    │    (Story flow, text display, character control)    │
    └──┬─────────┬────────────┬─────────────┬────────────┘
       │         │            │             │
   ┌───▼──┐  ┌──▼───────┐ ┌──▼─────────┐ ┌─▼──────────────┐
   │ UI   │  │ Live2D   │ │Localization│ │ Input Handler  │
   │System│  │Character │ │  System    │ │                │
   └──────┘  └──────────┘ └────────────┘ └────────────────┘
```

## Integration Patterns

### 1. Dialogue System Integration

#### Dialogue → Live2D Characters
**Pattern**: Event-driven expression control

```csharp
// Dialogue manager triggers character expressions
OnDialogueLine(DialogueData data) {
    if (data.characterExpression != null) {
        characterController.SetExpression(data.characterExpression);
    }
}
```

**Data Flow**:
1. Dialogue manager reads CSV line
2. Parses character ID and expression
3. Sends expression command to Live2D character controller
4. Character smoothly transitions to new expression

#### Dialogue → Audio System
**Pattern**: FMOD event triggering

```csharp
// Dialogue triggers audio events
OnDialogueStart(DialogueData data) {
    if (!string.IsNullOrEmpty(data.audioEvent)) {
        FMODUnity.RuntimeManager.PlayOneShot(data.audioEvent);
    }
}
```

**Audio Events**:
- `event:/UI/TextBeep` - Text display sound
- `event:/Voice/Character/Line_XX` - Character voice lines
- `event:/Ambience/Scene_XX` - Background ambience changes

#### Dialogue → Localization
**Pattern**: Localized text retrieval

```csharp
// Dialogue loads text based on current language
string GetLocalizedDialogue(string dialogueId) {
    var currentLocale = LocalizationSettings.SelectedLocale;
    return dialogueTable.GetEntry(dialogueId).GetLocalizedString();
}
```

**CSV Structure**:
```csv
ID,Character,English,Vietnamese,Japanese
DLG_001,Mai,"Hello!","Xin chào!","こんにちは！"
```

#### Dialogue → UI System
**Pattern**: Event subscription and UI update

```csharp
// UI subscribes to dialogue events
void OnEnable() {
    DialogueManager.OnDialogueUpdate += UpdateDialogueUI;
    DialogueManager.OnChoicePresented += ShowChoices;
}

void UpdateDialogueUI(DialogueData data) {
    characterNameText.text = data.characterName;
    dialogueText.text = data.dialogueText;
}
```

### 2. Live2D Character System Integration

#### Character Loading Pattern
**Pattern**: Addressables + Prefab instantiation

```csharp
// Load Live2D character via Addressables
async Task<GameObject> LoadCharacter(string characterId) {
    var handle = Addressables.LoadAssetAsync<GameObject>(
        $"Characters/{characterId}_Prefab"
    );
    await handle.Task;
    return Instantiate(handle.Result, characterSpawnPoint);
}
```

#### Expression Control Pattern
**Pattern**: Parameter-based animation

```csharp
// Live2D character expression control
public void SetExpression(string expressionName) {
    var cubismModel = GetComponent<CubismModel>();
    var expressionController = GetComponent<CubismExpressionController>();
    
    expressionController.StartExpression(expressionName);
}
```

**Available Expressions** (example):
- `Normal`
- `Happy`
- `Sad`
- `Angry`
- `Surprised`
- `Shy`

#### Character Positioning
**Pattern**: Scene-based spawn points

```csharp
// Position characters based on scene configuration
public enum CharacterPosition {
    Left,
    Center,
    Right
}

void PositionCharacter(GameObject character, CharacterPosition pos) {
    Transform spawnPoint = GetSpawnPoint(pos);
    character.transform.position = spawnPoint.position;
}
```

### 3. Audio System (FMOD) Integration

#### FMOD Bank Loading
**Pattern**: Scene-based bank management

```csharp
void Start() {
    // Load required FMOD banks for scene
    FMODUnity.RuntimeManager.LoadBank("Master");
    FMODUnity.RuntimeManager.LoadBank("Music");
    FMODUnity.RuntimeManager.LoadBank("SFX");
}
```

#### Background Music Control
**Pattern**: Music state management with parameters

```csharp
// Background music with smooth transitions
private FMOD.Studio.EventInstance bgmInstance;

void PlayBackgroundMusic(string eventPath) {
    if (bgmInstance.isValid()) {
        bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }
    
    bgmInstance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
    bgmInstance.start();
}

void SetMusicIntensity(float intensity) {
    bgmInstance.setParameterByName("Intensity", intensity);
}
```

#### Sound Effects
**Pattern**: One-shot sound playback

```csharp
// Simple SFX playback
void PlaySoundEffect(string eventPath, Vector3 position = default) {
    FMODUnity.RuntimeManager.PlayOneShot(eventPath, position);
}
```

**Common SFX Events**:
- `event:/UI/ButtonClick`
- `event:/UI/TextBeep`
- `event:/UI/MenuOpen`
- `event:/UI/MenuClose`

### 4. Localization System Integration

#### Language Selection
**Pattern**: Centralized locale management

```csharp
// Change game language
public void SetLanguage(SystemLanguage language) {
    LocaleIdentifier localeId;
    
    switch (language) {
        case SystemLanguage.English:
            localeId = new LocaleIdentifier("en");
            break;
        case SystemLanguage.Vietnamese:
            localeId = new LocaleIdentifier("vi");
            break;
        case SystemLanguage.Japanese:
            localeId = new LocaleIdentifier("ja");
            break;
        default:
            localeId = new LocaleIdentifier("en");
            break;
    }
    
    LocalizationSettings.SelectedLocale = 
        LocalizationSettings.AvailableLocales.GetLocale(localeId);
}
```

#### UI Text Localization
**Pattern**: Automatic text updates

```csharp
// UI text automatically updates with language change
using UnityEngine.Localization.Components;

// In Inspector: LocalizeStringEvent component references string table key
// Text updates automatically when SelectedLocale changes
```

#### CSV Dialogue Localization
**Pattern**: Manual column selection

```csharp
// Dialogue reads appropriate CSV column
string GetDialogueText(DialogueEntry entry) {
    var currentLanguage = LocalizationSettings.SelectedLocale.Identifier.Code;
    
    return currentLanguage switch {
        "en" => entry.englishText,
        "vi" => entry.vietnameseText,
        "ja" => entry.japaneseText,
        _ => entry.englishText
    };
}
```

### 5. Scene Management Integration

#### Scene Transition Pattern
**Pattern**: Async loading with loading screen

```csharp
// Load scene with transition
public async Task LoadSceneAsync(string sceneName) {
    // Show loading screen
    await ShowLoadingScreen();
    
    // Unload Addressables from current scene
    await UnloadSceneAssets();
    
    // Load new scene
    var asyncOp = SceneManager.LoadSceneAsync(sceneName);
    while (!asyncOp.isDone) {
        UpdateLoadingProgress(asyncOp.progress);
        await Task.Yield();
    }
    
    // Preload assets for new scene
    await PreloadSceneAssets(sceneName);
    
    // Hide loading screen
    await HideLoadingScreen();
}
```

#### Asset Cleanup Between Scenes
**Pattern**: Addressables cleanup

```csharp
// Clean up scene-specific assets
private async Task UnloadSceneAssets() {
    // Release loaded characters
    foreach (var handle in loadedCharacterHandles) {
        Addressables.Release(handle);
    }
    loadedCharacterHandles.Clear();
    
    // Unload FMOD banks
    FMODUnity.RuntimeManager.UnloadBank("SceneMusic");
    
    // Force garbage collection
    await Resources.UnloadUnusedAssets();
}
```

### 6. Input System Integration

#### Input Action Pattern
**Pattern**: Input Action Asset binding

```csharp
// Input handling via Input Actions
using UnityEngine.InputSystem;

private PlayerInput playerInput;

void Awake() {
    playerInput = GetComponent<PlayerInput>();
    
    // Subscribe to input actions
    playerInput.actions["Advance"].performed += OnAdvanceDialogue;
    playerInput.actions["Cancel"].performed += OnCancel;
    playerInput.actions["Skip"].performed += OnSkipDialogue;
}

void OnAdvanceDialogue(InputAction.CallbackContext context) {
    if (dialogueManager.IsDialogueActive) {
        dialogueManager.AdvanceDialogue();
    }
}
```

#### Input Context Switching
**Pattern**: Action map switching

```csharp
// Switch input contexts
public enum InputContext {
    Dialogue,
    Menu,
    Settings,
    Paused
}

void SwitchInputContext(InputContext context) {
    playerInput.SwitchCurrentActionMap(context.ToString());
}
```

### 7. Save System Integration

#### Save Data Structure
**Pattern**: Serializable data containers

```csharp
[System.Serializable]
public class GameSaveData {
    public string currentScene;
    public int currentDialogueIndex;
    public Dictionary<string, bool> storyFlags;
    public GameSettings settings;
    public DateTime saveTime;
}
```

#### Save/Load Pattern
**Pattern**: JSON serialization with PlayerPrefs or file

```csharp
// Save game state
public void SaveGame(int slotIndex) {
    var saveData = new GameSaveData {
        currentScene = SceneManager.GetActiveScene().name,
        currentDialogueIndex = dialogueManager.CurrentIndex,
        storyFlags = storyManager.GetAllFlags(),
        settings = settingsManager.GetCurrentSettings(),
        saveTime = DateTime.Now
    };
    
    string json = JsonUtility.ToJson(saveData);
    PlayerPrefs.SetString($"SaveSlot_{slotIndex}", json);
    PlayerPrefs.Save();
}

// Load game state
public void LoadGame(int slotIndex) {
    string json = PlayerPrefs.GetString($"SaveSlot_{slotIndex}", "");
    if (!string.IsNullOrEmpty(json)) {
        var saveData = JsonUtility.FromJson<GameSaveData>(json);
        RestoreGameState(saveData);
    }
}
```

## Cross-System Event Flow Examples

### Example 1: Starting a Dialogue Sequence

```
1. Player Input: Advances dialogue
   ↓
2. Dialogue Manager: Loads next dialogue line from CSV
   ↓
3. Localization System: Gets text in current language
   ↓
4. Dialogue Manager: Parses dialogue data
   ├─→ UI System: Updates text display
   ├─→ Character System: Changes expression
   ├─→ Audio System: Plays voice line / text beep
   └─→ Background: Fades/changes if specified
```

### Example 2: Changing Game Language

```
1. Settings UI: Language dropdown changed
   ↓
2. Localization System: Updates SelectedLocale
   ↓
3. Event Broadcast: OnLanguageChanged
   ├─→ UI System: All LocalizeStringEvent components update
   ├─→ Dialogue Manager: Refreshes current dialogue text
   └─→ Settings Manager: Saves language preference
```

### Example 3: Scene Transition

```
1. Dialogue Manager: Reaches scene transition point
   ↓
2. Scene Manager: Begins transition
   ├─→ Audio Manager: Fades out current music
   ├─→ Loading Screen: Shows with progress bar
   ├─→ Addressables: Unloads current scene assets
   ├─→ Unity: Loads new scene
   ├─→ Addressables: Preloads new scene assets
   ├─→ Audio Manager: Starts new scene music
   └─→ Loading Screen: Hides, gameplay begins
```

## Data Flow Patterns

### Dialogue Data Flow

```
CSV File → CSV Parser → DialogueData Object → Components
    ↓
Dialogue_3Language.csv (English, Vietnamese, Japanese columns)
    ↓
DialogueManager.LoadDialogue(id)
    ↓
DialogueData {
    id, character, text (localized), expression, audioEvent, ...
}
    ├→ UI: Display text and character name
    ├→ Character: Set expression
    ├→ Audio: Play sound/voice
    └→ Background: Change if needed
```

### Asset Loading Flow

```
Asset Reference → Addressables → Loading → Instantiation/Use
    ↓
"Characters/Mai_Prefab" (asset address)
    ↓
Addressables.LoadAssetAsync<GameObject>("Characters/Mai_Prefab")
    ↓
AsyncOperationHandle<GameObject>
    ↓
Instantiate(handle.Result)
    ↓
Live2D Character in scene
```

## Common Integration Challenges

### Challenge: Timing dialogue text with audio
**Solution**: FMOD callbacks and coroutines
```csharp
IEnumerator PlayDialogueWithAudio(string text, string audioEvent) {
    var instance = RuntimeManager.CreateInstance(audioEvent);
    instance.start();
    
    // Display text character by character, synced to audio
    // Use FMOD timeline callbacks for precise timing
}
```

### Challenge: Character expression transitions during dialogue
**Solution**: Queue-based expression system
```csharp
// Queue expression changes to prevent jarring transitions
expressionQueue.Enqueue(new ExpressionChange {
    timestamp = dialogueTimestamp,
    expression = "Happy",
    transitionDuration = 0.3f
});
```

### Challenge: Memory management with many assets
**Solution**: Addressables with reference counting
```csharp
// Track asset handles for proper cleanup
private List<AsyncOperationHandle> activeHandles = new();

// Always release when done
void OnSceneUnload() {
    foreach (var handle in activeHandles) {
        Addressables.Release(handle);
    }
    activeHandles.Clear();
}
```

## Best Practices

1. **Use Events for Loose Coupling**: Systems communicate via events, not direct references
2. **Addressables for Assets**: Use Addressables for runtime-loaded content
3. **Async/Await for Loading**: Modern async patterns for scene/asset loading
4. **FMOD for Audio**: Leverage FMOD's event system, don't bypass it
5. **Localization Package**: Use Unity Localization for UI, custom system for CSV dialogues
6. **Input Actions**: Use Input System's Action assets, not legacy input
7. **ScriptableObjects for Config**: Configuration as data, not hard-coded values

---

**Related Documentation**:
- Technical details: `docs/ai-context/project-structure.md`
- Component contexts: See Tier 2 CONTEXT.md files in respective folders










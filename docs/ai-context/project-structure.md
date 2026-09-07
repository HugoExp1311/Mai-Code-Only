# Project Structure - Mai's Love Story

## Technology Stack

### Game Engine
- **Unity 2022+** - Core game engine
- **C# (.NET)** - Primary programming language

### Asset & Content Systems
- **Live2D Cubism SDK** - Character animation and rigging
- **Unity Addressables** - Asset management and memory optimization
- **TextMesh Pro** - Advanced text rendering
- **Unity Localization** - Multi-language system

### Audio Middleware
- **FMOD Unity Integration** - Professional audio engine
  - Event-based audio triggering
  - Dynamic music layering
  - Real-time parameter control

### Input & UI
- **Unity Input System** - Modern input handling (new input system)
- **Unity UI (uGUI)** - User interface system
- **TextMesh Pro** - Text rendering in UI

### Development Tools
- **PlayerPrefsEditor** - Runtime preference inspection
- **Custom Editor Scripts** - Development workflow automation

## Directory Structure

### Root Level
```
Mai-s-Love-Story/
├── Assets/                    # Unity project assets
├── Audio System/              # FMOD project and builds
├── ProjectSettings/           # Unity configuration
├── Packages/                  # Unity Package Manager
├── Library/                   # Unity cache (ignored)
├── Temp/                      # Unity temp files (ignored)
├── Logs/                      # Unity logs
├── UserSettings/              # User-specific settings
├── docs/                      # AI context documentation
├── .claude/                   # Claude Code configuration
├── CLAUDE.md                  # Master AI context
├── MCP-ASSISTANT-RULES.md     # Coding standards
└── README.md                  # Project readme
```

## Assets Directory Structure

### Core Game Assets

#### Scripts (`Assets/Scripts/`)
**Purpose**: All C# game logic  
**Organization**:
```
Scripts/
├── Core/                 # Core game systems (managers, controllers)
├── DialogueSystem/       # Dialogue and story management
├── Characters/           # Character controllers and logic
├── UI/                   # UI controllers and components
├── Audio/                # Audio system integration
├── Localization/         # Localization utilities
├── SaveSystem/           # Save/load functionality
├── SceneManagement/      # Scene loading and transitions
├── Input/                # Input handling
├── Settings/             # Game settings management
└── Utilities/            # Helper classes and extensions
```

**Key Files**:
- Game managers (singleton pattern)
- Data models and scriptable objects
- Event systems
- Utility scripts

#### Live2D (`Assets/Live2D/`)
**Purpose**: Live2D Cubism SDK integration  
**Contains**:
- Live2D Cubism SDK framework (186 C# scripts)
- Character model files (.model3.json)
- Motion data (.motion3.json)
- Expression data (.exp3.json)
- Texture assets
- Prefabs for Live2D characters

**Integration**:
- SDK provides `CubismModel` component
- Character prefabs combine Live2D models with game logic
- Expression control through Cubism parameters

#### Image (`Assets/Image/`)
**Purpose**: 2D visual assets  
**Contains**:
- Character sprites and portraits
- Background images
- UI elements and icons
- Button graphics
- Scene decorations

**Organization**:
- Subdirectories by category (Characters, Backgrounds, UI, etc.)
- Sprite atlases for performance optimization

#### Audio (`Assets/Audio/`)
**Purpose**: Audio source files referenced by FMOD  
**Contains**:
- Background music files (.mp3)
- Sound effect files
- Voice lines (if applicable)

**Note**: These are source files; FMOD builds them into banks in `Audio System/Build/`

#### Animations (`Assets/Animations/`)
**Purpose**: Unity animation system (non-Live2D)  
**Contains**:
- Animation clips (.anim)
- Animator controllers (.controller)
- Used for UI animations, transitions, simple effects

#### Scenes (`Assets/Scenes/`)
**Purpose**: Unity scene files  
**Organization**:
- Main menu scene
- Game scene(s)
- Loading scenes

#### Localization (`Assets/Localization/`)
**Purpose**: Multi-language support  
**Contains**:
- String tables (.asset) for English, Vietnamese, Japanese
- Localization settings
- Locale metadata

**Languages Supported**:
- English (en)
- Vietnamese (vi)
- Japanese (ja)

#### Resources (`Assets/Resources/`)
**Purpose**: Runtime-loaded assets (legacy)  
**Contains**:
- `Dialogues_3Language.csv` - Main dialogue database
- Assets that must be loaded by name at runtime
- Consider migrating to Addressables for new content

#### Plugins (`Assets/Plugins/`)
**Purpose**: Third-party plugins and native libraries  
**Contains**:
- Native plugin DLLs
- Third-party SDKs
- Platform-specific binaries

#### Editor (`Assets/Editor/`)
**Purpose**: Unity Editor extensions  
**Contains**:
- Custom inspectors
- Editor windows
- Build pipeline extensions
- Development tools (editor-only, not included in builds)

#### Settings (`Assets/Settings/`)
**Purpose**: Scriptable object configurations  
**Contains**:
- Rendering settings
- Input action assets
- Game configuration objects

#### AddressableAssetsData (`Assets/AddressableAssetsData/`)
**Purpose**: Addressables system configuration  
**Contains**:
- Asset group definitions
- Addressable settings
- Build cache data

#### TextMesh Pro (`Assets/TextMesh Pro/`)
**Purpose**: TextMesh Pro resources  
**Contains**:
- Font assets
- TMP shaders
- Default resources
- Example content

#### PlayerPrefsEditor (`Assets/PlayerPrefsEditor/`)
**Purpose**: Development tool for inspecting PlayerPrefs  
**Usage**: Editor-only, helps debug save data and settings

## Audio System Structure

```
Audio System/
├── Audio System.fspro     # FMOD Studio project file
├── Assets/                # Source audio files
│   └── *.mp3              # Audio source files
├── Build/                 # Built FMOD banks
│   ├── Desktop/
│   │   ├── Master.bank
│   │   ├── Master.strings.bank
│   │   └── *.bank         # Event banks
├── Metadata/              # FMOD project metadata
│   └── *.xml              # FMOD internal files
```

**Workflow**:
1. Edit audio events in FMOD Studio (`Audio System.fspro`)
2. Build banks to `Build/` folder
3. Unity integration loads banks at runtime
4. Trigger events from C# scripts using FMOD Unity API

## Project Settings Structure

```
ProjectSettings/
├── ProjectSettings.asset         # Core Unity settings
├── ProjectVersion.txt            # Unity version
├── EditorBuildSettings.asset     # Build configuration
├── AudioManager.asset            # Unity audio settings (FMOD overrides)
├── GraphicsSettings.asset        # Rendering configuration
├── InputManager.asset            # Input system configuration
├── Physics2DSettings.asset       # 2D physics settings
├── QualitySettings.asset         # Quality presets
├── TagManager.asset              # Tags and layers
└── ...                           # Other Unity settings
```

## Package Dependencies

From `Packages/manifest.json`:

**Core Packages**:
- `com.unity.addressables` - Asset management
- `com.unity.localization` - Multi-language support
- `com.unity.inputsystem` - New input system
- `com.unity.textmeshpro` - Advanced text rendering

**Graphics & Rendering**:
- `com.unity.render-pipelines.universal` - URP rendering

**Development Tools**:
- `com.unity.collab-proxy` - Version control integration
- Various Unity editor tools and analyzers

## Build Artifacts (Excluded from Version Control)

### Library/
- Unity's build cache
- Imported asset data
- Script assemblies
- Shader cache

### Temp/
- Temporary build files
- Intermediate compilation output

### Logs/
- Unity Editor logs
- Build logs
- Error logs

### obj/ and bin/
- .NET compilation output

## File Types Overview

### Unity-Specific
- `.unity` - Scene files
- `.prefab` - Prefab assets
- `.asset` - ScriptableObjects and Unity assets
- `.controller` - Animator controllers
- `.anim` - Animation clips
- `.mat` - Materials
- `.meta` - Unity metadata (tracks GUID, import settings)

### Code
- `.cs` - C# scripts
- `.asmdef` - Assembly definitions

### Audio (FMOD)
- `.fspro` - FMOD Studio project
- `.bank` - FMOD audio banks

### Data
- `.csv` - Dialogue and data tables
- `.json` - Configuration and data files

### Assets
- `.png` - Images and sprites
- `.mp3` - Audio files
- `.shader` - Custom shaders

## Key Architectural Patterns

### Singleton Managers
Many core systems use singleton pattern:
- Game Manager
- Dialogue Manager
- Audio Manager
- Scene Manager
- Localization Manager

### ScriptableObject Configuration
Game configuration stored as ScriptableObject assets:
- Character data
- Dialogue settings
- Audio event references
- Game settings

### Event-Driven Architecture
Systems communicate via:
- Unity Events
- C# events/delegates
- Custom event systems

### Addressables Asset Loading
Runtime asset loading:
- Character sprites
- Backgrounds
- Audio banks (via FMOD)
- Localization tables

## Development Workflow

### Daily Development
1. Open Unity project
2. Scripts edited in IDE (Visual Studio/Rider)
3. Unity auto-recompiles on save
4. Test in Unity Editor play mode

### Audio Workflow
1. Edit in FMOD Studio
2. Build banks
3. Test in Unity Editor

### Localization Workflow
1. Update string tables in Unity
2. Or edit CSVs for dialogue
3. Test with language switcher

### Build Workflow
1. Configure build settings
2. Build Addressables
3. Build FMOD banks
4. Build Unity player

## Version Control Considerations

### Included in Git
- `Assets/` (except Library)
- `ProjectSettings/`
- `Packages/manifest.json` and `packages-lock.json`
- `Audio System/` (FMOD project)
- `docs/` and documentation files

### Excluded from Git (.gitignore)
- `Library/`
- `Temp/`
- `Logs/`
- `obj/` and `bin/`
- `UserSettings/`
- Build output folders

### Unity .meta Files
- Critical for version control
- Track asset GUIDs and references
- Must be committed with assets

## Integration Points

### Live2D ↔ Game Logic
- Live2D prefabs have game scripts attached
- Expression changes triggered via scripts
- Motion playback controlled from dialogue system

### FMOD ↔ Unity
- FMOD Unity integration plugin
- Banks loaded at startup or on-demand
- Events triggered via `FMODUnity.RuntimeManager`

### Addressables ↔ Asset Loading
- Sprites and assets loaded by address
- Memory managed automatically
- Async loading patterns

### Localization ↔ UI
- UI text components use Localization package
- Dialogue system reads localized CSV
- Language switching updates all text

---

**For integration patterns, see**: `docs/ai-context/system-integration.md`  
**For deployment details, see**: `docs/ai-context/deployment-infrastructure.md`










# Session Handoff & Current State

> **Last Updated**: 2025-10-29  
> **Updated By**: Initial Claude Code Development Kit Setup

## Current Project State

### Active Development Phase
**Phase**: Initial Claude Code Development Kit Setup  
**Status**: Framework installation in progress

### Recent Changes
- ✅ Created Claude Code Development Kit directory structure
- ✅ Set up 3-tier documentation system
- ✅ Created foundation documentation (Tier 1)
- 🚧 In Progress: Command files and hooks setup

### What's Working
- Unity project structure is stable
- Live2D character system implemented
- FMOD audio integration functional
- Multi-language dialogue system operational
- Basic scene navigation working

### Known Issues
None currently tracked in this handoff document.

**For detailed issues**: See `docs/open-issues/` folder

### In Progress
- Setting up Claude Code Development Kit framework
- Creating AI context documentation structure

## Quick Context for Next Session

### If Continuing This Setup
**Next Steps**:
1. Complete command file creation
2. Set up hook scripts
3. Create component-level CONTEXT.md files
4. Test the framework with a sample task

### If Starting New Work
**Load Context Based on Task**:
- Dialogue work → Load `Assets/Scripts/DialogueSystem/CONTEXT.md`
- Character work → Load `Assets/Live2D/CONTEXT.md`
- Audio work → Load `Audio System/CONTEXT.md`
- UI work → Load `Assets/Scripts/UI/CONTEXT.md`

**Always Available**: This file + all Tier 1 docs in `docs/ai-context/`

## Current Technical Context

### Dependencies Status
All dependencies properly configured:
- ✅ Unity Addressables
- ✅ Unity Localization
- ✅ FMOD Unity Integration
- ✅ Live2D Cubism SDK
- ✅ TextMesh Pro
- ✅ Unity Input System

### Build Status
- **Last Build**: [Not tracked yet]
- **Build Target**: Windows x64
- **Addressables**: [Needs building]
- **FMOD Banks**: Present in `Audio System/Build/Desktop/`

### Branch Information
**Current Branch**: [Check with git]
**Recent Commits**: [Check git log]

## Important Patterns to Remember

### Asset Loading
Always use Addressables for runtime asset loading:
```csharp
var handle = Addressables.LoadAssetAsync<T>(address);
await handle.Task;
// Use asset
// Release when done: Addressables.Release(handle);
```

### Audio Events
Always use FMOD events, never direct Unity audio:
```csharp
FMODUnity.RuntimeManager.PlayOneShot("event:/path/to/event");
```

### Localization
- UI text: Use Unity Localization components
- Dialogue: Use CSV column selection based on current locale

### Character Expressions
Live2D expressions are controlled via CubismExpressionController:
```csharp
expressionController.StartExpression("ExpressionName");
```

## Configuration Files

### Key Config Locations
- Unity Settings: `ProjectSettings/`
- FMOD Settings: `Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset`
- Addressables: `Assets/AddressableAssetsData/`
- Input Actions: `Assets/InputSystem_Actions.inputactions`

### Player Preferences
Stored via PlayerPrefs:
- Language selection
- Audio volumes
- Game settings
- Save data

## Development Environment

### Required Tools
- Unity Editor (2022+)
- IDE: Visual Studio or JetBrains Rider
- FMOD Studio (for audio editing)
- Git (version control)

### Optional Tools
- PlayerPrefsEditor (included in project for debugging)
- Unity Localization package editor tools

## Session Continuity Notes

### Context Preservation
This file is automatically updated via `/update-docs` command or manually when significant changes occur.

### What to Update in This File
- **After major features**: Update "Recent Changes" section
- **After fixing issues**: Remove from "Known Issues" or mark resolved
- **After setup changes**: Update "Dependencies Status" or "Configuration Files"
- **Before ending session**: Note "In Progress" items

### What NOT to Include Here
- Detailed technical implementation (goes in component CONTEXT.md files)
- Architectural decisions (goes in `docs/ai-context/system-integration.md`)
- Code snippets (unless crucial for immediate context)

## Quick Reference Links

### Documentation
- **Master Context**: `/CLAUDE.md`
- **Project Structure**: `/docs/ai-context/project-structure.md`
- **System Integration**: `/docs/ai-context/system-integration.md`
- **Documentation Guide**: `/docs/README.md`

### Code Locations
- **Scripts**: `/Assets/Scripts/`
- **Editor Tools**: `/Assets/Editor/`
- **Live2D**: `/Assets/Live2D/`
- **Audio**: `/Audio System/`
- **Dialogues**: `/Assets/Resources/Dialogues_3Language.csv`

### Common Commands
```bash
# Build Addressables (in Unity)
Window → Asset Management → Addressables → Groups → Build → New Build

# Build FMOD Banks (in Unity)
FMOD → Build All Banks

# Build Player (in Unity)
File → Build Settings → Build
```

## Next Session Checklist

When starting your next session:
- [ ] Read this handoff document
- [ ] Check "In Progress" items above
- [ ] Review "Recent Changes" for context
- [ ] Load relevant Tier 2/3 CONTEXT.md based on your task
- [ ] Check for any new issues in `docs/open-issues/`

---

**Remember**: Update this file at the end of each significant work session to ensure continuity.

**Use command**: `/update-docs "docs/ai-context/handoff.md"` (once commands are set up)










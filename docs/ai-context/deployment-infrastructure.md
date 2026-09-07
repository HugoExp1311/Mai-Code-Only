# Deployment & Infrastructure

## Build Configuration

### Target Platforms

**Primary Platform**: Windows (Standalone)
- **Architecture**: x64
- **Graphics API**: DirectX 11/12
- **Build Target**: Windows Standalone

**Potential Future Platforms**:
- Android (requires optimization for mobile)
- iOS (requires Live2D and FMOD mobile licenses)

### Unity Build Settings

Located in: `ProjectSettings/EditorBuildSettings.asset`

**Scenes in Build**:
1. Main Menu Scene
2. Game Scene(s)
3. Loading Scene(s)

**Build Options**:
- Development Build: Optional (for testing)
- Script Debugging: Optional (development only)
- Compression: LZ4 (faster) or LZMA (smaller)

### Player Settings

Located in: `ProjectSettings/ProjectSettings.asset`

**Key Settings**:
- **Company Name**: [Your Company]
- **Product Name**: Mai's Love Story
- **Version**: Semantic versioning (e.g., 1.0.0)
- **Icon**: Application icon
- **Splash Screen**: Unity splash or custom

**Resolution & Quality**:
- **Default Screen Width**: 1920
- **Default Screen Height**: 1080
- **Fullscreen Mode**: Fullscreen Window (recommended) or Windowed
- **Resizable Window**: Yes
- **Default Quality Level**: Medium/High (from QualitySettings.asset)

**Scripting**:
- **Scripting Backend**: IL2CPP (recommended for performance) or Mono
- **API Compatibility Level**: .NET Standard 2.1
- **Managed Stripping Level**: Medium (balance size/compatibility)

## Build Process

### Pre-Build Steps

1. **Build Addressables**
   ```
   Window → Asset Management → Addressables → Groups
   Build → New Build → Default Build Script
   ```
   - Generates asset bundles in `ServerData/` or `Library/com.unity.addressables/`
   - Required for runtime asset loading

2. **Build FMOD Banks**
   ```
   FMOD → Build All Banks
   ```
   - Or build in FMOD Studio and refresh Unity
   - Banks output to `Audio System/Build/Desktop/`
   - Must be done before Unity build

3. **Clean Build (Optional)**
   ```
   Build → Clean All
   ```
   - Removes previous build cache
   - Recommended for release builds

4. **Version Increment**
   - Update version number in PlayerSettings
   - Update CHANGELOG or release notes

### Build Command

**Via Unity Editor**:
```
File → Build Settings → Build (or Build and Run)
```

**Via Command Line** (for CI/CD):
```bash
"C:\Program Files\Unity\Hub\Editor\[VERSION]\Editor\Unity.exe" \
  -quit \
  -batchmode \
  -projectPath "D:\Unity Project\Mai-s-Love-Story" \
  -buildWindows64Player "Build/MaisLoveStory.exe" \
  -logFile "Build/build.log"
```

### Post-Build Steps

1. **Copy FMOD Banks**
   - Ensure `*.bank` files from `Audio System/Build/Desktop/` are in build folder
   - Unity FMOD integration usually handles this automatically

2. **Include Addressables Data**
   - Addressables bundles should be in build folder
   - Check `[BuildFolder]_Data/StreamingAssets/aa/`

3. **Test Build**
   - Run executable
   - Test all scenes
   - Verify audio playback
   - Test language switching
   - Check asset loading

4. **Package for Distribution**
   - Compress build folder (ZIP or installer)
   - Include README, LICENSE if applicable

## Addressables Configuration

### Asset Group Strategy

**Default Groups**:
- **Default Local Group**: Core assets loaded at startup
- **Characters**: Live2D character prefabs and textures
- **Backgrounds**: Background images
- **UI**: UI sprites and assets
- **Audio**: Additional audio banks (if using Addressable audio)

**Build Path**: 
- Local: `[Build]/[BuildTarget]` (embedded in build)
- Remote: Can be configured for future updates

**Load Path**:
- Local: `{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]`

### Asset Addressing

**Naming Convention**:
```
Characters/Mai_Prefab
Characters/Mai_Portrait
Backgrounds/School_Classroom
Backgrounds/City_Park
UI/Button_MainMenu
```

**Loading Pattern**:
```csharp
var handle = Addressables.LoadAssetAsync<GameObject>("Characters/Mai_Prefab");
await handle.Task;
GameObject character = Instantiate(handle.Result);
```

## FMOD Configuration

### Bank Structure

**Banks**:
1. **Master.bank** - Core FMOD system bank (always loaded)
2. **Master.strings.bank** - Event name strings (required)
3. **Music.bank** - Background music events
4. **SFX.bank** - Sound effects
5. **Voice.bank** - Character voice lines (if applicable)

**Bank Loading**:
- Master banks: Loaded at startup
- Other banks: Loaded per scene or on-demand

**Build Settings** (in FMOD Studio):
- **Platform**: Desktop
- **Format**: Vorbis for music, PCM for short SFX
- **Quality**: ~128kbps for music (balance quality/size)

### Integration Settings

Located in: `Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset`

**Key Settings**:
- **Bank Source Type**: Multiple Banks
- **Banks Path**: `Audio System/Build/Desktop`
- **Bank Load**: Explicit loading (recommended for control)
- **Live Update**: Enabled for development, disabled for release

## Localization Build

### String Tables

**Location**: `Assets/Localization/`

**Supported Locales**:
- English (en)
- Vietnamese (vi)
- Japanese (ja)

**CSV Dialogue**:
- `Assets/Resources/Dialogues_3Language.csv`
- Must be in Resources or Addressables for runtime access

**Font Assets**:
- Ensure TextMesh Pro fonts support all character sets:
  - Latin (English)
  - Vietnamese diacritics
  - Japanese (Hiragana, Katakana, Kanji)

## Distribution Structure

### Windows Build Folder Structure

```
MaisLoveStory/
├── MaisLoveStory.exe              # Game executable
├── MaisLoveStory_Data/            # Unity data folder
│   ├── Managed/                   # .NET assemblies
│   ├── Plugins/                   # Native plugins (FMOD, etc.)
│   ├── Resources/                 # Embedded resources
│   ├── StreamingAssets/           # StreamingAssets folder
│   │   ├── aa/                    # Addressables bundles
│   │   └── [FMOD_banks]/          # FMOD .bank files
│   ├── level0                     # Scene data
│   ├── sharedassets0.assets       # Shared assets
│   └── ...                        # Other Unity data
├── UnityPlayer.dll                # Unity runtime
├── UnityCrashHandler64.exe        # Crash handler
└── MonoBleedingEdge/              # Mono runtime (if using Mono backend)
```

### File Size Considerations

**Typical Build Size** (estimated):
- Executable + Unity Runtime: ~50-100 MB
- Assets (images, audio): 200-500 MB (depends on content)
- Total: ~300-600 MB

**Optimization Strategies**:
- Compress textures (use compression in import settings)
- Use Addressables for on-demand loading
- Optimize audio formats in FMOD
- Enable build compression (LZMA for smallest, LZ4 for faster loading)

## Testing & Quality Assurance

### Pre-Release Checklist

**Functionality**:
- [ ] All scenes load correctly
- [ ] Dialogue system works (all languages)
- [ ] Live2D characters animate properly
- [ ] Audio playback works (music, SFX, voice)
- [ ] Language switching works
- [ ] Save/Load functionality works
- [ ] Settings are saved and loaded correctly

**Performance**:
- [ ] Stable 60 FPS on target hardware
- [ ] No memory leaks (check in long play sessions)
- [ ] Addressables load/unload properly
- [ ] FMOD banks load without errors

**Compatibility**:
- [ ] Test on Windows 10 and 11
- [ ] Test on different screen resolutions
- [ ] Test on different graphics settings

**Build Quality**:
- [ ] No console errors or warnings
- [ ] No missing assets
- [ ] All Addressables bundles included
- [ ] All FMOD banks included

### Automated Build (CI/CD)

**Potential CI/CD Setup** (if using Git):
- GitHub Actions / GitLab CI
- Unity Cloud Build
- Jenkins with Unity build script

**Build Pipeline Steps**:
1. Pull latest code from repository
2. Run unit tests (if any)
3. Build Addressables
4. Build FMOD banks (requires FMOD CLI)
5. Build Unity player
6. Run automated tests on build
7. Package and upload build artifact

## Runtime Environment

### System Requirements

**Minimum Requirements**:
- **OS**: Windows 10 64-bit
- **Processor**: Dual-core 2.0 GHz
- **Memory**: 4 GB RAM
- **Graphics**: DirectX 11 compatible GPU
- **Storage**: 1 GB available space

**Recommended Requirements**:
- **OS**: Windows 10/11 64-bit
- **Processor**: Quad-core 2.5 GHz+
- **Memory**: 8 GB RAM
- **Graphics**: DirectX 12 compatible GPU with 2 GB VRAM
- **Storage**: 2 GB available space

### Runtime Dependencies

**Included in Build**:
- Unity Runtime (UnityPlayer.dll)
- .NET Runtime (Mono or IL2CPP runtime)
- FMOD Native Library
- Live2D Native Library

**User System Requirements**:
- DirectX 11 or later
- Visual C++ Redistributable (usually auto-installed with Unity)

## Deployment Workflow

### Development Build
1. Build Addressables (if changed)
2. Build FMOD banks (if changed)
3. Unity: File → Build Settings → Build
4. Test locally

### Release Build
1. Increment version number
2. Clean build cache
3. Build Addressables (full rebuild)
4. Build FMOD banks (full rebuild)
5. Unity: Build with "Release" configuration
6. Extensive QA testing
7. Package for distribution
8. Upload to distribution platform (Steam, itch.io, etc.)

## Future Considerations

### Potential Optimizations
- Asset streaming for larger games
- Remote Addressables server for updates
- FMOD audio compression optimization
- Texture atlas optimization

### Platform Expansion
- **Mobile (Android/iOS)**: 
  - Requires UI scaling adjustments
  - Touch input integration
  - Mobile-optimized assets
  - Live2D mobile runtime license
  
- **Console**: 
  - Platform-specific SDKs
  - Controller input
  - Platform certification requirements

### Live Updates
- Configure Addressables for remote content
- Set up CDN for asset hosting
- Implement patch/update system

---

**For integration details, see**: `docs/ai-context/system-integration.md`
**For project structure, see**: `docs/ai-context/project-structure.md`










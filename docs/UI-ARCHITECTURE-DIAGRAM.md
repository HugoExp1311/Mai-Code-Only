# UI System Architecture Diagrams

## High-Level Architecture

```
╔════════════════════════════════════════════════════════════════╗
║                     USER INTERFACE (Unity)                      ║
║  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐      ║
║  │  Panel   │  │  Panel   │  │  Panel   │  │  Panel   │      ║
║  │GameObject│  │GameObject│  │GameObject│  │GameObject│      ║
║  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘      ║
╚═══════╪══════════════╪══════════════╪══════════════╪═══════════╝
        │              │              │              │
        ▼              ▼              ▼              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   PRESENTATION LAYER                             │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐                │
│  │ PanelView  │  │ PanelView  │  │ PanelView  │                │
│  │  (40 ln)   │  │  (40 ln)   │  │  (40 ln)   │                │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘                │
│        │                │                │                       │
│  ┌─────▼──────────────────────────────────┐                     │
│  │       UI Components (Composable)       │                     │
│  │  • CanvasSetup      (40 lines)        │                     │
│  │  • BackgroundClick  (50 lines)        │                     │
│  │  • Animation        (60 lines)        │                     │
│  │  • ButtonContainer  (45 lines)        │                     │
│  └────────────────────────────────────────┘                     │
└────────────────────────┬────────────────────────────────────────┘
                         │ Events & Calls
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                   APPLICATION LAYER                              │
│  ┌────────────────────────────────────────────────┐             │
│  │         PanelPresenter (80 lines)              │             │
│  │  • Coordinates View & Services                 │             │
│  │  • Handles View Events                         │             │
│  │  • Updates Button States                       │             │
│  └─────────────────┬──────────────────────────────┘             │
│                    │                                             │
│  ┌─────────────────▼──────────────────────────────┐             │
│  │        UICoordinator (80 lines)                │             │
│  │  • Composition Root                            │             │
│  │  • Dependency Injection Setup                  │             │
│  │  • Service Registration                        │             │
│  └────────────────────────────────────────────────┘             │
└────────────────────────┬────────────────────────────────────────┘
                         │ Service Calls
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DOMAIN LAYER                                │
│  ┌──────────────────────────────────────────────────┐           │
│  │      PanelService (100 lines)                    │           │
│  │  • Panel Registration                            │           │
│  │  • Show/Hide Logic                               │           │
│  │  • Stack Management                              │           │
│  └──────────────┬───────────────────────────────────┘           │
│                 │ Uses                                           │
│  ┌──────────────▼──────────────────┐                            │
│  │  ButtonStatePolicy (40 lines)   │                            │
│  │  • Button Interactability Rules │                            │
│  └──────────────┬──────────────────┘                            │
│                 │                                                │
│  ┌──────────────▼──────────────────┐                            │
│  │  PanelStackPolicy (60 lines)    │                            │
│  │  • Stack Behavior Rules         │                            │
│  └─────────────────────────────────┘                            │
└────────────────────────┬────────────────────────────────────────┘
                         │ Infrastructure Calls
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                  INFRASTRUCTURE LAYER                            │
│  ┌────────────────────┐  ┌────────────────────┐                │
│  │ AnimationService   │  │  AudioService      │                │
│  │   (50 lines)       │  │   (40 lines)       │                │
│  └────────────────────┘  └────────────────────┘                │
│  ┌────────────────────┐  ┌────────────────────┐                │
│  │  ServiceLocator    │  │   InputService     │                │
│  │   (30 lines)       │  │   (optional)       │                │
│  └────────────────────┘  └────────────────────┘                │
└─────────────────────────────────────────────────────────────────┘
```

## Component Composition Example

### How a Panel is Built

```
┌─────────────────────────────────────────────────────────┐
│                    Panel GameObject                      │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │           PanelView Component                   │    │
│  │  • Config (ScriptableObject reference)         │    │
│  │  • Presenter (injected at runtime)             │    │
│  │  • Show/Hide methods                           │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │        CanvasSetupComponent                     │    │
│  │  • Creates Canvas if missing                   │    │
│  │  • Sets sorting order from config              │    │
│  │  • Creates CanvasGroup                         │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │      PanelAnimationComponent                    │    │
│  │  • Subscribes to Show/Hide events              │    │
│  │  • Triggers fade in/out                        │    │
│  │  • Uses AnimationService                       │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │     BackgroundClickComponent (optional)         │    │
│  │  • Creates invisible click detector            │    │
│  │  • Fires OnBackgroundClicked event             │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │       ButtonContainerComponent                  │    │
│  │  • Auto-discovers child buttons                │    │
│  │  • Provides button collection to presenter     │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**Benefit**: Each component is **independent and reusable**. You can mix and match as needed.

## Data Flow Diagram

### Opening a Panel

```
[User Clicks Button]
        │
        ▼
[ButtonView.OnClick]
        │
        ▼
[Execute UICommand]
        │
        ▼
[ShowPanelCommand.Execute]
        │
        ▼
┌───────────────────────────────────────────────┐
│         PanelService.ShowPanel()              │
│  1. Get panel from registry                   │
│  2. Apply stack policy                        │
│  3. Call panel.Show()                         │
└───────────────┬───────────────────────────────┘
                │
                ▼
┌───────────────────────────────────────────────┐
│           PanelView.Show()                    │
│  1. Notify presenter                          │
│  2. Set gameObject active                     │
│  3. Fire OnShown event                        │
└───────────────┬───────────────────────────────┘
                │
                ▼
┌───────────────────────────────────────────────┐
│        PanelPresenter.OnShow()                │
│  1. Register with panel service               │
│  2. Update button states                      │
│  3. Play audio                                │
└───────────────┬───────────────────────────────┘
                │
                ├──────────────────────┬─────────────────────┐
                ▼                      ▼                     ▼
      [Background Click        [Animation           [Buttons Updated]
       Detector Enabled]        Component            
                                 Fades In]
```

### Closing a Panel

```
[User Clicks Back or Background]
        │
        ▼
[UIPanelManager.HideCurrentPanel]
   OR [BackgroundClickComponent.OnBackgroundClicked]
        │
        ▼
[PanelService.HideCurrentPanel]
        │
        ▼
┌───────────────────────────────────────────────┐
│         PanelService.HidePanel()              │
│  1. Apply stack policy                        │
│  2. Remove from stack                         │
│  3. Call panel.Hide()                         │
└───────────────┬───────────────────────────────┘
                │
                ▼
┌───────────────────────────────────────────────┐
│           PanelView.Hide()                    │
│  1. Notify presenter                          │
│  2. Fire OnHidden event                       │
│  3. Set gameObject inactive                   │
└───────────────┬───────────────────────────────┘
                │
                ▼
┌───────────────────────────────────────────────┐
│        PanelPresenter.OnHide()                │
│  1. Notify panel service                      │
│  2. Play audio                                │
└───────────────┬───────────────────────────────┘
                │
                ├──────────────────────┬──────────────────────┐
                ▼                      ▼                      ▼
      [Background Click        [Animation           [Update Other
       Detector Disabled]       Component             Panel Buttons]
                                Fades Out]
```

## Dependency Flow

```
┌─────────────────────────────────────────────────────────────┐
│                  HIGH-LEVEL MODULES                          │
│  (PanelView, PanelPresenter, UICoordinator)                 │
│                                                              │
│  Depend on ▼                                                │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      ABSTRACTIONS                            │
│  (IPanelService, IButtonStatePolicy, IAnimationService)     │
│                                                              │
│  ◄ Implemented by                                           │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  LOW-LEVEL MODULES                           │
│  (PanelService, ModalBasedButtonPolicy, AnimationService)   │
└─────────────────────────────────────────────────────────────┘
```

**Key Principle**: High-level modules don't depend on low-level modules. Both depend on abstractions (interfaces).

## Service Locator Pattern

```
┌─────────────────────────────────────────────┐
│          UICoordinator (Startup)            │
│                                             │
│  1. Create Services                         │
│     ├─ PanelService                        │
│     ├─ ButtonStatePolicy                   │
│     ├─ AnimationService                    │
│     └─ AudioService                        │
│                                             │
│  2. Register Services                       │
│     ServiceLocator.Register(service)        │
│                                             │
└─────────────────┬───────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────┐
│           ServiceLocator                    │
│  Dictionary<Type, object>                   │
│                                             │
│  + Register<T>(T service)                   │
│  + Get<T>() : T                            │
└─────────────────┬───────────────────────────┘
                  │
                  │ Components request services
                  │
      ┌───────────┼───────────┐
      ▼           ▼           ▼
[PanelPresenter] [Animation] [Audio]
      │           Component   Component
      │                │           │
      └────────────────┴───────────┘
              Uses services
```

## Strategy Pattern (Panel Show Behavior)

```
┌────────────────────────────────────────────────┐
│         IPanelShowStrategy                     │
│  + Show(panel, context)                       │
└────────────────┬───────────────────────────────┘
                 │
                 │ Implemented by
                 │
    ┌────────────┼────────────┬─────────────┐
    ▼            ▼            ▼             ▼
┌────────┐  ┌────────┐  ┌────────┐  ┌──────────┐
│ Base   │  │ Popup  │  │ Modal  │  │ Overlay  │
│Strategy│  │Strategy│  │Strategy│  │ Strategy │
└────────┘  └────────┘  └────────┘  └──────────┘
    │            │            │             │
    ▼            ▼            ▼             ▼
  Hide        Disable      Disable      Don't
  Other       Underlying   Everything   Affect
  Base        Panels       Except This  Others
  Panels
```

**Usage:**
```csharp
var strategy = GetStrategy(panel.Config.Type);
strategy.Show(panel, context);
```

## Command Pattern (Button Actions)

```
┌────────────────────────────────────────────────┐
│              IUICommand                        │
│  + Execute()                                   │
└────────────────┬───────────────────────────────┘
                 │
                 │ Implemented by
                 │
    ┌────────────┼────────────┬─────────────────┐
    ▼            ▼            ▼                 ▼
┌────────────┐ ┌────────────┐ ┌──────────────┐ ┌──────────┐
│ Show       │ │ Hide       │ │ Composite    │ │ Custom   │
│ Panel      │ │ Current    │ │ Command      │ │ Command  │
│ Command    │ │ Panel      │ │              │ │          │
└────────────┘ └────────────┘ └──────────────┘ └──────────┘
      │              │               │               │
      ▼              ▼               ▼               ▼
  service.       service.        Execute         Your
  ShowPanel()    HidePanel()     Multiple        Custom
                                 Commands        Logic
```

**Usage:**
```csharp
buttonView.SetCommand(new ShowPanelCommand(panelService, "ProfilePanel"));
```

## Old vs New Architecture Comparison

### Old Architecture (Monolithic)

```
┌─────────────────────────────────────────────────────────┐
│              UIManagerFramework                         │
│                  (1843 lines)                           │
│                                                         │
│  • Panel references (20 fields)                        │
│  • UI element references (30 fields)                   │
│  • Button lists (5 lists)                              │
│  • Game state (10 flags)                               │
│  • Event subscriptions                                 │
│  • Dialogue system                                     │
│  • Button click handlers (50+ methods)                 │
│  • UI update methods (20+ methods)                     │
│  • Panel show/hide logic                               │
│  • Button state management                             │
│  • Time/player UI updates                              │
│  • Mai visibility logic                                │
│  • Live2D management                                   │
│  • Everything else...                                  │
│                                                         │
│  ⚠️ Problems:                                           │
│  • Too many responsibilities                           │
│  • Hard to understand                                  │
│  • Hard to test                                        │
│  • High coupling                                       │
│  • Low cohesion                                        │
└─────────────────────────────────────────────────────────┘
              │
              │ Directly controls
              ▼
┌─────────────────────────────────────────────────────────┐
│                  UIPanel                                │
│                 (610 lines)                             │
│                                                         │
│  • Button discovery                                    │
│  • Canvas management                                   │
│  • Animation                                           │
│  • Background click detection                          │
│  • Sub-panel management                                │
│  • Show/hide logic                                     │
│  • Everything else...                                  │
│                                                         │
│  ⚠️ Problems:                                           │
│  • Too many responsibilities                           │
│  • Hard to extend                                      │
│  • Hard to test                                        │
└─────────────────────────────────────────────────────────┘
```

### New Architecture (Clean & Modular)

```
┌──────────────────────────────────────────────────────────┐
│                  UICoordinator                           │
│                   (80 lines)                             │
│  • Initializes services                                 │
│  • Registers services                                   │
│  • Initializes panels                                   │
│  ✅ Single responsibility: Composition                   │
└─────────────┬────────────────────────────────────────────┘
              │
              │ Creates and wires
              │
      ┌───────┴─────────────┬──────────────┐
      ▼                     ▼              ▼
┌──────────────┐    ┌──────────────┐  ┌──────────────┐
│ PanelService │    │ Button       │  │ Animation    │
│ (100 lines)  │    │ StatePolicy  │  │ Service      │
│              │    │ (40 lines)   │  │ (50 lines)   │
└──────┬───────┘    └──────┬───────┘  └──────┬───────┘
       │                   │                  │
       └───────────────────┴──────────────────┘
                           │
                           │ Used by
                           ▼
               ┌──────────────────────┐
               │  PanelPresenter      │
               │   (80 lines)         │
               └──────────┬───────────┘
                          │
                          │ Controls
                          ▼
               ┌──────────────────────┐
               │    PanelView         │
               │    (40 lines)        │
               └──────────┬───────────┘
                          │
                          │ Composed of
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Canvas       │  │ Animation    │  │ Background   │
│ Setup        │  │ Component    │  │ Click        │
│ (40 lines)   │  │ (60 lines)   │  │ (50 lines)   │
└──────────────┘  └──────────────┘  └──────────────┘

✅ Benefits:
• Clear separation of concerns
• Easy to understand (small files)
• Easy to test (pure C# services)
• Easy to extend (add new components)
• Low coupling, high cohesion
```

## File Organization

```
Assets/Scripts/UI/
│
├── Core/ (Domain Layer - Business Logic)
│   ├── Services/
│   │   ├── IPanelService.cs (interface)
│   │   └── PanelService.cs (100 lines)
│   ├── Policies/
│   │   ├── IButtonStatePolicy.cs
│   │   ├── ModalBasedButtonPolicy.cs (40 lines)
│   │   ├── IPanelStackPolicy.cs
│   │   └── DefaultPanelStackPolicy.cs (60 lines)
│   └── Models/
│       └── PanelConfig.cs (ScriptableObject)
│
├── Infrastructure/ (Services & Utilities)
│   ├── Animation/
│   │   ├── IAnimationService.cs
│   │   └── AnimationService.cs (50 lines)
│   ├── Audio/
│   │   ├── IAudioService.cs
│   │   └── AudioService.cs (40 lines)
│   └── ServiceLocator.cs (30 lines)
│
├── Presentation/ (View Layer - MonoBehaviours)
│   ├── Views/
│   │   ├── UIView.cs (30 lines - base)
│   │   ├── PanelView.cs (40 lines)
│   │   └── ButtonView.cs (35 lines)
│   ├── Components/ (Composable UI features)
│   │   ├── IUIComponent.cs (interface)
│   │   ├── CanvasSetupComponent.cs (40 lines)
│   │   ├── BackgroundClickComponent.cs (50 lines)
│   │   ├── PanelAnimationComponent.cs (60 lines)
│   │   └── ButtonContainerComponent.cs (45 lines)
│   └── Presenters/
│       ├── IPanelPresenter.cs
│       └── PanelPresenter.cs (80 lines)
│
├── Application/ (Application Logic)
│   ├── UICoordinator.cs (80 lines)
│   ├── DialogueUIAdapter.cs (200 lines)
│   ├── GameUIManager.cs (150 lines)
│   └── UIButtonHandlers.cs (150 lines)
│
└── Examples/
    └── REFACTORED_EXAMPLES.cs (Complete working example)
```

## Summary

This architecture provides:

✅ **Clear Layers**: Presentation → Application → Domain → Infrastructure  
✅ **Small Files**: 30-100 lines each (vs 610-1843 lines)  
✅ **Single Responsibility**: Each class does one thing  
✅ **Testable**: 80%+ unit test coverage possible  
✅ **Extensible**: Add features without modifying existing code  
✅ **Maintainable**: Easy to understand and modify  
✅ **Professional**: Industry-standard patterns  

**Next Steps**: Read [UI-REFACTOR-README.md](./UI-REFACTOR-README.md) to get started!


# UI Button Actions System - Guide

## Overview

The `UIPanelButtonAction` component provides a **code-free, Inspector-based** way to wire up buttons to control panels **with integrated sound support**. No coding required - just add the component and configure in Inspector!

**Features:**
- Panel control (Show/Hide/Toggle/Chain)
- **Built-in FMOD sound support**
- No separate ButtonSound component needed
- All-in-one button functionality

## Quick Start

### Basic Usage

1. **Select your Button GameObject**
2. **Add Component** → `UIPanelButtonAction`
3. **Configure in Inspector:**
   - Action Type: Choose Show/Hide/Toggle/Chain
   - Set Panel IDs as needed
   - Sound Type: Choose sound to play (Normal/Back/Action/Place/None)
4. **Done!** Button now controls panels and plays sounds automatically

## Action Types

### 1. Show Action

**Purpose:** Show a panel when button is clicked.

**Configuration:**
- Action Type: `Show`
- Show Panel ID: `"BasePanel"` (or your panel ID)
- Pass Data: (optional) Enable if you want to pass data
- Show Delay: (optional) Delay in seconds before showing

**Example:**
```
Button "Start Game" → Show Panel: "MainMenu"
```

### 2. Hide Action

**Purpose:** Hide a panel when button is clicked.

**Configuration:**
- Action Type: `Hide`
- Hide Panel ID: `"BasePanel"` (specific panel)
  - **Leave empty to auto-detect parent panel** - Traverses up hierarchy to find parent UIPanel2
- **OR** Hide Current Panel: ✓ (hides top panel in stack)

**Example:**
```
Button "Close" → Hide Panel: "SettingsPanel" (explicit)
Button "Close" → Hide Panel: "" (empty - auto-detects parent)
Button "Back" → Hide Current Panel: ✓
```

**Auto-Detection:**
- If Hide Panel ID is empty, automatically finds parent UIPanel2 component
- Traverses up the hierarchy starting from button's parent
- Returns first UIPanel2 found
- Saves development time - no need to manually set panel IDs for close buttons

### 3. Toggle Action

**Purpose:** Toggle panel visibility (show if hidden, hide if shown).

**Configuration:**
- Action Type: `Toggle`
- Show Panel ID: `"InventoryPanel"`

**Example:**
```
Button "Inventory" → Toggle Panel: "InventoryPanel"
```

### 4. Chain Action

**Purpose:** Hide one panel and show another (smooth transitions).

**Configuration:**
- Action Type: `Chain`
- Chain Hide Panel ID: `"StartPanel"`
- Chain Show Panel ID: `"GamePanel"`
- Chain Pass Data: (optional) Enable if passing data

**Example:**
```
Button "New Game" → Hide: "StartPanel" → Show: "GamePanel"
```

## Advanced Features

### Passing Data to Panels

If you need to pass custom data when showing a panel:

1. **Create a derived class:**
```csharp
using UnityEngine;

public class MyPanelButtonAction : UIPanelButtonAction
{
    [SerializeField] private string customData;
    
    protected override object CreateDataForPanel(string panelId)
    {
        // Return custom data based on panel ID
        if (panelId == "InventoryPanel")
        {
            return new InventoryData { itemCount = 10 };
        }
        return null;
    }
}
```

2. **Use the derived class** instead of base `UIPanelButtonAction`
3. **Enable "Pass Data"** in Inspector
4. Panel will receive data via `OnDataReceived` event

### Multiple Actions on One Button

You can add **multiple `UIPanelButtonAction` components** to one button:
- Component 1: Hide "StartPanel" (with Normal sound)
- Component 2: Show "GamePanel" (with Action sound)
- Component 3: Custom action (with Place sound)

**Note:** Each component plays its own sound. If you want only one sound, set others to `None`.

### Delay Before Action

For Show actions, you can add a delay:
- Show Delay: `0.5` seconds
- Panel will appear after 0.5 seconds

Useful for:
- Waiting for animation to finish
- Coordinating multiple UI changes
- Creating smooth transitions

## Sound Configuration

### Sound Types

The component includes **integrated FMOD sound support**:

1. **None** - No sound played
2. **Normal** - Standard button click sound (`OnButton`)
3. **Back** - Back/cancel button sound (`OnBack`)
4. **Action** - Action button sound (`OnAction`)
5. **Place** - Place-specific sound (`OnPlaces[]`) with availability check

### Place-Specific Sounds

For **Place** sound type:
- Select `Place` from Sound Type dropdown
- Choose `Specific Area` (Home/HiepMart/Company/Park)
- Sound only plays if the place is **available** at the estimated arrival time
- Unavailable places (Company after 5pm, Park after 6pm) won't play sound
- The unavailable event system handles SFX for unavailable places

### Sound Timing

Sounds are played **before** the action executes, so:
- Click sound → Then show panel
- Provides immediate audio feedback

### Example Sound Configurations

**Normal Button:**
```
Sound Type: Normal
→ Plays OnButton sound
```

**Back Button:**
```
Sound Type: Back
→ Plays OnBack sound (different tone)
```

**Place Button:**
```
Sound Type: Place
Specific Area: Company
→ Plays Company sound if available, nothing if unavailable
```

## Common Patterns

### Pattern 1: Main Menu Navigation

```
Start Menu:
├── Button "New Game" → Chain: Hide "StartPanel", Show "GamePanel"
├── Button "Settings" → Show: "SettingsPanel"
└── Button "Quit" → (custom quit logic)

Settings Menu:
└── Button "Back" → Hide Current Panel
```

### Pattern 2: Inventory Toggle

```
Button "Inventory" → Toggle: "InventoryPanel"
```

### Pattern 3: Modal Dialogs

```
Button "Open Dialog" → Show: "ConfirmDialog"
Dialog:
├── Button "Yes" → Hide: "ConfirmDialog" + Custom Action
└── Button "No" → Hide: "ConfirmDialog"
```

### Pattern 4: Panel Chains (Profile → Body → Clothes)

```
Profile Panel:
└── Button "Customize Body" → Chain: Hide "ProfilePanel", Show "BodyPanel"

Body Panel:
└── Button "Customize Clothes" → Chain: Hide "BodyPanel", Show "ClothesPanel"

Clothes Panel:
└── Button "Back" → Hide Current Panel
```

## Inspector Configuration Examples

### Example 1: Show Panel Button

```
UIPanelButtonAction:
├── Action Type: Show
├── Show Panel ID: "MainMenu"
├── Pass Data: ✗
└── Show Delay: 0
```

### Example 2: Close Button

```
UIPanelButtonAction:
├── Action Type: Hide
├── Hide Panel ID: ""
├── Hide Current Panel: ✓
└── (hides whatever panel is currently on top)
```

### Example 3: Transition Button

```
UIPanelButtonAction:
├── Action Type: Chain
├── Chain Hide Panel ID: "StartPanel"
├── Chain Show Panel ID: "GamePanel"
├── Chain Pass Data: ✗
└── (smooth transition between panels)
```

## Best Practices

### ✅ DO:
- Use descriptive Panel IDs
- Use Chain actions for smooth transitions
- Use "Hide Current Panel" for back buttons
- Enable Debug Mode during development
- Test all button actions in Play mode

### ❌ DON'T:
- Hard-code panel IDs in code (use component instead)
- Use Show action without Panel ID
- Forget to set Panel IDs in Inspector
- Add component to non-button GameObjects (auto-requires Button)

## Troubleshooting

### Button doesn't do anything:
1. Check Console for errors
2. Verify Panel ID matches exactly (case-sensitive)
3. Ensure UIPanelManager2 exists in scene
4. Check Debug Mode to see action logs

### Panel ID not found:
- Verify panel is registered (check Console on play)
- Panel ID must match exactly (case-sensitive)
- Make sure UIPanelManager2 is in scene

### Multiple actions execute:
- This is expected if you have multiple components
- Remove duplicates if unwanted

### Component requires Button:
- Add Component → Button first
- Or component auto-adds Button (RequireComponent)

## Benefits

✅ **No Code Required** - Everything in Inspector  
✅ **Reusable** - Same component works everywhere  
✅ **Maintainable** - Change behavior by editing component  
✅ **Flexible** - Supports Show/Hide/Toggle/Chain  
✅ **Integrated Sounds** - Built-in FMOD support, no separate component needed  
✅ **Decoupled** - Buttons don't directly reference manager  
✅ **Testable** - Each action is isolated  
✅ **All-in-One** - Actions + Sounds in single component  

## Integration with Existing System

The button action system works seamlessly with:
- `UIPanel2` components
- `UIPanelManager2` manager
- Automatic sorting order system
- Panel data passing
- Animation system

No conflicts - just add components and go!


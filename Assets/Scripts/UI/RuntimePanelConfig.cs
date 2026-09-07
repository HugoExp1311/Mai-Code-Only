using UnityEngine;

/// <summary>
/// Lightweight runtime configuration struct for scenario-based panel customization
/// No ScriptableObject needed - just a data structure
/// </summary>
[System.Serializable]
public struct RuntimePanelConfig
{
    public string panelTitle;
    public Sprite backgroundImage;
    public Color backgroundColor;
    public PanelBehaviorType behaviorType;
    public bool closeOnBackgroundClick;
    public bool disableUnderlyingPanels;
    public bool animateShow;
    public bool animateHide;
    public float animationDuration;
    
    /// <summary>
    /// Create default config
    /// </summary>
    public static RuntimePanelConfig Default => new RuntimePanelConfig
    {
        panelTitle = "",
        backgroundImage = null,
        backgroundColor = Color.white,
        behaviorType = PanelBehaviorType.Popup,
        closeOnBackgroundClick = true,
        disableUnderlyingPanels = true,
        animateShow = true,
        animateHide = true,
        animationDuration = 0.3f
    };
}

public enum PanelBehaviorType
{
    Base,      // Main screen - replaces other base panels (0-99)
    Overlay,   // Non-blocking HUD (100-199)
    Popup,     // Overlay - can stack (200-299)
    Modal      // Blocks everything underneath (300-399)
}

/// <summary>
/// Sound types for UI button interactions
/// </summary>
public enum SoundType
{
    None,       // No sound
    Normal,     // Normal button sound (OnButton)
    Back,       // Back button sound (OnBack)
    Action,     // Action sound (OnAction)
    Place       // Place-specific sound (OnPlaces) with availability check
}

/// <summary>
/// Game sections for organizing panels by game context
/// Sections are exclusive - only one section can be active at a time
/// </summary>
public enum GameSection
{
    MainMenu,     // Main menu screens
    InGame,       // In-game UI (HUD, gameplay panels)
    Minigame,     // Minigame-specific UI
    Simulation,   // Simulation mode UI
    CG,           // CG scene viewer (image sequences with dialogue)
    None          // For panels not bound to sections (e.g., Live2D)
}


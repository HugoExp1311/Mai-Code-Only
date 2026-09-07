using UnityEngine;

/// <summary>
/// Interface for tab button state management handlers
/// Supports different methods: Animator Controller, Color changes, Image swaps, etc.
/// </summary>
public interface ITabStateHandler
{
    /// <summary>
    /// Initialize the handler with tab information
    /// </summary>
    /// <param name="tabIds">Array of all tab IDs this handler manages</param>
    void Initialize(string[] tabIds);
    
    /// <summary>
    /// Switch to a specific tab (update button states)
    /// </summary>
    /// <param name="activeTabId">ID of the tab that should be active</param>
    void SwitchToTab(string activeTabId);
    
    /// <summary>
    /// Reset all tab states (e.g., when panel is closed)
    /// </summary>
    void ResetAllStates();
}


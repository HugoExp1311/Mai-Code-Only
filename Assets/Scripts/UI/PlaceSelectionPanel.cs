using Base;
using Base.Settings;
using Events;
using EventBus;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel for selecting and changing game areas
/// Inherits from UIPanel to integrate with the panel management system
/// Automatically manages button states based on current area and time availability
/// </summary>
public class PlaceSelectionPanel : UIPanel
{
    [Header("Area Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button hiepMartButton;
    [SerializeField] private Button companyButton;
    [SerializeField] private Button parkButton;
    
    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<PlaceUnavailableEvent> placeUnavailableBinding;
    

    
    private void Awake()
    {
        // UIPanel.Awake() is private, so Unity will call it automatically
        // We just need to initialize our own components here
        
        // Subscribe to events in Awake() so subscriptions persist even when panel is disabled
        // This ensures PlaceSelectionPanel always receives events, regardless of its enabled state
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        placeUnavailableBinding = new EventBinding<PlaceUnavailableEvent>(HandlePlaceUnavailable);
        
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<PlaceUnavailableEvent>.Register(placeUnavailableBinding);
    }
    
    private void OnDestroy()
    {
        // Ensure bindings are deregistered
        if (gameStartBinding != null)
        {
            EventBus<GameStartEvent>.Deregister(gameStartBinding);
            gameStartBinding = null;
        }
        
        if (placeChangedBinding != null)
        {
            EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
            placeChangedBinding = null;
        }
        
        if (placeUnavailableBinding != null)
        {
            EventBus<PlaceUnavailableEvent>.Deregister(placeUnavailableBinding);
            placeUnavailableBinding = null;
        }
    }
    
    protected override void OnShow(object data)
    {
        base.OnShow(data);
        
        // Update button states when panel is shown
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Handle game start event - initialize button states
    /// </summary>
    private void HandleGameStart(GameStartEvent args)
    {
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Handle place changed event - update button states and hide panel if visible
    /// </summary>
    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        UpdateButtonStates();
        
        // Hide panel if area successfully changed
        if (IsVisible)
        {
            Hide();
        }
    }
    
    /// <summary>
    /// Handle place unavailable event - re-enable the unavailable button
    /// (NoticeUI handles showing the message to the player)
    /// </summary>
    private void HandlePlaceUnavailable(PlaceUnavailableEvent args)
    {
        // Re-enable the button for the unavailable area
        // This allows the player to try again if time changes
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Button click handler for Home area
    /// </summary>
    public void OnHomeButtonClick()
    {
        if (GameManager.Instance != null)
        {
            Area previousArea = GameManager.Instance.Area;
            GameManager.Instance.ChangeArea((int)Area.Home);
            
            // Hide panel if area successfully changed
            if (GameManager.Instance.Area != previousArea && IsVisible)
            {
                Hide();
            }
        }
    }
    
    /// <summary>
    /// Button click handler for HiepMart area
    /// </summary>
    public void OnHiepMartButtonClick()
    {
        if (GameManager.Instance != null)
        {
            Area previousArea = GameManager.Instance.Area;
            GameManager.Instance.ChangeArea((int)Area.HiepMart);
            
            // Hide panel if area successfully changed
            if (GameManager.Instance.Area != previousArea && IsVisible)
            {
                Hide();
            }
        }
    }
    
    /// <summary>
    /// Button click handler for Company area
    /// </summary>
    public void OnCompanyButtonClick()
    {
        if (GameManager.Instance != null)
        {
            Area previousArea = GameManager.Instance.Area;
            GameManager.Instance.ChangeArea((int)Area.Company);
            
            // Hide panel if area successfully changed
            if (GameManager.Instance.Area != previousArea && IsVisible)
            {
                Hide();
            }
        }
    }
    
    /// <summary>
    /// Button click handler for Park area
    /// </summary>
    public void OnParkButtonClick()
    {
        if (GameManager.Instance != null)
        {
            Area previousArea = GameManager.Instance.Area;
            GameManager.Instance.ChangeArea((int)Area.Park);
            
            // Hide panel if area successfully changed
            if (GameManager.Instance.Area != previousArea && IsVisible)
            {
                Hide();
            }
        }
    }
    
    /// <summary>
    /// Update button states based on current area and time availability
    /// Disables buttons for current area and areas unavailable at estimated arrival time
    /// </summary>
    private void UpdateButtonStates()
    {
        if (GameManager.Instance == null)
            return;
        
        Area currentArea = GameManager.Instance.Area;
        DateTime currentTime = GameManager.Instance.Time;
        
        // Update each button
        UpdateButtonState(homeButton, Area.Home, currentArea, currentTime);
        UpdateButtonState(hiepMartButton, Area.HiepMart, currentArea, currentTime);
        UpdateButtonState(companyButton, Area.Company, currentArea, currentTime);
        UpdateButtonState(parkButton, Area.Park, currentArea, currentTime);
    }
    
    /// <summary>
    /// Update state for a single button
    /// </summary>
    private void UpdateButtonState(Button button, Area targetArea, Area currentArea, DateTime currentTime)
    {
        if (button == null)
            return;
        
        // Disable if it's the current area (player is already there)
        if (targetArea == currentArea)
        {
            button.gameObject.SetActive(false);
            return;
        }
        
        // IMPORTANT: Do NOT disable buttons when areas are unavailable
        // Unavailable buttons should remain enabled so clicks can trigger ChangeArea()
        // GameManager.ChangeArea() will check availability and raise PlaceUnavailableEvent if unavailable
        // NoticeUI will show and blocked SFX will play when PlaceUnavailableEvent is raised
        // This allows the player to click unavailable areas and see the notice message
        
        // Enable the button GameObject (even if unavailable - let ChangeArea() handle the check)
        button.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Check if area is available at estimated arrival time
    /// Uses same logic as GameManager for travel time and availability checking
    /// </summary>
    private bool IsAreaAvailable(Area targetArea, DateTime currentTime)
    {
        // Calculate travel time
        int travelTime = GetTravelTime(targetArea);
        TimeSpan travelDuration = TimeSpan.FromMinutes(travelTime);
        
        // Calculate estimated arrival time
        DateTime estimatedArrivalTime = currentTime + travelDuration;
        
        // Check if area is unavailable at that time
        return !IsAreaUnavailable(targetArea, estimatedArrivalTime);
    }
    
    /// <summary>
    /// Get travel time to an area
    /// </summary>
    private int GetTravelTime(Area area)
    {
        return DefaultSettings.GetTravelTime(area);
    }
    
    /// <summary>
    /// Check if area is unavailable at a specific time
    /// Uses same logic as GameManager.IsAreaUnavailable()
    /// </summary>
    private bool IsAreaUnavailable(Area area, DateTime timeToCheck)
    {
        return DefaultSettings.IsAreaUnavailableAtTime(area, timeToCheck);
    }
}


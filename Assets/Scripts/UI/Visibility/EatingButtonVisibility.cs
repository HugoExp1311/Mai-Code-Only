using UnityEngine;
using EventBus;
using Events;
using System;

/// <summary>
/// Manages Eating button visibility
/// Available: 7 PM to 9 PM (19:00 - 21:00), once per day
/// Follows MaiPanel pattern with event-based updates
/// </summary>
public class EatingButtonVisibility : MonoBehaviour
{
    private const int START_HOUR = 19; // 7 PM
    private const int END_HOUR = 21;   // 9 PM
    
    private EventBinding<TimeChangedEvent> timeChangedBinding;
    private EventBinding<GameStartEvent> gameStartBinding;
    
    private void Awake()
    {
        timeChangedBinding = new EventBinding<TimeChangedEvent>(HandleTimeChanged);
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        
        EventBus<TimeChangedEvent>.Register(timeChangedBinding);
        EventBus<GameStartEvent>.Register(gameStartBinding);
    }
    
    private void OnDestroy()
    {
        EventBus<TimeChangedEvent>.Deregister(timeChangedBinding);
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
    }
    
    private void HandleGameStart(GameStartEvent args)
    {
        UpdateVisibility(args.Time);
    }
    
    private void HandleTimeChanged(TimeChangedEvent args)
    {
        UpdateVisibility(args.Time);
    }
    
    private void UpdateVisibility(DateTime currentTime)
    {
        bool isInTimeRange = TimeManager.IsTimeInRange(currentTime, START_HOUR, END_HOUR);
        bool hasNotEatenToday = GameManager.Instance != null && !GameManager.Instance.HasEatenToday();
        
        bool shouldShow = isInTimeRange && hasNotEatenToday;
        gameObject.SetActive(shouldShow);
    }
}


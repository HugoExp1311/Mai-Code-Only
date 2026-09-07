using UnityEngine;
using EventBus;
using Events;
using System;

/// <summary>
/// Manages Sex button visibility
/// Available: 9 PM to 11 PM (21:00 - 23:00), once per day
/// Follows MaiPanel pattern with event-based updates
/// </summary>
public class SexButtonVisibility : MonoBehaviour
{
    private const int START_HOUR = 21; // 9 PM
    private const int END_HOUR = 23;   // 11 PM
    
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
        bool hasNotHadSexToday = GameManager.Instance != null && !GameManager.Instance.HasHadSexToday();
        
        bool shouldShow = isInTimeRange && hasNotHadSexToday;
        gameObject.SetActive(shouldShow);
    }
}


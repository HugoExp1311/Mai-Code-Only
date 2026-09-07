using EventBus;
using Base.Settings;

#nullable enable

/// <summary>
/// Event raised when area transition is ready to show the new area
/// </summary>
public struct AreaTransitionReadyEvent : IEvent
{
    public Area NewArea { get; set; }
}

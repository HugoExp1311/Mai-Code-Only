using EventBus;

#nullable enable

/// <summary>
/// Event raised when transition reaches mid-point (screen fully black)
/// This is when area/section setup should occur
/// </summary>
public struct TransitionMidPointEvent : IEvent
{
}

using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when Mai's sensitive points decay is applied (once per night).
    /// Subscribers should refresh all sensitive point displays when receiving this event.
    /// This replaces 5 individual StatsChangedEvent raises with a single batched event.
    /// </summary>
    public struct SensitivePointsDecayEvent : IEvent { }
}

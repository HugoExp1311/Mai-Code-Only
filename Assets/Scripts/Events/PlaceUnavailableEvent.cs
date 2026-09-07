using System;
using Base.Settings;
using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when a place is unavailable at the requested time
    /// </summary>
    [Serializable]
    public struct PlaceUnavailableEvent : IEvent
    {
        public Area CurrentArea;
        public Area TargetArea;
        public DateTime Time;
    }
}

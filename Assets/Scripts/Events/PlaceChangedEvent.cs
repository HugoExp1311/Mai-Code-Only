using System;
using Base.Settings;
using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when the player changes location/area
    /// </summary>
    [Serializable]
    public struct PlaceChangedEvent : IEvent
    {
        public Area Area;
        public DateTime Time;
        public DayCycle Cycle;
    }
}

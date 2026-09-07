using System;
using Base.Settings;
using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when a new game starts
    /// </summary>
    [Serializable]
    public struct GameStartEvent : IEvent
    {
        public Area Area;
        public DateTime Time;
        public DayCycle Cycle;
    }
}

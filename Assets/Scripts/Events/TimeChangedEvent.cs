using System;
using Base.Settings;
using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when time advances (can trigger day cycle changes)
    /// </summary>
    [Serializable]
    public struct TimeChangedEvent : IEvent
    {
        public Area Area;
        public DateTime Time;
        public DayCycle Cycle;
        public DayCycle PreviousCycle;
    }
}

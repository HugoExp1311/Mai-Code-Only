using System;
using Base.Character.Stats;
using Base.Dialogues;
using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when a character's resource value changes (e.g., Money)
    /// Used to notify UI panels to update their displays
    /// </summary>
    [Serializable]
    public struct ResourceChangedEvent : IEvent
    {
        public RewardTarget Target;    // Player or Mai
        public BasicResource Resource; // Which resource changed
        public int NewValue;           // New value of the resource
    }
}

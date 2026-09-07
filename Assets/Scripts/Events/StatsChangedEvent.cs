using System;
using Base.Character.Stats;
using Base.Dialogues;
using EventBus;

namespace Events
{
    /// <summary>
    /// Event raised when a character's stat value changes
    /// Used to notify UI panels to update their displays
    /// </summary>
    [Serializable]
    public struct StatsChangedEvent : IEvent
    {
        public RewardTarget Target;  // Player or Mai
        public BasicStats Stat;       // Which stat changed
        public int NewValue;          // New value of the stat
        public int MaxValue;          // Maximum value of the stat (0 if no max)
    }
}

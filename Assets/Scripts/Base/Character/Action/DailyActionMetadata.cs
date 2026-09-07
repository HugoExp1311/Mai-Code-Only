using System;
using System.Collections.Generic;
using Base.Character.Action;
using Base.Settings;

namespace Base.Character.Action
{
    /// <summary>
    /// Centralized metadata for daily actions including time windows and area restrictions
    /// References DefaultSettings for configuration values (follows SkillType pattern)
    /// </summary>
    public static class DailyActionMetadata
    {
        /// <summary>
        /// Time windows for when actions are available.
        /// References DefaultSettings.DailyActionTimeWindows
        /// </summary>
        public static IReadOnlyDictionary<PlayerDailyAction, (double Start, double Duration)?> TimeWindows 
            => DefaultSettings.DailyActionTimeWindows;

        /// <summary>
        /// Allowed areas for each action.
        /// References DefaultSettings.DailyActionAllowedAreas
        /// </summary>
        public static IReadOnlyDictionary<PlayerDailyAction, Area[]> AllowedAreas 
            => DefaultSettings.DailyActionAllowedAreas;

        /// <summary>
        /// Check if an action is available in the specified area
        /// </summary>
        public static bool IsAvailableInArea(PlayerDailyAction action, Area area)
        {
            if (!AllowedAreas.TryGetValue(action, out Area[] areas))
                return true; // Default: available everywhere

            return Array.Exists(areas, a => a == area);
        }
    }
}

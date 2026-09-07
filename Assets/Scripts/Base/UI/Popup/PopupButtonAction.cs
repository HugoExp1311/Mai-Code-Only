using Base.Character.Stats;
using UI;

namespace Base.UI.Popup
{
    /// <summary>
    /// Actions that can be executed by popup buttons (UI-specific)
    /// Follows CharacterActions pattern using abstract record with derived records
    /// </summary>
    public abstract record PopupButtonAction
    {
        /// <summary>
        /// Progress game time by specified hours
        /// </summary>
        public record ProgressTime(int Hours) : PopupButtonAction;
        
        /// <summary>
        /// Progress time to next day at specified hour/minute (e.g., 7:00 AM or 6:30 AM)
        /// </summary>
        public record ProgressTimeToNextDay(int TargetHour, int TargetMinute = 0) : PopupButtonAction;
        
        /// <summary>
        /// Change player stat (reuses existing CharacterActions.ChangeStat pattern)
        /// </summary>
        public record ChangeStat(RewardTarget Target, BasicStats Stat, int Amount) : PopupButtonAction;

        /// <summary>
        /// Change player max stat (e.g., Max Energy)
        /// </summary>
        public record ChangeMaxStat(RewardTarget Target, BasicStats Stat, int Amount) : PopupButtonAction;

        /// <summary>
        /// Change player resource (Money, etc.)
        /// </summary>
        public record ChangeResource(RewardTarget Target, BasicResource Resource, int Amount) : PopupButtonAction;

        /// <summary>
        /// Transition popup to a different state
        /// </summary>
        public record TransitionState(string TargetStateId) : PopupButtonAction;
        
        /// <summary>
        /// Close the popup
        /// </summary>
        public record ClosePopup() : PopupButtonAction;
        
        /// <summary>
        /// Execute multiple actions in sequence
        /// </summary>
        public record MultipleActions(PopupButtonAction[] Actions) : PopupButtonAction;
        
        /// <summary>
        /// Add work progress points (for Progress system)
        /// </summary>
        public record AddWorkProgress(int Amount) : PopupButtonAction;
        
        /// <summary>
        /// Apply work rewards based on current work level (Money, Knowledge, Progress, Stamina, Time)
        /// Uses DefaultSettings.GetWorkRewards() to calculate level-scaled rewards
        /// </summary>
        /// <param name="IsHardWork">If true, applies 2x multiplier on top of level multiplier</param>
        public record ApplyWorkRewards(bool IsHardWork = false) : PopupButtonAction;
        
        /// <summary>
        /// Switch to a different game section (e.g., Simulation for Sex scene)
        /// </summary>
        /// <param name="Section">Target GameSection to switch to</param>
        /// <param name="UseTransition">Whether to use transition animation</param>
        public record SwitchSection(GameSection Section, bool UseTransition = true) : PopupButtonAction;
    }
}


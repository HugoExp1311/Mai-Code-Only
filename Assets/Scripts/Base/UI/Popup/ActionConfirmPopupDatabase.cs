using System.Collections.Generic;
using Base.Character.Stats;
using UnityEngine;

namespace Base.UI.Popup
{
    /// <summary>
    /// Static database for Action Confirm Popup states
    /// Follows TimeManager pattern - stores all popup configurations in code
    /// Maps action names (Sleep, Eating, Sex) to popup states
    /// </summary>
    public static class ActionConfirmPopupDatabase
    {
        /// <summary>
        /// Mapping from action names to initial state IDs
        /// Action names correspond to CharacterActions records (Sleep, Eating, Sex)
        /// </summary>
        private static readonly IReadOnlyDictionary<string, string> ActionToStateMap = 
            new Dictionary<string, string>
        {
            { "Sleep", "SleepInitial" },
            { "Eating", "EatingInitial" },
            { "Sex", "SexInitial" },
            { "Working", "WorkingInitial" },
            { "Talking", "TalkingInitial" },
            { "Exercise", "ExerciseInitial" },
            { "Progress", "ProgressInitial" }
        };
        
        /// <summary>
        /// Custom icons set from Inspector (runtime only)
        /// Key = action name, Value = icon array
        /// </summary>
        private static readonly Dictionary<string, Sprite[]> CustomIcons = new Dictionary<string, Sprite[]>();
        
        /// <summary>
        /// Mapping from state IDs to their icon index
        /// Used to determine which custom icon to use for each state
        /// </summary>
        private static readonly IReadOnlyDictionary<string, int> StateIconIndices = 
            new Dictionary<string, int>
        {
            // Sleep states
            { "SleepInitial", 0 },
            { "NapState", 0 },
            { "DeepSleepState", 0 },
            // Eating states
            { "EatingInitial", 0 },
            { "EatingWithMaiState", 1 }, // Secondary icon for eating with Mai
            // Sex states
            { "SexInitial", 0 },
            // Working states
            { "WorkingInitial", 0 },
            { "WorkNormalState", 0 },
            { "WorkHardState", 0 },
            // Talking states
            { "TalkingInitial", 0 },
            { "TalkingResultState", 0 },
            // Exercise states
            { "ExerciseInitial", 0 },
            { "ExerciseNormalState", 0 },
            { "ExerciseHardState", 0 },
            // Progress states
            { "ProgressInitial", 0 }
        };
        
        /// <summary>
        /// All popup states indexed by state ID
        /// </summary>
        private static readonly IReadOnlyDictionary<string, PopupState> States = 
            new Dictionary<string, PopupState>
        {
            // ========== SLEEP ACTION STATES ==========
            
            // Initial state: Choose sleep type
            // Limit checks are handled by ActionConfirmPopup.OnSleepButtonClicked()
            {
                "SleepInitial", new PopupState
                {
                    stateId = "SleepInitial",
                    headerText = "I'm tired, I need to sleep",
                    iconSprite = null, // TODO: Add sleep icon sprite
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Take a nap (2 hours, +20 Energy) — max 2/day
                        new PopupButtonState
                        {
                            buttonText = "Take a nap",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SleepNap].Stamina),
                                new PopupButtonAction.ProgressTime(DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SleepNap].TimeHours),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Button 2: Sweet dream (+80 Energy, -2 Knowledge, wake at 6:30 AM next day) — max 1/day, 9PM-6AM only
                        new PopupButtonState
                        {
                            buttonText = "Sweet dream...",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SleepDeep].Stamina),
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Knowledge, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SleepDeep].Knowledge),
                                new PopupButtonAction.ProgressTimeToNextDay(
                                    DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SleepDeep].NextDayHour.Value,
                                    DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SleepDeep].NextDayMinute),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Button 3: Cancel
                        new PopupButtonState
                        {
                            buttonText = "Nope, I'm still awake",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        }
                    }
                }
            },
            
            // Nap result state: Show result after nap
            {
                "NapState", new PopupState
                {
                    stateId = "NapState",
                    headerText = "I will wake up in an hour",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Result display (non-interactable)
                        new PopupButtonState
                        {
                            buttonText = "+20 Energy",
                            boosterText = "+ 20 Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },
            
            // Deep sleep result state: Show result after deep sleep
            {
                "DeepSleepState", new PopupState
                {
                    stateId = "DeepSleepState",
                    headerText = "ZZZ...",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Result display (non-interactable)
                        new PopupButtonState
                        {
                            buttonText = "+80 Energy\n- 2 Knowledge",
                            boosterText = "+ 80 Energy\n- 2 Knowledge",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },
            
            // ========== EATING ACTION STATES ==========
            
            // Initial state: Ask to eat with Mai
            {
                "EatingInitial", new PopupState
                {
                    stateId = "EatingInitial",
                    headerText = "Eating with Mai?",
                    iconSprite = null, // TODO: Add eating icon sprite
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Accept eating with Mai
                        new PopupButtonState
                        {
                            buttonText = "Sure, why not?",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.TransitionState("EatingWithMaiState")
                        },
                        // Button 2: Decline
                        new PopupButtonState
                        {
                            buttonText = "I'm not hungry",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        },
                        // Button 3: Disabled
                        PopupButtonState.Disabled
                    }
                }
            },
            
            // Eating with Mai state: Show result and execute action
            {
                "EatingWithMaiState", new PopupState
                {
                    stateId = "EatingWithMaiState",
                    headerText = "Food is delicious when eaten with Mai",
                    iconSprite = null, // TODO: Add different eating icon sprite (if needed)
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Execute eating action (non-interactable, shows result)
                        new PopupButtonState
                        {
                            buttonText = "+5 Love\n+ 40 Energy",
                            boosterText = "+ 5 Love\n+ 40 Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeStat(RewardTarget.Mai, BasicStats.Love, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.EatingWithMai].Love),
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.EatingWithMai].Stamina),
                                new PopupButtonAction.ProgressTime(DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.EatingWithMai].TimeHours),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },
            
            // ========== SEX ACTION STATES ==========
            
            // Initial state: Ask to have sex with Mai
            {
                "SexInitial", new PopupState
                {
                    stateId = "SexInitial",
                    headerText = "Have sex with Mai?",
                    iconSprite = null, // TODO: Add sex icon sprite
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Accept sex (triggers minigame later, for now just grant energy)
                        new PopupButtonState
                        {
                            buttonText = "Fuck yeah!",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ClosePopup(),
                                new PopupButtonAction.SwitchSection(GameSection.Simulation, true)
                            })
                        },
                        // Button 2: Decline and sleep
                        new PopupButtonState
                        {
                            buttonText = "I wanna sleep",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SexDecline].Stamina),
                                new PopupButtonAction.ProgressTimeToNextDay(
                                    DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SexDecline].NextDayHour.Value,
                                    DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.SexDecline].NextDayMinute),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Button 3: Disabled
                        PopupButtonState.Disabled
                    }
                }
            },

            // ========== WORKING ACTION STATES ==========

            // Initial state: Choose how hard to work
            {
                "WorkingInitial", new PopupState
                {
                    stateId = "WorkingInitial",
                    headerText = "Start Working",
                    iconSprite = null, // TODO: Add working icon sprite
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Work normally
                        new PopupButtonState
                        {
                            buttonText = "Work",
                            boosterText = "+ Money\n+ Knowledge\n- Energy",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.TransitionState("WorkNormalState")
                        },
                        // Button 2: Work hard
                        new PopupButtonState
                        {
                            buttonText = "Work hard",
                            boosterText = "+ Money\n+ Knowledge\n- Energy",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.TransitionState("WorkHardState")
                        },
                        // Button 3: Cancel
                        new PopupButtonState
                        {
                            buttonText = "I'm Lazy",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        }
                    }
                }
            },

            // Work normal state: Show result and execute action
            {
                "WorkNormalState", new PopupState
                {
                    stateId = "WorkNormalState",
                    headerText = "My job is done!",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Execute work action with level-based rewards
                        new PopupButtonState
                        {
                            buttonText = "+Money\n+ Knowledge\n+ Progress\n- Energy",
                            boosterText = "+ Money\n+ Knowledge\n+ Progress\n- Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ApplyWorkRewards(),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },

            // Work hard state: Show result and execute action
            {
                "WorkHardState", new PopupState
                {
                    stateId = "WorkHardState",
                    headerText = "My job is done!",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Execute work hard action with level-based rewards (x2 multiplier on top)
                        new PopupButtonState
                        {
                            buttonText = "+Money\n+ Knowledge\n+ Progress\n- Energy",
                            boosterText = "+ Money\n+ Knowledge\n+ Progress\n- Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ApplyWorkRewards(true),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },

            // ========== TALKING ACTION STATES ==========

            // Initial state: Ask to talk
            {
                "TalkingInitial", new PopupState
                {
                    stateId = "TalkingInitial",
                    headerText = "Talk to someone?",
                    iconSprite = null, // TODO: Add talking icon sprite
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Accept talking
                        new PopupButtonState
                        {
                            buttonText = "Let's talk!",
                            boosterText = "+ Charming\n- Energy",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.TransitionState("TalkingResultState")
                        },
                        // Button 2: Decline
                        new PopupButtonState
                        {
                            buttonText = "I'm an introvert",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        },
                        // Button 3: Disabled
                        PopupButtonState.Disabled
                    }
                }
            },

            // Talking result state: Show result and execute action
            {
                "TalkingResultState", new PopupState
                {
                    stateId = "TalkingResultState",
                    headerText = "It's nice to talk to everyone",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Execute talking action (non-interactable, shows result)
                        new PopupButtonState
                        {
                            buttonText = "+5 Charming\n- 10 Energy",
                            boosterText = "+ 5 Charming\n- 10 Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Charming, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.Talking].Charming),
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.Talking].Stamina),
                                new PopupButtonAction.ProgressTime(DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.Talking].TimeHours),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },

            // ========== EXERCISE ACTION STATES ==========

            // Initial state: Choose exercise intensity
            {
                "ExerciseInitial", new PopupState
                {
                    stateId = "ExerciseInitial",
                    headerText = "Start Exercise?",
                    iconSprite = null, // TODO: Add exercise icon sprite
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Normal exercise
                        new PopupButtonState
                        {
                            buttonText = "Okay...",
                            boosterText = "+ Max Energy\n- Energy",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.TransitionState("ExerciseNormalState")
                        },
                        // Button 2: Hard exercise
                        new PopupButtonState
                        {
                            buttonText = "Try my best!",
                            boosterText = "+ Max Energy\n- Energy",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.TransitionState("ExerciseHardState")
                        },
                        // Button 3: Cancel
                        new PopupButtonState
                        {
                            buttonText = "I'm lazy...",
                            boosterText = "",
                            isInteractable = true,
                            isEnabled = true,
                            action = new PopupButtonAction.ClosePopup()
                        }
                    }
                }
            },

            // Exercise normal state: Show result and execute action
            {
                "ExerciseNormalState", new PopupState
                {
                    stateId = "ExerciseNormalState",
                    headerText = "Done! So tired...",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Execute normal exercise action (non-interactable, shows result)
                        new PopupButtonState
                        {
                            buttonText = "+5 Max Energy\n- 20 Energy",
                            boosterText = "+ 5 Max Energy\n- 20 Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeMaxStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.ExerciseNormal].MaxStamina),
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.ExerciseNormal].Stamina),
                                new PopupButtonAction.ProgressTime(DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.ExerciseNormal].TimeHours),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },

            // Exercise hard state: Show result and execute action
            {
                "ExerciseHardState", new PopupState
                {
                    stateId = "ExerciseHardState",
                    headerText = "Done! So tired...",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // Button 1: Execute hard exercise action (non-interactable, shows result)
                        new PopupButtonState
                        {
                            buttonText = "+10 Max Energy\n- 40 Energy",
                            boosterText = "+ 10 Max Energy\n- 40 Energy",
                            isInteractable = false,
                            isEnabled = true,
                            action = new PopupButtonAction.MultipleActions(new PopupButtonAction[]
                            {
                                new PopupButtonAction.ChangeMaxStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.ExerciseHard].MaxStamina),
                                new PopupButtonAction.ChangeStat(RewardTarget.Player, BasicStats.Stamina, DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.ExerciseHard].Stamina),
                                new PopupButtonAction.ProgressTime(DefaultSettings.DailyActionRewards[DefaultSettings.DailyActionVariant.ExerciseHard].TimeHours),
                                new PopupButtonAction.ClosePopup()
                            })
                        },
                        // Buttons 2-3: Disabled
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            },
            
            // ========== PROGRESS ACTION STATES ==========
            // Progress action uses a unique popup layout - this state is a placeholder
            // The actual UI is handled by ActionConfirmPopup's Progress-specific layout
            {
                "ProgressInitial", new PopupState
                {
                    stateId = "ProgressInitial",
                    headerText = "Work Progress",
                    iconSprite = null,
                    buttons = new PopupButtonState[]
                    {
                        // All buttons disabled - Progress uses custom layout
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled,
                        PopupButtonState.Disabled
                    }
                }
            }
        };
        
        /// <summary>
        /// Get initial popup state for an action name
        /// </summary>
        /// <param name="actionName">Action name corresponding to CharacterActions (e.g., "Sleep", "Eating", "Sex")</param>
        /// <returns>Initial popup state, or null if not found</returns>
        public static PopupState GetInitialState(string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                Debug.LogWarning("[ActionConfirmPopupDatabase] Action name is null or empty");
                return null;
            }
            
            if (ActionToStateMap.TryGetValue(actionName, out string initialStateId))
            {
                return GetState(initialStateId);
            }
            
            Debug.LogWarning($"[ActionConfirmPopupDatabase] No initial state found for action: {actionName}");
            return null;
        }
        
        /// <summary>
        /// Get popup state by state ID
        /// </summary>
        /// <param name="stateId">State ID (e.g., "SleepInitial", "NapState")</param>
        /// <returns>Popup state, or null if not found</returns>
        public static PopupState GetState(string stateId)
        {
            if (string.IsNullOrEmpty(stateId))
            {
                Debug.LogWarning("[ActionConfirmPopupDatabase] State ID is null or empty");
                return null;
            }
            
            if (States.TryGetValue(stateId, out PopupState state))
            {
                if (!state.IsValid())
                {
                    Debug.LogError($"[ActionConfirmPopupDatabase] State '{stateId}' is invalid (must have exactly 3 buttons)");
                    return null;
                }
                return state;
            }
            
            Debug.LogWarning($"[ActionConfirmPopupDatabase] State not found: {stateId}");
            return null;
        }
        
        /// <summary>
        /// Check if an action name exists in the database
        /// </summary>
        public static bool HasAction(string actionName)
        {
            return !string.IsNullOrEmpty(actionName) && ActionToStateMap.ContainsKey(actionName);
        }
        
        /// <summary>
        /// Check if a state ID exists in the database
        /// </summary>
        public static bool HasState(string stateId)
        {
            return !string.IsNullOrEmpty(stateId) && States.ContainsKey(stateId);
        }
        
        /// <summary>
        /// Set custom icons for an action (called from Inspector configuration)
        /// </summary>
        /// <param name="actionName">Action name (Sleep, Eating, Sex)</param>
        /// <param name="icons">Array of icon sprites</param>
        public static void SetCustomIcons(string actionName, Sprite[] icons)
        {
            if (string.IsNullOrEmpty(actionName) || icons == null || icons.Length == 0)
                return;
            
            CustomIcons[actionName] = icons;
        }
        
        /// <summary>
        /// Get custom icon for a specific state
        /// </summary>
        /// <param name="actionName">Action name (Sleep, Eating, Sex)</param>
        /// <param name="stateId">State ID</param>
        /// <returns>Custom icon sprite, or null if not set</returns>
        public static Sprite GetCustomIcon(string actionName, string stateId)
        {
            if (!CustomIcons.TryGetValue(actionName, out Sprite[] icons))
                return null;
            
            if (!StateIconIndices.TryGetValue(stateId, out int iconIndex))
                return null;
            
            if (iconIndex >= 0 && iconIndex < icons.Length)
                return icons[iconIndex];
            
            return null;
        }
        
        /// <summary>
        /// Clear custom icons (useful for cleanup)
        /// </summary>
        public static void ClearCustomIcons()
        {
            CustomIcons.Clear();
        }
    }
}


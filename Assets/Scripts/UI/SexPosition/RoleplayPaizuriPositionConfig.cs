using System;
using UnityEngine;

namespace UI.SexPosition
{
    /// <summary>
    /// Runtime strategy for the Roleplay Paizuri Live2D position. All nine
    /// actions share Slow/Fast blend trees; categories lock until Stop while
    /// actions inside the selected category remain freely switchable.
    /// </summary>
    [Serializable]
    public sealed class RoleplayPaizuriPositionConfig : SexPositionConfig
    {
        private enum RoleplayPlaybackMode
        {
            Start,
            ReturningToStart,
            Slow,
            Fast,
            Cumming,
            AfterCumming
        }

        private const string SlowLoop = "SlowLoop";
        private const string FastLoop = "FastLoop";
        private const string SlowValue = "SlowValue";
        private const string FastValue = "FastValue";
        private const string TriggerCumDick1 = "CumDick1";
        private const string TriggerCumDick2 = "CumDick2";
        private const string TriggerCumDick3 = "CumDick3";
        private const string TriggerCumMai = "CumMai";
        private const string TriggerStop = "Stop";

        private RoleplayCategory currentCategory = RoleplayCategory.Boob;
        private int currentAnimationNumber;
        private int currentBlendIndex;
        private RoleplayPlaybackMode playbackMode = RoleplayPlaybackMode.Start;
        private bool categoryLocked;
        private bool hasSelectedAnimation;

        public RoleplayPaizuriPositionConfig()
        {
            positionName = "Roleplay Paizuri";
            initialState = SimulationState.Active;
        }

        protected override void ConfigureStateButtonMap()
        {
            stateButtonMap[SimulationState.Active] = new ButtonVisibilitySet
            {
                slowButton = true,
                fastButton = true,
                stopButton = true,
                roleplaySelectorButtons = true,
                finishButton = true
            };

            stateButtonMap[SimulationState.Thrusting] = new ButtonVisibilitySet
            {
                slowButton = true,
                fastButton = true,
                stopButton = true,
                roleplaySelectorButtons = true,
                finishButton = true
            };

            stateButtonMap[SimulationState.Transitioning] = new ButtonVisibilitySet
            {
                finishButton = true
            };

            stateButtonMap[SimulationState.AfterCumming] = new ButtonVisibilitySet
            {
                stopButton = true,
                finishButton = true
            };
        }

        public override void OnPositionEntered(SimulationNavigationPanel panel)
        {
            currentCategory = RoleplayCategory.Boob;
            currentAnimationNumber = 0;
            currentBlendIndex = 0;
            playbackMode = RoleplayPlaybackMode.Start;
            categoryLocked = false;
            hasSelectedAnimation = false;

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.PlayDefaultAnimation();
            panel.SetState(initialState);
            RefreshControls(panel);
        }

        public override RoleplayCategory GetRoleplayCategoryForButton(int buttonNumber)
        {
            return buttonNumber switch
            {
                1 => RoleplayCategory.Boob,
                2 => RoleplayCategory.Hand,
                3 => RoleplayCategory.Tongue,
                _ => throw new ArgumentOutOfRangeException(nameof(buttonNumber))
            };
        }

        public override string GetRoleplayCategoryButtonLabel(int buttonNumber)
        {
            return buttonNumber switch
            {
                1 => "Boob",
                2 => "Hand",
                3 => "Tongue",
                _ => string.Empty
            };
        }

        public override int GetRoleplayAnimationButtonCount()
        {
            return currentCategory switch
            {
                RoleplayCategory.Boob => 4,
                RoleplayCategory.Hand => 3,
                RoleplayCategory.Tongue => 2,
                _ => 0
            };
        }

        public override bool AreRoleplayCategoriesAvailable() => !categoryLocked;

        public override void OnInsertClicked(SimulationNavigationPanel panel)
        {
            panel.ShowSimulationLive2D();
            panel.SetState(SimulationState.Active);
            RefreshControls(panel);
        }

        public override void OnHoleSelected(
            SimulationNavigationPanel panel,
            HoleType holeType)
        {
            // Roleplay Paizuri has no hole-selection step.
        }

        public override void OnRoleplayCategorySelected(
            SimulationNavigationPanel panel,
            RoleplayCategory category)
        {
            if (!CanSelectAction(panel) || categoryLocked)
            {
                return;
            }

            if (category != RoleplayCategory.Boob &&
                category != RoleplayCategory.Hand &&
                category != RoleplayCategory.Tongue)
            {
                Debug.LogWarning($"[RoleplayPaizuri] Unsupported category: {category}");
                return;
            }

            currentCategory = category;
            currentAnimationNumber = 0;
            currentBlendIndex = GetBlendIndex(category, 1);
            categoryLocked = true;
            hasSelectedAnimation = false;
            RefreshControls(panel);
        }

        public override void OnRoleplayAnimationSelected(
            SimulationNavigationPanel panel,
            int animationNumber)
        {
            if (!CanSelectAction(panel) || !categoryLocked)
            {
                return;
            }

            int buttonCount = GetRoleplayAnimationButtonCount();
            if (animationNumber < 1 || animationNumber > buttonCount)
            {
                Debug.LogWarning(
                    $"[RoleplayPaizuri] Animation {animationNumber} is invalid for {currentCategory}.");
                return;
            }

            currentAnimationNumber = animationNumber;
            currentBlendIndex = GetBlendIndex(currentCategory, animationNumber);
            hasSelectedAnimation = true;

            // Every numbered action begins immediately at Slow. Selecting a
            // new action while Fast is active intentionally returns to Slow.
            BeginBlendLoop(panel, false);
            RefreshControls(panel);
        }

        public override void OnThrustStarted(
            SimulationNavigationPanel panel,
            bool isFast)
        {
            if (!CanSelectAction(panel) ||
                !categoryLocked ||
                !hasSelectedAnimation)
            {
                return;
            }

            BeginBlendLoop(panel, isFast);
        }

        public override void OnThrustStopped(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.Cumming ||
                panel.CurrentState == SimulationState.Transitioning)
            {
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.Start)
            {
                UnlockCategory(panel);
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            playbackMode = RoleplayPlaybackMode.ReturningToStart;
            panel.SetState(SimulationState.Active);
            panel.SetRoleplaySpeedAvailability(false, false);
            panel.TriggerAnimation(TriggerStop);
        }

        public override void OnCumDecision(
            SimulationNavigationPanel panel,
            CumDecisionType decision)
        {
            // Roleplay completion is delivered by the shared Player/Mai bars.
        }

        public override void OnPlayerCumReached(SimulationNavigationPanel panel)
        {
            if (currentCategory != RoleplayCategory.Boob ||
                !IsActivePlayback(playbackMode) ||
                !hasSelectedAnimation)
            {
                return;
            }

            BeginCumming(panel, GetPlayerCumTrigger());
        }

        public override void OnMaiOrgasm(SimulationNavigationPanel panel)
        {
            if (currentCategory == RoleplayCategory.Boob ||
                !IsActivePlayback(playbackMode) ||
                !hasSelectedAnimation)
            {
                return;
            }

            panel.MarkRoleplayOpeningComplete();
            BeginCumming(panel, TriggerCumMai);
        }

        public override void OnAfterCummingEntered(SimulationNavigationPanel panel)
        {
            if (playbackMode != RoleplayPlaybackMode.Cumming)
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            playbackMode = RoleplayPlaybackMode.AfterCumming;
            panel.SetState(SimulationState.AfterCumming);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        public override void OnRoleplayStartEntered(SimulationNavigationPanel panel)
        {
            if (playbackMode != RoleplayPlaybackMode.Start &&
                playbackMode != RoleplayPlaybackMode.ReturningToStart)
            {
                return;
            }

            playbackMode = RoleplayPlaybackMode.Start;
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            UnlockCategory(panel);
        }

        private void BeginBlendLoop(SimulationNavigationPanel panel, bool isFast)
        {
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);

            if (isFast)
            {
                panel.SetFloat(FastValue, currentBlendIndex);
                panel.SetBool(FastLoop, true);
                playbackMode = RoleplayPlaybackMode.Fast;
            }
            else
            {
                panel.SetFloat(SlowValue, currentBlendIndex);
                panel.SetBool(SlowLoop, true);
                playbackMode = RoleplayPlaybackMode.Slow;
            }

            panel.SetState(SimulationState.Thrusting);
            SexSimulationManager.Instance?.StartRoleplayPaizuriPlayback(
                currentCategory,
                isFast);
            ApplySpeedAvailability(panel);
        }

        private void BeginCumming(
            SimulationNavigationPanel panel,
            string trigger)
        {
            if (string.IsNullOrEmpty(trigger))
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            playbackMode = RoleplayPlaybackMode.Cumming;
            panel.SetState(SimulationState.Transitioning);
            panel.SetRoleplaySpeedAvailability(false, false);
            panel.TriggerAnimation(trigger);
        }

        private string GetPlayerCumTrigger()
        {
            if (currentCategory != RoleplayCategory.Boob)
            {
                return string.Empty;
            }

            return currentAnimationNumber switch
            {
                1 => TriggerCumDick1,
                2 => TriggerCumDick2,
                3 => TriggerCumDick1,
                4 => TriggerCumDick3,
                _ => string.Empty
            };
        }

        private void UnlockCategory(SimulationNavigationPanel panel)
        {
            categoryLocked = false;
            hasSelectedAnimation = false;
            currentAnimationNumber = 0;
            panel.SetState(SimulationState.Active);
            RefreshControls(panel);
        }

        private static void ClearLoopBools(SimulationNavigationPanel panel)
        {
            panel.SetBool(SlowLoop, false);
            panel.SetBool(FastLoop, false);
        }

        private static void ResetTransientTriggers(SimulationNavigationPanel panel)
        {
            panel.ResetAnimationTrigger(TriggerCumDick1);
            panel.ResetAnimationTrigger(TriggerCumDick2);
            panel.ResetAnimationTrigger(TriggerCumDick3);
            panel.ResetAnimationTrigger(TriggerCumMai);
            panel.ResetAnimationTrigger(TriggerStop);
        }

        private void RefreshControls(SimulationNavigationPanel panel)
        {
            panel.RefreshRoleplayControls();
            ApplySpeedAvailability(panel);
        }

        private void ApplySpeedAvailability(SimulationNavigationPanel panel)
        {
            bool speedAvailable =
                categoryLocked &&
                hasSelectedAnimation &&
                panel.CurrentState != SimulationState.Transitioning &&
                panel.CurrentState != SimulationState.AfterCumming;
            panel.SetRoleplaySpeedAvailability(speedAvailable, speedAvailable);
        }

        private bool CanSelectAction(SimulationNavigationPanel panel)
        {
            return panel.CurrentState != SimulationState.Transitioning &&
                   panel.CurrentState != SimulationState.AfterCumming &&
                   playbackMode != RoleplayPlaybackMode.Cumming &&
                   playbackMode != RoleplayPlaybackMode.ReturningToStart;
        }

        private static bool IsActivePlayback(RoleplayPlaybackMode mode)
        {
            return mode == RoleplayPlaybackMode.Slow ||
                   mode == RoleplayPlaybackMode.Fast;
        }

        private static int GetBlendIndex(
            RoleplayCategory category,
            int animationNumber)
        {
            return category switch
            {
                RoleplayCategory.Boob => animationNumber - 1,
                RoleplayCategory.Hand => animationNumber + 3,
                RoleplayCategory.Tongue => animationNumber + 6,
                _ => 0
            };
        }
    }
}

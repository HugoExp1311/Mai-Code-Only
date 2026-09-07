using System;
using Base.Character.Skills;
using UnityEngine;

namespace UI.SexPosition
{
    /// <summary>
    /// Runtime strategy for the Roleplay Blowjob Live2D position.
    /// Handjob and Blowjob actions use shared Slow/Fast blend trees while the
    /// three Tongue actions are standalone loops.
    /// </summary>
    [Serializable]
    public sealed class RoleplayBlowjobPositionConfig : SexPositionConfig
    {
        private enum RoleplayPlaybackMode
        {
            Start,
            ReturningToStart,
            Slow,
            Fast,
            KissDick,
            TongueMove1,
            TongueMove2,
            Cumming,
            AfterCumming
        }

        private const string SlowLoop = "SlowLoop";
        private const string FastLoop = "FastLoop";
        private const string SlowValue = "SlowValue";
        private const string FastValue = "FastValue";
        private const string TriggerKissDick = "KissDick";
        private const string TriggerTongueMove1 = "TongueMove1";
        private const string TriggerTongueMove2 = "TongueMove2";
        private const string TriggerCumHandjob = "CumHandjob";
        private const string TriggerCumBlowjob1 = "CumBlowjob1";
        private const string TriggerCumBlowjob2 = "CumBlowjob2";
        private const string TriggerCumBlowjob3 = "CumBlowjob3";
        private const string TriggerCumFace = "CumFace";
        private const string TriggerStop = "Stop";

        private RoleplayCategory currentCategory = RoleplayCategory.Hand;
        private int currentAnimationNumber;
        private int currentBlendIndex;
        private RoleplayPlaybackMode playbackMode = RoleplayPlaybackMode.Start;
        private bool categoryLocked;
        private bool hasSelectedAnimation;

        public RoleplayBlowjobPositionConfig()
        {
            positionName = "Roleplay Blowjob";
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
            currentCategory = RoleplayCategory.Hand;
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
                1 => RoleplayCategory.Hand,
                2 => RoleplayCategory.Blowjob,
                3 => RoleplayCategory.Tongue,
                _ => throw new ArgumentOutOfRangeException(nameof(buttonNumber))
            };
        }

        public override string GetRoleplayCategoryButtonLabel(int buttonNumber)
        {
            return buttonNumber switch
            {
                1 => "Handjob",
                2 => "Blowjob",
                3 => "Tongue",
                _ => string.Empty
            };
        }

        public override int GetRoleplayAnimationButtonCount()
        {
            return currentCategory == RoleplayCategory.Hand ? 2 : 3;
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
            // Roleplay Blowjob has no hole-selection step.
        }

        public override void OnRoleplayCategorySelected(
            SimulationNavigationPanel panel,
            RoleplayCategory category)
        {
            if (!CanSelectAction(panel) || categoryLocked)
            {
                return;
            }

            if (category != RoleplayCategory.Hand &&
                category != RoleplayCategory.Blowjob &&
                category != RoleplayCategory.Tongue)
            {
                Debug.LogWarning($"[RoleplayBlowjob] Unsupported category: {category}");
                return;
            }

            currentCategory = category;
            currentAnimationNumber = 0;
            currentBlendIndex = category == RoleplayCategory.Blowjob ? 2 : 0;
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
                    $"[RoleplayBlowjob] Animation {animationNumber} is invalid for {currentCategory}.");
                return;
            }

            currentAnimationNumber = animationNumber;
            hasSelectedAnimation = true;

            if (currentCategory == RoleplayCategory.Tongue)
            {
                BeginTongueLoop(panel, animationNumber);
            }
            else
            {
                currentBlendIndex = GetBlendIndex(currentCategory, animationNumber);
                BeginBlendLoop(panel, false);
            }

            RefreshControls(panel);
        }

        public override void OnThrustStarted(
            SimulationNavigationPanel panel,
            bool isFast)
        {
            if (!CanSelectAction(panel) ||
                !categoryLocked ||
                !hasSelectedAnimation ||
                currentCategory == RoleplayCategory.Tongue)
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
            // Player Cum completion is delivered through OnPlayerCumReached.
        }

        public override void OnMaiOrgasm(SimulationNavigationPanel panel)
        {
            // Mai's orgasm is counted and rewarded by SexSimulationManager but
            // does not interrupt the player-cum-driven Blowjob animation.
        }

        public override void OnPlayerCumReached(SimulationNavigationPanel panel)
        {
            if (!IsActivePlayback(playbackMode) || !hasSelectedAnimation)
            {
                return;
            }

            string cumTrigger = GetCumTrigger();
            if (string.IsNullOrEmpty(cumTrigger))
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            playbackMode = RoleplayPlaybackMode.Cumming;
            panel.SetState(SimulationState.Transitioning);
            panel.SetRoleplaySpeedAvailability(false, false);
            panel.TriggerAnimation(cumTrigger);
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
            SexSimulationManager.Instance?.StartThrusting(
                GetSkillForCurrentAction(),
                isFast);
            ApplySpeedAvailability(panel);
        }

        private void BeginTongueLoop(
            SimulationNavigationPanel panel,
            int animationNumber)
        {
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);

            string trigger;
            switch (animationNumber)
            {
                case 1:
                    trigger = TriggerKissDick;
                    playbackMode = RoleplayPlaybackMode.KissDick;
                    break;
                case 2:
                    trigger = TriggerTongueMove1;
                    playbackMode = RoleplayPlaybackMode.TongueMove1;
                    break;
                default:
                    trigger = TriggerTongueMove2;
                    playbackMode = RoleplayPlaybackMode.TongueMove2;
                    break;
            }

            panel.SetState(SimulationState.Thrusting);
            panel.TriggerAnimation(trigger);
            SexSimulationManager.Instance?.StartThrusting(SkillType.Tongue, false);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        private string GetCumTrigger()
        {
            if (currentCategory == RoleplayCategory.Hand)
            {
                return TriggerCumHandjob;
            }

            if (currentCategory == RoleplayCategory.Tongue)
            {
                return TriggerCumFace;
            }

            return currentAnimationNumber switch
            {
                1 => TriggerCumBlowjob1,
                2 => TriggerCumBlowjob2,
                3 => TriggerCumBlowjob3,
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
            panel.ResetAnimationTrigger(TriggerKissDick);
            panel.ResetAnimationTrigger(TriggerTongueMove1);
            panel.ResetAnimationTrigger(TriggerTongueMove2);
            panel.ResetAnimationTrigger(TriggerCumHandjob);
            panel.ResetAnimationTrigger(TriggerCumBlowjob1);
            panel.ResetAnimationTrigger(TriggerCumBlowjob2);
            panel.ResetAnimationTrigger(TriggerCumBlowjob3);
            panel.ResetAnimationTrigger(TriggerCumFace);
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
                currentCategory != RoleplayCategory.Tongue &&
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
                   mode == RoleplayPlaybackMode.Fast ||
                   mode == RoleplayPlaybackMode.KissDick ||
                   mode == RoleplayPlaybackMode.TongueMove1 ||
                   mode == RoleplayPlaybackMode.TongueMove2;
        }

        private static int GetBlendIndex(
            RoleplayCategory category,
            int animationNumber)
        {
            return category == RoleplayCategory.Hand
                ? animationNumber - 1
                : animationNumber + 1;
        }

        private SkillType GetSkillForCurrentAction()
        {
            return currentCategory == RoleplayCategory.Hand
                ? SkillType.Hand
                : SkillType.Tongue;
        }
    }
}

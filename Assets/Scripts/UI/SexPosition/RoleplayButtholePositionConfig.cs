using System;
using Base.Character.Skills;
using UnityEngine;

namespace UI.SexPosition
{
    /// <summary>
    /// Runtime strategy for the Roleplay Butthole Live2D position.
    /// Blend-tree actions start at Slow, standalone Hand actions and Egg start
    /// immediately, and an inserted Egg is removed before another toy can play.
    /// </summary>
    [Serializable]
    public class RoleplayButtholePositionConfig : SexPositionConfig
    {
        private enum RoleplayPlaybackMode
        {
            Start,
            ReturningToStart,
            Slow,
            Fast,
            FingerMassage,
            HandGrabBoth,
            EggInsert,
            EggLoop,
            EggRemoving,
            Cumming1,
            Cumming2
        }

        private enum PendingPlaybackAction
        {
            None,
            Slow,
            Fast,
            FingerMassage,
            HandGrabBoth,
            Egg
        }

        private const string SlowLoop = "SlowLoop";
        private const string FastLoop = "FastLoop";
        private const string SlowValue = "SlowValue";
        private const string FastValue = "FastValue";
        private const string TriggerFingerMassage = "FingerMassage";
        private const string TriggerHandGrabBoth = "HandGrabBoth";
        private const string TriggerEgg = "Egg";
        private const string TriggerEggOut = "EggOut";
        private const string TriggerCum = "Cum";
        private const string TriggerStop = "Stop";
        private const string SkipCumming2 = "SkipCumming2";

        private RoleplayCategory currentCategory = RoleplayCategory.Hand;
        private int currentAnimationNumber;
        private int currentBlendIndex;
        private RoleplayPlaybackMode playbackMode = RoleplayPlaybackMode.Start;
        private PendingPlaybackAction pendingPlaybackAction = PendingPlaybackAction.None;
        private bool categoryLocked;
        private bool hasSelectedAnimation;

        public RoleplayButtholePositionConfig()
        {
            positionName = "Roleplay Butthole";
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
            pendingPlaybackAction = PendingPlaybackAction.None;
            categoryLocked = false;
            hasSelectedAnimation = false;

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            panel.PlayDefaultAnimation();
            panel.SetState(initialState);
            RefreshControls(panel);
        }

        public override int GetRoleplayAnimationButtonCount()
        {
            switch (currentCategory)
            {
                case RoleplayCategory.Hand:
                    return 4;
                case RoleplayCategory.Tongue:
                    return 2;
                case RoleplayCategory.SexToy:
                    return 3;
                default:
                    return 0;
            }
        }

        public override bool AreRoleplayCategoriesAvailable()
        {
            return !categoryLocked;
        }

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
            // Roleplay foreplay has no hole-selection step.
        }

        public override void OnRoleplayCategorySelected(
            SimulationNavigationPanel panel,
            RoleplayCategory category)
        {
            if (!CanSelectAction(panel) || categoryLocked)
            {
                return;
            }

            currentCategory = category;
            currentAnimationNumber = 0;
            currentBlendIndex = GetFirstBlendIndex(category);
            hasSelectedAnimation = false;
            categoryLocked = true;
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
                    $"[RoleplayButthole] Animation {animationNumber} is invalid for {currentCategory}.");
                return;
            }

            currentAnimationNumber = animationNumber;
            hasSelectedAnimation = true;

            switch (currentCategory)
            {
                case RoleplayCategory.Hand:
                    SelectHandAnimation(panel, animationNumber);
                    break;
                case RoleplayCategory.Tongue:
                    SelectBlendAnimation(panel, animationNumber + 1);
                    break;
                case RoleplayCategory.SexToy:
                    SelectToyAnimation(panel, animationNumber);
                    break;
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
                !IsCurrentActionBlend())
            {
                return;
            }

            BeginBlendLoop(panel, isFast);
        }

        public override void OnThrustStopped(SimulationNavigationPanel panel)
        {
            switch (playbackMode)
            {
                case RoleplayPlaybackMode.Cumming1:
                case RoleplayPlaybackMode.EggRemoving:
                    return;

                case RoleplayPlaybackMode.EggInsert:
                case RoleplayPlaybackMode.EggLoop:
                    BeginEggRemoval(
                        panel,
                        panel.CurrentState == SimulationState.AfterCumming,
                        PendingPlaybackAction.None);
                    return;

                case RoleplayPlaybackMode.Cumming2:
                    UnlockCategory();
                    RequestReturnToStart(panel, PendingPlaybackAction.None);
                    return;
            }

            UnlockCategory();
            RequestReturnToStart(panel, PendingPlaybackAction.None);
        }

        public override void OnCumDecision(
            SimulationNavigationPanel panel,
            CumDecisionType decision)
        {
            // Roleplay completion is driven by the player's cum bar.
        }

        public override void OnMaiOrgasm(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.EggInsert ||
                playbackMode == RoleplayPlaybackMode.EggLoop)
            {
                BeginEggRemoval(panel, false, PendingPlaybackAction.None);
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.Slow ||
                playbackMode == RoleplayPlaybackMode.Fast ||
                playbackMode == RoleplayPlaybackMode.FingerMassage ||
                playbackMode == RoleplayPlaybackMode.HandGrabBoth)
            {
                BeginCumming(panel);
            }
        }

        public override void OnPlayerCumReached(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            pendingPlaybackAction = PendingPlaybackAction.None;
            panel.SetState(SimulationState.AfterCumming);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        public override void OnAfterCummingEntered(SimulationNavigationPanel panel)
        {
            if (playbackMode != RoleplayPlaybackMode.Cumming1 &&
                playbackMode != RoleplayPlaybackMode.EggRemoving &&
                playbackMode != RoleplayPlaybackMode.Cumming2)
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            pendingPlaybackAction = PendingPlaybackAction.None;
            ClearLoopBools(panel);
            playbackMode = RoleplayPlaybackMode.Cumming2;
            panel.SetState(SimulationState.AfterCumming);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        public override void OnRoleplayStartEntered(SimulationNavigationPanel panel)
        {
            if (playbackMode != RoleplayPlaybackMode.Start &&
                playbackMode != RoleplayPlaybackMode.ReturningToStart &&
                playbackMode != RoleplayPlaybackMode.EggRemoving)
            {
                return;
            }

            PendingPlaybackAction action = pendingPlaybackAction;
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = RoleplayPlaybackMode.Start;

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            panel.SetState(SimulationState.Active);

            switch (action)
            {
                case PendingPlaybackAction.Slow:
                    BeginBlendLoop(panel, false);
                    break;
                case PendingPlaybackAction.Fast:
                    BeginBlendLoop(panel, true);
                    break;
                case PendingPlaybackAction.FingerMassage:
                    BeginStandalone(panel, RoleplayPlaybackMode.FingerMassage);
                    break;
                case PendingPlaybackAction.HandGrabBoth:
                    BeginStandalone(panel, RoleplayPlaybackMode.HandGrabBoth);
                    break;
                case PendingPlaybackAction.Egg:
                    BeginEggInsert(panel);
                    break;
                default:
                    ApplySpeedAvailability(panel);
                    break;
            }
        }

        public override void OnRoleplayEggLoopEntered(SimulationNavigationPanel panel)
        {
            if (playbackMode != RoleplayPlaybackMode.EggInsert)
            {
                return;
            }

            playbackMode = RoleplayPlaybackMode.EggLoop;
            panel.SetState(SimulationState.Thrusting);
            SexSimulationManager.Instance?.StartThrusting(SkillType.Hand, false);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        private void SelectHandAnimation(
            SimulationNavigationPanel panel,
            int animationNumber)
        {
            switch (animationNumber)
            {
                case 1:
                    SelectBlendAnimation(panel, 0);
                    break;
                case 2:
                    SelectBlendAnimation(panel, 1);
                    break;
                case 3:
                    SelectStandalone(panel, RoleplayPlaybackMode.FingerMassage);
                    break;
                case 4:
                    SelectStandalone(panel, RoleplayPlaybackMode.HandGrabBoth);
                    break;
            }
        }

        private void SelectToyAnimation(
            SimulationNavigationPanel panel,
            int animationNumber)
        {
            switch (animationNumber)
            {
                case 1: // Pen
                    SelectBlendAnimation(panel, 4);
                    break;
                case 2: // Egg
                    BeginEggInsert(panel);
                    break;
                case 3: // Cucumber
                    SelectBlendAnimation(panel, 5);
                    break;
            }
        }

        private void SelectBlendAnimation(
            SimulationNavigationPanel panel,
            int blendIndex)
        {
            currentBlendIndex = blendIndex;

            if (IsEggPlayback(playbackMode))
            {
                BeginEggRemoval(panel, true, PendingPlaybackAction.Slow);
                return;
            }

            BeginBlendLoop(panel, false);
        }

        private void SelectStandalone(
            SimulationNavigationPanel panel,
            RoleplayPlaybackMode targetMode)
        {
            PendingPlaybackAction action = targetMode == RoleplayPlaybackMode.FingerMassage
                ? PendingPlaybackAction.FingerMassage
                : PendingPlaybackAction.HandGrabBoth;

            if (IsEggPlayback(playbackMode))
            {
                BeginEggRemoval(panel, true, action);
                return;
            }

            BeginStandalone(panel, targetMode);
        }

        private void BeginBlendLoop(SimulationNavigationPanel panel, bool isFast)
        {
            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = isFast
                    ? PendingPlaybackAction.Fast
                    : PendingPlaybackAction.Slow;
                ApplySpeedAvailability(panel);
                return;
            }

            if (IsEggPlayback(playbackMode))
            {
                BeginEggRemoval(panel, true, PendingPlaybackAction.Slow);
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            pendingPlaybackAction = PendingPlaybackAction.None;

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

        private void BeginStandalone(
            SimulationNavigationPanel panel,
            RoleplayPlaybackMode targetMode)
        {
            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction =
                    targetMode == RoleplayPlaybackMode.FingerMassage
                        ? PendingPlaybackAction.FingerMassage
                        : PendingPlaybackAction.HandGrabBoth;
                ApplySpeedAvailability(panel);
                return;
            }

            if (playbackMode == targetMode)
            {
                ApplySpeedAvailability(panel);
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = targetMode;
            panel.SetState(SimulationState.Thrusting);

            panel.TriggerAnimation(
                targetMode == RoleplayPlaybackMode.FingerMassage
                    ? TriggerFingerMassage
                    : TriggerHandGrabBoth);
            SexSimulationManager.Instance?.StartThrusting(SkillType.Hand, false);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        private void BeginEggInsert(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = PendingPlaybackAction.Egg;
                panel.SetRoleplaySpeedAvailability(false, false);
                return;
            }

            if (IsEggPlayback(playbackMode))
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = RoleplayPlaybackMode.EggInsert;
            panel.SetState(SimulationState.Thrusting);
            panel.TriggerAnimation(TriggerEgg);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        private void BeginEggRemoval(
            SimulationNavigationPanel panel,
            bool skipCumming2,
            PendingPlaybackAction action)
        {
            if (!IsEggPlayback(playbackMode))
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            pendingPlaybackAction = action;
            playbackMode = RoleplayPlaybackMode.EggRemoving;
            panel.SetBool(SkipCumming2, skipCumming2);
            panel.SetState(SimulationState.Transitioning);
            panel.SetRoleplaySpeedAvailability(false, false);
            panel.TriggerAnimation(TriggerEggOut);
        }

        private void BeginCumming(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = RoleplayPlaybackMode.Cumming1;
            panel.SetState(SimulationState.Transitioning);
            panel.SetRoleplaySpeedAvailability(false, false);
            panel.TriggerAnimation(TriggerCum);
        }

        private void RequestReturnToStart(
            SimulationNavigationPanel panel,
            PendingPlaybackAction action)
        {
            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = action;
                ApplySpeedAvailability(panel);
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.Start)
            {
                pendingPlaybackAction = action;
                OnRoleplayStartEntered(panel);
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetBool(SkipCumming2, false);
            pendingPlaybackAction = action;
            playbackMode = RoleplayPlaybackMode.ReturningToStart;
            panel.SetState(SimulationState.Active);
            panel.TriggerAnimation(TriggerStop);
            ApplySpeedAvailability(panel);
        }

        private void UnlockCategory()
        {
            categoryLocked = false;
            hasSelectedAnimation = false;
            currentAnimationNumber = 0;
            pendingPlaybackAction = PendingPlaybackAction.None;
        }

        private static void ClearLoopBools(SimulationNavigationPanel panel)
        {
            panel.SetBool(SlowLoop, false);
            panel.SetBool(FastLoop, false);
        }

        private static void ResetTransientTriggers(SimulationNavigationPanel panel)
        {
            panel.ResetAnimationTrigger(TriggerFingerMassage);
            panel.ResetAnimationTrigger(TriggerHandGrabBoth);
            panel.ResetAnimationTrigger(TriggerEgg);
            panel.ResetAnimationTrigger(TriggerEggOut);
            panel.ResetAnimationTrigger(TriggerCum);
            panel.ResetAnimationTrigger(TriggerStop);
        }

        private void RefreshControls(SimulationNavigationPanel panel)
        {
            panel.RefreshRoleplayControls();
            ApplySpeedAvailability(panel);
        }

        private void ApplySpeedAvailability(SimulationNavigationPanel panel)
        {
            bool speedsAvailable =
                panel.CurrentState != SimulationState.Transitioning &&
                panel.CurrentState != SimulationState.AfterCumming &&
                categoryLocked &&
                hasSelectedAnimation &&
                IsCurrentActionBlend() &&
                (playbackMode == RoleplayPlaybackMode.Slow ||
                 playbackMode == RoleplayPlaybackMode.Fast);

            panel.SetRoleplaySpeedAvailability(speedsAvailable, speedsAvailable);
        }

        private bool CanSelectAction(SimulationNavigationPanel panel)
        {
            return panel.CurrentState != SimulationState.Transitioning &&
                   panel.CurrentState != SimulationState.AfterCumming &&
                   playbackMode != RoleplayPlaybackMode.Cumming1 &&
                   playbackMode != RoleplayPlaybackMode.Cumming2 &&
                   playbackMode != RoleplayPlaybackMode.EggRemoving;
        }

        private bool IsCurrentActionBlend()
        {
            if (currentCategory == RoleplayCategory.Hand)
            {
                return currentAnimationNumber == 1 || currentAnimationNumber == 2;
            }

            if (currentCategory == RoleplayCategory.Tongue)
            {
                return currentAnimationNumber == 1 || currentAnimationNumber == 2;
            }

            return currentCategory == RoleplayCategory.SexToy &&
                   (currentAnimationNumber == 1 || currentAnimationNumber == 3);
        }

        private static bool IsEggPlayback(RoleplayPlaybackMode mode)
        {
            return mode == RoleplayPlaybackMode.EggInsert ||
                   mode == RoleplayPlaybackMode.EggLoop;
        }

        private static int GetFirstBlendIndex(RoleplayCategory category)
        {
            switch (category)
            {
                case RoleplayCategory.Tongue:
                    return 2;
                case RoleplayCategory.SexToy:
                    return 4;
                default:
                    return 0;
            }
        }

        private SkillType GetSkillForCurrentAction()
        {
            return currentCategory == RoleplayCategory.Tongue
                ? SkillType.Tongue
                : SkillType.Hand;
        }
    }
}

using System;
using Base.Character.Skills;
using UnityEngine;

namespace UI.SexPosition
{
    /// <summary>
    /// Runtime strategy for the Roleplay Pussy Live2D position.
    /// Numbered buttons select an action while Slow/Fast control blend-tree actions.
    /// Hitachi and Egg Vib are standalone animator states started by their number buttons.
    /// </summary>
    [Serializable]
    public class RoleplayPussyPositionConfig : SexPositionConfig
    {
        private enum RoleplayPlaybackMode
        {
            Start,
            ReturningToStart,
            Slow,
            Fast,
            Hitachi,
            EggInserted,
            EggWorkPending,
            EggWork,
            Cumming,
            AfterCumming
        }

        private enum PendingPlaybackAction
        {
            None,
            Slow,
            Fast,
            Hitachi,
            EggVib,
            EggVibWork
        }

        private const string SlowLoop = "SlowLoop";
        private const string FastLoop = "FastLoop";
        private const string EggVibWorkLoop = "EggVibWorkLoop";
        private const string SlowValue = "SlowValue";
        private const string FastValue = "FastValue";
        private const string TriggerHitachi = "Hitachi";
        private const string TriggerEggVib = "EggVib";
        private const string TriggerStop = "Stop";
        private const string TriggerCum = "Cum";

        private const int CucumberBlendIndex = 6;
        private const int DildoBlendIndex = 7;

        private RoleplayCategory currentCategory = RoleplayCategory.Hand;
        private int currentAnimationNumber;
        private int currentBlendIndex;
        private RoleplayPlaybackMode playbackMode = RoleplayPlaybackMode.Start;
        private PendingPlaybackAction pendingPlaybackAction = PendingPlaybackAction.None;
        private bool categoryLocked;
        private bool hasSelectedAnimation;

        public RoleplayPussyPositionConfig()
        {
            positionName = "Roleplay Pussy";
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
            panel.PlayDefaultAnimation();
            panel.SetState(initialState);
            RefreshControls(panel);
        }

        public override int GetRoleplayAnimationButtonCount()
        {
            return currentCategory == RoleplayCategory.SexToy ? 4 : 3;
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

        public override void OnHoleSelected(SimulationNavigationPanel panel, HoleType holeType)
        {
            // Roleplay foreplay has no hole-selection step.
        }

        public override void OnRoleplayCategorySelected(
            SimulationNavigationPanel panel,
            RoleplayCategory category)
        {
            if (!CanSelectAction(panel))
            {
                return;
            }

            if (categoryLocked)
            {
                return;
            }

            bool wasBlendPlayback = IsBlendPlayback(playbackMode);
            bool wasStandalonePlayback = IsStandalonePlayback(playbackMode);

            currentCategory = category;
            currentAnimationNumber = 0;
            hasSelectedAnimation = false;

            switch (category)
            {
                case RoleplayCategory.Hand:
                    currentBlendIndex = 0;
                    break;
                case RoleplayCategory.Tongue:
                    currentBlendIndex = 3;
                    break;
                case RoleplayCategory.SexToy:
                    break;
                default:
                    Debug.LogWarning($"[RoleplayPussy] Unknown category: {category}");
                    return;
            }

            categoryLocked = true;

            // Category randomization/selection does not choose an inner action.
            // Cancel any queued target and acknowledge Start before accepting one.
            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = PendingPlaybackAction.None;
            }
            else if (category == RoleplayCategory.SexToy &&
                     playbackMode != RoleplayPlaybackMode.Start)
            {
                RequestReturnToStart(panel, PendingPlaybackAction.None);
            }
            else if (category != RoleplayCategory.SexToy && wasStandalonePlayback)
            {
                RequestReturnToStart(panel, PendingPlaybackAction.None);
            }
            else if (category != RoleplayCategory.SexToy && wasBlendPlayback)
            {
                UpdateActiveBlendValue(panel);
            }

            RefreshControls(panel);
        }

        public override void OnRoleplayAnimationSelected(
            SimulationNavigationPanel panel,
            int animationNumber)
        {
            if (!CanSelectAction(panel))
            {
                return;
            }

            if (!categoryLocked)
            {
                return;
            }

            int buttonCount = GetRoleplayAnimationButtonCount();
            if (animationNumber < 1 || animationNumber > buttonCount)
            {
                Debug.LogWarning(
                    $"[RoleplayPussy] Animation {animationNumber} is invalid for {currentCategory}.");
                return;
            }

            currentAnimationNumber = animationNumber;
            hasSelectedAnimation = true;

            switch (currentCategory)
            {
                case RoleplayCategory.Hand:
                case RoleplayCategory.Tongue:
                    SelectBlendAnimation(panel, GetBlendIndex(currentCategory, animationNumber));
                    break;

                case RoleplayCategory.SexToy:
                    SelectSexToyAnimation(panel, animationNumber);
                    break;
            }

            RefreshControls(panel);
        }

        public override void OnThrustStarted(SimulationNavigationPanel panel, bool isFast)
        {
            if (panel.CurrentState == SimulationState.Transitioning ||
                panel.CurrentState == SimulationState.AfterCumming ||
                !categoryLocked ||
                !hasSelectedAnimation)
            {
                return;
            }

            if (currentCategory != RoleplayCategory.SexToy)
            {
                StartBlendLoop(panel, isFast);
                return;
            }

            switch (currentAnimationNumber)
            {
                case 1: // Hitachi starts only from its numbered button.
                    return;

                case 2: // Egg Vib has a Slow work loop and no Fast variant.
                    if (!isFast)
                    {
                        StartEggWork(panel);
                    }
                    return;

                case 3: // Cucumber
                case 4: // Dildo (assets intentionally retain the legacy Didlo spelling)
                    StartBlendLoop(panel, isFast);
                    return;
            }
        }

        public override void OnThrustStopped(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.Cumming ||
                panel.CurrentState == SimulationState.Transitioning)
            {
                return;
            }

            categoryLocked = false;
            hasSelectedAnimation = false;
            currentAnimationNumber = 0;
            RequestReturnToStart(panel, PendingPlaybackAction.None);
            RefreshControls(panel);
        }

        public override void OnCumDecision(
            SimulationNavigationPanel panel,
            CumDecisionType decision)
        {
            // Roleplay orgasm routing is driven by Mai's orgasm event.
        }

        public override void OnMaiOrgasm(SimulationNavigationPanel panel)
        {
            BeginCumming(panel);
        }

        public override void OnPlayerCumReached(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            pendingPlaybackAction = PendingPlaybackAction.None;
            panel.SetState(SimulationState.AfterCumming);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        private void BeginCumming(SimulationNavigationPanel panel)
        {
            switch (playbackMode)
            {
                case RoleplayPlaybackMode.Slow:
                case RoleplayPlaybackMode.Fast:
                case RoleplayPlaybackMode.Hitachi:
                case RoleplayPlaybackMode.EggWork:
                    break;
                default:
                    return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            pendingPlaybackAction = PendingPlaybackAction.None;
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            playbackMode = RoleplayPlaybackMode.Cumming;
            panel.SetState(SimulationState.Transitioning);
            panel.SetRoleplaySpeedAvailability(false, false);
            panel.TriggerAnimation(TriggerCum);
        }

        public override void OnAfterCummingEntered(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            pendingPlaybackAction = PendingPlaybackAction.None;
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

            PendingPlaybackAction action = pendingPlaybackAction;
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = RoleplayPlaybackMode.Start;

            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            panel.SetState(SimulationState.Active);

            switch (action)
            {
                case PendingPlaybackAction.Slow:
                    BeginBlendLoop(panel, false);
                    break;
                case PendingPlaybackAction.Fast:
                    BeginBlendLoop(panel, true);
                    break;
                case PendingPlaybackAction.Hitachi:
                    BeginHitachi(panel);
                    break;
                case PendingPlaybackAction.EggVib:
                    BeginEggVib(panel, false);
                    break;
                case PendingPlaybackAction.EggVibWork:
                    BeginEggVib(panel, true);
                    break;
                default:
                    ApplySpeedAvailability(panel);
                    break;
            }
        }

        public override void OnRoleplayEggWorkEntered(SimulationNavigationPanel panel)
        {
            if (currentCategory != RoleplayCategory.SexToy ||
                currentAnimationNumber != 2 ||
                playbackMode != RoleplayPlaybackMode.EggWorkPending)
            {
                return;
            }

            playbackMode = RoleplayPlaybackMode.EggWork;
            panel.SetState(SimulationState.Thrusting);
            SexSimulationManager.Instance?.StartThrusting(SkillType.Hand, false);
            panel.SetRoleplaySpeedAvailability(true, false);
        }

        private void SelectBlendAnimation(
            SimulationNavigationPanel panel,
            int blendIndex)
        {
            currentBlendIndex = blendIndex;

            // Every numbered blend action starts immediately at Slow. Selecting a
            // different action while Fast is active therefore switches both the
            // blend index and the speed, while toy actions remain interchangeable.
            StartBlendLoop(panel, false);
        }

        private void SelectSexToyAnimation(SimulationNavigationPanel panel, int animationNumber)
        {
            switch (animationNumber)
            {
                case 1:
                    ActivateHitachi(panel);
                    break;

                case 2:
                    ActivateEggVib(panel);
                    break;

                case 3:
                    SelectBlendAnimation(panel, CucumberBlendIndex);
                    break;

                case 4:
                    SelectBlendAnimation(panel, DildoBlendIndex);
                    break;
            }
        }

        private void ActivateHitachi(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.Hitachi)
            {
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = PendingPlaybackAction.Hitachi;
                panel.SetRoleplaySpeedAvailability(false, false);
                return;
            }

            if (IsEggPlayback(playbackMode) ||
                playbackMode == RoleplayPlaybackMode.Start ||
                (IsBlendPlayback(playbackMode) &&
                 currentCategory == RoleplayCategory.SexToy))
            {
                BeginHitachi(panel);
                return;
            }

            RequestReturnToStart(panel, PendingPlaybackAction.Hitachi);
        }

        private void BeginHitachi(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = RoleplayPlaybackMode.Hitachi;
            panel.SetState(SimulationState.Thrusting);
            panel.TriggerAnimation(TriggerHitachi);
            SexSimulationManager.Instance?.StartThrusting(SkillType.Hand, false);
            panel.SetRoleplaySpeedAvailability(false, false);
        }

        private void ActivateEggVib(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.EggInserted ||
                playbackMode == RoleplayPlaybackMode.EggWorkPending ||
                playbackMode == RoleplayPlaybackMode.EggWork)
            {
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = PendingPlaybackAction.EggVib;
                panel.SetRoleplaySpeedAvailability(true, false);
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.Hitachi ||
                playbackMode == RoleplayPlaybackMode.Start ||
                (IsBlendPlayback(playbackMode) &&
                 currentCategory == RoleplayCategory.SexToy))
            {
                BeginEggVib(panel, false);
                return;
            }

            RequestReturnToStart(panel, PendingPlaybackAction.EggVib);
        }

        private void BeginEggVib(SimulationNavigationPanel panel, bool startWork)
        {
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
            pendingPlaybackAction = PendingPlaybackAction.None;
            playbackMode = startWork
                ? RoleplayPlaybackMode.EggWorkPending
                : RoleplayPlaybackMode.EggInserted;
            panel.SetState(SimulationState.Thrusting);
            panel.TriggerAnimation(TriggerEggVib);
            if (startWork)
            {
                panel.SetBool(EggVibWorkLoop, true);
            }
            panel.SetRoleplaySpeedAvailability(true, false);
        }

        private void StartEggWork(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.ReturningToStart &&
                (pendingPlaybackAction == PendingPlaybackAction.EggVib ||
                 pendingPlaybackAction == PendingPlaybackAction.EggVibWork))
            {
                pendingPlaybackAction = PendingPlaybackAction.EggVibWork;
                panel.SetRoleplaySpeedAvailability(true, false);
                return;
            }

            if (playbackMode == RoleplayPlaybackMode.EggWorkPending ||
                playbackMode == RoleplayPlaybackMode.EggWork)
            {
                return;
            }

            if (playbackMode != RoleplayPlaybackMode.EggInserted)
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            panel.SetBool(EggVibWorkLoop, true);
            playbackMode = RoleplayPlaybackMode.EggWorkPending;
            panel.SetState(SimulationState.Thrusting);
            panel.SetRoleplaySpeedAvailability(true, false);
        }

        private void StartBlendLoop(SimulationNavigationPanel panel, bool isFast)
        {
            PendingPlaybackAction action = isFast
                ? PendingPlaybackAction.Fast
                : PendingPlaybackAction.Slow;

            if (playbackMode == RoleplayPlaybackMode.ReturningToStart)
            {
                pendingPlaybackAction = action;
                ApplySpeedAvailability(panel);
                return;
            }

            if (IsStandalonePlayback(playbackMode))
            {
                if (currentCategory == RoleplayCategory.SexToy &&
                    (currentAnimationNumber == 3 || currentAnimationNumber == 4))
                {
                    BeginBlendLoop(panel, isFast);
                    return;
                }

                RequestReturnToStart(panel, action);
                return;
            }

            BeginBlendLoop(panel, isFast);
        }

        private void BeginBlendLoop(SimulationNavigationPanel panel, bool isFast)
        {
            SexSimulationManager.Instance?.StopThrusting();
            ClearLoopBools(panel);
            ResetTransientTriggers(panel);
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
            SexSimulationManager.Instance?.StartThrusting(GetSkillForCurrentAction(), isFast);
            ApplySpeedAvailability(panel);
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
            pendingPlaybackAction = action;
            playbackMode = RoleplayPlaybackMode.ReturningToStart;
            panel.SetState(SimulationState.Active);
            panel.TriggerAnimation(TriggerStop);
            ApplySpeedAvailability(panel);
        }

        private static void ClearLoopBools(SimulationNavigationPanel panel)
        {
            panel.SetBool(SlowLoop, false);
            panel.SetBool(FastLoop, false);
            panel.SetBool(EggVibWorkLoop, false);
        }

        private static void ResetTransientTriggers(SimulationNavigationPanel panel)
        {
            panel.ResetAnimationTrigger(TriggerStop);
            panel.ResetAnimationTrigger(TriggerHitachi);
            panel.ResetAnimationTrigger(TriggerEggVib);
            panel.ResetAnimationTrigger(TriggerCum);
        }

        private void UpdateActiveBlendValue(SimulationNavigationPanel panel)
        {
            if (playbackMode == RoleplayPlaybackMode.Slow)
            {
                panel.SetFloat(SlowValue, currentBlendIndex);
            }
            else if (playbackMode == RoleplayPlaybackMode.Fast)
            {
                panel.SetFloat(FastValue, currentBlendIndex);
            }
        }

        private void RefreshControls(SimulationNavigationPanel panel)
        {
            panel.RefreshRoleplayControls();
            ApplySpeedAvailability(panel);
        }

        private void ApplySpeedAvailability(SimulationNavigationPanel panel)
        {
            if (panel.CurrentState == SimulationState.Transitioning ||
                panel.CurrentState == SimulationState.AfterCumming)
            {
                panel.SetRoleplaySpeedAvailability(false, false);
                return;
            }

            if (!categoryLocked || !hasSelectedAnimation)
            {
                panel.SetRoleplaySpeedAvailability(false, false);
                return;
            }

            if (currentCategory == RoleplayCategory.Hand ||
                currentCategory == RoleplayCategory.Tongue)
            {
                panel.SetRoleplaySpeedAvailability(true, true);
                return;
            }

            switch (currentAnimationNumber)
            {
                case 2:
                    bool eggInserted = playbackMode == RoleplayPlaybackMode.EggInserted ||
                                       playbackMode == RoleplayPlaybackMode.EggWorkPending ||
                                       playbackMode == RoleplayPlaybackMode.EggWork ||
                                       (playbackMode == RoleplayPlaybackMode.ReturningToStart &&
                                        (pendingPlaybackAction == PendingPlaybackAction.EggVib ||
                                         pendingPlaybackAction == PendingPlaybackAction.EggVibWork));
                    panel.SetRoleplaySpeedAvailability(eggInserted, false);
                    break;
                case 3:
                case 4:
                    panel.SetRoleplaySpeedAvailability(true, true);
                    break;
                default:
                    panel.SetRoleplaySpeedAvailability(false, false);
                    break;
            }
        }

        private bool CanSelectAction(SimulationNavigationPanel panel)
        {
            return panel.CurrentState != SimulationState.Transitioning &&
                   panel.CurrentState != SimulationState.AfterCumming &&
                   playbackMode != RoleplayPlaybackMode.Cumming;
        }

        private static bool IsBlendPlayback(RoleplayPlaybackMode mode)
        {
            return mode == RoleplayPlaybackMode.Slow || mode == RoleplayPlaybackMode.Fast;
        }

        private static bool IsStandalonePlayback(RoleplayPlaybackMode mode)
        {
            return mode == RoleplayPlaybackMode.Hitachi ||
                   mode == RoleplayPlaybackMode.EggInserted ||
                   mode == RoleplayPlaybackMode.EggWorkPending ||
                   mode == RoleplayPlaybackMode.EggWork ||
                   mode == RoleplayPlaybackMode.AfterCumming;
        }

        private static bool IsEggPlayback(RoleplayPlaybackMode mode)
        {
            return mode == RoleplayPlaybackMode.EggInserted ||
                   mode == RoleplayPlaybackMode.EggWorkPending ||
                   mode == RoleplayPlaybackMode.EggWork;
        }

        private static int GetBlendIndex(RoleplayCategory category, int animationNumber)
        {
            if (category == RoleplayCategory.Hand)
            {
                switch (animationNumber)
                {
                    case 1: return 0; // Clit Finger
                    case 2: return 1; // 2 Finger
                    case 3: return 2; // Moc Cua
                }
            }
            else if (category == RoleplayCategory.Tongue)
            {
                switch (animationNumber)
                {
                    case 1: return 3; // Tongue Clit
                    case 2: return 4; // Tongue Lick
                    case 3: return 5; // Tongue Insert
                }
            }

            return 0;
        }

        private SkillType GetSkillForCurrentAction()
        {
            return currentCategory == RoleplayCategory.Tongue
                ? SkillType.Tongue
                : SkillType.Hand;
        }
    }
}

using System;
using Base;
using Base.Character;
using Base.Character.Skills;

namespace UI.SexPosition
{
    /// <summary>
    /// Vaginal-only Cowgirl flow. Animation state-entry signals own all clip
    /// completion boundaries while Tick only owns the ten-second idle timeout.
    /// </summary>
    [Serializable]
    public sealed class CowgirlPositionConfig : SexPositionConfig
    {
        public enum CowgirlPlaybackMode
        {
            Starting = 0,
            Idle = 1,
            InsertLeadIn = 2,
            InsertLoop = 3,
            Thrusting = 4,
            StopLeadIn = 5,
            WaitingLeadIn = 6,
            WaitingLoop = 7,
            SquirtingLeadIn = 8,
            SquirtingLoop = 9,
            CumDecision = 10,
            CumLeadIn = 11,
            CumLoop = 12,
            CumOutsideLeadIn = 13,
            PullOutLeadIn = 14,
            Transitioning = 15
        }

        public const float InactivityDelaySeconds = 10f;
        public const int WaitingLoopsBeforeAutoSlow = 2;

        private const string UseCondom = "UseCondom";
        private const string Thrusting = "Thrusting";
        private const string Fast = "Fast";
        private const string LewdLevelValue = "LewdLevelValue";

        private const string Insert = "Insert";
        private const string Stop = "Stop";
        private const string Cum = "Cum";
        private const string CumOutside = "CumOutside";
        private const string PullOut = "PullOut";
        private const string Waiting = "Waiting";
        private const string Squirting = "Squirting";

        private static readonly string[] TransientTriggers =
        {
            Insert,
            Stop,
            Cum,
            CumOutside,
            PullOut,
            Waiting,
            Squirting
        };

        private CowgirlPlaybackMode playbackMode;
        private float inactivityElapsed;
        private int waitingLoopsCompleted;
        private bool inactivityTimerRunning;
        private bool hasCurrentPenetration;
        private bool currentPenetrationUsesCondom;

        public CowgirlPlaybackMode PlaybackMode => playbackMode;
        public float InactivityElapsed => inactivityElapsed;
        public int WaitingLoopsCompleted => waitingLoopsCompleted;
        public bool CurrentPenetrationUsesCondom =>
            hasCurrentPenetration && currentPenetrationUsesCondom;

        public CowgirlPositionConfig()
        {
            positionName = "Cowgirl";
            initialState = SimulationState.Idle;
        }

        public static float GetLewdLevelValue(int lewdLevel)
        {
            if (lewdLevel <= 2)
            {
                return 1.5f;
            }

            return lewdLevel <= 4 ? 3.5f : 5f;
        }

        protected override void ConfigureStateButtonMap()
        {
            stateButtonMap[SimulationState.Idle] = new ButtonVisibilitySet
            {
                insertButton = true,
                finishButton = true
            };
            stateButtonMap[SimulationState.Selecting] = new ButtonVisibilitySet
            {
                pussyButton = true,
                finishButton = true
            };

            ButtonVisibilitySet playbackControls = new ButtonVisibilitySet
            {
                slowButton = true,
                fastButton = true,
                stopButton = true,
                finishButton = true
            };
            stateButtonMap[SimulationState.Active] = playbackControls;
            stateButtonMap[SimulationState.Thrusting] = playbackControls;
            stateButtonMap[SimulationState.CumDecision] = new ButtonVisibilitySet
            {
                outsideButton = true,
                finishButton = true
            };
            stateButtonMap[SimulationState.CumInside] = new ButtonVisibilitySet
            {
                pulloutButton = true,
                finishButton = true
            };
            stateButtonMap[SimulationState.Transitioning] = new ButtonVisibilitySet
            {
                finishButton = true
            };
        }

        public override ButtonVisibilitySet GetButtonVisibility(SimulationState state)
        {
            ButtonVisibilitySet visibility = base.GetButtonVisibility(state);
            if (state == SimulationState.CumDecision && CurrentPenetrationUsesCondom)
            {
                visibility.outsideButton = false;
            }

            // Cowgirl is vaginal-only even if a serialized map is changed later.
            visibility.buttholeButton = false;
            return visibility;
        }

        public override void OnPositionEntered(SimulationNavigationPanel panel)
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            manager?.StopThrusting();
            manager?.CancelOutsideButtonTimer();

            panel.ClearPendingThrust();
            panel.isPussySelected = true;
            panel.isInsertAnimationComplete = true;

            playbackMode = CowgirlPlaybackMode.Starting;
            inactivityElapsed = 0f;
            waitingLoopsCompleted = 0;
            inactivityTimerRunning = false;
            hasCurrentPenetration = false;
            currentPenetrationUsesCondom = false;

            foreach (string trigger in TransientTriggers)
            {
                panel.ResetAnimationTrigger(trigger);
            }

            panel.SetBool(UseCondom, manager?.IsCowgirlCondomActive ?? false);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetFloat(LewdLevelValue, GetCurrentLewdLevelValue());
            panel.PlayDefaultAnimation();
        }

        public override void OnPositionExited(SimulationNavigationPanel panel)
        {
            inactivityTimerRunning = false;
            playbackMode = CowgirlPlaybackMode.Transitioning;
            panel.ClearPendingThrust();
            SexSimulationManager.Instance?.CancelOutsideButtonTimer();
            SexSimulationManager.Instance?.StopThrusting();
        }

        public override void Tick(SimulationNavigationPanel panel, float deltaTime)
        {
            if (!inactivityTimerRunning || playbackMode != CowgirlPlaybackMode.Idle)
            {
                return;
            }

            inactivityElapsed += Math.Max(0f, deltaTime);
            if (inactivityElapsed < InactivityDelaySeconds)
            {
                return;
            }

            BeginWaiting(panel);
        }

        public override void OnInsertClicked(SimulationNavigationPanel panel)
        {
            if (playbackMode != CowgirlPlaybackMode.Idle &&
                playbackMode != CowgirlPlaybackMode.Starting)
            {
                return;
            }

            inactivityTimerRunning = false;
            panel.ClearPendingThrust();
            panel.SetState(SimulationState.Selecting);
            panel.ShowSimulationLive2D();
        }

        public override void OnHoleSelected(
            SimulationNavigationPanel panel,
            HoleType holeType)
        {
            // Deliberately reject anal input even if invoked outside the hidden UI.
            if (holeType != HoleType.Pussy || !panel.IsHoleUnlocked(HoleType.Pussy))
            {
                return;
            }

            BeginPenetrationSnapshot(panel);
            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.isPussySelected = true;
            panel.isInsertAnimationComplete = false;
            panel.SetState(SimulationState.Active);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetFloat(LewdLevelValue, GetCurrentLewdLevelValue());
            playbackMode = CowgirlPlaybackMode.InsertLeadIn;
            panel.TriggerAnimation(Insert);
        }

        public override void OnThrustStarted(
            SimulationNavigationPanel panel,
            bool isFast)
        {
            if (!panel.isInsertAnimationComplete)
            {
                panel.QueuePendingThrust(isFast);
                return;
            }

            if (!hasCurrentPenetration)
            {
                return;
            }

            inactivityTimerRunning = false;
            waitingLoopsCompleted = 0;
            SexSimulationManager.Instance?.StopThrusting();
            SexSimulationManager.Instance?.StartThrusting(SkillType.F, isFast);
            panel.SetState(SimulationState.Thrusting);
            panel.SetFloat(LewdLevelValue, GetCurrentLewdLevelValue());
            panel.SetBool(Fast, isFast);
            panel.SetBool(Thrusting, true);
            playbackMode = CowgirlPlaybackMode.Thrusting;
        }

        public override void OnThrustStopped(SimulationNavigationPanel panel)
        {
            if (!hasCurrentPenetration)
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            SexSimulationManager.Instance?.CancelOutsideButtonTimer();
            panel.ClearPendingThrust();
            panel.SetState(SimulationState.Transitioning);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            playbackMode = CowgirlPlaybackMode.StopLeadIn;
            panel.TriggerAnimation(Stop);
        }

        public override void OnPlayerCumReached(SimulationNavigationPanel panel)
        {
            if (!hasCurrentPenetration)
            {
                return;
            }

            inactivityTimerRunning = false;
            panel.ClearPendingThrust();
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetState(SimulationState.CumDecision);
            playbackMode = CowgirlPlaybackMode.CumDecision;
        }

        public override void OnMaiOrgasm(SimulationNavigationPanel panel)
        {
            // OnThrust raises Player Cum first. If both bars complete on the same
            // thrust, rewards still accrue in the manager but Cum keeps visual priority.
            if (!hasCurrentPenetration ||
                playbackMode == CowgirlPlaybackMode.CumDecision ||
                (SexSimulationManager.Instance?.GetPlayerCumBar() ?? 0f) >= 100f)
            {
                return;
            }

            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.SetState(SimulationState.Transitioning);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            playbackMode = CowgirlPlaybackMode.SquirtingLeadIn;
            panel.TriggerAnimation(Squirting);
        }

        public override void OnCumDecision(
            SimulationNavigationPanel panel,
            CumDecisionType decision)
        {
            if (!hasCurrentPenetration)
            {
                return;
            }

            if (decision == CumDecisionType.Outside && CurrentPenetrationUsesCondom)
            {
                return;
            }

            if (decision == CumDecisionType.Pullout &&
                panel.CurrentState != SimulationState.CumInside)
            {
                return;
            }

            SexSimulationManager.Instance?.CancelOutsideButtonTimer();
            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.SetState(SimulationState.Transitioning);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetFloat(LewdLevelValue, GetCurrentLewdLevelValue());

            switch (decision)
            {
                case CumDecisionType.Inside:
                    playbackMode = CowgirlPlaybackMode.CumLeadIn;
                    panel.TriggerAnimation(Cum);
                    break;
                case CumDecisionType.Outside:
                    playbackMode = CowgirlPlaybackMode.CumOutsideLeadIn;
                    panel.TriggerAnimation(CumOutside);
                    break;
                case CumDecisionType.Pullout:
                    playbackMode = CowgirlPlaybackMode.PullOutLeadIn;
                    panel.TriggerAnimation(PullOut);
                    break;
            }
        }

        public override void OnAnimationSignal(
            SimulationNavigationPanel panel,
            CowgirlAnimationSignal signal)
        {
            switch (signal)
            {
                case CowgirlAnimationSignal.StandardLoopEntered
                    when playbackMode == CowgirlPlaybackMode.Starting:
                    EnterIdle(panel);
                    break;

                case CowgirlAnimationSignal.InsertLoopEntered
                    when playbackMode == CowgirlPlaybackMode.InsertLeadIn:
                    playbackMode = CowgirlPlaybackMode.InsertLoop;
                    SexSimulationManager.Instance?.OnInsertComplete();
                    break;

                case CowgirlAnimationSignal.StopLoopEntered
                    when playbackMode == CowgirlPlaybackMode.StopLeadIn:
                    EndCurrentPenetration();
                    EnterIdle(panel);
                    break;

                case CowgirlAnimationSignal.WaitingLoopEntered
                    when playbackMode == CowgirlPlaybackMode.WaitingLeadIn:
                    playbackMode = CowgirlPlaybackMode.WaitingLoop;
                    waitingLoopsCompleted = 0;
                    panel.isInsertAnimationComplete = true;
                    panel.SetState(SimulationState.Active);
                    break;

                case CowgirlAnimationSignal.WaitingLoopCompleted
                    when playbackMode == CowgirlPlaybackMode.WaitingLoop:
                    waitingLoopsCompleted++;
                    if (waitingLoopsCompleted >= WaitingLoopsBeforeAutoSlow)
                    {
                        OnThrustStarted(panel, false);
                    }
                    break;

                case CowgirlAnimationSignal.SquirtingLoopEntered
                    when playbackMode == CowgirlPlaybackMode.SquirtingLeadIn:
                    playbackMode = CowgirlPlaybackMode.SquirtingLoop;
                    panel.SetState(SimulationState.Active);
                    break;

                case CowgirlAnimationSignal.CumLoopEntered
                    when playbackMode == CowgirlPlaybackMode.CumLeadIn:
                    playbackMode = CowgirlPlaybackMode.CumLoop;
                    SexSimulationManager.Instance?.OnCumInsideComplete(
                        CurrentPenetrationUsesCondom);
                    break;

                case CowgirlAnimationSignal.CumOutsideLoopEntered
                    when playbackMode == CowgirlPlaybackMode.CumOutsideLeadIn:
                    EndCurrentPenetration();
                    SexSimulationManager.Instance?.OnCumOutsideComplete();
                    EnterIdleIfBulletsRemain(panel);
                    break;

                case CowgirlAnimationSignal.PullOutLoopEntered
                    when playbackMode == CowgirlPlaybackMode.PullOutLeadIn:
                    EndCurrentPenetration();
                    SexSimulationManager.Instance?.OnPulloutComplete();
                    EnterIdleIfBulletsRemain(panel);
                    break;
            }
        }

        private void BeginWaiting(SimulationNavigationPanel panel)
        {
            BeginPenetrationSnapshot(panel);
            inactivityTimerRunning = false;
            waitingLoopsCompleted = 0;
            panel.ClearPendingThrust();
            panel.isInsertAnimationComplete = false;
            // Keep the idle Insert/Finish presentation during the Waiting lead-in.
            // The playback mode rejects Insert input until Waiting Loop exposes
            // Slow/Fast/Stop automatically.
            panel.SetState(SimulationState.Idle);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            playbackMode = CowgirlPlaybackMode.WaitingLeadIn;
            panel.TriggerAnimation(Waiting);
        }

        private void BeginPenetrationSnapshot(SimulationNavigationPanel panel)
        {
            hasCurrentPenetration = true;
            currentPenetrationUsesCondom =
                SexSimulationManager.Instance?.IsCowgirlCondomActive ?? false;
            inactivityTimerRunning = false;
            panel.SetBool(UseCondom, currentPenetrationUsesCondom);
        }

        private void EndCurrentPenetration()
        {
            hasCurrentPenetration = false;
            currentPenetrationUsesCondom = false;
        }

        private void EnterIdleIfBulletsRemain(SimulationNavigationPanel panel)
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null || manager.HasBulletsRemaining())
            {
                EnterIdle(panel);
            }
            else
            {
                inactivityTimerRunning = false;
                playbackMode = CowgirlPlaybackMode.Transitioning;
            }
        }

        private void EnterIdle(SimulationNavigationPanel panel)
        {
            playbackMode = CowgirlPlaybackMode.Idle;
            inactivityElapsed = 0f;
            waitingLoopsCompleted = 0;
            inactivityTimerRunning = true;
            panel.isInsertAnimationComplete = true;
            panel.SetBool(
                UseCondom,
                SexSimulationManager.Instance?.IsCowgirlCondomActive ?? false);
            panel.SetState(SimulationState.Idle);
        }

        private static float GetCurrentLewdLevelValue()
        {
            Target mai = GameManager.Instance?.DataManager?.GetCurrentBoss() as Target;
            return GetLewdLevelValue(mai?.GetLewdLevel() ?? 1);
        }
    }
}

using System;
using Base.Character.Skills;

namespace UI.SexPosition
{
    /// <summary>
    /// Configuration for the Missionary sex position. This class is the runtime
    /// source of truth for the controller's consolidated 13-parameter contract.
    /// </summary>
    [Serializable]
    public class MissionaryPositionConfig : SexPositionConfig
    {
        private const string IsButthole = "IsButthole";
        private const string UseCondom = "UseCondom";
        private const string Thrusting = "Thrusting";
        private const string Fast = "Fast";
        private const string SquirtingLoop = "SquirtingLoop";
        private const string SkillValue = "SkillValue";

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

        public MissionaryPositionConfig()
        {
            positionName = "Missionary";
            initialState = SimulationState.Idle;
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
                buttholeButton = true,
                finishButton = true
            };

            stateButtonMap[SimulationState.Active] = new ButtonVisibilitySet
            {
                slowButton = true,
                fastButton = true,
                stopButton = true,
                finishButton = true
            };

            stateButtonMap[SimulationState.Thrusting] = new ButtonVisibilitySet
            {
                slowButton = true,
                fastButton = true,
                stopButton = true,
                finishButton = true
            };

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

        public override void OnPositionEntered(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.isPussySelected = true;
            panel.isInsertAnimationComplete = true;

            foreach (string trigger in TransientTriggers)
            {
                panel.ResetAnimationTrigger(trigger);
            }

            panel.SetBool(IsButthole, false);
            panel.SetBool(UseCondom, false);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetBool(SquirtingLoop, false);
            panel.SetFloat(SkillValue, panel.GetSkillAnimationValue(SkillType.F));
            panel.PlayDefaultAnimation();
        }

        public override void OnInsertClicked(SimulationNavigationPanel panel)
        {
            panel.ClearPendingThrust();
            panel.SetState(SimulationState.Selecting);
            panel.ShowSimulationLive2D();
        }

        public override void OnHoleSelected(
            SimulationNavigationPanel panel,
            HoleType holeType)
        {
            if (!panel.IsHoleUnlocked(holeType))
            {
                return;
            }

            bool isButthole = holeType == HoleType.Butthole;
            SkillType holeSkill = isButthole ? SkillType.A : SkillType.F;

            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.isPussySelected = !isButthole;
            panel.isInsertAnimationComplete = false;
            panel.SetState(SimulationState.Active);

            panel.SetBool(IsButthole, isButthole);
            panel.SetBool(UseCondom, false);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetBool(SquirtingLoop, false);
            panel.SetFloat(SkillValue, panel.GetSkillAnimationValue(holeSkill));
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

            SkillType holeSkill = panel.isPussySelected ? SkillType.F : SkillType.A;
            SexSimulationManager.Instance?.StopThrusting();
            SexSimulationManager.Instance?.StartThrusting(holeSkill, isFast);

            panel.SetState(SimulationState.Thrusting);
            panel.SetFloat(SkillValue, panel.GetSkillAnimationValue(holeSkill));
            panel.SetBool(SquirtingLoop, false);
            panel.SetBool(Fast, isFast);
            panel.SetBool(Thrusting, true);
        }

        public override void OnThrustStopped(SimulationNavigationPanel panel)
        {
            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.SetState(SimulationState.Idle);
            panel.SetBool(Thrusting, false);
            panel.SetBool(Fast, false);
            panel.SetBool(SquirtingLoop, false);
            panel.TriggerAnimation(Stop);
        }

        public override void OnCumDecision(
            SimulationNavigationPanel panel,
            CumDecisionType decision)
        {
            SkillType holeSkill = panel.isPussySelected ? SkillType.F : SkillType.A;
            SexSimulationManager.Instance?.StopThrusting();
            panel.ClearPendingThrust();
            panel.SetBool(Thrusting, false);
            panel.SetBool(SquirtingLoop, false);
            panel.SetFloat(SkillValue, panel.GetSkillAnimationValue(holeSkill));

            switch (decision)
            {
                case CumDecisionType.Inside:
                    panel.SetState(SimulationState.Transitioning);
                    panel.TriggerAnimation(Cum);
                    break;

                case CumDecisionType.Outside:
                    panel.SetState(SimulationState.Transitioning);
                    panel.TriggerAnimation(CumOutside);
                    break;

                case CumDecisionType.Pullout:
                    panel.SetState(SimulationState.Transitioning);
                    panel.TriggerAnimation(PullOut);
                    break;
            }
        }
    }
}

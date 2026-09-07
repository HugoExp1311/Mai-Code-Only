using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.SexPosition
{
    /// <summary>
    /// Abstract base class for sex position configurations.
    /// Defines button visibility rules and position-specific behaviors.
    /// </summary>
    [Serializable]
    public abstract class SexPositionConfig
    {
        [SerializeField] protected string positionName;
        [SerializeField] protected SimulationState initialState = SimulationState.Idle;

        /// <summary>
        /// Maps simulation states to button visibility configurations
        /// </summary>
        protected Dictionary<SimulationState, ButtonVisibilitySet> stateButtonMap;

        /// <summary>
        /// Gets the button visibility configuration for a given state
        /// </summary>
        public virtual ButtonVisibilitySet GetButtonVisibility(SimulationState state)
        {
            if (stateButtonMap != null && stateButtonMap.ContainsKey(state))
            {
                return stateButtonMap[state];
            }
            return ButtonVisibilitySet.None;
        }

        /// <summary>
        /// Gets the initial state for this position
        /// </summary>
        public virtual SimulationState GetInitialState() => initialState;

        /// <summary>
        /// Gets the name of this position
        /// </summary>
        public string GetPositionName() => positionName;

        /// <summary>
        /// Initializes the position configuration
        /// </summary>
        public virtual void Initialize()
        {
            stateButtonMap = new Dictionary<SimulationState, ButtonVisibilitySet>();
            ConfigureStateButtonMap();
        }

        /// <summary>
        /// Called after the panel has selected this config and resolved its motion controller.
        /// </summary>
        public virtual void OnPositionEntered(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Called before the panel switches away from this position.
        /// </summary>
        public virtual void OnPositionExited(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Position-specific per-frame update hook.
        /// </summary>
        public virtual void Tick(SimulationNavigationPanel panel, float deltaTime) { }

        /// <summary>
        /// Receives position animation boundaries forwarded by the simulation manager.
        /// </summary>
        public virtual void OnAnimationSignal(
            SimulationNavigationPanel panel,
            CowgirlAnimationSignal signal) { }

        /// <summary>
        /// Configures the state-to-button-visibility mapping for this position
        /// </summary>
        protected abstract void ConfigureStateButtonMap();

        /// <summary>
        /// Called when the Insert button is clicked
        /// </summary>
        public abstract void OnInsertClicked(SimulationNavigationPanel panel);

        /// <summary>
        /// Called when a hole selection button is clicked
        /// </summary>
        public abstract void OnHoleSelected(SimulationNavigationPanel panel, HoleType holeType);

        /// <summary>
        /// Called when a thrust button (Slow/Fast) is clicked
        /// </summary>
        public abstract void OnThrustStarted(SimulationNavigationPanel panel, bool isFast);

        /// <summary>
        /// Called when the Stop button is clicked
        /// </summary>
        public abstract void OnThrustStopped(SimulationNavigationPanel panel);

        /// <summary>
        /// Called when a cum decision button is clicked
        /// </summary>
        public abstract void OnCumDecision(SimulationNavigationPanel panel, CumDecisionType decision);


        /// <summary>
        /// Called when a Roleplay category button (Hand / Tongue / Sex Toy) is clicked.
        /// Default is a no-op; Roleplay configs override this. This is the first level
        /// of the two-level foreplay selector.
        /// </summary>
        public virtual void OnRoleplayCategorySelected(SimulationNavigationPanel panel, RoleplayCategory category) { }

        /// <summary>
        /// Called when a Roleplay Animation button (1 / 2 / 3, plus 4 for Sex Toy) is clicked.
        /// Default is a no-op; Roleplay configs override this. This is the second level
        /// of the two-level foreplay selector — it picks the variant within the
        /// currently selected category.
        /// </summary>
        public virtual void OnRoleplayAnimationSelected(SimulationNavigationPanel panel, int animationNumber) { }

        /// <summary>
        /// Gets the number of numbered animation buttons available for the current selector.
        /// Non-roleplay positions do not expose numbered animation buttons.
        /// </summary>
        public virtual int GetRoleplayAnimationButtonCount() => 0;

        /// <summary>
        /// Whether the Roleplay category row may currently accept another selection.
        /// </summary>
        public virtual bool AreRoleplayCategoriesAvailable() => true;

        /// <summary>
        /// Maps one of the three physical Roleplay category buttons to the
        /// position-specific semantic category.
        /// </summary>
        public virtual RoleplayCategory GetRoleplayCategoryForButton(int buttonNumber)
        {
            return buttonNumber switch
            {
                1 => RoleplayCategory.Hand,
                2 => RoleplayCategory.Tongue,
                3 => RoleplayCategory.SexToy,
                _ => throw new ArgumentOutOfRangeException(nameof(buttonNumber))
            };
        }

        /// <summary>
        /// Gets the label displayed on a physical Roleplay category button.
        /// </summary>
        public virtual string GetRoleplayCategoryButtonLabel(int buttonNumber)
        {
            return buttonNumber switch
            {
                1 => "Hand",
                2 => "Tongue",
                3 => "Sex Toy",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Called when the player's cum bar completes during a Roleplay opener.
        /// </summary>
        public virtual void OnPlayerCumReached(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Called when Mai reaches orgasm (SexSimulationManager.OnMaiOrgasmReached).
        /// Default is a no-op; positions whose cum sequence is Mai-orgasm-driven
        /// (e.g. Roleplay Pussy) override this to trigger their cum animation.
        /// </summary>
        public virtual void OnMaiOrgasm(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Called when the animator enters a looping Roleplay after-cumming state.
        /// </summary>
        public virtual void OnAfterCummingEntered(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Called when the Roleplay Animator has actually entered its Start state.
        /// </summary>
        public virtual void OnRoleplayStartEntered(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Called when the Roleplay Egg Work loop has actually started.
        /// </summary>
        public virtual void OnRoleplayEggWorkEntered(SimulationNavigationPanel panel) { }

        /// <summary>
        /// Called when a Roleplay Egg insert loop has actually started.
        /// </summary>
        public virtual void OnRoleplayEggLoopEntered(SimulationNavigationPanel panel) { }
    }
}

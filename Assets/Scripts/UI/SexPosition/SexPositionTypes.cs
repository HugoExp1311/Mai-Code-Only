using System;

namespace UI.SexPosition
{
    /// <summary>
    /// Defines the simulation state for sex position interactions
    /// </summary>
    public enum SimulationState
    {
        Idle,           // Only Insert enabled
        Selecting,      // Pussy/Butthole enabled (after Insert clicked)
        Active,         // Slow/Fast/Stop enabled (after hole selected)
        Thrusting,      // Slow/Fast/Stop enabled (during thrusting)
        CumDecision,    // Outside button enabled (2 second window)
        CumInside,      // Pullout button enabled (after cum inside)
        Transitioning,  // No buttons enabled (during animations)
        AfterCumming,   // After-cumming loop; only Stop and Finish enabled
    }

    /// <summary>
    /// Defines the type of hole selection for sex positions
    /// </summary>
    public enum HoleType
    {
        Pussy,
        Butthole
    }

    /// <summary>
    /// Defines the type of cum decision the player can make
    /// </summary>
    public enum CumDecisionType
    {
        Inside,     // Cum inside (leads to pullout state)
        Outside,    // Cum outside (ends simulation)
        Pullout     // Pull out after cumming inside (ends simulation)
    }

    /// <summary>
    /// Signals emitted by Cowgirl Animator state-entry behaviours and guarded
    /// animation events. Gameplay waits for these authored animation boundaries
    /// instead of guessing clip durations in code.
    /// </summary>
    public enum CowgirlAnimationSignal
    {
        StandardLoopEntered = 0,
        InsertLoopEntered = 1,
        StopLoopEntered = 2,
        WaitingLoopEntered = 3,
        WaitingLoopCompleted = 4,
        SquirtingLoopEntered = 5,
        CumLoopEntered = 6,
        CumOutsideLoopEntered = 7,
        PullOutLoopEntered = 8
    }

    /// <summary>
    /// Defines available sex position types
    /// </summary>
    public enum SexPositionConfigType
    {
        Missionary = 0,
        Doggy = 1,          // Future implementation
        Cowgirl = 2,
        Standing = 3,       // Future implementation
        RoleplayPussy = 4,
        RoleplayButthole = 5,
        RoleplayBlowjob = 6,
        RoleplayPaizuri = 7
    }

    /// <summary>
    /// Roleplay Pussy foreplay category (first level of the two-level selector).
    /// The second level (Animation 1/2/3, plus 4 for Sex Toy) picks the variant within the category.
    /// </summary>
    public enum RoleplayCategory
    {
        Hand = 0,
        Tongue = 1,
        SexToy = 2,
        Blowjob = 3,
        Boob = 4
    }

    /// <summary>
    /// Defines which buttons should be visible for a given simulation state
    /// </summary>
    [Serializable]
    public struct ButtonVisibilitySet
    {
        public bool insertButton;
        public bool pussyButton;
        public bool buttholeButton;
        public bool slowButton;
        public bool fastButton;
        public bool stopButton;
        public bool outsideButton;
        public bool pulloutButton;
        public bool finishButton;

        // Roleplay Pussy two-level selector (category + Animation 1/2/3, plus 4 for Sex Toy).
        // Shown together while foreplay controls are active, hidden during cumming.
        public bool roleplaySelectorButtons;

        /// <summary>
        /// Returns a visibility set with all buttons hidden
        /// </summary>
        public static ButtonVisibilitySet None => new ButtonVisibilitySet();

        /// <summary>
        /// Returns a visibility set with all buttons visible
        /// </summary>
        public static ButtonVisibilitySet All => new ButtonVisibilitySet
        {
            insertButton = true,
            pussyButton = true,
            buttholeButton = true,
            slowButton = true,
            fastButton = true,
            stopButton = true,
            outsideButton = true,
            pulloutButton = true,
            finishButton = true,
            roleplaySelectorButtons = true
        };
    }
}

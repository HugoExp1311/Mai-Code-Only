using Base;
using Base.Persistence;
using Base.Character;
using Base.Character.Stats;
using Base.Settings;
using Base.SexScenes;
using Events;
using EventBus;
using System;
using System.Collections.Generic;
using System.Globalization;
using Base.Localization;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public CultureInfo CultureInfo => LocalizationManager.Instance.CurrentCulture;
    public Player Player => player;
    public DataManager DataManager => dataManager;
    public SettingsManager SettingsManager => settingsManager;

    public Area Area { get; private set; }
    public DateTime Time { get; private set; }
    public DayCycle Cycle { get; private set; }
    public int DaysPlayed => (Time - _initialGameStartDate).Days + 1;

    private DateTime _initialGameStartDate = new DateTime(2001, 1, 1, 0, 0, 0);

    private Player player;
    private DataManager dataManager;
    private SettingsManager settingsManager;

    // Pending area change for transition
    private (Area targetArea, TimeSpan travelDuration)? pendingAreaChange;
    
    // Event bindings
    private EventBinding<TransitionMidPointEvent> transitionMidPointBinding;

    private string playerName;
    
    // Intro CG tracking
    private bool isPlayingIntroCG = false;
    public bool IsPlayingIntroCG => isPlayingIntroCG;
    private bool suppressInGameUIForIntroTransition = false;
    private System.Action _activeIntroDialogueEndCallback;
    
    // Daily action tracking
    private DateTime lastEatingDate = DateTime.MinValue;
    private DateTime lastSexDate = DateTime.MinValue;
    private DateTime lastWorkingDate = DateTime.MinValue;
    
    // Talking is once-per-day-per-area
    private Dictionary<Area, DateTime> lastTalkingDateByArea = new Dictionary<Area, DateTime>();
    
    // Exercise tracking (once-per-day)
    private DateTime lastExerciseDate = DateTime.MinValue;
    
    // Sleep tracking (nap: max 2/day, deep sleep: max 1/day)
    private int napCountToday = 0;
    private int deepSleepCountToday = 0;
    private DateTime lastSleepDate = DateTime.MinValue;
    
    // Work Level (Progress) tracking
    private int workLevel = 1;
    private int workProgress = 0;



    void Awake()
    {
        Instance = this;
        settingsManager = new();
        InitializeDefaultValues();
    }

    void OnDestroy()
    {
        ClearIntroDialogueEndCallback();

        // Unsubscribe from section switch events
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnSectionSwitched.RemoveListener(HandleSectionSwitched);
        }
        
        // Unsubscribe from transition events
        if (transitionMidPointBinding != null)
        {
            EventBus<TransitionMidPointEvent>.Deregister(transitionMidPointBinding);
            transitionMidPointBinding = null;
        }

        if (Instance == this)
            Instance = null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeLocale();
        
        // Subscribe to section switch events to handle game start
        SubscribeToSectionSwitches();
        
        // Subscribe to transition events
        transitionMidPointBinding = new EventBinding<TransitionMidPointEvent>(OnTransitionMidPoint);
        EventBus<TransitionMidPointEvent>.Register(transitionMidPointBinding);
    }
    
    /// <summary>
    /// Subscribe to UIPanelManager section switch events
    /// Automatically starts game when switching from MainMenu to InGame
    /// </summary>
    private void SubscribeToSectionSwitches()
    {
        // Wait for UIPanelManager to be available (it might initialize after GameManager)
        StartCoroutine(SubscribeToSectionSwitchesDelayed());
    }
    
    private System.Collections.IEnumerator SubscribeToSectionSwitchesDelayed()
    {
        // Wait a frame to ensure UIPanelManager is initialized
        yield return null;
        
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnSectionSwitched.AddListener(HandleSectionSwitched);
        }
        else
        {
            Debug.LogWarning("[GameManager] UIPanelManager.Instance is null. Section switch listener not subscribed.");
        }
    }
    
    /// <summary>
    /// Handle section switch events
    /// Automatically starts game when switching from MainMenu to InGame
    /// </summary>
    private void HandleSectionSwitched(GameSection oldSection, GameSection newSection)
    {
        // When switching from MainMenu to CG, automatically start a new game (intro)
        if (oldSection == GameSection.MainMenu && newSection == GameSection.CG)
        {
            StartGame();
        }
        // Future: When save/load is implemented, we can check for load scenario here
        // For example: if (isLoadingGame) { LoadGame(saveData); } else { StartGame(); }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ValidatePlayerName(string playerName) => this.playerName = playerName;

    /// <summary>
    /// Start a new game - initializes game data, plays Intro CG, then transitions to InGame.
    /// UI management is handled by UIPanelManager.
    /// </summary>
    public void StartGame()
    {
        // Validate player name
        if (string.IsNullOrEmpty(playerName))
            playerName = "Player";

        // Create DataManager and initialize new player
        dataManager = ScriptableObject.CreateInstance<DataManager>();
        dataManager.OnCreateNewPlayer(playerName);

        // Assign player from DataManager
        player = dataManager.GetPlayer() as Player;

        // Play Intro CG dialogue before starting the game
        PlayIntroCG();
    }

    /// <summary>
    /// Debug tool: Skips the intro CG sequence entirely and jumps straight to gameplay.
    /// Cleans up any in-progress intro state, stops active dialogue, and raises GameStartEvent.
    /// </summary>
    public void SkipIntroCG()
    {
        // Disable the intro callback guard so any pending OnDialogueEnd callbacks are no-ops
        isPlayingIntroCG = false;
        currentIntroCGPhase = IntroCGPhase.None;
        _pendingDialogueEndCallback = null;
        
        // Stop any active dialogue (this may fire OnDialogueEnd, but isPlayingIntroCG=false guards it)
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndDialogue();
        }
        
        // Set game state to post-intro values
        Area = Area.Company;
        Time = new DateTime(2001, 1, 1, 7, 30, 0);
        Cycle = DayCycle.Morning;
        
        // Switch to InGame section
        UIPanelManager.Instance.SwitchToSection(GameSection.InGame);
        
        // Notify action buttons to update visibility for new area/time
        EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
        // Raise GameStartEvent to initialize all UI and systems
        EventBus<GameStartEvent>.Raise(new GameStartEvent { Area = Area, Time = Time, Cycle = Cycle });

        // Story milestone: intro skipped via debug tools, gameplay begins (devlog TODO #2).
        AutoQuickSave();
        
        Debug.Log("[GameManager] Intro CG skipped via debug tools. Game started at Company, 7:30 AM.");
    }
    
    /// <summary>
    /// Debug: Get current intro phase name for display
    /// </summary>
    public string GetCurrentIntroPhase()
    {
        return currentIntroCGPhase.ToString();
    }
    
    /// <summary>
    /// Debug: Check if intro is currently playing
    /// </summary>
    public bool GetIsPlayingIntro()
    {
        return isPlayingIntroCG;
    }

    /// <summary>
    /// Prevents InGame UI from restoring normal Home visuals while Intro Part 4
    /// is ending and the transition to Company is still covering the screen.
    /// </summary>
    public bool ShouldSuppressInGameUIForIntroTransition()
    {
        return suppressInGameUIForIntroTransition || (isPlayingIntroCG && currentIntroCGPhase == IntroCGPhase.Part4);
    }
    
    /// <summary>
    /// Debug: Clean up any active intro state, stop dialogue, prepare for a fresh phase start.
    /// Must be called before starting any intro phase from the debug tools.
    /// </summary>
    private void CleanupIntroState()
    {
        ClearIntroDialogueEndCallback();
        isPlayingIntroCG = false;
        currentIntroCGPhase = IntroCGPhase.None;
        suppressInGameUIForIntroTransition = false;
        _pendingDialogueEndCallback = null;
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.EndDialogue();
        }
    }

    private void SetIntroDialogueEndCallback(System.Action callback)
    {
        ClearIntroDialogueEndCallback();

        if (callback == null || DialogueManager.Instance == null)
            return;

        _activeIntroDialogueEndCallback = callback;
        DialogueManager.Instance.OnDialogueEnd += callback;
    }

    private void ClearIntroDialogueEndCallback()
    {
        if (_activeIntroDialogueEndCallback != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnd -= _activeIntroDialogueEndCallback;
        }

        _activeIntroDialogueEndCallback = null;
    }
    
    /// <summary>
    /// Debug: Ensure player/dataManager exist (creates them if starting mid-intro without StartGame)
    /// </summary>
    private void EnsurePlayerInitialized()
    {
        if (dataManager == null)
        {
            dataManager = ScriptableObject.CreateInstance<DataManager>();
            dataManager.OnCreateNewPlayer(string.IsNullOrEmpty(playerName) ? "Debug" : playerName);
            player = dataManager.GetPlayer() as Player;
        }
    }
    
    /// <summary>
    /// Debug tool: Skip directly to Intro Part 1 (CG: Dinner scene).
    /// Switches to CG section and starts Part1 dialogue with full intro callback chain.
    /// </summary>
    public void SkipToIntroPart1()
    {
        CleanupIntroState();
        EnsurePlayerInitialized();
        
        UIPanelManager.Instance.SwitchToSection(GameSection.CG);
        PlayIntroCG();
        
        Debug.Log("[DebugTools] Skipped to Intro Part 1 (CG: Dinner scene)");
    }
    
    /// <summary>
    /// Debug tool: Skip directly to Intro Part 2 (CG: Reminiscing).
    /// Switches to CG section and starts Part2 dialogue with continuation callback chain.
    /// </summary>
    public void SkipToIntroPart2()
    {
        CleanupIntroState();
        EnsurePlayerInitialized();
        
        // Build the callback chain starting from Part2
        isPlayingIntroCG = true;
        
        System.Action onDialogueEndCallback = null;
        onDialogueEndCallback = () =>
        {
            if (!isPlayingIntroCG) return;
            
            switch (currentIntroCGPhase)
            {
                case IntroCGPhase.Part2:
                    TransitionToSimulation(onDialogueEndCallback);
                    break;
                case IntroCGPhase.Part3:
                    TransitionToInGameWithPart4(onDialogueEndCallback);
                    break;
                case IntroCGPhase.Part4:
                    ClearIntroDialogueEndCallback();
                    isPlayingIntroCG = false;
                    currentIntroCGPhase = IntroCGPhase.None;
                    FinalizeIntroToCompany();
                    break;
            }
        };
        
        SetIntroDialogueEndCallback(onDialogueEndCallback);
        
        // Switch to CG and start Part 2
        UIPanelManager.Instance.SwitchToSection(GameSection.CG);
        
        var part2 = Resources.Load<Base.CG.CGDialogueSequenceSO>("Sequences/Intro/Intro_Part2");
        if (part2 == null)
        {
            Debug.LogError("[DebugTools] Failed to load Intro_Part2!");
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part2;
        DialogueManager.Instance.StartDialogue(part2);
        
        Debug.Log("[DebugTools] Skipped to Intro Part 2 (CG: Reminiscing)");
    }
    
    /// <summary>
    /// Debug tool: Skip directly to the Simulation section (Sex gameplay).
    /// Switches to Simulation section with continuation callback chain for Part3→Part4→game.
    /// </summary>
    public void SkipToIntroSimulation()
    {
        CleanupIntroState();
        EnsurePlayerInitialized();
        
        // Build the callback chain starting from Simulation end
        isPlayingIntroCG = true;
        currentIntroCGPhase = IntroCGPhase.Simulation;
        
        System.Action onDialogueEndCallback = null;
        onDialogueEndCallback = () =>
        {
            if (!isPlayingIntroCG) return;
            
            switch (currentIntroCGPhase)
            {
                case IntroCGPhase.Part3:
                    TransitionToInGameWithPart4(onDialogueEndCallback);
                    break;
                case IntroCGPhase.Part4:
                    ClearIntroDialogueEndCallback();
                    isPlayingIntroCG = false;
                    currentIntroCGPhase = IntroCGPhase.None;
                    FinalizeIntroToCompany();
                    break;
            }
        };
        
        SetIntroDialogueEndCallback(onDialogueEndCallback);
        _pendingDialogueEndCallback = onDialogueEndCallback;
        
        UIPanelManager.Instance.SwitchToSection(GameSection.Simulation);
        
        Debug.Log("[DebugTools] Skipped to Intro Simulation (Sex gameplay)");
    }
    
    /// <summary>
    /// Debug tool: Skip directly to Intro Part 3 (CG: Bedroom talk).
    /// Switches to CG section and starts Part3 dialogue with continuation callback chain.
    /// </summary>
    public void SkipToIntroPart3()
    {
        CleanupIntroState();
        EnsurePlayerInitialized();
        
        // Build the callback chain starting from Part3
        isPlayingIntroCG = true;
        
        System.Action onDialogueEndCallback = null;
        onDialogueEndCallback = () =>
        {
            if (!isPlayingIntroCG) return;
            
            switch (currentIntroCGPhase)
            {
                case IntroCGPhase.Part3:
                    TransitionToInGameWithPart4(onDialogueEndCallback);
                    break;
                case IntroCGPhase.Part4:
                    ClearIntroDialogueEndCallback();
                    isPlayingIntroCG = false;
                    currentIntroCGPhase = IntroCGPhase.None;
                    FinalizeIntroToCompany();
                    break;
            }
        };
        
        SetIntroDialogueEndCallback(onDialogueEndCallback);
        
        // Switch to CG and start Part 3
        UIPanelManager.Instance.SwitchToSection(GameSection.CG);
        
        var part3 = Resources.Load<Base.CG.CGDialogueSequenceSO>("Sequences/Intro/Intro_Part3");
        if (part3 == null)
        {
            Debug.LogError("[DebugTools] Failed to load Intro_Part3!");
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part3;
        DialogueManager.Instance.StartDialogue(part3);
        
        Debug.Log("[DebugTools] Skipped to Intro Part 3 (CG: Bedroom talk)");
    }
    
    /// <summary>
    /// Debug tool: Skip directly to Intro Part 4 (Live2D: Morning goodbye).
    /// Switches to InGame section and starts Part4 dialogue.
    /// </summary>
    public void SkipToIntroPart4()
    {
        CleanupIntroState();
        EnsurePlayerInitialized();
        
        // Build the callback chain for Part4 end
        isPlayingIntroCG = true;
        
        System.Action onDialogueEndCallback = null;
        onDialogueEndCallback = () =>
        {
            if (!isPlayingIntroCG) return;
            
            if (currentIntroCGPhase == IntroCGPhase.Part4)
            {
                ClearIntroDialogueEndCallback();
                isPlayingIntroCG = false;
                currentIntroCGPhase = IntroCGPhase.None;
                FinalizeIntroToCompany();
            }
        };
        
        SetIntroDialogueEndCallback(onDialogueEndCallback);
        
        var part4 = Resources.Load<Base.Dialogues.DialogueSequenceSO>("Sequences/Intro/Intro_Part4");
        if (part4 == null)
        {
            Debug.LogError("[DebugTools] Failed to load Intro_Part4!");
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part4;
        
        // Set game state for Part4 (Home, morning)
        Time = new DateTime(2001, 1, 1, 7, 0, 0);
        Cycle = DayCycle.Morning;
        Area = Area.Home;
        
        // Switch to InGame section, then start dialogue
        UnityEngine.Events.UnityAction<GameSection, GameSection> sectionCallback = null;
        sectionCallback = (oldSection, newSection) =>
        {
            UIPanelManager.Instance.OnSectionSwitched.RemoveListener(sectionCallback);
            
            EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
            DialogueManager.Instance.StartDialogue(part4);
        };
        
        UIPanelManager.Instance.OnSectionSwitched.AddListener(sectionCallback);
        UIPanelManager.Instance.SwitchToSection(GameSection.InGame);
        
        Debug.Log("[DebugTools] Skipped to Intro Part 4 (Live2D: Morning goodbye)");
    }
    
    /// <summary>
    /// Shared helper: Finalize intro by transitioning to Company at 7:30 AM.
    /// Used by debug skip methods.
    /// </summary>
    private void FinalizeIntroToCompany()
    {
        suppressInGameUIForIntroTransition = true;

        if (TransitionPanel.Instance != null)
        {
            EventBinding<TransitionMidPointEvent> midPoint = null;
            midPoint = new EventBinding<TransitionMidPointEvent>((evt) =>
            {
                EventBus<TransitionMidPointEvent>.Deregister(midPoint);

                suppressInGameUIForIntroTransition = false;
                ApplyIntroCompanyStartState();
            });
            EventBus<TransitionMidPointEvent>.Register(midPoint);
            EventBus<TransitionRequestEvent>.Raise(new TransitionRequestEvent());
        }
        else
        {
            suppressInGameUIForIntroTransition = false;
            ApplyIntroCompanyStartState();
        }
    }

    private void ApplyIntroCompanyStartState()
    {
        Area = Area.Company;
        Time = new DateTime(2001, 1, 1, 7, 30, 0);
        Cycle = DayCycle.Morning;

        EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
        EventBus<GameStartEvent>.Raise(new GameStartEvent { Area = Area, Time = Time, Cycle = Cycle });

        // Story milestone: intro finished, gameplay begins (devlog TODO #2).
        AutoQuickSave();
    }
    
    /// <summary>
    /// Tracks which phase of the intro CG sequence we're in
    /// </summary>
    private enum IntroCGPhase
    {
        None,
        Part1,       // CG: Intro (dinner scene)
        Part2,       // CG: Intro_Part2 (reminiscing)
        Simulation,  // Simulation gameplay
        Part3,       // CG: Intro_Part3 (bedroom talk)
        Part4         // Live2D: Intro_Part4 (morning goodbye)
    }
    
    private IntroCGPhase currentIntroCGPhase = IntroCGPhase.None;
    
    /// <summary>
    /// Play the Intro CG dialogue sequence (4-part flow).
    /// Part1 (CG) → Part2 (CG) → Simulation → Part3 (CG) → InGame + Part4 (Live2D)
    /// </summary>
    private void PlayIntroCG()
    {
        // Load the Intro CG dialogue sequence (Part 1)
        Base.CG.CGDialogueSequenceSO introCG = Resources.Load<Base.CG.CGDialogueSequenceSO>("Sequences/Intro/Intro");
        
        if (introCG == null)
        {
            Debug.LogError("[GameManager] Failed to load Intro CG dialogue! Starting game without intro.");
            EventBus<GameStartEvent>.Raise(new GameStartEvent { Area = Area, Time = Time, Cycle = Cycle });
            AutoQuickSave(); // game still started — keep the milestone save in sync
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part1;
        isPlayingIntroCG = true;
        
        // Subscribe to dialogue end event to handle phase transitions
        System.Action onDialogueEndCallback = null;
        onDialogueEndCallback = () =>
        {
            if (!isPlayingIntroCG) return;

            switch (currentIntroCGPhase)
            {
                case IntroCGPhase.Part1:
                    // Part1 ended → Start Part2 (still in CG section)
                    // Don't unsubscribe - callback will handle Part2 end
                    StartIntroPart2(onDialogueEndCallback);
                    break;
                    
                case IntroCGPhase.Part2:
                    // Part2 ended → Transition to Simulation
                    // Don't unsubscribe - callback will handle Part3 end after simulation
                    TransitionToSimulation(onDialogueEndCallback);
                    break;
                    
                case IntroCGPhase.Part3:
                    // Part3 ended → Transition to InGame + start Part4 (Live2D)
                    // Don't unsubscribe - callback will handle Part4 end
                    TransitionToInGameWithPart4(onDialogueEndCallback);
                    break;
                    
                case IntroCGPhase.Part4:
                    // Part4 ended → Intro is complete, start the actual game
                    ClearIntroDialogueEndCallback();
                    isPlayingIntroCG = false;
                    currentIntroCGPhase = IntroCGPhase.None;
                    FinalizeIntroToCompany();
                    break;
            }
        };
        
        SetIntroDialogueEndCallback(onDialogueEndCallback);
        
        // Start Part 1 (already in CG section from button)
        DialogueManager.Instance.StartDialogue(introCG);
    }
    
    /// <summary>
    /// Start Intro Part 2 (CG) - continues in CG section
    /// </summary>
    private void StartIntroPart2(System.Action onDialogueEndCallback)
    {
        Base.CG.CGDialogueSequenceSO part2 = Resources.Load<Base.CG.CGDialogueSequenceSO>("Sequences/Intro/Intro_Part2");
        
        if (part2 == null)
        {
            Debug.LogError("[GameManager] Failed to load Intro Part2 CG! Skipping to simulation.");
            TransitionToSimulation(onDialogueEndCallback);
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part2;
        
        // Don't re-subscribe - callback is already subscribed from PlayIntroCG
        // Use StartDialogueContinuation to avoid fade-in animation and black background reset
        // since this is a direct continuation of Part1 (EndDialogue already cleaned up state)
        DialogueManager.Instance.StartDialogueContinuation(part2);
    }
    
    /// <summary>
    /// Transition to Simulation section after Part2 CG ends.
    /// Call OnSimulationFinished() when simulation gameplay is done.
    /// </summary>
    private void TransitionToSimulation(System.Action onDialogueEndCallback)
    {
        currentIntroCGPhase = IntroCGPhase.Simulation;
        
        // Store callback for use after simulation finishes
        _pendingDialogueEndCallback = onDialogueEndCallback;
        UIPanelManager.Instance.SwitchToSection(GameSection.Simulation);
    }
    
    private System.Action _pendingDialogueEndCallback;
    
    /// <summary>
    /// Call this method when the simulation gameplay is finished.
    /// Transitions back to CG section and starts Part 3.
    /// </summary>
    public void OnSimulationFinished()
    {
        if (currentIntroCGPhase != IntroCGPhase.Simulation)
        {
            Debug.LogWarning("[GameManager] OnSimulationFinished called but not in Simulation phase.");
            return;
        }
        
        Base.CG.CGDialogueSequenceSO part3 = Resources.Load<Base.CG.CGDialogueSequenceSO>("Sequences/Intro/Intro_Part3");
        
        if (part3 == null)
        {
            Debug.LogError("[GameManager] Failed to load Intro Part3 CG! Skipping to InGame.");
            TransitionToInGameWithPart4(_pendingDialogueEndCallback);
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part3;
        
        // Defer dialogue start to OnSectionSwitched (fires at mid-point, screen is black)
        // This prevents the dialogue box from flashing before the fade transition
        UnityEngine.Events.UnityAction<GameSection, GameSection> sectionCallback = null;
        sectionCallback = (oldSection, newSection) =>
        {
            UIPanelManager.Instance.OnSectionSwitched.RemoveListener(sectionCallback);
            
            // Start Part3 dialogue now that screen is black
            // Note: onDialogueEndCallback is already subscribed from PlayIntroCG(), don't re-subscribe
            DialogueManager.Instance.StartDialogue(part3);
        };
        
        UIPanelManager.Instance.OnSectionSwitched.AddListener(sectionCallback);
        UIPanelManager.Instance.SwitchToSection(GameSection.CG);
    }
    
    /// <summary>
    /// Transition to InGame section after Part3 CG ends.
    /// Sets time to 7:00 AM Monday 01/01/2001, then starts Part4 Live2D dialogue.
    /// </summary>
    private void TransitionToInGameWithPart4(System.Action onDialogueEndCallback)
    {
        // Load Part 4 (Live2D dialogue)
        Base.Dialogues.DialogueSequenceSO part4 = Resources.Load<Base.Dialogues.DialogueSequenceSO>("Sequences/Intro/Intro_Part4");
        
        if (part4 == null)
        {
            Debug.LogError("[GameManager] Failed to load Intro Part4 Live2D dialogue! Starting game without it.");
            isPlayingIntroCG = false;
            currentIntroCGPhase = IntroCGPhase.None;
            
            // Set game state before switching section (fallback path)
            Time = new DateTime(2001, 1, 1, 7, 0, 0);
            Cycle = DayCycle.Morning;
            Area = Area.Home;
            
            UIPanelManager.Instance.SwitchToSection(GameSection.InGame);
            EventBus<GameStartEvent>.Raise(new GameStartEvent { Area = Area, Time = Time, Cycle = Cycle });
            AutoQuickSave(); // game still started — keep the milestone save in sync
            return;
        }
        
        currentIntroCGPhase = IntroCGPhase.Part4;

        // Preload the Part 4 game state before switching sections so InGame base panels
        // initialize against the Home/morning state instead of any stale previous area.
        // Defer the PlaceChanged event itself until after the section switch completes.
        Time = new DateTime(2001, 1, 1, 7, 0, 0);
        Cycle = DayCycle.Morning;
        Area = Area.Home;
        
        // Defer the PlaceChanged event and dialogue start to OnSectionSwitched (fires at mid-point, screen is black)
        // This prevents Mai's Live2D from being visible before the fade transition.
        UnityEngine.Events.UnityAction<GameSection, GameSection> sectionCallback = null;
        sectionCallback = (oldSection, newSection) =>
        {
            UIPanelManager.Instance.OnSectionSwitched.RemoveListener(sectionCallback);
            
            // Notify UI panels to update time/day/area displays
            EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
            
            // Start Part 4 Live2D dialogue (uses InGame background and Live2D)
            // Note: onDialogueEndCallback is already subscribed from PlayIntroCG(), don't re-subscribe
            DialogueManager.Instance.StartDialogue(part4);
        };
        
        UIPanelManager.Instance.OnSectionSwitched.AddListener(sectionCallback);
        UIPanelManager.Instance.SwitchToSection(GameSection.InGame);
    }

    /// <summary>Saves the current game to a numbered manual slot (1..3).</summary>
    public bool SaveGame(int slot = 1)
    {
        if (player == null || dataManager == null)
        {
            Debug.LogWarning("[GameManager] Cannot save before a game has been initialized.");
            return false;
        }

        string slotId = Mathf.Clamp(slot, 1, SaveLoadService.SlotCount).ToString();
        SaveLoadService.Write(slotId, CaptureSaveData(), $"Day {DaysPlayed} - {Time:HH:mm}");
        Debug.Log($"[GameManager] Saved game to slot {slotId}.");
        return true;
    }

    /// <summary>Writes the current game state to the automatic quick-save slot.</summary>
    public bool QuickSave()
    {
        if (player == null || dataManager == null) return false;
        SaveLoadService.Write(SaveLoadService.QuickSaveSlotId, CaptureSaveData(), $"Day {DaysPlayed} - {Time:HH:mm}");
        return true;
    }

    /// <summary>
    /// Story-milestone auto save (devlog save_load_25aug TODO #2, GDD §3 Quick Save:
    /// "Auto Save mỗi khi người chơi đã chơi đến 1 phân cảnh cụ thể đã unlock trong cốt truyện").
    /// Called when the Opening CG finishes and by future Ending flows (P1.2).
    /// </summary>
    public bool AutoQuickSave()
    {
        bool saved = QuickSave();
        if (saved)
        {
            Debug.Log($"[GameManager] Auto quick save at story milestone (Day {DaysPlayed}, {Time:HH:mm}).");
        }
        return saved;
    }

    /// <summary>
    /// Daily auto-save (one per in-game day). Writes to a rolling pool of
    /// <see cref="SaveLoadService.AutoSlotCount"/> slots named "AutoSave_1..N".
    /// The slot is chosen by DaysPlayed modulo AutoSlotCount, so the oldest
    /// daily save is overwritten while the other two remain. The save label
    /// starts with "AutoSave" so the UI can distinguish auto-saves from
    /// manual saves.
    /// </summary>
    public bool DailyAutoSave()
    {
        if (player == null || dataManager == null)
        {
            Debug.LogWarning("[GameManager] Cannot auto-save before a game has been initialized.");
            return false;
        }

        string slotId = SaveLoadService.GetAutoSlotIdForDay(DaysPlayed);
        SaveLoadService.Write(slotId, CaptureSaveData(), $"AutoSave Day {DaysPlayed} - {Time:HH:mm}");
        Debug.Log($"[GameManager] Daily auto save to slot '{slotId}' (Day {DaysPlayed}, {Time:HH:mm}).");
        return true;
    }

    /// <summary>Loads an auto-save slot by 1-based index (1..AutoSlotCount).</summary>
    public bool LoadAutoSave(int index)
    {
        int clamped = Mathf.Clamp(index, 1, SaveLoadService.AutoSlotCount);
        return LoadGame(SaveLoadService.Read(SaveLoadService.GetAutoSlotId(clamped)));
    }

    public bool HasSaveGame(int slot) => SaveLoadService.HasSave(Mathf.Clamp(slot, 1, SaveLoadService.SlotCount).ToString());

    public bool LoadGame(int slot)
    {
        return LoadGame(SaveLoadService.Read(Mathf.Clamp(slot, 1, SaveLoadService.SlotCount).ToString()));
    }

    /// <summary>Loads the automatic quick-save slot.</summary>
    public bool LoadQuickSave()
    {
        return LoadGame(SaveLoadService.Read(SaveLoadService.QuickSaveSlotId));
    }

    /// <summary>Restores a typed save snapshot and opens the in-game section without replaying the intro.</summary>
    public bool LoadGame(SaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogWarning("[GameManager] Requested save slot is empty or invalid.");
            return false;
        }

        playerName = string.IsNullOrEmpty(saveData.playerName) ? "Player" : saveData.playerName;
        dataManager = ScriptableObject.CreateInstance<DataManager>();
        dataManager.OnCreateNewPlayer(playerName);
        player = dataManager.GetPlayer() as Player;

        var playerState = new Player.PlayerState
        {
            stamina = saveData.stamina,
            maxStamina = saveData.maxStamina,
            charming = saveData.charming,
            knowledge = saveData.knowledge,
            money = saveData.money,
            skillPoint = saveData.skillPoint
        };
        foreach (var skill in saveData.skills)
            playerState.skills.Add(new Player.SkillStateEntry { skillType = skill.skillType, level = skill.level, unlocked = skill.unlocked });
        foreach (var item in saveData.inventory)
            playerState.inventory.Add(new Player.InventoryStateEntry { id = item.id, amount = item.amount, price = item.price, type = item.type, value = item.value });
        player.RestoreState(playerState);

        var maiState = new Target.MaiState
        {
            love = saveData.love,
            libido = saveData.libido,
            lewdLevel = saveData.lewdLevel,
            pregnancyChance = saveData.pregnancyChance
        };
        foreach (var part in saveData.sensitiveParts)
        {
            switch ((SensitiveBodyPart)part.bodyPart)
            {
                case SensitiveBodyPart.Boobs: maiState.boobsSensitivePoints = part.points; maiState.boobsSensitiveLevel = part.level; break;
                case SensitiveBodyPart.Mouth: maiState.mouthSensitivePoints = part.points; maiState.mouthSensitiveLevel = part.level; break;
                case SensitiveBodyPart.Pussy: maiState.pussySensitivePoints = part.points; maiState.pussySensitiveLevel = part.level; break;
                case SensitiveBodyPart.Butthole: maiState.buttholeSensitivePoints = part.points; maiState.buttholeSensitiveLevel = part.level; break;
            }
        }
        (dataManager.GetCurrentBoss() as Target).RestoreState(maiState);

        try { Time = DateTime.Parse(saveData.time, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); }
        catch { Time = new DateTime(2001, 1, 1, 6, 0, 0); }
        Area = Enum.IsDefined(typeof(Area), saveData.area) ? (Area)saveData.area : Area.Home;
        Cycle = Enum.IsDefined(typeof(DayCycle), saveData.cycle) ? (DayCycle)saveData.cycle : GetCurrentDayCycle(Time);
        workLevel = Mathf.Clamp(saveData.workLevel, 1, DefaultSettings.MaxWorkLevel);
        workProgress = Mathf.Max(0, saveData.workProgress);

        // Sex scene unlocks — write persisted flags back to PlayerPrefs.
        // Empty list means legacy save; keep current flags untouched.
        if (saveData.unlockedSexScenes != null && saveData.unlockedSexScenes.Count > 0)
        {
            foreach (var definition in SexSceneUnlockService.GetDefinitions())
            {
                SexSceneUnlockService.SetUnlocked(definition.Key, saveData.unlockedSexScenes.Contains(definition.Key.ToString()));
            }
        }
        lastEatingDate = DeserializeDate(saveData.lastEatingDate);
        lastSexDate = DeserializeDate(saveData.lastSexDate);
        lastWorkingDate = DeserializeDate(saveData.lastWorkingDate);
        lastExerciseDate = DeserializeDate(saveData.lastExerciseDate);
        lastSleepDate = DeserializeDate(saveData.lastSleepDate);
        napCountToday = Mathf.Max(0, saveData.napCountToday);
        deepSleepCountToday = Mathf.Max(0, saveData.deepSleepCountToday);
        isPlayingIntroCG = false;

        EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
        EventBus<TimeChangedEvent>.Raise(new TimeChangedEvent { Area = Area, Time = Time, Cycle = Cycle, PreviousCycle = Cycle });
        UIPanelManager.Instance?.SwitchToSection(GameSection.InGame);
        Debug.Log($"[GameManager] Loaded Day {DaysPlayed}, {Time:HH:mm}, {Area}.");
        return true;
    }

    public SaveData CaptureSaveData()
    {
        Player.PlayerState playerState = player.CaptureState();
        Target mai = dataManager.GetCurrentBoss() as Target;
        Target.MaiState maiState = mai.CaptureState();
        var data = new SaveData
        {
            time = Time.ToString("O", CultureInfo.InvariantCulture),
            area = (int)Area,
            cycle = (int)Cycle,
            playerName = player.GetName(),
            stamina = playerState.stamina,
            maxStamina = playerState.maxStamina,
            charming = playerState.charming,
            knowledge = playerState.knowledge,
            money = playerState.money,
            skillPoint = playerState.skillPoint,
            love = maiState.love,
            libido = maiState.libido,
            lewdLevel = maiState.lewdLevel,
            pregnancyChance = maiState.pregnancyChance,
            workLevel = workLevel,
            workProgress = workProgress,
            lastEatingDate = SerializeDate(lastEatingDate),
            lastSexDate = SerializeDate(lastSexDate),
            lastWorkingDate = SerializeDate(lastWorkingDate),
            lastExerciseDate = SerializeDate(lastExerciseDate),
            lastSleepDate = SerializeDate(lastSleepDate),
            napCountToday = napCountToday,
            deepSleepCountToday = deepSleepCountToday
        };

        foreach (var entry in playerState.skills)
            data.skills.Add(new SaveData.SkillSaveData { skillType = entry.skillType, level = entry.level, unlocked = entry.unlocked });
        foreach (var entry in playerState.inventory)
            data.inventory.Add(new SaveData.InventoryItemSaveData { id = entry.id, amount = entry.amount, price = entry.price, type = entry.type, value = entry.value });
        foreach (var definition in SexSceneUnlockService.GetDefinitions())
        {
            if (SexSceneUnlockService.IsUnlocked(definition.Key))
                data.unlockedSexScenes.Add(definition.Key.ToString());
        }
        AddSensitivePart(data, SensitiveBodyPart.Boobs, maiState.boobsSensitivePoints, maiState.boobsSensitiveLevel);
        AddSensitivePart(data, SensitiveBodyPart.Mouth, maiState.mouthSensitivePoints, maiState.mouthSensitiveLevel);
        AddSensitivePart(data, SensitiveBodyPart.Pussy, maiState.pussySensitivePoints, maiState.pussySensitiveLevel);
        AddSensitivePart(data, SensitiveBodyPart.Butthole, maiState.buttholeSensitivePoints, maiState.buttholeSensitiveLevel);
        return data;
    }

    private static void AddSensitivePart(SaveData data, SensitiveBodyPart bodyPart, int points, int level)
    {
        data.sensitiveParts.Add(new SaveData.SensitivePartSaveData { bodyPart = (int)bodyPart, points = points, level = level });
    }

    private static string SerializeDate(DateTime value) => value == DateTime.MinValue ? string.Empty : value.ToString("O", CultureInfo.InvariantCulture);
    private static DateTime DeserializeDate(string value) => string.IsNullOrEmpty(value) ? DateTime.MinValue : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);


    /// <summary>Legacy UI entry point. Loads the quick-save when no typed payload is supplied.</summary>
    public void LoadGame(object saveData = null)
    {
        if (saveData is SaveData typedSave)
        {
            LoadGame(typedSave);
            return;
        }

        LoadQuickSave();
    }

    public void ChangeArea(int area)
    {
        Area targetArea = (Area)area;

        int travelTime = GetTravelTime(targetArea);
        TimeSpan travelDuration = TimeSpan.FromMinutes(travelTime);
        DateTime estimatedArrivalTime = Time + travelDuration;

        bool isUnavailable = IsAreaUnavailable(targetArea, estimatedArrivalTime);

        if (isUnavailable)
        {
            EventBus<PlaceUnavailableEvent>.Raise(new PlaceUnavailableEvent { CurrentArea = Area, TargetArea = targetArea, Time = estimatedArrivalTime });
            return;
        }

        // Check if TransitionPanel exists
        if (TransitionPanel.Instance != null)
        {
            // Use event-driven transition
            this.pendingAreaChange = (targetArea, travelDuration);
            EventBus<TransitionRequestEvent>.Raise(new TransitionRequestEvent());
        }
        else
        {
            // Fallback: No TransitionPanel, execute immediately
            Area = targetArea;
            AdvanceTime(travelDuration);
            
            EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
            EventBus<AreaTransitionReadyEvent>.Raise(new AreaTransitionReadyEvent { NewArea = targetArea });
        }
    }

    private void OnTransitionMidPoint(TransitionMidPointEvent evt)
    {
        if (this.pendingAreaChange.HasValue)
        {
            var (targetArea, travelDuration) = this.pendingAreaChange.Value;
            
            Area = targetArea;
            AdvanceTime(travelDuration);
            
            EventBus<PlaceChangedEvent>.Raise(new PlaceChangedEvent { Area = Area, Time = Time, Cycle = Cycle });
            EventBus<AreaTransitionReadyEvent>.Raise(new AreaTransitionReadyEvent { NewArea = targetArea });
            
            this.pendingAreaChange = null;
        }
    }

    // OPT-7/8: Use typed getters/setters and cached language count
    public void ChangeLocale(int value = 0)
    {
        int currentLang = (int)SettingsManager.GetLanguage();
        int newLang = currentLang + value;

        if (newLang < 0 || newLang > (Enum.GetNames(typeof(Language)).Length - 1))
            return;

        var language = (Language)newLang;
        SettingsManager.UpdateLanguage(language);
        LocalizationManager.Instance.SetLanguage(language);
    }

    public void GrantReward(BasicStats stat, int amount)
    {

    }
    
    /// <summary>
    /// Advance time by the specified duration (for actions, sleep, etc.)
    /// </summary>
    /// <param name="duration">TimeSpan to advance</param>
    public void AdvanceTimePublic(TimeSpan duration)
    {
        AdvanceTime(duration);
    }

    private void InitializeDefaultValues()
    {
        Area = Area.Home;
        Time = new DateTime(2001, 1, 1, 6, 0, 0);
        Cycle = DayCycle.Morning;
    }

    private void AdvanceTime(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        DayCycle previousCycle = Cycle;
        Time += duration;
        Cycle = GetCurrentDayCycle(Time);

        // Fire TimeChanged event when time advances
        EventBus<TimeChangedEvent>.Raise(new TimeChangedEvent
        {
            Area = Area,
            Time = Time,
            Cycle = Cycle,
            PreviousCycle = previousCycle
        });
    }

    private DayCycle GetCurrentDayCycle(DateTime time)
    {
        int h = time.Hour;
        if (h >= 6 && h < 18)
            return DayCycle.Morning;
        if (h >= 18 && h < 23)
            return DayCycle.Evening;
        // Night: h >= 23 || h < 6
        return DayCycle.Night;
    }


    private int GetTravelTime(Area area)
    {
        return DefaultSettings.GetTravelTime(area);
    }

    private bool IsAreaUnavailable(Area area, DateTime timeToCheck)
    {
        return DefaultSettings.IsAreaUnavailableAtTime(area, timeToCheck);
    }
    
    // Daily action tracking methods
    
    /// <summary>
    /// Check if player has eaten today
    /// </summary>
    public bool HasEatenToday()
    {
        return TimeManager.IsSameDay(lastEatingDate, Time);
    }

    public bool HasWorkedToday()
    {
        return TimeManager.IsSameDay(lastWorkingDate, Time);
    }

    /// <summary>
    /// Check if player has talked in the current area today
    /// Talking is once-per-day-per-area
    /// </summary>
    public bool HasTalkedTodayInArea(Area area)
    {
        if (!lastTalkingDateByArea.TryGetValue(area, out DateTime lastDate))
            return false;
        
        return TimeManager.IsSameDay(lastDate, Time);
    }

    /// <summary>
    /// Check if player has exercised today
    /// </summary>
    public bool HasExercisedToday()
    {
        return TimeManager.IsSameDay(lastExerciseDate, Time);
    }
    
    /// <summary>
    /// Check if player has had sex today
    /// </summary>
    public bool HasHadSexToday()
    {
        return TimeManager.IsSameDay(lastSexDate, Time);
    }
    
    /// <summary>
    /// Mark eating action as used for today
    /// </summary>
    public void MarkEatingUsed()
    {
        lastEatingDate = Time;
        Debug.Log($"[GameManager] Eating marked as used on {Time.ToShortDateString()}");
    }

    public void MarkWorkingUsed()
    {
        lastWorkingDate = Time;
        Debug.Log($"[GameManager] Working marked as used on {Time.ToShortDateString()}");
    }

    /// <summary>
    /// Mark talking action as used for the current area
    /// Talking is once-per-day-per-area
    /// </summary>
    public void MarkTalkingUsedInArea(Area area)
    {
        lastTalkingDateByArea[area] = Time;
        Debug.Log($"[GameManager] Talking marked as used in {area} on {Time.ToShortDateString()}");
    }

    /// <summary>
    /// Mark exercise action as used for today
    /// </summary>
    public void MarkExerciseUsed()
    {
        lastExerciseDate = Time;
        Debug.Log($"[GameManager] Exercise marked as used on {Time.ToShortDateString()}");
    }
    
    /// <summary>
    /// Mark sex action as used for today
    /// </summary>
    public void MarkSexUsed()
    {
        lastSexDate = Time;
        Debug.Log($"[GameManager] Sex marked as used on {Time.ToShortDateString()}");
    }
    
    /// <summary>
    /// Reset daily action tracking (called when day changes)
    /// </summary>
    public void ResetDailyActions()
    {
        lastEatingDate = DateTime.MinValue;
        lastSexDate = DateTime.MinValue;
        lastWorkingDate = DateTime.MinValue;
        lastTalkingDateByArea.Clear();
        lastExerciseDate = DateTime.MinValue;
        napCountToday = 0;
        deepSleepCountToday = 0;
        lastSleepDate = DateTime.MinValue;
        Debug.Log("[GameManager] Daily actions reset");
    }
    
    /// <summary>
    /// Check if player can take a nap today (max 2 per day)
    /// </summary>
    public bool CanNapToday()
    {
        // Reset counts if it's a new day
        if (!TimeManager.IsSameDay(lastSleepDate, Time))
            return true;
        return napCountToday < 2;
    }
    
    /// <summary>
    /// Check if player can deep sleep today (max 1 per day, only 9 PM - 6 AM)
    /// </summary>
    public bool CanDeepSleepToday()
    {
        // Reset counts if it's a new day
        if (!TimeManager.IsSameDay(lastSleepDate, Time))
            return true;
        return deepSleepCountToday < 1;
    }
    
    /// <summary>
    /// Check if current time is within deep sleep hours (9 PM to 6 AM)
    /// </summary>
    public bool IsDeepSleepTimeWindow()
    {
        int hour = Time.Hour;
        return hour >= 21 || hour < 6; // 9 PM to 6 AM
    }
    
    /// <summary>
    /// Mark nap as used for today
    /// </summary>
    public void MarkNapUsed()
    {
        if (!TimeManager.IsSameDay(lastSleepDate, Time))
        {
            napCountToday = 0;
            deepSleepCountToday = 0;
        }
        napCountToday++;
        lastSleepDate = Time;
        Debug.Log($"[GameManager] Nap used ({napCountToday}/2) on {Time.ToShortDateString()}");
    }
    
    /// <summary>
    /// Mark deep sleep as used for today
    /// </summary>
    public void MarkDeepSleepUsed()
    {
        if (!TimeManager.IsSameDay(lastSleepDate, Time))
        {
            napCountToday = 0;
            deepSleepCountToday = 0;
        }
        deepSleepCountToday++;
        lastSleepDate = Time;
        Debug.Log($"[GameManager] Deep sleep used ({deepSleepCountToday}/1) on {Time.ToShortDateString()}");
    }

    /// <summary>
    /// Advance to the configured morning time on the next day
    /// Handles Libido increment, sensitive points decay, and Libido 5 penalty
    /// </summary>
    public void AdvanceToNextDayMorning(int targetHour = 6, int targetMinute = 0)
    {
        Target mai = DataManager.GetCurrentBoss() as Target;
        if (mai == null)
        {
            Debug.LogWarning("[GameManager] AdvanceToNextDayMorning: Mai is null, cannot apply day change effects");
            return;
        }

        DayCycle previousCycle = Cycle;

        // Check if player had sex today
        bool hadSex = HasHadSexToday();

        if (!hadSex)
        {
            // Apply sensitive points decay
            mai.ApplySensitivePointsDecay();
            Debug.Log($"[GameManager] Applied sensitive points decay (-{DefaultSettings.SensitivePointsDecayPerNight} to sensitive point stats)");

            // Increment the days-without-sex counter (capped at MaxLibido).
            // The named arousal state is derived from this counter.
            mai.IncrementLibido();
            Debug.Log($"[GameManager] Incremented days-without-sex to {mai.GetLibido()} (Libido state {mai.GetLibidoState()})");
        }
        else
        {
            // Player had sex - Libido should already be reset to 0 by sex scene
            // But ensure it's reset in case it wasn't
            if (mai.GetLibido() > 0)
            {
                mai.ResetLibido();
                Debug.Log("[GameManager] Reset Libido to 0 after sex");
            }
        }
        
        // Set time to next day at the configured morning time
        DateTime nextMorning = Time.Date.AddDays(1).AddHours(targetHour).AddMinutes(targetMinute);
        Time = nextMorning;
        
        // Update cycle to Morning
        Cycle = DayCycle.Morning;
        
        // Reset daily actions
        ResetDailyActions();
        
        // Fire time changed event
        EventBus<TimeChangedEvent>.Raise(new TimeChangedEvent
        {
            Time = Time,
            Cycle = Cycle,
            Area = Area,
            PreviousCycle = previousCycle
        });
        
        Debug.Log($"[GameManager] Advanced to next day morning: {Time}");

        // Daily auto-save at the start of each new day.
        DailyAutoSave();
    }
    
    // Work Level (Progress) methods
    
    /// <summary>
    /// Get current work level (1-5)
    /// </summary>
    public int GetWorkLevel() => workLevel;
    
    /// <summary>
    /// Get current work progress points
    /// </summary>
    public int GetWorkProgress() => workProgress;
    
    /// <summary>
    /// Get progress required for next level promotion
    /// </summary>
    public int GetNextLevelRequirement()
    {
        if (Base.DefaultSettings.WorkLevelProgressRequirements.TryGetValue(workLevel, out int required))
            return required;
        return -1;
    }
    
    /// <summary>
    /// Add progress points from working
    /// </summary>
    public void AddWorkProgress(int amount)
    {
        workProgress += amount;
        Debug.Log($"[GameManager] Work progress: {workProgress} (added {amount})");
    }
    
    /// <summary>
    /// Attempt to promote to next work level
    /// </summary>
    /// <returns>True if promotion successful, false otherwise</returns>
    public bool TryPromoteWorkLevel()
    {
        if (!Base.DefaultSettings.CanPromoteWorkLevel(workLevel, workProgress))
            return false;
        
        workLevel++;
        workProgress = 0; // Reset progress after promotion
        Debug.Log($"[GameManager] Work level promoted to {workLevel}");
        return true;
    }

}

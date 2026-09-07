using UI;
using UI.SexPosition;
using EventBus;
using UnityEngine;
using MaisLoveStory.Live2D;
using UnityEngine.UI;
using Base.Character;
using Base.Character.Skills;
using Base.Localization;
using Base.SexScenes;
using System.Collections.Generic;

/// <summary>
/// Navigation panel for Simulation section - controls Live2D model animations
/// Buttons: Insert, Pussy, Butthole, Slow, Fast, Stop, Outside, Pullout
/// </summary>
public class SimulationNavigationPanel : UIPanel
{
    [Header("Live2D Reference")]
    [Tooltip("Reference to Live2DPanel which manages all Live2D models")]
    [SerializeField] private Live2DPanel live2DPanel;
    
    [Header("Sex Simulation UI")]
    [SerializeField] private Image maiOrgasmFillImage1;  // Shortest bar (caps first)
    [SerializeField] private Image maiOrgasmFillImage2;  // Medium bar
    [SerializeField] private Image maiOrgasmFillImage3;  // Tallest bar (reference)
    
    // Height ratios: tallestHeight / thisImageHeight
    // Used to scale fillAmount so shorter images cap earlier
    private float orgasmHeightRatio1 = 1f;
    private float orgasmHeightRatio2 = 1f;
    [SerializeField] private Image playerStaminaFillImage;
    [SerializeField] private Image playerCumFillImage;
    [SerializeField] private LocalizedText playerBulletText;
    
    [Header("Control Buttons")]
    [SerializeField] private Button finishButton;
    [SerializeField] private Button insertButton;
    [SerializeField] private Button pussyButton;
    [SerializeField] private Button buttholeButton;
    [SerializeField] private Button slowButton;
    [SerializeField] private Button fastButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button outsideButton;
    [SerializeField] private Button pulloutButton;

    [Header("Roleplay Playback Buttons")]
    [SerializeField] private Button roleplaySlowButton;
    [SerializeField] private Button roleplayFastButton;
    [SerializeField] private Button roleplayStopButton;

    [Header("Roleplay Selector Buttons")]
    // Category row ("Roleplay - Buttons"): Hand / Tongue / Sex Toy.
    [SerializeField] private Button roleplayHandButton;
    [SerializeField] private Button roleplayTongueButton;
    [SerializeField] private Button roleplaySexToyButton;
    // Animation row ("Roleplay - Animation 1/2/3/4"): switches within the selected category.
    [SerializeField] private Button roleplayAnimation1Button;
    [SerializeField] private Button roleplayAnimation2Button;
    [SerializeField] private Button roleplayAnimation3Button;
    [SerializeField] private Button roleplayAnimation4Button;

    // Cached motion controller from Live2DPanel
    private Live2DMotionController motionController;
    
    // Track current hole selection (Pussy or Butthole)
    public bool isPussySelected { get; set; } = true;
    
    // Insert animation gating: buttons show immediately but thrust waits for animation
    public bool isInsertAnimationComplete { get; set; } = true;
    private bool hasPendingThrust = false;
    private bool pendingThrustIsFast = false;

    // OPT-26: Dirty-flag UI — only update when values change
    private float _lastOrgasm = -1f;
    private int _lastStamina = -1;
    private int _lastMaxStamina = -1;
    private float _lastCum = -1f;
    private int _lastBullets = -1;

    // OPT-41: Guard against duplicate listener setup
    private bool _buttonsSetup = false;

    // OPT-42: Cached player reference
    private Player _cachedPlayer;

    [Header("Position Configuration")]
    [SerializeField] private SexPositionConfigType positionType = SexPositionConfigType.Missionary;
    private SexPositionConfig currentPositionConfig;
    
    // State machine for button flow

    
    private SimulationState currentState = SimulationState.Idle;
    private bool isRoleplayOpeningActive;
    private bool isRoleplayOpeningComplete;
    private bool isRoleplayToSexTransitioning;
    private EventBinding<TransitionMidPointEvent> roleplayHandoffMidPointBinding;
    private EventBinding<TransitionCompleteEvent> roleplayHandoffCompleteBinding;
    
    private void Awake()
    {
        // Get motion controller from Live2DPanel (matched to the active position)
        if (live2DPanel != null)
        {
            motionController = live2DPanel.GetSimulationMotionController(positionType);
        }
        
        // Compute orgasm bar height ratios from RectTransform sizes
        ComputeOrgasmHeightRatios();
        
        // Initialize position configuration
        InitializePositionConfig();
        
        // Hide Live2D by default (only show when in Simulation section)
        HideSimulationLive2D();
    }
    
    private void ComputeOrgasmHeightRatios()
    {
        if (maiOrgasmFillImage3 == null) return;
        
        float referenceHeight = maiOrgasmFillImage3.rectTransform.rect.height;
        if (referenceHeight <= 0) return;
        
        if (maiOrgasmFillImage1 != null)
        {
            float h1 = maiOrgasmFillImage1.rectTransform.rect.height;
            orgasmHeightRatio1 = (h1 > 0) ? referenceHeight / h1 : 1f;
        }
        
        if (maiOrgasmFillImage2 != null)
        {
            float h2 = maiOrgasmFillImage2.rectTransform.rect.height;
            orgasmHeightRatio2 = (h2 > 0) ? referenceHeight / h2 : 1f;
        }
    }

    private void InitializePositionConfig()
    {
        currentPositionConfig = SexPositionConfigFactory.Create(positionType);
        currentPositionConfig.Initialize();

        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.SetCurrentPosition(positionType);
        }

        currentPositionConfig.OnPositionEntered(this);
        SetState(currentPositionConfig.GetInitialState());
    }

    /// <summary>
    /// Switches the active simulation position and its Live2D controller.
    /// </summary>
    public void SetPosition(SexPositionConfigType newPositionType)
    {
        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.StopThrusting();
        }

        currentPositionConfig?.OnPositionExited(this);
        HideSimulationLive2D();

        positionType = newPositionType;
        motionController = live2DPanel != null
            ? live2DPanel.GetSimulationMotionController(positionType)
            : null;

        ShowSimulationLive2D();
        InitializePositionConfig();
    }

    /// <summary>
    /// Marks the randomized Roleplay opener as complete without conflating the
    /// completion source with Player Cum. Paizuri Hand/Tongue call this from
    /// their Mai-orgasm route; Player-Cum-driven openers use the shared handler.
    /// </summary>
    public void MarkRoleplayOpeningComplete()
    {
        if (isRoleplayOpeningActive)
        {
            isRoleplayOpeningComplete = true;
        }
    }

    private void BeginRoleplayOpening()
    {
        ClearRoleplayHandoffBindings();
        isRoleplayOpeningActive = true;
        isRoleplayOpeningComplete = false;
        isRoleplayToSexTransitioning = false;

        SetPosition(GetRandomUnlockedRoleplayPosition());

        SelectRoleplayCategoryButton(UnityEngine.Random.Range(1, 4));
    }

    private static SexPositionConfigType GetRandomUnlockedRoleplayPosition()
    {
        var unlockedPositions = new List<SexPositionConfigType>();
        if (SexSceneUnlockService.IsUnlocked(SexSceneType.RoleplayPussy))
        {
            unlockedPositions.Add(SexPositionConfigType.RoleplayPussy);
        }
        if (SexSceneUnlockService.IsUnlocked(SexSceneType.RoleplayButthole))
        {
            unlockedPositions.Add(SexPositionConfigType.RoleplayButthole);
        }
        if (SexSceneUnlockService.IsUnlocked(SexSceneType.Blowjob))
        {
            unlockedPositions.Add(SexPositionConfigType.RoleplayBlowjob);
        }
        if (SexSceneUnlockService.IsUnlocked(SexSceneType.Paizuri))
        {
            unlockedPositions.Add(SexPositionConfigType.RoleplayPaizuri);
        }

        if (unlockedPositions.Count > 0)
        {
            return unlockedPositions[UnityEngine.Random.Range(0, unlockedPositions.Count)];
        }

        // Roleplay Pussy is default-unlocked and remains the safe fallback if
        // external PlayerPrefs data leaves both entries unavailable.
        return SexPositionConfigType.RoleplayPussy;
    }

    private static bool IsRoleplayPosition(SexPositionConfigType type)
    {
        return type == SexPositionConfigType.RoleplayPussy ||
               type == SexPositionConfigType.RoleplayButthole ||
               type == SexPositionConfigType.RoleplayBlowjob ||
               type == SexPositionConfigType.RoleplayPaizuri;
    }

    private void HandleRoleplayStopClicked()
    {
        if (isRoleplayOpeningActive &&
            isRoleplayOpeningComplete &&
            currentState == SimulationState.AfterCumming)
        {
            // Send Stop to the Roleplay Animator as the fade begins. Missionary
            // replaces the model at the black midpoint.
            currentPositionConfig?.OnThrustStopped(this);
            BeginRoleplayToSexHandoff();
            return;
        }

        currentPositionConfig?.OnThrustStopped(this);
    }

    private void BeginRoleplayToSexHandoff()
    {
        if (isRoleplayToSexTransitioning)
        {
            return;
        }

        isRoleplayToSexTransitioning = true;
        if (TransitionPanel.Instance == null)
        {
            CompleteRoleplayToSexHandoff();
            isRoleplayToSexTransitioning = false;
            return;
        }

        roleplayHandoffMidPointBinding =
            new EventBinding<TransitionMidPointEvent>((evt) =>
            {
                if (roleplayHandoffMidPointBinding != null)
                {
                    EventBus<TransitionMidPointEvent>.Deregister(
                        roleplayHandoffMidPointBinding);
                    roleplayHandoffMidPointBinding = null;
                }

                CompleteRoleplayToSexHandoff();
            });
        roleplayHandoffCompleteBinding =
            new EventBinding<TransitionCompleteEvent>((evt) =>
            {
                if (roleplayHandoffCompleteBinding != null)
                {
                    EventBus<TransitionCompleteEvent>.Deregister(
                        roleplayHandoffCompleteBinding);
                    roleplayHandoffCompleteBinding = null;
                }

                isRoleplayToSexTransitioning = false;
            });

        EventBus<TransitionMidPointEvent>.Register(roleplayHandoffMidPointBinding);
        EventBus<TransitionCompleteEvent>.Register(roleplayHandoffCompleteBinding);
        EventBus<TransitionRequestEvent>.Raise(new TransitionRequestEvent());
    }

    private void CompleteRoleplayToSexHandoff()
    {
        SexSimulationManager.Instance?.BeginSexPhase();
        isRoleplayOpeningActive = false;
        isRoleplayOpeningComplete = false;
        SetPosition(SexPositionConfigType.Missionary);
        ResetOrgasmBarUI();
        _lastOrgasm = -1f;
        _lastCum = -1f;
    }

    private void ClearRoleplayHandoffBindings()
    {
        if (roleplayHandoffMidPointBinding != null)
        {
            EventBus<TransitionMidPointEvent>.Deregister(roleplayHandoffMidPointBinding);
            roleplayHandoffMidPointBinding = null;
        }

        if (roleplayHandoffCompleteBinding != null)
        {
            EventBus<TransitionCompleteEvent>.Deregister(roleplayHandoffCompleteBinding);
            roleplayHandoffCompleteBinding = null;
        }
    }

    private void Start()
    {
        // Setup button listeners
        SetupButtons();
        
        // Subscribe to section switch events
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnSectionSwitched.AddListener(HandleSectionSwitched);
            
            // Check current section in case we're already in Simulation
            if (UIPanelManager.Instance.GetCurrentSection() == GameSection.Simulation)
            {
                ShowSimulationLive2D();
            }
        }
        
        // Subscribe to SexSimulationManager events
        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.SetCurrentPosition(positionType);

            SexSimulationManager.Instance.OnPlayerCumReached += HandlePlayerCumReached;
            SexSimulationManager.Instance.OnMaiOrgasmReached += HandleMaiOrgasm;
            SexSimulationManager.Instance.OnStaminaDepleted += HandleStaminaDepleted;
            SexSimulationManager.Instance.OnBulletsEmpty += HandleBulletsEmpty;
            SexSimulationManager.Instance.OnCumOutsideAnimationComplete += HandleCumOutsideComplete;
            SexSimulationManager.Instance.OnCumInsideAnimationComplete += HandleCumInsideComplete;
            SexSimulationManager.Instance.OnPulloutAnimationComplete += HandlePulloutComplete;
            SexSimulationManager.Instance.OnInsertAnimationComplete += HandleInsertComplete;
            SexSimulationManager.Instance.OnRoleplayStartEntered += HandleRoleplayStartEntered;
            SexSimulationManager.Instance.OnRoleplayEggWorkEntered += HandleRoleplayEggWorkEntered;
            SexSimulationManager.Instance.OnRoleplayEggLoopEntered += HandleRoleplayEggLoopEntered;
            SexSimulationManager.Instance.OnRoleplayAfterCummingEntered += HandleRoleplayAfterCummingEntered;
            SexSimulationManager.Instance.OnCowgirlAnimationSignal += HandleCowgirlAnimationSignal;
        }
    }

    private void Update()
    {
        currentPositionConfig?.Tick(this, Time.deltaTime);

        // OPT-26: Update UI with dirty flags — only write when values change
        UpdateSimulationUI();
    }
    
    private void UpdateSimulationUI()
    {
        if (SexSimulationManager.Instance == null) return;
        
        // Update Mai orgasm fill (3 layered images with different heights)
        float orgasmPercentage = SexSimulationManager.Instance.GetMaiOrgasmBar(); // 0-100
        if (orgasmPercentage != _lastOrgasm)
        {
            _lastOrgasm = orgasmPercentage;
            float normalizedOrgasm = orgasmPercentage / 100f;

            if (maiOrgasmFillImage1 != null)
                maiOrgasmFillImage1.fillAmount = Mathf.Min(normalizedOrgasm * orgasmHeightRatio1, 1f);
            if (maiOrgasmFillImage2 != null)
                maiOrgasmFillImage2.fillAmount = Mathf.Min(normalizedOrgasm * orgasmHeightRatio2, 1f);
            if (maiOrgasmFillImage3 != null)
                maiOrgasmFillImage3.fillAmount = normalizedOrgasm;
        }
        
        // Update player stamina fill
        int currentStamina = SexSimulationManager.Instance.GetPlayerStamina();
        int maxStamina = SexSimulationManager.Instance.GetPlayerMaxStamina();
        if (currentStamina != _lastStamina || maxStamina != _lastMaxStamina)
        {
            _lastStamina = currentStamina;
            _lastMaxStamina = maxStamina;
            if (playerStaminaFillImage != null)
                playerStaminaFillImage.fillAmount = maxStamina > 0 ? (float)currentStamina / maxStamina : 0f;
        }
        
        // Update player cum fill
        float cumBar = SexSimulationManager.Instance.GetPlayerCumBar();
        if (cumBar != _lastCum)
        {
            _lastCum = cumBar;
            if (playerCumFillImage != null)
                playerCumFillImage.fillAmount = cumBar / 100f;
        }
        
        // Update bullet text
        int bullets = SexSimulationManager.Instance.GetCurrentBullets();
        if (bullets != _lastBullets)
        {
            _lastBullets = bullets;
            if (playerBulletText != null)
                playerBulletText.SetLocalized("UI Value Integer", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
                {
                    ["amount"] = bullets
                });
        }
    }
    
    /// <summary>
    /// Reset all Mai orgasm fill images to 0
    /// Called when entering Simulation section or showing panel
    /// </summary>
    private void ResetOrgasmBarUI()
    {
        if (maiOrgasmFillImage1 != null)
        {
            maiOrgasmFillImage1.fillAmount = 0f;
        }
        
        if (maiOrgasmFillImage2 != null)
        {
            maiOrgasmFillImage2.fillAmount = 0f;
        }
        
        if (maiOrgasmFillImage3 != null)
        {
            maiOrgasmFillImage3.fillAmount = 0f;
        }
    }

    protected override void OnShow(object data = null)
    {
        base.OnShow(data);
        SetupButtons();

        // Every normal Simulation session starts with a randomized Roleplay category.
        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.ResetSimulation();
        }

        BeginRoleplayOpening();
        
        // Reset Mai orgasm fill images to 0
        ResetOrgasmBarUI();
        // Reset dirty flags so UI updates on next frame
        _lastOrgasm = -1f;
        _lastStamina = -1;
        _lastMaxStamina = -1;
        _lastCum = -1f;
        _lastBullets = -1;

        // OPT-42: Cache player reference
        _cachedPlayer = GameManager.Instance?.DataManager?.GetPlayer() as Player;
    }

    private void OnDestroy()
    {
        // Cleanup section listener
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.OnSectionSwitched.RemoveListener(HandleSectionSwitched);
        }
        
        // Cleanup SexSimulationManager events
        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.OnPlayerCumReached -= HandlePlayerCumReached;
            SexSimulationManager.Instance.OnMaiOrgasmReached -= HandleMaiOrgasm;
            SexSimulationManager.Instance.OnStaminaDepleted -= HandleStaminaDepleted;
            SexSimulationManager.Instance.OnBulletsEmpty -= HandleBulletsEmpty;
            SexSimulationManager.Instance.OnCumOutsideAnimationComplete -= HandleCumOutsideComplete;
            SexSimulationManager.Instance.OnCumInsideAnimationComplete -= HandleCumInsideComplete;
            SexSimulationManager.Instance.OnPulloutAnimationComplete -= HandlePulloutComplete;
            SexSimulationManager.Instance.OnInsertAnimationComplete -= HandleInsertComplete;
            SexSimulationManager.Instance.OnRoleplayStartEntered -= HandleRoleplayStartEntered;
            SexSimulationManager.Instance.OnRoleplayEggWorkEntered -= HandleRoleplayEggWorkEntered;
            SexSimulationManager.Instance.OnRoleplayEggLoopEntered -= HandleRoleplayEggLoopEntered;
            SexSimulationManager.Instance.OnRoleplayAfterCummingEntered -= HandleRoleplayAfterCummingEntered;
            SexSimulationManager.Instance.OnCowgirlAnimationSignal -= HandleCowgirlAnimationSignal;
        }

        currentPositionConfig?.OnPositionExited(this);
        ClearRoleplayHandoffBindings();
        
        // Cleanup button listeners
        if (insertButton != null) insertButton.onClick.RemoveAllListeners();
        if (pussyButton != null) pussyButton.onClick.RemoveAllListeners();
        if (buttholeButton != null) buttholeButton.onClick.RemoveAllListeners();
        if (slowButton != null) slowButton.onClick.RemoveAllListeners();
        if (fastButton != null) fastButton.onClick.RemoveAllListeners();
        if (stopButton != null) stopButton.onClick.RemoveAllListeners();
        if (outsideButton != null) outsideButton.onClick.RemoveAllListeners();
        if (pulloutButton != null) pulloutButton.onClick.RemoveAllListeners();
        if (roleplaySlowButton != null) roleplaySlowButton.onClick.RemoveAllListeners();
        if (roleplayFastButton != null) roleplayFastButton.onClick.RemoveAllListeners();
        if (roleplayStopButton != null) roleplayStopButton.onClick.RemoveAllListeners();
        if (roleplayHandButton != null) roleplayHandButton.onClick.RemoveAllListeners();
        if (roleplayTongueButton != null) roleplayTongueButton.onClick.RemoveAllListeners();
        if (roleplaySexToyButton != null) roleplaySexToyButton.onClick.RemoveAllListeners();
        if (roleplayAnimation1Button != null) roleplayAnimation1Button.onClick.RemoveAllListeners();
        if (roleplayAnimation2Button != null) roleplayAnimation2Button.onClick.RemoveAllListeners();
        if (roleplayAnimation3Button != null) roleplayAnimation3Button.onClick.RemoveAllListeners();
        if (roleplayAnimation4Button != null) roleplayAnimation4Button.onClick.RemoveAllListeners();
        if (finishButton != null) finishButton.onClick.RemoveAllListeners();
    }

    #region Section Management
    
    private void HandleSectionSwitched(GameSection oldSection, GameSection newSection)
    {
        if (newSection == GameSection.Simulation)
        {
            ShowSimulationLive2D();
        }
        else if (oldSection == GameSection.Simulation)
        {
            HideSimulationLive2D();
        }
    }

    public void ShowSimulationLive2D()
    {
        if (live2DPanel != null)
        {
            live2DPanel.ShowSimulationLive2D(positionType);
        }
        else if (motionController != null)
        {
            motionController.gameObject.SetActive(true);
        }
    }

    public void HideSimulationLive2D()
    {
        if (live2DPanel != null)
        {
            live2DPanel.HideSimulationLive2D(positionType);
        }
        else if (motionController != null)
        {
            motionController.ResetAllBoolParameters();
            motionController.gameObject.SetActive(false);
        }
    }
    
    #endregion

    #region State Management
    
    public void SetState(SimulationState newState)
    {
        currentState = newState;
        UpdateButtonStates();
    }

    /// <summary>
    /// Forces the active Live2D controller back to its authored default state.
    /// </summary>
    public void PlayDefaultAnimation()
    {
        motionController?.PlayDefaultMotion();
    }

    /// <summary>
    /// Re-evaluates Roleplay selector visibility after the active category changes.
    /// </summary>
    public void RefreshRoleplayControls()
    {
        UpdateButtonStates();
    }

    /// <summary>
    /// Applies action-specific speed availability to the Roleplay playback controls.
    /// </summary>
    public void SetRoleplaySpeedAvailability(bool slowAvailable, bool fastAvailable)
    {
        if (roleplaySlowButton != null)
        {
            roleplaySlowButton.interactable = slowAvailable;
        }

        if (roleplayFastButton != null)
        {
            roleplayFastButton.interactable = fastAvailable;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Creates the same Roleplay Pussy configuration used by the Simulation panel,
    /// but binds it to an isolated Live2D Test Bench controller.
    /// </summary>
    public RoleplayPussyPositionConfig ConfigureRoleplayTestBench(
        Live2DMotionController testController)
    {
        return (RoleplayPussyPositionConfig)ConfigureRoleplayTestBench(
            SexPositionConfigType.RoleplayPussy,
            testController);
    }

    public SexPositionConfig ConfigureRoleplayTestBench(
        SexPositionConfigType testPositionType,
        Live2DMotionController testController)
    {
        if (!IsRoleplayPosition(testPositionType))
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(testPositionType),
                testPositionType,
                "The Roleplay Test Bench only accepts Roleplay positions.");
        }

        motionController = testController;
        positionType = testPositionType;
        currentPositionConfig = SexPositionConfigFactory.Create(testPositionType);
        currentPositionConfig.Initialize();
        SexSimulationManager.Instance?.SetCurrentPosition(testPositionType);
        currentPositionConfig.OnPositionEntered(this);
        SetState(currentPositionConfig.GetInitialState());
        return currentPositionConfig;
    }
#endif

    public bool IsHoleUnlocked(HoleType holeType)
    {
        if (holeType == HoleType.Pussy)
        {
            return true;
        }

        if (_cachedPlayer == null)
        {
            _cachedPlayer = GameManager.Instance?.DataManager?.GetPlayer() as Player;
        }

        return _cachedPlayer?.IsSkillUnlocked(SkillType.A) ?? false;
    }
    
    private void UpdateButtonStates()
    {
        if (currentPositionConfig == null)
        {
            return;
        }

        ButtonVisibilitySet visibility = currentPositionConfig.GetButtonVisibility(currentState);
        RefreshRoleplayCategoryLabels();
        bool showButtholeButton = visibility.buttholeButton && IsHoleUnlocked(HoleType.Butthole);
        bool isRoleplayPosition = IsRoleplayPosition(positionType);
        bool showRoleplaySelectors = isRoleplayPosition && visibility.roleplaySelectorButtons;
        int roleplayAnimationButtonCount = currentPositionConfig.GetRoleplayAnimationButtonCount();

        SetButtonActive(insertButton, visibility.insertButton);
        SetButtonActive(pussyButton, visibility.pussyButton);
        SetButtonActive(buttholeButton, showButtholeButton);
        SetButtonActive(slowButton, !isRoleplayPosition && visibility.slowButton);
        SetButtonActive(fastButton, !isRoleplayPosition && visibility.fastButton);
        SetButtonActive(stopButton, !isRoleplayPosition && visibility.stopButton);
        SetButtonActive(roleplaySlowButton, isRoleplayPosition && visibility.slowButton);
        SetButtonActive(roleplayFastButton, isRoleplayPosition && visibility.fastButton);
        SetButtonActive(roleplayStopButton, isRoleplayPosition && visibility.stopButton);
        SetButtonActive(outsideButton, visibility.outsideButton);
        SetButtonActive(pulloutButton, visibility.pulloutButton);
        SetButtonActive(roleplayHandButton, showRoleplaySelectors);
        SetButtonActive(roleplayTongueButton, showRoleplaySelectors);
        SetButtonActive(roleplaySexToyButton, showRoleplaySelectors);
        bool roleplayCategoriesAvailable =
            showRoleplaySelectors && currentPositionConfig.AreRoleplayCategoriesAvailable();
        bool roleplayAnimationsAvailable =
            showRoleplaySelectors && !roleplayCategoriesAvailable;
        if (roleplayHandButton != null)
            roleplayHandButton.interactable = roleplayCategoriesAvailable;
        if (roleplayTongueButton != null)
            roleplayTongueButton.interactable = roleplayCategoriesAvailable;
        if (roleplaySexToyButton != null)
            roleplaySexToyButton.interactable = roleplayCategoriesAvailable;
        SetButtonActive(roleplayAnimation1Button, showRoleplaySelectors && roleplayAnimationButtonCount >= 1);
        SetButtonActive(roleplayAnimation2Button, showRoleplaySelectors && roleplayAnimationButtonCount >= 2);
        SetButtonActive(roleplayAnimation3Button, showRoleplaySelectors && roleplayAnimationButtonCount >= 3);
        SetButtonActive(roleplayAnimation4Button, showRoleplaySelectors && roleplayAnimationButtonCount >= 4);
        if (roleplayAnimation1Button != null)
            roleplayAnimation1Button.interactable = roleplayAnimationsAvailable;
        if (roleplayAnimation2Button != null)
            roleplayAnimation2Button.interactable = roleplayAnimationsAvailable;
        if (roleplayAnimation3Button != null)
            roleplayAnimation3Button.interactable = roleplayAnimationsAvailable;
        if (roleplayAnimation4Button != null)
            roleplayAnimation4Button.interactable = roleplayAnimationsAvailable;
        SetButtonActive(finishButton, visibility.finishButton);
    }

    private void SelectRoleplayCategoryButton(int buttonNumber)
    {
        if (currentPositionConfig == null)
        {
            return;
        }

        RoleplayCategory category =
            currentPositionConfig.GetRoleplayCategoryForButton(buttonNumber);
        currentPositionConfig.OnRoleplayCategorySelected(this, category);
    }

    private void RefreshRoleplayCategoryLabels()
    {
        if (currentPositionConfig == null)
        {
            return;
        }

        SetButtonLabel(
            roleplayHandButton,
            currentPositionConfig.GetRoleplayCategoryButtonLabel(1));
        SetButtonLabel(
            roleplayTongueButton,
            currentPositionConfig.GetRoleplayCategoryButtonLabel(2));
        SetButtonLabel(
            roleplaySexToyButton,
            currentPositionConfig.GetRoleplayCategoryButtonLabel(3));
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMPro.TMP_Text text = button.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }
    
    private void SetButtonActive(Button button, bool active)
    {
        if (button != null)
        {
            button.gameObject.SetActive(active);
        }
    }
    
    #endregion

    #region Button Setup
    
    private void SetupButtons()
    {
        // OPT-41: Only set up listeners once to prevent stacking
        if (_buttonsSetup) return;
        _buttonsSetup = true;

        // Wire up button listeners to delegate to position config
        if (insertButton != null)
            insertButton.onClick.AddListener(() => currentPositionConfig.OnInsertClicked(this));
        
        if (pussyButton != null)
            pussyButton.onClick.AddListener(() => currentPositionConfig.OnHoleSelected(this, HoleType.Pussy));
        
        if (buttholeButton != null)
            buttholeButton.onClick.AddListener(() => currentPositionConfig.OnHoleSelected(this, HoleType.Butthole));
        
        if (slowButton != null)
            slowButton.onClick.AddListener(() => currentPositionConfig.OnThrustStarted(this, false));
        
        if (fastButton != null)
            fastButton.onClick.AddListener(() => currentPositionConfig.OnThrustStarted(this, true));
        
        if (stopButton != null)
            stopButton.onClick.AddListener(() => currentPositionConfig.OnThrustStopped(this));
        
        if (outsideButton != null)
            outsideButton.onClick.AddListener(() => currentPositionConfig.OnCumDecision(this, CumDecisionType.Outside));
        
        if (pulloutButton != null)
            pulloutButton.onClick.AddListener(() => currentPositionConfig.OnCumDecision(this, CumDecisionType.Pullout));

        if (roleplaySlowButton != null)
            roleplaySlowButton.onClick.AddListener(() => currentPositionConfig.OnThrustStarted(this, false));

        if (roleplayFastButton != null)
            roleplayFastButton.onClick.AddListener(() => currentPositionConfig.OnThrustStarted(this, true));

        if (roleplayStopButton != null)
            roleplayStopButton.onClick.AddListener(HandleRoleplayStopClicked);

        if (roleplayHandButton != null)
            roleplayHandButton.onClick.AddListener(() => SelectRoleplayCategoryButton(1));

        if (roleplayTongueButton != null)
            roleplayTongueButton.onClick.AddListener(() => SelectRoleplayCategoryButton(2));

        if (roleplaySexToyButton != null)
            roleplaySexToyButton.onClick.AddListener(() => SelectRoleplayCategoryButton(3));

        if (roleplayAnimation1Button != null)
            roleplayAnimation1Button.onClick.AddListener(() => currentPositionConfig.OnRoleplayAnimationSelected(this, 1));

        if (roleplayAnimation2Button != null)
            roleplayAnimation2Button.onClick.AddListener(() => currentPositionConfig.OnRoleplayAnimationSelected(this, 2));

        if (roleplayAnimation3Button != null)
            roleplayAnimation3Button.onClick.AddListener(() => currentPositionConfig.OnRoleplayAnimationSelected(this, 3));

        if (roleplayAnimation4Button != null)
            roleplayAnimation4Button.onClick.AddListener(() => currentPositionConfig.OnRoleplayAnimationSelected(this, 4));

        if (finishButton != null)
            finishButton.onClick.AddListener(OnFinishClicked);
    }

    private void OnFinishClicked()
    {
        // Create confirm popup data
        ConfirmPopupData popupData = new ConfirmPopupData(
            questionKey: "Simulation EndSexScene Question",
            yesCallback: OnConfirmEndSexScene,
            noCallback: null, // No callback needed, popup will just close
            closeCallback: null
        );
        
        // Show confirm popup
        UIPanelManager.Instance.ShowPanel("Confirm Popup", popupData);
    }
    
    private void OnConfirmEndSexScene()
    {
        // Calculate and apply session rewards
        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.CalculateAndApplySessionRewards();
            
            // Get session data and show result panel
            SexSessionData sessionData = SexSimulationManager.Instance.GetSessionData();
            UIPanelManager.Instance.ShowPanel("Result Panel", sessionData);
        }
    }
    
    #endregion


    #region SexSimulationManager Event Handlers
    
    private void HandlePlayerCumReached()
    {
        if (IsRoleplayPosition(positionType))
        {
            MarkRoleplayOpeningComplete();
            currentPositionConfig?.OnPlayerCumReached(this);
            return;
        }

        currentPositionConfig?.OnPlayerCumReached(this);
        if (currentState != SimulationState.CumDecision)
        {
            SetState(SimulationState.CumDecision);
        }
        
        // Start 2-second timer for Outside button
        if (SexSimulationManager.Instance != null)
        {
            SexSimulationManager.Instance.StartOutsideButtonTimer(OnOutsideButtonTimerExpired);
        }
    }
    
    private void OnOutsideButtonTimerExpired()
    {
        // Immediately disable Outside button
        SetButtonActive(outsideButton, false);

        // Delegate to config for cum inside decision
        currentPositionConfig.OnCumDecision(this, CumDecisionType.Inside);
    }
    
    private void HandleMaiOrgasm()
    {
        // Let the active position config drive its Mai-orgasm sequence.
        // Both Roleplay configs fire their position-specific cum animation.
        currentPositionConfig?.OnMaiOrgasm(this);
    }


    private void HandleRoleplayAfterCummingEntered()
    {
        currentPositionConfig?.OnAfterCummingEntered(this);
    }

    private void HandleRoleplayStartEntered()
    {
        currentPositionConfig?.OnRoleplayStartEntered(this);
    }

    private void HandleRoleplayEggWorkEntered()
    {
        currentPositionConfig?.OnRoleplayEggWorkEntered(this);
    }

    private void HandleRoleplayEggLoopEntered()
    {
        currentPositionConfig?.OnRoleplayEggLoopEntered(this);
    }

    private void HandleCowgirlAnimationSignal(CowgirlAnimationSignal signal)
    {
        if (positionType == SexPositionConfigType.Cowgirl)
        {
            currentPositionConfig?.OnAnimationSignal(this, signal);
        }
    }
    
    private void HandleStaminaDepleted()
    {
        if (IsRoleplayPosition(positionType))
        {
            currentPositionConfig?.OnThrustStopped(this);
            BeginRoleplayToSexHandoff();
            return;
        }

        currentPositionConfig?.OnThrustStopped(this);
        OnConfirmEndSexScene();
    }
    
    private void HandleBulletsEmpty()
    {

    }

    #endregion

    #region Animation Event Handlers (Called via SexSimulationManager Events)
    
    private void HandleCumOutsideComplete()
    {
        // Check if player has bullets remaining before returning to Idle
        if (SexSimulationManager.Instance != null && SexSimulationManager.Instance.HasBulletsRemaining())
        {
            // Has bullets - return to the active position's initial state.
            SetState(currentPositionConfig?.GetInitialState() ?? SimulationState.Idle);
        }
        else
        {
            // No bullets - stay in Transitioning state (only Finish button enabled)
            SetState(SimulationState.Transitioning);
        }
    }

    private void HandleCumInsideComplete()
    {
        // Set state to CumInside to show Pullout button
        SetState(SimulationState.CumInside);
    }

    private void HandleInsertComplete()
    {
        isInsertAnimationComplete = true;
        
        // If player already clicked Slow/Fast during insert animation, execute it now
        if (hasPendingThrust)
        {
            hasPendingThrust = false;
            currentPositionConfig.OnThrustStarted(this, pendingThrustIsFast);
        }
    }

    private void HandlePulloutComplete()
    {
        // Check if player has bullets remaining before returning to Idle
        if (SexSimulationManager.Instance != null && SexSimulationManager.Instance.HasBulletsRemaining())
        {
            // Has bullets - return to the active position's initial state.
            SetState(currentPositionConfig?.GetInitialState() ?? SimulationState.Idle);
        }
        else
        {
            // No bullets - stay in Transitioning state (only Finish button enabled)
            SetState(SimulationState.Transitioning);
        }
    }

    #endregion

    #region Animation Helpers
    
    public void TriggerAnimation(string triggerName)
    {
        if (motionController == null) return;
        motionController.SetTrigger(triggerName);
    }

    public void ResetAnimationTrigger(string triggerName)
    {
        if (motionController == null) return;
        motionController.ResetTrigger(triggerName);
    }

    public void SetBool(string paramName, bool value)
    {
        if (motionController == null) return;
        motionController.SetBool(paramName, value);
    }

    public void SetFloat(string paramName, float value)
    {
        if (motionController == null) return;
        motionController.SetFloat(paramName, value);
    }
    
    /// <summary>
    /// Get animation value based on player's skill level
    /// Level 1-2 = 1.5, Level 3-4 = 3.5, Level 5+ = 5.0
    /// </summary>
    public float GetSkillAnimationValue(SkillType skillType)
    {
        // OPT-42: Use cached player reference instead of 3-deep null-conditional chain
        if (_cachedPlayer == null)
        {
            _cachedPlayer = GameManager.Instance?.DataManager?.GetPlayer() as Player;
        }
        if (_cachedPlayer == null) return 1.5f;
        
        ISkill skill = _cachedPlayer.GetSkill(skillType);
        if (skill == null) return 1.5f;
        
        int level = skill.SkillLevel();
        
        if (level <= 2) return 1.5f;
        if (level <= 4) return 3.5f;
        return 5.0f;
    }
    
    #endregion

    #region Public API
    
    /// <summary>
    /// Get current simulation state
    /// </summary>
    public SimulationState CurrentState => currentState;
    
    /// <summary>
    /// Queue a thrust action to execute when the insert animation completes.
    /// Called by position configs when player clicks Slow/Fast before insert animation finishes.
    /// </summary>
    public void QueuePendingThrust(bool isFast)
    {
        hasPendingThrust = true;
        pendingThrustIsFast = isFast;
    }

    /// <summary>
    /// Clears a queued Slow/Fast input so it cannot leak into a later insert.
    /// </summary>
    public void ClearPendingThrust()
    {
        hasPendingThrust = false;
        pendingThrustIsFast = false;
    }
    
    #endregion
}

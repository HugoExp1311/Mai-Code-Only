using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Base;
using Base.Character;
using Base.Character.Action;
using Base.Character.Skills;
using Base.Character.Stats;
using UI.SexPosition;

/// <summary>
/// Manages sex simulation mechanics: stamina consumption, cum bar, orgasm tracking
/// </summary>
/// </summary>
public class SexSimulationManager : MonoBehaviour
{
    #region Singleton
    public static SexSimulationManager Instance { get; private set; }
    public bool IsMissionaryPlaybackActive =>
        currentPositionType == SexPositionConfigType.Missionary && isThrusting;
    public bool IsCowgirlPlaybackActive =>
        currentPositionType == SexPositionConfigType.Cowgirl && isThrusting;
    public bool IsCowgirlCondomActive => cowgirlCondomActive;
    public SexPositionConfigType CurrentPositionType => currentPositionType;
    public bool IsRoleplayPlaybackActive =>
        IsRoleplayPosition(currentPositionType) && isThrusting;
    public bool IsRoleplayPussyPlaybackActive =>
        currentPositionType == SexPositionConfigType.RoleplayPussy && isThrusting;
    public bool IsRoleplayButtholePlaybackActive =>
        currentPositionType == SexPositionConfigType.RoleplayButthole && isThrusting;
    public bool IsRoleplayBlowjobPlaybackActive =>
        currentPositionType == SexPositionConfigType.RoleplayBlowjob && isThrusting;
    public bool IsRoleplayPaizuriPlaybackActive =>
        currentPositionType == SexPositionConfigType.RoleplayPaizuri && isThrusting;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    #endregion

    #region Serialized Fields
    [Header("Simulation Settings")]
    [Tooltip("Duration of Outside button availability (seconds)")]
    [SerializeField] private float outsideButtonDuration = 2f;
    #endregion

    #region Private Fields
    // Tracking bars (0-100)
    private float playerCumBar = 0f;
    private float maiOrgasmBar = 0f;
    
    // Current state
    private bool isThrusting = false;
    private bool isFastSpeed = false;
    private SkillType currentHole = SkillType.F; // F = Pussy, A = Butthole
    private SexPositionConfigType currentPositionType = SexPositionConfigType.Missionary;
    private RoleplayCategory currentRoleplayCategory = RoleplayCategory.Hand;
    private bool cowgirlCondomActive;
    
    // Bullet tracking
    private int currentBullets = 0;
    
    // Session tracking data
    private SexSessionData sessionData = new SexSessionData();
    
    // Outside button timer
    private Coroutine outsideButtonTimerCoroutine;
    
    // OPT-27: Cached references to avoid repeated lookup chains per thrust
    private Player _cachedPlayer;
    private Target _cachedMai;
    
    // Events
    public System.Action OnPlayerCumReached;
    public System.Action OnMaiOrgasmReached;
    public System.Action OnStaminaDepleted;
    public System.Action OnBulletsEmpty;
    public System.Action OnCumOutsideAnimationComplete;
    public System.Action OnCumInsideAnimationComplete;
    public System.Action OnPulloutAnimationComplete;
    public System.Action OnInsertAnimationComplete;
    // Roleplay: fired when the active Animator has actually returned to Start.
    public System.Action OnRoleplayStartEntered;
    // Roleplay Pussy: fired when the delayed Egg Work loop actually begins.
    public System.Action OnRoleplayEggWorkEntered;
    // Roleplay Butthole: fired when the inserted Egg reaches its loop.
    public System.Action OnRoleplayEggLoopEntered;
    // Roleplay: fired when a looping terminal state is entered.
    public System.Action OnRoleplayAfterCummingEntered;
    // Cowgirl: forwarded Animator state entries and guarded loop events.
    public System.Action<CowgirlAnimationSignal> OnCowgirlAnimationSignal;
    #endregion

    #region Initialization
    private void Start()
    {
        ResetSimulation();
    }
    #endregion

    #region Public API
    /// <summary>
    /// Start thrusting simulation
    /// </summary>
    public void StartThrusting(SkillType hole, bool isFast)
    {
        isThrusting = true;
        isFastSpeed = isFast;
        currentHole = hole;
        
        // Update bullet count from player skill
        UpdateBulletCount();

    }

    /// <summary>
    /// Starts an active Paizuri loop and retains its semantic category for
    /// operation tracking and the Player/Mai bar split.
    /// </summary>
    public void StartRoleplayPaizuriPlayback(
        RoleplayCategory category,
        bool isFast)
    {
        currentRoleplayCategory = category;
        SkillType orgasmSkill = category == RoleplayCategory.Tongue
            ? SkillType.Tongue
            : SkillType.Hand;
        StartThrusting(orgasmSkill, isFast);
    }
    
    /// <summary>
    /// Stop thrusting simulation
    /// </summary>
    public void StopThrusting()
    {
        isThrusting = false;
    }

    /// <summary>
    /// A Cowgirl condom is consumed once and remains active until ResetSimulation.
    /// The position config snapshots this value at penetration start.
    /// </summary>
    public bool CanActivateCowgirlCondom()
    {
        return currentPositionType == SexPositionConfigType.Cowgirl &&
               !cowgirlCondomActive;
    }

    public bool TryActivateCowgirlCondom()
    {
        if (!CanActivateCowgirlCondom())
        {
            return false;
        }

        cowgirlCondomActive = true;
        return true;
    }
    
    /// <summary>
    /// Called by animation events on each thrust
    /// </summary>
    public void OnThrust()
    {
        if (!isThrusting)
        {

            return;
        }

        // Track thrust for current hole and increment operation counts.
        if (currentHole == SkillType.F)
        {
            sessionData.pussyThrustCount++;
            TrackSensitiveOperation(SensitiveBodyPart.Pussy);
        }
        else if (currentHole == SkillType.A)
        {
            sessionData.buttholeThrustCount++;
            TrackSensitiveOperation(SensitiveBodyPart.Butthole);
        }
        
        // Don't consume stamina if player cum bar is at 100% (during cum decision window)
        if (playerCumBar < 100f)
        {
            // Calculate stamina consumption
            ProcessStaminaConsumption();
            if (!isThrusting)
            {
                return;
            }
        }
        
        // Calculate cum bar increase (only if not already at 100%)
        if (playerCumBar < 100f)
        {
            ProcessCumBarIncrease();
        }
        
        // Calculate orgasm bar increase (continues during cum decision)
        ProcessOrgasmBarIncrease();
    }

    /// <summary>
    /// Apply orgasm bar progress from a foreplay action that uses Hand or Tongue.
    /// The current skill value maps directly to percentage points gained.
    /// </summary>
    public void ApplyForeplayOrgasmBarIncrease(SkillType foreplaySkill)
    {
        if (!IsForeplaySkill(foreplaySkill))
        {
            return;
        }

        if (foreplaySkill == SkillType.Hand)
        {
            TrackSensitiveOperation(SensitiveBodyPart.Boobs);
        }
        else if (foreplaySkill == SkillType.Tongue)
        {
            TrackSensitiveOperation(SensitiveBodyPart.Mouth);
        }

        TryIncreaseMaiOrgasmBar(GetSkillBasedOrgasmIncrease(foreplaySkill));
    }

    /// <summary>
    /// Reset simulation state (called when starting new sequence)
    /// Resets Mai's Libido to 0 and marks sex as used for the day
    /// </summary>
    public void ResetSimulation()
    {
        isThrusting = false;
        isFastSpeed = false;
        currentHole = SkillType.F;
        currentRoleplayCategory = RoleplayCategory.Hand;
        cowgirlCondomActive = false;
        playerCumBar = 0f;
        maiOrgasmBar = 0f;
        CancelOutsideButtonTimer();
        
        // OPT-27: Cache references at session start
        _cachedPlayer = GameManager.Instance?.DataManager?.GetPlayer() as Player;
        _cachedMai = GameManager.Instance?.DataManager?.GetCurrentBoss() as Target;
        
        // Get bullet count from player's Bullet skill (not hardcoded)
        UpdateBulletCount();
        
        // Reset session tracking
        sessionData.Reset();
        
        // Reset Mai's Libido to 0 (player is having sex)
        if (_cachedMai != null)
        {
            _cachedMai.ResetLibido();
#if UNITY_EDITOR
            Debug.Log("[SexSim] Reset Mai's Libido to 0");
#endif
        }
        
        // Mark sex as used for today
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MarkSexUsed();
#if UNITY_EDITOR
            Debug.Log("[SexSim] Marked sex as used for today");
#endif
        }
    }

    /// <summary>
    /// Starts the penetration phase without discarding rewards accumulated during
    /// the preceding Roleplay opener.
    /// </summary>
    public void BeginSexPhase()
    {
        isThrusting = false;
        isFastSpeed = false;
        currentHole = SkillType.F;
        currentRoleplayCategory = RoleplayCategory.Hand;
        playerCumBar = 0f;
        maiOrgasmBar = 0f;
        RestorePlayerStamina();
        UpdateBulletCount();
    }
    
    /// <summary>
    /// Start the 2-second outside button timer
    /// </summary>
    public void StartOutsideButtonTimer(System.Action onTimerExpired)
    {
        if (outsideButtonTimerCoroutine != null)
        {
            StopCoroutine(outsideButtonTimerCoroutine);
        }
        
        outsideButtonTimerCoroutine = StartCoroutine(OutsideButtonTimerCoroutine(onTimerExpired));
    }
    
    /// <summary>
    /// Cancel the outside button timer (player pressed button)
    /// </summary>
    public void CancelOutsideButtonTimer()
    {
        if (outsideButtonTimerCoroutine != null)
        {
            StopCoroutine(outsideButtonTimerCoroutine);
            outsideButtonTimerCoroutine = null;
        }
    }
    
    /// <summary>
    /// Consume one bullet (called after cum animation completes)
    /// </summary>
    public void ConsumeBullet()
    {
        if (currentBullets > 0)
        {
            currentBullets--;
            
            if (currentBullets <= 0)
            {
                OnBulletsEmpty?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Get current bullet count
    /// </summary>
    public int GetCurrentBullets() => currentBullets;
    
    /// <summary>
    /// Check if player has bullets remaining
    /// </summary>
    public bool HasBulletsRemaining() => currentBullets > 0;
    
    /// <summary>
    /// Get Mai's orgasm bar (0-100)
    /// </summary>
    public float GetMaiOrgasmBar() => maiOrgasmBar;
    
    /// <summary>
    /// Get player's cum bar (0-100)
    /// </summary>
    public float GetPlayerCumBar() => playerCumBar;
    
    /// <summary>
    /// Get player's current stamina
    /// </summary>
    public int GetPlayerStamina()
    {
        return _cachedPlayer?.GetStamina() ?? 0;
    }
    
    /// <summary>
    /// Get player's max stamina
    /// </summary>
    public int GetPlayerMaxStamina()
    {
        return _cachedPlayer?.GetMaxStamina() ?? 0;
    }
    
    /// <summary>
    /// Called by animation event when Cum Outside animation completes
    /// </summary>
    public void OnCumOutsideComplete()
    {
        ConsumeBullet();
        
        // Reset cum bar for next round
        playerCumBar = 0f;
        
        OnCumOutsideAnimationComplete?.Invoke();
    }
    
    /// <summary>
    /// Called by animation event when Cum Inside (CD) animation completes
    /// </summary>
    public void OnCumInsideComplete(bool usedCondom = false)
    {
        if (!usedCondom)
        {
            sessionData.cumInsideCount++;
        }
        ConsumeBullet();
        
        // Reset cum bar for next round
        playerCumBar = 0f;
        
        OnCumInsideAnimationComplete?.Invoke();
    }
    
    /// <summary>
    /// Called by animation event when Pullout animation completes
    /// </summary>
    public void OnPulloutComplete()
    {
        // Reset cum bar for next round
        playerCumBar = 0f;
        
        OnPulloutAnimationComplete?.Invoke();
    }
    
    /// <summary>
    /// Called by animation event when Insert animation completes
    /// </summary>
    public void OnInsertComplete()
    {
        OnInsertAnimationComplete?.Invoke();
    }

    /// <summary>
    /// Roleplay per-cycle progress for every active loop. Stamina always uses
    /// the Missionary formula. Paizuri then splits bar ownership by category:
    /// Boob advances Player Cum, while Hand/Tongue advance Mai Cum.
    /// </summary>
    public void OnRoleplayLoopComplete()
    {
        if (!IsRoleplayPlaybackActive)
        {
            return;
        }

        if (currentPositionType == SexPositionConfigType.RoleplayPaizuri)
        {
            TrackPaizuriOperation(currentRoleplayCategory);
        }
        else
        {
            TrackSensitiveOperation(GetRoleplaySensitiveBodyPart(currentPositionType));
        }

        bool isPaizuri =
            currentPositionType == SexPositionConfigType.RoleplayPaizuri;
        bool advancesPlayerCum =
            !isPaizuri || currentRoleplayCategory == RoleplayCategory.Boob;
        bool advancesMaiOrgasm =
            !isPaizuri || currentRoleplayCategory != RoleplayCategory.Boob;

        ProcessStaminaConsumption();
        if (!isThrusting)
        {
            return;
        }

        if (advancesPlayerCum && playerCumBar < 100f)
        {
            ProcessCumBarIncrease();
        }

        if (advancesMaiOrgasm)
        {
            ProcessOrgasmBarIncrease(
                () => TrackRoleplayOrgasm(currentPositionType));
        }
    }

    // Compatibility entry points retained for the already-authored Pussy and
    // Butthole animation events. All now use the same accounting path.
    public void OnRoleplayThrust() => OnRoleplayLoopComplete();

    public void OnRoleplayToyLoopComplete()
    {
        OnRoleplayLoopComplete();
    }

    public void OnRoleplayButtholeEggLoopComplete()
    {
        OnRoleplayLoopComplete();
    }

    /// <summary>
    /// Called when the Animator enters either looping Roleplay after-cumming state.
    /// The panel remains there until the player explicitly presses Stop.
    /// </summary>
    public void NotifyRoleplayAfterCummingEntered()
    {
        OnRoleplayAfterCummingEntered?.Invoke();
    }

    public void NotifyRoleplayStartEntered()
    {
        OnRoleplayStartEntered?.Invoke();
    }

    public void NotifyRoleplayEggWorkEntered()
    {
        OnRoleplayEggWorkEntered?.Invoke();
    }

    public void NotifyRoleplayEggLoopEntered()
    {
        OnRoleplayEggLoopEntered?.Invoke();
    }

    public void NotifyCowgirlAnimationSignal(CowgirlAnimationSignal signal)
    {
        if (currentPositionType != SexPositionConfigType.Cowgirl)
        {
            return;
        }

        OnCowgirlAnimationSignal?.Invoke(signal);
    }

    /// <summary>
    /// Calculate and apply all rewards accumulated during the sex session
    /// Called when player confirms exit via Finish button
    /// </summary>
    public void CalculateAndApplySessionRewards()
    {
        // Use cached references (populated in ResetSimulation)
        if (_cachedMai == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[SexSim] Cannot apply rewards - Mai not found");
#endif
            return;
        }
        
        if (_cachedPlayer == null)
        {
#if UNITY_EDITOR
            Debug.LogError("[SexSim] Cannot apply rewards - Player not found");
#endif
            return;
        }
        
        // === Apply Sensitive Points to Mai ===
        int boobsSP = sessionData.GetBoobsSensitivePoints(_cachedMai);
        if (boobsSP > 0)
        {
            _cachedMai.AddSensitivePoints(SensitiveBodyPart.Boobs, boobsSP);
        }
        
        int mouthSP = sessionData.GetMouthSensitivePoints(_cachedMai);
        if (mouthSP > 0)
        {
            _cachedMai.AddSensitivePoints(SensitiveBodyPart.Mouth, mouthSP);
        }
        
        int pussySP = sessionData.GetPussySensitivePoints(_cachedMai);
        if (pussySP > 0)
        {
            _cachedMai.AddSensitivePoints(SensitiveBodyPart.Pussy, pussySP);
        }
        
        int buttholeSP = sessionData.GetButtholeSensitivePoints(_cachedMai);
        if (buttholeSP > 0)
        {
            _cachedMai.AddSensitivePoints(SensitiveBodyPart.Butthole, buttholeSP);
        }

        if (sessionData.cumInsideCount > 0)
        {
            _cachedMai.RecordCumInsideNoCondom(sessionData.cumInsideCount);
        }
        
        // === Calculate Mai's Satisfaction / Lewd-Level-based bonuses ===
        int maiLewdLevel = _cachedMai.GetLewdLevel();

        // === Apply Sex Points to Player ===
        int totalSexPoints = sessionData.CalculateTotalSexPointsForLewdLevel(maiLewdLevel);
        if (totalSexPoints > 0)
        {
            _cachedPlayer.DoAction(new CharacterActions.ChangeResource(
                RewardTarget.Player,
                BasicResource.SkillPoint,
                totalSexPoints
            ));
        }
        
        // === Calculate Mai's Satisfaction ===
        int totalOrgasms = sessionData.GetTotalOrgasms();
        int requiredOrgasms = DefaultSettings.SatisfactionOrgasmRequirements.TryGetValue(maiLewdLevel, out int req) ? req : 2;
        bool isSatisfied = totalOrgasms >= requiredOrgasms;
        
        // Calculate love points change based on satisfaction
        int loveChange;
        if (isSatisfied)
        {
            loveChange = DefaultSettings.SatisfactionLoveRewards.TryGetValue(maiLewdLevel, out int reward) ? reward : 10;
        }
        else
        {
            loveChange = DefaultSettings.SatisfactionLovePenalties.TryGetValue(maiLewdLevel, out int penalty) ? penalty : -5;
        }
        
        // Store love points change for result panel display
        sessionData.lovePointsChange = loveChange;
        
        // Apply love points change to Mai
        if (loveChange != 0)
        {
            _cachedMai.DoAction(new CharacterActions.ChangeStat(
                RewardTarget.Mai,
                BasicStats.Love,
                loveChange
            ));
        }
        
#if UNITY_EDITOR
        Debug.Log($"[SexSim] Satisfaction: {(isSatisfied ? "MET" : "NOT MET")} (Orgasms: {totalOrgasms}/{requiredOrgasms}, Lewd Level: {maiLewdLevel})");
        Debug.Log($"[SexSim] Love Points Change: {(loveChange >= 0 ? "+" : "")}{loveChange}");
        Debug.Log($"[SexSim] {sessionData.GetSummary(maiLewdLevel, _cachedMai)}");
#endif
    }

    /// <summary>
    /// Set the current sex position type (called by position config when position changes)
    /// </summary>
    public void SetCurrentPosition(SexPositionConfigType positionType)
    {
        currentPositionType = positionType;
#if UNITY_EDITOR
        Debug.Log($"[SexSim] Position set to: {positionType}");
#endif
    }
    
    /// <summary>
    /// Get current session data for result display
    /// </summary>
    public SexSessionData GetSessionData() => sessionData;
    #endregion

    #region Private Methods
    private void ProcessStaminaConsumption()
    {
        if (_cachedPlayer == null) return;
        
        ISkill sizeSkill = _cachedPlayer.GetSkill(SkillType.Size);
        int sizeValue = sizeSkill?.SkillValue() ?? 100;
        
        float staminaCost = isFastSpeed ? 2f : 1f;
        staminaCost *= (sizeValue / 1000f);
        
        int staminaCostInt = Mathf.CeilToInt(staminaCost);
        var changeStatAction = new CharacterActions.ChangeStat(
            RewardTarget.Player, 
            BasicStats.Stamina, 
            -staminaCostInt
        );
        _cachedPlayer.DoAction(changeStatAction);
        
        if (_cachedPlayer.GetStamina() <= 0)
        {
            StopThrusting();
            OnStaminaDepleted?.Invoke();
        }
    }
    
    private void ProcessCumBarIncrease(System.Action onCumReached = null)
    {
        if (_cachedPlayer == null) return;
        
        ISkill cumSkill = _cachedPlayer.GetSkill(SkillType.Cum);
        int cumValue = cumSkill?.SkillValue() ?? 50;
        
        float cumIncrease = isFastSpeed ? 2f : 1f;
        cumIncrease *= (cumValue / 10f);
        
        TryIncreasePlayerCumBar(cumIncrease, onCumReached);
    }

    private void TryIncreasePlayerCumBar(
        float increase,
        System.Action onCumReached = null)
    {
        if (increase <= 0f)
        {
            return;
        }

        playerCumBar += increase;
        if (playerCumBar < 100f)
        {
            return;
        }

        playerCumBar = 100f;
        StopThrusting();
        onCumReached?.Invoke();
        OnPlayerCumReached?.Invoke();
    }
    
    private void ProcessOrgasmBarIncrease(System.Action onOrgasm = null)
    {
        TryIncreaseMaiOrgasmBar(
            GetSkillBasedOrgasmIncrease(currentHole),
            onOrgasm ?? TrackCurrentSexOrgasm);
    }

    private float GetSkillBasedOrgasmIncrease(SkillType skillType, int defaultSkillValue = 5)
    {
        if (_cachedPlayer == null)
        {
            return 0f;
        }

        ISkill skill = _cachedPlayer.GetSkill(skillType);
        int skillValue = skill?.SkillValue() ?? defaultSkillValue;
        return skillValue / 10f;
    }

    private void TryIncreaseMaiOrgasmBar(float increasePercent, System.Action onOrgasm = null)
    {
        if (increasePercent <= 0f)
        {
            return;
        }

        maiOrgasmBar += increasePercent;

        if (maiOrgasmBar < 100f)
        {
            return;
        }

        maiOrgasmBar = 0f;
        OnMaiOrgasmReached?.Invoke();
        onOrgasm?.Invoke();
        ApplyMaiOrgasmRewards();
    }

    private void TrackCurrentSexOrgasm()
    {
        switch (currentPositionType)
        {
            case SexPositionConfigType.Missionary:
                sessionData.missionaryOrgasmCount++;
                break;
            case SexPositionConfigType.Doggy:
                sessionData.doggyOrgasmCount++;
                break;
            case SexPositionConfigType.Cowgirl:
                sessionData.cowgirlOrgasmCount++;
                break;
            default:
                sessionData.missionaryOrgasmCount++;
                break;
        }
    }

    private void TrackRoleplayOrgasm(SexPositionConfigType positionType)
    {
        switch (positionType)
        {
            case SexPositionConfigType.RoleplayPussy:
                sessionData.roleplayPussyOrgasmCount++;
                break;
            case SexPositionConfigType.RoleplayButthole:
                sessionData.roleplayButtholeOrgasmCount++;
                break;
            case SexPositionConfigType.RoleplayBlowjob:
                sessionData.roleplayBlowjobOrgasmCount++;
                break;
            case SexPositionConfigType.RoleplayPaizuri:
                sessionData.roleplayPaizuriOrgasmCount++;
                break;
        }
    }

    private void TrackPaizuriOperation(RoleplayCategory category)
    {
        switch (category)
        {
            case RoleplayCategory.Boob:
                TrackSensitiveOperation(SensitiveBodyPart.Boobs);
                break;
            case RoleplayCategory.Hand:
                sessionData.handOperationCount++;
                break;
            case RoleplayCategory.Tongue:
                TrackSensitiveOperation(SensitiveBodyPart.Mouth);
                break;
        }
    }

    private static SensitiveBodyPart GetRoleplaySensitiveBodyPart(
        SexPositionConfigType positionType)
    {
        return positionType switch
        {
            SexPositionConfigType.RoleplayButthole => SensitiveBodyPart.Butthole,
            SexPositionConfigType.RoleplayBlowjob => SensitiveBodyPart.Mouth,
            _ => SensitiveBodyPart.Pussy
        };
    }

    private void TrackSensitiveOperation(SensitiveBodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case SensitiveBodyPart.Boobs:
                sessionData.boobsOperationCount++;
                break;
            case SensitiveBodyPart.Mouth:
                sessionData.mouthOperationCount++;
                break;
            case SensitiveBodyPart.Pussy:
                sessionData.pussyOperationCount++;
                break;
            case SensitiveBodyPart.Butthole:
                sessionData.buttholeOperationCount++;
                break;
        }
    }

    private static bool IsForeplaySkill(SkillType skillType)
    {
        return skillType is SkillType.Hand or SkillType.Tongue;
    }

    private static bool IsRoleplayPosition(SexPositionConfigType positionType)
    {
        return positionType == SexPositionConfigType.RoleplayPussy ||
               positionType == SexPositionConfigType.RoleplayButthole ||
               positionType == SexPositionConfigType.RoleplayBlowjob ||
               positionType == SexPositionConfigType.RoleplayPaizuri;
    }

    private void RestorePlayerStamina()
    {
        if (_cachedPlayer == null)
        {
            _cachedPlayer = GameManager.Instance?.DataManager?.GetPlayer() as Player;
        }

        if (_cachedPlayer == null)
        {
            return;
        }

        int missingStamina = _cachedPlayer.GetMaxStamina() - _cachedPlayer.GetStamina();
        if (missingStamina <= 0)
        {
            return;
        }

        _cachedPlayer.DoAction(new CharacterActions.ChangeStat(
            RewardTarget.Player,
            BasicStats.Stamina,
            missingStamina));
    }
    
    private void ApplyMaiOrgasmRewards()
    {
        if (_cachedMai == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[SexSim] Cannot apply orgasm rewards - Mai not found");
#endif
            return;
        }
        
        _cachedMai.DoAction(new CharacterActions.ChangeStat(
            RewardTarget.Mai,
            BasicStats.Love,
            5
        ));
        
#if UNITY_EDITOR
        Debug.Log("[SexSim] Mai orgasm rewards applied: +5 Love");
#endif
    }
    
    private void UpdateBulletCount()
    {
        if (_cachedPlayer != null)
        {
            ISkill bulletSkill = _cachedPlayer.GetSkill(SkillType.Bullet);
            if (bulletSkill != null)
            {
                currentBullets = bulletSkill.SkillValue();
            }
        }
    }
    
    private IEnumerator OutsideButtonTimerCoroutine(System.Action onTimerExpired)
    {
        yield return new WaitForSeconds(outsideButtonDuration);
        
        // Timer expired, invoke callback
        onTimerExpired?.Invoke();
        
        outsideButtonTimerCoroutine = null;
    }
    #endregion
}

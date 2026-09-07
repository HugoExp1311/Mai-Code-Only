using Base;
using Base.Character;
using Base.Character.Stats;
using Events;
using EventBus;
using UnityEngine;
using Base.Localization;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Mai profile panel for In Game section that displays Mai's general stats
/// Shows Mai's Love and Libido stats using localized Smart Strings
/// Inherits from UIPanel to integrate with the panel management system
/// </summary>
public class MaiProfilePanel : UIPanel
{
    [Header("Mai General Stats - Localized")]
    [SerializeField] private LocalizedText loveLocalizedText;
    [SerializeField] private LocalizedText loveLevelLocalizedText;
    [SerializeField] private LocalizedText libidoLocalizedText;

    [Header("Love Level Bar")]
    [Tooltip("Filled Image (Bar_Love_Level_2, pink) whose fillAmount shows progress toward the next Love Level.")]
    [SerializeField] private Image loveBarFillImage;

    [Header("Mai Profile Image")]
    [SerializeField] private Image maiProfileImage;
    [SerializeField] private Sprite normalProfileSprite;
    [SerializeField] private Sprite maxLibidoProfileSprite;

    // Localization key prefix for the 5 derived Libido states (0..4).
    private const string LibidoStateKeyPrefix = "Mai Profile Libido State ";

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<StatsChangedEvent> statsChangedBinding;
    private int _prevProfileImageState = -1;

    // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
    private void Awake()
    {
        AutoBindProfileImage();

        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        statsChangedBinding = new EventBinding<StatsChangedEvent>(HandleStatsChanged);
    }

    private void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<StatsChangedEvent>.Register(statsChangedBinding);

        // Initial update if DataManager exists
        if (GameManager.Instance?.DataManager != null)
        {
            UpdateStats();
        }
    }

    private void OnDisable()
    {
        // OPT-49: Deregister only — bindings are reused, not reallocated
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
    }

    private void OnDestroy()
    {
        // Ensure bindings are deregistered on destroy
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);

        // Update UI when panel is shown
        UpdateStats();
    }

    /// <summary>
    /// Handle game start event
    /// </summary>
    private void HandleGameStart(GameStartEvent args)
    {
        UpdateStats();
    }

    /// <summary>
    /// Handle place changed event
    /// </summary>
    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        UpdateStats();
    }

    /// <summary>
    /// Handle stats changed event
    /// </summary>
    private void HandleStatsChanged(StatsChangedEvent args)
    {
        // Only update if the stat change is for Mai
        if (args.Target == RewardTarget.Mai)
        {
            UpdateStats();
        }
    }

    /// <summary>
    /// Update all general stats display using Smart String localization
    /// Uses "Mai Profile Love" and "Mai Profile Libido" keys with {amount} variable
    /// </summary>
    private void UpdateStats()
    {
        if (GameManager.Instance?.DataManager != null)
        {
            ITarget mai = GameManager.Instance.DataManager.GetCurrentBoss();
            if (mai != null && mai is Target target)
            {
                int love = target.GetLove();
                int libidoDays = target.GetLibido();

                SetStatText(loveLocalizedText, "Mai Profile Love", love);
                SetLoveLevelText(target.GetLoveLevel());
                UpdateLoveBar(love);
                SetLibidoStateText(target.GetLibidoState());
                UpdateProfileImage(libidoDays);
            }
            else
            {
                // Set defaults if target is null
                SetDefaultValues();
            }
        }
        else
        {
            // Set defaults if DataManager is null
            SetDefaultValues();
        }
    }

    /// <summary>
    /// Set default values for all stats (fresh game: 0 Love, Level 1, "Not interested").
    /// </summary>
    private void SetDefaultValues()
    {
        SetStatText(loveLocalizedText, "Mai Profile Love", 0);
        SetLoveLevelText(DefaultSettings.DefaultLoveLevel);
        UpdateLoveBar(DefaultSettings.DefaultLove);
        SetLibidoStateText(DefaultSettings.GetLibidoState(DefaultSettings.DefaultLibido));
        UpdateProfileImage(DefaultSettings.DefaultLibido);
    }

    /// <summary>
    /// Display the derived Love Level (e.g. "Level 3") via the "Mai Profile Love Level" key.
    /// </summary>
    private void SetLoveLevelText(int level)
    {
        if (loveLevelLocalizedText == null)
            return;

        loveLevelLocalizedText.SetLocalized("Mai Profile Love Level", LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["level"] = level
        });
    }

    /// <summary>
    /// Display the current Libido as one of 5 named states via
    /// "Mai Profile Libido State {index}" keys (no {amount} variable).
    /// </summary>
    private void SetLibidoStateText(int stateIndex)
    {
        if (libidoLocalizedText == null)
            return;

        libidoLocalizedText.SetLocalized(LibidoStateKeyPrefix + stateIndex, LocalizationDomains.UI);
    }

    /// <summary>
    /// Set the pink Love bar fill to the fraction of progress toward the next Love Level.
    /// </summary>
    private void UpdateLoveBar(int lovePoints)
    {
        if (loveBarFillImage == null)
            return;

        loveBarFillImage.fillAmount = DefaultSettings.GetLoveLevelProgress(lovePoints);
    }

    private void SetStatText(LocalizedText target, string key, int amount)
    {
        if (target == null)
            return;

        target.SetLocalized(key, LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["amount"] = amount
        });
    }

    private void AutoBindProfileImage()
    {
        if (maiProfileImage == null)
        {
            Transform profileMai = transform.Find("Profile/Mai");
            if (profileMai != null)
            {
                profileMai.TryGetComponent(out maiProfileImage);
            }
        }

        if (normalProfileSprite == null && maiProfileImage != null)
        {
            normalProfileSprite = maiProfileImage.sprite;
        }
    }

    private void UpdateProfileImage(int libido)
    {
        if (maiProfileImage == null)
        {
            return;
        }

        bool isMaxLibido = libido >= DefaultSettings.MaxLibido;
        int imageState = isMaxLibido ? DefaultSettings.MaxLibido : DefaultSettings.DefaultLibido;
        Sprite targetSprite = isMaxLibido ? maxLibidoProfileSprite : normalProfileSprite;
        if (targetSprite == null)
        {
            return;
        }

        if (_prevProfileImageState == imageState && maiProfileImage.sprite == targetSprite)
        {
            return;
        }

        maiProfileImage.sprite = targetSprite;
        _prevProfileImageState = imageState;
    }
}

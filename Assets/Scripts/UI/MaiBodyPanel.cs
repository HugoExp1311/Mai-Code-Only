using Base;
using Base.Character;
using Base.Character.Stats;
using Base.Localization;
using Events;
using EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Mai body panel for In Game section that displays body sensitive points,
/// Sensitive Levels, Lewd Level, and pregnancy status.
/// </summary>
public class MaiBodyPanel : UIPanel
{
    private const string ConfirmPopupPanelId = "Confirm Popup";

    [Header("Mai Sensitive Points")]
    [SerializeField] private LocalizedText boobsSensitivePointsText;
    [SerializeField] private LocalizedText mouthSensitivePointsText;
    [SerializeField] private LocalizedText pussySensitivePointsText;
    [SerializeField] private LocalizedText buttholeSensitivePointsText;

    [Header("Body Status")]
    [SerializeField] private LocalizedText lewdLevelText;
    [SerializeField] private LocalizedText pregnancyStatusText;

    [Header("Sensitive Values")]
    [FormerlySerializedAs("boobsSensitiveLevelText")]
    [SerializeField] private LocalizedText boobsSensitiveValueText;
    [FormerlySerializedAs("mouthSensitiveLevelText")]
    [SerializeField] private LocalizedText mouthSensitiveValueText;
    [FormerlySerializedAs("pussySensitiveLevelText")]
    [SerializeField] private LocalizedText pussySensitiveValueText;
    [FormerlySerializedAs("buttholeSensitiveLevelText")]
    [SerializeField] private LocalizedText buttholeSensitiveValueText;
    [FormerlySerializedAs("sensitiveBonusComparedToBaseText")]
    [FormerlySerializedAs("sensitiveBonusText")]
    [SerializeField] private LocalizedText sensitivePointsPerActivityText;

    [Header("Lewd Level Image")]
    [SerializeField] private Image maiLewdLevelImage;
    [SerializeField] private Sprite lewdLevel1Sprite;
    [SerializeField] private Sprite lewdLevel2Sprite;
    [SerializeField] private Sprite lewdLevel3Sprite;
    [SerializeField] private Sprite lewdLevel4Sprite;
    [SerializeField] private Sprite lewdLevel5Sprite;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button lewdLevelUpButton;
    [SerializeField] private Button boobsSensitiveLevelUpButton;
    [SerializeField] private Button mouthSensitiveLevelUpButton;
    [SerializeField] private Button pussySensitiveLevelUpButton;
    [SerializeField] private Button buttholeSensitiveLevelUpButton;

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<StatsChangedEvent> statsChangedBinding;
    private EventBinding<SensitivePointsDecayEvent> decayBinding;

    private string _prevBoobs;
    private string _prevMouth;
    private string _prevPussy;
    private string _prevButthole;
    private string _prevBoobsSensitiveValue;
    private string _prevMouthSensitiveValue;
    private string _prevPussySensitiveValue;
    private string _prevButtholeSensitiveValue;
    private string _prevLewdLevel;
    private string _prevPregnancyStatus;
    private string _prevSensitivePointsPerActivity;
    private int _prevLewdLevelImage = -1;

    private void Awake()
    {
        AutoBindExistingBodyStatusTexts();

        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        statsChangedBinding = new EventBinding<StatsChangedEvent>(HandleStatsChanged);
        decayBinding = new EventBinding<SensitivePointsDecayEvent>(_ => UpdateSensitivePoints());

        BindUpgradeButtons();
    }

    private void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<StatsChangedEvent>.Register(statsChangedBinding);
        EventBus<SensitivePointsDecayEvent>.Register(decayBinding);

        if (GameManager.Instance?.DataManager != null)
        {
            UpdateSensitivePoints();
        }
    }

    private void OnDisable()
    {
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<SensitivePointsDecayEvent>.Deregister(decayBinding);
    }

    private void OnDestroy()
    {
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<SensitivePointsDecayEvent>.Deregister(decayBinding);
        UnbindUpgradeButtons();
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);
        UpdateSensitivePoints();
    }

    public void OnLewdLevelUpClicked()
    {
        Target target = GetMaiTarget();
        if (target == null || target.IsMaxLewdLevel())
        {
            return;
        }

        int targetLevel = target.GetLewdLevel() + 1;
        if (!DefaultSettings.TryGetLewdLevelUpgradeRequirement(
            targetLevel,
            out int pussyButtholeCost,
            out int boobsMouthCost))
        {
            return;
        }

        var variables = new System.Collections.Generic.Dictionary<string, object>
        {
            ["level"] = targetLevel,
            ["boobsCost"] = boobsMouthCost,
            ["mouthCost"] = boobsMouthCost,
            ["pussyCost"] = pussyButtholeCost,
            ["buttholeCost"] = pussyButtholeCost
        };

        ShowUpgradeConfirm(
            "Body Lewd Level Upgrade Confirm",
            variables,
            "Body Level Up Button",
            ConfirmLewdLevelUpgrade);
    }

    public void OnBoobsSensitiveLevelUpClicked()
    {
        TryUpgradeSensitiveLevel(SensitiveBodyPart.Boobs);
    }

    public void OnMouthSensitiveLevelUpClicked()
    {
        TryUpgradeSensitiveLevel(SensitiveBodyPart.Mouth);
    }

    public void OnPussySensitiveLevelUpClicked()
    {
        TryUpgradeSensitiveLevel(SensitiveBodyPart.Pussy);
    }

    public void OnButtholeSensitiveLevelUpClicked()
    {
        TryUpgradeSensitiveLevel(SensitiveBodyPart.Butthole);
    }

    private void TryUpgradeSensitiveLevel(SensitiveBodyPart bodyPart)
    {
        Target target = GetMaiTarget();
        if (target == null || target.GetSensitiveLevel(bodyPart) >= DefaultSettings.MaxSensitiveLevel)
        {
            return;
        }

        int targetLevel = target.GetSensitiveLevel(bodyPart) + 1;
        int cost = DefaultSettings.GetSensitiveLevelUpgradeRequirement(targetLevel);
        if (cost < 0)
        {
            return;
        }

        ShowUpgradeConfirm(
            "Body Sensitive Level Upgrade Confirm",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["cost"] = cost
            },
            "Body Upgrade Button",
            () => ConfirmSensitiveLevelUpgrade(bodyPart));
    }

    private void ConfirmLewdLevelUpgrade()
    {
        Target target = GetMaiTarget();
        if (target == null)
        {
            return;
        }

        if (!target.TryUpgradeLewdLevel())
        {
            NoticeUI.ShowLocalized("Notice Not Enough Sensitive Points");
            return;
        }

        UpdateSensitivePoints();
    }

    private void ConfirmSensitiveLevelUpgrade(SensitiveBodyPart bodyPart)
    {
        Target target = GetMaiTarget();
        if (target == null)
        {
            return;
        }

        if (!target.TryUpgradeSensitiveLevel(bodyPart))
        {
            NoticeUI.ShowLocalized("Notice Not Enough Sensitive Points");
            return;
        }

        UpdateSensitivePoints();
        int gain = target.GetSensitivePointsPerInteraction(bodyPart);
        NoticeUI.ShowLocalized(
            "Notice Sensitive Level Upgrade Success",
            new System.Collections.Generic.Dictionary<string, object>
            {
                ["amount"] = gain
            });
    }

    private void ShowUpgradeConfirm(
        string messageKey,
        System.Collections.Generic.Dictionary<string, object> messageVariables,
        string yesTextKey,
        System.Action onConfirm)
    {
        if (UIPanelManager.Instance == null)
        {
            return;
        }

        var popupData = UI.ConfirmPopupData.CreateLocalized(
            messageKey,
            messageVariables,
            yesTextKey,
            onConfirm,
            showNoButton: false);

        UIPanelManager.Instance.ShowPanel(ConfirmPopupPanelId, popupData);
    }

    private void HandleGameStart(GameStartEvent args)
    {
        UpdateSensitivePoints();
    }

    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        UpdateSensitivePoints();
    }

    private void HandleStatsChanged(StatsChangedEvent args)
    {
        if (args.Target == RewardTarget.Mai)
        {
            UpdateSensitivePoints();
        }
    }

    private void UpdateSensitivePoints()
    {
        Target target = GetMaiTarget();
        if (target == null)
        {
            SetDefaultValues();
            return;
        }

        UpdateBodyPart(
            target,
            SensitiveBodyPart.Boobs,
            boobsSensitivePointsText,
            boobsSensitiveValueText,
            ref _prevBoobs,
            ref _prevBoobsSensitiveValue);
        UpdateBodyPart(
            target,
            SensitiveBodyPart.Mouth,
            mouthSensitivePointsText,
            mouthSensitiveValueText,
            ref _prevMouth,
            ref _prevMouthSensitiveValue);
        UpdateBodyPart(
            target,
            SensitiveBodyPart.Pussy,
            pussySensitivePointsText,
            pussySensitiveValueText,
            ref _prevPussy,
            ref _prevPussySensitiveValue);
        UpdateBodyPart(
            target,
            SensitiveBodyPart.Butthole,
            buttholeSensitivePointsText,
            buttholeSensitiveValueText,
            ref _prevButthole,
            ref _prevButtholeSensitiveValue);

        SetIntegerIfChanged(lewdLevelText, target.GetLewdLevel(), ref _prevLewdLevel);
        SetPregnancyStatusIfChanged(pregnancyStatusText, target.GetPregnancyChance(), ref _prevPregnancyStatus);
        bool showPerActivityText = !target.IsMaxLewdLevel();
        SetSensitivePointsPerActivityVisible(showPerActivityText);
        if (showPerActivityText)
        {
            SetSensitivePointsPerActivityIfChanged(
                sensitivePointsPerActivityText,
                target,
                ref _prevSensitivePointsPerActivity);
        }
        UpdateLewdLevelImage(target.GetLewdLevel());
        UpdateUpgradeButtons(target);
    }

    private void UpdateBodyPart(
        Target target,
        SensitiveBodyPart bodyPart,
        LocalizedText pointsText,
        LocalizedText valueText,
        ref string previousPoints,
        ref string previousValue)
    {
        int points = target.GetSensitivePoints(bodyPart);
        bool sharesTextField = pointsText != null && pointsText == valueText;

        if (valueText != null && !sharesTextField)
        {
            SetSensitivePointsIfChanged(valueText, points, target.IsMaxLewdLevel(), ref previousValue);
        }

        SetSensitivePointsIfChanged(pointsText, points, target.IsMaxLewdLevel(), ref previousPoints);
    }

    private Target GetMaiTarget()
    {
        return GameManager.Instance?.DataManager?.GetCurrentBoss() as Target;
    }

    private void SetDefaultValues()
    {
        SetDefaultBodyPartText(boobsSensitivePointsText, boobsSensitiveValueText, ref _prevBoobs, ref _prevBoobsSensitiveValue);
        SetDefaultBodyPartText(mouthSensitivePointsText, mouthSensitiveValueText, ref _prevMouth, ref _prevMouthSensitiveValue);
        SetDefaultBodyPartText(pussySensitivePointsText, pussySensitiveValueText, ref _prevPussy, ref _prevPussySensitiveValue);
        SetDefaultBodyPartText(buttholeSensitivePointsText, buttholeSensitiveValueText, ref _prevButthole, ref _prevButtholeSensitiveValue);
        SetIntegerIfChanged(lewdLevelText, DefaultSettings.DefaultLewdLevel, ref _prevLewdLevel);
        SetPregnancyStatusIfChanged(pregnancyStatusText, DefaultSettings.DefaultPregnancyChance, ref _prevPregnancyStatus);
        SetSensitivePointsPerActivityVisible(true);
        SetDefaultSensitivePointsPerActivityIfChanged(
            sensitivePointsPerActivityText,
            ref _prevSensitivePointsPerActivity);
        UpdateLewdLevelImage(DefaultSettings.DefaultLewdLevel);
        SetButtonState(lewdLevelUpButton, true, false);
        SetButtonState(boobsSensitiveLevelUpButton, true, false);
        SetButtonState(mouthSensitiveLevelUpButton, true, false);
        SetButtonState(pussySensitiveLevelUpButton, true, false);
        SetButtonState(buttholeSensitiveLevelUpButton, true, false);
    }

    private void SetDefaultBodyPartText(
        LocalizedText pointsText,
        LocalizedText valueText,
        ref string previousPoints,
        ref string previousValue)
    {
        bool sharesTextField = pointsText != null && pointsText == valueText;

        if (valueText != null && !sharesTextField)
        {
            SetSensitivePointsIfChanged(valueText, 0, false, ref previousValue);
        }

        SetSensitivePointsIfChanged(pointsText, 0, false, ref previousPoints);
    }

    private static int GetSensitivePointsPerActivity(Target target, SensitiveBodyPart bodyPart)
    {
        return target != null
            ? target.GetSensitivePointsPerInteraction(bodyPart)
            : DefaultSettings.GetSensitivePointsPerInteraction(DefaultSettings.DefaultSensitiveLevel);
    }

    private void SetSensitivePointsPerActivityIfChanged(LocalizedText text, Target target, ref string previousValue)
    {
        int boobs = GetSensitivePointsPerActivity(target, SensitiveBodyPart.Boobs);
        int mouth = GetSensitivePointsPerActivity(target, SensitiveBodyPart.Mouth);
        int pussy = GetSensitivePointsPerActivity(target, SensitiveBodyPart.Pussy);
        int butthole = GetSensitivePointsPerActivity(target, SensitiveBodyPart.Butthole);
        string cacheValue = $"{boobs}|{mouth}|{pussy}|{butthole}";
        if (text == null || previousValue == cacheValue)
            return;

        text.SetLocalized("Body Sensitive Points Per Activity Summary", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["boobs"] = boobs,
            ["mouth"] = mouth,
            ["pussy"] = pussy,
            ["butthole"] = butthole
        });
        previousValue = cacheValue;
    }

    private void SetDefaultSensitivePointsPerActivityIfChanged(LocalizedText text, ref string previousValue)
    {
        SetSensitivePointsPerActivityIfChanged(text, null, ref previousValue);
    }

    private void SetSensitivePointsPerActivityVisible(bool visible)
    {
        if (sensitivePointsPerActivityText != null
            && sensitivePointsPerActivityText.gameObject.activeSelf != visible)
        {
            sensitivePointsPerActivityText.gameObject.SetActive(visible);
        }
    }

    private void SetSensitivePointsIfChanged(LocalizedText text, int points, bool isMaxLewdLevel, ref string previousValue)
    {
        string cacheValue = isMaxLewdLevel ? "Max" : points.ToString();
        if (text == null || previousValue == cacheValue)
            return;

        if (isMaxLewdLevel)
        {
            text.SetLocalized("UI Value Max", LocalizationDomains.UI);
        }
        else
        {
            text.SetLocalized("UI Value Integer", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
            {
                ["amount"] = points
            });
        }

        previousValue = cacheValue;
    }

    private void SetIntegerIfChanged(LocalizedText text, int value, ref string previousValue)
    {
        string cacheValue = value.ToString();
        if (text == null || previousValue == cacheValue)
            return;

        text.SetLocalized("UI Value Integer", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["amount"] = value
        });
        previousValue = cacheValue;
    }

    private void SetPregnancyStatusIfChanged(LocalizedText text, int chance, ref string previousValue)
    {
        string cacheValue = chance.ToString();
        if (text == null || previousValue == cacheValue)
            return;

        if (chance >= DefaultSettings.MaxPregnancyChance)
        {
            text.SetLocalized("Body Pregnant", LocalizationDomains.UI);
        }
        else if (chance > 0)
        {
            text.SetLocalized("Body Not Pregnant Chance", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
            {
                ["chance"] = chance
            });
        }
        else
        {
            text.SetLocalized("Body Not Pregnant", LocalizationDomains.UI);
        }

        previousValue = cacheValue;
    }

    private void UpdateUpgradeButtons(Target target)
    {
        bool canShowLewdUpgrade = !target.IsMaxLewdLevel();
        SetButtonState(lewdLevelUpButton, canShowLewdUpgrade, canShowLewdUpgrade);
        UpdateSensitiveLevelButton(target, SensitiveBodyPart.Boobs, boobsSensitiveLevelUpButton);
        UpdateSensitiveLevelButton(target, SensitiveBodyPart.Mouth, mouthSensitiveLevelUpButton);
        UpdateSensitiveLevelButton(target, SensitiveBodyPart.Pussy, pussySensitiveLevelUpButton);
        UpdateSensitiveLevelButton(target, SensitiveBodyPart.Butthole, buttholeSensitiveLevelUpButton);
    }

    private void UpdateSensitiveLevelButton(Target target, SensitiveBodyPart bodyPart, Button button)
    {
        bool isMax = target.GetSensitiveLevel(bodyPart) >= DefaultSettings.MaxSensitiveLevel;
        SetButtonState(button, !isMax, !isMax);
    }

    private static void SetButtonState(Button button, bool visible, bool interactable)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(visible);
        button.interactable = interactable;
    }

    private void BindUpgradeButtons()
    {
        if (lewdLevelUpButton != null) lewdLevelUpButton.onClick.AddListener(OnLewdLevelUpClicked);
        if (boobsSensitiveLevelUpButton != null) boobsSensitiveLevelUpButton.onClick.AddListener(OnBoobsSensitiveLevelUpClicked);
        if (mouthSensitiveLevelUpButton != null) mouthSensitiveLevelUpButton.onClick.AddListener(OnMouthSensitiveLevelUpClicked);
        if (pussySensitiveLevelUpButton != null) pussySensitiveLevelUpButton.onClick.AddListener(OnPussySensitiveLevelUpClicked);
        if (buttholeSensitiveLevelUpButton != null) buttholeSensitiveLevelUpButton.onClick.AddListener(OnButtholeSensitiveLevelUpClicked);
    }

    private void UnbindUpgradeButtons()
    {
        if (lewdLevelUpButton != null) lewdLevelUpButton.onClick.RemoveListener(OnLewdLevelUpClicked);
        if (boobsSensitiveLevelUpButton != null) boobsSensitiveLevelUpButton.onClick.RemoveListener(OnBoobsSensitiveLevelUpClicked);
        if (mouthSensitiveLevelUpButton != null) mouthSensitiveLevelUpButton.onClick.RemoveListener(OnMouthSensitiveLevelUpClicked);
        if (pussySensitiveLevelUpButton != null) pussySensitiveLevelUpButton.onClick.RemoveListener(OnPussySensitiveLevelUpClicked);
        if (buttholeSensitiveLevelUpButton != null) buttholeSensitiveLevelUpButton.onClick.RemoveListener(OnButtholeSensitiveLevelUpClicked);
    }

    private void AutoBindExistingBodyStatusTexts()
    {
        if (maiLewdLevelImage == null)
        {
            maiLewdLevelImage = FindMaiLewdLevelImage();
        }

        if (boobsSensitivePointsText == null)
        {
            boobsSensitivePointsText = FindSiblingValueText("Body Boobs");
        }

        if (mouthSensitivePointsText == null)
        {
            mouthSensitivePointsText = FindSiblingValueText("Body Mouth");
        }

        if (pussySensitivePointsText == null)
        {
            pussySensitivePointsText = FindSiblingValueText("Body Pussy");
        }

        if (buttholeSensitivePointsText == null)
        {
            buttholeSensitivePointsText = FindSiblingValueText("Body Butthole");
        }

        boobsSensitiveValueText ??= boobsSensitivePointsText;
        mouthSensitiveValueText ??= mouthSensitivePointsText;
        pussySensitiveValueText ??= pussySensitivePointsText;
        buttholeSensitiveValueText ??= buttholeSensitivePointsText;

        if (lewdLevelText == null)
        {
            lewdLevelText = FindSiblingValueText("Body Lewd Level Title");
        }

        if (pregnancyStatusText == null)
        {
            pregnancyStatusText = FindSiblingValueText("Body Pregnancy Status");
        }

        if (sensitivePointsPerActivityText == null)
        {
            sensitivePointsPerActivityText = FindSensitivePointsPerActivityText();
        }
    }

    private void UpdateLewdLevelImage(int lewdLevel)
    {
        if (maiLewdLevelImage == null)
        {
            return;
        }

        int clampedLevel = Mathf.Clamp(lewdLevel, DefaultSettings.DefaultLewdLevel, DefaultSettings.MaxLewdLevel);
        Sprite sprite = GetLewdLevelSprite(clampedLevel);
        if (sprite == null)
        {
            return;
        }

        if (_prevLewdLevelImage == clampedLevel && maiLewdLevelImage.sprite == sprite)
        {
            return;
        }

        maiLewdLevelImage.sprite = sprite;
        _prevLewdLevelImage = clampedLevel;
    }

    private Sprite GetLewdLevelSprite(int lewdLevel)
    {
        switch (lewdLevel)
        {
            case 1:
                return lewdLevel1Sprite;
            case 2:
                return lewdLevel2Sprite;
            case 3:
                return lewdLevel3Sprite;
            case 4:
                return lewdLevel4Sprite;
            case 5:
                return lewdLevel5Sprite;
            default:
                return lewdLevel1Sprite;
        }
    }

    private Image FindMaiLewdLevelImage()
    {
        Transform bodyMai = transform.Find("Body/Mai");
        if (bodyMai != null && bodyMai.TryGetComponent(out Image image))
        {
            return image;
        }

        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image candidate in images)
        {
            if (candidate == null || candidate.gameObject.name != "Mai")
            {
                continue;
            }

            Transform parent = candidate.transform.parent;
            if (parent != null && parent.name == "Body")
            {
                return candidate;
            }
        }

        return null;
    }

    private LocalizedText FindSensitivePointsPerActivityText()
    {
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        LocalizedText largestSensitivePointsBlock = null;
        float largestAnchorHeight = 0f;

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null)
                continue;

            string value = text.text;
            if (!string.IsNullOrEmpty(value)
                && (value.Contains("Boob") || value.Contains("Boobs"))
                && value.Contains("Mouth")
                && value.Contains("Pussy")
                && value.Contains("Butthole"))
            {
                return EnsureLocalizedText(text);
            }

            if (IsKnownDynamicText(text))
                continue;

            LocalizedText localizedText = text.GetComponent<LocalizedText>();
            if (localizedText != null && localizedText.Key == "Body Sensitive points")
            {
                RectTransform rectTransform = text.rectTransform;
                float anchorHeight = rectTransform.anchorMax.y - rectTransform.anchorMin.y;
                if (anchorHeight > largestAnchorHeight)
                {
                    largestAnchorHeight = anchorHeight;
                    largestSensitivePointsBlock = localizedText;
                }
            }
        }

        return largestSensitivePointsBlock;
    }

    private bool IsKnownDynamicText(TextMeshProUGUI text)
    {
        LocalizedText localizedText = text != null ? text.GetComponent<LocalizedText>() : null;
        return localizedText != null
            && (localizedText == boobsSensitivePointsText
                || localizedText == mouthSensitivePointsText
                || localizedText == pussySensitivePointsText
                || localizedText == buttholeSensitivePointsText
                || localizedText == boobsSensitiveValueText
                || localizedText == mouthSensitiveValueText
                || localizedText == pussySensitiveValueText
                || localizedText == buttholeSensitiveValueText
                || localizedText == lewdLevelText
                || localizedText == pregnancyStatusText
                || localizedText == sensitivePointsPerActivityText);
    }

    private LocalizedText FindSiblingValueText(string localizedKey)
    {
        LocalizedText[] localizedTexts = GetComponentsInChildren<LocalizedText>(true);
        foreach (LocalizedText localizedText in localizedTexts)
        {
            if (localizedText == null || localizedText.Key != localizedKey)
                continue;

            Transform parent = localizedText.transform.parent;
            TextMeshProUGUI labelText = localizedText.GetComponent<TextMeshProUGUI>();
            if (parent == null || labelText == null)
                continue;

            TextMeshProUGUI[] siblingTexts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI siblingText in siblingTexts)
            {
                if (siblingText != null
                    && siblingText != labelText
                    && siblingText.GetComponent<LocalizedText>() == null)
                {
                    return EnsureLocalizedText(siblingText);
                }
            }

            foreach (TextMeshProUGUI siblingText in siblingTexts)
            {
                if (siblingText != null && siblingText != labelText)
                {
                    return EnsureLocalizedText(siblingText);
                }
            }
        }

        return null;
    }

    private static LocalizedText EnsureLocalizedText(TextMeshProUGUI text)
    {
        if (text == null)
            return null;

        LocalizedText localizedText = text.GetComponent<LocalizedText>();
        return localizedText != null ? localizedText : text.gameObject.AddComponent<LocalizedText>();
    }
}

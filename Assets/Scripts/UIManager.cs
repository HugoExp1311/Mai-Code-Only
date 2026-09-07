using System;
using System.Collections.Generic;
using Base;
using Base.Localization;
using Base.Settings;
using Events;
using EventBus;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    // OPT-40: Cache language count to avoid Enum.GetNames() allocation
    private static readonly int _languageCount = Enum.GetNames(typeof(Language)).Length;

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<PlaceUnavailableEvent> placeUnavailableBinding;

    [SerializeField] private GameObject mai;

    [Header("Buttons")]
    [SerializeField] private Button newGameBtn;
    [SerializeField] private Button loadGameBtn;
    [SerializeField] private Button sceneBtn;
    [SerializeField] private Button sceneEventBtn;
    [SerializeField] private Button sceneEndingBtn;
    [SerializeField] private Button settingBtn;
    [SerializeField] private Button languageSettingLeftBtn;
    [SerializeField] private Button languageSettingRightBtn;
    [SerializeField] private Button creditBtn;
    [SerializeField] private Button confirmYesBtn;
    [SerializeField] private Button confirmNoBtn;
    [SerializeField] private Button returnToTitleBtn;

    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject newGamePanel;
    [SerializeField] private GameObject loadGamePanel;
    [SerializeField] private GameObject scenePanel;
    [SerializeField] private GameObject sceneEventPanel;
    [SerializeField] private GameObject sceneEndingPanel;
    [SerializeField] private GameObject creditPanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject hiepMartPanel;
    [SerializeField] private GameObject companyPanel;
    [SerializeField] private GameObject parkPanel;
    [SerializeField] private GameObject inGamePanel;
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject bodyPanel;
    [SerializeField] private GameObject clothesPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject skillProfilePanel;
    [SerializeField] private GameObject skillSexSkill1Panel;
    [SerializeField] private GameObject skillSexSkill2Panel;
    [SerializeField] private GameObject placePanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Sliders & Scrollbars")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Scrollbar languageScrollbar;

    [Header("Animators")]
    [SerializeField] private Animator skillTabsAnimator;

    [Header("Texts & Localized Text")]
    [SerializeField] private LocalizedText staminaText;
    [SerializeField] private LocalizedText moneyText;
    [SerializeField] private LocalizedText playerNameText;
    [SerializeField] private LocalizedText skillPointsText;
    [SerializeField] private LocalizedText confirmQuestionLocalizedText;

    [Header("Reorganizable")]
    [SerializeField] private GameObject homeChoosePlace;
    [SerializeField] private GameObject hiepMartChoosePlace;
    [SerializeField] private GameObject companyChoosePlace;
    [SerializeField] private GameObject parkChoosePlace;

    private bool isInGame;
    private bool isRealOnStartInvoked;

    void Awake()
    {
        Instance = this;
        LocalizationManager.Instance.LanguageChanged += HandleLocalizationChanged;
        
        // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        placeUnavailableBinding = new EventBinding<PlaceUnavailableEvent>(HandlePlaceUnavailable);
    }

    void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<PlaceUnavailableEvent>.Register(placeUnavailableBinding);
    }

    void OnDisable()
    {
        // OPT-49: Deregister only — bindings are reused, not reallocated
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<PlaceUnavailableEvent>.Deregister(placeUnavailableBinding);
        
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= HandleLocalizationChanged;
    }

    void OnDestroy()
    {
        // Ensure bindings are deregistered on destroy
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<PlaceUnavailableEvent>.Deregister(placeUnavailableBinding);
        
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= HandleLocalizationChanged;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        newGamePanel.SetActive(false);
        loadGamePanel.SetActive(false);
        scenePanel.SetActive(false);
        sceneEventPanel.SetActive(false);
        sceneEndingPanel.SetActive(false);
        creditPanel.SetActive(false);
        confirmPanel.SetActive(false);
        homePanel.SetActive(false);
        hiepMartPanel.SetActive(false);
        companyPanel.SetActive(false);
        parkPanel.SetActive(false);
        inGamePanel.SetActive(false);
        profilePanel.SetActive(false);
        bodyPanel.SetActive(false);
        clothesPanel.SetActive(false);
        skillPanel.SetActive(false);
        skillProfilePanel.SetActive(false);
        skillSexSkill1Panel.SetActive(false);
        skillSexSkill2Panel.SetActive(false);
        placePanel.SetActive(false);
        settingPanel.SetActive(false);
        inventoryPanel.SetActive(false);

        // OPT-7/8: Use typed getters/setters instead of dynamic GetSetting()/UpdateSettings()
        musicSlider.value = GameManager.Instance.SettingsManager.GetMusicVolume();
        soundSlider.value = GameManager.Instance.SettingsManager.GetSoundVolume();
        musicSlider.onValueChanged.AddListener(value => GameManager.Instance.SettingsManager.UpdateMusicVolume((int)value));
        soundSlider.onValueChanged.AddListener(value => GameManager.Instance.SettingsManager.UpdateSoundVolume((int)value));
        languageSettingLeftBtn.onClick.AddListener(() => GameManager.Instance.ChangeLocale(-1));
        languageSettingRightBtn.onClick.AddListener(() => GameManager.Instance.ChangeLocale(1));
        returnToTitleBtn.onClick.AddListener(() =>
        {
            confirmQuestionLocalizedText.SetLocalized("Confirm Question", LocalizationDomains.UI);
            confirmPanel.SetActive(true);
            confirmYesBtn.onClick.RemoveAllListeners();
            confirmYesBtn.onClick.AddListener(() => DisablePanel(inGamePanel));
            confirmNoBtn.onClick.RemoveAllListeners();
            confirmNoBtn.onClick.AddListener(() => DisablePanel(confirmPanel));
        });
    }



    public void EnablePanel(GameObject panel)
    {
        newGamePanel.SetActive(false);
        loadGamePanel.SetActive(false);
        scenePanel.SetActive(false);
        sceneEventPanel.SetActive(false);
        sceneEndingPanel.SetActive(false);
        creditPanel.SetActive(false);
        confirmPanel.SetActive(false);
        homePanel.SetActive(false);
        hiepMartPanel.SetActive(false);
        companyPanel.SetActive(false);
        parkPanel.SetActive(false);

        if (!isInGame)
            inGamePanel.SetActive(false);
        
        profilePanel.SetActive(false);
        bodyPanel.SetActive(false);
        clothesPanel.SetActive(false);
        skillPanel.SetActive(false);
        skillProfilePanel.SetActive(false);
        skillSexSkill1Panel.SetActive(false);
        skillSexSkill2Panel.SetActive(false);
        placePanel.SetActive(false);
        inventoryPanel.SetActive(false);
        settingPanel.SetActive(false);

        panel.SetActive(true);

        if (panel == scenePanel)
            sceneEventPanel.SetActive(true);

        if (panel == sceneEventPanel || panel == sceneEndingPanel)
            scenePanel.SetActive(true);

        if (panel == skillPanel)
        {
            skillProfilePanel.SetActive(true);
            skillTabsAnimator.SetBool("Profile", true);
            SetTextValue(playerNameText, GameManager.Instance.Player.GetName());
            SetTextAmount(skillPointsText, GameManager.Instance.Player.GetSkillPoint());
        }

        if (panel == skillProfilePanel || panel == skillSexSkill1Panel || panel == skillSexSkill2Panel)
        {
            skillPanel.SetActive(true);
            skillTabsAnimator.SetBool(panel == skillProfilePanel ? "Profile" : panel == skillSexSkill1Panel ? "Sex Skill 1" : "Sex Skill 2", true);
        }

        if (inGamePanel.activeSelf)
            HandleGameStart(new GameStartEvent() { Area = GameManager.Instance.Area, Cycle = GameManager.Instance.Cycle, Time = GameManager.Instance.Time });

    }

    public void DisablePanel(GameObject panel)
    {
        if (panel == scenePanel)
        {
            sceneEventPanel.SetActive(false);
            sceneEndingPanel.SetActive(false);
        }

        if (panel == inGamePanel)
        {
            isInGame = false;
            homePanel.SetActive(false);
            hiepMartPanel.SetActive(false);
            companyPanel.SetActive(false);
            parkPanel.SetActive(false);
            settingPanel.SetActive(false);

            var settingLayout = settingPanel.GetComponentInChildren<HorizontalLayoutGroup>();
            settingLayout.transform.GetChild(2).gameObject.SetActive(false);
            settingLayout.padding.left = 178;
            settingLayout.padding.right = 178;
            confirmYesBtn.onClick.RemoveAllListeners();
            confirmYesBtn.onClick.AddListener(() =>
            {
                confirmQuestionLocalizedText.SetLocalized("Start Confirm Question", LocalizationDomains.UI);
                EnablePanel(inGamePanel);
                GameManager.Instance.StartGame();
            });
            confirmNoBtn.onClick.RemoveAllListeners();
            confirmNoBtn.onClick.AddListener(() =>
            {
                DisablePanel(confirmPanel);
                EnablePanel(newGamePanel);
            });
        }

        panel.SetActive(false);
    }

    private void HandleGameStart(GameStartEvent gameStartEventArgs)
    {
        isInGame = true;
        switch (gameStartEventArgs.Area)
        {
            case Area.Home:
                mai.SetActive(TimeManager.IsMaiAtHome(gameStartEventArgs.Time));
                homePanel.SetActive(true);
                break;
            case Area.HiepMart:
                hiepMartPanel.SetActive(true);
                break;
            case Area.Company:
                companyPanel.SetActive(true);
                break;
            case Area.Park:
                parkPanel.SetActive(true);
                break;
        }

        if (isRealOnStartInvoked)
        {
            SetTextRatio(staminaText, GameManager.Instance.Player.GetStamina(), 100);
            SetTextAmount(moneyText, GameManager.Instance.Player.GetMoney());
        }

        var settingLayout = settingPanel.GetComponentInChildren<HorizontalLayoutGroup>();
        settingLayout.transform.GetChild(2).gameObject.SetActive(true);
        settingLayout.padding.left = 65;
        settingLayout.padding.right = 10;

        isRealOnStartInvoked = true;
    }
    
    private void HandlePlaceChanged(PlaceChangedEvent placeChangedEventArgs)
    {
        isInGame = true;
        switch (placeChangedEventArgs.Area)
        {
            case Area.Home:
                mai.SetActive(TimeManager.IsMaiAtHome(placeChangedEventArgs.Time));
                homePanel.SetActive(true);
                homeChoosePlace.SetActive(false);
                hiepMartChoosePlace.SetActive(placeChangedEventArgs.Area != Area.HiepMart);
                companyChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Company);
                parkChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Park);
                break;
            case Area.HiepMart:
                hiepMartPanel.SetActive(true);
                homeChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Home);
                hiepMartChoosePlace.SetActive(false);
                companyChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Company);
                parkChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Park);
                break;
            case Area.Company:
                companyPanel.SetActive(true);
                homeChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Home);
                hiepMartChoosePlace.SetActive(placeChangedEventArgs.Area != Area.HiepMart);
                companyChoosePlace.SetActive(false);
                parkChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Park);
                break;
            case Area.Park:
                parkPanel.SetActive(true);
                homeChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Home);
                hiepMartChoosePlace.SetActive(placeChangedEventArgs.Area != Area.HiepMart);
                companyChoosePlace.SetActive(placeChangedEventArgs.Area != Area.Company);
                parkChoosePlace.SetActive(false);
                break;
        }
        placePanel.SetActive(false);
    }

    private void HandlePlaceUnavailable(PlaceUnavailableEvent placeUnavailableEventArgs)
    {
        // Show notice panel with appropriate message - the NoticeUI component will handle the UI display
        // The NoticeUI component is already subscribed to PlaceUnavailableEventArgs and will show the notice
        
        switch (placeUnavailableEventArgs.TargetArea)
        {
            case Area.Company:
                homeChoosePlace.SetActive(placeUnavailableEventArgs.CurrentArea != Area.Home);
                hiepMartChoosePlace.SetActive(placeUnavailableEventArgs.CurrentArea != Area.HiepMart);
                companyChoosePlace.SetActive(true);
                parkChoosePlace.SetActive(placeUnavailableEventArgs.CurrentArea != Area.Park);
                break;
            case Area.Park:
                homeChoosePlace.SetActive(placeUnavailableEventArgs.CurrentArea != Area.Home);
                hiepMartChoosePlace.SetActive(placeUnavailableEventArgs.CurrentArea != Area.HiepMart);
                companyChoosePlace.SetActive(placeUnavailableEventArgs.CurrentArea != Area.Company);
                parkChoosePlace.SetActive(true);
                break;
        }
    }

    private void HandleLocalizationChanged(Language language, string localeCode, System.Globalization.CultureInfo culture)
    {
        // OPT-40: Use cached language count
        languageScrollbar.value = ((float)(int)language) / ((float)(_languageCount - 1));

        switch (languageScrollbar.value)
        {
            case 0f:
                languageSettingLeftBtn.interactable = false;
                languageSettingRightBtn.interactable = true;
                break;
            case 1f:
                languageSettingLeftBtn.interactable = true;
                languageSettingRightBtn.interactable = false;
                break;
            default:
                languageSettingLeftBtn.interactable = true;
                languageSettingRightBtn.interactable = true;
                break;
        }
    }

    private static void SetTextAmount(LocalizedText target, int amount)
    {
        if (target == null)
            return;

        target.SetLocalized("UI Value Integer", LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["amount"] = amount
        });
    }

    private static void SetTextRatio(LocalizedText target, int current, int max)
    {
        if (target == null)
            return;

        target.SetLocalized("UI Value Ratio", LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["current"] = current,
            ["max"] = max
        });
    }

    private static void SetTextValue(LocalizedText target, string value)
    {
        if (target == null)
            return;

        target.SetLocalized("UI Value Text", LocalizationDomains.UI, new Dictionary<string, object>
        {
            ["value"] = value ?? string.Empty
        });
    }
}

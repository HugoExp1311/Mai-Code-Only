using System.Collections.Generic;
using Base;
using Base.Character;
using Base.Character.Stats;
using Base.Localization;
using Base.SexScenes;
using Events;
using EventBus;
using TMPro;
using UI;
using UI.SexPosition;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SexSceneUnlockPanelController : MonoBehaviour
{
    private const string ConfirmPopupPanelId = "Confirm Popup";
    private const string UnlockQuestionKey = "Sex Scene Unlock Confirm Question";
    private const string UnlockYesKey = "Sex Scene Unlock Confirm Yes";
    private const string UnlockNoKey = "Sex Scene Unlock Confirm No";
    private const string UnlockSuccessNoticeKey = "Notice Sex Scene Unlock Success";
    private const string UnlockFailureNoticeKey = "Notice Sex Scene Unlock Failure";
    private const string UnlockInfoHeaderKey = "Sex Scene Unlock Info Header";

    private readonly Dictionary<SexSceneType, SceneEntry> sceneEntries = new();
    private readonly Dictionary<SexSceneType, Button> simulationButtons = new();

    private GameObject sexSceneInfoRoot;
    private LocalizedText infoTitleLocalizedText;
    private LocalizedText infoHeaderLocalizedText;
    private LocalizedText infoBodyLocalizedText;
    private TMP_Text infoTitleText;
    private TMP_Text infoHeaderText;
    private TMP_Text infoBodyText;
    private SimulationNavigationPanel simulationNavigationPanel;
    private UIPanel positionsPanel;

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<ResourceChangedEvent> resourceChangedBinding;
    private EventBinding<StatsChangedEvent> statsChangedBinding;

    private sealed class SceneEntry
    {
        public Button SkillButton;
        public GameObject LockedOverlay;
        public SexSceneHoverHandler HoverHandler;
    }

    private void Awake()
    {
        gameStartBinding = new EventBinding<GameStartEvent>(_ => RefreshAll());
        resourceChangedBinding = new EventBinding<ResourceChangedEvent>(HandleResourceChanged);
        statsChangedBinding = new EventBinding<StatsChangedEvent>(HandleStatsChanged);

        BindInfoPanel();
        BindSkillSceneButtons();
        BindSimulationPositionButtons();
        HideInfoPanel();
        RefreshAll();
    }

    private void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<ResourceChangedEvent>.Register(resourceChangedBinding);
        EventBus<StatsChangedEvent>.Register(statsChangedBinding);
        RefreshAll();
    }

    private void OnDisable()
    {
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        HideInfoPanel();
    }

    private void OnDestroy()
    {
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);

        if (simulationButtons.TryGetValue(SexSceneType.RoleplayPussy, out Button roleplayPussyButton))
        {
            roleplayPussyButton.onClick.RemoveListener(HandleRoleplayPussyPositionClicked);
        }

        if (simulationButtons.TryGetValue(SexSceneType.RoleplayButthole, out Button roleplayButtholeButton))
        {
            roleplayButtholeButton.onClick.RemoveListener(HandleRoleplayButtholePositionClicked);
        }
    }

    private void HandleResourceChanged(ResourceChangedEvent args)
    {
        if (args.Target == RewardTarget.Player && args.Resource == BasicResource.SkillPoint)
        {
            RefreshAll();
        }
    }

    private void HandleStatsChanged(StatsChangedEvent args)
    {
        if (args.Target == RewardTarget.Player || args.Target == RewardTarget.Mai)
        {
            RefreshAll();
        }
    }

    private void BindInfoPanel()
    {
        Transform infoTransform = transform.Find("Sex Scene Info");
        if (infoTransform == null)
        {
            return;
        }

        sexSceneInfoRoot = infoTransform.gameObject;

        TMP_Text[] textComponents = infoTransform.GetComponentsInChildren<TMP_Text>(true);
        if (textComponents.Length >= 3)
        {
            infoTitleText = textComponents[0];
            infoHeaderText = textComponents[1];
            infoBodyText = textComponents[2];
            infoTitleLocalizedText = infoTitleText.GetComponent<LocalizedText>();
            infoHeaderLocalizedText = infoHeaderText.GetComponent<LocalizedText>();
            infoBodyLocalizedText = infoBodyText.GetComponent<LocalizedText>();
        }
    }

    private void BindSkillSceneButtons()
    {
        Transform sexSceneRoot = transform.Find("Skill/Sex Scene");
        if (sexSceneRoot == null)
        {
            return;
        }

        foreach (KeyValuePair<SexSceneType, SexSceneDefinition> pair in SexSceneUnlockService.GetDefinitions())
        {
            Transform tile = sexSceneRoot.Find(pair.Value.SkillSceneName);
            if (tile == null)
            {
                continue;
            }

            Button button = tile.GetComponent<Button>();
            if (button == null)
            {
                continue;
            }

            Transform lockedOverlay = tile.Find("Icon/Locked");
            SexSceneHoverHandler hoverHandler = tile.gameObject.GetComponent<SexSceneHoverHandler>();
            if (hoverHandler == null)
            {
                hoverHandler = tile.gameObject.AddComponent<SexSceneHoverHandler>();
            }

            hoverHandler.Initialize(this, pair.Key);

            sceneEntries[pair.Key] = new SceneEntry
            {
                SkillButton = button,
                LockedOverlay = lockedOverlay != null ? lockedOverlay.gameObject : null,
                HoverHandler = hoverHandler
            };
        }
    }

    private void BindSimulationPositionButtons()
    {
        Transform positionsRoot = transform.root.Find("Simulation/Positions/Positions");
        if (positionsRoot == null)
        {
            return;
        }

        positionsPanel = positionsRoot.parent != null
            ? positionsRoot.parent.GetComponent<UIPanel>()
            : null;
        simulationNavigationPanel = transform.root.GetComponentInChildren<SimulationNavigationPanel>(true);

        foreach (KeyValuePair<SexSceneType, SexSceneDefinition> pair in SexSceneUnlockService.GetDefinitions())
        {
            if (string.IsNullOrEmpty(pair.Value.SimulationPositionName))
            {
                continue;
            }

            Transform positionButton = positionsRoot.Find(pair.Value.SimulationPositionName);
            if (positionButton == null)
            {
                continue;
            }

            Button button = positionButton.GetComponent<Button>();
            if (button != null)
            {
                simulationButtons[pair.Key] = button;

                if (pair.Key == SexSceneType.RoleplayPussy)
                {
                    button.onClick.RemoveListener(HandleRoleplayPussyPositionClicked);
                    button.onClick.AddListener(HandleRoleplayPussyPositionClicked);
                }
                else if (pair.Key == SexSceneType.RoleplayButthole)
                {
                    button.onClick.RemoveListener(HandleRoleplayButtholePositionClicked);
                    button.onClick.AddListener(HandleRoleplayButtholePositionClicked);
                }
                else if (pair.Key == SexSceneType.Blowjob)
                {
                    button.onClick.RemoveListener(HandleRoleplayBlowjobPositionClicked);
                    button.onClick.AddListener(HandleRoleplayBlowjobPositionClicked);
                }
                else if (pair.Key == SexSceneType.Paizuri)
                {
                    button.onClick.RemoveListener(HandleRoleplayPaizuriPositionClicked);
                    button.onClick.AddListener(HandleRoleplayPaizuriPositionClicked);
                }
                else if (pair.Key == SexSceneType.Cowgirl)
                {
                    button.onClick.RemoveListener(HandleCowgirlPositionClicked);
                    button.onClick.AddListener(HandleCowgirlPositionClicked);
                }
            }
        }
    }

    private void HandleRoleplayPussyPositionClicked()
    {
        if (!SexSceneUnlockService.IsUnlocked(SexSceneType.RoleplayPussy))
        {
            return;
        }

        if (simulationNavigationPanel == null)
        {
            Debug.LogError("[SexSceneUnlockPanelController] SimulationNavigationPanel was not found; cannot select Roleplay Pussy.");
            return;
        }

        simulationNavigationPanel.SetPosition(SexPositionConfigType.RoleplayPussy);
        positionsPanel?.Hide();
    }

    private void HandleRoleplayButtholePositionClicked()
    {
        if (!SexSceneUnlockService.IsUnlocked(SexSceneType.RoleplayButthole))
        {
            return;
        }

        if (simulationNavigationPanel == null)
        {
            Debug.LogError(
                "[SexSceneUnlockPanelController] SimulationNavigationPanel was not found; cannot select Roleplay Butthole.");
            return;
        }

        simulationNavigationPanel.SetPosition(
            SexPositionConfigType.RoleplayButthole);
        positionsPanel?.Hide();
    }

    private void HandleRoleplayBlowjobPositionClicked()
    {
        if (!SexSceneUnlockService.IsUnlocked(SexSceneType.Blowjob))
        {
            return;
        }

        if (simulationNavigationPanel == null)
        {
            Debug.LogError(
                "[SexSceneUnlockPanelController] SimulationNavigationPanel was not found; cannot select Roleplay Blowjob.");
            return;
        }

        simulationNavigationPanel.SetPosition(
            SexPositionConfigType.RoleplayBlowjob);
        positionsPanel?.Hide();
    }

    private void HandleRoleplayPaizuriPositionClicked()
    {
        if (!SexSceneUnlockService.IsUnlocked(SexSceneType.Paizuri))
        {
            return;
        }

        if (simulationNavigationPanel == null)
        {
            Debug.LogError(
                "[SexSceneUnlockPanelController] SimulationNavigationPanel was not found; cannot select Roleplay Paizuri.");
            return;
        }

        simulationNavigationPanel.SetPosition(
            SexPositionConfigType.RoleplayPaizuri);
        positionsPanel?.Hide();
    }

    private void HandleCowgirlPositionClicked()
    {
        if (!SexSceneUnlockService.IsUnlocked(SexSceneType.Cowgirl))
        {
            return;
        }

        if (simulationNavigationPanel == null)
        {
            Debug.LogError(
                "[SexSceneUnlockPanelController] SimulationNavigationPanel was not found; cannot select Cowgirl.");
            return;
        }

        simulationNavigationPanel.SetPosition(SexPositionConfigType.Cowgirl);
        positionsPanel?.Hide();
    }

    public void HandlePointerEnter(SexSceneType sceneType)
    {
        if (SexSceneUnlockService.IsUnlocked(sceneType))
        {
            HideInfoPanel();
            return;
        }

        ShowInfoPanel(sceneType);
    }

    public void HandlePointerExit()
    {
        HideInfoPanel();
    }

    public void HandlePointerClick(SexSceneType sceneType)
    {
        if (SexSceneUnlockService.IsUnlocked(sceneType) || UIPanelManager.Instance == null)
        {
            return;
        }

        ConfirmPopupData popupData = ConfirmPopupData.CreateLocalized(
            UnlockQuestionKey,
            null,
            UnlockYesKey,
            () => AttemptUnlock(sceneType),
            showNoButton: true,
            yesButtonVariables: null,
            noButtonKey: UnlockNoKey,
            noButtonVariables: null,
            noCallback: null,
            closeCallback: null);

        UIPanelManager.Instance.ShowPanel(ConfirmPopupPanelId, popupData);
    }

    private void AttemptUnlock(SexSceneType sceneType)
    {
        Player player = GameManager.Instance?.Player;
        Target mai = GameManager.Instance?.DataManager?.GetCurrentBoss() as Target;
        if (player == null || mai == null)
        {
            NoticeUI.ShowLocalized(UnlockFailureNoticeKey);
            return;
        }

        if (!SexSceneUnlockService.CanUnlock(sceneType, player, mai))
        {
            NoticeUI.ShowLocalized(UnlockFailureNoticeKey);
            return;
        }

        SexSceneDefinition definition = SexSceneUnlockService.GetDefinition(sceneType);
        if (!player.TrySpendSkillPoints(definition.Cost))
        {
            NoticeUI.ShowLocalized(UnlockFailureNoticeKey);
            return;
        }

        SexSceneUnlockService.SetUnlocked(sceneType, true);
        RefreshAll();
        HideInfoPanel();
        NoticeUI.ShowLocalized(UnlockSuccessNoticeKey);
    }

    private void RefreshAll()
    {
        foreach (KeyValuePair<SexSceneType, SceneEntry> pair in sceneEntries)
        {
            bool unlocked = SexSceneUnlockService.IsUnlocked(pair.Key);
            if (pair.Value.LockedOverlay != null)
            {
                pair.Value.LockedOverlay.SetActive(!unlocked);
            }
        }

        foreach (KeyValuePair<SexSceneType, Button> pair in simulationButtons)
        {
            pair.Value.interactable = SexSceneUnlockService.IsUnlocked(pair.Key);
        }
    }

    private void ShowInfoPanel(SexSceneType sceneType)
    {
        if (sexSceneInfoRoot == null)
        {
            return;
        }

        SexSceneDefinition definition = SexSceneUnlockService.GetDefinition(sceneType);
        if (infoTitleLocalizedText != null)
        {
            infoTitleLocalizedText.SetDirect(definition.SkillSceneName);
        }
        else if (infoTitleText != null)
        {
            infoTitleText.text = definition.SkillSceneName;
        }

        if (infoHeaderLocalizedText != null)
        {
            infoHeaderLocalizedText.SetLocalized(UnlockInfoHeaderKey, LocalizationDomains.UI);
        }
        else if (infoHeaderText != null)
        {
            infoHeaderText.text = "Unlock conditions";
        }

        if (infoBodyLocalizedText != null && !string.IsNullOrEmpty(definition.RequirementLocalizationKey))
        {
            infoBodyLocalizedText.SetLocalized(definition.RequirementLocalizationKey, LocalizationDomains.UI);
        }
        else if (infoBodyText != null)
        {
            infoBodyText.text = definition.BuildRequirementText();
        }

        sexSceneInfoRoot.SetActive(true);
    }

    private void HideInfoPanel()
    {
        if (sexSceneInfoRoot != null)
        {
            sexSceneInfoRoot.SetActive(false);
        }
    }

    private sealed class SexSceneHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private SexSceneUnlockPanelController controller;
        private SexSceneType sceneType;

        public void Initialize(SexSceneUnlockPanelController owner, SexSceneType type)
        {
            controller = owner;
            sceneType = type;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            controller?.HandlePointerEnter(sceneType);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            controller?.HandlePointerExit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.HandlePointerClick(sceneType);
        }
    }
}

using Base;
using Base.Character.Stats;
using Base.Localization;
using Base.SexScenes;
using Events;
using EventBus;
using UnityEngine;

/// <summary>
/// Skill panel for In Game section that displays player stats
/// Shows player name, energy (stamina), knowledge, and charming stats
/// Inherits from UIPanel to integrate with the panel management system
/// </summary>
public class SkillPanel : UIPanel
{
    [Header("Player Information")]
    [SerializeField] private LocalizedText playerNameText;

    [Header("Player Stats")]
    [SerializeField] private LocalizedText energyText;
    [SerializeField] private LocalizedText knowledgeText;
    [SerializeField] private LocalizedText charmingText;
    [SerializeField] private LocalizedText sexPointsText;

    private EventBinding<GameStartEvent> gameStartBinding;
    private EventBinding<PlaceChangedEvent> placeChangedBinding;
    private EventBinding<StatsChangedEvent> statsChangedBinding;

    private EventBinding<ResourceChangedEvent> resourceChangedBinding;
    private SexSceneUnlockPanelController unlockController;

    // OPT-49: Allocate bindings once in Awake, reuse across enable/disable cycles
    private void Awake()
    {
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        placeChangedBinding = new EventBinding<PlaceChangedEvent>(HandlePlaceChanged);
        statsChangedBinding = new EventBinding<StatsChangedEvent>(HandleStatsChanged);
        resourceChangedBinding = new EventBinding<ResourceChangedEvent>(HandleResourceChanged);
        unlockController = GetComponent<SexSceneUnlockPanelController>();
        if (unlockController == null)
        {
            unlockController = gameObject.AddComponent<SexSceneUnlockPanelController>();
        }
    }

    private void OnEnable()
    {
        EventBus<GameStartEvent>.Register(gameStartBinding);
        EventBus<PlaceChangedEvent>.Register(placeChangedBinding);
        EventBus<StatsChangedEvent>.Register(statsChangedBinding);
        EventBus<ResourceChangedEvent>.Register(resourceChangedBinding);

        // Initial update if player exists
        if (GameManager.Instance?.Player != null)
        {
            UpdateAllStats();
        }
    }

    private void OnDisable()
    {
        // OPT-49: Deregister only — bindings are reused, not reallocated
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
    }

    private void OnDestroy()
    {
        // Ensure bindings are deregistered on destroy
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        EventBus<PlaceChangedEvent>.Deregister(placeChangedBinding);
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<ResourceChangedEvent>.Deregister(resourceChangedBinding);
    }

    private void HandleResourceChanged(ResourceChangedEvent args)
    {
        if (args.Target == RewardTarget.Player)
        {
            UpdateSexPoints();
        }
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);

        // Update UI when panel is shown
        UpdateAllStats();
    }

    private void HandleGameStart(GameStartEvent args)
    {
        UpdateAllStats();
    }

    private void HandlePlaceChanged(PlaceChangedEvent args)
    {
        UpdateAllStats();
    }

    private void HandleStatsChanged(StatsChangedEvent args)
    {
        if (args.Target == RewardTarget.Player)
        {
            UpdateAllStats();
        }
    }

    private void UpdateAllStats()
    {
        UpdatePlayerName();
        UpdateEnergy();
        UpdateKnowledge();
        UpdateCharming();
        UpdateSexPoints();
    }

    private void UpdatePlayerName()
    {
        if (playerNameText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            string playerName = GameManager.Instance.Player.GetName();
            SetValueText(playerNameText, playerName);
        }
        else
        {
            SetEmpty(playerNameText);
        }
    }

    private void UpdateEnergy()
    {
        if (energyText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            int stamina = GameManager.Instance.Player.GetStamina();
            SetInteger(energyText, stamina);
        }
        else
        {
            SetInteger(energyText, 0);
        }
    }

    private void UpdateKnowledge()
    {
        if (knowledgeText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            int knowledge = GameManager.Instance.Player.GetKnowledge();
            SetInteger(knowledgeText, knowledge);
        }
        else
        {
            SetInteger(knowledgeText, 0);
        }
    }

    private void UpdateCharming()
    {
        if (charmingText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            int charming = GameManager.Instance.Player.GetCharming();
            SetInteger(charmingText, charming);
        }
        else
        {
            SetInteger(charmingText, 0);
        }
    }

    private void UpdateSexPoints()
    {
        if (sexPointsText == null) return;

        if (GameManager.Instance?.Player != null)
        {
            int sexPoints = GameManager.Instance.Player.GetSkillPoint();
            SetInteger(sexPointsText, sexPoints);
        }
        else
        {
            SetInteger(sexPointsText, 0);
        }
    }

    private static void SetInteger(LocalizedText text, int amount)
    {
        text?.SetLocalized("UI Value Integer", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["amount"] = amount
        });
    }

    private static void SetValueText(LocalizedText text, string value)
    {
        text?.SetLocalized("UI Value Text", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
        {
            ["value"] = value ?? string.Empty
        });
    }

    private static void SetEmpty(LocalizedText text)
    {
        text?.SetLocalized("UI Empty", LocalizationDomains.UI);
    }
}

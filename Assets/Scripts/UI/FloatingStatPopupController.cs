using System.Collections;
using System.Collections.Generic;
using Base.Character;
using Base.Character.Stats;
using Base.Localization;
using Events;
using EventBus;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Global listener that shows floating feedback for visible stat changes.
/// </summary>
public class FloatingStatPopupController : MonoBehaviour
{
    [SerializeField] private FloatingStatPopupItem popupTemplate;
    [Header("Position")]
    [SerializeField] private Vector2 monologueAnchor = new Vector2(0.5f, 1.25f);
    [SerializeField] private Vector2 fallbackViewportAnchor = new Vector2(0.5f, 0.329f);
    [Header("Animation")]
    [SerializeField] private float stackSpacing = 42f;
    [SerializeField] private float riseDistance = 55f;
    [SerializeField] private float popupDuration = 1.35f;
    [SerializeField] private int maxActivePopups = 6;
    [Header("Style")]
    [SerializeField] private Color positiveTextColor = new Color(0.42f, 1f, 0.2f, 1f);
    // LDR glow color; alpha drives glow power. Rendered by TMP's built-in Distance
    // Field glow, so it needs no bloom camera or post-processing.
    [FormerlySerializedAs("positiveBloomFaceColor")]
    [SerializeField] private Color positiveGlowColor = new Color(0.3f, 1f, 0.25f, 0.75f);
    [SerializeField] private Color negativeTextColor = new Color(1f, 0.24f, 0.18f, 1f);
    [FormerlySerializedAs("negativeBloomFaceColor")]
    [SerializeField] private Color negativeGlowColor = new Color(1f, 0.28f, 0.22f, 0.75f);
    [Header("Debug")]
    [SerializeField] private bool showSpawnGizmo = true;
    [SerializeField] private Color spawnGizmoColor = new Color(0f, 1f, 1f, 0.9f);
    [SerializeField] private float spawnGizmoRadius = 0.35f;
    [SerializeField] private bool showSpawnGizmoLabel = true;

    private static FloatingStatPopupController instance;

    private readonly Dictionary<StatKey, int> previousValues = new();
    private readonly List<FloatingStatPopupItem> activePopups = new();
    private EventBinding<StatsChangedEvent> statsChangedBinding;
    private EventBinding<GameStartEvent> gameStartBinding;
    private RectTransform rootTransform;
    private Canvas popupCanvas;
    private Coroutine seedCoroutine;
    private bool missingReferenceLogged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        statsChangedBinding = new EventBinding<StatsChangedEvent>(HandleStatsChanged);
        gameStartBinding = new EventBinding<GameStartEvent>(HandleGameStart);
        ResolveSceneReferences();
        HideTemplate();
    }

    private void OnEnable()
    {
        EventBus<StatsChangedEvent>.Register(statsChangedBinding);
        EventBus<GameStartEvent>.Register(gameStartBinding);
    }

    private void Start()
    {
        EnsureSeedCoroutine();
    }

    private void OnDisable()
    {
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
    }

    private void OnDestroy()
    {
        EventBus<StatsChangedEvent>.Deregister(statsChangedBinding);
        EventBus<GameStartEvent>.Deregister(gameStartBinding);
        if (instance == this)
            instance = null;
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnGizmo || !TryGetSpawnGizmoWorldPosition(out Vector3 worldPosition, out string source))
            return;

        float radius = Mathf.Max(0.01f, spawnGizmoRadius);
        Color previousColor = Gizmos.color;
        Gizmos.color = spawnGizmoColor;
        Gizmos.DrawWireSphere(worldPosition, radius);
        Gizmos.DrawLine(worldPosition + Vector3.left * radius * 1.8f, worldPosition + Vector3.right * radius * 1.8f);
        Gizmos.DrawLine(worldPosition + Vector3.down * radius * 1.8f, worldPosition + Vector3.up * radius * 1.8f);
        Gizmos.color = previousColor;

#if UNITY_EDITOR
        if (showSpawnGizmoLabel)
        {
            Handles.color = spawnGizmoColor;
            Handles.Label(
                worldPosition + Vector3.up * radius * 1.6f,
                $"Stat Popup Spawn\n{source}\nMonologue Anchor {monologueAnchor}\nFallback Anchor {fallbackViewportAnchor}");
        }
#endif
    }

    private void OnValidate()
    {
        ResolveSceneReferences();
    }

    private void EnsureSeedCoroutine()
    {
        if (seedCoroutine == null)
            seedCoroutine = StartCoroutine(SeedKnownValuesUntilReady());
    }

    private IEnumerator SeedKnownValuesUntilReady()
    {
        while (!SeedKnownValues())
        {
            yield return null;
        }

        seedCoroutine = null;
    }

    private void HandleGameStart(GameStartEvent args)
    {
        previousValues.Clear();
        EnsureSeedCoroutine();
    }

    private bool SeedKnownValues()
    {
        bool seededPlayer = false;
        bool seededMai = false;

        var player = GameManager.Instance?.Player;
        if (player != null)
        {
            previousValues[new StatKey(RewardTarget.Player, BasicStats.Stamina)] = player.GetStamina();
            previousValues[new StatKey(RewardTarget.Player, BasicStats.Charming)] = player.GetCharming();
            previousValues[new StatKey(RewardTarget.Player, BasicStats.Knowledge)] = player.GetKnowledge();
            seededPlayer = true;
        }

        Target mai = GameManager.Instance?.DataManager?.GetCurrentBoss() as Target;
        if (mai != null)
        {
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.Love)] = mai.GetLove();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.Libido)] = mai.GetLibido();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.BoobsSensitivePoints)] = mai.GetBoobsSensitivePoints();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.MouthSensitivePoints)] = mai.GetMouthSensitivePoints();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.PussySensitivePoints)] = mai.GetPussySensitivePoints();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.ButtholeSensitivePoints)] = mai.GetButtholeSensitivePoints();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.LewdLevel)] = mai.GetLewdLevel();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.BoobsSensitiveLevel)] = mai.GetBoobsSensitiveLevel();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.MouthSensitiveLevel)] = mai.GetMouthSensitiveLevel();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.PussySensitiveLevel)] = mai.GetPussySensitiveLevel();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.ButtholeSensitiveLevel)] = mai.GetButtholeSensitiveLevel();
            previousValues[new StatKey(RewardTarget.Mai, BasicStats.PregnancyChance)] = mai.GetPregnancyChance();
            seededMai = true;
        }

        return seededPlayer && seededMai;
    }

    private void ResolveSceneReferences()
    {
        rootTransform = transform as RectTransform;
        popupCanvas = GetComponentInParent<Canvas>(true);
    }

    private bool HasRequiredReferences()
    {
        ResolveSceneReferences();

        bool hasReferences = rootTransform != null &&
            popupTemplate != null &&
            popupCanvas != null;

        if (!hasReferences && !missingReferenceLogged)
        {
            Debug.LogError(
                $"{nameof(FloatingStatPopupController)} requires scene-authored references: " +
                $"{nameof(popupTemplate)}, a RectTransform on this object, and a parent Canvas.",
                this);
            missingReferenceLogged = true;
        }

        if (hasReferences)
            missingReferenceLogged = false;

        return hasReferences;
    }

    private void HideTemplate()
    {
        if (popupTemplate == null || !Application.isPlaying)
            return;

        popupTemplate.gameObject.SetActive(false);
    }

    private void HandleStatsChanged(StatsChangedEvent args)
    {
        StatKey key = new StatKey(args.Target, args.Stat);
        if (!previousValues.TryGetValue(key, out int previousValue))
        {
            previousValues[key] = args.NewValue;
            return;
        }

        previousValues[key] = args.NewValue;

        int delta = args.NewValue - previousValue;
        if (delta == 0 || ShouldSuppress(args, delta))
            return;

        ShowPopup(args.Stat, delta);
    }

    private static bool ShouldSuppress(StatsChangedEvent args, int delta)
    {
        if (args.Target == RewardTarget.Player &&
            args.Stat == BasicStats.Stamina &&
            delta < 0 &&
            UIPanelManager.Instance != null &&
            UIPanelManager.Instance.GetCurrentSection() == GameSection.Simulation)
        {
            return true;
        }

        return false;
    }

    private void ShowPopup(BasicStats stat, int delta)
    {
        if (!HasRequiredReferences())
            return;

        activePopups.RemoveAll(item => item == null || !item.IsPlaying);
        while (activePopups.Count >= maxActivePopups)
        {
            if (activePopups[0] != null)
                Destroy(activePopups[0].gameObject);
            activePopups.RemoveAt(0);
        }

        Vector2 anchorPosition = ResolveAnchorPosition();
        Vector2 stackedOffset = new Vector2(0f, activePopups.Count * stackSpacing);
        bool positive = delta > 0;

        FloatingStatPopupItem item = CreatePopupItem(positive);
        if (item == null)
            return;

        activePopups.Add(item);
        item.Play(
            BuildMessage(stat, delta),
            anchorPosition,
            stackedOffset,
            riseDistance,
            popupDuration,
            positive ? positiveTextColor : negativeTextColor,
            positive ? positiveGlowColor : negativeGlowColor);
    }

    private FloatingStatPopupItem CreatePopupItem(bool positive)
    {
        if (popupTemplate == null || rootTransform == null)
            return null;

        FloatingStatPopupItem item = Instantiate(popupTemplate, rootTransform, false);
        item.name = positive ? "Positive Stat Popup" : "Negative Stat Popup";
        item.gameObject.SetActive(false);
        return item;
    }

    private Vector2 ResolveAnchorPosition()
    {
        ResolveSceneReferences();

        RectTransform monologueRect = DialoguePanel.ActiveMonologueRect;
        if (monologueRect != null && TryGetAnchoredPosition(monologueRect, monologueAnchor, out Vector2 anchoredPosition))
        {
            return anchoredPosition;
        }

        return GetFallbackAnchoredPosition();
    }

    private bool TryGetSpawnGizmoWorldPosition(out Vector3 worldPosition, out string source)
    {
        worldPosition = transform.position;
        source = "Unavailable";
        ResolveSceneReferences();

        if (rootTransform != null)
        {
            RectTransform monologueRect = GetGizmoMonologueRect();
            Vector2 anchoredPosition = monologueRect != null &&
                TryGetAnchoredPosition(monologueRect, monologueAnchor, out Vector2 monologuePosition)
                    ? monologuePosition
                    : GetFallbackAnchoredPosition();

            worldPosition = BottomCenterAnchoredPositionToWorld(rootTransform, anchoredPosition);
            source = monologueRect != null ? "Monologue anchor" : "Fallback anchor";
            return true;
        }

        RectTransform previewMonologueRect = GetGizmoMonologueRect();
        if (previewMonologueRect != null &&
            TryGetRectScreenPosition(previewMonologueRect, monologueAnchor, out Vector2 screenPosition, out Camera camera, out float planeDistance))
        {
            worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, planeDistance));
            source = "Monologue preview";
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        Vector2 screenSize = GetGizmoScreenSize();
        Vector2 fallbackScreenPosition = new Vector2(
            screenSize.x * fallbackViewportAnchor.x,
            screenSize.y * fallbackViewportAnchor.y);
        worldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(fallbackScreenPosition.x, fallbackScreenPosition.y, GetGizmoPlaneDistance(null)));
        source = "Fallback preview";
        return true;
    }

    private static Vector3 BottomCenterAnchoredPositionToWorld(RectTransform root, Vector2 anchoredPosition)
    {
        Rect rect = root.rect;
        Vector3 localPosition = new Vector3(
            Mathf.Lerp(rect.xMin, rect.xMax, 0.5f) + anchoredPosition.x,
            rect.yMin + anchoredPosition.y,
            0f);
        return root.TransformPoint(localPosition);
    }

    private Vector2 GetFallbackAnchoredPosition()
    {
        if (rootTransform == null)
            return Vector2.zero;

        Rect rect = rootTransform.rect;
        Vector2 localPosition = new Vector2(
            Mathf.LerpUnclamped(rect.xMin, rect.xMax, fallbackViewportAnchor.x),
            Mathf.LerpUnclamped(rect.yMin, rect.yMax, fallbackViewportAnchor.y));
        return LocalPointToBottomCenterAnchoredPosition(rootTransform, localPosition);
    }

    private bool TryGetAnchoredPosition(
        RectTransform rectTransform,
        Vector2 normalizedPosition,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;
        if (rootTransform == null || popupCanvas == null)
            return false;

        if (!TryGetRectScreenPosition(rectTransform, normalizedPosition, out Vector2 screenPosition, out _, out _))
            return false;

        Camera targetCamera = popupCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? popupCanvas.worldCamera
            : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootTransform,
                screenPosition,
                targetCamera,
                out Vector2 localPosition))
        {
            return false;
        }

        anchoredPosition = LocalPointToBottomCenterAnchoredPosition(rootTransform, localPosition);
        return true;
    }

    private static Vector2 LocalPointToBottomCenterAnchoredPosition(RectTransform root, Vector2 localPosition)
    {
        Rect rect = root.rect;
        Vector2 bottomCenter = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, 0.5f), rect.yMin);
        return localPosition - bottomCenter;
    }

    private static RectTransform GetGizmoMonologueRect()
    {
        if (DialoguePanel.ActiveMonologueRect != null)
            return DialoguePanel.ActiveMonologueRect;

        RectTransform[] rectTransforms = FindObjectsByType<RectTransform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform != null && rectTransform.name == "Monologue")
                return rectTransform;
        }

        return null;
    }

    private static bool TryGetRectScreenPosition(
        RectTransform rectTransform,
        Vector2 normalizedPosition,
        out Vector2 screenPosition,
        out Camera camera,
        out float planeDistance)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera sourceCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector3 worldPoint = GetNormalizedRectWorldPoint(rectTransform, normalizedPosition);
        screenPosition = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        camera = sourceCamera != null ? sourceCamera : Camera.main;
        planeDistance = GetGizmoPlaneDistance(canvas);
        return camera != null;
    }

    private static Vector3 GetNormalizedRectWorldPoint(RectTransform rectTransform, Vector2 normalizedPosition)
    {
        Rect rect = rectTransform.rect;
        Vector3 localPoint = new Vector3(
            Mathf.LerpUnclamped(rect.xMin, rect.xMax, normalizedPosition.x),
            Mathf.LerpUnclamped(rect.yMin, rect.yMax, normalizedPosition.y),
            0f);
        return rectTransform.TransformPoint(localPoint);
    }

    private static float GetGizmoPlaneDistance(Canvas canvas)
    {
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            return canvas.planeDistance;

        return 10f;
    }

    private static Vector2 GetGizmoScreenSize()
    {
        float width = Screen.width > 0 ? Screen.width : 1920f;
        float height = Screen.height > 0 ? Screen.height : 1080f;
        return new Vector2(width, height);
    }

    private string BuildMessage(BasicStats stat, int delta)
    {
        string amount = delta > 0 ? $"+{delta}" : delta.ToString();
        string statName = GetStatName(stat);

        if (LocalizationManager.Instance != null)
        {
            var variables = new Dictionary<string, object>
            {
                ["amount"] = amount,
                ["stat"] = statName
            };

            return LocalizationManager.Instance.GetLocalizedString(
                "Floating Stat Popup",
                LocalizationDomains.UI,
                variables);
        }

        return $"{amount} {statName} Points";
    }

    private static string GetStatName(BasicStats stat)
    {
        string key = stat switch
        {
            BasicStats.Stamina => "Stat Energy",
            BasicStats.BoobsSensitivePoints => "Stat Boobs Sensitive Points",
            BasicStats.MouthSensitivePoints => "Stat Mouth Sensitive Points",
            BasicStats.PussySensitivePoints => "Stat Pussy Sensitive Points",
            BasicStats.ButtholeSensitivePoints => "Stat Butthole Sensitive Points",
            BasicStats.LewdLevel => "Stat Lewd Level",
            BasicStats.BoobsSensitiveLevel => "Stat Boobs Sensitive Level",
            BasicStats.MouthSensitiveLevel => "Stat Mouth Sensitive Level",
            BasicStats.PussySensitiveLevel => "Stat Pussy Sensitive Level",
            BasicStats.ButtholeSensitiveLevel => "Stat Butthole Sensitive Level",
            BasicStats.PregnancyChance => "Stat Pregnancy Chance",
            _ => $"Stat {stat}"
        };

        if (LocalizationManager.Instance != null &&
            LocalizationManager.Instance.TryGetLocalizedString(key, LocalizationDomains.UI, null, out string localized))
        {
            return localized;
        }

        return stat.ToString();
    }

    private readonly struct StatKey
    {
        private readonly RewardTarget target;
        private readonly BasicStats stat;

        public StatKey(RewardTarget target, BasicStats stat)
        {
            this.target = target;
            this.stat = stat;
        }

        public override int GetHashCode()
        {
            return ((int)target * 397) ^ (int)stat;
        }

        public override bool Equals(object obj)
        {
            return obj is StatKey other && other.target == target && other.stat == stat;
        }
    }
}

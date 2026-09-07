using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Short-lived floating text item for stat changes.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class FloatingStatPopupItem : MonoBehaviour
{
    private const string FaceColorProperty = "_FaceColor";
    private const string GlowColorProperty = "_GlowColor";
    private const string GlowPowerProperty = "_GlowPower";
    private const string GlowOuterProperty = "_GlowOuter";
    private const string GlowInnerProperty = "_GlowInner";
    private const string GlowKeyword = "GLOW_ON";

    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private CanvasGroup canvasGroup;
    [Header("Glow")]
    [SerializeField] private float glowOuter = 0.35f;
    [SerializeField] private float glowInner = 0.05f;
    [Header("Dynamic Layout")]
    [SerializeField] private float horizontalPadding = 120f;
    [SerializeField] private float verticalPadding = 24f;
    [SerializeField] private float minWidth = 260f;
    [SerializeField] private float maxWidth = 900f;

    private RectTransform rectTransform;
    private RectTransform textRectTransform;
    private Coroutine animationCoroutine;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (popupText == null)
            popupText = GetComponentInChildren<TextMeshProUGUI>(true);

        textRectTransform = popupText != null
            ? popupText.rectTransform
            : null;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (popupText != null)
            popupText.raycastTarget = false;
    }

    public void Play(
        string message,
        Vector2 startPosition,
        Vector2 stackedOffset,
        float riseDistance,
        float duration,
        Color textColor,
        Color glowColor)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        CacheReferences();

        if (popupText != null)
        {
            popupText.text = message;
        }

        ApplyDynamicLayout();
        ApplyColors(textColor, glowColor);

        rectTransform.anchoredPosition = startPosition + stackedOffset;
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        animationCoroutine = StartCoroutine(Animate(riseDistance, duration));
    }

    private void ApplyDynamicLayout()
    {
        if (popupText == null || rectTransform == null)
            return;

        float resolvedMinWidth = Mathf.Max(1f, minWidth);
        float resolvedMaxWidth = Mathf.Max(resolvedMinWidth, maxWidth);
        float resolvedHorizontalPadding = Mathf.Max(0f, horizontalPadding);
        float resolvedVerticalPadding = Mathf.Max(0f, verticalPadding);

        popupText.ForceMeshUpdate(true);
        Vector2 unconstrainedPreferred = popupText.GetPreferredValues(popupText.text, Mathf.Infinity, Mathf.Infinity);
        float popupWidth = Mathf.Clamp(Mathf.Max(1f, unconstrainedPreferred.x) + resolvedHorizontalPadding, resolvedMinWidth, resolvedMaxWidth);

        float textWidthConstraint = Mathf.Max(1f, popupWidth - resolvedHorizontalPadding);
        Vector2 constrainedPreferred = popupText.GetPreferredValues(popupText.text, textWidthConstraint, Mathf.Infinity);
        float textHeight = Mathf.Max(1f, constrainedPreferred.y);
        float popupHeight = Mathf.Max(rectTransform.sizeDelta.y, textHeight + resolvedVerticalPadding);

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, popupWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, popupHeight);

        if (textRectTransform != null && textRectTransform != rectTransform)
        {
            textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textRectTransform.pivot = new Vector2(0.5f, 0.5f);
            textRectTransform.anchoredPosition = Vector2.zero;
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, popupWidth);
            textRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, popupHeight);
        }

        popupText.ForceMeshUpdate(true);
    }

    private void ApplyColors(Color textColor, Color glowColor)
    {
        if (popupText == null)
            return;

        popupText.color = textColor;

        // Use a per-instance material so glow tweaks never leak to the shared font asset.
        Material fontMaterial = popupText.fontMaterial;
        if (fontMaterial == null)
            return;

        // Face stays a plain LDR color; the glow is what reads as a soft halo.
        if (fontMaterial.HasProperty(FaceColorProperty))
            fontMaterial.SetColor(FaceColorProperty, textColor);

        // TMP built-in Distance Field glow: self-contained in the text shader, so it
        // renders correctly on a Screen Space - Overlay canvas with no bloom camera
        // or post-processing, and is immune to scene color grading.
        if (fontMaterial.HasProperty(GlowColorProperty))
        {
            fontMaterial.EnableKeyword(GlowKeyword);
            fontMaterial.SetColor(GlowColorProperty, glowColor);

            if (fontMaterial.HasProperty(GlowPowerProperty))
                fontMaterial.SetFloat(GlowPowerProperty, glowColor.a);
            if (fontMaterial.HasProperty(GlowOuterProperty))
                fontMaterial.SetFloat(GlowOuterProperty, glowOuter);
            if (fontMaterial.HasProperty(GlowInnerProperty))
                fontMaterial.SetFloat(GlowInnerProperty, glowInner);
        }
    }

    private IEnumerator Animate(float riseDistance, float duration)
    {
        IsPlaying = true;
        canvasGroup.alpha = 0f;

        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0f, riseDistance);
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
            canvasGroup.alpha = t < 0.15f
                ? Mathf.Lerp(0f, 1f, t / 0.15f)
                : Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.45f) / 0.55f));

            yield return null;
        }

        canvasGroup.alpha = 0f;
        IsPlaying = false;
        animationCoroutine = null;
        Destroy(gameObject);
    }
}

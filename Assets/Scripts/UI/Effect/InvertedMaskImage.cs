using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

/// <summary>
/// Image that provides a soft inverted alpha mask to child <see cref="MaskedFillImage"/> components.
/// Semi-transparent pixels attenuate the child instead of binary-clipping it.
/// </summary>
public class InvertedMaskImage : Image
{
    private Mask _legacyMask;

    public Texture MaskTexture => mainTexture;

    public Color MaskTint => color;

    public Vector4 GetMaskLocalRect()
    {
        Rect rect = rectTransform.rect;
        return new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
    }

    public Vector4 GetMaskUVRect()
    {
        Sprite activeSprite = overrideSprite != null ? overrideSprite : sprite;
        if (activeSprite == null)
            return new Vector4(0f, 0f, 1f, 1f);

        Vector4 outerUv = DataUtility.GetOuterUV(activeSprite);
        return new Vector4(outerUv.x, outerUv.y, outerUv.z, outerUv.w);
    }

    protected override void Awake()
    {
        base.Awake();
        DisableLegacyMask();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        DisableLegacyMask();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        DisableLegacyMask();
    }

    protected override void Reset()
    {
        base.Reset();
        DisableLegacyMask();
    }
#endif

    private void DisableLegacyMask()
    {
        _legacyMask = GetComponent<Mask>();
        if (_legacyMask == null)
            return;

        _legacyMask.showMaskGraphic = true;
        _legacyMask.enabled = false;
    }
}

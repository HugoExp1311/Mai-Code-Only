using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Image that renders through the inverse alpha of a parent InvertedMaskImage.
/// </summary>
public class MaskedFillImage : Image
{
    private const string InvertedMaskShaderResourcePath = "InvertedAlphaMaskUI";

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
    private static readonly int MaskColorId = Shader.PropertyToID("_MaskColor");
    private static readonly int MaskRectId = Shader.PropertyToID("_MaskRect");
    private static readonly int MaskUVRectId = Shader.PropertyToID("_MaskUVRect");
    private static readonly int MaskWorldToLocalId = Shader.PropertyToID("_MaskWorldToLocal");

    private Material _material;
    private InvertedMaskImage _maskImage;
    private static Shader _cachedShader;

    protected override void Awake()
    {
        base.Awake();
        CacheMaskImage();
        CreateMaterial();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CacheMaskImage();
        SetMaterialDirty();
    }

    protected override void OnTransformParentChanged()
    {
        base.OnTransformParentChanged();
        CacheMaskImage();
        SetMaterialDirty();
    }

    protected override void OnCanvasHierarchyChanged()
    {
        base.OnCanvasHierarchyChanged();
        CacheMaskImage();
        SetMaterialDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        ReleaseMaterial(destroyImmediately: true);
        CacheMaskImage();
        CreateMaterial();
        SetMaterialDirty();
    }
#endif

    private void CacheMaskImage()
    {
        _maskImage = transform.parent == null
            ? null
            : transform.parent.GetComponentInParent<InvertedMaskImage>(true);
    }

    private void CreateMaterial()
    {
        if (_material == null)
        {
            if (_cachedShader == null)
            {
                _cachedShader = Resources.Load<Shader>(InvertedMaskShaderResourcePath);
                if (_cachedShader == null)
                {
                    Debug.LogWarning($"[MaskedFillImage] Missing shader resource '{InvertedMaskShaderResourcePath}'.");
                    return;
                }
            }

            _material = new Material(_cachedShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            material = _material;
        }
    }

    public override Material materialForRendering
    {
        get
        {
            if (_maskImage == null)
                CacheMaskImage();

            CreateMaterial();
            if (_material == null)
                return base.materialForRendering;

            Material renderedMaterial = base.materialForRendering;
            UpdateMaterialProperties(_material);

            if (renderedMaterial != null && renderedMaterial != _material)
                UpdateMaterialProperties(renderedMaterial);

            return renderedMaterial;
        }
    }

    private void UpdateMaterialProperties(Material targetMaterial)
    {
        if (targetMaterial == null)
            return;

        targetMaterial.SetTexture(MainTexId, mainTexture);
        targetMaterial.SetColor(ColorId, color);

        if (_maskImage == null || !_maskImage.isActiveAndEnabled)
        {
            targetMaterial.SetTexture(MaskTexId, Texture2D.whiteTexture);
            targetMaterial.SetColor(MaskColorId, Color.clear);
            targetMaterial.SetVector(MaskRectId, new Vector4(0f, 0f, 1f, 1f));
            targetMaterial.SetVector(MaskUVRectId, new Vector4(0f, 0f, 1f, 1f));
            targetMaterial.SetMatrix(MaskWorldToLocalId, Matrix4x4.identity);
            return;
        }

        targetMaterial.SetTexture(MaskTexId, _maskImage.MaskTexture);
        targetMaterial.SetColor(MaskColorId, _maskImage.MaskTint);
        targetMaterial.SetVector(MaskRectId, _maskImage.GetMaskLocalRect());
        targetMaterial.SetVector(MaskUVRectId, _maskImage.GetMaskUVRect());
        targetMaterial.SetMatrix(MaskWorldToLocalId, _maskImage.rectTransform.worldToLocalMatrix);
    }

    protected override void OnDestroy()
    {
        ReleaseMaterial(destroyImmediately: false);
        base.OnDestroy();
    }

    private void ReleaseMaterial(bool destroyImmediately)
    {
        if (_material == null)
            return;

        if (material == _material)
            material = null;

#if UNITY_EDITOR
        if (destroyImmediately || !Application.isPlaying)
            DestroyImmediate(_material);
        else
#endif
            Destroy(_material);

        _material = null;
    }
}

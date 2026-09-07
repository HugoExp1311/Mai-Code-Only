#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RectTransformContextMenu
{
    [MenuItem("CONTEXT/RectTransform/Calculate Anchors Relative to Canvas")]
    private static void CalculateAnchorsRelativeToCanvas(MenuCommand menuCommand)
    {
        RectTransform rectTransform = menuCommand.context as RectTransform;

        if (rectTransform == null)
        {
            Debug.LogWarning("The selected object does not have a RectTransform.");
            return;
        }

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("The selected RectTransform is not inside a Canvas.");
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        Rect rect = rectTransform.rect;
        Vector2 position = rectTransform.localPosition;
        Vector2 size = new Vector2(rect.width, rect.height);

        Vector2 anchorMin = new Vector2(
            (position.x - size.x * 0.5f) / canvasSize.x + 0.5f,
            (position.y - size.y * 0.5f) / canvasSize.y + 0.5f
        );
        Vector2 anchorMax = new Vector2(
            (position.x + size.x * 0.5f) / canvasSize.x + 0.5f,
            (position.y + size.y * 0.5f) / canvasSize.y + 0.5f
        );

        Undo.RecordObject(rectTransform, "Set Anchors");
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log("Anchors calculated and applied relative to Canvas.");
    }

    [MenuItem("CONTEXT/RectTransform/Calculate Anchors Relative to Parent")]
    private static void CalculateAnchorsRelativeToParent(MenuCommand menuCommand)
    {
        RectTransform rectTransform = menuCommand.context as RectTransform;

        if (rectTransform == null)
        {
            Debug.LogWarning("The selected object does not have a RectTransform.");
            return;
        }

        RectTransform parentRectTransform = rectTransform.parent as RectTransform;

        if (parentRectTransform == null)
        {
            Debug.LogWarning("The selected RectTransform does not have a RectTransform parent.");
            return;
        }

        Vector2 parentSize = parentRectTransform.rect.size;
        Vector2 position = rectTransform.localPosition;
        Vector2 size = new Vector2(rectTransform.rect.width, rectTransform.rect.height);

        Vector2 anchorMin = new Vector2(
            (position.x - size.x * 0.5f) / parentSize.x + 0.5f,
            (position.y - size.y * 0.5f) / parentSize.y + 0.5f
        );
        Vector2 anchorMax = new Vector2(
            (position.x + size.x * 0.5f) / parentSize.x + 0.5f,
            (position.y + size.y * 0.5f) / parentSize.y + 0.5f
        );

        Undo.RecordObject(rectTransform, "Set Anchors");
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Debug.Log("Anchors calculated and applied relative to Parent.");
    }
}
#endif

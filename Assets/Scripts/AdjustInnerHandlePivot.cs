using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class AdjustInnerHandlePivot : MonoBehaviour
{
    // called by ScrollRect.onValueChanged
    public void AdjustPivotHorizontal(Vector2 scrollValue)
        => AdjustPivot(scrollValue.x, 0);

    // called by ScrollRect.onValueChanged
    public void AdjustPivotVertical(Vector2 scrollValue)
        => AdjustPivot(scrollValue.y, 1);

    void AdjustPivot(float scrollValue, int axis)
    {
        RectTransform rt = transform as RectTransform;

        var pivot = rt.pivot;
        pivot[axis] = scrollValue;
        rt.pivot = pivot;

        rt.anchoredPosition = Vector2.zero;
    }
}
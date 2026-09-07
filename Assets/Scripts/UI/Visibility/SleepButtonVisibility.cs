using UnityEngine;

/// <summary>
/// Manages Sleep button visibility - always available.
/// Simplified: no event subscriptions needed since button is always active.
/// </summary>
public class SleepButtonVisibility : MonoBehaviour
{
    private void OnEnable()
    {
        // Sleep button is always available — ensure it's active when enabled
        gameObject.SetActive(true);
    }
}

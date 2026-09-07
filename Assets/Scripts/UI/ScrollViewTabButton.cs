using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple button component that switches ScrollViewTabManager sections
/// Attach to buttons that should trigger section changes
/// </summary>
[RequireComponent(typeof(Button))]
public class ScrollViewTabButton : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The ScrollViewTabManager to control")]
    [SerializeField] private ScrollViewTabManager tabManager;
    
    [Tooltip("The section ID to switch to when clicked")]
    [SerializeField] private string targetSectionId;
    
    private Button button;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClick);
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
    
    private void OnButtonClick()
    {
        if (tabManager != null && !string.IsNullOrEmpty(targetSectionId))
        {
            tabManager.SwitchToSection(targetSectionId);
        }
    }
}

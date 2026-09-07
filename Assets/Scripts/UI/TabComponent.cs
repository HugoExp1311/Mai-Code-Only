using UnityEngine;

/// <summary>
/// Component to mark a GameObject or UIPanel as a tab
/// Used for tab identification and auto-discovery
/// </summary>
public class TabComponent : MonoBehaviour
{
    [Header("Tab Identification")]
    [Tooltip("Unique identifier for this tab (e.g., 'Profile', 'Settings')")]
    [SerializeField] private string tabId = "";
    
    public string TabId => tabId;
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(tabId))
        {
            // Auto-generate tab ID from GameObject name if empty
            tabId = gameObject.name;
        }
    }
}


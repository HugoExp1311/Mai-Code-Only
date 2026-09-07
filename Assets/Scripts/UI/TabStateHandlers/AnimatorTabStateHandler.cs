using UnityEngine;

/// <summary>
/// Tab state handler using Unity Animator Controller
/// Sets bool parameters on the animator to control tab button states
/// </summary>
[System.Serializable]
public class AnimatorTabStateHandler : ITabStateHandler
{
    [SerializeField] private Animator animator;
    [SerializeField] private string[] tabIds;
    
    public Animator Animator
    {
        get => animator;
        set => animator = value;
    }
    
    public void Initialize(string[] tabIds)
    {
        this.tabIds = tabIds;
    }
    
    public void SwitchToTab(string activeTabId)
    {
        if (animator == null)
        {
            Debug.LogWarning("[AnimatorTabStateHandler] Animator is null. Cannot update tab states.");
            return;
        }
        
        if (tabIds == null || tabIds.Length == 0)
        {
            Debug.LogWarning("[AnimatorTabStateHandler] Tab IDs not initialized.");
            return;
        }
        
        // Reset all tab states to false
        foreach (string tabId in tabIds)
        {
            if (!string.IsNullOrEmpty(tabId))
            {
                animator.SetBool(tabId, false);
            }
        }
        
        // Set the active tab to true
        if (!string.IsNullOrEmpty(activeTabId))
        {
            animator.SetBool(activeTabId, true);
        }
    }
    
    public void ResetAllStates()
    {
        if (animator == null || tabIds == null)
        {
            Debug.LogWarning("[AnimatorTabStateHandler] ResetAllStates: Animator or tabIds is null");
            return;
        }
        
        // Reset all tab states to false
        foreach (string tabId in tabIds)
        {
            if (!string.IsNullOrEmpty(tabId))
            {
                animator.SetBool(tabId, false);
            }
        }
    }
}


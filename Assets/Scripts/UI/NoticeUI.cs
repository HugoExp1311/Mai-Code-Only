using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Base;
using Base.Localization;
using Events;
using EventBus;

/// <summary>
/// Simple notice/notification UI for displaying messages to the player
/// Auto-closes after a duration
/// Subscribes to PlaceUnavailableEvent to show area restriction notices
/// </summary>
public class NoticeUI : UIPanel
{
    [Header("UI References")]
    [SerializeField] private LocalizedText messageText;
    
    [Header("Settings")]
    [SerializeField] private float autoCloseDuration = 2f;
    
    private Coroutine autoCloseCoroutine;
    private static NoticeUI cachedInstance;
    private EventBinding<PlaceUnavailableEvent> placeUnavailableBinding;
    
    private void Awake()
    {
        // Cache this instance for static access
        cachedInstance = this;
        
        // Subscribe to PlaceUnavailableEvent
        placeUnavailableBinding = new EventBinding<PlaceUnavailableEvent>(HandlePlaceUnavailable);
        EventBus<PlaceUnavailableEvent>.Register(placeUnavailableBinding);
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (placeUnavailableBinding != null)
        {
            EventBus<PlaceUnavailableEvent>.Deregister(placeUnavailableBinding);
            placeUnavailableBinding = null;
        }
    }
    
    /// <summary>
    /// Handle place unavailable event - show notice and play SFX
    /// </summary>
    private void HandlePlaceUnavailable(PlaceUnavailableEvent args)
    {
        // Show localized notice
        ShowNoticeLocalized("Notice Area Restricted");
        
        // Play notice sound
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
        }
    }
    
    /// <summary>
    /// Show notice with a direct message (non-localized)
    /// </summary>
    public void ShowNotice(string message)
    {
        if (messageText != null)
        {
#pragma warning disable CS0618 // Intentional use for non-localized fallback messages
            messageText.SetDirect(message);
#pragma warning restore CS0618
        }
        
        Show();
        
        // Start auto-close timer
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }
        autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }
    
    /// <summary>
    /// Show notice with localized key
    /// </summary>
    public void ShowNoticeLocalized(string localizationKey, Dictionary<string, object> variables = null)
    {
        if (messageText != null)
        {
            messageText.SetLocalized(localizationKey, LocalizationDomains.UI, variables);
        }
        
        Show();
        
        // Start auto-close timer
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }
        autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
    }
    
    private IEnumerator AutoCloseAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDuration);
        CloseNotice();
    }
    
    /// <summary>
    /// Close the notice (called by auto-close timer)
    /// </summary>
    private void CloseNotice()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
        
        Hide();
    }
    
    /// <summary>
    /// Static helper to show notice from anywhere (direct message)
    /// </summary>
    public static void Show(string message)
    {
        // Try to use cached instance first
        if (cachedInstance != null)
        {
            cachedInstance.ShowNotice(message);
            return;
        }
        
        // Try to find via UIPanelManager
        if (UIPanelManager.Instance != null)
        {
            var noticePanel = UIPanelManager.Instance.GetPanel("Notice Popup") as NoticeUI;
            if (noticePanel != null)
            {
                cachedInstance = noticePanel; // Cache for future use
                noticePanel.ShowNotice(message);
                return;
            }
        }
        
        // Fallback: try to find in scene directly (including inactive objects)
        var foundPanel = FindFirstObjectByType<NoticeUI>(FindObjectsInactive.Include);
        if (foundPanel != null)
        {
            cachedInstance = foundPanel; // Cache for future use
            foundPanel.ShowNotice(message);
            return;
        }
        
        // Last resort: log to console
        Debug.Log($"[Notice] {message}");
    }
    
    /// <summary>
    /// Static helper to show localized notice from anywhere
    /// </summary>
    public static void ShowLocalized(string localizationKey, Dictionary<string, object> variables = null)
    {
        // Try to use cached instance first
        if (cachedInstance != null)
        {
            cachedInstance.ShowNoticeLocalized(localizationKey, variables);
            return;
        }
        
        // Try to find via UIPanelManager
        if (UIPanelManager.Instance != null)
        {
            var noticePanel = UIPanelManager.Instance.GetPanel("Notice Popup") as NoticeUI;
            if (noticePanel != null)
            {
                cachedInstance = noticePanel; // Cache for future use
                noticePanel.ShowNoticeLocalized(localizationKey, variables);
                return;
            }
        }
        
        // Fallback: try to find in scene directly (including inactive objects)
        var foundPanel = FindFirstObjectByType<NoticeUI>(FindObjectsInactive.Include);
        if (foundPanel != null)
        {
            cachedInstance = foundPanel; // Cache for future use
            foundPanel.ShowNoticeLocalized(localizationKey, variables);
            return;
        }
        
        // Last resort: log to console
        Debug.Log($"[Notice] {localizationKey}");
    }
}

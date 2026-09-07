using UnityEngine;
using UnityEngine.UI;
using Base.Dialogues;
using Base.CG;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI panel for displaying CG (Computer Graphics) background images during dialogue
/// Subscribes to DialogueManager events to update CG sprites from CGDialogueLine
/// Inherits from UIPanel for proper integration with UIPanelManager
/// </summary>
public class CGPanel : UIPanel
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    [Header("Fade Reveal Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    
    // Track the last displayed sprite to preserve it when showing choices
    private Sprite lastDisplayedSprite = null;
    private bool areChoicesDisplayed = false;
    private bool isFadeRevealing = false;
    private Coroutine fadeRevealCoroutine = null;
    
    private void Start()
    {
        // Subscribe to dialogue events in Start() - ensures DialogueManager.Instance is available
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart += HandleDialogueStart;
            DialogueManager.Instance.OnDialogueEnd += HandleDialogueEnd;
            DialogueManager.Instance.OnDialogueLineDisplayed += HandleDialogueLineDisplayed;
        }
        else
        {
            Debug.LogWarning("[CGPanel] DialogueManager.Instance is null! Cannot subscribe to dialogue events.");
        }
        
        // Subscribe to DialogueManager choice events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnChoicesDisplayed += HandleChoicesDisplayed;
            DialogueManager.Instance.OnChoicesCleared += HandleChoicesCleared;
            if (enableDebugLogs) Log("Successfully subscribed to DialogueManager choice events");
        }
        else
        {
            LogWarning("DialogueManager.Instance is NULL! Cannot subscribe to choice events.");
        }
    }
    
    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart -= HandleDialogueStart;
            DialogueManager.Instance.OnDialogueEnd -= HandleDialogueEnd;
            DialogueManager.Instance.OnDialogueLineDisplayed -= HandleDialogueLineDisplayed;
            DialogueManager.Instance.OnChoicesDisplayed -= HandleChoicesDisplayed;
            DialogueManager.Instance.OnChoicesCleared -= HandleChoicesCleared;
        }
    }
    
    /// <summary>
    /// Whether the panel is currently in the middle of a fade reveal animation
    /// </summary>
    public bool IsFadeRevealing => isFadeRevealing;
    
    /// <summary>
    /// Called when panel is shown
    /// </summary>
    protected override void OnShow(object data = null)
    {
        base.OnShow(data);
        if (enableDebugLogs) Log("CGPanel shown - starting with black background");
        
        // Start with black background - sprite will be set when dialogue lines are displayed
        lastDisplayedSprite = null;
        SetBackgroundImage(null, Color.black);
    }
    
    /// <summary>
    /// Called when panel is hidden
    /// </summary>
    protected override void OnHide()
    {
        base.OnHide();
        if (enableDebugLogs) Log("CGPanel hidden");
        
        // Clear CG image using UIPanel's SetBackgroundImage
        lastDisplayedSprite = null;
        SetBackgroundImage(null, Color.black);
    }
    
    /// <summary>
    /// Handle dialogue start - clear any previous CG image
    /// </summary>
    private void HandleDialogueStart()
    {
        if (enableDebugLogs) Debug.Log("[CGPanel] HandleDialogueStart CALLED - initializing with black background");
        
        // Start with black background - sprite will be set when first CGDialogueLine is displayed
        lastDisplayedSprite = null;
        SetBackgroundImage(null, Color.black);
    }
    
    /// <summary>
    /// Handle dialogue end - clear CG image
    /// </summary>
    private void HandleDialogueEnd()
    {
        if (enableDebugLogs) Log("Dialogue ended - preserving sprite for transition");
        
        // CRITICAL: Do NOT clear the sprite here
        // Preserve the last displayed sprite so the transition fade-in is visible
        // The sprite will be cleared when OnHide() is called during section switch
    }
    
    /// <summary>
    /// Handle dialogue line displayed - update CG sprite if this is a CG dialogue line
    /// Ignores normal dialogue lines (DialogueLine)
    /// Preserves last sprite when showing choices OR when line has empty text (choice preparation node)
    /// </summary>
    private void HandleDialogueLineDisplayed(DialogueLine line)
    {
        if (enableDebugLogs) Log($"HandleDialogueLineDisplayed called - areChoicesDisplayed: {areChoicesDisplayed}");
        
        // Check if this is a CG dialogue line
        if (line is CGDialogueLine cgLine)
        {
            bool hasTextSource = !string.IsNullOrEmpty(cgLine.localizationKey);
            if (enableDebugLogs) Log($"Processing CGDialogueLine - has sprite: {cgLine.cgSprite != null}, has text: {hasTextSource}");
            
            if (cgLine.cgSprite != null)
            {
                // Detect black-to-sprite transition: background is currently black and next line has a CG sprite
                bool isBlackToSpriteTransition = lastDisplayedSprite == null;
                
                if (isBlackToSpriteTransition)
                {
                    if (enableDebugLogs) Log($"Black-to-sprite transition detected! Starting fade reveal for: {cgLine.cgSprite.name}");
                    
                    // Start the fade reveal coroutine
                    if (fadeRevealCoroutine != null)
                        StopCoroutine(fadeRevealCoroutine);
                    fadeRevealCoroutine = StartCoroutine(FadeRevealCoroutine(cgLine.cgSprite));
                }
                else
                {
                    if (enableDebugLogs) Log($"Displaying CG sprite: {cgLine.cgSprite.name}");
                    
                    // Normal sprite change (not from black) - update immediately
                    lastDisplayedSprite = cgLine.cgSprite;
                    SetBackgroundImage(cgLine.cgSprite, Color.white);
                }
            }
            else
            {
                // No sprite specified - check if we should preserve or clear
                bool hasEmptyText = !hasTextSource;
                bool shouldPreserve = areChoicesDisplayed || (hasEmptyText && lastDisplayedSprite != null);
                
                if (shouldPreserve)
                {
                    if (enableDebugLogs) Log($"CGDialogueLine has no sprite but preserving last sprite: {lastDisplayedSprite?.name ?? "NULL"} (choices: {areChoicesDisplayed}, emptyText: {hasEmptyText})");
                    // Preserve last sprite when choices are shown OR when line is empty (choice preparation)
                }
                else
                {
                    if (enableDebugLogs) Log("CGDialogueLine has no sprite and no preservation needed - setting background to black");
                    // Set to black for regular lines with no sprite
                    lastDisplayedSprite = null;
                    SetBackgroundImage(null, Color.black);
                }
            }
        }
        else
        {
            // Ignore normal dialogue lines - they use Live2D animations
            if (enableDebugLogs) Log($"Ignoring normal DialogueLine - no CG sprite");
        }
    }
    
    /// <summary>
    /// Coroutine that handles the fade-out reveal when transitioning from black to a CG sprite.
    /// Sets the sprite with Color.black (invisible) then animates the color from black to white
    /// to smoothly reveal the CG background.
    /// Uses CanvasGroup alpha to hide dialogue (immune to SwitchSpeakerBox re-activation).
    /// </summary>
    private IEnumerator FadeRevealCoroutine(Sprite newSprite)
    {
        isFadeRevealing = true;
        if (enableDebugLogs) Debug.Log($"[CGPanel] FadeRevealCoroutine STARTED for sprite: {newSprite.name}");
        
        // Hide dialogue using CanvasGroup alpha instead of SetActive.
        // SwitchSpeakerBox (called by SetDialogueText) uses SetActive(true) on child GameObjects,
        // which undoes HideDialoguePanel(). But CanvasGroup.alpha=0 stays invisible regardless
        // of child active states — it's immune to SwitchSpeakerBox.
        CanvasGroup dialogueCanvasGroup = null;
        if (DialogueManager.Instance != null)
        {
            var dialogueUI = DialogueManager.Instance.GetDialogueUI();
            if (dialogueUI is MonoBehaviour dialogueMono)
            {
                dialogueCanvasGroup = dialogueMono.GetComponent<CanvasGroup>();
                if (dialogueCanvasGroup != null)
                {
                    dialogueCanvasGroup.alpha = 0f;
                    dialogueCanvasGroup.blocksRaycasts = false;
                }
            }
        }
        
        // Set the sprite with Color.black (invisible - black tint hides it)
        lastDisplayedSprite = newSprite;
        SetBackgroundImage(newSprite, Color.black);
        
        // Animate the background color from black to white to reveal the sprite
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetBackgroundImage(newSprite, Color.Lerp(Color.black, Color.white, t));
            yield return null;
        }
        SetBackgroundImage(newSprite, Color.white);
        
        // Restore dialogue visibility
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f;
            dialogueCanvasGroup.blocksRaycasts = true;
        }
        
        isFadeRevealing = false;
        fadeRevealCoroutine = null;
        if (enableDebugLogs) Log("FadeRevealCoroutine finished");
    }
    
    private void HandleChoicesDisplayed()
    {
        areChoicesDisplayed = true;
        if (enableDebugLogs) Log($"[EVENT] Choices displayed - flag set to TRUE. Last sprite: {lastDisplayedSprite?.name ?? "NULL"}");
    }
    
    private void HandleChoicesCleared()
    {
        areChoicesDisplayed = false;
        if (enableDebugLogs) Log("[EVENT] Choices cleared - flag set to FALSE");
    }
    
    private void Log(string message)
    {
        Debug.Log($"[CGPanel] {message}");
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[CGPanel] {message}");
    }
}

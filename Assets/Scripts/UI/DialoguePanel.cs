using System;
using System.Collections;
using System.Collections.Generic;
using Base.Character.Stats;
using Base.CG;
using Base.Dialogues;
using Base.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Main dialogue panel that handles displaying dialogue text, choices,
/// speaker switching, monologue mode, and smart line cutting.
/// Implements IDialogueUI for communication with DialogueManager.
/// </summary>
public class DialoguePanel : UIPanel, IDialogueUI
{
    public static RectTransform ActiveMonologueRect { get; private set; }

    [Header("Dialogue UI Elements - Right Side (Primary for Boss, Secondary for Player)")]
    [SerializeField] private GameObject playerDialogueBox;
    [SerializeField] private LocalizedText playerDialogueText;
    [SerializeField] private GameObject bossDialogueBox;
    [SerializeField] private LocalizedText bossDialogueText;

    [Header("Dialogue UI Elements - Left Side (Primary for Player, Secondary for Boss)")]
    [SerializeField] private GameObject playerDialogueBoxLeft;
    [SerializeField] private LocalizedText playerDialogueTextLeft;
    [SerializeField] private GameObject bossDialogueBoxLeft;
    [SerializeField] private LocalizedText bossDialogueTextLeft;

    [Header("Other Dialogue Boxes")]
    [SerializeField] private GameObject simulationDialogueBox;
    [SerializeField] private LocalizedText simulationDialogueText;
    [SerializeField] private GameObject monologueDialogueBox;
    [SerializeField] private LocalizedText monologueDialogueText;
    [SerializeField] private Transform dialogueUIChoicesContainer;
    [SerializeField] private GameObject dialogueUIChoiceButtonPrefab;

    // Prefab component references (validated in Awake, used for instantiation)
    private Button prefabButtonComponent;
    private LocalizedText prefabTextComponent;

    // Dialogue system state
    private Coroutine uiTypingCoroutine;
    private List<Button> activeChoiceButtons = new List<Button>();
    private LocalizedText _currentActiveDialogueText;
    private bool _isShowingChoices = false;

    // Smart line cutting - sub-line queue state
    private static readonly char[] LineSeparators = new char[] {
        '.', ',', ';', '!', '?', ':', '—', '…',       // Latin
        '。', '、', '！', '？', '：', '）', '」', '』'    // Japanese
    };
    private List<string> _splitParts = new List<string>();
    private int _currentSplitIndex = 0;
    private RewardTarget _splitSpeaker;
    private FontStyles _splitFontStyle;
    private bool _splitInProgress = false;
    private bool _currentPartFullyTyped = false;
    private float _splitAdvanceCooldown = 0f;
    private const float SPLIT_ADVANCE_DELAY = 0.25f;
    private Coroutine _innerTypingCoroutine;

    // OPT-35: Cache TMP_Text component lookups to avoid GetComponent per dialogue line
    private readonly Dictionary<LocalizedText, TMP_Text> _tmpTextCache = new();

    private event Action _onDialogueAdvanceInput;

    // Public events for other panels to subscribe to
    public event Action OnChoicesDisplayed;
    public event Action OnChoicesCleared;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // IDialogueUI implementation
    bool IDialogueUI.IsVisible => IsVisible;
    bool IDialogueUI.HasPendingSplitParts => _splitInProgress;
    event Action IDialogueUI.OnDialogueAdvanceInput
    {
        add { _onDialogueAdvanceInput += value; }
        remove { _onDialogueAdvanceInput -= value; }
    }

    private void Awake()
    {
        if (enableDebugLogs) Debug.Log("[DialoguePanel] Awake called");

        // Ensure panel starts hidden
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }

        // CRITICAL: Ensure all dialogue boxes start inactive
        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
        if (playerDialogueBoxLeft != null) playerDialogueBoxLeft.SetActive(false);
        if (bossDialogueBox != null) bossDialogueBox.SetActive(false);
        if (bossDialogueBoxLeft != null) bossDialogueBoxLeft.SetActive(false);
        if (simulationDialogueBox != null) simulationDialogueBox.SetActive(false);
        if (monologueDialogueBox != null) monologueDialogueBox.SetActive(false);
        if (monologueDialogueBox != null) ActiveMonologueRect = monologueDialogueBox.transform as RectTransform;

        // Validate prefab structure and cache component references
        if (dialogueUIChoiceButtonPrefab != null)
        {
            prefabButtonComponent = dialogueUIChoiceButtonPrefab.GetComponent<Button>();
            prefabTextComponent = dialogueUIChoiceButtonPrefab.GetComponentInChildren<LocalizedText>();

            if (prefabButtonComponent == null)
            {
                Debug.LogError($"[DialoguePanel] Choice button prefab '{dialogueUIChoiceButtonPrefab.name}' is missing Button component!");
            }
            if (prefabTextComponent == null)
            {
                Debug.LogError($"[DialoguePanel] Choice button prefab '{dialogueUIChoiceButtonPrefab.name}' is missing LocalizedText component!");
            }
        }
        else
        {
            Debug.LogError("[DialoguePanel] Choice button prefab not assigned in Inspector!");
        }
    }

    private void OnDestroy()
    {
        if (ActiveMonologueRect != null && monologueDialogueBox != null && ActiveMonologueRect == (monologueDialogueBox.transform as RectTransform))
            ActiveMonologueRect = null;
    }

    private void Update()
    {
        // Handle dialogue advance input
        if (IsVisible)
        {
            // CRITICAL: Block input during transitions
            if (IsTransitionActive()) return;

            // Decrement split advance cooldown
            if (_splitAdvanceCooldown > 0f)
                _splitAdvanceCooldown -= Time.deltaTime;

            // Check input
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touchBegan = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool hasInput = mouseClicked || touchBegan || spacePressed;

            if (hasInput && !_isShowingChoices)
            {
                // If sub-lines are in progress, handle locally instead of forwarding to DialogueManager
                if (_splitInProgress)
                {
                    HandleSplitAdvance();
                }
                else
                {
                    _onDialogueAdvanceInput?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Check if a transition is currently active (blocking input)
    /// </summary>
    private bool IsTransitionActive()
    {
        if (TransitionPanel.Instance == null) return false;
        return TransitionPanel.Instance.gameObject.activeSelf;
    }

    protected override void OnShow(object data)
    {
        base.OnShow(data);
        // Dialogue panel is shown by DialogueManager, not here
    }

    protected override void OnHide()
    {
        base.OnHide();
        ClearChoices();
    }

    #region IDialogueUI Implementation

    public void ShowDialoguePanel(RewardTarget initialSpeaker)
    {
        if (enableDebugLogs) Debug.Log($"[DialoguePanel] ShowDialoguePanel called: initialSpeaker={initialSpeaker}");

        // Hide simulation dialogue box when showing normal dialogue
        if (simulationDialogueBox != null && simulationDialogueBox.activeSelf)
        {
            if (enableDebugLogs) Debug.Log("[DialoguePanel] Hiding simulationDialogueBox for normal dialogue");
            simulationDialogueBox.SetActive(false);
        }

        if (UIPanelManager.Instance != null)
        {
            if (enableDebugLogs) Debug.Log($"[DialoguePanel] Using UIPanelManager to show panel: {PanelId}");
            UIPanelManager.Instance.ShowPanel(PanelId);
        }
        else
        {
            if (enableDebugLogs) Debug.Log("[DialoguePanel] UIPanelManager not found, using direct Show()");
            Show();
        }

        SwitchSpeakerBox(initialSpeaker);
        _isShowingChoices = false;

        if (enableDebugLogs) Debug.Log($"[DialoguePanel] After ShowDialoguePanel: IsVisible={IsVisible}");
    }

    /// <summary>
    /// Show the dialogue panel immediately without fade animation.
    /// Used for chained dialogues where EndDialogue hid the panel between sequences.
    /// </summary>
    public void ShowDialoguePanelImmediate(RewardTarget initialSpeaker)
    {
        if (enableDebugLogs) Debug.Log($"[DialoguePanel] ShowDialoguePanelImmediate called: initialSpeaker={initialSpeaker}");

        // Hide simulation dialogue box when showing normal dialogue
        if (simulationDialogueBox != null && simulationDialogueBox.activeSelf)
        {
            simulationDialogueBox.SetActive(false);
        }

        // Use base ShowImmediate() which sets isVisible, activates GO, and sets alpha=1
        // without triggering fade animation
        ShowImmediate();

        SwitchSpeakerBox(initialSpeaker);
        _isShowingChoices = false;

        if (enableDebugLogs) Debug.Log($"[DialoguePanel] After ShowDialoguePanelImmediate: IsVisible={IsVisible}");
    }

    public void HideDialoguePanel()
    {
        if (UIPanelManager.Instance != null)
        {
            UIPanelManager.Instance.HidePanel(PanelId);
        }
        else
        {
            Hide();
        }

        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
        if (playerDialogueBoxLeft != null) playerDialogueBoxLeft.SetActive(false);
        if (bossDialogueBox != null) bossDialogueBox.SetActive(false);
        if (bossDialogueBoxLeft != null) bossDialogueBoxLeft.SetActive(false);
        if (monologueDialogueBox != null) monologueDialogueBox.SetActive(false);
        _currentActiveDialogueText = null;
        ClearSplitState();
        _isShowingChoices = false;
    }

    public void ClearAllDialogueBoxes()
    {
        // Stop any active typing
        if (uiTypingCoroutine != null)
        {
            StopCoroutine(uiTypingCoroutine);
            uiTypingCoroutine = null;
        }
        if (_innerTypingCoroutine != null)
        {
            StopCoroutine(_innerTypingCoroutine);
            _innerTypingCoroutine = null;
        }

        ClearSplitState();

        // Hide all dialogue boxes
        if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
        if (playerDialogueBoxLeft != null) playerDialogueBoxLeft.SetActive(false);
        if (bossDialogueBox != null) bossDialogueBox.SetActive(false);
        if (bossDialogueBoxLeft != null) bossDialogueBoxLeft.SetActive(false);
        if (monologueDialogueBox != null) monologueDialogueBox.SetActive(false);

        // Clear text on all boxes
        if (playerDialogueText != null) playerDialogueText.SetDirect("");
        if (playerDialogueTextLeft != null) playerDialogueTextLeft.SetDirect("");
        if (bossDialogueText != null) bossDialogueText.SetDirect("");
        if (bossDialogueTextLeft != null) bossDialogueTextLeft.SetDirect("");
        if (monologueDialogueText != null) monologueDialogueText.SetDirect("");

        _currentActiveDialogueText = null;
    }

    private void SwitchSpeakerBox(RewardTarget speaker, bool isMonologue = false)
    {
        if (enableDebugLogs) Debug.Log($"[DialoguePanel] SwitchSpeakerBox called: speaker={speaker}, isMonologue={isMonologue}");

        if (isMonologue)
        {
            // Monologue mode: show only monologue box, hide everything else
            if (playerDialogueBox != null) playerDialogueBox.SetActive(false);
            if (playerDialogueBoxLeft != null) playerDialogueBoxLeft.SetActive(false);
            if (bossDialogueBox != null) bossDialogueBox.SetActive(false);
            if (bossDialogueBoxLeft != null) bossDialogueBoxLeft.SetActive(false);
            if (monologueDialogueBox != null) monologueDialogueBox.SetActive(true);
            if (monologueDialogueBox != null) ActiveMonologueRect = monologueDialogueBox.transform as RectTransform;
            _currentActiveDialogueText = monologueDialogueText;
        }
        else
        {
            // Determine the target box BEFORE hiding anything to avoid flicker
            GameObject targetBox;
            LocalizedText targetText;

            if (speaker == RewardTarget.Player)
            {
                targetBox = playerDialogueBoxLeft;
                targetText = playerDialogueTextLeft;
            }
            else
            {
                targetBox = bossDialogueBox;
                targetText = bossDialogueText;
            }

            // Hide all NON-TARGET boxes (skip the target to prevent flicker)
            if (monologueDialogueBox != null) monologueDialogueBox.SetActive(false);
            if (playerDialogueBox != null && playerDialogueBox != targetBox) playerDialogueBox.SetActive(false);
            if (playerDialogueBoxLeft != null && playerDialogueBoxLeft != targetBox) playerDialogueBoxLeft.SetActive(false);
            if (bossDialogueBox != null && bossDialogueBox != targetBox) bossDialogueBox.SetActive(false);
            if (bossDialogueBoxLeft != null && bossDialogueBoxLeft != targetBox) bossDialogueBoxLeft.SetActive(false);

            // Activate target (no-op if already active, no flicker)
            if (targetBox != null) targetBox.SetActive(true);
            _currentActiveDialogueText = targetText;
        }

        if (enableDebugLogs) Debug.Log($"[DialoguePanel] Current active text component: {(_currentActiveDialogueText != null ? _currentActiveDialogueText.name : "NULL")}");
    }

    public void SetDialogueText(string text, bool useTypingEffect, RewardTarget speaker, FontStyles fontStyle = FontStyles.Normal, bool isMonologue = false)
    {
        if (enableDebugLogs) Debug.Log($"[DialoguePanel] SetDialogueText called: speaker={speaker}, isMonologue={isMonologue}, text length={text?.Length ?? 0}");

        SwitchSpeakerBox(speaker, isMonologue);
        if (_currentActiveDialogueText == null)
        {
            Debug.LogError("[DialoguePanel] _currentActiveDialogueText is NULL after SwitchSpeakerBox!");
            return;
        }

        if (uiTypingCoroutine != null) StopCoroutine(uiTypingCoroutine);
        _isShowingChoices = false;
        ClearSplitState();

        // Apply font style to primary box
        var tmpText = _currentActiveDialogueText.GetComponent<TMP_Text>();
        if (tmpText != null) tmpText.fontStyle = fontStyle;

        // Smart line cutting: split text into sub-lines for non-monologue, non-empty text
        bool canSplit = !isMonologue && HasSecondaryBox(speaker);
        List<string> parts = canSplit ? SplitDialogueTextMulti(text) : new List<string> { text };

        if (parts.Count > 1)
        {
            // Enter split mode: show first part, queue the rest

            _splitParts = parts;
            _currentSplitIndex = 0;
            _splitSpeaker = speaker;
            _splitFontStyle = fontStyle;
            _splitInProgress = true;
            _currentPartFullyTyped = false;
            ShowSplitPart(0, useTypingEffect);
        }
        else
        {
            // Single line: normal display on primary box
            if (useTypingEffect)
            {
                _currentActiveDialogueText.SetDirect("");
                uiTypingCoroutine = StartCoroutine(TypeTextEffectSingle(text, _currentActiveDialogueText));
            }
            else
            {
                _currentActiveDialogueText.SetDirect(text);
            }
        }
    }

    public void FastForwardText(string fullText, RewardTarget speaker, FontStyles fontStyle = FontStyles.Normal, bool isMonologue = false)
    {

        // If in split mode, fast-forward is handled by HandleSplitAdvance
        if (_splitInProgress)
        {

            FastForwardCurrentSplitPart();
            return;
        }

        if (_currentActiveDialogueText == null) return;

        if (uiTypingCoroutine != null)
        {
            StopCoroutine(uiTypingCoroutine);
            uiTypingCoroutine = null;
        }

        ForceShowAllText(_currentActiveDialogueText, fullText, fontStyle);
        DialogueManager.Instance?.NotifyLineFullyDisplayed();
    }

    #endregion

    #region Smart Line Cutting

    /// <summary>
    /// Returns true when sub-lines are being displayed sequentially.
    /// When true, DialogueManager should NOT call NotifyLineFullyDisplayed.
    /// </summary>
    public bool HasPendingSplitParts => _splitInProgress;

    /// <summary>
    /// Check if a secondary (alternate) box exists for the given speaker.
    /// </summary>
    private bool HasSecondaryBox(RewardTarget speaker)
    {
        if (speaker == RewardTarget.Player)
            return playerDialogueBox != null && playerDialogueText != null; // Right box is secondary for Player
        else
            return bossDialogueBoxLeft != null && bossDialogueTextLeft != null; // Left box is secondary for Boss
    }

    /// <summary>
    /// Get the box + text component for a specific split part index.
    /// Even indices use primary box, odd indices use secondary (alternate) box.
    /// </summary>
    private (GameObject box, LocalizedText text) GetBoxForSplitIndex(int index, RewardTarget speaker)
    {
        bool usePrimary = (index % 2 == 0);

        if (speaker == RewardTarget.Player)
        {
            // Player: primary=Left (new), secondary=Right (existing)
            return usePrimary
                ? (playerDialogueBoxLeft, playerDialogueTextLeft)
                : (playerDialogueBox, playerDialogueText);
        }
        else
        {
            // Boss: primary=Right (existing), secondary=Left (new)
            return usePrimary
                ? (bossDialogueBox, bossDialogueText)
                : (bossDialogueBoxLeft, bossDialogueTextLeft);
        }
    }

    /// <summary>
    /// Display a specific split part on the appropriate box.
    /// </summary>
    private void ShowSplitPart(int index, bool useTypingEffect)
    {

        if (index < 0 || index >= _splitParts.Count) return;

        var (box, localizedText) = GetBoxForSplitIndex(index, _splitSpeaker);
        if (box == null || localizedText == null)
        {
            Debug.LogWarning($"[DialoguePanel] No box/text for split part {index}, ending split.");
            _splitInProgress = false;
            return;
        }

        // Activate the box for this part (previous boxes stay visible)
        box.SetActive(true);
        _currentActiveDialogueText = localizedText;
        _currentPartFullyTyped = false;

        // Apply font style
        var tmpText = localizedText.GetComponent<TMP_Text>();
        if (tmpText != null) tmpText.fontStyle = _splitFontStyle;

        if (uiTypingCoroutine != null) StopCoroutine(uiTypingCoroutine);

        if (useTypingEffect)
        {
            localizedText.SetDirect("");
            uiTypingCoroutine = StartCoroutine(TypeSplitPart(_splitParts[index], localizedText));
        }
        else
        {
            localizedText.SetDirect(_splitParts[index]);
            _currentPartFullyTyped = true;
        }
    }

    /// <summary>
    /// Handle advance input while sub-lines are in progress.
    /// </summary>
    private void HandleSplitAdvance()
    {
        if (_splitAdvanceCooldown > 0f)
            return;

        if (!_currentPartFullyTyped)
        {
            // Still typing: check if current part ends with a line separator
            string currentText = _splitParts[_currentSplitIndex];
            bool endsWithSeparator = currentText.Length > 0 &&
                Array.IndexOf(LineSeparators, currentText[currentText.Length - 1]) >= 0;

            if (!endsWithSeparator)
            {
                // Non-separator split: skip ALL remaining parts at once
                FastForwardAllSplitParts();
            }
            else
            {
                // Separator split: fast-forward only the current sub-line
                FastForwardCurrentSplitPart();
                _splitAdvanceCooldown = SPLIT_ADVANCE_DELAY;
            }
        }
        else if (_currentSplitIndex < _splitParts.Count - 1)
        {
            // Current part done, more parts remain: advance to next
            _currentSplitIndex++;
            ShowSplitPart(_currentSplitIndex, true);
        }
        else
        {
            // All parts done: exit split mode and signal completion
            _splitInProgress = false;
            _splitParts.Clear();
            DialogueManager.Instance?.NotifyLineFullyDisplayed();
        }
    }

    /// <summary>
    /// Fast-forward the current sub-line (stop typing, show full text)
    /// </summary>
    private void FastForwardCurrentSplitPart()
    {
        if (uiTypingCoroutine != null)
        {
            StopCoroutine(uiTypingCoroutine);
            uiTypingCoroutine = null;
        }
        // Stop the inner typing coroutine to prevent orphaned character reveal
        if (_innerTypingCoroutine != null)
        {
            StopCoroutine(_innerTypingCoroutine);
            _innerTypingCoroutine = null;
        }

        if (_currentSplitIndex < _splitParts.Count && _currentActiveDialogueText != null)
        {
            ForceShowAllText(_currentActiveDialogueText, _splitParts[_currentSplitIndex], _splitFontStyle);
        }

        _currentPartFullyTyped = true;
    }

    /// <summary>
    /// Fast-forward ALL remaining split parts at once.
    /// Shows the current part and all subsequent parts instantly on their respective boxes,
    /// then exits split mode and signals completion.
    /// </summary>
    private void FastForwardAllSplitParts()
    {
        // Stop all typing coroutines
        if (uiTypingCoroutine != null)
        {
            StopCoroutine(uiTypingCoroutine);
            uiTypingCoroutine = null;
        }
        if (_innerTypingCoroutine != null)
        {
            StopCoroutine(_innerTypingCoroutine);
            _innerTypingCoroutine = null;
        }

        // Force-show ALL parts from current index onwards
        for (int i = _currentSplitIndex; i < _splitParts.Count; i++)
        {
            var (box, localizedText) = GetBoxForSplitIndex(i, _splitSpeaker);
            if (box != null && localizedText != null)
            {
                box.SetActive(true);
                var tmpText = localizedText.GetComponent<TMP_Text>();
                if (tmpText != null) tmpText.fontStyle = _splitFontStyle;
                ForceShowAllText(localizedText, _splitParts[i], _splitFontStyle);
            }
        }

        // Exit split mode and signal completion
        _currentPartFullyTyped = true;
        _splitInProgress = false;
        _splitParts.Clear();
        DialogueManager.Instance?.NotifyLineFullyDisplayed();
    }

    /// <summary>
    /// Typing coroutine for a single split part. Marks the part as done when finished.
    /// If the part does NOT end with a line separator (. , ;), auto-advances to the next
    /// split part immediately with typing, so the flow feels seamless.
    /// </summary>
    private IEnumerator TypeSplitPart(string text, LocalizedText targetLocalizedText)
    {
        _innerTypingCoroutine = StartCoroutine(TypeTextEffectSingle(text, targetLocalizedText));
        yield return _innerTypingCoroutine;
        _innerTypingCoroutine = null;
        _currentPartFullyTyped = true;
        uiTypingCoroutine = null;

        // Check if this part ends with a line separator — if not, auto-advance
        bool endsWithSeparator = text.Length > 0 &&
            Array.IndexOf(LineSeparators, text[text.Length - 1]) >= 0;

        if (!endsWithSeparator && _currentSplitIndex < _splitParts.Count - 1)
        {
            // Auto-advance to next split part without waiting for input
            _currentSplitIndex++;
            ShowSplitPart(_currentSplitIndex, true);
        }
        else
        {
            // Apply a minimum read cooldown so very short parts aren't instantly skipped past
            _splitAdvanceCooldown = SPLIT_ADVANCE_DELAY;
        }
    }

    /// <summary>
    /// Clear all split state.
    /// </summary>
    private void ClearSplitState()
    {
        _splitParts.Clear();
        _currentSplitIndex = 0;
        _splitInProgress = false;
        _currentPartFullyTyped = false;
        _innerTypingCoroutine = null;
    }

    /// <summary>
    /// Dynamically split text into parts that fit within the dialogue box using TMP_Text measurement.
    /// Prioritizes breaking at punctuation (. , ;) over spaces. Allows over-extending slightly
    /// past the box height to reach a punctuation mark, if the parent container has room.
    /// </summary>
    private const int MIN_SPLIT_PART_CHARS = 15; // Minimum chars to avoid tiny fragments

    private List<string> SplitDialogueTextMulti(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string> { text };

        // Get the TMP_Text from the current active dialogue box to measure against
        var tmpText = _currentActiveDialogueText?.GetComponent<TMP_Text>();
        if (tmpText == null)
            return new List<string> { text };

        // Calculate available area and over-extension budget from parent
        var rect = tmpText.rectTransform.rect;
        var margin = tmpText.margin;
        float availW = rect.width - margin.x - margin.z;
        float availH = rect.height - margin.y - margin.w;
        float overExtendBudget = GetOverExtendBudget(tmpText);

        Vector2 prefFull = tmpText.GetPreferredValues(text, availW, 0);
        bool overflows = prefFull.y > availH * 1.05f;

        // Check if the full text fits in the box without overflow
        if (!overflows)
            return new List<string> { text };

        var parts = new List<string>();
        string remaining = text;

        while (remaining.Length > 0)
        {
            remaining = remaining.TrimStart();
            if (remaining.Length == 0) break;

            // If what's left fits in the box, add it as the last part
            if (!DoesTextOverflow(tmpText, remaining))
            {
                parts.Add(remaining);
                break;
            }

            // Binary search for how many characters fit in the box
            int maxFit = FindMaxFittingChars(tmpText, remaining);

            // Find the best break point — punctuation-first, with over-extension
            int breakAt = FindSmartBreakPoint(tmpText, remaining, maxFit, overExtendBudget);

            // Ensure we don't create a tiny leftover fragment
            string leftover = remaining.Substring(breakAt).TrimStart();
            if (leftover.Length > 0 && leftover.Length < MIN_SPLIT_PART_CHARS)
            {
                // If the original break landed right after punctuation, keep it — it's a natural pause
                char charBeforeBreak = remaining[breakAt - 1];
                bool brokeAtPunctuation = Array.IndexOf(LineSeparators, charBeforeBreak) >= 0;
                if (!brokeAtPunctuation)
                {
                    // Rebalance: target balanced halves so both parts feel even
                    int idealTarget = remaining.Length / 2;
                    int earlierTarget = Math.Clamp(idealTarget, MIN_SPLIT_PART_CHARS, breakAt - 5);
                    int earlierBreak = FindSmartBreakPoint(tmpText, remaining, earlierTarget, 0);
                    if (earlierBreak >= MIN_SPLIT_PART_CHARS)
                    {
                        breakAt = earlierBreak;
                    }
                    else
                    {
                        // Can't split reasonably, take everything
                        parts.Add(remaining);
                        break;
                    }
                }
            }

            parts.Add(remaining.Substring(0, breakAt).TrimEnd());
            remaining = remaining.Substring(breakAt);
        }

        // Post-split: if 3+ parts and last part is small, try merging with previous part
        if (parts.Count >= 3)
        {
            string lastPart = parts[parts.Count - 1];
            string prevPart = parts[parts.Count - 2];
            if (lastPart.Length < MIN_SPLIT_PART_CHARS + 10)
            {
                string merged = prevPart + " " + lastPart;
                if (!DoesTextOverflow(tmpText, merged, overExtendBudget))
                {
                    parts[parts.Count - 2] = merged;
                    parts.RemoveAt(parts.Count - 1);
                }
            }
        }

        return parts.Count > 1 ? parts : new List<string> { text };
    }

    /// <summary>
    /// Calculate how much extra height the text can over-extend into, based on TMP_Text margins.
    /// The margins represent space within the box that text normally doesn't use,
    /// but we can allow slight over-extension into this space to reach a punctuation break.
    /// </summary>
    private float GetOverExtendBudget(TMP_Text tmpText)
    {
        var margin = tmpText.margin; // (left, top, right, bottom)
        float boxHeight = tmpText.rectTransform.rect.height;
        float marginVertical = margin.y + margin.w; // top + bottom margins
        float availH = boxHeight - marginVertical;

        // Allow over-extending into up to 50% of the bottom margin to reach punctuation
        // This is safe because TMP_Text will still render the text within the box rect
        float budget = margin.w * 0.5f; // Use half of the bottom margin
        return budget;
    }

    /// <summary>
    /// Check if the given text would overflow the TMP_Text's rect bounds,
    /// optionally with extra height allowance for over-extension.
    /// </summary>
    private bool DoesTextOverflow(TMP_Text tmpText, string text, float extraHeight = 0f)
    {
        var rect = tmpText.rectTransform.rect;
        var margin = tmpText.margin;
        float availableWidth = rect.width - margin.x - margin.z;
        float availableHeight = rect.height - margin.y - margin.w + extraHeight;
        Vector2 preferredSize = tmpText.GetPreferredValues(text, availableWidth, 0);
        return preferredSize.y > availableHeight * 1.05f;
    }

    /// <summary>
    /// Binary search for the maximum number of characters from the start of text
    /// that fit within the TMP_Text box without overflow.
    /// </summary>
    private int FindMaxFittingChars(TMP_Text tmpText, string text)
    {
        int lo = 1, hi = text.Length;

        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            string substring = text.Substring(0, mid);
            if (!DoesTextOverflow(tmpText, substring))
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo;
    }

    /// <summary>
    /// Find the best break point using a punctuation-first strategy with over-extension.
    /// Priority: (1) forward punctuation with over-extend, (2) backward punctuation,
    /// (3) backward space, (4) hard break at target.
    /// </summary>
    private int FindSmartBreakPoint(TMP_Text tmpText, string text, int maxFit, float overExtendBudget)
    {
        int searchFloor = Math.Max(maxFit / 2, MIN_SPLIT_PART_CHARS);
        int searchCeiling = Math.Min(text.Length - 1, maxFit + maxFit / 2); // Look up to 50% past maxFit

        // === Step 1: Search FORWARD from maxFit for nearby punctuation (over-extend) ===
        // Scan ALL punctuation in the forward range, closest first — don't give up on first miss
        if (overExtendBudget > 0)
        {
            int bestForwardBreak = -1;
            for (int i = maxFit; i <= searchCeiling; i++)
            {
                if (i < text.Length && Array.IndexOf(LineSeparators, text[i]) >= 0)
                {
                    string candidate = text.Substring(0, i + 1);
                    bool fits = !DoesTextOverflow(tmpText, candidate, overExtendBudget);
                    if (fits)
                    {
                        bestForwardBreak = i + 1; // Keep scanning for later punctuation that still fits
                    }
                    else
                    {
                        break; // Past budget, stop — anything further won't fit either
                    }
                }
            }
            if (bestForwardBreak > 0)
                return bestForwardBreak;
        }

        // === Step 2: Search BACKWARD from maxFit for punctuation ===
        for (int i = Math.Min(maxFit, text.Length - 1); i >= searchFloor; i--)
        {
            if (Array.IndexOf(LineSeparators, text[i]) >= 0)
            {
                return i + 1;
            }
        }

        // === Step 3: Search BACKWARD for space (last resort word break) ===
        for (int i = Math.Min(maxFit, text.Length - 1); i >= searchFloor; i--)
        {
            if (text[i] == ' ')
            {
                return i + 1;
            }
        }

        // === Step 4: Hard break at maxFit ===
        return Math.Min(maxFit, text.Length);
    }

    #endregion

    #region Text Display Helpers

    /// <summary>
    /// Force-show all text in a LocalizedText component (used for fast-forward)
    /// </summary>
    private void ForceShowAllText(LocalizedText localizedText, string text, FontStyles fontStyle)
    {
        if (localizedText == null) return;

        // OPT-35: Use cached TMP_Text instead of GetComponent per call
        var tmpText = GetCachedTMPText(localizedText);
        if (tmpText == null) return;

        tmpText.fontStyle = fontStyle;
        localizedText.SetDirect(text);
        tmpText.ForceMeshUpdate();
        TMP_TextInfo textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            vertexColors[vertexIndex + 0].a = 255;
            vertexColors[vertexIndex + 1].a = 255;
            vertexColors[vertexIndex + 2].a = 255;
            vertexColors[vertexIndex + 3].a = 255;
        }

        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    /// <summary>
    /// Types text on a single LocalizedText component with per-character alpha reveal.
    /// </summary>
    private IEnumerator TypeTextEffectSingle(string text, LocalizedText targetLocalizedText)
    {
        if (targetLocalizedText == null) yield break;

        // OPT-35: Use cached TMP_Text instead of GetComponent per call
        var targetTextComponent = GetCachedTMPText(targetLocalizedText);
        if (targetTextComponent == null) yield break;

        // Set the full text so AutoSize can calculate
        targetLocalizedText.SetDirect(text);
        targetTextComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = targetTextComponent.textInfo;

        // Make all characters transparent
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            vertexColors[vertexIndex + 0].a = 0;
            vertexColors[vertexIndex + 1].a = 0;
            vertexColors[vertexIndex + 2].a = 0;
            vertexColors[vertexIndex + 3].a = 0;
        }

        targetTextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        // Gradually reveal characters
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            vertexColors[vertexIndex + 0].a = 255;
            vertexColors[vertexIndex + 1].a = 255;
            vertexColors[vertexIndex + 2].a = 255;
            vertexColors[vertexIndex + 3].a = 255;

            targetTextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            // OPT-36: Static cached WaitForSeconds eliminates per-character heap allocation
            yield return _typingWait;
        }
    }
    
    // OPT-36: Static cached WaitForSeconds for typing effect (shared across all instances)
    private static readonly WaitForSeconds _typingWait = new WaitForSeconds(0.025f);

    /// <summary>
    /// OPT-35: Returns cached TMP_Text for a LocalizedText, avoiding GetComponent per dialogue line.
    /// </summary>
    private TMP_Text GetCachedTMPText(LocalizedText localizedText)
    {
        if (_tmpTextCache.TryGetValue(localizedText, out var cached))
            return cached;
        
        var tmp = localizedText.GetComponent<TMP_Text>();
        if (tmp != null)
            _tmpTextCache[localizedText] = tmp;
        return tmp;
    }

    #endregion

    #region Choice Display

    public void DisplayChoices(List<DialogueChoiceData> choiceDataList, Action<DialogueChoiceData> onChoiceSelectedCallback)
    {
        ClearChoices();
        if (dialogueUIChoicesContainer == null || dialogueUIChoiceButtonPrefab == null || choiceDataList.Count == 0)
        {
            Debug.LogError("[DialoguePanel] DisplayChoices failed: null references or empty choice list");
            _isShowingChoices = false;
            return;
        }

        _isShowingChoices = true;
        if (enableDebugLogs) Debug.Log("[DialoguePanel] Firing OnChoicesDisplayed event");
        OnChoicesDisplayed?.Invoke(); // Fire event for other panels

        foreach (DialogueChoiceData choiceData in choiceDataList)
        {
            GameObject choiceGO = Instantiate(dialogueUIChoiceButtonPrefab, dialogueUIChoicesContainer);
            choiceGO.SetActive(true);

            Button choiceButton = choiceGO.GetComponent<Button>();
            LocalizedText choiceTextUI = choiceGO.GetComponentInChildren<LocalizedText>();

            if (choiceTextUI == null)
            {
                Debug.LogError("[DialoguePanel] Instantiated choice button missing LocalizedText component! Prefab structure may have changed.");
                continue;
            }

            if (!string.IsNullOrEmpty(choiceData.localizationKey))
            {
                choiceTextUI.SetLocalized(choiceData.localizationKey, LocalizationDomains.Dialogues);
            }
            else
            {
                choiceTextUI.SetDirect(choiceData.choiceText);
            }

            if (choiceButton == null)
            {
                Debug.LogError("[DialoguePanel] Instantiated choice button missing Button component! Prefab structure may have changed.");
                continue;
            }

            choiceButton.onClick.AddListener(() =>
            {
                _isShowingChoices = false;
                onChoiceSelectedCallback?.Invoke(choiceData);
            });

            activeChoiceButtons.Add(choiceButton);
        }
    }

    // Overload for CG dialogue choices
    public void DisplayChoices(List<CGDialogueChoiceData> choiceDataList, Action<CGDialogueChoiceData> onChoiceSelectedCallback)
    {
        ClearChoices();
        if (dialogueUIChoicesContainer == null || dialogueUIChoiceButtonPrefab == null || choiceDataList.Count == 0)
        {
            Debug.LogError("[DialoguePanel] DisplayChoices (CG) failed: null references or empty choice list");
            _isShowingChoices = false;
            return;
        }

        _isShowingChoices = true;
        if (enableDebugLogs) Debug.Log("[DialoguePanel] Firing OnChoicesDisplayed event (CG)");
        OnChoicesDisplayed?.Invoke(); // Fire event for other panels

        foreach (CGDialogueChoiceData choiceData in choiceDataList)
        {
            GameObject choiceGO = Instantiate(dialogueUIChoiceButtonPrefab, dialogueUIChoicesContainer);
            choiceGO.SetActive(true);

            Button choiceButton = choiceGO.GetComponent<Button>();
            LocalizedText choiceTextUI = choiceGO.GetComponentInChildren<LocalizedText>();

            if (choiceTextUI == null)
            {
                Debug.LogError("[DialoguePanel] Instantiated choice button missing LocalizedText component! Prefab structure may have changed.");
                continue;
            }

            if (!string.IsNullOrEmpty(choiceData.localizationKey))
            {
                choiceTextUI.SetLocalized(choiceData.localizationKey, LocalizationDomains.Dialogues);
            }
            else
            {
                choiceTextUI.SetDirect(choiceData.choiceText);
            }

            if (choiceButton == null)
            {
                Debug.LogError("[DialoguePanel] Instantiated choice button missing Button component! Prefab structure may have changed.");
                continue;
            }

            choiceButton.onClick.AddListener(() =>
            {
                _isShowingChoices = false;
                onChoiceSelectedCallback?.Invoke(choiceData);
            });

            activeChoiceButtons.Add(choiceButton);
        }
    }

    public void ClearChoices()
    {
        foreach (Button button in activeChoiceButtons)
            if (button != null) Destroy(button.gameObject);

        activeChoiceButtons.Clear();
        _isShowingChoices = false;
        if (enableDebugLogs) Debug.Log("[DialoguePanel] Firing OnChoicesCleared event");
        OnChoicesCleared?.Invoke(); // Fire event for other panels
    }

    #endregion
}

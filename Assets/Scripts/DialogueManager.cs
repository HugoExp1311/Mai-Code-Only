using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Base.Dialogues;
using Base.CG;
using System;
using System.Linq;
using Base.Logger;
using Base.Character.Stats;
using Base.Character.Action;
using Base.Localization;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue UI Reference")]
    [SerializeField] private DialoguePanel dialoguePanel;

    private IDialogueUI _dialogueUI;
    private ScriptableObject _initialSequenceSOForRewards; // Can be DialogueSequenceSO or CGDialogueSequenceSO
    private ScriptableObject currentSequenceSO; // Can be DialogueSequenceSO or CGDialogueSequenceSO
    private object currentNodeData; // Can be DialogueNodeData or CGDialogueNodeData
    private int currentLineIndex;
    private int _currentNodeIndexInSequence = -1; // PERF-4: Cached node index for O(1) next-node lookup
    private HashSet<int> _processedRewardSequences = new HashSet<int>(); // BUG-13: Guard against duplicate rewards
    private Coroutine typingCoroutineTracker;
    private bool isVisuallyTyping = false;
    private bool canAdvance = false;
    private bool isLineFullyDisplayed = false;
    private Coroutine advanceLockCoroutine;
    private string lastDisplayedText = "";
    private RewardTarget lastSpeaker = RewardTarget.Mai;
    private bool lastIsMonologue = false;

    [Header("Timing Settings")]
    [SerializeField] private float delayBeforeSkipAllowed = 0.2f;
    [SerializeField] private float delayAfterLineTyped = 0.3f;
    [SerializeField] private float delayBeforeChoicesAppear = 0.5f;
    [SerializeField] private float delayBetweenNodes = 0.125f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    public bool IsDialogueActive => _dialogueUI != null && _dialogueUI.IsVisible;

    /// <summary>
    /// Returns the dialogue UI interface for external panel coordination (e.g., fade reveals).
    /// </summary>
    public IDialogueUI GetDialogueUI() => _dialogueUI;

    // Events for panels to subscribe to
    public event Action OnDialogueStart;
    public event Action OnDialogueEnd;
    public event Action<DialogueLine> OnDialogueLineDisplayed;
    public event Action OnChoicesDisplayed;
    public event Action OnChoicesCleared;

    // Helper methods to safely access node properties
    private int GetNodeLineCount()
    {
        if (currentNodeData is DialogueNodeData dialogueNode)
            return dialogueNode.lines.Count;
        if (currentNodeData is CGDialogueNodeData cgNode)
            return cgNode.lines.Count;
        return 0;
    }

    private DialogueLine GetNodeLine(int index)
    {
        if (currentNodeData is DialogueNodeData dialogueNode && index < dialogueNode.lines.Count)
            return dialogueNode.lines[index];
        if (currentNodeData is CGDialogueNodeData cgNode && index < cgNode.lines.Count)
            return cgNode.lines[index];
        return null;
    }

    private int GetNodeChoiceCount()
    {
        if (currentNodeData is DialogueNodeData dialogueNode)
            return dialogueNode.choices.Count;
        if (currentNodeData is CGDialogueNodeData cgNode)
            return cgNode.choices.Count;
        return 0;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Use serialized DialoguePanel field (assigned in Inspector)
        if (dialoguePanel != null)
        {
            _dialogueUI = dialoguePanel;
            _dialogueUI.OnDialogueAdvanceInput += HandleAdvanceInput;
        }
        else
        {
            Debug.LogError("[DialogueManager] Start: DialoguePanel not assigned in Inspector! Please assign DialoguePanel in the Inspector. Dialogue system will not work.");
        }

        LocalizationManager.Instance.LanguageChanged += HandleLanguageChanged;
    }

    void OnDisable()
    {
        if (_dialogueUI != null)
            _dialogueUI.OnDialogueAdvanceInput -= HandleAdvanceInput;

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.LanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged(Base.Settings.Language language, string localeCode, System.Globalization.CultureInfo culture)
    {
        if (IsDialogueActive && currentNodeData != null && currentLineIndex > 0)
        {
            // currentLineIndex is the index of the *next* line, so get the previous one
            int lineIndexToUpdate = currentLineIndex - 1;
            int lineCount = GetNodeLineCount();
            if (lineIndexToUpdate < lineCount)
            {
                DialogueLine currentLine = GetNodeLine(lineIndexToUpdate);
                if (currentLine != null)
                {
                    StartCoroutine(UpdateDialogueTextForLocaleChange(currentLine));
                }
            }
        }
    }

    private IEnumerator UpdateDialogueTextForLocaleChange(DialogueLine line)
    {
        var resolveTask = ResolveDialogueLineTextAsync(line);
        yield return new WaitUntil(() => resolveTask.IsCompleted);

        if (resolveTask.IsFaulted)
        {
            Debug.LogError($"[DialogueManager] Failed to update dialogue text for locale change: {resolveTask.Exception}");
            yield break;
        }

        string processedText = ProcessDialogueText(resolveTask.Result);
        if ((string.IsNullOrWhiteSpace(processedText) || IsMissingLocalizationResult(processedText, line.localizationKey)) &&
            GetNodeChoiceCount() > 0 &&
            !string.IsNullOrWhiteSpace(lastDisplayedText))
        {
            _dialogueUI.SetDialogueText(lastDisplayedText, false, lastSpeaker, line.fontStyle, lastIsMonologue);
            yield break;
        }

        _dialogueUI.SetDialogueText(processedText, false, line.speaker, line.fontStyle, line.isMonologue);
    }

    private void HandleAdvanceInput()
    {
        if (!IsDialogueActive)
        {
            Debug.LogWarning("[DialogueManager] HandleAdvanceInput: Dialogue is not active, ignoring input");
            return;
        }

        if (canAdvance)
        {
            AdvanceDialogue();
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"[DialogueManager] HandleAdvanceInput: canAdvance is false, ignoring input. Current state - isLineFullyDisplayed: {isLineFullyDisplayed}, isVisuallyTyping: {isVisuallyTyping}");
        }
    }

    public async void NotifyLineFullyDisplayed()
    {
        // Guard: if DialoguePanel is still processing split sub-lines, don't mark as complete
        if (_dialogueUI != null && _dialogueUI.HasPendingSplitParts)
        {
            return;
        }

        if (enableDebugLogs) Debug.Log($"[DialogueManager] NotifyLineFullyDisplayed called - currentLineIndex: {currentLineIndex}, isVisuallyTyping: {isVisuallyTyping}");
        isVisuallyTyping = false;
        isLineFullyDisplayed = true;

        // Check if this is the last line and if there are choices to display
        int lineCount = GetNodeLineCount();
        int choiceCount = GetNodeChoiceCount();
        if (enableDebugLogs) Debug.Log($"[DialogueManager] NotifyLineFullyDisplayed - lineCount: {lineCount}, choiceCount: {choiceCount}, currentLineIndex: {currentLineIndex}");

        if (currentNodeData != null && choiceCount > 0 &&
            (currentLineIndex == lineCount || lineCount == 0))
        {
            if (enableDebugLogs) Debug.Log($"[DialogueManager] Node has choices - waiting for delays (lineDelay: {delayAfterLineTyped}s, choiceDelay: {delayBeforeChoicesAppear}s)");
            // Wait for both the line delay and the choice appearance delay
            float totalDelay = delayAfterLineTyped + delayBeforeChoicesAppear;
            await System.Threading.Tasks.Task.Delay((int)(totalDelay * 1000));
            if (enableDebugLogs) Debug.Log($"[DialogueManager] Delay complete - displaying choices now");

            if (currentNodeData != null && GetNodeChoiceCount() > 0 && IsDialogueActive)
            {
                await ProcessNodeCompletionRewards(currentNodeData).ConfigureAwait(false);

                // Ensure dialogue text is not empty when showing choices
                bool currentNodeHasEmptyText = lineCount == 0 ||
                    (lineCount == 1 &&
                     !HasDialogueLineTextSource(GetNodeLine(0)));

                if (currentNodeHasEmptyText && !string.IsNullOrWhiteSpace(lastDisplayedText))
                {
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] Restoring last displayed text for empty choice node");
                    _dialogueUI.SetDialogueText(lastDisplayedText, false, lastSpeaker, isMonologue: lastIsMonologue);
                }

                // Display choices based on node type
                if (currentNodeData is DialogueNodeData dialogueNode)
                {
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] Displaying {dialogueNode.choices.Count} regular dialogue choices");
                    foreach (var choice in dialogueNode.choices)
                    {
                        ValidateChoiceData(choice, currentSequenceSO as DialogueSequenceSO);
                    }
                    _dialogueUI.DisplayChoices(dialogueNode.choices, (DialogueChoiceData choice) => OnChoiceSelected(choice));
                    OnChoicesDisplayed?.Invoke();
                }
                else if (currentNodeData is CGDialogueNodeData cgNode)
                {
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] Displaying {cgNode.choices.Count} CG dialogue choices");
                    _dialogueUI.DisplayChoices(cgNode.choices, (CGDialogueChoiceData choice) => OnCGChoiceSelected(choice));
                    OnChoicesDisplayed?.Invoke();
                }

                if (enableDebugLogs) Debug.Log($"[DialogueManager] Choices displayed - setting canAdvance to FALSE");
                canAdvance = false;
                return;
            }
        }
        else
        {
            if (enableDebugLogs) Debug.Log($"[DialogueManager] No choices to display - checking next node");

            // Check if the NEXT node will have choices - if so, don't lock advancement
            bool nextNodeHasChoices = WillAdvanceToNodeWithChoices();

            if (nextNodeHasChoices)
            {
                if (enableDebugLogs) Debug.Log($"[DialogueManager] Next node has choices - enabling immediate advancement (no lock)");
                // Enable immediate advancement so user can quickly get to the choice node
                canAdvance = true;
                return;
            }

            // Check if next node has choices and should auto-advance (optional auto-advance feature)
            if (ShouldAutoAdvanceToNextNodeWithChoices())
            {
                if (enableDebugLogs) Debug.Log($"[DialogueManager] Auto-advancing to next node with choices after {delayAfterLineTyped}s delay");
                // Auto-advance to next node with choices after delay
                await System.Threading.Tasks.Task.Delay((int)(delayAfterLineTyped * 1000));
                if (IsDialogueActive) // Ensure dialogue is still active
                {
                    AdvanceDialogue();
                }
            }
            else
            {
                // Enable manual advancement (player clicks/presses space to advance)
                LockAdvanceTemporarily(delayAfterLineTyped);
            }
        }
    }

    private void LockAdvanceTemporarily(float duration)
    {
        if (enableDebugLogs) Debug.Log($"[DialogueManager] LockAdvanceTemporarily called - duration: {duration}s, canAdvance currently: {canAdvance}");
        if (advanceLockCoroutine != null) StopCoroutine(advanceLockCoroutine);
        advanceLockCoroutine = StartCoroutine(AdvanceLockCoroutine(duration));
    }

    private IEnumerator AdvanceLockCoroutine(float duration)
    {
        if (enableDebugLogs) Debug.Log($"[DialogueManager] AdvanceLockCoroutine started - setting canAdvance to FALSE for {duration}s");
        canAdvance = false;
        yield return new WaitForSeconds(duration);
        canAdvance = true;
        if (enableDebugLogs) Debug.Log($"[DialogueManager] AdvanceLockCoroutine complete - canAdvance set to TRUE");
        advanceLockCoroutine = null;
    }

    // Overload for regular dialogue sequences
    public void StartDialogue(DialogueSequenceSO sequenceSO, string overrideStartNodeID = null)
    {
        StartDialogueInternal(sequenceSO, overrideStartNodeID);
    }

    // Overload for CG dialogue sequences
    public void StartDialogue(CGDialogueSequenceSO sequenceSO, string overrideStartNodeID = null)
    {
        StartDialogueInternal(sequenceSO, overrideStartNodeID);
    }

    /// <summary>
    /// Start a dialogue as a continuation of a previous dialogue (e.g., chained CG sequences).
    /// Skips OnDialogueStart event and ShowDialoguePanel to avoid fade-in animation
    /// and panel reset (e.g., CGPanel resetting background to black).
    /// </summary>
    public void StartDialogueContinuation(CGDialogueSequenceSO sequenceSO, string overrideStartNodeID = null)
    {
        StartDialogueInternal(sequenceSO, overrideStartNodeID, forceContinuation: true);
    }

    public void StartDialogueContinuation(DialogueSequenceSO sequenceSO, string overrideStartNodeID = null)
    {
        StartDialogueInternal(sequenceSO, overrideStartNodeID, forceContinuation: true);
    }

    private async void StartDialogueInternal(ScriptableObject sequenceSO, string overrideStartNodeID = null, bool forceContinuation = false)
    {
        if (enableDebugLogs) Debug.Log($"[DialogueManager] ========== StartDialogue called ==========");
        if (enableDebugLogs) Debug.Log($"[DialogueManager] Sequence: {sequenceSO?.name ?? "NULL"}, overrideStartNodeID: '{overrideStartNodeID}', forceContinuation: {forceContinuation}");

        if (_dialogueUI == null)
        {
            Debug.LogError("DialogueManager.StartDialogue: _dialogueUI is null. Cannot start dialogue.");
            return;
        }
        if (sequenceSO == null)
        {
            Debug.LogError("DialogueManager.StartDialogue: sequenceSO is null. Cannot start dialogue.");
            return;
        }

        // Detect if this is a continuation from an already-active dialogue (e.g., choice leading to new sequence)
        // or a forced continuation (e.g., chained CG sequences where EndDialogue was called between parts).
        // When continuing, skip OnDialogueStart and ShowDialoguePanel
        // to avoid the fade-in animation and panel reset (e.g., CGPanel resetting background to black).
        bool isContinuation = forceContinuation || (currentSequenceSO != null && IsDialogueActive);
        if (enableDebugLogs) Debug.Log($"[DialogueManager] isContinuation: {isContinuation} (forceContinuation: {forceContinuation}, currentSequenceSO: {currentSequenceSO?.name ?? "NULL"}, IsDialogueActive: {IsDialogueActive})");

        if (currentSequenceSO == null) _initialSequenceSOForRewards = sequenceSO;

        if (!isContinuation)
        {
            // Fire event for panels to handle (Live2DPanel, CGPanel, etc.)
            int subscriberCount = OnDialogueStart?.GetInvocationList().Length ?? 0;
            if (enableDebugLogs) Debug.Log($"[DialogueManager] OnDialogueStart has {subscriberCount} subscribers");
            if (subscriberCount == 0)
            {
                Debug.LogWarning("[DialogueManager] OnDialogueStart has NO subscribers! Panels may not be listening.");
            }

            OnDialogueStart?.Invoke();
            if (enableDebugLogs) Debug.Log("[DialogueManager] OnDialogueStart event fired");
        }
        else
        {
            if (enableDebugLogs) Debug.Log("[DialogueManager] Skipping OnDialogueStart (dialogue continuation from choice)");
        }

        currentSequenceSO = sequenceSO;
        currentNodeData = null;
        currentLineIndex = 0;
        canAdvance = false;
        isLineFullyDisplayed = false;
        lastDisplayedText = "";
        lastSpeaker = RewardTarget.Mai;
        lastIsMonologue = false;

        // Handle regular dialogue sequences
        if (sequenceSO is DialogueSequenceSO dialogueSeq)
        {
            if (dialogueSeq.type == SequenceType.RandomOutcome)
            {
                ResolveAndStartRandomOutcome(dialogueSeq);
                return;
            }

            string startNodeIdToUse = !string.IsNullOrEmpty(overrideStartNodeID) ? overrideStartNodeID : dialogueSeq.startingNodeID;
            DialogueNodeData nodeToStartWith = dialogueSeq.GetNodeByID(startNodeIdToUse);
            if (nodeToStartWith == null)
            {
                Debug.LogError($"DialogueManager.StartDialogue: Starting node '{startNodeIdToUse}' not found in sequence '{sequenceSO.name}'. Ending dialogue.");
                await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                EndDialogue();
                return;
            }

            RewardTarget initialSpeaker = (nodeToStartWith.lines.Count > 0) ? nodeToStartWith.lines[0].speaker : RewardTarget.Mai;
            if (!isContinuation)
            {
                _dialogueUI.ShowDialoguePanel(initialSpeaker);
            }
            else if (forceContinuation)
            {
                // For forced continuations (chained dialogues), EndDialogue() already hid the panel.
                // Re-show it without animation by directly activating and setting alpha to 1.
                _dialogueUI.ShowDialoguePanelImmediate(initialSpeaker);
            }
            // During choice-based continuation, panel is still visible — skip ShowDialoguePanel.
            // The speaker box will be updated when the first line is displayed via SetDialogueText.
            StartNode(nodeToStartWith);
        }
        // Handle CG dialogue sequences
        else if (sequenceSO is CGDialogueSequenceSO cgSeq)
        {
            string startNodeIdToUse = !string.IsNullOrEmpty(overrideStartNodeID) ? overrideStartNodeID : cgSeq.startingNodeID;
            CGDialogueNodeData nodeToStartWith = cgSeq.GetNodeByID(startNodeIdToUse);
            if (nodeToStartWith == null)
            {
                Debug.LogError($"DialogueManager.StartDialogue: Starting node '{startNodeIdToUse}' not found in CG sequence '{sequenceSO.name}'. Ending dialogue.");
                await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                EndDialogue();
                return;
            }

            RewardTarget initialSpeaker = (nodeToStartWith.lines.Count > 0) ? nodeToStartWith.lines[0].speaker : RewardTarget.Mai;
            if (!isContinuation)
            {
                _dialogueUI.ShowDialoguePanel(initialSpeaker);
            }
            else if (forceContinuation)
            {
                // For forced continuations (chained dialogues), EndDialogue() already hid the panel.
                // Re-show it without animation by directly activating and setting alpha to 1.
                _dialogueUI.ShowDialoguePanelImmediate(initialSpeaker);
            }
            // During choice-based continuation, panel is still visible — skip ShowDialoguePanel.
            // The speaker box will be updated when the first line is displayed via SetDialogueText.
            StartNode(nodeToStartWith);
        }
        else
        {
            Debug.LogError($"DialogueManager.StartDialogue: Unknown sequence type '{sequenceSO.GetType().Name}'. Ending dialogue.");
            EndDialogue();
        }
    }

    private async void ResolveAndStartRandomOutcome(DialogueSequenceSO randomContainerSO)
    {
        DialogueSequenceSO chosenOutcomeSO = null;
        List<DialogueSequenceSO> possibleOutcomes = new List<DialogueSequenceSO>();

        if (randomContainerSO.goodOutcomeSequence != null) possibleOutcomes.Add(randomContainerSO.goodOutcomeSequence);
        if (randomContainerSO.neutralOutcomeSequence != null) possibleOutcomes.Add(randomContainerSO.neutralOutcomeSequence);
        if (randomContainerSO.badOutcomeSequence != null) possibleOutcomes.Add(randomContainerSO.badOutcomeSequence);


        if (possibleOutcomes.Count == 0)
        {
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            EndDialogue();
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, possibleOutcomes.Count);
        chosenOutcomeSO = possibleOutcomes[randomIndex];


        if (chosenOutcomeSO == null)
        {
            Debug.LogWarning("[DialogueManager] Chosen outcome is null, ending dialogue");
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            EndDialogue();
            return;
        }

        currentSequenceSO = chosenOutcomeSO;
        DialogueNodeData startingNodeOfOutcome = chosenOutcomeSO.GetNodeByID(chosenOutcomeSO.startingNodeID);


        if (startingNodeOfOutcome == null)
        {
            Debug.LogWarning($"[DialogueManager] Starting node '{chosenOutcomeSO.startingNodeID}' not found in chosen outcome '{chosenOutcomeSO.name}', ending dialogue");
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            EndDialogue();
            return;
        }


        RewardTarget initialSpeaker = (startingNodeOfOutcome.lines.Count > 0) ? startingNodeOfOutcome.lines[0].speaker : RewardTarget.Mai;
        _dialogueUI.ShowDialoguePanel(initialSpeaker);
        StartNode(startingNodeOfOutcome);
    }

    // Overload for regular dialogue nodes
    private async void StartNode(DialogueNodeData nodeData)
    {
        try
        {
        if (nodeData == null)
        {
            Debug.LogWarning("[DialogueManager] StartNode: Called with null nodeData, ending dialogue");
            await ProcessSequenceCompletionRewards(currentSequenceSO);
            EndDialogue();
            return;
        }

        currentNodeData = nodeData;
        currentLineIndex = 0;
        isLineFullyDisplayed = false;
        canAdvance = false;
        _dialogueUI.ClearChoices();
        OnChoicesCleared?.Invoke();

        // PERF-4: Cache node index for O(1) next-node lookup
        _currentNodeIndexInSequence = -1;
        if (currentSequenceSO is DialogueSequenceSO dialogueSeq && dialogueSeq.nodeList != null)
        {
            for (int i = 0; i < dialogueSeq.nodeList.Count; i++)
            {
                if (dialogueSeq.nodeList[i].nodeID == nodeData.nodeID)
                {
                    _currentNodeIndexInSequence = i;
                    break;
                }
            }
        }

        // Special case: if node has no lines and no choices, this is an end node
        if (nodeData.lines.Count == 0 && nodeData.choices.Count == 0)
        {
            await ProcessNodeCompletionRewards(nodeData);
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            EndDialogue();
            return;
        }

        // Special case: if node has choices but no lines, show choices immediately
        if (nodeData.lines.Count == 0 && nodeData.choices.Count > 0)
        {
            await ProcessNodeCompletionRewards(nodeData);

            // Preserve last displayed text for choice display
            if (!string.IsNullOrWhiteSpace(lastDisplayedText))
            {
                _dialogueUI.SetDialogueText(lastDisplayedText, false, lastSpeaker, isMonologue: lastIsMonologue);
            }

            foreach (var choice in nodeData.choices)
            {
                ValidateChoiceData(choice, currentSequenceSO as DialogueSequenceSO);
            }

            _dialogueUI.DisplayChoices(nodeData.choices, OnChoiceSelected);
            OnChoicesDisplayed?.Invoke();
            canAdvance = false;
            return;
        }

        await DisplayNextLine();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueManager] StartNode(Dialogue) failed: {ex}");
        }
    }

    // Overload for CG dialogue nodes
    private async void StartNode(CGDialogueNodeData nodeData)
    {
        try
        {
        if (nodeData == null)
        {
            Debug.LogWarning("[DialogueManager] StartNode: Called with null CGDialogueNodeData, ending dialogue");
            await ProcessSequenceCompletionRewards(currentSequenceSO);
            EndDialogue();
            return;
        }

        currentNodeData = nodeData;
        currentLineIndex = 0;
        isLineFullyDisplayed = false;
        canAdvance = false;
        _dialogueUI.ClearChoices();

        // PERF-4: Cache node index for O(1) next-node lookup
        _currentNodeIndexInSequence = -1;
        if (currentSequenceSO is CGDialogueSequenceSO cgSeq && cgSeq.nodeList != null)
        {
            for (int i = 0; i < cgSeq.nodeList.Count; i++)
            {
                if (cgSeq.nodeList[i].nodeID == nodeData.nodeID)
                {
                    _currentNodeIndexInSequence = i;
                    break;
                }
            }
        }

        // Special case: if node has no lines and no choices, this is an end node
        if (nodeData.lines.Count == 0 && nodeData.choices.Count == 0)
        {
            await ProcessNodeCompletionRewards(nodeData);
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);

            // Check for node-level sequence transition
            if (nodeData.nextSequenceSO != null)
            {
                Debug.Log($"[DialogueManager] Node '{nodeData.nodeID}' transitioning to sequence '{nodeData.nextSequenceSO.name}'");
                StartDialogue(nodeData.nextSequenceSO);
                return;
            }

            EndDialogue();
            return;
        }

        // Special case: if node has choices but no lines, show choices immediately
        if (nodeData.lines.Count == 0 && nodeData.choices.Count > 0)
        {
            await ProcessNodeCompletionRewards(nodeData);

            // Preserve last displayed text for choice display
            if (!string.IsNullOrWhiteSpace(lastDisplayedText))
            {
                _dialogueUI.SetDialogueText(lastDisplayedText, false, lastSpeaker, isMonologue: lastIsMonologue);
            }

            _dialogueUI.DisplayChoices(nodeData.choices, (CGDialogueChoiceData choice) => OnCGChoiceSelected(choice));
            OnChoicesDisplayed?.Invoke();
            canAdvance = false;
            return;
        }

        await DisplayNextLine();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueManager] StartNode(CG) failed: {ex}");
        }
    }

    public async void AdvanceDialogue()
    {
        try
        {
        if (!IsDialogueActive || currentNodeData == null)
        {
            return;
        }

        if (isVisuallyTyping)
        {
            if (typingCoroutineTracker != null) StopCoroutine(typingCoroutineTracker);
            typingCoroutineTracker = null;
            isVisuallyTyping = false;

            int lineCount = GetNodeLineCount();
            if (currentLineIndex > 0 && currentLineIndex <= lineCount)
            {
                DialogueLine lineBeingTyped = GetNodeLine(currentLineIndex - 1);
                if (lineBeingTyped != null)
                {
                    string fullProcessedText = await lineBeingTyped.GetTextAsync();
                    fullProcessedText = ProcessDialogueText(fullProcessedText);
                    _dialogueUI.FastForwardText(fullProcessedText, lineBeingTyped.speaker, lineBeingTyped.fontStyle, lineBeingTyped.isMonologue);
                }
            }
            return;
        }

        if (isLineFullyDisplayed)
        {
            int lineCount = GetNodeLineCount();
            int choiceCount = GetNodeChoiceCount();

            if (currentLineIndex >= lineCount)
            {
                // Don't manually process rewards or choices here - NotifyLineFullyDisplayed handles this automatically
                // Only handle the case where there are no choices (continue to next node or end dialogue)
                if (choiceCount == 0)
                {
                    // Check if there's a next node in the sequence to auto-advance to
                    if (currentNodeData is DialogueNodeData dialogueNode)
                    {
                        DialogueNodeData nextNode = GetNextNodeInSequence(dialogueNode);
                        if (nextNode != null)
                        {
                            await ProcessNodeCompletionRewards(currentNodeData);
                            _dialogueUI.ClearAllDialogueBoxes();
                            await System.Threading.Tasks.Task.Delay((int)(delayBetweenNodes * 1000));
                            if (!IsDialogueActive) return;
                            StartNode(nextNode);
                        }
                        else
                        {
                            await ProcessNodeCompletionRewards(currentNodeData);
                            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                            EndDialogue();
                        }
                    }
                    else if (currentNodeData is CGDialogueNodeData cgNode)
                    {
                        if (enableDebugLogs) Debug.Log($"[DialogueManager] AdvanceDialogue: Processing CG node '{cgNode.nodeID}' - all lines displayed, no choices");

                        // CG dialogue: Check for next node in sequence first
                        CGDialogueNodeData nextNode = GetNextCGNodeInSequence(cgNode);
                        if (nextNode != null)
                        {
                            if (enableDebugLogs) Debug.Log($"[DialogueManager] AdvanceDialogue: Found next node '{nextNode.nodeID}', advancing");
                            await ProcessNodeCompletionRewards(currentNodeData);
                            _dialogueUI.ClearAllDialogueBoxes();
                            await System.Threading.Tasks.Task.Delay((int)(delayBetweenNodes * 1000));
                            if (!IsDialogueActive) return;
                            StartNode(nextNode);
                        }
                        else
                        {
                            if (enableDebugLogs) Debug.Log($"[DialogueManager] AdvanceDialogue: No next node found, ending dialogue");
                            await ProcessNodeCompletionRewards(currentNodeData);
                            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);

                            // Check for node-level sequence transition
                            if (cgNode.nextSequenceSO != null)
                            {
                                if (enableDebugLogs) Debug.Log($"[DialogueManager] Node '{cgNode.nodeID}' transitioning to sequence '{cgNode.nextSequenceSO.name}'");
                                StartDialogue(cgNode.nextSequenceSO);
                                return;
                            }

                            if (enableDebugLogs) Debug.Log($"[DialogueManager] AdvanceDialogue: Calling EndDialogue()");
                            EndDialogue();
                        }
                    }
                }
                // If there are choices, NotifyLineFullyDisplayed will handle showing them automatically
                return;
            }
            else
            {
                await DisplayNextLine();
            }
        }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueManager] AdvanceDialogue failed: {ex}");
        }
    }

    private async System.Threading.Tasks.Task DisplayNextLine()
    {
        int lineCount = GetNodeLineCount();
        if (enableDebugLogs) Debug.Log($"[DialogueManager] DisplayNextLine called. currentLineIndex={currentLineIndex}, nodeData lines count={lineCount}");

        if (currentNodeData == null || currentLineIndex >= lineCount)
        {
            if (enableDebugLogs) Debug.Log("[DialogueManager] No more lines to display, advancing dialogue");
            if (currentNodeData != null) await ProcessNodeCompletionRewards(currentNodeData);
            AdvanceDialogue();
            return;
        }

        DialogueLine line = GetNodeLine(currentLineIndex);
        if (line == null)
        {
            Debug.LogError($"[DialogueManager] Failed to get line at index {currentLineIndex}");
            AdvanceDialogue();
            return;
        }
        if (enableDebugLogs) Debug.Log($"[DialogueManager] Processing line {currentLineIndex}: speaker={line.speaker}, animation={line.animationToPlay}, loop={line.loopAnimation}, floatValue={line.animationFloatValue}");

        string localizedText = await ResolveDialogueLineTextAsync(line);

        string processedText = ProcessDialogueText(localizedText);
        if (enableDebugLogs) Debug.Log($"[DialogueManager] Processed text: '{(processedText?.Length > 50 ? processedText.Substring(0, 50) + "..." : processedText)}'");

        isVisuallyTyping = true;
        isLineFullyDisplayed = false;
        canAdvance = false;

        // Fire event for panels to handle visual updates (Live2D animations, CG sprites, etc.)
        OnDialogueLineDisplayed?.Invoke(line);

        // Check if this is an empty line in a node with choices - preserve last text instead
        int choiceCount = GetNodeChoiceCount();
        bool isEmptyLineWithChoices = string.IsNullOrWhiteSpace(processedText) &&
                                     choiceCount > 0 &&
                                     !string.IsNullOrWhiteSpace(lastDisplayedText);
        bool isMissingKeyLineWithChoices = IsMissingLocalizationResult(processedText, line.localizationKey) &&
                                           choiceCount > 0 &&
                                           !string.IsNullOrWhiteSpace(lastDisplayedText);

        if (isEmptyLineWithChoices || isMissingKeyLineWithChoices)
        {
            // Don't update display text, keep the previous text visible
        }
        else
        {
            _dialogueUI.SetDialogueText(processedText, true, line.speaker, line.fontStyle, line.isMonologue);

            // Track the last displayed text and speaker for choice display
            if (!string.IsNullOrWhiteSpace(processedText))
            {
                lastDisplayedText = processedText;
                lastSpeaker = line.speaker;
                lastIsMonologue = line.isMonologue;
            }
        }

        if (typingCoroutineTracker != null) StopCoroutine(typingCoroutineTracker);

        typingCoroutineTracker = StartCoroutine(TrackTypingDuration(processedText));
        LockAdvanceTemporarily(delayBeforeSkipAllowed);
        currentLineIndex++;
    }

    private IEnumerator TrackTypingDuration(string text)
    {
        float duration = Mathf.Max(0.05f, text.Length * 0.025f);
        yield return new WaitForSeconds(duration);
        // Special handling for nodes with choices - always call NotifyLineFullyDisplayed
        bool hasChoices = currentNodeData != null && GetNodeChoiceCount() > 0;
        if (isVisuallyTyping || hasChoices)
        {
            // For very short text with choices, ensure we give enough time for choice display
            if (hasChoices && text.Length <= 5)
            {
                yield return new WaitForSeconds(0.1f); // Additional small delay for short text with choices
            }
            NotifyLineFullyDisplayed();
        }
        typingCoroutineTracker = null;
    }

    private System.Threading.Tasks.Task<string> ResolveDialogueLineTextAsync(DialogueLine line)
    {
        if (line == null)
            return System.Threading.Tasks.Task.FromResult(string.Empty);

        if (!string.IsNullOrEmpty(line.localizationKey))
        {
            string localizedText = LocalizationManager.Instance.GetLocalizedString(line.localizationKey, LocalizationDomains.Dialogues);
            return System.Threading.Tasks.Task.FromResult(localizedText);
        }

        return System.Threading.Tasks.Task.FromResult(string.Empty);
    }

    private bool IsMissingLocalizationResult(string text, string key)
    {
        return string.IsNullOrEmpty(text) || text == "???" || (!string.IsNullOrEmpty(key) && text == $"[{key}]");
    }

    private bool HasDialogueLineTextSource(DialogueLine line)
    {
        if (line == null)
            return false;

        if (!string.IsNullOrEmpty(line.localizationKey))
            return LocalizationManager.Instance.KeyExists(line.localizationKey, LocalizationDomains.Dialogues);

        return false;
    }

    private string ProcessDialogueText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return "";
        string processedText = rawText;
        if (GameManager.Instance != null && GameManager.Instance.Player != null && !string.IsNullOrEmpty(GameManager.Instance.Player.GetName()))
        {
            processedText = processedText.Replace("{playerName}", GameManager.Instance.Player.GetName());
        }
        else
        {
            processedText = processedText.Replace("{playerName}", "Player");
        }
        return processedText;
    }

    private async void OnChoiceSelected(DialogueChoiceData choiceMade)
    {
        try
        {
        if (currentNodeData == null)
        {
            Debug.LogWarning("[DialogueManager] OnChoiceSelected: currentNodeData is null, cannot process choice");
            return;
        }

        string currentSequenceName = currentSequenceSO?.name ?? "Unknown";

        // Validation: Warn if nextSequenceSO is null but nextNodeID is not empty AND the node doesn't exist in current sequence
        if (choiceMade.nextSequenceSO == null && !string.IsNullOrEmpty(choiceMade.nextNodeID))
        {
            // Check if the node exists in the current sequence
            if (currentSequenceSO is DialogueSequenceSO dialogueSeq)
            {
                bool nodeExistsInCurrentSequence = dialogueSeq.nodeList != null &&
                    dialogueSeq.nodeList.Any(node => node.nodeID == choiceMade.nextNodeID);

                if (!nodeExistsInCurrentSequence)
                {
                    Debug.LogWarning($"[DialogueManager] OnChoiceSelected: WARNING - Choice has nextNodeID '{choiceMade.nextNodeID}' but nextSequenceSO is null " +
                        $"and the node doesn't exist in current sequence '{currentSequenceName}'. " +
                        $"If '{choiceMade.nextNodeID}' is a sequence name, the ScriptableObject may need to be re-imported from CSV.");
                }
            }
        }

        _dialogueUI.ClearChoices();
        canAdvance = false;

        if (choiceMade.nextSequenceSO != null)
        {
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            StartDialogue(choiceMade.nextSequenceSO, choiceMade.startingNodeInNextSequenceID);
        }
        else if (!string.IsNullOrEmpty(choiceMade.nextNodeID))
        {
            if (currentSequenceSO is DialogueSequenceSO dialogueSeq)
            {
                DialogueNodeData nextNode = dialogueSeq.GetNodeByID(choiceMade.nextNodeID);

                if (nextNode != null)
                {
                    StartNode(nextNode);
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] OnChoiceSelected: Node '{choiceMade.nextNodeID}' not found in sequence '{currentSequenceName}', ending dialogue");
                    await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                    EndDialogue();
                }
            }
            else
            {
                Debug.LogError($"[DialogueManager] OnChoiceSelected: currentSequenceSO is not DialogueSequenceSO, cannot get node");
                await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                EndDialogue();
            }
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] OnChoiceSelected: Choice has no nextNodeID or nextSequenceSO configured. Ending dialogue.");
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            EndDialogue();
        }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueManager] OnChoiceSelected failed: {ex}");
        }
    }

    private async void OnCGChoiceSelected(CGDialogueChoiceData choiceMade)
    {
        try
        {
        if (currentNodeData == null)
        {
            Debug.LogWarning("[DialogueManager] OnCGChoiceSelected: currentNodeData is null, cannot process choice");
            return;
        }

        string currentSequenceName = currentSequenceSO?.name ?? "Unknown";

        _dialogueUI.ClearChoices();
        canAdvance = false;

        // Check for next CG sequence
        if (choiceMade.nextSequenceSO != null)
        {
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            Debug.Log($"[DialogueManager] OnCGChoiceSelected: Transitioning to CG sequence '{choiceMade.nextSequenceSO.name}'");
            StartDialogue(choiceMade.nextSequenceSO, choiceMade.startingNodeInNextSequenceID);
        }
        // Check for next node in current sequence
        else if (!string.IsNullOrEmpty(choiceMade.nextNodeID))
        {
            if (currentSequenceSO is CGDialogueSequenceSO cgSeq)
            {
                CGDialogueNodeData nextNode = cgSeq.GetNodeByID(choiceMade.nextNodeID);

                if (nextNode != null)
                {
                    StartNode(nextNode);
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] OnCGChoiceSelected: Node '{choiceMade.nextNodeID}' not found in CG sequence '{currentSequenceName}', ending dialogue");
                    await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                    EndDialogue();
                }
            }
            else
            {
                Debug.LogError($"[DialogueManager] OnCGChoiceSelected: currentSequenceSO is not CGDialogueSequenceSO, cannot get node");
                await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
                EndDialogue();
            }
        }
        else
        {
            Debug.LogWarning($"[DialogueManager] OnCGChoiceSelected: Choice has no nextNodeID or nextSequenceSO configured. Ending dialogue.");
            await ProcessSequenceCompletionRewards(_initialSequenceSOForRewards);
            EndDialogue();
        }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DialogueManager] OnCGChoiceSelected failed: {ex}");
        }
    }

    // Overload for regular dialogue nodes
    private async System.Threading.Tasks.Task ProcessNodeCompletionRewards(DialogueNodeData completedNodeData)
    {
        if (completedNodeData == null || completedNodeData.rewardsOnNodeCompletion.Count == 0) return;
        if (GameManager.Instance == null) return;

        foreach (DialogueReward reward in completedNodeData.rewardsOnNodeCompletion)
        {
            var rewardAction = new CharacterActions.ChangeStat(reward.target, reward.stat, reward.amount);
            if (reward.target == RewardTarget.Player && GameManager.Instance.Player != null)
                await GameManager.Instance.Player.DoAction(rewardAction);
            else if (reward.target == RewardTarget.Mai && GameManager.Instance.DataManager?.GetCurrentBoss() != null)
                await GameManager.Instance.DataManager.GetCurrentBoss().DoAction(rewardAction);
        }
    }

    // Overload for CG dialogue nodes
    private async System.Threading.Tasks.Task ProcessNodeCompletionRewards(CGDialogueNodeData completedNodeData)
    {
        if (completedNodeData == null || completedNodeData.rewardsOnNodeCompletion.Count == 0) return;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[DialogueManager] ProcessNodeCompletionRewards: GameManager.Instance is null");
            return;
        }

        foreach (DialogueReward reward in completedNodeData.rewardsOnNodeCompletion)
        {
            Debug.Log($"[Reward] {reward.target}.{reward.stat} {reward.amount:+#;-#;0}");
            var rewardAction = new CharacterActions.ChangeStat(reward.target, reward.stat, reward.amount);
            if (reward.target == RewardTarget.Player && GameManager.Instance.Player != null)
                await GameManager.Instance.Player.DoAction(rewardAction);
            else if (reward.target == RewardTarget.Mai && GameManager.Instance.DataManager?.GetCurrentBoss() != null)
                await GameManager.Instance.DataManager.GetCurrentBoss().DoAction(rewardAction);
            else
                Debug.LogWarning($"[DialogueManager] Could not apply reward - target character not found: {reward.target}");
        }
    }

    // Generic overload for object type (used when currentNodeData is object)
    private async System.Threading.Tasks.Task ProcessNodeCompletionRewards(object completedNodeData)
    {
        if (completedNodeData is DialogueNodeData dialogueNode)
        {
            await ProcessNodeCompletionRewards(dialogueNode);
        }
        else if (completedNodeData is CGDialogueNodeData cgNode)
        {
            await ProcessNodeCompletionRewards(cgNode);
        }
    }

    // Overload for regular dialogue sequences
    private async System.Threading.Tasks.Task ProcessSequenceCompletionRewards(DialogueSequenceSO sequenceSOToEnd)
    {
        if (sequenceSOToEnd == null || sequenceSOToEnd.rewardsOnCompletion.Count == 0) return;
        // BUG-13: Guard against duplicate rewards for the same sequence
        int instanceId = sequenceSOToEnd.GetInstanceID();
        if (!_processedRewardSequences.Add(instanceId)) return;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[DialogueManager] ProcessSequenceCompletionRewards: GameManager.Instance is null");
            return;
        }

        foreach (DialogueReward reward in sequenceSOToEnd.rewardsOnCompletion)
        {
            Debug.Log($"[Reward] {reward.target}.{reward.stat} {reward.amount:+#;-#;0}");
            var rewardAction = new CharacterActions.ChangeStat(reward.target, reward.stat, reward.amount);
            if (reward.target == RewardTarget.Player && GameManager.Instance.Player != null)
                await GameManager.Instance.Player.DoAction(rewardAction);
            else if (reward.target == RewardTarget.Mai && GameManager.Instance.DataManager?.GetCurrentBoss() != null)
                await GameManager.Instance.DataManager.GetCurrentBoss().DoAction(rewardAction);
            else
                Debug.LogWarning($"[DialogueManager] Could not apply reward - target character not found: {reward.target}");
        }
    }

    // Overload for CG dialogue sequences
    private async System.Threading.Tasks.Task ProcessSequenceCompletionRewards(CGDialogueSequenceSO sequenceSOToEnd)
    {
        if (sequenceSOToEnd == null || sequenceSOToEnd.rewardsOnCompletion.Count == 0) return;
        // BUG-13: Guard against duplicate rewards for the same sequence
        int instanceId = sequenceSOToEnd.GetInstanceID();
        if (!_processedRewardSequences.Add(instanceId)) return;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[DialogueManager] ProcessSequenceCompletionRewards: GameManager.Instance is null");
            return;
        }

        foreach (DialogueReward reward in sequenceSOToEnd.rewardsOnCompletion)
        {
            Debug.Log($"[Reward] {reward.target}.{reward.stat} {reward.amount:+#;-#;0}");
            var rewardAction = new CharacterActions.ChangeStat(reward.target, reward.stat, reward.amount);
            if (reward.target == RewardTarget.Player && GameManager.Instance.Player != null)
                await GameManager.Instance.Player.DoAction(rewardAction);
            else if (reward.target == RewardTarget.Mai && GameManager.Instance.DataManager?.GetCurrentBoss() != null)
                await GameManager.Instance.DataManager.GetCurrentBoss().DoAction(rewardAction);
            else
                Debug.LogWarning($"[DialogueManager] Could not apply reward - target character not found: {reward.target}");
        }
    }

    // Generic overload for ScriptableObject type (used when currentSequenceSO is ScriptableObject)
    private async System.Threading.Tasks.Task ProcessSequenceCompletionRewards(ScriptableObject sequenceSOToEnd)
    {
        if (sequenceSOToEnd is DialogueSequenceSO dialogueSeq)
        {
            await ProcessSequenceCompletionRewards(dialogueSeq);
        }
        else if (sequenceSOToEnd is CGDialogueSequenceSO cgSeq)
        {
            await ProcessSequenceCompletionRewards(cgSeq);
        }
    }

    /// <summary>
    /// Diagnostic method to validate choice data for debugging
    /// </summary>
    public void ValidateChoiceData(DialogueChoiceData choice, DialogueSequenceSO currentSequence)
    {
        if (choice == null)
        {
            Debug.LogError("[DialogueManager] Choice data is null!");
            return;
        }

        if (choice.nextSequenceSO != null)
        {
            if (!string.IsNullOrEmpty(choice.startingNodeInNextSequenceID))
            {
                var targetNode = choice.nextSequenceSO.GetNodeByID(choice.startingNodeInNextSequenceID);
                if (targetNode == null)
                    Debug.LogWarning($"[DialogueManager] Starting node '{choice.startingNodeInNextSequenceID}' not found in target sequence!");
            }
        }
        else if (!string.IsNullOrEmpty(choice.nextNodeID))
        {
            if (currentSequence != null)
            {
                var targetNode = currentSequence.GetNodeByID(choice.nextNodeID);
                if (targetNode == null)
                    Debug.LogWarning($"[DialogueManager] Target node '{choice.nextNodeID}' not found in current sequence '{currentSequence.name}'!");
            }
        }
    }

    public void EndDialogue()
    {
        if (isVisuallyTyping && typingCoroutineTracker != null) StopCoroutine(typingCoroutineTracker);
        isVisuallyTyping = false; typingCoroutineTracker = null;

        // Clean up UI first
        _dialogueUI?.HideDialoguePanel();
        _dialogueUI?.ClearChoices();
        OnChoicesCleared?.Invoke();

        // Reset all state BEFORE firing OnDialogueEnd, because event handlers
        // may start a new dialogue inside the callback (e.g., GameManager's intro
        // sequence chaining). If we fire the event first and clean up after,
        // the cleanup would nuke the newly-started dialogue's state.
        _initialSequenceSOForRewards = null;
        currentSequenceSO = null;
        currentNodeData = null;
        currentLineIndex = 0;
        _currentNodeIndexInSequence = -1;
        _processedRewardSequences.Clear();

        // Fire event for panels and external listeners to handle cleanup.
        // This MUST be last so that any new dialogue started inside a handler
        // is not destroyed by the state reset above.
        OnDialogueEnd?.Invoke();
    }

    /// <summary>
    /// Gets the next node in the sequence array for automatic chaining
    /// </summary>
    private DialogueNodeData GetNextNodeInSequence(DialogueNodeData currentNode)
    {
        if (currentSequenceSO == null || currentNode == null)
            return null;

        // Only works for regular dialogue sequences
        if (!(currentSequenceSO is DialogueSequenceSO dialogueSeq))
            return null;

        if (dialogueSeq.nodeList == null)
            return null;

        // PERF-4: Use cached index for O(1) lookup instead of O(N) linear scan
        if (_currentNodeIndexInSequence >= 0 && _currentNodeIndexInSequence < dialogueSeq.nodeList.Count)
        {
            int nextIndex = _currentNodeIndexInSequence + 1;
            if (nextIndex < dialogueSeq.nodeList.Count)
                return dialogueSeq.nodeList[nextIndex];
            return null;
        }

        // Fallback: linear scan if cache is invalid
        for (int i = 0; i < dialogueSeq.nodeList.Count; i++)
        {
            if (dialogueSeq.nodeList[i].nodeID == currentNode.nodeID)
            {
                int nextIndex = i + 1;
                if (nextIndex < dialogueSeq.nodeList.Count)
                    return dialogueSeq.nodeList[nextIndex];
                break;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the next CG node in the sequence array for automatic chaining
    /// </summary>
    private CGDialogueNodeData GetNextCGNodeInSequence(CGDialogueNodeData currentNode)
    {
        if (enableDebugLogs) Debug.Log($"[DialogueManager] GetNextCGNodeInSequence called for node: {currentNode?.nodeID ?? "NULL"}");

        if (currentSequenceSO == null || currentNode == null)
        {
            Debug.LogWarning($"[DialogueManager] GetNextCGNodeInSequence: currentSequenceSO or currentNode is null");
            return null;
        }

        // Only works for CG dialogue sequences
        if (!(currentSequenceSO is CGDialogueSequenceSO cgSeq))
        {
            Debug.LogWarning($"[DialogueManager] GetNextCGNodeInSequence: currentSequenceSO is not CGDialogueSequenceSO");
            return null;
        }

        if (cgSeq.nodeList == null)
        {
            Debug.LogWarning($"[DialogueManager] GetNextCGNodeInSequence: nodeList is null");
            return null;
        }

        // PERF-4: Use cached index for O(1) lookup instead of O(N) linear scan
        int nodeIndex = _currentNodeIndexInSequence;
        if (nodeIndex < 0 || nodeIndex >= cgSeq.nodeList.Count)
        {
            // Fallback: linear scan if cache is invalid
            for (int i = 0; i < cgSeq.nodeList.Count; i++)
            {
                if (cgSeq.nodeList[i].nodeID == currentNode.nodeID)
                {
                    nodeIndex = i;
                    break;
                }
            }
        }

        if (nodeIndex >= 0 && nodeIndex < cgSeq.nodeList.Count)
        {
                if (enableDebugLogs) Debug.Log($"[DialogueManager] Found current node at index {nodeIndex}");

                // If the node has an explicit nextNodeID, use it (respects CSV NextNodeID column)
                if (!string.IsNullOrEmpty(currentNode.nextNodeID))
                {
                    CGDialogueNodeData explicitNext = cgSeq.GetNodeByID(currentNode.nextNodeID);
                    if (explicitNext != null)
                    {
                        if (enableDebugLogs) Debug.Log($"[DialogueManager] Next node found via explicit nextNodeID: {explicitNext.nodeID}");
                        return explicitNext;
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueManager] Explicit nextNodeID '{currentNode.nextNodeID}' not found in nodeList for node '{currentNode.nodeID}'!");
                    }
                }

                // If the node transitions to a new sequence, there is no "next node" in THIS sequence
                if (currentNode.nextSequenceSO != null)
                {
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] Node '{currentNode.nodeID}' has nextSequenceSO set. Ending current sequence.");
                    return null;
                }

                // If this node is marked as an end-node (branch terminal), stop here
                if (currentNode.isEndNode)
                {
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] Node '{currentNode.nodeID}' is marked as end-node. Ending sequence.");
                    return null;
                }

                int nextIndex = nodeIndex + 1;
                if (nextIndex < cgSeq.nodeList.Count)
                {
                    CGDialogueNodeData nextNode = cgSeq.nodeList[nextIndex];
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] Next node found: {nextNode.nodeID} at index {nextIndex}");
                    return nextNode;
                }
                else
                {
                    if (enableDebugLogs) Debug.Log($"[DialogueManager] No next node - reached end of sequence (index {nodeIndex} is last)");
                }
                return null;
        }

        Debug.LogWarning($"[DialogueManager] Current node '{currentNode.nodeID}' not found in nodeList!");
        return null;
    }

    /// <summary>
    /// Checks if advancing dialogue will lead to a node with choices
    /// </summary>
    private bool WillAdvanceToNodeWithChoices()
    {
        if (currentNodeData == null || currentSequenceSO == null)
            return false;

        // Check for CG dialogue
        if (currentNodeData is CGDialogueNodeData cgNode)
        {
            CGDialogueNodeData nextNode = GetNextCGNodeInSequence(cgNode);
            if (nextNode != null && nextNode.choices.Count > 0)
            {
                if (enableDebugLogs) Debug.Log($"[DialogueManager] WillAdvanceToNodeWithChoices: Next CG node '{nextNode.nodeID}' has {nextNode.choices.Count} choices");
                return true;
            }
        }
        // Check for regular dialogue
        else if (currentNodeData is DialogueNodeData dialogueNode)
        {
            DialogueNodeData nextNode = GetNextNodeInSequence(dialogueNode);
            if (nextNode != null && nextNode.choices.Count > 0)
            {
                if (enableDebugLogs) Debug.Log($"[DialogueManager] WillAdvanceToNodeWithChoices: Next dialogue node '{nextNode.nodeID}' has {nextNode.choices.Count} choices");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if we should auto-advance to the next node when it has choices
    /// </summary>
    private bool ShouldAutoAdvanceToNextNodeWithChoices()
    {
        if (currentNodeData == null || currentSequenceSO == null)
            return false;

        // Only works for regular dialogue
        if (!(currentNodeData is DialogueNodeData dialogueNode))
            return false;

        // Only check if current node has no choices (so we're at end of current node)
        if (dialogueNode.choices.Count > 0)
            return false;

        // Get the next node in sequence
        DialogueNodeData nextNode = GetNextNodeInSequence(dialogueNode);
        if (nextNode == null)
            return false;

        // Check if next node has choices
        if (nextNode.choices.Count == 0)
            return false;

        // Check if next node has no lines or only empty lines
        bool nextNodeHasNoMeaningfulText = nextNode.lines.Count == 0 ||
            nextNode.lines.All(line => !HasDialogueLineTextSource(line));

        if (nextNodeHasNoMeaningfulText)
        {
            return true;
        }

        return false;
    }
}

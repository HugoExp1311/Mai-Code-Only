using Base.Character.Stats;
using Base.CG;
using System;
using System.Collections.Generic;
using TMPro;

namespace Base.Dialogues
{
    public interface IDialogueUI
    {
        bool IsVisible { get; }
        void ShowDialoguePanel(RewardTarget initialSpeaker);
        /// <summary>
        /// Show the dialogue panel immediately without fade animation.
        /// Used for chained dialogues where EndDialogue hid the panel between sequences.
        /// </summary>
        void ShowDialoguePanelImmediate(RewardTarget initialSpeaker);
        void HideDialoguePanel();
        void SetDialogueText(string text, bool isTypingEffect, RewardTarget speaker, FontStyles fontStyle = FontStyles.Normal, bool isMonologue = false);
        void DisplayChoices(List<DialogueChoiceData> choices, Action<DialogueChoiceData> onChoiceSelectedCallback);
        void DisplayChoices(List<CGDialogueChoiceData> choices, Action<CGDialogueChoiceData> onChoiceSelectedCallback);
        void ClearChoices();
        void FastForwardText(string fullText, RewardTarget speaker, FontStyles fontStyle = FontStyles.Normal, bool isMonologue = false);

        /// <summary>
        /// Hides all dialogue boxes and clears their text. Used to clean the screen
        /// between nodes before the between-node delay starts.
        /// </summary>
        void ClearAllDialogueBoxes();

        /// <summary>
        /// Returns true when the UI is displaying split sub-lines and hasn't finished the last one yet.
        /// DialogueManager should not call NotifyLineFullyDisplayed while this is true.
        /// </summary>
        bool HasPendingSplitParts { get; }

        event Action OnDialogueAdvanceInput;
    }
}
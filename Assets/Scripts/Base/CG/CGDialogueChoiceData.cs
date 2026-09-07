using UnityEngine;
using System;
using Base.Localization;

namespace Base.CG
{
    /// <summary>
    /// A choice in a CG dialogue sequence
    /// Similar to DialogueChoiceData but references CGDialogueSequenceSO
    /// </summary>
    [Serializable]
    public class CGDialogueChoiceData
    {
        [Header("Localized Choice Text")]
        public string localizationKey;
        
        [Header("Legacy Support (Deprecated)")]
        public string choiceText;
        
        [Header("Navigation")]
        public string nextNodeID;
        public CGDialogueSequenceSO nextSequenceSO;
        public string startingNodeInNextSequenceID;

#if UNITY_EDITOR
        [NonSerialized] public string _editor_nextNodeID_temp;
        [NonSerialized] public string _editor_nextSequenceID_temp;
        [NonSerialized] public string _editor_startingNodeInNextSequenceID_temp;
#endif

        /// <summary>
        /// Gets the localized choice text if available, otherwise falls back to legacy choiceText field
        /// </summary>
        public string GetChoiceText()
        {
            if (!string.IsNullOrEmpty(localizationKey))
                return LocalizationManager.Instance.GetLocalizedString(localizationKey, LocalizationDomains.Dialogues);

            return choiceText;
        }
        
        /// <summary>
        /// Gets the localized choice text asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<string> GetChoiceTextAsync()
        {
            return System.Threading.Tasks.Task.FromResult(GetChoiceText());
        }
    }
}

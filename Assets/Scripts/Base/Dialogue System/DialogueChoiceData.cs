using Base.Dialogues;
using Base.Localization;
using UnityEngine;
using System;

namespace Base.Dialogues
{
    [Serializable]
    public class DialogueChoiceData
    {
        [Header("Localized Choice Text")]
        public string localizationKey;
        
        [Header("Legacy Support (Deprecated)")]
        public string choiceText;
        
        [Header("Navigation")]
        public string nextNodeID;
        public DialogueSequenceSO nextSequenceSO;
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

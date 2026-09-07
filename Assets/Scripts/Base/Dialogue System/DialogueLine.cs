using UnityEngine;
using System;
using Base.Character.Stats;
using Base.Localization;
using TMPro;

namespace Base.Dialogues
{
    [Serializable]
    public class DialogueLine
    {
        public RewardTarget speaker = RewardTarget.Mai;

        [Header("Localized Text")]
        public string localizationKey;

        [Header("Text Style")]
        public FontStyles fontStyle = FontStyles.Normal;

        [Header("Monologue")]
        public bool isMonologue = false;

        [Header("Animation Settings")]
        public AnimationType animationToPlay = AnimationType.MaiTalk;
        public bool loopAnimation = true;
        public float animationFloatValue = 0.5f;

        /// <summary>
        /// Gets the localized text
        /// </summary>
        public string GetText()
        {
            if (!string.IsNullOrEmpty(localizationKey))
                return LocalizationManager.Instance.GetLocalizedString(localizationKey, LocalizationDomains.Dialogues);

            return string.Empty;
        }

        /// <summary>
        /// Gets the localized text asynchronously
        /// </summary>
        public System.Threading.Tasks.Task<string> GetTextAsync()
        {
            return System.Threading.Tasks.Task.FromResult(GetText());
        }
    }
}

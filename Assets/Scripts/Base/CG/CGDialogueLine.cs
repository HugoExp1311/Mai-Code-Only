using UnityEngine;
using System;
using Base.Character.Stats;
using Base.Dialogues;

namespace Base.CG
{
    /// <summary>
    /// A dialogue line for CG scenes with an associated background image
    /// Extends DialogueLine to add CG sprite support while maintaining compatibility
    /// with DialogueManager's existing dialogue system
    /// </summary>
    [Serializable]
    public class CGDialogueLine : DialogueLine
    {
        [Header("CG Background Image")]
        [Tooltip("The CG sprite to display as background for this line")]
        public Sprite cgSprite;
        
        // Inherited from DialogueLine:
        // - speaker (RewardTarget)
        // - localizationKey
        // - animationToPlay (AnimationType) - not used for CG
        // - loopAnimation (bool) - not used for CG
        // - animationFloatValue (float) - not used for CG
        // - GetText() method
        // - GetTextAsync() method
    }
}

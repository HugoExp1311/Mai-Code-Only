using System;
using UnityEngine;

namespace Base.UI.Popup
{
    /// <summary>
    /// Complete state of the Action Confirm Popup
    /// Defines header, icon, and configuration for all 3 buttons
    /// </summary>
    [Serializable]
    public class PopupState
    {
        /// <summary>
        /// Unique identifier for this state (e.g., "SleepInitial", "NapState", "DeepSleepState")
        /// </summary>
        public string stateId;
        
        /// <summary>
        /// Header text displayed at the top of the popup
        /// </summary>
        public string headerText;
        
        /// <summary>
        /// Optional icon sprite displayed in the popup
        /// </summary>
        public Sprite iconSprite;
        
        /// <summary>
        /// Configuration for the 3 buttons in the popup
        /// Array must have exactly 3 elements
        /// </summary>
        public PopupButtonState[] buttons;
        
        /// <summary>
        /// Validate that the state has exactly 3 buttons
        /// </summary>
        public bool IsValid()
        {
            return buttons != null && buttons.Length == 3;
        }
    }
}


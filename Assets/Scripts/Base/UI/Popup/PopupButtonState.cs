using System;

namespace Base.UI.Popup
{
    /// <summary>
    /// Configuration for a single button within the popup
    /// Defines button text, appearance, and action to execute
    /// </summary>
    [Serializable]
    public class PopupButtonState
    {
        /// <summary>
        /// Text displayed on the button
        /// </summary>
        public string buttonText;
        
        /// <summary>
        /// Additional text showing stat changes (e.g., "+20 Energy", "-2 Knowledge")
        /// Can be displayed below button text or combined with it
        /// </summary>
        public string boosterText;
        
        /// <summary>
        /// Whether the button can be clicked
        /// False = button is visible but grayed out/non-clickable
        /// </summary>
        public bool isInteractable;
        
        /// <summary>
        /// Whether the button GameObject is active/visible
        /// False = button is completely hidden
        /// </summary>
        public bool isEnabled;
        
        /// <summary>
        /// Action to execute when button is clicked
        /// </summary>
        public PopupButtonAction action;
        
        /// <summary>
        /// Create a disabled/hidden button state
        /// </summary>
        public static PopupButtonState Disabled => new PopupButtonState
        {
            buttonText = "",
            boosterText = "",
            isInteractable = false,
            isEnabled = false,
            action = null
        };
    }
}


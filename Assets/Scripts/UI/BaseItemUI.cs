using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Base.Localization;

namespace MaisLoveStory.UI
{
    /// <summary>
    /// Base class for item UI elements (Shop, Inventory, etc.)
    /// Handles common display logic for icons, names, and descriptions
    /// </summary>
    public abstract class BaseItemUI : MonoBehaviour
    {
        [Header("Common UI References")]
        [SerializeField] protected Image iconImage;
        [SerializeField] protected LocalizedText nameText;
        
        [Header("Optional")]
        [SerializeField] protected LocalizedText descriptionText;
        
        /// <summary>
        /// Set the item icon sprite
        /// </summary>
        protected void SetIcon(Sprite icon)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }
        }
        
        /// <summary>
        /// Set the localized name using a localization key
        /// </summary>
        protected void SetLocalizedName(string localizationKey)
        {
            if (nameText != null)
            {
                nameText.SetLocalized(localizationKey);
            }
        }
        
        /// <summary>
        /// Set the localized description using a localization key
        /// </summary>
        protected void SetLocalizedDescription(string localizationKey)
        {
            if (descriptionText != null)
            {
                descriptionText.SetLocalized(localizationKey);
            }
        }
    }
}

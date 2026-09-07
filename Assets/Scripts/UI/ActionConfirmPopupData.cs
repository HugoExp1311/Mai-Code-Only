using UnityEngine;

/// <summary>
/// Data structure for configuring Action Confirm Popup content
/// Pass this to the popup when showing it to set icon, header text, and stat change information
/// </summary>
[System.Serializable]
public class ActionConfirmPopupData
{
    [Header("Popup Content")]
    [Tooltip("Icon sprite to display in the popup")]
    public Sprite iconSprite;
    
    [Tooltip("Main header/title text for the popup")]
    public string headerText;
    
    [Tooltip("Text showing stat changes (e.g., '+10 Stamina', '-5 Money', 'Stamina: +10, Money: -5')")]
    public string boosterText;
    
    [Header("Source Information")]
    [Tooltip("Identifier for the button that triggered this popup (e.g., button name, action ID)")]
    public string sourceButtonId;
    
    [Tooltip("Reference to the UIPanelButtonAction component that triggered this popup")]
    public UIPanelButtonAction sourceButtonAction;
    
    /// <summary>
    /// Create empty/default popup data
    /// </summary>
    public static ActionConfirmPopupData Empty => new ActionConfirmPopupData
    {
        iconSprite = null,
        headerText = "",
        boosterText = "",
        sourceButtonId = "",
        sourceButtonAction = null
    };
    
    /// <summary>
    /// Create popup data with specified values
    /// </summary>
    public static ActionConfirmPopupData Create(Sprite icon, string header, string booster)
    {
        return new ActionConfirmPopupData
        {
            iconSprite = icon,
            headerText = header,
            boosterText = booster,
            sourceButtonId = "",
            sourceButtonAction = null
        };
    }
    
    /// <summary>
    /// Create popup data with specified values and source button information
    /// </summary>
    public static ActionConfirmPopupData Create(Sprite icon, string header, string booster, string buttonId, UIPanelButtonAction sourceAction)
    {
        return new ActionConfirmPopupData
        {
            iconSprite = icon,
            headerText = header,
            boosterText = booster,
            sourceButtonId = buttonId,
            sourceButtonAction = sourceAction
        };
    }
}


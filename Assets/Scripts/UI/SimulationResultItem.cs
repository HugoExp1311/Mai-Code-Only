using Base.Localization;
using UnityEngine;

/// <summary>
/// Data class for individual result item
/// Contains localization key and value to display
/// </summary>
/// <summary>
/// Component for individual result item in simulation result panel
/// Displays a label and value (e.g., "Pussy Orgasms: +2")
/// </summary>
public class SimulationResultItem : MonoBehaviour
{
    [SerializeField] private LocalizedText labelLocalizedText;
    [SerializeField] private LocalizedText valueText;
    
    /// <summary>
    /// Setup the result item with localized label and value
    /// </summary>
    public void Setup(string labelKey, int value)
    {
        // Set localized label
        if (labelLocalizedText != null)
        {
            labelLocalizedText.SetLocalized(labelKey, LocalizationDomains.UI);
        }
        
        // Set value with + prefix for positive numbers
        if (valueText != null)
        {
            valueText.SetLocalized("UI Value Signed", LocalizationDomains.UI, new System.Collections.Generic.Dictionary<string, object>
            {
                ["amount"] = value >= 0 ? $"+{value}" : value.ToString()
            });
        }
    }
}

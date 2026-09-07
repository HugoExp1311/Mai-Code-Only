using UnityEngine;
using Base;
using System.Linq;

/// <summary>
/// ScriptableObject that defines a shop item's visual representation
/// Links to DefaultSettings.ShopItems for data
/// Create via: Assets > Create > Shop > Item Visual
/// </summary>
[CreateAssetMenu(fileName = "Item_NewItem", menuName = "Shop/Item Visual")]
public class ShopItemData : ScriptableObject
{
    [Header("Item Reference")]
    [Tooltip("Item ID that matches DefaultSettings.ShopItems key")]
    public string itemId;
    
    [Header("Visual")]
    [Tooltip("Item icon sprite")]
    public Sprite icon;
    
    [Header("Optional")]
    [Tooltip("Is this item currently available for purchase?")]
    public bool isAvailable = true;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-generate unique ID if empty
        if (string.IsNullOrEmpty(itemId))
        {
            // Try to get first available item from DefaultSettings
            if (DefaultSettings.ShopItems.Count > 0)
            {
                itemId = DefaultSettings.ShopItems.Keys.First();
            }
        }
    }
#endif
    
    /// <summary>
    /// Get item info from DefaultSettings
    /// </summary>
    public DefaultSettings.ShopItemInfo GetItemInfo()
    {
        if (DefaultSettings.ShopItems.TryGetValue(itemId, out var info))
        {
            return info;
        }
        
        Debug.LogError($"ShopItemData: Item ID '{itemId}' not found in DefaultSettings.ShopItems!");
        return default;
    }
    
    /// <summary>
    /// Get localized name using "Item {ItemName} Name" key
    /// </summary>
    public string GetLocalizedName()
    {
        return Base.Localization.KeyHelper.ItemName(itemId); // Return key for LocalizedText to resolve
    }
    
    /// <summary>
    /// Get localized description using "Item {ItemName} Desc" key
    /// </summary>
    public string GetLocalizedDescription()
    {
        return Base.Localization.KeyHelper.ItemDesc(itemId); // Return key for LocalizedText to resolve
    }
}

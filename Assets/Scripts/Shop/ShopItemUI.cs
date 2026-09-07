using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Base;
using Base.Localization;
using Base.Character;
using Base.Character.Action;
using Base.Character.Stats;
using Base.Inventory.Item;
using MaisLoveStory.UI;
using System.Collections.Generic;

/// <summary>
/// UI component for a single shop item
/// Displays item info and handles purchase interaction
/// Uses LocalizedText for name/description
/// Integrates with Player's BuyItem system
/// </summary>
public class ShopItemUI : BaseItemUI
{
    [Header("Shop-Specific UI")]
    [SerializeField] private LocalizedText priceText;
    [SerializeField] private Button purchaseButton;
    
    private ShopItemData itemData;
    private DefaultSettings.ShopItemInfo itemInfo;
    
    /// <summary>
    /// Initialize this UI element with item data
    /// </summary>
    public void Setup(ShopItemData data)
    {
        itemData = data;
        
        if (itemData == null)
        {
            Debug.LogError("ShopItemUI: Item data is null!");
            return;
        }
        
        // Get item info from DefaultSettings
        itemInfo = itemData.GetItemInfo();
        
        // Set common UI elements using base class methods
        SetIcon(itemData.icon);
        SetLocalizedName(itemData.GetLocalizedName());
        SetLocalizedDescription(itemData.GetLocalizedDescription());
        
        // Set price with variable - LocalizedText handles the variable update
        if (priceText != null)
        {
            var priceVariables = new System.Collections.Generic.Dictionary<string, object>
            {
                { "amount", itemInfo.Price }
            };
            priceText.SetLocalized("UI ItemPrice", null, priceVariables);
        }
        else
        {
            Debug.LogWarning($"[ShopItemUI] priceText is null for item {itemData.itemId}!");
        }
        
        // Setup button - always enabled unless item is unavailable
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
            purchaseButton.interactable = itemData.isAvailable;
        }
    }
    
    private async void OnPurchaseClicked()
    {
        try
        {
            if (itemData == null) return;
            
            // Check availability
            if (!itemData.isAvailable)
            {
                NoticeUI.ShowLocalized("Notice Item Unavailable");
                
                // Play notice sound
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                {
                    AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
                }
                return;
            }
            
            // Check if player can afford
            int playerMoney = GameManager.Instance.Player.GetMoney();
            if (playerMoney < itemInfo.Price)
            {
                NoticeUI.ShowLocalized("Notice Not Enough Money");
                
                // Play notice sound
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                {
                    AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
                }
                return;
            }
            
            // Create item instance with amount = 1
            IItem item = new CommonItem(itemInfo.Price, itemInfo.ItemId, itemInfo.Type, itemInfo.EffectValue, amount: 1);
            
            // Purchase through Player's BuyItem action
            var action = new CharacterActions.BuyItem(item);
            var result = await GameManager.Instance.Player.DoAction(action);
            
            // Handle result
            if (result is UIAction.UpdateNewValue<BasicResource> updateAction)
            {
                Debug.Log($"Purchased {itemInfo.ItemId} for Money: {itemInfo.Price}. New money: {updateAction.NewValue}");
                
                // Play purchase success sound
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                {
                    AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ShopBuy);
                }
            }
            else
            {
                Debug.LogWarning($"Purchase failed for {itemInfo.ItemId}");
                NoticeUI.ShowLocalized("Notice Purchase Failed");
                
                // Play purchase failed sound
                if (AudioManager.Instance != null && FMODEvents.Instance != null)
                {
                    AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopItemUI] Purchase error for {itemData?.itemId}: {ex}");
        }
    }
    
    public ShopItemData GetItemData()
    {
        return itemData;
    }
    
    public DefaultSettings.ShopItemInfo GetItemInfo()
    {
        return itemInfo;
    }
}

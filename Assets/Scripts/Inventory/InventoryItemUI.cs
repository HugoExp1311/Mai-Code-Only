using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Base;
using Base.Localization;
using Base.Inventory.Item;
using Base.Character.Action;
using MaisLoveStory.UI;

/// <summary>
/// UI component for a single inventory item
/// Displays item info and amount owned
/// </summary>
public class InventoryItemUI : BaseItemUI
{
    [Header("Inventory-Specific UI")]
    [SerializeField] private LocalizedText amountText;
    [SerializeField] private Button actionButton;
    
    [Header("Action Button Visuals")]
    [SerializeField] private Image actionButtonImage;
    [SerializeField] private LocalizedText actionButtonText;
    [SerializeField] private Sprite giveButtonSprite;
    [SerializeField] private Sprite useButtonSprite;
    
    private IItem item;
    
    /// <summary>
    /// Initialize this UI element with item data
    /// </summary>
    public void Setup(IItem itemData, Sprite itemIcon = null)
    {
        item = itemData;
        
        if (item == null)
        {
            Debug.LogError("InventoryItemUI: Item data is null!");
            return;
        }
        
        // Get item info from DefaultSettings
        if (!DefaultSettings.ShopItems.TryGetValue(item.Identifier(), out var itemInfo))
        {
            Debug.LogError($"InventoryItemUI: Item info not found for {item.Identifier()}!");
            return;
        }
        
        // Set common UI elements using base class methods
        SetIcon(itemIcon);
        SetLocalizedName(Base.Localization.KeyHelper.ItemName(item.Identifier()));
        SetLocalizedDescription(Base.Localization.KeyHelper.ItemDesc(item.Identifier()));
        
        // Set amount
        UpdateAmount();
        
        // Setup action button based on item type
        SetupActionButton(itemInfo);
    }
    
    /// <summary>
    /// Setup action button based on item type (Give for gifts, Use for goods)
    /// </summary>
    private void SetupActionButton(DefaultSettings.ShopItemInfo itemInfo)
    {
        if (actionButton == null) return;
        
        actionButton.onClick.RemoveAllListeners();
        actionButton.interactable = item.Amount() > 0;
        
        // Determine button type based on item category
        bool isGift = itemInfo.Category == DefaultSettings.ItemCategory.Gift;
        
        // Set button image
        if (actionButtonImage != null)
        {
            actionButtonImage.sprite = isGift ? giveButtonSprite : useButtonSprite;
        }
        
        // Set button text
        if (actionButtonText != null)
        {
            string localizationKey = isGift ? "Inventory Give" : "Inventory Use";
            actionButtonText.SetLocalized(localizationKey);
        }
        
        // Set button action
        if (isGift)
        {
            actionButton.onClick.AddListener(OnGiveClicked);
        }
        else
        {
            actionButton.onClick.AddListener(OnUseClicked);
        }
    }
    
    /// <summary>
    /// Update the amount display
    /// </summary>
    public void UpdateAmount()
    {
        if (item != null && amountText != null)
        {
            amountText.SetLocalized("Inventory Item Amount", LocalizationDomains.UI, new Dictionary<string, object>
            {
                ["amount"] = item.Amount()
            });
            
            // Update button interactability
            if (actionButton != null)
            {
                actionButton.interactable = item.Amount() > 0;
            }
        }
    }
    
    private async void OnGiveClicked()
    {
        if (item == null || item.Amount() <= 0) return;

        if (GameManager.Instance == null || GameManager.Instance.DataManager == null) return;

        await GameManager.Instance.DataManager.OnUseItem(item, null);
        UpdateAmount();

        // Play gift sound
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
        }
    }
    
    private async void OnUseClicked()
    {
        if (item == null || item.Amount() <= 0) return;

        if (item.Identifier() == "condom")
        {
            SexSimulationManager manager = SexSimulationManager.Instance;
            if (manager == null || !manager.CanActivateCowgirlCondom())
            {
                Debug.LogWarning(
                    "[InventoryItemUI] A condom can only be activated once while Cowgirl is selected.");
                return;
            }

            if (GameManager.Instance?.Player == null)
            {
                return;
            }

            UIAction result = await GameManager.Instance.Player.DoAction(
                new CharacterActions.UseItem(item));
            if (result is UIAction.FalseWithNothing ||
                !manager.TryActivateCowgirlCondom())
            {
                return;
            }

            UpdateAmount();
            PlayUseSound();
            return;
        }
        
        if (GameManager.Instance == null || GameManager.Instance.DataManager == null) return;

        await GameManager.Instance.DataManager.OnUseItem(item, null);
        UpdateAmount();

        PlayUseSound();
    }

    private static void PlayUseSound()
    {
        if (AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.OnNotice);
        }
    }
    
    public IItem GetItem()
    {
        return item;
    }
}

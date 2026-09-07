using UnityEngine;
using Base;

/// <summary>
/// Shop panel - extends UIPanel with shop-specific functionality
/// Handles shop UI orchestration, audio, and refresh logic
/// </summary>
public class ShopPanel : UIPanel
{
    [Header("Shop Containers")]
    [SerializeField] private Transform goodsContainer;
    [SerializeField] private Transform giftsContainer;
    
    [Header("Optional")]
    [SerializeField] private ScrollViewTabManager tabManager;
    
    [Header("Audio")]
    [SerializeField] private bool playOpenSound = true;
    [SerializeField] private bool playCloseSound = true;
    
    protected override void OnShow(object data)
    {
        base.OnShow(data);
        
        // Play shop open sound
        if (playOpenSound && AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ShopOpen);
        }
        
        // Populate shop when opened
        PopulateShop();
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        
        // Clear shop UI
        ClearShop();
        
        // Play shop close sound
        if (playCloseSound && AudioManager.Instance != null && FMODEvents.Instance != null)
        {
            AudioManager.Instance.PlayOneShot(FMODEvents.Instance.ShopClose);
        }
    }
    
    /// <summary>
    /// Populate all shop categories with items
    /// </summary>
    private void PopulateShop()
    {
        if (ItemManager.Instance == null)
        {
            Debug.LogError("[ShopPanel] ItemManager.Instance is null!");
            return;
        }
        
        // Clear existing items
        ClearShop();
        
        // Populate categories
        PopulateCategory(DefaultSettings.ItemCategory.Goods, goodsContainer);
        PopulateCategory(DefaultSettings.ItemCategory.Gift, giftsContainer);
    }
    
    private void PopulateCategory(DefaultSettings.ItemCategory category, Transform container)
    {
        if (container == null) return;
        
        var items = ItemManager.Instance.GetItemsByCategory(category);
        var prefab = ItemManager.Instance.ShopItemPrefab;
        
        if (prefab == null)
        {
            Debug.LogError("[ShopPanel] Shop item prefab is not assigned in ItemManager!");
            return;
        }
        
        foreach (var itemData in items)
        {
            GameObject itemObj = Instantiate(prefab, container);
            
            // Enable the item if it's disabled by default
            if (!itemObj.activeSelf)
            {
                itemObj.SetActive(true);
            }
            
            ShopItemUI itemUI = itemObj.GetComponent<ShopItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Setup(itemData);
            }
            else
            {
                Debug.LogError("[ShopPanel] Shop item prefab missing ShopItemUI component!");
                Destroy(itemObj);
            }
        }
    }
    
    private void ClearShop()
    {
        ClearContainer(goodsContainer);
        ClearContainer(giftsContainer);
    }
    
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
    
    /// <summary>
    /// Manually refresh shop (useful after purchases)
    /// </summary>
    public void RefreshShop()
    {
        PopulateShop();
    }
}

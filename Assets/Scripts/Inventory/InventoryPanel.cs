using UnityEngine;
using System.Collections.Generic;
using Base;
using Base.Inventory.Item;

/// <summary>
/// Panel that displays player's inventory items
/// Gets data from ItemManager singleton
/// Populates when shown with items from player's inventory
/// </summary>
public class InventoryPanel : UIPanel
{
    [Header("Inventory Settings")]
    [SerializeField] private Transform itemContainer;
    
    private List<InventoryItemUI> itemUIList = new List<InventoryItemUI>();
    
    protected override void OnShow(object data)
    {
        base.OnShow(data);
        PopulateInventory();
    }
    
    protected override void OnHide()
    {
        base.OnHide();
        ClearInventory();
    }
    
    /// <summary>
    /// Populate the inventory with player's items
    /// </summary>
    private void PopulateInventory()
    {
        ClearInventory();
        
        if (ItemManager.Instance == null)
        {
            Debug.LogWarning("[InventoryPanel] ItemManager.Instance is null!");
            return;
        }
        
        var items = ItemManager.Instance.GetPlayerInventory();
        
        if (items == null || items.Count == 0)
        {
            Debug.Log("[InventoryPanel] Player has no items in inventory");
            return;
        }
        
        foreach (var item in items)
        {
            if (item == null || item.Amount() <= 0)
                continue;
            
            CreateItemUI(item);
        }
    }
    
    /// <summary>
    /// Create UI element for an item
    /// </summary>
    private void CreateItemUI(IItem item)
    {
        if (itemContainer == null)
        {
            Debug.LogError("[InventoryPanel] Item container is not assigned!");
            return;
        }
        
        var prefab = ItemManager.Instance.InventoryItemPrefab;
        if (prefab == null)
        {
            Debug.LogError("[InventoryPanel] Inventory item prefab is not assigned in ItemManager!");
            return;
        }
        
        GameObject itemObj = Instantiate(prefab, itemContainer);
        
        // Enable the item if it's disabled by default
        if (!itemObj.activeSelf)
        {
            itemObj.SetActive(true);
        }
        
        InventoryItemUI itemUI = itemObj.GetComponent<InventoryItemUI>();
        
        if (itemUI != null)
        {
            // Get icon from ItemManager
            Sprite icon = ItemManager.Instance.GetItemIcon(item.Identifier());
            itemUI.Setup(item, icon);
            itemUIList.Add(itemUI);
        }
        else
        {
            Debug.LogError("[InventoryPanel] Inventory item prefab doesn't have InventoryItemUI component!");
            Destroy(itemObj);
        }
    }
    
    /// <summary>
    /// Clear all inventory UI elements
    /// </summary>
    private void ClearInventory()
    {
        foreach (var itemUI in itemUIList)
        {
            if (itemUI != null)
            {
                Destroy(itemUI.gameObject);
            }
        }
        
        itemUIList.Clear();
    }
    
    /// <summary>
    /// Refresh the inventory display (useful after buying/using items)
    /// </summary>
    public void RefreshInventory()
    {
        PopulateInventory();
    }
}

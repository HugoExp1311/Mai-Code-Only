using UnityEngine;
using System.Collections.Generic;
using Base;
using Base.Inventory.Item;

/// <summary>
/// Centralized manager for all item-related data and operations
/// Handles both shop items and player inventory
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }
    
    [Header("Item Visual Data")]
    [Tooltip("All shop item visual data (icons, sprites)")]
    [SerializeField] private List<ShopItemData> shopItemDataList = new List<ShopItemData>();
    
    [Header("UI Prefabs")]
    [Tooltip("Prefab for shop item UI")]
    [SerializeField] private GameObject shopItemPrefab;
    
    [Tooltip("Prefab for inventory item UI")]
    [SerializeField] private GameObject inventoryItemPrefab;
    
    private Dictionary<string, ShopItemData> itemDataLookup = new Dictionary<string, ShopItemData>();
    
    public GameObject ShopItemPrefab => shopItemPrefab;
    public GameObject InventoryItemPrefab => inventoryItemPrefab;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Initialize()
    {
        // Auto-load items from Resources if list is empty
        if (shopItemDataList.Count == 0)
        {
            LoadItemsFromResources();
        }
        
        BuildItemDataLookup();
    }
    
    /// <summary>
    /// Auto-load all ShopItemData assets from Resources/Shop Item folder
    /// </summary>
    private void LoadItemsFromResources()
    {
        ShopItemData[] items = Resources.LoadAll<ShopItemData>("Shop Item");
        
        if (items.Length > 0)
        {
            shopItemDataList.AddRange(items);
            Debug.Log($"[ItemManager] Auto-loaded {items.Length} items from Resources/Shop Item");
        }
        else
        {
            Debug.LogWarning("[ItemManager] No ShopItemData assets found in Resources/Shop Item folder!");
        }
    }
    
    /// <summary>
    /// Build lookup dictionary for quick access by item ID
    /// </summary>
    private void BuildItemDataLookup()
    {
        itemDataLookup.Clear();
        
        foreach (var itemData in shopItemDataList)
        {
            if (itemData != null && !string.IsNullOrEmpty(itemData.itemId))
            {
                itemDataLookup[itemData.itemId] = itemData;
            }
        }
    }
    
    /// <summary>
    /// Get item visual data by ID
    /// </summary>
    public ShopItemData GetItemData(string itemId)
    {
        itemDataLookup.TryGetValue(itemId, out var itemData);
        return itemData;
    }
    
    /// <summary>
    /// Get item icon by ID
    /// </summary>
    public Sprite GetItemIcon(string itemId)
    {
        var itemData = GetItemData(itemId);
        return itemData?.icon;
    }
    
    /// <summary>
    /// Get all shop items
    /// </summary>
    public List<ShopItemData> GetAllShopItems()
    {
        return shopItemDataList;
    }
    
    /// <summary>
    /// Get shop items by category
    /// </summary>
    public List<ShopItemData> GetItemsByCategory(DefaultSettings.ItemCategory category)
    {
        List<ShopItemData> result = new List<ShopItemData>();
        
        foreach (var itemData in shopItemDataList)
        {
            if (itemData != null && itemData.GetItemInfo().Category == category)
            {
                result.Add(itemData);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Get player's inventory items from GameManager
    /// </summary>
    public List<IItem> GetPlayerInventory()
    {
        if (GameManager.Instance?.Player != null)
        {
            return GameManager.Instance.Player.GetInventoryItems();
        }
        
        Debug.LogWarning("[ItemManager] GameManager or Player is null!");
        return new List<IItem>();
    }
    
    /// <summary>
    /// Check if player has item in inventory
    /// </summary>
    public bool PlayerHasItem(string itemId)
    {
        var inventory = GetPlayerInventory();
        foreach (var item in inventory)
        {
            if (item.Identifier() == itemId && item.Amount() > 0)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get item amount in player's inventory
    /// </summary>
    public int GetPlayerItemAmount(string itemId)
    {
        var inventory = GetPlayerInventory();
        foreach (var item in inventory)
        {
            if (item.Identifier() == itemId)
            {
                return item.Amount();
            }
        }
        return 0;
    }
}

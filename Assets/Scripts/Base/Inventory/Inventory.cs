using System;
using System.Collections.Generic;
using Base.Inventory.Item;

namespace Base.Inventory
{
    public interface IInventory
    {
        public bool HasItem(IItem item);
        public void AddItem(IItem item);
        public void RemoveItem(IItem item);
        public void ReplaceItems(IEnumerable<IItem> items);

        public List<IItem> GetAllItems();
    }

    public class Inventory : IInventory
    {
        private readonly List<IItem> _items = new();
        private const string INVENTORY_SAVE_KEY = "PlayerInventory";

        public Inventory()
        {
            LoadInventory();
        }

        private int CurrentIndex(IItem item)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (_items[i].Identifier() == item.Identifier())
                {
                    return i;
                }
            }

            return -1;
        }

        public bool HasItem(IItem item)
        {
            return CurrentIndex(item) >= 0;
        }

        // OPT-28: Single O(N) scan instead of HasItem() + CurrentIndex() = 2× O(N)
        public void AddItem(IItem item)
        {
            int idx = CurrentIndex(item);
            if (idx >= 0)
            {
                var curItem = _items[idx];
                curItem.UpdateAmount(curItem.Amount() + item.Amount());
            }
            else
            {
                _items.Add(item);
            }
            
            SaveInventory();
        }

        public void RemoveItem(IItem item)
        {
            var curIndex = CurrentIndex(item);
            if (curIndex < 0)
            {
                UnityEngine.Debug.LogWarning($"[Inventory] RemoveItem: Item '{item.Identifier()}' not found in inventory.");
                return;
            }
            var curItem = _items[curIndex];
            curItem.UpdateAmount(curItem.Amount() - 1);
            
            // Remove zero-amount items from the list
            if (curItem.Amount() <= 0)
            {
                _items.RemoveAt(curIndex);
            }
            
            SaveInventory();
        }
        
        public List<IItem> GetAllItems()
        {
            return new List<IItem>(_items);
        }

        public void ReplaceItems(IEnumerable<IItem> items)
        {
            _items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item != null && item.Amount() > 0)
                        _items.Add(item);
                }
            }

            SaveInventory();
        }
        
        /// <summary>
        /// Save inventory to PlayerPrefs
        /// </summary>
        private void SaveInventory()
        {
            var saveData = new InventorySaveData();
            foreach (var item in _items)
            {
                if (item.Amount() > 0)
                {
                    saveData.items.Add(new ItemSaveData
                    {
                        id = item.Identifier(),
                        amount = item.Amount(),
                        price = item.GetValues(),
                        type = (int)item.ItemType,
                        value = item.Value
                    });
                }
            }
            
            string json = UnityEngine.JsonUtility.ToJson(saveData);
            UnityEngine.PlayerPrefs.SetString(INVENTORY_SAVE_KEY, json);
            UnityEngine.PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Load inventory from PlayerPrefs
        /// </summary>
        private void LoadInventory()
        {
            if (!UnityEngine.PlayerPrefs.HasKey(INVENTORY_SAVE_KEY))
            {
                return;
            }
            
            string json = UnityEngine.PlayerPrefs.GetString(INVENTORY_SAVE_KEY);
            var saveData = UnityEngine.JsonUtility.FromJson<InventorySaveData>(json);
            
            _items.Clear();
            foreach (var itemData in saveData.items)
            {
                var item = new CommonItem(
                    itemData.price,
                    itemData.id,
                    (ItemType)itemData.type,
                    itemData.value,
                    itemData.amount
                );
                _items.Add(item);
            }
        }
        
        [System.Serializable]
        private class InventorySaveData
        {
            public List<ItemSaveData> items = new List<ItemSaveData>();
        }
        
        [System.Serializable]
        private class ItemSaveData
        {
            public string id;
            public int amount;
            public int price;
            public int type;
            public int value;
        }
    }
}
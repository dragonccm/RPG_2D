using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unified inventory system to replace multiple inventory managers
/// Consolidates Inventory, ItemManager, and equipment systems
/// </summary>
public class UnifiedInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    [SerializeField] private int maxSlots = 50;
    [SerializeField] private bool enableDebugLogging = false;

    [Header("Equipment Slots")]
    [SerializeField] private int weaponSlotIndex = 0;
    [SerializeField] private int armorSlotIndex = 1;
    [SerializeField] private int accessorySlotIndex = 2;

    private List<InventoryItem> items = new List<InventoryItem>();
    private int gold = 0;
    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    private void Awake()
    {
        ServiceLocator.RegisterService(this);
        InitializeItemDatabase();
    }

    private void InitializeItemDatabase()
    {
        // Create default items
        CreateItem("sword_basic", "Basic Sword", "A simple iron sword", ItemType.Weapon, 10, 50);
        CreateItem("shield_wooden", "Wooden Shield", "Basic wooden protection", ItemType.Armor, 5, 30);
        CreateItem("potion_health", "Health Potion", "Restores 50 HP", ItemType.Consumable, 25, 10);
        CreateItem("herb_healing", "Healing Herb", "Natural healing plant", ItemType.Material, 5, 5);
        CreateItem("gem_ruby", "Ruby Gem", "A valuable red gem", ItemType.Valuable, 100, 20);
        CreateItem("ring_power", "Ring of Power", "Increases attack damage", ItemType.Accessory, 50, 100);
    }

    /// <summary>
    /// Create a new item in the database
    /// </summary>
    public void CreateItem(string id, string name, string description, ItemType type, int value, int maxStack = 1)
    {
        if (itemDatabase.ContainsKey(id))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Item with ID '{0}' already exists", id));
            return;
        }

        ItemData item = new ItemData
        {
            Id = id,
            Name = name,
            Description = description,
            Type = type,
            Value = value,
            MaxStack = maxStack,
            IconPath = "" // Would set actual icon path
        };

        itemDatabase.Add(id, item);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("📦 Created item: {0}", name));
        }
    }

    /// <summary>
    /// Add item to inventory
    /// </summary>
    public bool AddItem(string itemId, int quantity = 1)
    {
        if (!itemDatabase.TryGetValue(itemId, out ItemData itemData))
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Item not found in database: {0}", itemId));
            return false;
        }

        // Check if item already exists in inventory
        InventoryItem existingItem = items.Find(i => i.ItemId == itemId && i.Quantity < itemData.MaxStack);

        if (existingItem != null)
        {
            // Add to existing stack
            int spaceAvailable = itemData.MaxStack - existingItem.Quantity;
            int addAmount = Mathf.Min(quantity, spaceAvailable);

            existingItem.Quantity += addAmount;
            quantity -= addAmount;

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("📦 Added {0} to existing stack of {1}", addAmount, itemData.Name));
            }
        }

        // Add remaining items as new stacks
        while (quantity > 0 && items.Count < maxSlots)
        {
            int addAmount = Mathf.Min(quantity, itemData.MaxStack);

            InventoryItem newItem = new InventoryItem
            {
                ItemId = itemId,
                Quantity = addAmount,
                Durability = itemData.Type == ItemType.Weapon || itemData.Type == ItemType.Armor ? 100 : 0,
                ItemData = itemData
            };

            items.Add(newItem);
            quantity -= addAmount;

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("📦 Added new stack of {0} ({1})", itemData.Name, addAmount));
            }
        }

        // Trigger inventory changed event
        GameEvents.OnInventoryChanged?.Invoke();

        return quantity == 0; // Return true if all items were added
    }

    /// <summary>
    /// Remove item from inventory
    /// </summary>
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        List<InventoryItem> itemsToRemove = items.FindAll(i => i.ItemId == itemId);

        if (itemsToRemove.Count == 0)
        {
            return false;
        }

        int totalRemoved = 0;

        foreach (var item in itemsToRemove)
        {
            if (totalRemoved >= quantity) break;

            int removeAmount = Mathf.Min(quantity - totalRemoved, item.Quantity);
            item.Quantity -= removeAmount;
            totalRemoved += removeAmount;

            if (item.Quantity <= 0)
            {
                items.Remove(item);
            }
        }

        if (totalRemoved > 0)
        {
            GameEvents.OnInventoryChanged?.Invoke();

            if (enableDebugLogging)
            {
                PerformanceUtils.Log(PerformanceUtils.FormatString("🗑️ Removed {0} of item {1}", totalRemoved, itemId));
            }
        }

        return totalRemoved >= quantity;
    }

    /// <summary>
    /// Get item count
    /// </summary>
    public int GetItemCount(string itemId)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item.ItemId == itemId)
            {
                count += item.Quantity;
            }
        }
        return count;
    }

    /// <summary>
    /// Check if inventory has item
    /// </summary>
    public bool HasItem(string itemId, int quantity = 1)
    {
        return GetItemCount(itemId) >= quantity;
    }

    /// <summary>
    /// Use consumable item
    /// </summary>
    public bool UseItem(string itemId)
    {
        if (!itemDatabase.TryGetValue(itemId, out ItemData itemData))
        {
            return false;
        }

        if (itemData.Type != ItemType.Consumable)
        {
            PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ Item is not consumable: {0}", itemData.Name));
            return false;
        }

        if (!RemoveItem(itemId, 1))
        {
            return false;
        }

        // Apply item effect
        ApplyItemEffect(itemData);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🧪 Used item: {0}", itemData.Name));
        }

        return true;
    }

    /// <summary>
    /// Apply item effect
    /// </summary>
    private void ApplyItemEffect(ItemData item)
    {
        switch (item.Id)
        {
            case "potion_health":
                // ServiceLocator.GetService<PlayerController>()?.Heal(50);
                GameEvents.OnHealingReceived?.Invoke(50f);
                break;
            case "herb_healing":
                GameEvents.OnHealingReceived?.Invoke(25f);
                break;
            default:
                PerformanceUtils.LogWarning(PerformanceUtils.FormatString("⚠️ No effect defined for item: {0}", item.Name));
                break;
        }
    }

    /// <summary>
    /// Equip item
    /// </summary>
    public bool EquipItem(string itemId)
    {
        if (!itemDatabase.TryGetValue(itemId, out ItemData itemData))
        {
            return false;
        }

        if (!CanEquipItem(itemData))
        {
            return false;
        }

        // Find equipment slot
        int slotIndex = GetEquipmentSlot(itemData.Type);
        if (slotIndex == -1) return false;

        // Unequip current item if any
        UnequipSlot(slotIndex);

        // Remove item from inventory
        if (!RemoveItem(itemId, 1))
        {
            return false;
        }

        // Create equipped item
        InventoryItem equippedItem = new InventoryItem
        {
            ItemId = itemId,
            Quantity = 1,
            Durability = 100,
            IsEquipped = true,
            EquipmentSlot = slotIndex,
            ItemData = itemData
        };

        items.Insert(slotIndex, equippedItem);

        // Apply equipment effects
        ApplyEquipmentEffect(itemData, true);

        GameEvents.OnEquipmentChanged?.Invoke();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("⚔️ Equipped item: {0}", itemData.Name));
        }

        return true;
    }

    /// <summary>
    /// Unequip item from slot
    /// </summary>
    public bool UnequipSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= items.Count) return false;

        InventoryItem equippedItem = items[slotIndex];
        if (!equippedItem.IsEquipped) return false;

        // Remove from equipment slot
        var itemData = itemDatabase[equippedItem.ItemId];
        items.RemoveAt(slotIndex);

        // Add back to inventory
        AddItem(equippedItem.ItemId, 1);

        // Remove equipment effects
        ApplyEquipmentEffect(itemData, false);

        GameEvents.OnEquipmentChanged?.Invoke();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🔓 Unequipped item from slot {0}", slotIndex));
        }

        return true;
    }

    /// <summary>
    /// Check if item can be equipped
    /// </summary>
    private bool CanEquipItem(ItemData item)
    {
        return item.Type == ItemType.Weapon ||
               item.Type == ItemType.Armor ||
               item.Type == ItemType.Accessory;
    }

    /// <summary>
    /// Get equipment slot for item type
    /// </summary>
    private int GetEquipmentSlot(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon: return weaponSlotIndex;
            case ItemType.Armor: return armorSlotIndex;
            case ItemType.Accessory: return accessorySlotIndex;
            default: return -1;
        }
    }

    /// <summary>
    /// Apply equipment effect
    /// </summary>
    private void ApplyEquipmentEffect(ItemData item, bool equip)
    {
        float modifier = equip ? 1f : -1f;

        switch (item.Id)
        {
            case "sword_basic":
                // ServiceLocator.GetService<PlayerController>()?.ModifyAttackDamage(modifier * 5);
                break;
            case "shield_wooden":
                // ServiceLocator.GetService<PlayerController>()?.ModifyDefense(modifier * 3);
                break;
            case "ring_power":
                // ServiceLocator.GetService<PlayerController>()?.ModifyAttackDamage(modifier * 10);
                break;
        }
    }

    /// <summary>
    /// Add gold to inventory
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        GameEvents.OnGoldChanged?.Invoke(gold);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("💰 Added gold: {0} (Total: {1})", amount, gold));
        }
    }

    /// <summary>
    /// Remove gold from inventory
    /// </summary>
    public bool RemoveGold(int amount)
    {
        if (gold < amount) return false;

        gold -= amount;
        GameEvents.OnGoldChanged?.Invoke(gold);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("💸 Removed gold: {0} (Total: {1})", amount, gold));
        }

        return true;
    }

    /// <summary>
    /// Get current gold amount
    /// </summary>
    public int GetGold()
    {
        return gold;
    }

    /// <summary>
    /// Get all items in inventory
    /// </summary>
    public List<InventoryItem> GetItems()
    {
        return new List<InventoryItem>(items);
    }

    /// <summary>
    /// Set inventory items from save data (used for loading)
    /// </summary>
    public void SetItemsFromSaveData(List<InventoryItem> saveDataList)
    {
        items.Clear();
        foreach (var saveItem in saveDataList)
        {
            // Ensure the ItemData reference is still valid
            if (itemDatabase.TryGetValue(saveItem.ItemId, out ItemData itemTemplate))
            {
                saveItem.ItemData = itemTemplate;
            }
            items.Add(saveItem);
        }
        GameEvents.OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Set gold amount (used for loading save data)
    /// </summary>
    public void SetGold(int amount)
    {
        gold = Mathf.Max(0, amount);
        GameEvents.OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// Get equipped items
    /// </summary>
    public List<InventoryItem> GetEquippedItems()
    {
        return items.FindAll(i => i.IsEquipped);
    }

    /// <summary>
    /// Get item data from database
    /// </summary>
    public ItemData GetItemData(string itemId)
    {
        itemDatabase.TryGetValue(itemId, out ItemData item);
        return item;
    }

    /// <summary>
    /// Get inventory capacity
    /// </summary>
    public int GetCapacity()
    {
        return maxSlots;
    }

    /// <summary>
    /// Get used slots count
    /// </summary>
    public int GetUsedSlots()
    {
        return items.Count;
    }

    /// <summary>
    /// Check if inventory is full
    /// </summary>
    public bool IsFull()
    {
        return items.Count >= maxSlots;
    }

    /// <summary>
    /// Clear all items
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        gold = 0;
        GameEvents.OnInventoryChanged?.Invoke();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🗑️ Cleared inventory");
        }
    }

    /// <summary>
    /// Get inventory value
    /// </summary>
    public int GetTotalValue()
    {
        int totalValue = gold;

        foreach (var item in items)
        {
            if (itemDatabase.TryGetValue(item.ItemId, out ItemData itemData))
            {
                totalValue += itemData.Value * item.Quantity;
            }
        }

        return totalValue;
    }

    /// <summary>
    /// Sort inventory by type
    /// </summary>
    public void SortInventory()
    {
        items.Sort((a, b) =>
        {
            var itemA = itemDatabase[a.ItemId];
            var itemB = itemDatabase[b.ItemId];

            // Sort by type first, then by name
            int typeComparison = itemA.Type.CompareTo(itemB.Type);
            if (typeComparison != 0) return typeComparison;

            return itemA.Name.CompareTo(itemB.Name);
        });

        GameEvents.OnInventoryChanged?.Invoke();

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("🔄 Sorted inventory");
        }
    }
}

/// <summary>
/// Inventory item structure
/// </summary>
[System.Serializable]
public class InventoryItem
{
    public string ItemId;
    public int Quantity;
    public int Durability;
    public bool IsEquipped;
    public int EquipmentSlot;
    [System.NonSerialized]
    public ItemData ItemData;
}

/// <summary>
/// Item data structure
/// </summary>
[System.Serializable]
public class ItemData
{
    public string Id;
    public string Name;
    public string Description;
    public ItemType Type;
    public int Value;
    public int MaxStack;
    public string IconPath;
}

/// <summary>
/// Item type enumeration
/// </summary>
public enum ItemType
{
    Weapon,
    Armor,
    Accessory,
    Consumable,
    Material,
    Valuable,
    Quest
}

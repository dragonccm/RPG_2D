using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ô chứa trong túi đồ.
/// </summary>
[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;

    public void Clear()
    {
        item = null;
        quantity = 0;
    }
}

/// <summary>
/// Dữ liệu lưu trữ túi đồ dạng JSON (Save/Load).
/// </summary>
[Serializable]
public class InventorySaveEntry
{
    public int slotIndex;
    public string itemId;
    public int quantity;
}

/// <summary>
/// Hệ thống quản lý Túi đồ (Inventory) 16 ô cho người chơi.
/// Hỗ trợ: Dùng bình máu/mana, học Cuộn kỹ năng (Skill Scrolls), chứa vật phẩm nhiệm vụ.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public const int TOTAL_SLOTS = 16;

    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<InventoryManager>();
                if (_instance == null)
                {
                    var go = new GameObject("InventoryManager");
                    _instance = go.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }

    [SerializeField] private InventorySlot[] slots = new InventorySlot[TOTAL_SLOTS];

    public event Action OnInventoryChanged;
    public event Action<string> OnNotificationMessage; // Thông báo trên màn hình

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureSlotsInitialized();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void EnsureSlotsInitialized()
    {
        if (slots == null || slots.Length != TOTAL_SLOTS)
        {
            slots = new InventorySlot[TOTAL_SLOTS];
        }
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            if (slots[i] == null) slots[i] = new InventorySlot();
        }
    }

    public InventorySlot GetSlot(int index)
    {
        EnsureSlotsInitialized();
        if (index < 0 || index >= TOTAL_SLOTS) return null;
        return slots[index];
    }

    public int SlotCount => TOTAL_SLOTS;

    /// <summary>
    /// Thêm vật phẩm vào túi đồ (tự động cộng dồn nếu có sẵn).
    /// </summary>
    public bool AddItem(ItemData item, int count = 1)
    {
        if (item == null || count <= 0) return false;
        EnsureSlotsInitialized();

        int remaining = count;

        // BƯỚC 1: Tìm ô có sẵn cùng loại để stack
        if (item.maxStack > 1)
        {
            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item.itemId == item.itemId)
                {
                    int space = item.maxStack - slots[i].quantity;
                    if (space > 0)
                    {
                        int add = Mathf.Min(space, remaining);
                        slots[i].quantity += add;
                        remaining -= add;
                        if (remaining <= 0) break;
                    }
                }
            }
        }

        // BƯỚC 2: Nếu còn dư, tìm ô trống đầu tiên
        if (remaining > 0)
        {
            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].item = item;
                    int add = Mathf.Min(item.maxStack, remaining);
                    slots[i].quantity = add;
                    remaining -= add;
                    if (remaining <= 0) break;
                }
            }
        }

        OnInventoryChanged?.Invoke();

        bool success = (remaining < count);
        if (success)
        {
            OnNotificationMessage?.Invoke($"Nhận được: {item.itemName} x{count - remaining}");
        }
        else
        {
            OnNotificationMessage?.Invoke("Túi đồ đã đầy!");
        }

        return remaining <= 0;
    }

    /// <summary>
    /// Sử dụng vật phẩm tại vị trí slotIndex.
    /// </summary>
    public bool UseItem(int slotIndex, Character user = null)
    {
        EnsureSlotsInitialized();
        if (slotIndex < 0 || slotIndex >= TOTAL_SLOTS) return false;

        var slot = slots[slotIndex];
        if (slot.IsEmpty || !slot.item.isUsable) return false;

        var item = slot.item;

        // 1. DÙNG CUỘN KỸ NĂNG (SKILL SCROLL)
        if (item.itemType == ItemType.SkillScroll)
        {
            if (string.IsNullOrEmpty(item.unlockSkillId)) return false;

            if (SkillTreeManager.Instance != null)
            {
                if (SkillTreeManager.Instance.IsUnlocked(item.unlockSkillId))
                {
                    OnNotificationMessage?.Invoke($"Bạn đã lĩnh ngộ {item.itemName} trước đó rồi!");
                    return false;
                }

                bool unlocked = SkillTreeManager.Instance.Unlock(item.unlockSkillId, bypassCost: true);
                if (unlocked)
                {
                    slot.quantity--;
                    if (slot.quantity <= 0) slot.Clear();

                    OnNotificationMessage?.Invoke($"Lĩnh ngộ thành công: {item.itemName}!");
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        // 2. DÙNG BÌNH HỒI PHỤC (CONSUMABLE)
        if (item.itemType == ItemType.Consumable)
        {
            if (user == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null) user = playerGo.GetComponent<Character>();
            }

            if (user != null && user.health != null)
            {
                if (user.health.currentValue >= user.health.maxValue && item.healAmount > 0)
                {
                    OnNotificationMessage?.Invoke("Máu đã đầy, không cần dùng!");
                    return false;
                }

                if (item.healAmount > 0)
                {
                    user.health.currentValue = Mathf.Min(user.health.maxValue, user.health.currentValue + item.healAmount);
                }
                if (item.manaAmount > 0 && user.mana != null)
                {
                    user.mana.currentValue = Mathf.Min(user.mana.maxValue, user.mana.currentValue + item.manaAmount);
                }

                slot.quantity--;
                if (slot.quantity <= 0) slot.Clear();

                OnNotificationMessage?.Invoke($"Đã dùng: {item.itemName}");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Xóa bớt số lượng của vật phẩm.
    /// </summary>
    public bool RemoveItem(string itemId, int count = 1)
    {
        EnsureSlotsInitialized();
        int need = count;

        for (int i = TOTAL_SLOTS - 1; i >= 0; i--)
        {
            if (!slots[i].IsEmpty && slots[i].item.itemId == itemId)
            {
                if (slots[i].quantity <= need)
                {
                    need -= slots[i].quantity;
                    slots[i].Clear();
                }
                else
                {
                    slots[i].quantity -= need;
                    need = 0;
                }
                if (need <= 0) break;
            }
        }

        OnInventoryChanged?.Invoke();
        return need <= 0;
    }

    public int GetItemCount(string itemId)
    {
        EnsureSlotsInitialized();
        int total = 0;
        for (int i = 0; i < TOTAL_SLOTS; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item.itemId == itemId)
                total += slots[i].quantity;
        }
        return total;
    }

    public void ClearAll()
    {
        EnsureSlotsInitialized();
        for (int i = 0; i < TOTAL_SLOTS; i++) slots[i].Clear();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Khôi phục túi đồ từ save (tra item theo id qua ItemDatabase).</summary>
    public void ImportSave(List<InventorySaveEntry> entries)
    {
        EnsureSlotsInitialized();
        for (int i = 0; i < TOTAL_SLOTS; i++) slots[i].Clear();
        if (entries != null)
        {
            foreach (var e in entries)
            {
                if (e == null || e.slotIndex < 0 || e.slotIndex >= TOTAL_SLOTS || e.quantity <= 0) continue;
                var item = ItemDatabase.Get(e.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"[Inventory] Save có item lạ '{e.itemId}' — bỏ qua");
                    continue;
                }
                slots[e.slotIndex].item = item;
                slots[e.slotIndex].quantity = Mathf.Min(e.quantity, item.maxStack);
            }
        }
        OnInventoryChanged?.Invoke();
    }

    public static void ResetForTest()
    {
        if (_instance != null)
        {
            _instance.ClearAll();
        }
    }
}

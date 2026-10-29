using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tra ItemData theo itemId ổn định (CORE_PROJECT_GUIDELINES 6.6). Mọi ItemData nằm ở Resources/Items.
/// Dùng khi load save (lưu id, không lưu tham chiếu asset) và khi code cần phát thưởng theo id.
/// </summary>
public static class ItemDatabase
{
    public const string ResourcesFolder = "Items";

    private static Dictionary<string, ItemData> byId;

    public static ItemData Get(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (byId == null) Build();
        if (byId.TryGetValue(itemId, out var item) && item != null) return item;
        Build(); // asset có thể vừa được thêm (test, editor)
        return byId.TryGetValue(itemId, out item) ? item : null;
    }

    /// <summary>Đăng ký item tạo runtime (test / item sinh bằng code).</summary>
    public static void Register(ItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId)) return;
        if (byId == null) Build();
        byId[item.itemId] = item;
    }

    private static void Build()
    {
        if (byId == null) byId = new Dictionary<string, ItemData>();
        foreach (var item in Resources.LoadAll<ItemData>(ResourcesFolder))
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            if (byId.TryGetValue(item.itemId, out var existing) && existing != null && existing != item)
                Debug.LogWarning($"[ItemDatabase] Trùng itemId '{item.itemId}': {existing.name} / {item.name}");
            byId[item.itemId] = item;
        }
    }
}

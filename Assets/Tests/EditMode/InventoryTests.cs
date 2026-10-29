using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests cho Hệ thống Túi đồ (Inventory) & Học kỹ năng qua Cuộn bí kíp (Skill Scrolls).
/// </summary>
public class InventoryTests
{
    private GameObject mgrGo;
    private ItemData potionItem;
    private ItemData scrollItem;

    [SetUp]
    public void SetUp()
    {
        if (InventoryManager.Instance != null) Object.DestroyImmediate(InventoryManager.Instance.gameObject);
        if (SkillTreeManager.Instance != null) Object.DestroyImmediate(SkillTreeManager.Instance.gameObject);

        mgrGo = new GameObject("Test_Inventory_Managers");
        mgrGo.AddComponent<InventoryManager>();
        mgrGo.AddComponent<SkillTreeManager>();

        // Tạo item test dạng ScriptableObject runtime
        potionItem = ScriptableObject.CreateInstance<ItemData>();
        potionItem.itemId = "potion_hp";
        potionItem.itemName = "Bình Hồi Máu Nhỏ";
        potionItem.itemType = ItemType.Consumable;
        potionItem.maxStack = 10;
        potionItem.healAmount = 30f;
        potionItem.isUsable = true;

        scrollItem = ScriptableObject.CreateInstance<ItemData>();
        scrollItem.itemId = "scroll_dash";
        scrollItem.itemName = "Cuộn Bí Kíp: Lướt Gió";
        scrollItem.itemType = ItemType.SkillScroll;
        scrollItem.maxStack = 1;
        scrollItem.unlockSkillId = "gio_1";
        scrollItem.isUsable = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (mgrGo != null) Object.DestroyImmediate(mgrGo);
        if (potionItem != null) Object.DestroyImmediate(potionItem);
        if (scrollItem != null) Object.DestroyImmediate(scrollItem);
    }

    [Test]
    public void AddItem_StacksConsumablesUpToMaxStack()
    {
        var inv = InventoryManager.Instance;
        Assert.IsTrue(inv.AddItem(potionItem, 4));
        Assert.AreEqual(4, inv.GetItemCount("potion_hp"));

        // Thêm tiếp 3 bình vào cùng loại
        Assert.IsTrue(inv.AddItem(potionItem, 3));
        Assert.AreEqual(7, inv.GetItemCount("potion_hp"));
        Assert.AreEqual(7, inv.GetSlot(0).quantity);
    }

    [Test]
    public void AddItem_FullInventory_RejectsExtra()
    {
        var inv = InventoryManager.Instance;
        // Điền kín 16 ô với item maxStack = 1
        for (int i = 0; i < 16; i++)
        {
            var single = ScriptableObject.CreateInstance<ItemData>();
            single.itemId = "item_" + i;
            single.maxStack = 1;
            inv.AddItem(single, 1);
        }

        // Ô thứ 17 phải bị từ chối
        var overflow = ScriptableObject.CreateInstance<ItemData>();
        overflow.itemId = "overflow";
        overflow.maxStack = 1;
        bool added = inv.AddItem(overflow, 1);
        Assert.IsFalse(added, "Túi đã đầy 16 ô thì không thể thêm item mới");
    }

    [Test]
    public void UseItem_Consumable_RestoresHealth()
    {
        var inv = InventoryManager.Instance;
        inv.AddItem(potionItem, 2);

        var playerGo = new GameObject("Player");
        playerGo.tag = "Player";
        var ch = playerGo.AddComponent<Character>();
        ch.health = playerGo.AddComponent<Resource>();
        ch.health.Initialize(100f, 0f);
        ch.health.currentValue = 40f; // Mất 60 máu

        bool used = inv.UseItem(0, ch);
        Assert.IsTrue(used, "Dùng bình máu thành công");
        Assert.AreEqual(70f, ch.health.currentValue, "Hồi 30 HP: 40 + 30 = 70");
        Assert.AreEqual(1, inv.GetSlot(0).quantity, "Số lượng bình giảm từ 2 xuống 1");

        Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void UseItem_SkillScroll_UnlocksSkillDirectlyWithoutPoints()
    {
        var inv = InventoryManager.Instance;
        var tree = SkillTreeManager.Instance;

        Assert.IsFalse(tree.IsUnlocked("gio_1"), "Ban đầu chưa có kỹ năng Dash");
        Assert.AreEqual(0, tree.SkillPoints, "0 điểm kỹ năng");

        inv.AddItem(scrollItem, 1);
        Assert.AreEqual(1, inv.GetItemCount("scroll_dash"));

        bool used = inv.UseItem(0);
        Assert.IsTrue(used, "Dùng cuộn bí kíp thành công");
        Assert.IsTrue(tree.IsUnlocked("gio_1"), "Kỹ năng gio_1 đã được mở khóa trực tiếp qua bí kíp!");
        Assert.IsTrue(tree.IsUnlocked("root"), "Root node cũng được mở tự động");
        Assert.AreEqual(0, inv.GetItemCount("scroll_dash"), "Cuộn bí kíp đã tiêu hao");
    }

    [Test]
    public void SaveSystem_Roundtrip_PreservesInventoryEntries()
    {
        var inv = InventoryManager.Instance;
        inv.AddItem(potionItem, 5);
        inv.AddItem(scrollItem, 1);

        var data = SaveSystem.CaptureFrom(null, null, "test_cp");
        Assert.AreEqual(2, data.inventoryEntries.Count, "Lưu đủ 2 loại vật phẩm");

        Assert.AreEqual(0, data.inventoryEntries[0].slotIndex);
        Assert.AreEqual("potion_hp", data.inventoryEntries[0].itemId);
        Assert.AreEqual(5, data.inventoryEntries[0].quantity);

        Assert.AreEqual(1, data.inventoryEntries[1].slotIndex);
        Assert.AreEqual("scroll_dash", data.inventoryEntries[1].itemId);
        Assert.AreEqual(1, data.inventoryEntries[1].quantity);
    }
}

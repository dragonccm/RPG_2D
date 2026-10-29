using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Trạng thái thế giới lưu được: WorldFlags, túi đồ khôi phục theo itemId, cuộn bí kíp đúng thiết kế.
/// </summary>
public class WorldStateTests
{
    private GameObject mgrGo;

    [SetUp]
    public void SetUp()
    {
        WorldFlags.ResetForTest();
        if (InventoryManager.Instance != null) Object.DestroyImmediate(InventoryManager.Instance.gameObject);
        if (SkillTreeManager.Instance != null) Object.DestroyImmediate(SkillTreeManager.Instance.gameObject);
        mgrGo = new GameObject("WorldState_TestManagers");
        mgrGo.AddComponent<InventoryManager>();
        mgrGo.AddComponent<SkillTreeManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (mgrGo != null) Object.DestroyImmediate(mgrGo);
        WorldFlags.ResetForTest();
    }

    [Test]
    public void WorldFlags_SetExportImport_Roundtrip()
    {
        string changed = "none";
        WorldFlags.Changed += f => changed = f;

        WorldFlags.Set("chest:a");
        Assert.AreEqual("chest:a", changed);
        Assert.IsTrue(WorldFlags.Has("chest:a"));

        var saved = WorldFlags.Export();
        WorldFlags.Import(new List<string>());
        Assert.IsFalse(WorldFlags.Has("chest:a"));
        Assert.IsNull(changed, "Import báo Changed(null) để mọi object đọc lại trạng thái");

        WorldFlags.Import(saved);
        Assert.IsTrue(WorldFlags.Has("chest:a"));
    }

    [Test]
    public void ItemDatabase_FindsShippedScrolls()
    {
        Assert.IsNotNull(ItemDatabase.Get("scroll_dash"), "Resources/Items/Scroll_WindDash");
        Assert.IsNotNull(ItemDatabase.Get("scroll_teleport"), "Resources/Items/Scroll_ShadowTeleport");
        Assert.IsNotNull(ItemDatabase.Get("scroll_charge"), "Resources/Items/Scroll_FlameCharge");
        Assert.IsNotNull(ItemDatabase.Get("key_tower"));
        Assert.IsNull(ItemDatabase.Get("khong_ton_tai"));
    }

    [Test]
    public void Inventory_ImportSave_RestoresItemsById()
    {
        var entries = new List<InventorySaveEntry>
        {
            new InventorySaveEntry { slotIndex = 2, itemId = "potion_hp", quantity = 3 },
            new InventorySaveEntry { slotIndex = 5, itemId = "scroll_dash", quantity = 1 },
            new InventorySaveEntry { slotIndex = 6, itemId = "item_bi_xoa", quantity = 1 },
        };
        InventoryManager.Instance.ImportSave(entries);

        Assert.AreEqual(3, InventoryManager.Instance.GetSlot(2).quantity);
        Assert.AreEqual("scroll_dash", InventoryManager.Instance.GetSlot(5).item.itemId);
        Assert.IsTrue(InventoryManager.Instance.GetSlot(6).IsEmpty, "item không còn tồn tại bị bỏ qua");
    }

    [Test]
    public void SaveCapture_IncludesFlagsAndInventory()
    {
        WorldFlags.Set("switch:altar_1");
        InventoryManager.Instance.AddItem(ItemDatabase.Get("key_tower"), 1);

        var data = SaveSystem.CaptureFrom(null, null, "test");
        Assert.Contains("switch:altar_1", data.questFlags);
        Assert.AreEqual(1, data.inventoryEntries.Count);
        Assert.AreEqual("key_tower", data.inventoryEntries[0].itemId);
    }

    [Test]
    public void ScrollUse_UnlocksScrollOnlyNode()
    {
        var mgr = SkillTreeManager.Instance;
        Assert.AreEqual(SkillTreeManager.UnlockResult.ScrollOnly, mgr.CanUnlock("khong_1", out _));

        var inv = InventoryManager.Instance;
        inv.AddItem(ItemDatabase.Get("scroll_teleport"), 1);
        Assert.IsTrue(inv.UseItem(0));
        Assert.IsTrue(mgr.IsUnlocked("khong_1"));
        Assert.AreEqual(0, inv.GetItemCount("scroll_teleport"), "cuộn bị tiêu hao");
    }
}

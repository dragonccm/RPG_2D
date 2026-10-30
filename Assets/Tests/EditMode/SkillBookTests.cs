using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Kỹ năng chỉ dùng được sau khi học (cuộn/quest/boss) — SkillBook là nguồn sự thật.</summary>
public class SkillBookTests
{
    private GameObject playerGo;
    private ModularSkillManager manager;
    private SkillModule ice, heal;
    private GameObject invGo;

    [SetUp]
    public void SetUp()
    {
        SkillBook.ResetForTest();
        PlayerPrefs.SetInt("PlayerLevel", 10);
        if (InventoryManager.Instance != null) Object.DestroyImmediate(InventoryManager.Instance.gameObject);
        invGo = new GameObject("Inv");
        invGo.AddComponent<InventoryManager>();

        ice = ScriptableObject.CreateInstance<SkillModule>();
        ice.name = "IceBlast"; ice.skillId = "ice_blast"; ice.skillName = "Ice Blast"; ice.requiredLevel = 1;
        heal = ScriptableObject.CreateInstance<SkillModule>();
        heal.name = "Heal"; heal.skillId = "heal"; heal.skillName = "Heal"; heal.requiredLevel = 3;

        playerGo = new GameObject("P");
        playerGo.AddComponent<Character>();
        manager = playerGo.AddComponent<ModularSkillManager>();
        manager.availableSkills.Add(ice);
        manager.availableSkills.Add(heal);
        manager.EnsureInitialized();
        manager.UpdateUnlockedSlots();
    }

    [TearDown]
    public void TearDown()
    {
        SkillBook.ResetForTest();
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(invGo);
        Object.DestroyImmediate(ice);
        Object.DestroyImmediate(heal);
        SkillBook.ResetForTest();
        PlayerPrefs.DeleteKey("PlayerLevel");
    }

    [Test]
    public void SkillId_FallsBackToAssetName()
    {
        var m = ScriptableObject.CreateInstance<SkillModule>();
        m.name = "Fireball";
        Assert.AreEqual("Fireball", m.Id);
        m.skillId = "fire_ball";
        Assert.AreEqual("fire_ball", m.Id);
        Object.DestroyImmediate(m);
    }

    [Test]
    public void Character_StartsWithNoSkills()
    {
        Assert.IsEmpty(manager.GetAvailableSkills(), "chưa học gì → không có kỹ năng");
        Assert.IsFalse(manager.EquipSkill(0, ice), "không gắn được kỹ năng chưa học");
    }

    [Test]
    public void LearningSkill_UnlocksItAndAutoEquipsFirstEmptySlot()
    {
        Assert.IsTrue(SkillBook.Learn(ice));
        Assert.IsFalse(SkillBook.Learn(ice), "học lần 2 không có tác dụng");
        CollectionAssert.AreEquivalent(new[] { ice }, manager.GetAvailableSkills());
        Assert.AreSame(ice, manager.GetSlot(0).equippedSkill, "tự gắn vào ô 0");

        SkillBook.Learn(heal);
        Assert.AreSame(heal, manager.GetSlot(1).equippedSkill, "kỹ năng kế → ô trống kế");
    }

    [Test]
    public void SkillScroll_LearnsSkill_AndIsConsumed()
    {
        var scroll = ScriptableObject.CreateInstance<ItemData>();
        scroll.itemId = "scroll_skill_ice_blast"; scroll.itemName = "Cuộn Băng Tiễn";
        scroll.itemType = ItemType.SkillScroll; scroll.maxStack = 1; scroll.learnSkill = ice;

        var inv = InventoryManager.Instance;
        inv.AddItem(scroll, 1);
        Assert.IsTrue(inv.UseItem(0));
        Assert.IsTrue(SkillBook.Has("ice_blast"));
        Assert.AreEqual(0, inv.GetItemCount("scroll_skill_ice_blast"));

        inv.AddItem(scroll, 1);
        Assert.IsFalse(inv.UseItem(0), "đã học thì không dùng lại, cuộn còn nguyên");
        Assert.AreEqual(1, inv.GetItemCount("scroll_skill_ice_blast"));
        Object.DestroyImmediate(scroll);
    }

    [Test]
    public void SaveRoundtrip_KeepsLearnedAndEquippedById()
    {
        SkillBook.Learn(ice);
        SkillBook.Learn(heal);
        manager.UnequipSkill(0);
        manager.EquipSkill(2, ice);

        var data = SaveSystem.CaptureFrom(null, manager, "test");
        CollectionAssert.AreEquivalent(new[] { "ice_blast", "heal" }, data.learnedSkills);
        CollectionAssert.Contains(data.equippedSkills, "2=ice_blast");

        // Game mới → nạp save
        SkillBook.Import(null);
        manager.ImportEquipped(null);
        Assert.IsEmpty(manager.GetAvailableSkills());

        SkillBook.Import(data.learnedSkills);
        manager.ImportEquipped(data.equippedSkills);
        Assert.AreSame(ice, manager.GetSlot(2).equippedSkill);
        Assert.AreSame(heal, manager.GetSlot(1).equippedSkill);
        Assert.IsFalse(manager.GetSlot(0).HasSkill(), "ô 0 đã bỏ gắn trước khi lưu → vẫn trống");
    }

    [Test]
    public void Import_DoesNotAutoEquipUnlisted()
    {
        SkillBook.Import(new List<string> { "ice_blast" });
        Assert.IsFalse(manager.GetSlot(0).HasSkill(), "nạp save không tự gắn — ImportEquipped quyết định");
        Assert.IsTrue(SkillBook.Has("ice_blast"));
    }
}

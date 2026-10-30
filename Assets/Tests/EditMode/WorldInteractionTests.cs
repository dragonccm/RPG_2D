using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests cho Bẫy (SpikeTrap), Câu đố (PuzzleSwitch), và Rương báu (TreasureChest).
/// </summary>
public class WorldInteractionTests
{
    private GameObject rootGo;

    [TearDown]
    public void TearDown()
    {
        if (rootGo != null)
        {
            Object.DestroyImmediate(rootGo);
        }
        if (InventoryManager.Instance != null)
        {
            Object.DestroyImmediate(InventoryManager.Instance.gameObject);
        }
        if (QuestManager.Instance != null)
        {
            Object.DestroyImmediate(QuestManager.Instance.gameObject);
        }
    }

    [Test]
    public void SpikeTrap_InitialState_IsRetracted()
    {
        rootGo = new GameObject("Test_SpikeTrap");
        rootGo.AddComponent<BoxCollider2D>();
        var trap = rootGo.AddComponent<SpikeTrap>();

        Assert.AreEqual(SpikeTrap.TrapState.Retracted, trap.CurrentState);
    }

    [Test]
    public void SpikeTrap_TriggerManually_TransitionsToTelegraph()
    {
        rootGo = new GameObject("Test_SpikeTrap");
        rootGo.AddComponent<BoxCollider2D>();
        var trap = rootGo.AddComponent<SpikeTrap>();

        trap.TriggerManually();
        Assert.AreEqual(SpikeTrap.TrapState.Telegraph, trap.CurrentState);
    }

    [Test]
    public void PuzzleSwitch_InitialState_IsNotActivated()
    {
        rootGo = new GameObject("Test_PuzzleSwitch");
        var sw = rootGo.AddComponent<PuzzleSwitch>();

        Assert.IsFalse(sw.IsActivated);
        Assert.IsFalse(sw.IsDead);
        Assert.AreEqual(1f, sw.MaxHealth);
        Assert.AreEqual(1f, sw.CurrentHealth);
    }

    [Test]
    public void PuzzleSwitch_ActivateSwitch_DeactivatesBarrierAndFiresEvent()
    {
        rootGo = new GameObject("Test_PuzzleSwitch");
        var barrierGo = new GameObject("TargetBarrier");
        barrierGo.transform.SetParent(rootGo.transform);

        var sw = rootGo.AddComponent<PuzzleSwitch>();
        // Sử dụng reflection hoặc serialize field để gán targetBarrier
        var barrierField = typeof(PuzzleSwitch).GetField("targetBarrier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        barrierField?.SetValue(sw, barrierGo);

        bool eventFired = false;
        sw.OnSwitchToggled += (s) => eventFired = true;

        sw.ActivateSwitch();

        Assert.IsTrue(sw.IsActivated);
        Assert.IsFalse(barrierGo.activeSelf, "Rào cản phải bị vô hiệu hóa khi công tắc bật");
        Assert.IsTrue(eventFired, "Sự kiện OnSwitchToggled phải được kích hoạt");
        Assert.IsTrue(sw.IsDead);
        Assert.AreEqual(0f, sw.CurrentHealth);
    }

    [Test]
    public void PuzzleSwitch_TakeDamage_ActivatesSwitch()
    {
        rootGo = new GameObject("Test_PuzzleSwitch");
        var sw = rootGo.AddComponent<PuzzleSwitch>();

        sw.TakeDamage(10f);
        Assert.IsTrue(sw.IsActivated, "Chém trúng công tắc phải kích hoạt nó");
    }

    [Test]
    public void PuzzleSwitch_ResetSwitch_RestoresBarrier()
    {
        rootGo = new GameObject("Test_PuzzleSwitch");
        var barrierGo = new GameObject("TargetBarrier");
        barrierGo.transform.SetParent(rootGo.transform);

        var sw = rootGo.AddComponent<PuzzleSwitch>();
        var barrierField = typeof(PuzzleSwitch).GetField("targetBarrier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        barrierField?.SetValue(sw, barrierGo);

        sw.ActivateSwitch();
        Assert.IsFalse(barrierGo.activeSelf);

        sw.ResetSwitch();
        Assert.IsFalse(sw.IsActivated);
        Assert.IsTrue(barrierGo.activeSelf, "Rào cản phải xuất hiện trở lại khi reset công tắc");
    }

    [Test]
    public void TreasureChest_OpenChest_GrantsRewardToInventoryOnlyOnce()
    {
        var mgrGo = new GameObject("Test_Managers");
        mgrGo.AddComponent<InventoryManager>();

        rootGo = new GameObject("Test_Chest");
        var chest = rootGo.AddComponent<TreasureChest>();

        var testItem = ScriptableObject.CreateInstance<ItemData>();
        testItem.itemId = "test_scroll";
        testItem.itemName = "Cuộn Test";
        testItem.itemType = ItemType.SkillScroll;
        testItem.maxStack = 1;

        var itemField = typeof(TreasureChest).GetField("rewardItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var countField = typeof(TreasureChest).GetField("rewardCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        itemField?.SetValue(chest, testItem);
        countField?.SetValue(chest, 1);

        Assert.IsFalse(chest.IsOpened);
        Assert.AreEqual(0, InventoryManager.Instance.GetItemCount("test_scroll"));

        // Mở rương lần 1
        chest.OpenChest();
        Assert.IsTrue(chest.IsOpened);
        Assert.AreEqual(1, InventoryManager.Instance.GetItemCount("test_scroll"));

        // Mở lại không được nhận thêm
        chest.OpenChest();
        Assert.AreEqual(1, InventoryManager.Instance.GetItemCount("test_scroll"));

        Object.DestroyImmediate(testItem);
        Object.DestroyImmediate(mgrGo);
    }
}

using NUnit.Framework;
using UnityEngine;

/// <summary>Logic bẫy & câu đố mới (LEVEL_DESIGN 4) — phần thuần, không cần physics runtime.</summary>
public class WorldTrapTests
{
    [SetUp] public void SetUp() => WorldFlags.ResetForTest();
    [TearDown] public void TearDown() => WorldFlags.ResetForTest();

    [Test]
    public void TorchLogic_AllLitWithinWindow_Solves()
    {
        var t = new TimedTorchLogic(3, 6f);
        Assert.IsFalse(t.Light(0, 0f));
        Assert.IsFalse(t.Light(1, 2f));
        Assert.IsTrue(t.Light(2, 5.9f), "3 đuốc cùng cháy trong 6s → giải");
        Assert.IsTrue(t.Solved);
        Assert.IsTrue(t.IsLit(0, 100f), "giải xong thì cháy mãi");
    }

    [Test]
    public void TorchLogic_FirstBurnsOut_NotSolved()
    {
        var t = new TimedTorchLogic(3, 6f);
        t.Light(0, 0f);
        t.Light(1, 3f);
        Assert.IsFalse(t.Light(2, 6.5f), "đuốc 0 đã tắt ở 6s");
        Assert.IsFalse(t.IsLit(0, 6.5f));
        Assert.AreEqual(0.5f, t.Remaining01(1, 6f), 0.001f);
        Assert.IsTrue(t.Light(0, 7f), "thắp lại đuốc 0 khi 1, 2 còn cháy → giải");
    }

    [Test]
    public void FallHazard_RespawnPoint_IsOutsideFromEntry()
    {
        var gap = new Rect(0f, 0f, 2f, 3f);
        var walkedIn = FallHazard.RespawnPoint(new Vector2(0.2f, 1.5f), gap, 0.6f);
        Assert.AreEqual(-0.6f, walkedIn.x, 0.001f, "bước vào từ mép trái → về bên trái");
        var teleportedIn = FallHazard.RespawnPoint(new Vector2(1f, 1.5f), gap, 0.6f);
        Assert.IsFalse(gap.Contains(teleportedIn), "đứng giữa khe vẫn được đẩy ra ngoài");
        Assert.IsTrue(FallHazard.ShouldFall(false));
        Assert.IsFalse(FallHazard.ShouldFall(true), "dash/teleport bay qua");
    }

    [Test]
    public void CollapsingFloor_StateMachine()
    {
        var s = CollapsingFloor.FloorState.Cracking;
        Assert.AreEqual(s, CollapsingFloor.Next(s, 0.5f, 1f, 0f));
        Assert.AreEqual(CollapsingFloor.FloorState.Collapsed, CollapsingFloor.Next(s, 1f, 1f, 0f));
        var c = CollapsingFloor.FloorState.Collapsed;
        Assert.AreEqual(c, CollapsingFloor.Next(c, 99f, 1f, 0f), "không restore → hố vĩnh viễn");
        Assert.AreEqual(CollapsingFloor.FloorState.Intact, CollapsingFloor.Next(c, 5f, 1f, 5f));
    }

    [Test]
    public void DartTrap_TelegraphWindow()
    {
        Assert.IsFalse(DartTrap.InTelegraph(1.0f, 2f, 0.6f));
        Assert.IsTrue(DartTrap.InTelegraph(1.5f, 2f, 0.6f));
        Assert.IsFalse(DartTrap.InTelegraph(2.0f, 2f, 0.6f), "đã bắn");
    }

    [Test]
    public void SpeedModifiers_TakeSlowest_AndRestore()
    {
        var go = new GameObject("P");
        go.AddComponent<Rigidbody2D>();
        var pc = go.AddComponent<PlayerController>();
        var a = new GameObject("A");
        var b = new GameObject("B");

        pc.SetSpeedModifier(a, 0.5f);
        pc.SetSpeedModifier(b, 0.8f);
        Assert.AreEqual(0.5f, pc.SpeedMultiplier, 0.001f);
        pc.RemoveSpeedModifier(a);
        Assert.AreEqual(0.8f, pc.SpeedMultiplier, 0.001f);
        pc.RemoveSpeedModifier(b);
        Assert.AreEqual(1f, pc.SpeedMultiplier, 0.001f);

        Object.DestroyImmediate(a);
        Object.DestroyImmediate(b);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Breakable_BreakSetsFlagAndDisablesCollider()
    {
        var go = new GameObject("Rock");
        var col = go.AddComponent<BoxCollider2D>();
        var br = go.AddComponent<Breakable>();
        br.Break();
        Assert.IsTrue(br.IsBroken);
        Assert.IsFalse(col.enabled);
        Assert.IsTrue(WorldFlags.Has(br.Flag));
        Object.DestroyImmediate(go);
    }
}

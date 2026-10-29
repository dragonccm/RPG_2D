using NUnit.Framework;
using UnityEngine;

/// <summary>Dữ liệu đòn đánh hợp lệ (CORE_PROJECT_GUIDELINES 6.2): windup, telegraph, chuỗi không vòng.</summary>
public class AttackDefTests
{
    private static AttackDef New(AttackTeam team, float windup = 0.5f)
    {
        var d = ScriptableObject.CreateInstance<AttackDef>();
        d.attackId = "t_" + team; d.team = team; d.windup = windup;
        return d;
    }

    [Test]
    public void EveryShippedAttackDef_IsValid()
    {
        var all = Resources.LoadAll<AttackDef>("Attacks");
        Assert.GreaterOrEqual(all.Length, 11, "Player ×3, quái ×5, Agis ×3 — chạy Tools/RPG/Combat/Build Attack Defs");
        foreach (var d in all)
        {
            Assert.IsTrue(d.Validate(out string err), err);
            Assert.Greater(d.windup, 0f, d.name);
        }
    }

    [Test]
    public void PlayerCombo_IsAThreeHitAcyclicChain()
    {
        var head = Resources.Load<AttackDef>("Attacks/Player_Slash1");
        Assert.IsNotNull(head);
        Assert.IsTrue(AttackDef.IsChainAcyclic(head, out int len));
        Assert.AreEqual(3, len);
        Assert.AreEqual(1.5f, head.next.next.damageMultiplier, "nhịp 3 (finisher) mạnh hơn");
        Assert.AreEqual(HitFeedback.Heavy, head.next.next.feedback);
    }

    [Test]
    public void Validate_RejectsShortWindups_ByTeam()
    {
        var enemy = New(AttackTeam.Enemy, 0.1f);
        Assert.IsFalse(enemy.Validate(out _), "quái cần windup ≥ 0.3s");
        var boss = New(AttackTeam.Boss, 0.3f);
        Assert.IsFalse(boss.Validate(out _), "boss cần windup ≥ 0.4s");
        var player = New(AttackTeam.Player, 0.1f); player.telegraph = false;
        Assert.IsTrue(player.Validate(out string e), e);
    }

    [Test]
    public void Validate_BossAreaAttack_NeedsLongWindup()
    {
        var slam = New(AttackTeam.Boss, 0.6f);
        slam.shape = AttackShape.Circle; slam.radius = 3f;
        Assert.IsFalse(slam.Validate(out _), "AoE boss ≥ 0.8s");
        slam.windup = 0.8f;
        Assert.IsTrue(slam.Validate(out string e), e);
    }

    [Test]
    public void Validate_EnemyAttackMustTelegraph()
    {
        var d = New(AttackTeam.Enemy);
        d.telegraph = false;
        Assert.IsFalse(d.Validate(out string e));
        StringAssert.Contains("telegraph", e);
    }

    [Test]
    public void Chain_Cycle_IsRejected()
    {
        var a = New(AttackTeam.Player, 0.2f); a.telegraph = false;
        var b = New(AttackTeam.Player, 0.2f); b.telegraph = false;
        a.attackId = "a"; b.attackId = "b";
        a.next = b; b.next = a;
        Assert.IsFalse(AttackDef.IsChainAcyclic(a, out _));
        Assert.IsFalse(a.Validate(out _));
    }

    [Test]
    public void TriggerRange_FollowsShape()
    {
        var circle = New(AttackTeam.Enemy); circle.shape = AttackShape.Circle; circle.reach = 1f; circle.radius = 0.8f;
        Assert.AreEqual(1.4f, circle.TriggerRange, 0.001f);
        circle.centerOnOwner = true;
        Assert.AreEqual(0.8f, circle.TriggerRange, 0.001f);
        var proj = New(AttackTeam.Enemy); proj.shape = AttackShape.Projectile; proj.projectileRange = 10f;
        Assert.AreEqual(9f, proj.TriggerRange, 0.001f);
    }
}

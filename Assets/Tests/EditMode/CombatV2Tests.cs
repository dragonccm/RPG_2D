using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// S4 v2 — combat rebuild: kỹ năng đặc biệt bị khoá bởi cây; hệ số tier-2–4 đổi hành vi.
/// Test logic thuần (không phụ trợ runtime physics).
/// </summary>
public class CombatV2Tests
{
    private GameObject mgrGo;

    [SetUp]
    public void SetUp()
    {
        LevelSystem.ResetForTest();
        if (QuestManager.Instance != null) Object.DestroyImmediate(QuestManager.Instance.gameObject);
        if (SkillTreeManager.Instance != null) Object.DestroyImmediate(SkillTreeManager.Instance.gameObject);
        mgrGo = new GameObject("CV2_TestManagers");
        mgrGo.AddComponent<QuestManager>();
        mgrGo.AddComponent<SkillTreeManager>();
    }

    [TearDown]
    public void TearDown()
    {
        if (mgrGo != null) Object.DestroyImmediate(mgrGo);
        LevelSystem.ResetForTest();
    }

    [Test]
    public void SkillGating_DashTeleportCharge_RequireTier1Nodes()
    {
        var sm = new GameObject("SM").AddComponent<special_move>();
        Assert.IsTrue(sm.IsSkillGated("gio_1"), "Chưa mở gio_1 → dash bị khoá");
        Assert.IsTrue(sm.IsSkillGated("khong_1"), "Chưa mở khong_1 → teleport bị khoá");
        Assert.IsTrue(sm.IsSkillGated("lua_1"), "Chưa mở lua_1 → charge bị khoá");

        var mgr = SkillTreeManager.Instance;
        mgr.Unlock("root"); // miễn phí
        mgr.GrantPoints(3);
        Assert.IsTrue(mgr.Unlock("gio_1"));
        Assert.IsFalse(sm.IsSkillGated("gio_1"), "Mở gio_1 → dash giải khoá");
        Object.DestroyImmediate(sm.gameObject);
    }

    [Test]
    public void SkillTree_Passives_Than1Than2aThan2b_AppliedOnUnlock()
    {
        var go = new GameObject("TestPlayer");
        go.AddComponent<PlayerController>();
        var ch = go.AddComponent<Character>();
        ch.health = go.AddComponent<Resource>();
        ch.health.Initialize(100f, 0f);
        float apBefore = ch.AttackPower;
        float critBefore = ch.CriticalChance;

        var mgr = SkillTreeManager.Instance;
        mgr.Unlock("root");
        LevelSystem.Import(2, 0);
        mgr.GrantPoints(3);
        mgr.Unlock("than_1");
        mgr.Unlock("than_2a");
        mgr.Unlock("than_2b");

        Assert.AreEqual(125f, ch.health.maxValue, 0.01f, "than_1 +25 HP");
        Assert.AreEqual(apBefore + 1.5f, ch.AttackPower, 0.01f, "than_2a +1.5 dmg");
        Assert.AreEqual(Mathf.Clamp01(critBefore + 0.05f), ch.CriticalChance, 0.001f, "than_2b +5% crit");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Than4_DeathDefiance_TriggersOncePerWindow()
    {
        var mgr = SkillTreeManager.Instance;
        Assert.IsFalse(mgr.TryTriggerDeathDefiance(), "Chưa mở than_4 → không sống sót");
        LevelSystem.Import(12, 0);
        mgr.Unlock("root");
        mgr.GrantPoints(6);
        mgr.Unlock("than_1");
        mgr.Unlock("than_2a");
        mgr.Unlock("than_2b");
        mgr.Unlock("than_3a");
        mgr.Unlock("than_3b");
        mgr.Unlock("than_4");
        Assert.IsTrue(mgr.TryTriggerDeathDefiance(), "Mở than_4 + lần đầu → kích hoạt");
        Assert.IsFalse(mgr.TryTriggerDeathDefiance(), "60s cooldown → lần 2 trong cửa sổ bị chặn");
    }

    [Test]
    public void Quest_Q2_ForestKills_CompleteQuest()
    {
        var qm = QuestManager.Instance;
        qm.StartQuest("q2");
        for (int i = 0; i < 3; i++) qm.ReportProgress("q2", "q2_kill");
        Assert.AreEqual(QuestState.Active, qm.GetState("q2"), "3 kills nhưng chưa hạ mini-boss");
        qm.ReportProgress("q2", "q2_miniboss");
        Assert.AreEqual(QuestState.Done, qm.GetState("q2"), "Đủ kill + mini-boss → Complete + auto-claim");
        Assert.AreEqual(2, SkillTreeManager.Instance.SkillPoints,
            "1 điểm thưởng + 1 điểm level-up (120 XP)");
        Assert.AreEqual(2, LevelSystem.Level, "120 XP → level 2");
    }

    [Test]
    public void SaveSystem_Roundtrip_KeepsTreeAndQuests()
    {
        var mgr = SkillTreeManager.Instance;
        var qm = QuestManager.Instance;
        mgr.Unlock("root");
        mgr.GrantPoints(2);
        mgr.Unlock("gio_1");
        qm.StartQuest("q2");
        qm.ReportProgress("q2", "q2_kill");

        var data = SaveSystem.CaptureFrom(null, null, "test");
        Assert.AreEqual(1, data.skillPoints, "2 điểm - 1 đã tiêu = 1");
        Assert.Contains("root", data.unlockedSkills);
        Assert.Contains("gio_1", data.unlockedSkills);
        Assert.AreEqual(1, data.questEntries.Count);
        Assert.AreEqual("q2", data.questEntries[0].questId);

        // Import ngược
        mgr.ImportSave(data.skillPoints, data.unlockedSkills);
        qm.ImportSave(data.questEntries);
        Assert.IsTrue(mgr.IsUnlocked("gio_1"));
        Assert.AreEqual(QuestState.Active, qm.GetState("q2"));
        Assert.AreEqual(1, qm.GetCounter("q2", "q2_kill"));
    }
}

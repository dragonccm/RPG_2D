using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// S4 (GAME_MASTER_PLAN 7, 9, 10): test skill tree gating, quest lifecycle, dialogue data.
/// </summary>
public class QuestSkillTests
{
    private GameObject mgrGo;

    [SetUp]
    public void SetUp()
    {
        LevelSystem.ResetForTest();
        if (QuestManager.Instance != null) Object.DestroyImmediate(QuestManager.Instance.gameObject);
        if (SkillTreeManager.Instance != null) Object.DestroyImmediate(SkillTreeManager.Instance.gameObject);
        mgrGo = new GameObject("QS_TestManagers");
        mgrGo.AddComponent<QuestManager>();
        mgrGo.AddComponent<SkillTreeManager>();
        var st = SkillTreeManager.Instance;
        Assert.IsNotNull(st, "SkillTreeManager.Instance phải sẵn sàng sau AddComponent");
        var (points, nodes) = st.ExportSave();
        Assert.AreEqual(0, points, "Bắt đầu không có điểm");
        Assert.AreEqual(0, nodes.Count, "Bắt đầu không có nút mở");
    }

    [TearDown]
    public void TearDown()
    {
        if (mgrGo != null) Object.DestroyImmediate(mgrGo);
        LevelSystem.ResetForTest();
    }

    [Test]
    public void SkillTree_Has19Nodes_RootPlus18()
    {
        Assert.AreEqual(19, SkillTreeManager.ALL_NODES.Length);
        int branchNodes = 0;
        foreach (var n in SkillTreeManager.ALL_NODES)
            if (n.tier > 0) branchNodes++;
        Assert.AreEqual(18, branchNodes, "Gốc + 18 nút nhánh = 19");
    }

    [Test]
    public void SkillTree_Tier4RequiresLevel12_AndPrereqChain()
    {
        var mgr = SkillTreeManager.Instance;
        // Không điểm, level 1
        Assert.AreEqual(SkillTreeManager.UnlockResult.LockedPrereq,
            mgr.CanUnlock("gio_3", out _), "tier 3 cần nút tier 2 trước");
        // Level 6: mở được tới gio_3 (chain + level), gio_4 vẫn chặn level 12
        LevelSystem.Import(6, 0);
        mgr.GrantPoints(3);
        Assert.IsTrue(mgr.Unlock("root"), "Gốc mở miễn phí");
        Assert.AreEqual(SkillTreeManager.UnlockResult.ScrollOnly,
            mgr.CanUnlock("gio_1", out _), "tier 1 nhánh kỹ năng chỉ học từ cuộn");
        Assert.IsTrue(mgr.Unlock("gio_1", bypassCost: true), "cuộn Lướt Gió mở gio_1");
        Assert.IsTrue(mgr.Unlock("gio_2"));
        Assert.IsTrue(mgr.Unlock("gio_3"));
        Assert.AreEqual(SkillTreeManager.UnlockResult.LevelTooLow,
            mgr.CanUnlock("gio_4", out _), "tier 4 cần level 12");
        // Level 12 + đủ chain + điểm
        LevelSystem.Import(12, 0);
        mgr.GrantPoints(1);
        Assert.IsTrue(mgr.Unlock("gio_4"), "Level 12 + đủ chain + đủ điểm → mở được đại kỹ");
        Assert.IsTrue(mgr.IsUnlocked("gio_4"));
    }
    [Test]
    public void SkillTree_PassiveThan1_IncreasesMaxHealth()
    {
        var go = new GameObject("TestPlayer");
        var pc = go.AddComponent<PlayerController>();
        var ch = go.AddComponent<Character>();
        ch.health = go.AddComponent<Resource>();
        ch.health.Initialize(100f, 0f);
        var mgr = SkillTreeManager.Instance;
        mgr.GrantPoints(2);
        mgr.Unlock("root"); // miễn phí
        Assert.IsTrue(mgr.Unlock("than_1"));
        Assert.AreEqual(125f, ch.health.maxValue, 0.01f, "Thể Lực: +25 HP");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Quest_Lifecycle_Q1_ReportAndAutoClaim()
    {
        var qm = QuestManager.Instance;
        Assert.AreEqual(QuestState.Locked, qm.GetState("q1"));
        qm.StartQuest("q1");
        Assert.AreEqual(QuestState.Active, qm.GetState("q1"));
        qm.ReportProgress("q1", "talk_ongte");
        Assert.AreEqual(QuestState.Active, qm.GetState("q1"), "Còn thiếu combo → chưa Complete");
        qm.ReportProgress("q1", "q1_combo");
        Assert.AreEqual(QuestState.Done, qm.GetState("q1"), "Đủ mục tiêu → Complete + tự nhận thưởng (Done)");
        Assert.GreaterOrEqual(LevelSystem.Level, 1);
    }

    [Test]
    public void Quest_RewardGrantsSkillPoint_AndXPLevelsUp()
    {
        var qm = QuestManager.Instance;
        qm.StartQuest("q2");
        qm.ReportProgress("q2", "q2_kill");       // 1/3
        qm.ReportProgress("q2", "q2_kill");       // 2/3
        qm.ReportProgress("q2", "q2_miniboss");   // đủ miniboss nhưng thiếu 1 kill
        Assert.AreEqual(QuestState.Active, qm.GetState("q2"));
        qm.ReportProgress("q2", "q2_kill");       // 3/3 → Complete + claim
        Assert.AreEqual(QuestState.Done, qm.GetState("q2"));
        Assert.AreEqual(2, SkillTreeManager.Instance.SkillPoints,
            "1 điểm thưởng + 1 điểm level-up (120 XP)");
        Assert.GreaterOrEqual(LevelSystem.Level, 2);
        Assert.AreEqual(20, LevelSystem.Xp);
    }

    [Test]
    public void Dialogue_HasMaiAndOngTeBlocks_WithVNText()
    {
        Assert.IsNotNull(DialogueDatabase.MaiFirstMeet);
        Assert.AreEqual(3, DialogueDatabase.MaiFirstMeet.lines.Length);
        StringAssert.Contains("trăng", DialogueDatabase.MaiFirstMeet.lines[0].text);
        Assert.AreEqual("q1", DialogueDatabase.MaiFirstMeet.startQuestOnEnd /* S4 */);
        Assert.IsNotNull(DialogueDatabase.OngTeQuest);
        StringAssert.Contains("q1:talk_ongte", DialogueDatabase.OngTeQuest.reportObjectiveOnEnd);
    }
}

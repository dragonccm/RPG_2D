using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// S3 (GAME_MASTER_PLAN 5.5, 14-12): test save/load JSON — roundtrip + backup + version.
/// </summary>
public class SaveSystemTests
{
    private string tempPath;

    [SetUp]
    public void SetUp()
    {
        tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"rpg_save_test_{Guid.NewGuid():N}.json");
        SaveSystem.PathOverride = () => tempPath;
    }

    [TearDown]
    public void TearDown()
    {
        SaveSystem.PathOverride = null;
        if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
        if (System.IO.File.Exists(tempPath + ".bak")) System.IO.File.Delete(tempPath + ".bak");
    }

    [Test]
    public void SaveLoad_Roundtrip_PreservesAllFields()
    {
        var d = new SaveSystem.SaveData
        {
            scene = "M1_Village",
            posX = 3.5f,
            posY = -19.95f,
            hp = 77f,
            maxHp = 120f,
            level = 7,
            attackPower = 14f,
            criticalChance = 0.25f,
            criticalMultiplier = 2.5f,
            checkpointId = "campfire_forest",
        };
        d.questFlags.Add("q2_done");

        Assert.IsTrue(SaveSystem.Save(d));
        var loaded = SaveSystem.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual("M1_Village", loaded.scene);
        Assert.AreEqual(3.5f, loaded.posX, 0.001f);
        Assert.AreEqual(-19.95f, loaded.posY, 0.001f);
        Assert.AreEqual(77f, loaded.hp, 0.001f);
        Assert.AreEqual(120f, loaded.maxHp, 0.001f);
        Assert.AreEqual(7, loaded.level);
        Assert.AreEqual(14f, loaded.attackPower, 0.001f);
        Assert.AreEqual(0.25f, loaded.criticalChance, 0.001f);
        Assert.AreEqual(2.5f, loaded.criticalMultiplier, 0.001f);
        Assert.AreEqual("campfire_forest", loaded.checkpointId);
        Assert.AreEqual(SaveSystem.CURRENT_VERSION, loaded.saveVersion);
        Assert.Contains("q2_done", loaded.questFlags);
    }

    [Test]
    public void Save_CreatesBackup_OnSecondSave()
    {
        var d1 = new SaveSystem.SaveData { checkpointId = "first" };
        var d2 = new SaveSystem.SaveData { checkpointId = "second" };
        SaveSystem.Save(d1);
        SaveSystem.Save(d2);
        Assert.IsTrue(System.IO.File.Exists(tempPath + ".bak"), "Lần save thứ 2 phải tạo backup");
        var backup = System.IO.File.ReadAllText(tempPath + ".bak");
        StringAssert.Contains("first", backup);
        StringAssert.Contains("second", System.IO.File.ReadAllText(tempPath));
    }

    [Test]
    public void Load_CorruptMainFile_FallsBackToBackup()
    {
        var d = new SaveSystem.SaveData { checkpointId = "backup_data", level = 4 };
        SaveSystem.Save(d);
        // Lần save thứ 2 tạo backup chứa d; rồi ghi rác đè file chính
        SaveSystem.Save(new SaveSystem.SaveData { checkpointId = "newer" });
        System.IO.File.WriteAllText(tempPath, "{json hỏng!!!");
        // Lỗi đọc file hỏng là hành vi có chủ đích của SaveSystem
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("SaveSystem: đọc .* lỗi"));

        var loaded = SaveSystem.Load();
        Assert.IsNotNull(loaded, "File chính hỏng phải fallback về backup");
        Assert.AreEqual("backup_data", loaded.checkpointId, "Backup chứa save trước khi bị hỏng");
    }

    [Test]
    public void Load_FutureVersion_ReturnsNull()
    {
        // Save() ép saveVersion về bản hiện tại (chủ đích) — ghi JSON tay để mô phỏng save từ engine mới hơn
        System.IO.File.WriteAllText(tempPath,
            "{\"saveVersion\":999,\"checkpointId\":\"from-future\"}");
        Assert.IsNull(SaveSystem.Load(), "Save từ engine mới hơn phải bị bỏ qua");
    }

    [Test]
    public void Save_Null_ReturnsFalse()
    {
        Assert.IsFalse(SaveSystem.Save(null));
    }
}

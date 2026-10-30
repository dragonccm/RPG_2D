using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

/// <summary>Bảng chuỗi (Loc) và bảng âm thanh (AudioService) đầy đủ, không khoá/clip mồ côi.</summary>
public class LocAudioTests
{
    [SetUp] public void SetUp() { PlayerPrefs.DeleteKey("language"); Loc.ResetForTest(); }
    [TearDown] public void TearDown() { PlayerPrefs.DeleteKey("language"); Loc.ResetForTest(); }

    [Test]
    public void EveryLocKeyUsedInCode_ExistsInVietnameseTable()
    {
        var rx = new Regex("Loc\\.T\\(\"([^\"]+)\"");
        var missing = Directory.GetFiles(Path.Combine(Application.dataPath, "_Project/Scripts"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("Loc.cs"))
            .SelectMany(f => rx.Matches(File.ReadAllText(f)).Cast<Match>().Select(m => m.Groups[1].Value))
            .Distinct().Where(k => !Loc.Has(k)).ToList();
        Assert.IsEmpty(missing, "khoá thiếu trong Resources/Loc/vi.json: " + string.Join(", ", missing));
    }

    [Test]
    public void EnglishTable_OnlyContainsKnownKeys_AndSwitchingWorks()
    {
        Loc.Language = "en";
        Assert.AreEqual("New game", Loc.T("menu.new"));
        Assert.AreEqual("Learned: Heal!", Loc.T("inv.learned.ok", "Heal"));
        Loc.Language = "vi";
        Assert.AreEqual("Chơi mới", Loc.T("menu.new"));

        var en = JsonUtility.FromJson<EntriesProbe>(Resources.Load<TextAsset>("Loc/en").text);
        foreach (var e in en.entries) Assert.IsTrue(Loc.Has(e.key), "en có khoá lạ: " + e.key);
    }

    [System.Serializable] private class EntriesProbe { public KeyText[] entries; }
    [System.Serializable] private class KeyText { public string key; public string text; }

    [Test]
    public void MissingKey_ReturnsKey_AndBadFormatDoesNotThrow()
    {
        Assert.AreEqual("khong.co", Loc.T("khong.co"));
        Assert.DoesNotThrow(() => Loc.T("inv.detail", "a"));
    }

    [Test]
    public void AudioBank_CoversEverySfxId_WithClips_AndMusic()
    {
        var bank = Resources.Load<SfxBank>("Audio/AudioBank");
        Assert.IsNotNull(bank, "Tools/RPG/Audio/Build Audio Bank");
        foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
        {
            var e = bank.Find(id);
            Assert.IsNotNull(e, id + " thiếu entry");
            Assert.Greater(e.clips.Length, 0, id + " không có clip");
            Assert.IsFalse(e.clips.Any(c => c == null), id + " có clip null");
        }
        Assert.IsNotNull(bank.mapMusic);
        Assert.IsNotNull(bank.battleMusic);
    }

    [Test]
    public void Attenuation_FadesWithDistance()
    {
        Assert.AreEqual(1f, AudioService.AttenuationFor(0f), 0.001f);
        Assert.AreEqual(0.5f, AudioService.AttenuationFor(AudioService.HearingRange * 0.5f), 0.001f);
        Assert.AreEqual(0f, AudioService.AttenuationFor(AudioService.HearingRange * 2f), 0.001f);
    }
}

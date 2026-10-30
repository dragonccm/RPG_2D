using System.Linq;
using NUnit.Framework;

/// <summary>Mọi chuỗi từ dữ liệu (hội thoại, quest, vật phẩm, kỹ năng) đều có khoá trong vi.json.</summary>
public class LocExportTests
{
    [SetUp] public void SetUp() { UnityEngine.PlayerPrefs.DeleteKey("language"); Loc.ResetForTest(); }

    [Test]
    public void ViTable_ContainsEveryDataString()
    {
        var missing = LocExport.CollectDataStrings().Keys.Where(k => !Loc.Has(k)).ToList();
        Assert.IsEmpty(missing, "chạy Tools/RPG/Loc/Export Vietnamese Table — thiếu: " + string.Join(", ", missing.Take(10)));
    }

    [Test]
    public void DialogueText_UsesTranslation_WhenAvailable_ElseOriginal()
    {
        var block = DialogueDatabase.MaiFirstMeet;
        Assert.AreEqual(block.lines[0].text, DialogueLoc.Text(block, 0), "vi = chữ gốc");
        Loc.Language = "en"; // en chưa dịch hội thoại → rơi về vi/gốc
        Assert.AreEqual(block.lines[0].text, DialogueLoc.Text(block, 0));
        Loc.Language = "vi";
    }

    [Test]
    public void Dialogue_CoversAllBlocksAndSpeakers()
    {
        var d = LocExport.CollectDataStrings();
        Assert.IsTrue(d.ContainsKey("dlg.mai_first.0.text"));
        Assert.IsTrue(d.ContainsKey("dlg.speaker.Mai"));
        Assert.IsTrue(d.ContainsKey("quest.q1.title"));
        Assert.IsTrue(d.ContainsKey("skillnode.gio_1.name"));
    }
}

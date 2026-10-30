using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Kỹ năng phát qua cuộn nằm trong khung cân bằng (GAME_MASTER_PLAN 5.3: HP 100, mana 50, quái 30–60 HP).</summary>
public class SkillBalanceTests
{
    private static SkillModule[] ScrollSkills() =>
        AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Resources/Items" })
            .Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i => i != null && i.learnSkill != null).Select(i => i.learnSkill).ToArray();

    [Test]
    public void ScrollSkills_FitBalanceEnvelope()
    {
        var skills = ScrollSkills();
        Assert.GreaterOrEqual(skills.Length, 6, "đủ cuộn kỹ năng (Băng Tiễn, Hồi Phục, Lôi Kích, Phép Tiễn, Chấn Động, Hỏa Cầu)");
        foreach (var s in skills)
        {
            Assert.IsFalse(string.IsNullOrEmpty(s.skillId), s.name + " thiếu skillId");
            Assert.GreaterOrEqual(s.manaCost, 5f, s.name + ": mana quá rẻ");
            Assert.LessOrEqual(s.manaCost, 50f, s.name + ": đắt hơn tổng mana 50");
            Assert.GreaterOrEqual(s.cooldown, 0.5f, s.name + ": hồi chiêu quá ngắn");
            Assert.LessOrEqual(s.damage / s.cooldown, 20f, s.name + ": sát thương/giây quá cao");
            Assert.LessOrEqual(s.damage, 60f, s.name + ": một phát quá mạnh (quái 30–60 HP, boss 1000)");
        }
        Assert.AreEqual(skills.Length, skills.Select(s => s.Id).Distinct().Count(), "skillId trùng");
    }

    [Test]
    public void EveryEliteReward_IsARealItem()
    {
        foreach (var m in MapSpecs.All())
            foreach (var enc in m.encounters.Values)
                foreach (var e in enc.enemies.Where(x => !string.IsNullOrEmpty(x.rewardItemId)))
                    Assert.IsNotNull(ItemDatabase.Get(e.rewardItemId), $"{m.sceneName}: {e.eliteName} thưởng '{e.rewardItemId}' không tồn tại");
    }
}

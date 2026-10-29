using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh cuộn bí kíp (ItemData) cho các SkillModule học được từ cuộn — Resources/Items/Scroll_Skill_*.asset.
/// Mỗi cuộn: itemId ổn định "scroll_skill_<skillId>", trỏ learnSkill tới SkillModule. Chạy kèm MapBuilder.BuildAll.
/// </summary>
public static class SkillScrollBuilder
{
    private const string ItemFolder = "Assets/_Project/Resources/Items";

    // (asset SkillModule, id ổn định) — nguồn: LEVEL_DESIGN 2.2 / phần thưởng rương các map
    private static readonly (string asset, string id, string vi)[] Scrolls =
    {
        ("IceBlast", "ice_blast", "Băng Tiễn"),
        ("Heal", "heal", "Hồi Phục"),
        ("ThunderStrike", "thunder_strike", "Lôi Kích"),
    };

    public static void BuildAll()
    {
        foreach (var (asset, id, vi) in Scrolls)
        {
            string skillPath = $"Assets/_Project/Data/Skills/Old/{asset}.asset";
            var skill = AssetDatabase.LoadAssetAtPath<SkillModule>(skillPath);
            if (skill == null) { Debug.LogWarning("[SkillScrollBuilder] Thiếu " + skillPath); continue; }

            if (skill.skillId != id || skill.source != SkillSource.Scroll)
            {
                skill.skillId = id;
                skill.source = SkillSource.Scroll;
                EditorUtility.SetDirty(skill);
            }

            string path = $"{ItemFolder}/Scroll_Skill_{asset}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.itemId = "scroll_skill_" + id;
            item.itemName = "Cuộn Bí Kíp: " + vi;
            item.description = $"Dùng để lĩnh ngộ {vi}. Kỹ năng được gắn vào ô kỹ năng trống đầu tiên.";
            item.itemType = ItemType.SkillScroll;
            item.maxStack = 1;
            item.isUsable = true;
            item.learnSkill = skill;
            item.unlockSkillId = "";
            EditorUtility.SetDirty(item);
        }
        AssetDatabase.SaveAssets();
    }
}

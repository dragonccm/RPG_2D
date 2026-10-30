using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Xuất chuỗi hiển thị từ dữ liệu (hội thoại, quest, vật phẩm, kỹ năng, nút cây kỹ năng) vào Resources/Loc/vi.json
/// để dịch mà không đụng mã. Khoá cũ không thuộc dữ liệu (menu.*, inv.*...) được giữ nguyên.
/// Tools/RPG/Loc/Export Vietnamese Table — cũng chạy trong MapBuilder.BuildAll. Test LocExportTests đảm bảo bảng luôn đủ.
/// </summary>
public static class LocExport
{
    private const string ViPath = "Assets/_Project/Resources/Loc/vi.json";
    private static readonly string[] DataPrefixes = { "dlg.", "quest.", "item.", "skill.", "skillnode." };

    // Khoá nhập tay trùng tiền tố dữ liệu (nhãn loại vật phẩm...) — không được coi là khoá dữ liệu
    private static bool IsDataKey(string k) => !k.StartsWith("item.type.") && DataPrefixes.Any(p => k.StartsWith(p));

    [System.Serializable] private class Entry { public string key; public string text; }
    [System.Serializable] private class Table { public Entry[] entries; }

    /// <summary>Mọi chuỗi sinh từ dữ liệu: khoá → chữ gốc.</summary>
    public static Dictionary<string, string> CollectDataStrings()
    {
        var d = new Dictionary<string, string>();

        // Hội thoại: các DialogueBlock tĩnh của DialogueDatabase
        foreach (var f in typeof(DialogueDatabase).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!(f.GetValue(null) is DialogueBlock b)) continue;
            for (int i = 0; i < b.lines.Length; i++)
            {
                d[DialogueLoc.TextKey(b.blockId, i)] = b.lines[i].text;
                d[DialogueLoc.SpeakerKey(b.lines[i].speaker)] = b.lines[i].speaker;
            }
        }

        // Quest
        foreach (var q in QuestManager.ALL_QUESTS)
        {
            d[$"quest.{q.id}.title"] = q.title;
            d[$"quest.{q.id}.desc"] = q.desc;
            foreach (var o in q.objectives) d[$"quest.{q.id}.obj.{o.id}"] = o.label;
        }

        // Vật phẩm
        foreach (var g in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Resources/Items" }))
        {
            var it = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g));
            if (it == null || string.IsNullOrEmpty(it.itemId)) continue;
            d[$"item.{it.itemId}.name"] = it.itemName;
            d[$"item.{it.itemId}.desc"] = it.description ?? "";
        }

        // Kỹ năng chủ động
        foreach (var g in AssetDatabase.FindAssets("t:SkillModule", new[] { "Assets/_Project/Data/Skills" }))
        {
            var sk = AssetDatabase.LoadAssetAtPath<SkillModule>(AssetDatabase.GUIDToAssetPath(g));
            if (sk == null) continue;
            d[$"skill.{sk.Id}.name"] = sk.skillName;
            d[$"skill.{sk.Id}.desc"] = sk.description ?? "";
        }

        // Nút cây kỹ năng
        foreach (var n in SkillTreeManager.ALL_NODES)
        {
            d[$"skillnode.{n.id}.name"] = n.name;
            d[$"skillnode.{n.id}.desc"] = n.desc;
        }
        return d;
    }

    [MenuItem("Tools/RPG/Loc/Export Vietnamese Table")]
    public static void Export()
    {
        var table = JsonUtility.FromJson<Table>(File.ReadAllText(ViPath)) ?? new Table();
        var manual = (table.entries ?? new Entry[0]).Where(e => !IsDataKey(e.key)).ToList();
        var data = CollectDataStrings();
        var all = manual.Concat(data.OrderBy(kv => kv.Key).Select(kv => new Entry { key = kv.Key, text = kv.Value })).ToArray();
        // Giữ bản dịch tay đã sửa cho khoá dữ liệu nếu có (không ghi đè)
        var existing = (table.entries ?? new Entry[0]).ToDictionary(e => e.key, e => e.text);
        foreach (var e in all) if (IsDataKey(e.key) && existing.TryGetValue(e.key, out var t) && !string.IsNullOrEmpty(t)) e.text = t;
        File.WriteAllText(ViPath, JsonUtility.ToJson(new Table { entries = all }, true));
        AssetDatabase.ImportAsset(ViPath);
        Debug.Log($"[LocExport] vi.json: {manual.Count} khoá tay + {data.Count} khoá dữ liệu");
    }
}

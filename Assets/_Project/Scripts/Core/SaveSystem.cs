using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// S3 (GAME_MASTER_PLAN 5.5): save/load JSON tại persistentDataPath.
/// - 1 slot chính + 1 backup (ghi backup trước khi ghi chính — chống hỏng file).
/// - saveVersion để migrate tương lai.
/// - Không lưu Transform path — chỉ scene + toạ độ.
/// Checkpoint là điểm save duy nhất (đống lửa) + auto-save khi qua portal.
/// </summary>
public static class SaveSystem
{
    public const int CURRENT_VERSION = 1;

    [Serializable]
    public class SaveData
    {
        public int saveVersion = CURRENT_VERSION;
        public string scene;
        public float posX, posY;
        public float hp, maxHp;
        public int level = 1;
        public float attackPower, criticalChance, criticalMultiplier;
        public List<string> questFlags = new List<string>();
        public string checkpointId = "";
        public string savedAtIso = "";
        public int skillPoints;
        public List<string> unlockedSkills = new List<string>();
        public List<QuestSaveEntry> questEntries = new List<QuestSaveEntry>();
        public int playerXp;
        public List<InventorySaveEntry> inventoryEntries = new List<InventorySaveEntry>();
        public List<string> learnedSkills = new List<string>();   // SkillBook
        public List<string> equippedSkills = new List<string>();  // "slot=skillId"
    }

    /// <summary>Override cho unit test (mặc định persistentDataPath).</summary>
    public static Func<string> PathOverride;

    public static string SavePath => PathOverride != null
        ? PathOverride()
        : Path.Combine(Application.persistentDataPath, "save_rpg.json");
    public static string BackupPath => SavePath + ".bak";

    public static bool Save(SaveData data)
    {
        if (data == null) return false;
        data.saveVersion = CURRENT_VERSION;
        data.savedAtIso = DateTime.UtcNow.ToString("o");
        try
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, overwrite: true);
            File.WriteAllText(SavePath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: ghi save lỗi — {e.Message}");
            return false;
        }
    }

    public static SaveData Load()
    {
        var d = TryRead(SavePath);
        if (d == null) d = TryRead(BackupPath);
        return d;
    }

    private static SaveData TryRead(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (d == null || d.saveVersion > CURRENT_VERSION) return null; // save mới hơn engine — bỏ
            return d;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem: đọc {path} lỗi — {e.Message}");
            return null;
        }
    }

    public static bool HasSave => File.Exists(SavePath);

    /// <summary>Chụp trạng thái game hiện tại thành SaveData.</summary>
    public static SaveData CaptureFrom(PlayerController player, ModularSkillManager skill, string checkpointId)
    {
        var character = player != null ? player.GetComponent<Character>() : null;
        var health = character != null ? character.health : null;
        var data = new SaveData
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            posX = player != null ? player.transform.position.x : 0f,
            posY = player != null ? player.transform.position.y : 0f,
            hp = health != null ? health.currentValue : 0f,
            maxHp = health != null ? health.maxValue : 100f,
            level = skill != null ? skill.GetPlayerLevel() : 1,
            attackPower = character != null ? character.AttackPower : 10f,
            criticalChance = character != null ? character.CriticalChance : 0.1f,
            criticalMultiplier = character != null ? character.CriticalMultiplier : 2f,
            checkpointId = checkpointId ?? "",
        };
        if (SkillTreeManager.Instance != null)
        {
            var (points, nodes) = SkillTreeManager.Instance.ExportSave();
            data.skillPoints = points;
            data.unlockedSkills = nodes;
        }
        if (QuestManager.Instance != null)
            data.questEntries = QuestManager.Instance.ExportSave();
        data.playerXp = LevelSystem.Xp;
        data.questFlags = WorldFlags.Export();
        data.learnedSkills = SkillBook.Export();
        if (skill != null) data.equippedSkills = skill.ExportEquipped();

        if (InventoryManager.Instance != null)
        {
            for (int i = 0; i < InventoryManager.TOTAL_SLOTS; i++)
            {
                var s = InventoryManager.Instance.GetSlot(i);
                if (s != null && !s.IsEmpty)
                {
                    data.inventoryEntries.Add(new InventorySaveEntry
                    {
                        slotIndex = i,
                        itemId = s.item.itemId,
                        quantity = s.quantity
                    });
                }
            }
        }
        return data;
    }

    /// <summary>Áp SaveData vào game (load).</summary>
    public static void ApplyTo(SaveData d, PlayerController player, ModularSkillManager skill)
    {
        if (d == null || player == null) return;
        var character = player.GetComponent<Character>();
        if (character != null)
        {
            if (character.health != null) character.health.Initialize(
                d.maxHp > 0 ? d.maxHp : 100f, 0f);
            character.health.currentValue = d.hp > 0 ? d.hp : d.maxHp;
            character.AttackPower = d.attackPower > 0 ? d.attackPower : 10f;
            character.CriticalChance = Mathf.Clamp01(d.criticalChance);
            character.CriticalMultiplier = Mathf.Max(1f, d.criticalMultiplier);
        }
        if (skill != null && d.level > 0) skill.SetPlayerLevel(d.level);
        if (SkillTreeManager.Instance != null)
            SkillTreeManager.Instance.ImportSave(d.skillPoints, d.unlockedSkills);
        if (QuestManager.Instance != null)
            QuestManager.Instance.ImportSave(d.questEntries);
        LevelSystem.Import(d.level, d.playerXp);
        WorldFlags.Import(d.questFlags);
        SkillBook.Import(d.learnedSkills);
        if (skill != null) skill.ImportEquipped(d.equippedSkills);
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ImportSave(d.inventoryEntries);
        player.transform.position = new Vector3(d.posX, d.posY, 0f);
    }
}

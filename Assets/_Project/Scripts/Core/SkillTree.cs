using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S4 (GAME_MASTER_PLAN 7): cây kỹ năng 19 nút — 4 nhánh (Gió/Không/Lửa/Thân) + gốc.
/// Mỗi nút 1 điểm; tier 3 cần level ≥ 6, tier 4 cần level ≥ 12.
/// Nguyên tắc: nút nào cũng đổi hành vi nhìn thấy được. Passive áp ngay lúc mở;
/// nút hành vi để cờ IsUnlocked — hệ thống dash/teleport/charge đọc sau (S4+).
/// </summary>
public enum SkillBranch { Gio = 0, Khong = 1, Lua = 2, Than = 3 }

[Serializable]
public class SkillNodeDef
{
    public string id;
    public SkillBranch branch;
    public int tier;
    public string name;
    public string desc;
    public int requiredLevel;
}

public class SkillTreeManager : MonoBehaviour
{
    private static SkillTreeManager _instance;
    public static SkillTreeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<SkillTreeManager>();
                if (_instance != null) _instance.EnsureInit();
            }
            return _instance;
        }
    }

    public static readonly SkillNodeDef[] ALL_NODES =
    {
        new SkillNodeDef { id = "root", branch = (SkillBranch)0, tier = 0, name = "Kiến Thức Tỉnh", desc = "Mở cây kỹ năng (Tab)", requiredLevel = 1 },
        // NHÁNH GIÓ (Dash)
        new SkillNodeDef { id = "gio_1", branch = SkillBranch.Gio, tier = 1, name = "Gió Lạnh", desc = "Mở Dash", requiredLevel = 1 },
        new SkillNodeDef { id = "gio_2", branch = SkillBranch.Gio, tier = 2, name = "Lướt Xuyên", desc = "Dash xuyên địch", requiredLevel = 2 },
        new SkillNodeDef { id = "gio_3", branch = SkillBranch.Gio, tier = 3, name = "Kích Ứng", desc = "Dash gây 20 dmg", requiredLevel = 6 },
        new SkillNodeDef { id = "gio_4", branch = SkillBranch.Gio, tier = 4, name = "CUỒNG GIÓ", desc = "Đại kỹ: dash không hồi 5s", requiredLevel = 12 },
        // NHÁNH KHÔNG (Teleport)
        new SkillNodeDef { id = "khong_1", branch = SkillBranch.Khong, tier = 1, name = "Bước Ngắn", desc = "Mở Teleport", requiredLevel = 1 },
        new SkillNodeDef { id = "khong_2", branch = SkillBranch.Khong, tier = 2, name = "Đổi Vị", desc = "Teleport qua tường mỏng", requiredLevel = 2 },
        new SkillNodeDef { id = "khong_3", branch = SkillBranch.Khong, tier = 3, name = "Phân Thân", desc = "Tạo bản sao dụ địch 2s", requiredLevel = 6 },
        new SkillNodeDef { id = "khong_4", branch = SkillBranch.Khong, tier = 4, name = "HOÁ BÓNG ẤM", desc = "Đại kỹ: teleport 3 điểm liên tiếp", requiredLevel = 12 },
        // NHÁNH LỬA (Charge)
        new SkillNodeDef { id = "lua_1", branch = SkillBranch.Lua, tier = 1, name = "Sốc Nhiệt", desc = "Mở Charge", requiredLevel = 1 },
        new SkillNodeDef { id = "lua_2", branch = SkillBranch.Lua, tier = 2, name = "Cháy Lan", desc = "Charge gây burn 3s", requiredLevel = 2 },
        new SkillNodeDef { id = "lua_3", branch = SkillBranch.Lua, tier = 3, name = "Trấn Áp", desc = "Charge stun 1s", requiredLevel = 6 },
        new SkillNodeDef { id = "lua_4", branch = SkillBranch.Lua, tier = 4, name = "THIÊN PHỤC", desc = "Đại kỹ: charge nổ diện rộng", requiredLevel = 12 },
        // NHÁNH THÂN (Passive) — 6 nút
        new SkillNodeDef { id = "than_1", branch = SkillBranch.Than, tier = 1, name = "Thể Lực", desc = "+25 HP tối đa", requiredLevel = 1 },
        new SkillNodeDef { id = "than_2a", branch = SkillBranch.Than, tier = 2, name = "Cường Kích", desc = "+1.5 sát thương", requiredLevel = 2 },
        new SkillNodeDef { id = "than_2b", branch = SkillBranch.Than, tier = 2, name = "Mũi Nhọn", desc = "+5% chí mạng", requiredLevel = 2 },
        new SkillNodeDef { id = "than_3a", branch = SkillBranch.Than, tier = 3, name = "Bóng Nhẹ", desc = "Dash hồi nhanh −0.15s", requiredLevel = 6 },
        new SkillNodeDef { id = "than_3b", branch = SkillBranch.Than, tier = 3, name = "Dự Bị", desc = "I-frame khi hồi máu", requiredLevel = 6 },
        new SkillNodeDef { id = "than_4", branch = SkillBranch.Than, tier = 4, name = "BẤT DIỆT THÂN", desc = "Đại kỹ: 5s không chết + hồi 30% HP", requiredLevel = 12 },
    };

    private readonly HashSet<string> unlocked = new HashSet<string>();
    public int SkillPoints { get; private set; }

    public event Action<string> OnNodeUnlocked;
    public event Action<int> OnPointsChanged;

    private Character playerCharacter;

    // S4 (than_4): đứng dậy 1 lần mỗi 60s
    private float deathDefianceReadyAt = -999f;

    public bool TryTriggerDeathDefiance()
    {
        if (!IsUnlocked("than_4")) return false;
        if (Time.time < deathDefianceReadyAt) return false;
        deathDefianceReadyAt = Time.time + 60f;
        return true;
    }

    private void Awake() => EnsureInit();

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>EditMode test AddComponent không chạy Awake — bootstrap chủ động.</summary>
    public void EnsureInit()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
    }

    private Character GetPlayerCharacter()
    {
        if (playerCharacter == null)
        {
            var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) playerCharacter = pc.GetComponent<Character>();
        }
        return playerCharacter;
    }

    public void GrantPoints(int amount)
    {
        if (amount <= 0) return;
        SkillPoints += amount;
        OnPointsChanged?.Invoke(SkillPoints);
    }

    public bool IsUnlocked(string nodeId) => unlocked.Contains(nodeId);

    public static SkillNodeDef FindNode(string nodeId)
    {
        foreach (var n in ALL_NODES) if (n.id == nodeId) return n;
        return null;
    }

    public static string PrereqId(string nodeId)
    {
        var n = FindNode(nodeId);
        if (n == null || n.tier <= 0) return null;
        if (n.tier == 1) return "root"; // mọi nhánh đều mở từ gốc
        foreach (var other in ALL_NODES)
            if (other.branch == n.branch && other.tier == n.tier - 1) return other.id;
        return null;
    }

    public enum UnlockResult { Ok, NoPoints, LockedPrereq, LevelTooLow, AlreadyUnlocked, UnknownNode }

    public UnlockResult CanUnlock(string nodeId, out string reason)
    {
        reason = "";
        var n = FindNode(nodeId);
        if (n == null) return UnlockResult.UnknownNode;
        if (unlocked.Contains(nodeId)) { reason = "Đã mở"; return UnlockResult.AlreadyUnlocked; }
        var prereq = PrereqId(nodeId);
        if (prereq != null && !unlocked.Contains(prereq))
        {
            reason = "Cần mở nút trước";
            return UnlockResult.LockedPrereq;
        }
        if (LevelSystem.Level < n.requiredLevel)
        {
            reason = $"Cần level {n.requiredLevel}";
            return UnlockResult.LevelTooLow;
        }
        bool free = nodeId == "root"; // gốc mở miễn phí
        if (!free && SkillPoints < 1) { reason = "Không đủ điểm"; return UnlockResult.NoPoints; }
        return UnlockResult.Ok;
    }

    public bool Unlock(string nodeId, bool bypassCost = false)
    {
        if (unlocked.Contains(nodeId)) return false;
        var n = FindNode(nodeId);
        if (n == null) return false;

        if (!bypassCost)
        {
            var r = CanUnlock(nodeId, out var reason);
            if (r != UnlockResult.Ok)
            {
                Debug.Log($"🌳 Không mở được [{nodeId}]: {reason}");
                return false;
            }
            if (nodeId != "root")
            {
                SkillPoints--;
                OnPointsChanged?.Invoke(SkillPoints);
            }
        }
        else
        {
            // Nếu dùng cuộn bí kíp: tự động mở nút root (gốc) nếu chưa có
            if (!unlocked.Contains("root")) unlocked.Add("root");
        }

        unlocked.Add(nodeId);
        ApplyNode(nodeId);
        Debug.Log($"🌳 Đã mở nút: {n.name} (còn {SkillPoints} điểm)");
        OnNodeUnlocked?.Invoke(nodeId);
        return true;
    }

    /// <summary>Áp hiệu ứng nhìn thấy được ngay lúc mở (passive); nút hành vi để cờ.</summary>
    private void ApplyNode(string nodeId)
    {
        var character = GetPlayerCharacter();
        switch (nodeId)
        {
            case "than_1": // +25 HP
                if (character != null && character.health != null)
                {
                    character.health.maxValue += 25f;
                    character.health.currentValue += 25f;
                }
                break;
            case "than_2a": // +1.5 dmg
                if (character != null) character.AttackPower += 1.5f;
                break;
            case "than_2b": // +5% crit
                if (character != null) character.CriticalChance = Mathf.Clamp01(character.CriticalChance + 0.05f);
                break;
            // gio_2/gio_3/khong_*/lua_*/than_3a/than_3b/than_4: cờ hành vi — hệ thống tương ứng đọc IsUnlocked
        }
    }

    // === Save/load ===
    public (int points, List<string> nodes) ExportSave() => (SkillPoints, new List<string>(unlocked));

    public void ImportSave(int points, List<string> nodes)
    {
        SkillPoints = Mathf.Max(0, points);
        unlocked.Clear();
        if (nodes != null)
        {
            foreach (var id in nodes) if (FindNode(id) != null) unlocked.Add(id);
            foreach (var id in unlocked) ApplyNode(id); // áp lại passive sau load
        }
        OnPointsChanged?.Invoke(SkillPoints);
    }
}

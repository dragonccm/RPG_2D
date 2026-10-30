using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S4 (GAME_MASTER_PLAN 10, 6.2): quest + level/XP tối giản.
/// - QuestDef khai báo trong code (MVP); flag + counter lưu vào SaveSystem qua SaveEntry.
/// - LevelSystem: XP curve 100×level, mỗi level-up +1 skill point (dùng cho SkillTreeManager).
/// </summary>
public enum QuestState { Locked = 0, Active = 1, Complete = 2, Done = 3 }

[Serializable]
public class QuestObjective
{
    public string id;
    public string label;
    public int required = 1;
}

[Serializable]
public class QuestDef
{
    public string id;
    public string title;
    public string desc;
    public QuestObjective[] objectives;
    public int rewardXp;
    public int rewardSkillPoints;
}

[Serializable]
public class QuestSaveEntry
{
    public string questId;
    public int state; // QuestState
    public string counters; // "objId:count,objId:count"
}

public class QuestManager : MonoBehaviour
{
    private static QuestManager _instance;
    public static QuestManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<QuestManager>();
                if (_instance != null) _instance.EnsureInit();
            }
            return _instance;
        }
    }

    public static readonly QuestDef[] ALL_QUESTS =
    {
        new QuestDef
        {
            id = "q1", title = "Gươm của người canh giữ",
            desc = "Gặp Ông Tễ trong làng, rồi luyện trọn 1 combo 3 nhịp trong sân.",
            objectives = new[]
            {
                new QuestObjective { id = "talk_ongte", label = "Gặp Ông Tễ", required = 1 },
                new QuestObjective { id = "q1_combo", label = "Luyện 1 combo trọn 3 nhịp", required = 1 },
            },
            rewardXp = 50,
            rewardSkillPoints = 1,
        },
        new QuestDef
        {
            id = "q2", title = "Bóng trong rừng",
            desc = "Dõi theo dấu vết vào Rừng Thảm Vàng: diệt 3 kẻ địch và hạ Thợ Săn Bóng.",
            objectives = new[]
            {
                new QuestObjective { id = "q2_kill", label = "Diệt kẻ địch trong rừng", required = 3 },
                new QuestObjective { id = "q2_miniboss", label = "Hạ Thợ Săn Bóng", required = 1 },
            },
            rewardXp = 120,
            rewardSkillPoints = 1,
        },
        new QuestDef
        {
            id = "q3", title = "Phong ấn Tháp Cổ",
            desc = "Giải câu đố công tắc đá tại đầm sương và thu thập Chìa Khóa Tháp Cổ.",
            objectives = new[]
            {
                new QuestObjective { id = "q3_switch", label = "Kích hoạt công tắc cổ", required = 1 },
                new QuestObjective { id = "q3_key", label = "Thu thập Chìa Khóa Tháp", required = 1 },
            },
            rewardXp = 150,
            rewardSkillPoints = 1,
        },
        new QuestDef
        {
            id = "q4", title = "Đỉnh Tháp Trăng Đỏ",
            desc = "Tiến vào đỉnh tháp đối mặt với Pháp sư Agis và kết thúc đêm trăng đỏ.",
            objectives = new[]
            {
                new QuestObjective { id = "q4_agis", label = "Đánh bại Pháp sư Agis", required = 1 },
            },
            rewardXp = 300,
            rewardSkillPoints = 2,
        },
    };

    private readonly Dictionary<string, QuestState> states = new Dictionary<string, QuestState>();
    private readonly Dictionary<string, Dictionary<string, int>> counters =
        new Dictionary<string, Dictionary<string, int>>();

    public event Action<string> OnQuestStarted;
    public event Action<string> OnQuestCompleted;

    private void Awake() => EnsureInit();

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        EnemySpawner.EnemySpawned -= OnEnemySpawned;
    }

    /// <summary>EditMode test AddComponent không chạy Awake — bootstrap chủ động.</summary>
    public void EnsureInit()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) { Destroy(gameObject); return; }
        foreach (var q in ALL_QUESTS) if (!states.ContainsKey(q.id)) states[q.id] = QuestState.Locked;
        EnemySpawner.EnemySpawned -= OnEnemySpawned; // tránh đăng ký 2 lần
        EnemySpawner.EnemySpawned += OnEnemySpawned;
        HookExistingEnemies(); // địch đặt sẵn scene (mini-boss, nhóm đầu) cũng tính Q2
    }

    /// <summary>Re-hook sau khi mọi object đã activate (Awake có thể chạy trước boss/enemy xong).</summary>
    private void Start() => HookExistingEnemies();

    /// <summary>S4 (Q2): enemy spawn trong rừng → khi chết, ghi tiến độ quest.</summary>
    private void OnEnemySpawned(GameObject enemyGo)
    {
        if (enemyGo == null) return;
        SubscribeDeath(enemyGo);
    }

    private readonly HashSet<UnityEngine.Object> hookedEnemies = new HashSet<UnityEngine.Object>();

    /// <summary>Hook cả địch đặt sẵn trong scene (mini-boss, nhóm ban đầu) — chống hook 2 lần.</summary>
    private void HookExistingEnemies()
    {
        foreach (var c in UnityEngine.Object.FindObjectsByType<Character>(UnityEngine.FindObjectsSortMode.None))
            SubscribeDeath(c.gameObject);
    }

    private void SubscribeDeath(GameObject enemyGo)
    {
        var character = enemyGo.GetComponent<Character>();
        // loại trừ player (có PlayerController) — phần còn lại là địch (boss có thể không có Enemy comp)
        if (character == null || enemyGo.GetComponent<PlayerController>() != null) return;
        if (!hookedEnemies.Add(character)) return; // chống hook 2 lần
        bool isMiniboss = enemyGo.GetComponent<EliteBuff>() != null;
        character.OnDeath += () =>
        {
            if (isMiniboss) ReportProgress("q2", "q2_miniboss");
            else ReportProgress("q2", "q2_kill");
        };
    }

    public QuestState GetState(string questId) =>
        states.TryGetValue(questId, out var s) ? s : QuestState.Locked;

    public void StartQuest(string questId)
    {
        if (GetState(questId) != QuestState.Locked) return;
        states[questId] = QuestState.Active;
        counters[questId] = new Dictionary<string, int>();
        WorldFlags.Set($"quest:{questId}:started"); // thế giới đổi theo tiến trình (FlagAmbience, FlagGate)
        Debug.Log($"📜 Quest bắt đầu: {questId}");
        OnQuestStarted?.Invoke(questId);
    }

    /// <summary>Ghi tiến độ 1 mục tiêu; tự chuyển Active → Complete khi đủ hết.</summary>
    public void ReportProgress(string questId, string objectiveId, int amount = 1)
    {
        var def = FindDef(questId);
        if (def == null || GetState(questId) != QuestState.Active) return;
        if (!counters.TryGetValue(questId, out var c))
        {
            c = new Dictionary<string, int>();
            counters[questId] = c;
        }
        c.TryGetValue(objectiveId, out var cur);
        c[objectiveId] = cur + amount;

        foreach (var obj in def.objectives)
        {
            c.TryGetValue(obj.id, out var have);
            if (have < obj.required) return; // chưa đủ
        }
        states[questId] = QuestState.Complete;
        Debug.Log($"📜 Quest hoàn thành — tự nhận thưởng: {questId}");
        ClaimReward(questId); // tự nhận thưởng ngay khi đủ mục tiêu (MVP)
    }

    /// <summary>Nhận thưởng: Complete → Done, cộng XP + skill point.</summary>
    public bool ClaimReward(string questId)
    {
        if (GetState(questId) != QuestState.Complete) return false;
        var def = FindDef(questId);
        states[questId] = QuestState.Done;
        WorldFlags.Set($"quest:{questId}:done");
        if (def != null)
        {
            LevelSystem.AddXP(def.rewardXp);
            if (def.rewardSkillPoints > 0 && SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.GrantPoints(def.rewardSkillPoints);
        }
        Debug.Log($"📜 Đã nhận thưởng quest: {questId}");
        OnQuestCompleted?.Invoke(questId);
        if (questId == "q2") StartQuest("q3");
        else if (questId == "q3") StartQuest("q4");
        return true;
    }

    public int GetCounter(string questId, string objectiveId)
    {
        return counters.TryGetValue(questId, out var c) && c.TryGetValue(objectiveId, out var v) ? v : 0;
    }

    public static QuestDef FindDef(string questId)
    {
        foreach (var q in ALL_QUESTS) if (q.id == questId) return q;
        return null;
    }

    // === Save/load (SaveSystem gọi) ===
    public List<QuestSaveEntry> ExportSave()
    {
        var list = new List<QuestSaveEntry>();
        foreach (var q in ALL_QUESTS)
        {
            if (GetState(q.id) == QuestState.Locked) continue;
            var sb = new System.Text.StringBuilder();
            if (counters.TryGetValue(q.id, out var c))
                foreach (var kv in c) sb.Append(kv.Key).Append(':').Append(kv.Value).Append(',');
            list.Add(new QuestSaveEntry
            {
                questId = q.id,
                state = (int)GetState(q.id),
                counters = sb.ToString().TrimEnd(','),
            });
        }
        return list;
    }

    public void ImportSave(List<QuestSaveEntry> saved)
    {
        if (saved == null) return;
        foreach (var e in saved)
        {
            states[e.questId] = (QuestState)e.state;
            var c = new Dictionary<string, int>();
            if (!string.IsNullOrEmpty(e.counters))
                foreach (var part in e.counters.Split(','))
                {
                    var kv = part.Split(':');
                    if (kv.Length == 2 && int.TryParse(kv[1], out var v)) c[kv[0]] = v;
                }
            counters[e.questId] = c;
        }
    }
}

/// <summary>S4 (plan 6.2): XP/level tối giản — 100 XP × level để lên tiếp.</summary>
public static class LevelSystem
{
    public static event Action<int> OnLevelUp;

    public static int Level { get; private set; } = 1;
    public static int Xp { get; private set; }
    public static int XpToNext => 100 * Level;

    public static void AddXP(int amount)
    {
        if (amount <= 0) return;
        Xp += amount;
        while (Xp >= 100 * Level)
        {
            Xp -= 100 * Level;
            Level++;
            if (SkillTreeManager.Instance != null)
                SkillTreeManager.Instance.GrantPoints(1);
            Debug.Log($"⭐ Level lên {Level} (+1 skill point)");
            OnLevelUp?.Invoke(Level);
        }
    }

    public static void Import(int level, int xp)
    {
        Level = Mathf.Max(1, level);
        Xp = Mathf.Max(0, xp);
    }

    public static void ResetForTest()
    {
        Level = 1; Xp = 0;
    }
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Qu?n lý k? n?ng cho k? ??ch. Cho phép g?n nhi?u SkillModule (dùng chung v?i player) và t? ??ng thi tri?n k? n?ng khi t?n công.
/// G?n script này lên prefab k? ??ch ?? c?u hình k? n?ng linh ho?t.
/// </summary>
public class EnemySkillManager : MonoBehaviour
{
    [Header("Danh sách Kỹ năng (SkillModule)")]
    public List<SkillModule> skillModules = new List<SkillModule>(); // Kéo th? các ScriptableObject k? n?ng vào ?ây

    [Header("Tùy chỉnh sử dụng kỹ năng")]
    [Tooltip("Nếu true, kẻ địch sẽ ran dom mà dùng kỹ năng")]
    public bool useRandomSkill = false;

    private int lastSkillIndex = -1; // L?u l?i index k? n?ng ?ã dùng g?n nh?t
    private List<ISkillExecutor> skillExecutors = new List<ISkillExecutor>(); // Danh sách executor t??ng ?ng
    private Enemy enemy; // Tham chi?u ??n Enemy

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        // Kh?i t?o executor cho t?ng skill module
        skillExecutors.Clear();
        foreach (var module in skillModules)
        {
            if (module != null)
                skillExecutors.Add(module.CreateExecutor());
        }
    }

    /// <summary>
    /// G?i hàm này khi enemy mu?n s? d?ng k? n?ng (ví d? trong AttackState)
    /// </summary>
    public void UseSkill()
    {
        Debug.Log($"[EnemySkillManager] Bắt đầu xử lý UseSkill trên {gameObject.name}");
        if (skillExecutors.Count == 0)
        {
            Debug.LogWarning($"[EnemySkillManager] Không có kỹ năng nào để dùng cho {gameObject.name}");
            return;
        }
        if (enemy == null)
        {
            Debug.LogError($"[EnemySkillManager] Thiếu component Enemy trên {gameObject.name}");
            return;
        }
        var aiController = enemy.GetComponent<EnemyAIController>();
        if (aiController == null)
        {
            Debug.LogError($"[EnemySkillManager] Thiếu EnemyAIController trên {gameObject.name}");
            return;
        }
        if (aiController.playerTarget == null)
        {
            Debug.LogWarning($"[EnemySkillManager] Không có playerTarget cho {gameObject.name}");
            return;
        }
        Transform target = aiController.playerTarget;
        int skillIndex = 0;
        if (useRandomSkill)
        {
            skillIndex = Random.Range(0, skillExecutors.Count);
            Debug.Log($"[EnemySkillManager] Chọn kỹ năng ngẫu nhiên: index={skillIndex}, tên={skillModules[skillIndex]?.skillName}");
        }
        else
        {
            skillIndex = (lastSkillIndex + 1) % skillExecutors.Count;
            lastSkillIndex = skillIndex;
            Debug.Log($"[EnemySkillManager] Chọn kỹ năng tuần tự: index={skillIndex}, tên={skillModules[skillIndex]?.skillName}");
        }
        var executor = skillExecutors[skillIndex];
        var character = enemy.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError($"[EnemySkillManager] Thiếu component Character trên {gameObject.name}");
            return;
        }
        if (executor.CanExecute(character))
        {
            Debug.Log($"[EnemySkillManager] Đang thực thi kỹ năng '{skillModules[skillIndex]?.skillName}' lên mục tiêu {target.name} tại vị trí {target.position}");
            executor.Execute(character, target.position);
        }
        else
        {
            Debug.LogWarning($"[EnemySkillManager] Kỹ năng '{skillModules[skillIndex]?.skillName}' không thể thực thi bởi {gameObject.name}");
        }
    }

    /// <summary>
    /// Ki?m tra có th? dùng k? n?ng (ví d?: cooldown, target h?p l?)
    /// </summary>
    public bool CanUseSkill()
    {
        return skillExecutors.Count > 0;
    }

    /// <summary>
    /// Thêm k? n?ng m?i cho enemy (có th? g?i runtime)
    /// </summary>
    public void AddSkill(SkillModule module)
    {
        if (module != null && !skillModules.Contains(module))
        {
            skillModules.Add(module);
            skillExecutors.Add(module.CreateExecutor());
        }
    }

    /// <summary>
    /// Xóa k? n?ng kh?i enemy
    /// </summary>
    public void RemoveSkill(SkillModule module)
    {
        int idx = skillModules.IndexOf(module);
        if (idx >= 0)
        {
            skillModules.RemoveAt(idx);
            skillExecutors.RemoveAt(idx);
        }
    }
}

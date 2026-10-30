using System;
using System.Collections.Generic;

/// <summary>
/// Sổ kỹ năng của người chơi (CORE_PROJECT_GUIDELINES 6.5): nguồn sự thật duy nhất cho "đã học kỹ năng nào".
/// Nhân vật khởi đầu chỉ có đòn chém cơ bản; mọi kỹ năng (SkillModule) chỉ dùng được sau khi Learn() —
/// từ cuộn bí kíp (InventoryManager.UseItem), phần thưởng quest hoặc boss. Lưu trong SaveData.learnedSkills.
/// Cây kỹ năng KHÔNG bán kỹ năng mới — chỉ cường hoá (SkillTreeManager).
/// </summary>
public static class SkillBook
{
    private static readonly List<string> order = new List<string>();
    private static readonly HashSet<string> learned = new HashSet<string>();

    /// <summary>Phát khi học 1 kỹ năng (tham số = skillId) hoặc khi nạp save (null).</summary>
    public static event Action<string> Changed;

    public static IReadOnlyList<string> All => order;
    public static bool Has(string skillId) => !string.IsNullOrEmpty(skillId) && learned.Contains(skillId);
    public static bool Has(SkillModule skill) => skill != null && Has(skill.Id);

    /// <summary>Học kỹ năng. Trả về false nếu đã biết hoặc id rỗng.</summary>
    public static bool Learn(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || !learned.Add(skillId)) return false;
        order.Add(skillId);
        Changed?.Invoke(skillId);
        return true;
    }

    public static bool Learn(SkillModule skill) => skill != null && Learn(skill.Id);

    public static List<string> Export() => new List<string>(order);

    public static void Import(IEnumerable<string> saved)
    {
        order.Clear();
        learned.Clear();
        if (saved != null)
            foreach (var id in saved)
                if (!string.IsNullOrEmpty(id) && learned.Add(id)) order.Add(id);
        Changed?.Invoke(null);
    }

    public static void ResetForTest()
    {
        order.Clear();
        learned.Clear();
        Changed = null;
    }
}

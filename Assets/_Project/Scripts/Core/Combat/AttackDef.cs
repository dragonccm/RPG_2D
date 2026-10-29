using UnityEngine;

public enum AttackTeam { Player, Enemy, Boss }
public enum AttackShape
{
    /// <summary>Vòng tròn quét: tâm = điểm trước mặt (reach) hoặc chính chủ thể (centerOnOwner) / điểm chỉ định.</summary>
    Circle,
    /// <summary>Hình chữ nhật dài `reach`, rộng `2×radius`, xuất phát từ chủ thể theo hướng ngắm (đâm, húc).</summary>
    Line,
    /// <summary>Bắn đạn bay thẳng; `radius` là bán kính va chạm.</summary>
    Projectile
}
public enum HitFeedback { None, Light, Heavy }
public enum ProjectileStyle { Orb, Dart }

/// <summary>
/// Một đòn đánh dạng dữ liệu (CORE_PROJECT_GUIDELINES 6.2) — dùng chung cho Player, quái và boss.
/// AttackRunner chạy 3 pha: Windup (báo hiệu) → Active (gây sát thương) → Recovery (khựng, có thể nối đòn).
/// AI/boss chỉ CHỌN AttackDef; không tự viết coroutine đánh riêng. Sát thương = AttackPower của chủ thể × damageMultiplier.
/// </summary>
[CreateAssetMenu(fileName = "AttackDef", menuName = "RPG/Attack Def")]
public class AttackDef : ScriptableObject
{
    [Header("Định danh")]
    public string attackId;
    public AttackTeam team = AttackTeam.Enemy;
    public AttackShape shape = AttackShape.Circle;

    [Header("3 pha (giây)")]
    [Tooltip("Chuẩn bị / báo hiệu. Quái ≥ 0.3s, boss ≥ 0.4s, boss diện rộng ≥ 0.8s")]
    public float windup = 0.4f;
    [Tooltip("Thời gian gây sát thương (quét 1 lần ở đầu pha)")]
    public float active = 0.1f;
    [Tooltip("Khựng sau đòn — cửa sổ nối đòn / hủy bằng Dash")]
    public float recovery = 0.4f;
    [Tooltip("Nghỉ thêm sau recovery trước khi đánh lại (AI)")]
    public float cooldown = 0.6f;

    [Header("Hình dạng & tầm")]
    public float reach = 1f;
    public float radius = 0.8f;
    [Tooltip("Tâm vùng trúng ở chính chủ thể (đập đất, AoE quanh người)")]
    public bool centerOnOwner;

    [Header("Sát thương")]
    public float damageMultiplier = 1f;
    public float knockback = 0f;
    public float stunSeconds = 0f;
    public bool canCrit = false;

    [Header("Chuyển động")]
    [Tooltip("Lao tới trước bao xa trong pha Active (húc, đâm)")]
    public float lungeDistance = 0f;

    [Header("Báo hiệu & phản hồi")]
    public bool telegraph = true;
    public Color telegraphColor = new Color(1f, 0.25f, 0.2f, 0.75f);
    public HitFeedback feedback = HitFeedback.None;
    public Color slashColor = Color.white;
    public float slashSize = 1f;
    public string animTrigger = "Attack";
    [Tooltip("Vị trí nhịp trong chuỗi — truyền vào Animator (ComboIndex)")]
    public int comboIndex = 0;

    [Header("Đạn (shape = Projectile)")]
    public float projectileSpeed = 8f;
    public float projectileRange = 8f;
    public ProjectileStyle projectileStyle = ProjectileStyle.Orb;
    public Color projectileColor = new Color(1f, 0.55f, 0.15f);
    public GameObject projectilePrefab;

    [Header("Chuỗi đòn")]
    public AttackDef next;
    [Tooltip("Sau khi hết Recovery, còn bao lâu thì vẫn nối được đòn kế (giây)")]
    public float chainWindow = 0.35f;
    [Tooltip("Từ lúc nào của Recovery (0–1) được hủy để vào đòn kế")]
    [Range(0f, 1f)] public float cancelFrom = 0.4f;

    public float Total => windup + active + recovery;

    /// <summary>Khoảng cách AI nên bắt đầu đòn này.</summary>
    public float TriggerRange
    {
        get
        {
            switch (shape)
            {
                case AttackShape.Projectile: return projectileRange * 0.9f;
                case AttackShape.Line: return reach + radius * 0.3f + lungeDistance;
                default: return centerOnOwner ? radius : reach + radius * 0.5f;
            }
        }
    }

    private float MinWindup => team == AttackTeam.Player ? 0.05f : team == AttackTeam.Boss ? 0.4f : 0.3f;
    private bool IsAreaAttack => shape == AttackShape.Circle && radius >= 2.5f;

    /// <summary>Kiểm tra dữ liệu hợp lệ (test + editor). error = lý do đầu tiên không hợp lệ.</summary>
    public bool Validate(out string error)
    {
        error = null;
        string n = string.IsNullOrEmpty(attackId) ? name : attackId;
        if (string.IsNullOrEmpty(attackId)) { error = $"{n}: thiếu attackId"; return false; }
        if (windup < MinWindup) { error = $"{n}: windup {windup:0.00}s < tối thiểu {MinWindup:0.00}s của {team}"; return false; }
        if (team == AttackTeam.Boss && IsAreaAttack && windup < 0.8f) { error = $"{n}: boss diện rộng cần windup ≥ 0.8s"; return false; }
        if (team != AttackTeam.Player && !telegraph) { error = $"{n}: đòn của quái/boss phải có telegraph"; return false; }
        if (active <= 0f || active > 0.6f) { error = $"{n}: active phải trong (0, 0.6]s"; return false; }
        if (recovery < 0.05f) { error = $"{n}: recovery quá ngắn"; return false; }
        if (damageMultiplier <= 0f) { error = $"{n}: damageMultiplier phải > 0"; return false; }
        if (shape != AttackShape.Projectile && reach <= 0f && !centerOnOwner) { error = $"{n}: reach phải > 0"; return false; }
        if (radius <= 0f) { error = $"{n}: radius phải > 0"; return false; }
        if (shape == AttackShape.Projectile && (projectileSpeed <= 0f || projectileRange <= 0f)) { error = $"{n}: đạn thiếu speed/range"; return false; }
        if (!IsChainAcyclic(this, out int len)) { error = $"{n}: chuỗi nextAttack bị vòng"; return false; }
        for (var a = next; a != null && len-- > 0; a = a.next)
            if (a.team != team) { error = $"{n}: chuỗi lẫn team ({a.name})"; return false; }
        return true;
    }

    /// <summary>Chuỗi đòn không vòng lặp (vòng combo của player đóng bằng PlayerCombat, không bằng next).</summary>
    public static bool IsChainAcyclic(AttackDef head, out int length)
    {
        length = 0;
        var seen = new System.Collections.Generic.HashSet<AttackDef>();
        for (var a = head; a != null; a = a.next)
        {
            if (!seen.Add(a)) return false;
            length++;
            if (length > 16) return false;
        }
        return true;
    }
}

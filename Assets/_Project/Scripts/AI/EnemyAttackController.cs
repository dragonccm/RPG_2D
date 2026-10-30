using UnityEngine;

/// <summary>
/// Điều khiển tấn công của kẻ địch (Melee, Ranged, Elite). Đòn đánh là AttackDef chạy qua AttackRunner
/// (windup có telegraph → active → recovery) — CORE_PROJECT_GUIDELINES 6.2. AI chỉ gọi Attack(target).
/// attackRange (khoảng AI bắt đầu đánh) lấy từ AttackDef.TriggerRange khi có đòn.
/// </summary>
public class EnemyAttackController : MonoBehaviour
{
    [Header("Đòn đánh")]
    [Tooltip("Đòn thường của kẻ địch (dữ liệu AttackDef). Nhiều đòn: xâu bằng AttackDef.next.")]
    public AttackDef attack;

    [Header("Chế độ cũ (không có AttackDef)")]
    [Tooltip("Phạm vi tấn công — tự lấy từ AttackDef nếu có.")]
    public float attackRange = 2f;
    [Tooltip("Thời gian hồi chiêu giữa các đòn (chế độ cũ).")]
    public float attackCooldown = 1f;

    protected float lastAttackTime;
    private AttackRunner runner;

    public AttackRunner Runner
    {
        get
        {
            if (runner == null)
            {
                runner = GetComponent<AttackRunner>();
                if (runner == null) runner = gameObject.AddComponent<AttackRunner>();
            }
            return runner;
        }
    }

    /// <summary>Có đòn dạng dữ liệu → AttackState dùng controller này thay vì EnemySkillManager.</summary>
    public bool HasAttackDef => attack != null;

    // Property để truy cập attackRange từ bên ngoài (ví dụ: Gizmos, AttackState)
    public float AttackRange => attack != null ? attack.TriggerRange : attackRange;

    public bool IsAttacking => runner != null && runner.IsBusy;

    public virtual bool IsInAttackRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= AttackRange;
    }

    /// <summary>Thực hiện tấn công mục tiêu. AttackRunner lo cooldown/telegraph; đang đánh thì bỏ qua.</summary>
    public virtual void Attack(Transform target)
    {
        if (attack != null)
        {
            Vector2 aim = target != null ? (Vector2)(target.position - transform.position) : Vector2.right;
            Runner.TryStart(attack, aim);
            return;
        }

        // ── Chế độ cũ: chỉ cooldown + animation (prefab chưa có AttackDef) ──
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (target != null)
        {
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                float dx = target.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.01f) spriteRenderer.flipX = dx < 0f;
            }
        }
        var enemy = GetComponentInParent<Enemy>();
        if (enemy != null && enemy.EnemyAnimatorController != null) enemy.EnemyAnimatorController.PlayAttackAnimation();
        lastAttackTime = Time.time;
    }
}

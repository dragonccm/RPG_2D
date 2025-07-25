using UnityEngine;

/// <summary>
/// Điều khiển hành vi tấn công của kẻ địch. Có thể dùng cho mọi loại AI (Melee, Ranged, Boss).
/// Tích hợp với EnemyAIController để lấy target và kiểm soát cooldown, phạm vi tấn công.
/// </summary>
public class EnemyAttackController : MonoBehaviour
{
    // === Các thuộc tính cấu hình tấn công ===

    [Tooltip("Phạm vi tấn công của kẻ địch.")]
    public float attackRange = 2f;

    [Tooltip("Thời gian hồi chiêu giữa các đòn tấn công.")]
    public float attackCooldown = 1f;

    protected float lastAttackTime;

    // Property để truy cập attackRange từ bên ngoài (ví dụ: Gizmos)
    public float AttackRange => attackRange;

    // === Các phương thức chính ===

    /// <summary>
    /// Kiểm tra mục tiêu có trong phạm vi tấn công không.
    /// </summary>
    public virtual bool IsInAttackRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    /// <summary>
    /// Thực hiện tấn công mục tiêu (có kiểm tra cooldown). Có thể override để mở rộng logic.
    /// </summary>
    /// <param name="target">Mục tiêu cần tấn công.</param>
    public virtual void Attack(Transform target)
    {
        // Kiểm tra thời gian hồi chiêu
        if (Time.time < lastAttackTime + attackCooldown)
        {
            Debug.Log($"[{gameObject.name}] Attack on cooldown. Next attack in: {lastAttackTime + attackCooldown - Time.time:F2}s");
            return;
        }

        Debug.Log($"[{gameObject.name}] attacks {target.name}!");
        // Gọi animation tấn công nếu có EnemyAnimatorController
        var enemy = GetComponentInParent<Enemy>();
        if (enemy != null && enemy.EnemyAnimatorController != null)
        {
            enemy.EnemyAnimatorController.PlayAttackAnimation();
        }
        lastAttackTime = Time.time; // Cập nhật thời gian tấn công cuối cùng
    }
}

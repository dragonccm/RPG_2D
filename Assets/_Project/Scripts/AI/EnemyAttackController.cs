using UnityEngine;

/// <summary>
/// Điều khiển hành vi tấn công của kẻ địch.
/// </summary>
public class EnemyAttackController : MonoBehaviour
{
    // playerTarget không còn là public field để tránh gán nhầm từ Inspector.
    // Nó sẽ được lấy từ EnemyAIController.playerTarget.
    // private Transform _playerTarget; // Không cần thiết ở đây vì Attack() nhận target trực tiếp

    [Tooltip("Phạm vi tấn công của kẻ địch.")]
    public float attackRange = 2f;

    [Tooltip("Thời gian hồi chiêu giữa các đòn tấn công.")]
    public float attackCooldown = 1f;

    protected float lastAttackTime;

    // Property để truy cập attackRange từ bên ngoài (ví dụ: Gizmos)
    public float AttackRange => attackRange;

    /// <summary>
    /// Cập nhật mục tiêu người chơi cho bộ điều khiển tấn công.
    /// Lưu ý: Phương thức này có thể không cần thiết nếu Attack() luôn nhận target trực tiếp.
    /// </summary>
    // public void SetPlayerTarget(Transform newTarget)
    // {
    //     _playerTarget = newTarget;
    // }

    /// <summary>
    /// Kiểm tra player có trong phạm vi tấn công không.
    /// </summary>
    public virtual bool IsInAttackRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    /// <summary>
    /// Hành vi tấn công mục tiêu. Ghi đè ở các class con nếu cần.
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
        // Gây sát thương hoặc thực hiện hành động tấn công khác
        Character targetCharacter = target.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(10); // Ví dụ sát thương
            Debug.Log($"[{gameObject.name}] dealt 10 damage to {target.name}. Current HP: {targetCharacter.CurrentHealth:F2}");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Target {target.name} does not have a Character component to take damage.");
        }

        lastAttackTime = Time.time; // Cập nhật thời gian tấn công cuối cùng
    }
}

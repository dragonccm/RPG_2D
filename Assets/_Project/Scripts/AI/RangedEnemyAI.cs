using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho kẻ địch tầm xa.
/// </summary>
[DisallowMultipleComponent]
public class RangedEnemyAI : EnemyAIController
{
    public RepositionState repositionState;
    [Tooltip("Khoảng cách an toàn mà kẻ địch tầm xa muốn duy trì với mục tiêu.")]
    public float safeDistance = 5f;

    private Enemy enemy; // Cache tham chiếu đến Enemy component

    protected override void Awake()
    {
        base.Awake();
        enemy = GetComponent<Enemy>(); // Lấy tham chiếu khi Awake
        if (enemy == null)
        {
            Debug.LogError("RangedEnemyAI requires an Enemy component on the same GameObject.", this);
            enabled = false; // Tắt script nếu không có Enemy component
            return;
        }
        enemyType = EnemyType.Ranged; // Thiết lập loại kẻ địch là tầm xa

        repositionState = new RepositionState(this, stateMachine);
    }

    /// <summary>
    /// Kiểm tra xem mục tiêu có trong một phạm vi cụ thể không.
    /// </summary>
    /// <param name="target">Mục tiêu cần kiểm tra.</param>
    /// <param name="range">Phạm vi để kiểm tra.</param>
    /// <returns>True nếu mục tiêu trong phạm vi, ngược lại False.</returns>
    private bool IsTargetInSpecificRange(Transform target, float range)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= range;
    }

    public override void Alert(Transform target)
    {
        Debug.Log($"[RangedAI] Alerted to target: {target?.name}");
        // Nếu được alert, kiểm tra xem target có trong chaseRange không để bắt đầu truy đuổi.
        if (target != null && IsTargetInSpecificRange(target, enemy.chaseRange))
        {
            playerTarget = target; // Gán mục tiêu người chơi cho AI này
            ChangeState(chaseState); // Chuyển sang trạng thái truy đuổi
        }
    }

    // Phương thức GetPriorityTarget đã được loại bỏ ở đây vì Enemy.cs là nơi quản lý mục tiêu chính.


}

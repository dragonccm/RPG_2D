using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho kẻ địch tầm xa. Kế thừa EnemyAIController, chỉ cần gắn script này lên prefab là đủ cho AI tầm xa.
/// Tích hợp logic giữ khoảng cách an toàn, chuyển state khi được alert.
/// </summary>
[DisallowMultipleComponent]
public class RangedEnemyAI : EnemyAIController
{
    public RepositionState repositionState;
    [Tooltip("Khoảng cách an toàn mà kẻ địch tầm xa muốn duy trì với mục tiêu.")]
    public float safeDistance = 5f;

    private Enemy enemy; // Cache tham chiếu đến Enemy component

    // === Khởi tạo, cache component, thiết lập loại AI ===
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
    /// Kiểm tra mục tiêu có trong phạm vi chỉ định không (dùng cho alert, chase, attack).
    /// </summary>
    private bool IsTargetInSpecificRange(Transform target, float range)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= range;
    }

    /// <summary>
    /// Xử lý khi AI được "báo động" về một mục tiêu (ví dụ: bị phát hiện, bị tấn công).
    /// </summary>
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
    // Không override GetPriorityTarget: sử dụng logic mặc định của Enemy.cs
}

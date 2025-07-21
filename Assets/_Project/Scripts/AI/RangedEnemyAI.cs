using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho kẻ địch tầm xa.
/// </summary>
[DisallowMultipleComponent]
public class RangedEnemyAI : EnemyAIController
{
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

    protected override void Update()
    {
        base.Update(); // Gọi Update của lớp cha để thực thi trạng thái hiện tại

        // Logic giữ khoảng cách an toàn cho kẻ địch tầm xa
        if (playerTarget != null)
        {
            float dist = Vector3.Distance(transform.position, playerTarget.position);
            var moveCtrl = GetComponent<EnemyMovementController>();

            if (moveCtrl != null)
            {
                if (dist < safeDistance)
                {
                    // Lùi lại nếu quá gần mục tiêu
                    Vector3 dir = (transform.position - playerTarget.position).normalized;
                    moveCtrl.MoveTo(transform.position + dir * safeDistance);
                }
                else if (dist > safeDistance + 1f) // Nếu quá xa khoảng cách an toàn, di chuyển lại gần
                {
                    moveCtrl.MoveTo(playerTarget.position);
                }
                else
                {
                    // Nếu đang ở khoảng cách an toàn, dừng lại để tấn công
                    moveCtrl.Stop();
                }
            }
        }
    }

    private void OnEnable()
    {
        // Dòng 'enabled = true;' đã được loại bỏ vì nó không cần thiết.
        // Script tự động được kích hoạt khi OnEnable được gọi.
    }
}

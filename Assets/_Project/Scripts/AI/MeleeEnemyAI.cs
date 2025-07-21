using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho kẻ địch cận chiến. Target linh hoạt: ưu tiên player trong vùng, nếu có group thì ưu tiên groupTarget, có thể mở rộng cho các mục tiêu khác.
/// </summary>
[DisallowMultipleComponent] // Đảm bảo chỉ có một instance của script này trên GameObject
public class MeleeEnemyAI : EnemyAIController
{
    private Enemy enemy; // Cache tham chiếu đến Enemy component để truy cập detectionRange và chaseRange

    // Phương thức Reset được gọi khi script được thêm vào GameObject lần đầu hoặc khi Reset component trong Editor.
    void Reset()
    {
        enemyType = EnemyType.Melee; // Thiết lập loại kẻ địch là cận chiến
    }

    protected override void Awake()
    {
        base.Awake(); // Gọi Awake của lớp cha (EnemyAIController) để khởi tạo StateMachine
        enemy = GetComponent<Enemy>(); // Lấy tham chiếu đến Enemy component
        if (enemy == null)
        {
            Debug.LogError("MeleeEnemyAI requires an Enemy component on the same GameObject.", this);
            enabled = false; // Tắt script nếu không tìm thấy Enemy component để tránh lỗi
            return;
        }
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

    /// <summary>
    /// Xử lý khi AI được "báo động" về một mục tiêu.
    /// </summary>
    /// <param name="target">Mục tiêu được báo động.</param>
    public override void Alert(Transform target)
    {
        Debug.Log($"[MeleeAI] Alerted to target: {target?.name}");
        // Nếu được alert, kiểm tra xem target có trong chaseRange không để bắt đầu truy đuổi.
        // Alert thường dùng để thông báo về một mục tiêu mới, không nhất thiết phải trong detectionRange.
        if (target != null && IsTargetInSpecificRange(target, enemy.chaseRange))
        {
            playerTarget = target; // Gán mục tiêu người chơi cho AI này
            ChangeState(chaseState); // Chuyển sang trạng thái truy đuổi
        }
    }

    /// <summary>
    /// Xác định mục tiêu ưu tiên nhất cho AI này.
    /// </summary>
    /// <param name="availableTargets">Danh sách các mục tiêu có sẵn (thường là các player trong detectionRange).</param>
    /// <returns>Mục tiêu ưu tiên nhất.</returns>
    public override Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        // Nếu đã có playerTarget và nó vẫn trong tầm chaseRange, tiếp tục giữ target đó
        if (playerTarget != null && IsTargetInSpecificRange(playerTarget, enemy.chaseRange))
        {
            return playerTarget;
        }

        // 1. Ưu tiên groupTarget nếu có và trong vùng chaseRange
        if (group != null && group.groupTarget != null && IsTargetInSpecificRange(group.groupTarget, enemy.chaseRange))
        {
            return group.groupTarget;
        }

        // 2. Ưu tiên player gần nhất trong vùng detectionRange (để phát hiện mới)
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (var t in availableTargets)
        {
            // Chỉ xem xét player trong detectionRange để phát hiện mục tiêu mới
            if (t != null && t.CompareTag("Player") && IsTargetInSpecificRange(t, enemy.detectionRange))
            {
                float dist = Vector3.Distance(transform.position, t.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPlayer = t;
                }
            }
        }
        // 3. Có thể mở rộng: ưu tiên các mục tiêu khác (ví dụ: bait, trap, v.v.)
        return closestPlayer;
    }

    protected override void Update()
    {
        if (enemy == null) return; // Đảm bảo Enemy component tồn tại trước khi xử lý

        // Logic giữ target: Nếu đang có playerTarget và nó ra khỏi chaseRange thì bỏ target
        if (playerTarget != null && !IsTargetInSpecificRange(playerTarget, enemy.chaseRange))
        {
            Debug.Log("[MeleeAI] Player target out of chase range, stopping chase.");
            playerTarget = null; // Bỏ mục tiêu
            ChangeState(idleState); // Chuyển về trạng thái Idle hoặc Patrol
        }
        // Logic cho groupTarget: Nếu groupTarget có và nó ra khỏi chaseRange thì cũng bỏ nó như target chính
        else if (group != null && group.groupTarget != null && !IsTargetInSpecificRange(group.groupTarget, enemy.chaseRange))
        {
            if (playerTarget == group.groupTarget) // Nếu groupTarget đang là target chính của AI này
            {
                Debug.Log("[MeleeAI] Group target out of chase range, stopping chase.");
                playerTarget = null;
                ChangeState(idleState);
            }
        }

        base.Update(); // Gọi Update của lớp cha để thực thi trạng thái hiện tại
    }

    // OnEnable được gọi mỗi khi GameObject hoặc script được bật.
    private void OnEnable()
    {
        // Dòng này đã được loại bỏ vì nó không cần thiết.
        // Script tự động được kích hoạt khi OnEnable được gọi.
    }
}

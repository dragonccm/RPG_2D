using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho kẻ địch cận chiến. Kế thừa EnemyAIController, chỉ cần gắn script này lên prefab là đủ cho AI cận chiến.
/// Tích hợp sẵn logic ưu tiên target máu thấp nhất, chuyển state khi được alert.
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

    // === Khởi tạo, cache component, thiết lập loại AI ===
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
        Debug.Log($"[{gameObject.name}] MeleeEnemyAI Awake. Enemy component found.");
    }
    /// <summary>
    /// Kiểm tra mục tiêu có trong phạm vi chỉ định không (dùng cho alert, chase, attack).
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
    /// Xử lý khi AI được "báo động" về một mục tiêu (ví dụ: bị phát hiện, bị tấn công).
    /// </summary>
    /// <param name="target">Mục tiêu được báo động.</param>
    public override void Alert(Transform target)
    {
        Debug.Log($"[MeleeAI] Alerted to target: {target?.name}");
        // Nếu được alert, kiểm tra xem target có trong chaseRange không để bắt đầu truy đuổi.
        // Alert thường dùng để thông báo về một mục tiêu mới, không nhất thiết phải trong detectionRange.
        if (target != null && enemy != null && IsTargetInSpecificRange(target, enemy.chaseRange))
        {
            playerTarget = target; // Gán mục tiêu người chơi cho AI này
            ChangeState(chaseState); // Chuyển sang trạng thái truy đuổi
            Debug.Log($"[MeleeAI] Alerted and changing to ChaseState with target: {playerTarget.name}");
        }
    }

    /// <summary>
    /// Ưu tiên target player máu thấp nhất trong vùng detection (override logic mặc định).
    /// </summary>
    public override Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        // CHANGED: Luôn ưu tiên player máu thấp nhất trong vùng detection
        Transform priority = null;
        float minHP = float.MaxValue;
        foreach (var t in availableTargets)
        {
            if (t != null && t.CompareTag("Player"))
            {
                var c = t.GetComponent<Character>();
                float hp = c != null ? c.CurrentHealth : 0f;
                if (hp < minHP)
                {
                    minHP = hp;
                    priority = t;
                }
            }
        }
        return priority;
    }


}

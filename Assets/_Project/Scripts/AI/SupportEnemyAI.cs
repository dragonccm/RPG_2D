using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AI cho kẻ địch hỗ trợ. Kế thừa EnemyAIController, chỉ cần gắn script này lên prefab là đủ cho AI support.
/// Tích hợp logic ưu tiên hỗ trợ đồng minh máu thấp nhất, hoặc phát hiện player gần nhất.
/// </summary>
[DisallowMultipleComponent]
public class SupportEnemyAI : EnemyAIController
{
    [Tooltip("Phạm vi hỗ trợ của kẻ địch hỗ trợ.")]
    public float supportRange = 10f; // Phạm vi hỗ trợ riêng cho Support AI

    private Enemy enemy; // Cache tham chiếu đến Enemy component

    // === Khởi tạo, cache component, thiết lập loại AI ===
    protected override void Awake()
    {
        base.Awake();
        enemy = GetComponent<Enemy>(); // Lấy tham chiếu khi Awake
        if (enemy == null)
        {
            Debug.LogError("SupportEnemyAI requires an Enemy component on the same GameObject.", this);
            enabled = false; // Tắt script nếu không có Enemy component
            return;
        }
        enemyType = EnemyType.Support; // Thiết lập loại kẻ địch là hỗ trợ
    }

    /// <summary>
    /// Kiểm tra mục tiêu có trong phạm vi hỗ trợ không (dùng cho alert, support).
    /// </summary>
    /// <param name="target">Mục tiêu cần kiểm tra.</param>
    /// <returns>True nếu mục tiêu trong phạm vi hỗ trợ, ngược lại False.</returns>
    private bool IsTargetInSupportRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= supportRange;
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
    /// Xử lý khi AI được "báo động" về một mục tiêu (đồng minh hoặc player).
    /// </summary>
    public override void Alert(Transform target)
    {
        Debug.Log($"[SupportAI] Alerted to target: {target?.name}");
        // Chỉ nhận target là đồng minh hoặc player trong vùng support
        if (target != null && (target.CompareTag("Enemy") || target.CompareTag("Player")))
        {
            if (IsTargetInSupportRange(target))
            {
                playerTarget = target; // Gán mục tiêu cho AI này
                ChangeState(chaseState); // Chuyển sang trạng thái truy đuổi để đến gần mục tiêu hỗ trợ
            }
        }
    }

    /// <summary>
    /// Ưu tiên hỗ trợ đồng minh máu thấp nhất, nếu không có thì chọn player gần nhất.
    /// </summary>
    public override Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        // Logic này sẽ được gọi bởi Enemy.cs để xác định target chung.

        // 1. Ưu tiên đồng minh máu thấp nhất trong vùng supportRange
        Transform allyToSupport = null;
        float minHP = float.MaxValue;
        foreach (var t in availableTargets)
        {
            // Chỉ xem xét đồng minh (có tag "Enemy") trong supportRange
            if (t != null && t.CompareTag("Enemy") && IsTargetInSupportRange(t))
            {
                var c = t.GetComponent<Character>(); // Giả định đồng minh cũng có Character component
                if (c != null && c.CurrentHealth < minHP)
                {
                    minHP = c.CurrentHealth;
                    allyToSupport = t;
                }
            }
        }
        if (allyToSupport != null) return allyToSupport;

        // 2. Nếu không có đồng minh cần hỗ trợ, xét player gần nhất trong vùng detectionRange (để phát hiện mới)
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
        return closestPlayer;
    }

    /// <summary>
    /// Update: Thực thi logic giữ vị trí hỗ trợ, di chuyển đến gần đồng minh hoặc player.
    /// </summary>
    protected override void Update()
    {
        base.Update(); // Gọi Update của lớp cha để thực thi trạng thái hiện tại

        // Logic giữ vị trí hỗ trợ (ví dụ: di chuyển đến gần đồng minh máu thấp nhất)
        if (playerTarget != null) // playerTarget ở đây có thể là đồng minh hoặc player
        {
            var moveCtrl = GetComponent<EnemyMovementController>();
            if (moveCtrl != null)
            {
                if (IsTargetInSupportRange(playerTarget))
                {
                    // Nếu mục tiêu (đồng minh/player) đã trong tầm hỗ trợ, dừng lại hoặc di chuyển ít
                    moveCtrl.Stop();
                    // TODO: Thực hiện hành động hỗ trợ (hồi máu, tạo lá chắn, buff, v.v.)
                    Debug.Log($"[SupportAI] Supporting {playerTarget.name}");
                }
                else
                {
                    // Di chuyển đến gần mục tiêu hỗ trợ
                    moveCtrl.MoveTo(playerTarget.position);
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

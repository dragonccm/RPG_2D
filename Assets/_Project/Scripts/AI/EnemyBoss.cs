using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Lớp điều khiển AI cho Boss, kế thừa từ Enemy để tùy chỉnh hành vi và ưu tiên mục tiêu.
/// </summary>
public class EnemyBoss : Enemy
{
    [Header("Boss Specific Settings")]
    [Tooltip("Phạm vi mà Boss muốn duy trì với mục tiêu để thực hiện các hành động đặc biệt.")]
    public float bossActionRange = 8f;
    [Tooltip("Khoảng cách tối thiểu Boss muốn giữ với mục tiêu.")]
    public float bossMinDistance = 3f;

    /// <summary>
    /// Ghi đè phương thức chọn ứng cử viên mục tiêu Player tốt nhất của lớp Enemy.
    /// Boss sẽ ưu tiên player có HP thấp nhất trong số các ứng cử viên.
    /// </summary>
    /// <param name="candidates">Danh sách các Transform ứng cử viên Player.</param>
    /// <returns>Transform của player có HP thấp nhất, hoặc null nếu không tìm thấy.</returns>
    protected override Transform EvaluatePlayerTargetCandidates(List<Transform> candidates)
    {
        // Kiểm tra nếu danh sách ứng cử viên rỗng hoặc null
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        Transform lowestHPPlayer = null;
        float lowestHP = float.MaxValue;

        // Duyệt qua tất cả các ứng cử viên để tìm player có HP thấp nhất
        foreach (var player in candidates)
        {
            // Đảm bảo player không null trước khi truy cập GetComponent
            if (player == null) continue;

            var character = player.GetComponent<Character>();
            // Nếu ứng cử viên có component Character và HP của họ thấp hơn HP thấp nhất hiện tại
            if (character != null)
            {
                if (character.CurrentHealth < lowestHP)
                {
                    lowestHP = character.CurrentHealth;
                    lowestHPPlayer = player;
                }
            }
        }

        // Nếu tìm thấy player có HP thấp nhất, ưu tiên nó
        if (lowestHPPlayer != null)
        {
            return lowestHPPlayer;
        }

        // Nếu không có player nào có Character component (hoặc tất cả đều full HP),
        // thì fallback về logic mặc định của lớp Enemy (chọn gần nhất).
        // Tuy nhiên, vì EvaluatePlayerTargetCandidates là một phương thức ảo,
        // việc gọi base.EvaluatePlayerTargetCandidates(candidates) sẽ chỉ gọi lại chính nó
        // nếu không có logic cụ thể khác.
        // Trong trường hợp này, nếu không tìm thấy player HP thấp nhất, chúng ta có thể
        // trả về null để Enemy.UpdateTarget() xử lý các ưu tiên khác (như groupTarget)
        // hoặc để nó không có mục tiêu.
        return null; // Trả về null để Enemy.UpdateTarget() có thể tiếp tục với các ưu tiên khác.
    }

    /// <summary>
    /// Update: Ghi đè để thêm logic Boss movement đặc biệt.
    /// </summary>
    protected override void Update()
    {
        base.Update(); // Gọi logic Enemy cơ bản
        
        // Boss logic đặc biệt - chỉ thực hiện khi có target và không trong patrol state
        if (target != null && IsValidTarget(target))
        {
            HandleBossMovement();
        }
    }

    /// <summary>
    /// Xử lý di chuyển đặc biệt của Boss để duy trì khoảng cách tối ưu.
    /// </summary>
    private void HandleBossMovement()
    {
        if (movementController == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Nếu mục tiêu quá xa phạm vi hành động của Boss, tiếp tục đuổi theo
        if (distanceToTarget > bossActionRange)
        {
            movementController.MoveTo(target.position);
        }
        // Nếu mục tiêu quá gần khoảng cách tối thiểu, lùi lại
        else if (distanceToTarget < bossMinDistance)
        {
            Vector3 directionAway = (transform.position - target.position).normalized;
            Vector3 retreatPosition = transform.position + directionAway * (bossMinDistance + 1f);
            movementController.MoveTo(retreatPosition);
        }
        // Nếu ở khoảng cách tối ưu, dừng di chuyển và tấn công
        else
        {
            movementController.Stop();
            HandleBossAttack();
        }
    }

    /// <summary>
    /// Logic tấn công hoặc sử dụng skill đặc biệt của Boss.
    /// Phương thức này sẽ gọi EnemyAttackController.Attack nếu có, hoặc thực hiện skill riêng của Boss.
    /// </summary>
    protected virtual void HandleBossAttack()
    {
        // Lấy tham chiếu đến EnemyAttackController
        var attackController = GetComponent<EnemyAttackController>();

        // Nếu có EnemyAttackController và có mục tiêu hợp lệ
        if (attackController != null && target != null)
        {
            // Sử dụng logic Attack của EnemyAttackController
            attackController.Attack(target);
        }
        else
        {
            // Nếu không có EnemyAttackController hoặc không có mục tiêu, Boss thực hiện skill đặc biệt
            // TODO: Thêm logic skill đặc biệt của Boss ở đây
        }
    }

    /// <summary>
    /// Vẽ các Gizmos trong Unity Editor để hình dung các phạm vi của Boss.
    /// </summary>
    protected override void OnDrawGizmosSelected()
    {
        // Gọi phương thức OnDrawGizmosSelected của lớp cơ sở Enemy để vẽ detectionRange và chaseRange
        base.OnDrawGizmosSelected();

        // Vẽ phạm vi hành động của Boss (bossActionRange)
        Gizmos.color = Color.magenta; // Màu hồng
        Gizmos.DrawWireSphere(transform.position, bossActionRange);

        // Vẽ khoảng cách tối thiểu Boss muốn giữ (bossMinDistance)
        Gizmos.color = new Color(1, 0, 1, 0.2f); // Màu hồng nhạt, trong suốt
        Gizmos.DrawWireSphere(transform.position, bossMinDistance);
    }
}

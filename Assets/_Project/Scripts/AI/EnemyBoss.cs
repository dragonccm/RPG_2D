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
    /// Ghi đè phương thức điều khiển di chuyển của lớp Enemy để tùy chỉnh hành vi của Boss.
    /// Boss sẽ di chuyển để duy trì khoảng cách tối ưu với mục tiêu, lùi lại nếu quá gần và tiến lên nếu quá xa.
    /// </summary>
    protected override void HandleMovement()
    {
        // Đảm bảo NavMeshAgent đã được gán
        if (agent == null)
        {
            Debug.LogWarning($"NavMeshAgent is not assigned to EnemyBoss: {gameObject.name}", this);
            return;
        }

        // Nếu có mục tiêu
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            // Debug.Log($"[EnemyBoss-HandleMovement] Distance to target {target.name}: {distanceToTarget:F2}m");

            // Nếu mục tiêu quá xa phạm vi hành động của Boss, tiếp tục đuổi theo
            if (distanceToTarget > bossActionRange)
            {
                agent.SetDestination(target.position);
                agent.isStopped = false;
                // Debug.Log($"[EnemyBoss-HandleMovement] Chasing target to {target.position}.");
            }
            // Nếu mục tiêu quá gần khoảng cách tối thiểu, lùi lại
            else if (distanceToTarget < bossMinDistance)
            {
                Vector3 directionAway = (transform.position - target.position).normalized;
                // Tính toán vị trí lùi lại, xa hơn một chút so với bossMinDistance
                Vector3 retreatPosition = transform.position + directionAway * (bossMinDistance + 1f);

                NavMeshHit hit;
                // Tìm một vị trí hợp lệ trên NavMesh để lùi lại
                if (NavMesh.SamplePosition(retreatPosition, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    agent.isStopped = false;
                    // Debug.Log($"[EnemyBoss-HandleMovement] Retreating to {hit.position}.");
                }
                else
                {
                    // Nếu không tìm được điểm lùi hợp lệ, dừng lại
                    agent.isStopped = true;
                    if (agent.hasPath) agent.ResetPath();
                    // Debug.Log($"[EnemyBoss-HandleMovement] Cannot find retreat position, stopping.");
                }
            }
            // Nếu mục tiêu trong khoảng bossMinDistance và bossActionRange (phạm vi hành động tối ưu)
            else
            {
                // Dừng di chuyển và thực hiện hành động tấn công/skill
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
                // Debug.Log($"[EnemyBoss-HandleMovement] In optimal action range. Stopping movement and attempting attack.");
                HandleBossAttack(); // Gọi phương thức xử lý tấn công/skill của Boss
            }
        }
        else // Nếu không có mục tiêu
        {
            // Boss đứng yên
            agent.isStopped = true;
            if (agent.hasPath) agent.ResetPath();
            // Debug.Log($"[EnemyBoss-HandleMovement] No target, stopping movement.");
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
            // Debug.Log($"[EnemyBoss-HandleBossAttack] Calling Attack on {target.name}.");
            attackController.Attack(target);
        }
        else
        {
            // Nếu không có EnemyAttackController hoặc không có mục tiêu, Boss thực hiện skill đặc biệt
            Debug.Log($"Boss {gameObject.name} đang dùng skill đặc biệt! (Không có EnemyAttackController hoặc không có mục tiêu)");
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

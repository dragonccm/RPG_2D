using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq; // Cần thiết cho các thao tác LINQ nếu sử dụng

/// <summary>
/// Lớp điều khiển AI cho Boss, kế thừa từ Enemy để tùy chỉnh hành vi.
/// </summary>
public class EnemyBoss : Enemy
{
    [Header("Boss Specific Settings")]
    [Tooltip("Phạm vi mà Boss muốn duy trì với mục tiêu để thực hiện các hành động đặc biệt.")]
    public float bossActionRange = 8f; // Đổi tên từ bossRange để rõ ràng hơn về mục đích
    [Tooltip("Khoảng cách tối thiểu Boss muốn giữ với mục tiêu.")]
    public float bossMinDistance = 3f; // Đổi tên từ minDistance để rõ ràng hơn

    // Ghi đè phương thức UpdateTarget của lớp Enemy để tùy chỉnh logic chọn mục tiêu của Boss
    protected override void UpdateTarget()
    {
        Transform bestBossTarget = null;
        float bestBossTargetDistance = float.MaxValue;

        // 1. Boss ưu tiên target hiện tại nếu nó vẫn trong tầm chaseRange
        if (target != null && Vector3.Distance(transform.position, target.position) <= chaseRange)
        {
            bestBossTarget = target;
            bestBossTargetDistance = Vector3.Distance(transform.position, target.position);
            Debug.Log($"[EnemyBoss-UpdateTarget] Keeping current target: {bestBossTarget.name} at distance {bestBossTargetDistance:F2}");
        }

        // 2. Boss tìm tất cả players trong detectionRange
        tempPlayerList.Clear(); // tempPlayerList là biến protected static trong Enemy.cs
        var colliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayerMask);

        foreach (var col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                tempPlayerList.Add(col.transform);
            }
        }
        Debug.Log($"[EnemyBoss-UpdateTarget] Found {tempPlayerList.Count} players in detection range.");

        // 3. Boss ưu tiên player có HP thấp nhất trong số các player được phát hiện
        Transform lowestHPPlayer = null;
        float lowestHP = float.MaxValue;

        foreach (var player in tempPlayerList)
        {
            var character = player.GetComponent<Character>();
            if (character != null)
            {
                if (character.CurrentHealth < lowestHP)
                {
                    lowestHP = character.CurrentHealth;
                    lowestHPPlayer = player;
                }
            }
        }

        // Nếu tìm thấy player có HP thấp nhất, xem xét nó là ứng cử viên tốt nhất
        if (lowestHPPlayer != null)
        {
            float distToLowestHPPlayer = Vector3.Distance(transform.position, lowestHPPlayer.position);
            Debug.Log($"[EnemyBoss-UpdateTarget] Lowest HP Player: {lowestHPPlayer.name} with HP {lowestHP:F2} at distance {distToLowestHPPlayer:F2}");

            // Nếu không có bestBossTarget hiện tại HOẶC player HP thấp nhất gần hơn
            // HOẶC nếu boss muốn ưu tiên HP thấp hơn khoảng cách (logic này có thể tùy chỉnh)
            if (bestBossTarget == null || distToLowestHPPlayer < bestBossTargetDistance)
            {
                bestBossTarget = lowestHPPlayer;
                bestBossTargetDistance = distToLowestHPPlayer;
                Debug.Log($"[EnemyBoss-UpdateTarget] New best target candidate: {bestBossTarget.name}");
            }
        }

        // 4. Cập nhật target chính của Enemy (lớp cơ sở)
        target = bestBossTarget;

        // 5. Nếu target hiện tại đã ra khỏi chaseRange, đặt target về null
        if (target != null && Vector3.Distance(transform.position, target.position) > chaseRange)
        {
            Debug.Log($"[EnemyBoss-UpdateTarget] Target {target.name} ({Vector3.Distance(transform.position, target.position):F2}m) out of chase range ({chaseRange}m). Setting target to null.");
            target = null;
        }

        // 6. Cập nhật playerTarget cho EnemyAIController và chuyển trạng thái
        var aiController = GetComponent<EnemyAIController>();
        if (aiController != null)
        {
            aiController.playerTarget = target; // Đồng bộ playerTarget với target chính của Enemy

            if (target != null)
            {
                // Xác định trạng thái phù hợp dựa trên khoảng cách đến mục tiêu hiện tại
                var attackController = GetComponent<EnemyAttackController>();
                float currentDistance = Vector3.Distance(transform.position, target.position);

                if (attackController != null && currentDistance <= attackController.AttackRange)
                {
                    Debug.Log($"[EnemyBoss-UpdateTarget] Target {target.name} ({currentDistance:F2}m) in attack range ({attackController.AttackRange}m). Changing to AttackState.");
                    aiController.ChangeState(aiController.attackState);
                }
                else if (currentDistance <= chaseRange)
                {
                    Debug.Log($"[EnemyBoss-UpdateTarget] Target {target.name} ({currentDistance:F2}m) in chase range ({chaseRange}m). Changing to ChaseState.");
                    aiController.ChangeState(aiController.chaseState);
                }
                else
                {
                    // Trường hợp này chỉ xảy ra nếu target không null nhưng đã ra ngoài chaseRange
                    // và chưa kịp được đặt về null ở bước 5.
                    Debug.Log($"[EnemyBoss-UpdateTarget] Target {target.name} ({currentDistance:F2}m) outside chase range ({chaseRange}m). Changing to IdleState (fallback).");
                    aiController.ChangeState(aiController.idleState);
                }
            }
            else
            {
                // Nếu không có mục tiêu, chuyển trạng thái AI sang nhàn rỗi hoặc tuần tra
                Debug.Log($"[EnemyBoss-UpdateTarget] No target found. Changing to IdleState.");
                aiController.ChangeState(aiController.idleState);
            }
        }
    }

    // Ghi đè phương thức HandleMovement của lớp Enemy để tùy chỉnh hành vi di chuyển của Boss
    protected override void HandleMovement()
    {
        if (agent == null)
        {
            Debug.LogWarning("NavMeshAgent is not assigned to EnemyBoss: " + gameObject.name, this);
            return;
        }

        if (target != null)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            Debug.Log($"[EnemyBoss-HandleMovement] Distance to target {target.name}: {dist:F2}m");

            if (dist > bossActionRange) // Nếu mục tiêu quá xa phạm vi hành động của boss
            {
                agent.SetDestination(target.position); // Tiếp tục di chuyển đến mục tiêu
                agent.isStopped = false;
                Debug.Log($"[EnemyBoss-HandleMovement] Chasing target to {target.position}.");
            }
            else if (dist < bossMinDistance) // Nếu mục tiêu quá gần khoảng cách tối thiểu
            {
                // Quá gần - lùi lại một chút để duy trì khoảng cách tối ưu
                Vector3 directionAway = (transform.position - target.position).normalized;
                Vector3 retreatPosition = transform.position + directionAway * (bossMinDistance + 1f); // Lùi ra xa hơn minDistance

                NavMeshHit hit;
                if (NavMesh.SamplePosition(retreatPosition, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    agent.isStopped = false;
                    Debug.Log($"[EnemyBoss-HandleMovement] Retreating to {hit.position}.");
                }
                else
                {
                    // Nếu không tìm được điểm lùi hợp lệ, dừng lại
                    agent.isStopped = true;
                    if (agent.hasPath) agent.ResetPath();
                    Debug.Log($"[EnemyBoss-HandleMovement] Cannot find retreat position, stopping.");
                }
            }
            else // Nếu mục tiêu trong khoảng bossMinDistance và bossActionRange
            {
                // Đã ở trong phạm vi hành động tối ưu, dừng di chuyển và tấn công/dùng skill
                agent.isStopped = true;
                if (agent.hasPath) agent.ResetPath();
                Debug.Log($"[EnemyBoss-HandleMovement] In optimal action range. Stopping movement and attempting attack.");
                HandleBossAttack(); // Gọi phương thức xử lý tấn công/skill của Boss
            }
        }
        else
        {
            // Nếu không có mục tiêu, Boss đứng yên
            agent.isStopped = true;
            if (agent.hasPath) agent.ResetPath();
            Debug.Log($"[EnemyBoss-HandleMovement] No target, stopping movement.");
        }
    }

    /// <summary>
    /// Logic tấn công hoặc sử dụng skill đặc biệt của Boss.
    /// </summary>
    protected virtual void HandleBossAttack()
    {
        // Kiểm tra cooldown tấn công của EnemyAttackController nếu có
        var attackController = GetComponent<EnemyAttackController>();
        if (attackController != null && target != null)
        {
            // Sử dụng logic Attack của EnemyAttackController
            Debug.Log($"[EnemyBoss-HandleBossAttack] Calling Attack on {target.name}.");
            attackController.Attack(target);
        }
        else
        {
            Debug.Log($"Boss {gameObject.name} đang dùng skill đặc biệt! (Không có EnemyAttackController hoặc không có mục tiêu)");
            // Thêm logic skill đặc biệt của Boss ở đây
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Vẽ Gizmos của lớp Enemy (detectionRange, chaseRange)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, bossActionRange); // Vẽ phạm vi hành động của Boss
        Gizmos.color = new Color(1, 0, 1, 0.2f);
        Gizmos.DrawWireSphere(transform.position, bossMinDistance); // Vẽ khoảng cách tối thiểu của Boss
    }
}

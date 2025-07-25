using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Điều khiển di chuyển của kẻ địch bằng NavMeshAgent. Tự động theo dõi playerTarget.
/// Có thể dùng cho mọi loại AI (Melee, Ranged, Boss, Support).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementController : MonoBehaviour
{
    // === Các thuộc tính cấu hình di chuyển ===
    [Tooltip("Transform của player để kẻ địch đuổi theo")]
    public Transform playerTarget; // Được cập nhật bởi Enemy.cs hoặc EnemyAIController

    [Tooltip("Tốc độ di chuyển của kẻ địch")]
    public float moveSpeed = 3f;
    [Tooltip("Tốc độ xoay của kẻ địch khi di chuyển")]
    public float rotationSpeed = 10f;

    private NavMeshAgent agent;

    public NavMeshAgent Agent => agent;
    private EnemyAIController aiController;
    private EnemyAnimatorController animatorController;

    // === Các phương thức chính ===
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        aiController = GetComponent<EnemyAIController>();
        animatorController = GetComponent<EnemyAnimatorController>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    void Start()
    {
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    void Update()
    {
        // Nếu có playerTarget và NavMeshAgent đang hoạt động, luôn set destination về player
        if (playerTarget != null && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
        }
        else if (agent != null && !agent.isStopped)
        {
            // Nếu không có playerTarget hoặc không trên NavMesh, dừng di chuyển
            agent.isStopped = true;
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }

        // Xử lý animation và lật kẻ địch (2D chỉ lật trái/phải)
        if (agent != null)
        {
            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                // Lật sprite theo hướng di chuyển trên trục X
                if (agent.velocity.x > 0.01f)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }
                else if (agent.velocity.x < -0.01f)
                {
                    Vector3 scale = transform.localScale;
                    scale.x = -Mathf.Abs(scale.x);
                    transform.localScale = scale;
                }
                animatorController?.PlayMoveAnimation(agent.velocity.magnitude);
            }
            else
            {
                animatorController?.PlayIdleAnimation();
            }
        }
    }

    /// <summary>
    /// Di chuyển tới vị trí chỉ định (dùng cho AI support, boss, v.v.).
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    /// <summary>
    /// Dừng di chuyển (dùng khi tấn công, chết, v.v.).
    /// </summary>
    public void Stop()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }
}

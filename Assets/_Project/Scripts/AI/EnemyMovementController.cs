using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Điều khiển di chuyển của kẻ địch theo player bằng NavMeshAgent.
/// Chỉ cần gán playerTarget và đảm bảo có NavMeshAgent.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementController : MonoBehaviour
{
    [Tooltip("Transform của player để kẻ địch đuổi theo")]
    public Transform playerTarget; // Được cập nhật bởi Enemy.cs hoặc EnemyAIController

    [Tooltip("Tốc độ di chuyển của kẻ địch")]
    public float moveSpeed = 3f;

    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

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

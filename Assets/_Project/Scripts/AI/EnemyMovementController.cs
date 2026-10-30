using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Điều khiển di chuyển của kẻ địch bằng NavMeshAgent. Chỉ xử lý movement mechanics.
/// Enemy.cs sẽ chịu trách nhiệm về target và destination logic.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Tốc độ di chuyển của kẻ địch")]
    public float moveSpeed = 3f;
    [Tooltip("Tốc độ xoay của kẻ địch khi di chuyển")]
    public float rotationSpeed = 10f;
    
    private NavMeshAgent agent;
    private EnemyAnimatorController animatorController;
    private Vector3 lastDestination = Vector3.zero;
    private const float DESTINATION_CHANGE_THRESHOLD = 0.5f;

    public NavMeshAgent Agent => agent;
    public bool IsMoving => agent != null && agent.velocity.sqrMagnitude > 0.1f;
    public bool HasReachedDestination => agent != null && !agent.hasPath && agent.remainingDistance < 0.1f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
        // Giữ rotation cho 2D
        if (transform.eulerAngles.y != 0f || transform.eulerAngles.z != 0f)
        {
            var euler = transform.eulerAngles;
            euler.y = 0f;
            euler.z = 0f;
            transform.eulerAngles = euler;
        }

        // Xử lý animation và sprite flipping
        HandleAnimationAndFlipping();
    }

    /// <summary>
    /// Di chuyển tới vị trí được chỉ định. Tự động tối ưu để tránh recalculation không cần thiết.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Chỉ update destination nếu thay đổi đáng kể
        if (Vector3.Distance(destination, lastDestination) <= DESTINATION_CHANGE_THRESHOLD)
            return;

        // Kiểm tra và tìm vị trí NavMesh gần nhất
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(destination, out hit, 0.1f, NavMesh.AllAreas))
        {
            if (NavMesh.SamplePosition(destination, out hit, 10f, NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            else
            {
                return; // Không thể tìm thấy điểm NavMesh hợp lệ
            }
        }
        
        agent.isStopped = false;
        agent.SetDestination(destination);
        lastDestination = destination;
    }

    /// <summary>
    /// Dừng di chuyển ngay lập tức.
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

    /// <summary>
    /// Đặt tốc độ di chuyển mới.
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    /// <summary>
    /// Xử lý animation và lật sprite dựa trên velocity.
    /// </summary>
    private void HandleAnimationAndFlipping()
    {
        if (agent == null) return;

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            // Lật sprite theo hướng di chuyển
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                if (agent.velocity.x > 0.01f)
                    spriteRenderer.flipX = false;
                else if (agent.velocity.x < -0.01f)
                    spriteRenderer.flipX = true;
            }
            
            animatorController?.PlayMoveAnimation(agent.velocity.magnitude);
        }
        else
        {
            animatorController?.PlayIdleAnimation();
        }
    }
}

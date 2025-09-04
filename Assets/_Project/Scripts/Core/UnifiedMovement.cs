using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Unified movement system to replace multiple movement controllers
/// Consolidates PlayerMovement, EnemyMovement, and AI pathfinding
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class UnifiedMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private bool enableDebugLogging = false;

    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private LayerMask obstacleLayers;

    private Rigidbody2D rb;
    private UnifiedAnimator animator;
    private Vector2 currentVelocity;
    private Vector2 targetPosition;
    private bool isMovingToTarget = false;
    private Transform followTarget;

    // Cached components
    private NavMeshAgent navAgent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<UnifiedAnimator>();

        // Configure Rigidbody2D for smooth movement
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Try to get NavMeshAgent if available
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            ConfigureNavMeshAgent();
        }
    }

    private void ConfigureNavMeshAgent()
    {
        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
        navAgent.speed = moveSpeed;
        navAgent.stoppingDistance = stoppingDistance;
    }

    private void FixedUpdate()
    {
        if (isMovingToTarget && targetPosition != null)
        {
            MoveTowardsTarget();
        }
        else if (followTarget != null)
        {
            FollowTarget();
        }
    }

    /// <summary>
    /// Move in a direction with smooth acceleration/deceleration
    /// </summary>
    public void Move(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            // Decelerate to stop
            currentVelocity = Vector2.MoveTowards(currentVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            // Accelerate towards target direction
            Vector2 targetVelocity = direction.normalized * moveSpeed;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }

        // Apply movement
        rb.linearVelocity = currentVelocity;

        // Update animation
        if (animator != null)
        {
            bool isMoving = currentVelocity.magnitude > 0.1f;
            animator.SetMovement(isMoving, currentVelocity.magnitude);

            if (isMoving)
            {
                animator.SetFacingDirection(Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg);
            }
        }

        if (enableDebugLogging && currentVelocity.magnitude > 0.1f)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🚶 Moving: {0}", currentVelocity));
        }
    }

    /// <summary>
    /// Move towards a specific position
    /// </summary>
    public void MoveToPosition(Vector2 position)
    {
        targetPosition = position;
        isMovingToTarget = true;
        followTarget = null;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("🎯 Moving to position: {0}", position));
        }
    }

    /// <summary>
    /// Follow a target transform
    /// </summary>
    public void FollowTarget(Transform target)
    {
        followTarget = target;
        isMovingToTarget = false;

        if (enableDebugLogging && target != null)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("👤 Following target: {0}", target.name));
        }
    }

    /// <summary>
    /// Stop following current target
    /// </summary>
    public void StopFollowing()
    {
        followTarget = null;
        isMovingToTarget = false;
        Move(Vector2.zero);

        if (enableDebugLogging)
        {
            PerformanceUtils.Log("⏹️ Stopped following target");
        }
    }

    /// <summary>
    /// Move towards target position using simple AI
    /// </summary>
    private void MoveTowardsTarget()
    {
        Vector2 currentPos = transform.position;
        Vector2 direction = (targetPosition - currentPos).normalized;

        // Check if we've reached the target
        if (Vector2.Distance(currentPos, targetPosition) <= stoppingDistance)
        {
            isMovingToTarget = false;
            Move(Vector2.zero);

            if (enableDebugLogging)
            {
                PerformanceUtils.Log("✅ Reached target position");
            }
            return;
        }

        // Avoid obstacles (simple raycast-based avoidance)
        if (HasObstacleInPath(currentPos, targetPosition))
        {
            // Try to find a detour
            direction = FindDetourDirection(currentPos, targetPosition);
        }

        Move(direction);
    }

    /// <summary>
    /// Follow the assigned target
    /// </summary>
    private void FollowTarget()
    {
        if (followTarget == null) return;

        Vector2 currentPos = transform.position;
        Vector2 targetPos = followTarget.position;
        Vector2 direction = (targetPos - currentPos).normalized;

        // Check distance to target
        float distance = Vector2.Distance(currentPos, targetPos);

        if (distance <= stoppingDistance)
        {
            Move(Vector2.zero);
            return;
        }

        // Avoid obstacles
        if (HasObstacleInPath(currentPos, targetPos))
        {
            direction = FindDetourDirection(currentPos, targetPos);
        }

        Move(direction);
    }

    /// <summary>
    /// Check if there's an obstacle between two points
    /// </summary>
    private bool HasObstacleInPath(Vector2 from, Vector2 to)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(from, direction.normalized, distance, obstacleLayers);

        return hit.collider != null;
    }

    /// <summary>
    /// Find a detour direction around obstacles
    /// </summary>
    private Vector2 FindDetourDirection(Vector2 from, Vector2 to)
    {
        // Simple detour: try perpendicular directions
        Vector2 originalDirection = (to - from).normalized;

        // Try right detour
        Vector2 rightDetour = Quaternion.Euler(0, 0, 90) * originalDirection;
        if (!HasObstacleInPath(from, from + rightDetour * 2f))
        {
            return rightDetour;
        }

        // Try left detour
        Vector2 leftDetour = Quaternion.Euler(0, 0, -90) * originalDirection;
        if (!HasObstacleInPath(from, from + leftDetour * 2f))
        {
            return leftDetour;
        }

        // If both blocked, move away from obstacle
        return -originalDirection;
    }

    /// <summary>
    /// Teleport to position instantly
    /// </summary>
    public void TeleportTo(Vector2 position)
    {
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;

        if (enableDebugLogging)
        {
            PerformanceUtils.Log(PerformanceUtils.FormatString("✨ Teleported to: {0}", position));
        }
    }

    /// <summary>
    /// Get current velocity
    /// </summary>
    public Vector2 GetVelocity()
    {
        return currentVelocity;
    }

    /// <summary>
    /// Check if currently moving
    /// </summary>
    public bool IsMoving()
    {
        return currentVelocity.magnitude > 0.1f;
    }

    /// <summary>
    /// Set movement speed modifier
    /// </summary>
    public void SetSpeedModifier(float modifier)
    {
        moveSpeed *= modifier;
        if (navAgent != null)
        {
            navAgent.speed = moveSpeed;
        }
    }

    /// <summary>
    /// Reset movement speed to base
    /// </summary>
    public void ResetSpeedModifier()
    {
        // Reset to original value - would need to store original
        // moveSpeed = originalMoveSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Visualize stopping distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);

        // Visualize target position
        if (isMovingToTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(targetPosition, 0.2f);
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lớp trừu tượng điều khiển AI của kẻ địch. Quản lý state machine, animator, target, và các trạng thái cơ bản.
/// Mọi AI cụ thể (Melee, Ranged, Boss, Support) đều kế thừa từ đây để mở rộng logic riêng.
/// </summary>
public abstract class EnemyAIController : MonoBehaviour
{
    // State machine và các trạng thái
    public StateMachine stateMachine;
    public IdleState idleState;
    public ChaseState chaseState;
    public AttackState attackState;
    public State patrolState;
    public DeadState deadState;
    
    // Component references
    public EnemyAnimatorController animatorController;
    public EnemyMovementController movementController;
    
    // Target management
    public Transform playerTarget;
    public Vector3[] patrolPoints;
    public EnemyType enemyType;

    protected virtual void Awake()
    {
        // Cache component references
        stateMachine = new StateMachine();
        animatorController = GetComponent<EnemyAnimatorController>();
        movementController = GetComponent<EnemyMovementController>();
        
        if (animatorController == null)
        {
            Debug.LogWarning($"[{gameObject.name}] EnemyAnimatorController not found on this GameObject.", this);
        }

        // Khởi tạo các trạng thái
        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        patrolState = new PatrolState(this, stateMachine);
        deadState = new DeadState(this, stateMachine);

        // Khởi tạo máy trạng thái với trạng thái Idle ban đầu
        stateMachine.Initialize(idleState);
    }

    /// <summary>
    /// Update: Thực thi trạng thái hiện tại của AI (state machine pattern).
    /// </summary>
    protected virtual void Update()
    {
        stateMachine.currentState?.Execute();
    }

    /// <summary>
    /// Phương thức ảo: Cho phép các lớp con ghi đè logic khi AI được "báo động" về một mục tiêu (alert).
    /// </summary>
    /// <param name="target">Mục tiêu được báo động.</param>
    public virtual void Alert(Transform target) { }

    /// <summary>
    /// Đổi trạng thái hiện tại của AI sang trạng thái mới.
    /// </summary>
    /// <param name="newState">Trạng thái mới cần chuyển đến.</param>
    public void ChangeState(State newState) => stateMachine.ChangeState(newState);

    /// <summary>
    /// Lấy target ưu tiên từ danh sách (mặc định: player gần nhất). Có thể override ở lớp con.
    /// </summary>
    public virtual Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;
        
        foreach (var t in availableTargets)
        {
            if (t != null && t.CompareTag("Player"))
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
    /// Kiểm tra xem người chơi có nằm trong vùng phát hiện không
    /// </summary>
    /// <returns>True nếu người chơi trong tầm phát hiện, ngược lại là False</returns>
    public virtual bool IsPlayerInDetectionRange()
    {
        var enemy = GetComponent<Enemy>();
        float detectionRange = enemy != null ? enemy.detectionRange : 10f;
        if (playerTarget == null) return false;
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        return distance <= detectionRange;
    }
    
    /// <summary>
    /// Kiểm tra xem người chơi có nằm trong vùng truy đuổi không
    /// </summary>
    /// <returns>True nếu người chơi trong tầm truy đuổi, ngược lại là False</returns>
    public virtual bool IsPlayerInChaseRange()
    {
        var enemy = GetComponent<Enemy>();
        float chaseRange = enemy != null ? enemy.chaseRange : 20f;
        if (playerTarget == null) return false;
        float distance = Vector3.Distance(transform.position, playerTarget.position);
        return distance <= chaseRange;
    }
}

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Lớp trừu tượng điều khiển AI của kẻ địch, quản lý máy trạng thái và các tham chiếu chung.
/// </summary>
public abstract class EnemyAIController : MonoBehaviour
{
    public StateMachine stateMachine; // Máy trạng thái điều khiển AI

    public EnemyType enemyType; // Loại kẻ địch (cận chiến, tầm xa, hỗ trợ, boss)
    [HideInInspector] // Ẩn khỏi Inspector vì nó sẽ được Enemy.cs tự động cập nhật
    public Transform playerTarget; // Mục tiêu người chơi mà AI này đang tập trung vào (được cập nhật từ Enemy.cs)

    public AIGroup group; // Nhóm AI mà kẻ địch này thuộc về

    // Các trạng thái AI cơ bản
    public IdleState idleState;
    public ChaseState chaseState;
    public AttackState attackState;
    public PatrolState patrolState;
    public DeadState deadState; // Thêm DeadState vào đây

    public Vector3[] patrolPoints; // Các điểm tuần tra cho trạng thái Patrol

    protected virtual void Awake()
    {
        stateMachine = new StateMachine();

        // Khởi tạo các trạng thái
        idleState = new IdleState(this, stateMachine);
        chaseState = new ChaseState(this, stateMachine);
        attackState = new AttackState(this, stateMachine);
        patrolState = new PatrolState(this, stateMachine, patrolPoints);
        deadState = new DeadState(this, stateMachine); // Khởi tạo DeadState

        // Khởi tạo máy trạng thái với trạng thái Idle ban đầu
        stateMachine.Initialize(idleState);
    }

    protected virtual void Update()
    {
        // Logic quản lý target (xác định và bỏ target) đã được chuyển hoàn toàn sang lớp Enemy.cs.
        // Enemy.cs sẽ quản lý target chung dựa trên chaseRange và cập nhật playerTarget này.
        // Các lớp con của EnemyAIController sẽ sử dụng playerTarget này để điều khiển hành vi AI cụ thể.

        stateMachine.currentState?.Execute(); // Thực thi trạng thái hiện tại của AI
    }

    /// <summary>
    /// Phương thức ảo để các lớp con ghi đè logic khi AI được "báo động" về một mục tiêu.
    /// </summary>
    /// <param name="target">Mục tiêu được báo động.</param>
    public virtual void Alert(Transform target) { }

    /// <summary>
    /// Thay đổi trạng thái hiện tại của máy trạng thái.
    /// </summary>
    /// <param name="newState">Trạng thái mới cần chuyển đến.</param>
    public void ChangeState(State newState) => stateMachine.ChangeState(newState);
    public virtual Transform GetPriorityTarget(List<Transform> availableTargets)
    {
        Debug.Log("---------call GetPriorityTarget---------");

        // Mặc định, lớp cơ sở chỉ ưu tiên player gần nhất trong danh sách được cung cấp.
        // Lớp con như MeleeEnemyAI sẽ ghi đè để có logic ưu tiên phức tạp hơn.
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;
        foreach (var t in availableTargets)
        {
            Debug.Log("--------------------"+t.gameObject.name);
            if (t != null && t.CompareTag("Player")) // Chỉ xem xét các đối tượng có tag "Player"
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

}

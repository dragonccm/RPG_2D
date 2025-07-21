using UnityEngine;

/// <summary>
/// Trạng thái tấn công mục tiêu của kẻ địch.
/// </summary>
public class AttackState : State
{
    public AttackState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[{aiController.enemyType}] Enter AttackState");
        aiController.GetComponent<EnemyMovementController>()?.Stop(); // Dừng di chuyển khi tấn công
    }

    public override void Execute()
    {
        base.Execute();
        var enemy = aiController.GetComponent<Enemy>();
        var attackController = aiController.GetComponent<EnemyAttackController>();

        // Lấy phạm vi truy đuổi và tấn công từ Enemy và EnemyAttackController
        float chaseRange = enemy != null ? enemy.chaseRange : 20f;
        float attackRange = attackController != null ? attackController.AttackRange : 2f; // Sử dụng AttackRange property

        if (aiController.playerTarget == null)
        {
            Debug.Log($"[{aiController.enemyType}] AttackState: Lost target, switching to IdleState");
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        // Nếu player ra khỏi vùng chaseRange → Patrol/Idle
        if (distanceToPlayer > chaseRange)
        {
            Debug.Log($"[{aiController.enemyType}] AttackState: Player out of chase range ({distanceToPlayer:F2}m > {chaseRange:F2}m), switching to PatrolState");
            stateMachine.ChangeState(aiController.patrolState);
            return;
        }

        // Nếu player ra khỏi vùng attack nhưng vẫn trong vùng chaseRange → Chase
        if (attackController != null && distanceToPlayer > attackRange)
        {
            Debug.Log($"[{aiController.enemyType}] AttackState: Player out of attack range ({distanceToPlayer:F2}m > {attackRange:F2}m), switching to ChaseState");
            stateMachine.ChangeState(aiController.chaseState);
            return;
        }

        // Nếu vẫn trong vùng attack → tấn công
        if (attackController != null && distanceToPlayer <= attackRange)
        {
            attackController.Attack(aiController.playerTarget);
        }
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit AttackState");
    }
}

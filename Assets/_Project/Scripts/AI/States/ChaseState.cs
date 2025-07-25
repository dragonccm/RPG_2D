using UnityEngine;

/// <summary>
/// Trạng thái truy đuổi mục tiêu của kẻ địch.
/// </summary>
public class ChaseState : State
{
    public ChaseState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[{aiController.enemyType}] Enter ChaseState: target={aiController.playerTarget?.name}");
        var moveCtrl = aiController.GetComponent<EnemyMovementController>();
        if (moveCtrl != null)
        {
            // EnemyMovementController sẽ tự động di chuyển đến playerTarget
            moveCtrl.playerTarget = aiController.playerTarget;
            Debug.Log($"[{aiController.enemyType}] ChaseState: Set MovementController target to {moveCtrl.playerTarget?.name}");
            aiController.animatorController?.PlayMoveAnimation(moveCtrl.Agent.speed);
        }
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
            stateMachine.ChangeState(aiController.patrolState); // Hoặc idleState nếu không có tuần tra
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        if (distanceToPlayer > chaseRange)
        {
            stateMachine.ChangeState(aiController.patrolState);
            return;
        }

        if (aiController is RangedEnemyAI rangedAI && distanceToPlayer < rangedAI.safeDistance)
        {
            stateMachine.ChangeState(rangedAI.repositionState);
        }
        else if (distanceToPlayer <= attackRange)
        {
            stateMachine.ChangeState(aiController.attackState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit ChaseState");
        var moveCtrl = aiController.GetComponent<EnemyMovementController>();
        if (moveCtrl != null)
        {
            moveCtrl.playerTarget = null; // Dừng di chuyển theo player khi thoát trạng thái Chase
            Debug.Log($"[{aiController.enemyType}] ChaseState: Cleared MovementController target.");
        }
        aiController.animatorController?.PlayIdleAnimation();
    }
}

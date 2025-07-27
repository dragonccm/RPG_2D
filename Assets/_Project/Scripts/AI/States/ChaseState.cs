using UnityEngine;

/// <summary>
/// Trạng thái truy đuổi người chơi khi phát hiện trong tầm.
/// </summary>
public class ChaseState : State
{
    [Header("Cài đặt truy đuổi")]
    [Tooltip("Khoảng thời gian giữa các lần cập nhật trạng thái (giây)")]
    private float stateUpdateInterval = 0.3f;
    
    [Tooltip("Khoảng cách tối đa để tiếp tục đuổi người chơi")]
    private float chaseRange;
    
    [Tooltip("Khoảng cách để bắt đầu tấn công người chơi")]
    private float attackRange;
    
    private float nextStateUpdate;
    private EnemyMovementController movementController;
    
    public ChaseState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
        movementController = aiController.GetComponent<EnemyMovementController>();
    }

    public override void Enter()
    {
        base.Enter();
        
        if (movementController != null && aiController.playerTarget != null)
        {
            // EnemyMovementController sẽ tự động tối ưu destination updates
            movementController.MoveTo(aiController.playerTarget.position);
            aiController.animatorController?.PlayMoveAnimation(movementController.Agent?.speed ?? 3f);
        }
    }

    public override void Execute()
    {
        base.Execute();
        
        // Throttle state checking để tối ưu performance
        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;
        
        var enemy = aiController.GetComponent<Enemy>();
        var attackController = aiController.GetComponent<EnemyAttackController>();

        // Lấy phạm vi từ components
        chaseRange = enemy != null ? enemy.chaseRange : 20f;
        attackRange = attackController != null ? attackController.AttackRange : 2f;

        if (aiController.playerTarget == null)
        {
            // Không có player target → về Idle, để Enemy.cs xử lý patrol logic
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        // Kiểm tra vượt quá chase range
        if (distanceToPlayer > chaseRange)
        {
            // Player ra khỏi chase range → về Idle, để Enemy.cs xử lý return to anchor + patrol
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        // Kiểm tra reposition cho ranged enemies
        if (aiController is RangedEnemyAI rangedAI && distanceToPlayer < rangedAI.safeDistance)
        {
            stateMachine.ChangeState(rangedAI.repositionState);
            return;
        }
        
        // Kiểm tra attack range
        if (distanceToPlayer <= attackRange)
        {
            stateMachine.ChangeState(aiController.attackState);
            return;
        }
        
        // Tiếp tục chase - update destination
        if (movementController != null && aiController.playerTarget != null)
        {
            movementController.MoveTo(aiController.playerTarget.position);
        }
    }

    public override void Exit()
    {
        base.Exit();
        aiController.animatorController?.PlayIdleAnimation();
    }
}

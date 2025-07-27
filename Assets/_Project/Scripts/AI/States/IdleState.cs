using UnityEngine;

/// <summary>
/// Trạng thái nhàn rỗi của kẻ địch.
/// </summary>
public class IdleState : State
{
    private float stateUpdateInterval = 0.4f;
    private float nextStateUpdate;
    private EnemyMovementController movementController;

    public IdleState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
        movementController = aiController.GetComponent<EnemyMovementController>();
    }

    public override void Enter()
    {
        base.Enter();        
        movementController?.Stop(); // Dừng di chuyển khi nhàn rỗi
        aiController.animatorController?.PlayIdleAnimation();
    }

    public override void Execute()
    {
        base.Execute();
        
        // Throttle detection checking để tối ưu performance
        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;
        
        var enemy = aiController.GetComponent<Enemy>();
        float detectionRange = enemy != null ? enemy.detectionRange : 10f;

        if (aiController.playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);
            
            // Nếu player trong vùng detection, chuyển sang ChaseState
            if (distanceToPlayer < detectionRange)
            {
                stateMachine.ChangeState(aiController.chaseState);
            }
        }
        else
        {
            // Không có player target, kiểm tra có patrol setup không
            if (enemy != null && enemy.anchor != null)
            {
                // Có patrol setup, chuyển sang PatrolState để Enemy.cs xử lý patrol logic
                Debug.Log($"[IdleState] {aiController.name} không có player target, chuyển sang PatrolState");
                stateMachine.ChangeState(aiController.patrolState);
            }

        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}

using UnityEngine;

/// <summary>
/// Trạng thái tuần tra của kẻ địch.
/// </summary>
public class PatrolState : State
{
    private float stateUpdateInterval = 0.5f;
    private float nextStateUpdate;
    private EnemyMovementController movementController;

    public PatrolState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
        movementController = aiController.GetComponent<EnemyMovementController>();
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[PatrolState] {aiController.name} vào PatrolState");
        aiController.animatorController?.PlayIdleAnimation();
    }

    public override void Execute()
    {
        base.Execute();
        
        // Throttle state checking để tối ưu performance
        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;

        var enemy = aiController.GetComponent<Enemy>();
        float detectionRange = enemy != null ? enemy.detectionRange : 10f;

        // Chỉ kiểm tra player detection, Enemy.cs sẽ xử lý toàn bộ patrol logic
        if (aiController.playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);
            if (distanceToPlayer < detectionRange)
            {
                stateMachine.ChangeState(aiController.chaseState);
                return;
            }
        }

        // Tất cả patrol logic (return to anchor, waypoint navigation, random patrol) 
        // được xử lý hoàn toàn bởi Enemy.cs trong UpdatePatrolLogic()
        // PatrolState chỉ đảm nhiệm việc phát hiện player
    }

    public override void Exit()
    {
        base.Exit();
    }
}
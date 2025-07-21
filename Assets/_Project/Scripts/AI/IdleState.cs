using UnityEngine;

/// <summary>
/// Trạng thái nhàn rỗi của kẻ địch.
/// </summary>
public class IdleState : State
{
    public IdleState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[{aiController.enemyType}] Enter IdleState");
        aiController.GetComponent<EnemyMovementController>()?.Stop(); // Dừng di chuyển khi nhàn rỗi
    }

    public override void Execute()
    {
        base.Execute();
        var enemy = aiController.GetComponent<Enemy>();
        // Sử dụng detectionRange để phát hiện mục tiêu mới khi ở trạng thái Idle
        float detectionRange = enemy != null ? enemy.detectionRange : 10f;

        if (aiController.playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);
            Debug.Log($"[{aiController.enemyType}] IdleState: Player detected. Distance: {distanceToPlayer:F2}m. Detection Range: {detectionRange:F2}m.");
            // Nếu player trong vùng detection, chuyển sang ChaseState
            if (distanceToPlayer < detectionRange)
            {
                Debug.Log($"[{aiController.enemyType}] Player detected, switching to ChaseState");
                stateMachine.ChangeState(aiController.chaseState);
            }
        }
        else
        {
            // Debug.Log($"[{aiController.enemyType}] IdleState: No playerTarget.");
        }
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit IdleState");
    }
}

using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Trạng thái quay về điểm neo khi kẻ địch đi quá xa hoặc mất dấu người chơi.
/// </summary>
public class ReturnToAnchorState : State
{
    private float arriveThreshold;
    private float stateUpdateInterval = 0.4f;
    private float nextStateUpdate;
    private EnemyMovementController movementController;
    private Enemy enemy;
    
    // Anti-stuck mechanism
    private Vector3 lastPosition;
    private float lastPositionCheckTime;
    private int stuckCounter = 0;
    private const float STUCK_DETECTION_INTERVAL = 2f;
    private const float MIN_MOVEMENT_THRESHOLD = 0.2f;

    public ReturnToAnchorState(EnemyAIController aiController, StateMachine stateMachine, float arriveThreshold) : base(aiController, stateMachine)
    {
        this.arriveThreshold = arriveThreshold;
        this.movementController = aiController.GetComponent<EnemyMovementController>();
        this.enemy = aiController.GetComponent<Enemy>();
    }

    public override void Enter()
    {
        base.Enter();
        
        nextStateUpdate = Time.time + stateUpdateInterval;
        lastPosition = aiController.transform.position;
        lastPositionCheckTime = Time.time;
        stuckCounter = 0;
        
        aiController.animatorController?.PlayMoveAnimation(movementController?.Agent?.speed ?? 3f);
        
        // Bắt đầu di chuyển về anchor
        if (enemy?.anchor != null && movementController != null)
        {
            movementController.MoveTo(enemy.anchor.position);
        }
    }

    public override void Execute()
    {
        base.Execute();
        
        // Throttle state checking
        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;

        // Nếu phát hiện player trong vùng detection, chuyển sang ChaseState
        if (aiController.IsPlayerInDetectionRange())
        {
            stateMachine.ChangeState(aiController.chaseState);
            return;
        }

        // Nếu không có anchor, chuyển sang Idle
        if (enemy?.anchor == null)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }
        
        // Kiểm tra đã đến anchor chưa
        float distToAnchor = Vector3.Distance(aiController.transform.position, enemy.anchor.position);
        if (distToAnchor <= arriveThreshold)
        {
            // Đã về đến anchor, chuyển sang PatrolState hoặc IdleState
            if (aiController.patrolState != null)
            {
                stateMachine.ChangeState(aiController.patrolState);
            }
            else
            {
                stateMachine.ChangeState(aiController.idleState);
            }
            return;
        }
        
        // Anti-stuck mechanism
        HandleStuckDetection(distToAnchor);
        
        // Tiếp tục di chuyển về anchor
        if (movementController != null)
        {
            movementController.MoveTo(enemy.anchor.position);
        }
    }

    /// <summary>
    /// Xử lý phát hiện và giải quyết tình trạng bị kẹt.
    /// </summary>
    private void HandleStuckDetection(float distToAnchor)
    {
        if (Time.time - lastPositionCheckTime >= STUCK_DETECTION_INTERVAL)
        {
            float distanceMoved = Vector3.Distance(aiController.transform.position, lastPosition);
            
            // Nếu di chuyển quá ít và vẫn còn xa anchor
            if (distanceMoved < MIN_MOVEMENT_THRESHOLD && distToAnchor > arriveThreshold * 2f)
            {
                stuckCounter++;
                
                // Nếu bị kẹt quá nhiều lần, chuyển sang IdleState
                if (stuckCounter >= 3)
                {
                    stateMachine.ChangeState(aiController.idleState);
                    return;
                }
                
                // Thử tìm path mới bằng cách thêm offset random
                if (movementController != null && enemy?.anchor != null)
                {
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-2f, 2f), 
                        0f, 
                        Random.Range(-2f, 2f)
                    );
                    Vector3 alternativeTarget = enemy.anchor.position + randomOffset;
                    movementController.MoveTo(alternativeTarget);
                }
            }
            else
            {
                stuckCounter = 0; // Reset counter nếu đang di chuyển bình thường
            }
            
            lastPosition = aiController.transform.position;
            lastPositionCheckTime = Time.time;
        }
    }

    public override void Exit()
    {
        base.Exit();
        aiController.animatorController?.PlayIdleAnimation();
    }
}

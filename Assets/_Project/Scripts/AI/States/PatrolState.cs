using UnityEngine;

public class PatrolState : State
{
    private Vector3[] patrolPoints;
    private int currentPatrolIndex;

    public PatrolState(EnemyAIController aiController, StateMachine stateMachine, Vector3[] patrolPoints) : base(aiController, stateMachine)
    {
        this.patrolPoints = patrolPoints;
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[{aiController.enemyType}] Enter PatrolState");
        currentPatrolIndex = 0;
        MoveToNextPatrolPoint();
        aiController.animatorController?.PlayMoveAnimation(aiController.GetComponent<EnemyMovementController>()?.Agent.speed ?? 0f);
    }

    public override void Execute()
    {
        base.Execute();
        var enemy = aiController.GetComponent<Enemy>();
        // Sử dụng detectionRange để phát hiện mục tiêu mới khi ở trạng thái Patrol
        float detectionRange = enemy != null ? enemy.detectionRange : 10f;

        // Ưu tiên đuổi theo người chơi nếu phát hiện
        if (aiController.playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);
            if (distanceToPlayer < detectionRange)
            {
                Debug.Log($"[{aiController.enemyType}] Player detected, switching to ChaseState");
                stateMachine.ChangeState(aiController.chaseState);
                return;
            }
        }

        // Nếu AI là một phần của một nhóm, không thực hiện logic tuần tra độc lập
        if (aiController.group != null) return;

        // Logic đi tuần tra
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (Vector3.Distance(aiController.transform.position, patrolPoints[currentPatrolIndex]) < 1f)
        {
            MoveToNextPatrolPoint();
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        aiController.GetComponent<EnemyMovementController>()?.MoveTo(patrolPoints[currentPatrolIndex]);
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit PatrolState");
        aiController.animatorController?.PlayIdleAnimation();
    }
}

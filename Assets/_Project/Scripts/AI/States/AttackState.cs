using UnityEngine;

/// <summary>
/// Trạng thái tấn công mục tiêu của kẻ địch.
/// </summary>
public class AttackState : State
{
    private float stateUpdateInterval = 0.2f;
    private float nextStateUpdate;
    private EnemyMovementController movementController;

    public AttackState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
        movementController = aiController.GetComponent<EnemyMovementController>();
    }

    public override void Enter()
    {
        base.Enter();
        movementController?.Stop(); // Dừng di chuyển khi tấn công
    }

    public override void Execute()
    {
        base.Execute();
        
        // Throttle state checking để tối ưu performance
        if (Time.time < nextStateUpdate) return;
        nextStateUpdate = Time.time + stateUpdateInterval;
        
        var enemy = aiController.GetComponent<Enemy>();
        var attackController = aiController.GetComponent<EnemyAttackController>();
        var skillManager = aiController.GetComponent<EnemySkillManager>();

        // Lấy phạm vi từ components với fallback values
        float chaseRange = enemy != null ? enemy.chaseRange : 20f;
        float attackRange = attackController != null ? attackController.AttackRange : 2f;

        if (aiController.playerTarget == null)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        // Nếu player ra khỏi vùng chaseRange → về Idle, để Enemy.cs xử lý patrol
        if (distanceToPlayer > chaseRange)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        // Nếu player ra khỏi vùng attack nhưng vẫn trong vùng chaseRange → Chase
        if (distanceToPlayer > attackRange)
        {
            stateMachine.ChangeState(aiController.chaseState);
            return;
        }

        // Nếu vẫn trong vùng attack → tấn công
        if (distanceToPlayer <= attackRange)
        {
            // Ưu tiên dùng kỹ năng nếu có EnemySkillManager
            if (skillManager != null && skillManager.CanUseSkill())
            {
                skillManager.UseSkill();
            }
            else if (attackController != null)
            {
                attackController.Attack(aiController.playerTarget);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        aiController.animatorController?.PlayIdleAnimation();
    }
}

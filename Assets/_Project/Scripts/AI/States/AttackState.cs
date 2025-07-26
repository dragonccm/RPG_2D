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
        // KHÔNG gọi PlayAttackAnimation ở đây nữa, chỉ gọi khi thực sự tấn công trong AttackController hoặc SkillManager
    }

    public override void Execute()
    {
        base.Execute();
        var enemy = aiController.GetComponent<Enemy>();
        var attackController = aiController.GetComponent<EnemyAttackController>();
        var skillManager = aiController.GetComponent<EnemySkillManager>(); // Lấy script quản lý kỹ năng

        // Lấy phạm vi truy đuổi và tấn công từ Enemy và EnemyAttackController
        float chaseRange = enemy != null ? enemy.chaseRange : 20f;
        float attackRange = attackController != null ? attackController.AttackRange : 2f; // Sử dụng AttackRange property

        if (aiController.playerTarget == null)
        {
            stateMachine.ChangeState(aiController.idleState);
            return;
        }

        float distanceToPlayer = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        // Nếu player ra khỏi vùng chaseRange → Patrol/Idle
        if (distanceToPlayer > chaseRange)
        {
            stateMachine.ChangeState(aiController.patrolState);
            return;
        }

        // Nếu player ra khỏi vùng attack nhưng vẫn trong vùng chaseRange → Chase
        if (attackController != null && distanceToPlayer > attackRange)
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
                // Gọi kỹ năng module (có thể random hoặc theo thứ tự)
                skillManager.UseSkill();
            }
            else if (attackController != null)
            {
                // Nếu không có kỹ năng module, dùng tấn công cơ bản
                attackController.Attack(aiController.playerTarget);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit AttackState");
        aiController.animatorController?.PlayIdleAnimation();
    }
}

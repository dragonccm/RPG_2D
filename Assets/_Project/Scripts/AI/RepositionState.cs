using UnityEngine;

public class RepositionState : State
{
    private readonly RangedEnemyAI rangedAI;
    private readonly EnemyMovementController movementController;

    public RepositionState(EnemyAIController aiController, StateMachine stateMachine)
        : base(aiController, stateMachine)
    {
        rangedAI = aiController as RangedEnemyAI;
        movementController = aiController.GetComponent<EnemyMovementController>();
    }

    public override void Enter()
    {
        // Initialization logic for the reposition state
    }

    public override void Execute()
    {
        if (aiController.playerTarget == null) 
        {
            aiController.ChangeState(aiController.idleState);
            return;
        }

        float distance = Vector3.Distance(aiController.transform.position, aiController.playerTarget.position);

        if (distance < rangedAI.safeDistance)
        {
            Vector3 direction = (aiController.transform.position - aiController.playerTarget.position).normalized;
            movementController.MoveTo(aiController.transform.position + direction * rangedAI.safeDistance);
        }
        else if (distance > rangedAI.safeDistance + 1f)
        {
            aiController.ChangeState(aiController.chaseState);
        }
        else
        {
            aiController.ChangeState(aiController.attackState);
        }
    }

    public override void Exit()
    {
        // Cleanup logic for the reposition state
    }
}
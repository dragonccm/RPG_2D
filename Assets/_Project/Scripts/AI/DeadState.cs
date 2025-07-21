using UnityEngine;

public class DeadState : State
{
    public DeadState(EnemyAIController aiController, StateMachine stateMachine) : base(aiController, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Debug.Log($"[{aiController.enemyType}] Enter DeadState");
        // TODO: Trigger death animation and effects
        // Object.Destroy(aiController.gameObject, 2f); // Logic này đã được chuyển vào Enemy.Die()
    }

    public override void Execute()
    {
        // Trong trạng thái Dead, không cần thực thi logic gì thêm
    }

    public override void Exit()
    {
        base.Exit();
        Debug.Log($"[{aiController.enemyType}] Exit DeadState");
    }
}

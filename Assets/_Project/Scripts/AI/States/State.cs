using UnityEngine;

public abstract class State
{
    protected EnemyAIController aiController;
    protected StateMachine stateMachine;

    public State(EnemyAIController aiController, StateMachine stateMachine)
    {
        this.aiController = aiController;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Execute() { }
    public virtual void Exit() { }
}

using UnityEngine;

public abstract class EnemyBaseState
{
    protected EnemyCharacter character;
    protected EnemyStateMachine stateMachine;
    protected float timeInState;

    public EnemyBaseState(EnemyStateMachine stateMachine, EnemyCharacter character)
    {
        this.stateMachine = stateMachine;
        this.character = character;
    }
    private string StateName()
    {
        return this.GetType().Name;
    }
    public virtual void Enter()
    {
        //Debug.Log($"Enter {StateName()}");
    }
    public virtual void Update()
    {
        timeInState += Time.deltaTime;
    }
    public virtual void Exit()
    {
        //Debug.Log($"Exit {StateName()}");
        timeInState = 0;
    }
}
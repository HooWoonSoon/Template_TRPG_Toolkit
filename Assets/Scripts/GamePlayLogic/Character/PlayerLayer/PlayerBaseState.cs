using UnityEngine;

public abstract class PlayerBaseState
{
    protected PlayerCharacter character;
    protected PlayerStateMachine stateMachine;

    public PlayerBaseState(PlayerStateMachine stateMachine, PlayerCharacter character)
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
        //Debug.Log($"Update {StateName()}");
    }
    public virtual void LateUpdate()
    {
        //Debug.Log($"Late Update {StateName()}");
    }
    public virtual void FixedUpdate()
    {
        //Debug.Log($"Fixed Update {StateName()}");
    }
    public virtual void Exit() 
    {
        //Debug.Log($"Exit {StateName()}");
    }
}
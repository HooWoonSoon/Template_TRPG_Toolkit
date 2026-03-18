public abstract class PlayerTeamState
{
    protected PlayerTeamSystem team;
    protected TeamStateMachine stateMachine;

    public PlayerTeamState(TeamStateMachine stateMachine, PlayerTeamSystem team)
    {
        this.stateMachine = stateMachine;
        this.team = team;
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
    public virtual void Exit()
    {
        //Debug.Log($"Exit {StateName()}");
    }
}
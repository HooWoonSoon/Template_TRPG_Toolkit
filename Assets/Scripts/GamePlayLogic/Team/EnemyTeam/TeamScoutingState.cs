public class TeamScoutingState : EnemyTeamState
{
    public TeamScoutingState(TeamStateMachine stateMachine, EnemyTeamSystem team) : base(stateMachine, team)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        team.TeamSouting();
    }
}
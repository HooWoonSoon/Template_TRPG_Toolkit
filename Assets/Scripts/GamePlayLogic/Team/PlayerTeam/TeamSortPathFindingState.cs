public class TeamSortPathFindingState : PlayerTeamState
{
    public TeamSortPathFindingState(TeamStateMachine stateMachine, PlayerTeamSystem team) : base(stateMachine, team)
    {
    }

    public override void Enter()
    {
        base.Enter();
        team.EnableTeamPathFinding();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        if (IsAllReachedTarget())
        {
            stateMachine.ChangeState(team.teamIdleState);
        }
    }

    private bool IsAllReachedTarget()
    {
        foreach (var follower in team.linkMembers)
        {
            if (follower.character.pathRoute != null)
            {
                return false;
            }
        }
        return true;
    }
}

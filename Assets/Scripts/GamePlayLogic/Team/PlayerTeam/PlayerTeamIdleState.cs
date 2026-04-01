using UnityEngine;

public class PlayerTeamIdleState : PlayerTeamState
{
    public PlayerTeamIdleState(TeamStateMachine stateMachine, PlayerTeamSystem team) : base(stateMachine, team)
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
        team.currentLeader.MovementInput(out Vector3 direction);
        if (direction != Vector3.zero)
        {
            team.stateMachine.ChangeState(team.teamActionState);
        }
    }
}
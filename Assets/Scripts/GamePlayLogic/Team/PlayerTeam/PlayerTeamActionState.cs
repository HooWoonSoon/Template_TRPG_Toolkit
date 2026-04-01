using UnityEngine;

public class PlayerTeamActionState : PlayerTeamState
{
    public PlayerTeamActionState(TeamStateMachine stateMachine, PlayerTeamSystem team) : base(stateMachine, team)
    {
    }

    public override void Enter()
    {
        base.Enter();
        //PlayerTeamLinkUIManager.instance.PopInTeamLinkOptionContent();
        CameraController.instance.ChangeFollowTarget(team.currentLeader.transform);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        
        team.currentLeader.MovementInput(out Vector3 direction);
        team.currentLeader.SetMoveDirection(direction);

        bool anyMoving = false;

        for (int i = 0; i < team.linkMembers.Count; i++)
        {
            var character = team.linkMembers[i].character;
            team.FollowWithNearIndexMember(character, team.linkMembers[i].targetToFollow);

            if (character.direction != Vector3.zero)
            {
                character.UpdateHistory();
                anyMoving = true;
            }
        }

        if (direction != Vector3.zero)
        {
            anyMoving = true;
        }

        if (!anyMoving)
        {
            team.stateMachine.ChangeState(team.teamIdleState);
        }
    }
}
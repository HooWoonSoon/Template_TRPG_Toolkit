using UnityEngine;

public class PlayerIdleStateExplore : PlayerBaseState
{
    public PlayerIdleStateExplore(PlayerStateMachine stateMachine, PlayerCharacter character) : base(stateMachine, character)
    {
    }

    public override void Enter()
    {
        base.Enter();
        character.ForceStopVelocity();
        character.YCoordinateAllignment();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override void Update()
    {
        base.Update();

        character.CalculateVelocity();
        character.YCoordinateAllignment();

        if (character.direction != Vector3.zero)
        {
            stateMachine.ChangeState(character.moveStateExplore);
        }
    }
}
using UnityEngine;

public class PlayerMoveStateExplore : PlayerBaseState
{
    public PlayerMoveStateExplore(PlayerStateMachine stateMachine, PlayerCharacter character) : base(stateMachine, character)
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

        character.StepClimbUp(character.direction.x, character.direction.z, character.stepClimbHeight);
        character.Move(character.direction);
        character.CalculateVelocity();
        character.YCoordinateAllignment();

        if (character.direction == Vector3.zero)
        {
            stateMachine.ChangeState(character.idleStateExplore);
        }
    }
}
public class PlayerMovePathStateExplore : PlayerBaseState
{
    public PlayerMovePathStateExplore(PlayerStateMachine stateMachine, PlayerCharacter character) : base(stateMachine, character)
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
        character.PathToTarget();
        if (character.pathRoute == null)
        {
            stateMachine.ChangeState(character.idleStateExplore);
        }
    }
}


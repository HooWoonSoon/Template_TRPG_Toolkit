public class PlayerStateMachine
{
    public PlayerBaseState currentState { get; private set; }

    public void Initialize(PlayerBaseState newState)
    {
        this.currentState = newState;
        this.currentState.Enter();
    }

    public void ChangeState(PlayerBaseState newState)
    {
        if (currentState == newState || currentState == null) { return; }
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
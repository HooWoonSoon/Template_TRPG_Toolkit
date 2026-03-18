public class EnemyStateMachine
{
    public EnemyBaseState currentState { get; private set; }

    public void Initialize(EnemyBaseState roofState)
    {
        this.currentState = roofState;
        this.currentState.Enter();
    }

    public void ChangeState(EnemyBaseState newState)
    {
        if (currentState == newState || currentState == null) { return; }
        currentState.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
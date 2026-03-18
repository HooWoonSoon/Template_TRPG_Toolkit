public class TeamStateMachine
{
    public PlayerTeamState currentPlayerTeamState;
    public EnemyTeamState currentEnemyTeamState;

    public void Initialize(PlayerTeamState newTeamState)
    {
        currentPlayerTeamState = newTeamState;
        currentPlayerTeamState.Enter();
    }

    public void Initialize(EnemyTeamState newEnemyTeamState)
    {
        currentEnemyTeamState = newEnemyTeamState;
        currentEnemyTeamState.Enter();
    }

    public void ChangeState(PlayerTeamState newTeamState)
    {
        if (currentPlayerTeamState == newTeamState || currentPlayerTeamState == null) { return; }
        currentPlayerTeamState.Exit();
        currentPlayerTeamState = newTeamState;
        currentPlayerTeamState.Enter();
    }

    public void ChangeState(EnemyTeamState newEnemyTeamState)
    {
        if (currentEnemyTeamState == newEnemyTeamState || currentEnemyTeamState == null) { return; }
        currentEnemyTeamState.Exit();
        currentEnemyTeamState = newEnemyTeamState;
        currentEnemyTeamState.Enter();
    }
}
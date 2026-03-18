using UnityEngine;
using Tactics.AI;

public class EnemyCharacter : CharacterBase
{
    public EnemyStateMachine stateMechine;
    public EnemyDeploymentState deploymentState { get; private set; }
    public EnemyBattleState battleState { get; private set; }
    public EnemyIdleStateExplore idleStateExplore { get; private set; }

    public DecisionSystem decisionSystem;
    public UtilityAIScoreConfig utilityAI;
    public bool debugMode = false;

    private void Awake()
    {
        stateMechine = new EnemyStateMachine();

        battleState = new EnemyBattleState(stateMechine, this);
        deploymentState = new EnemyDeploymentState(stateMechine, this);

        idleStateExplore = new EnemyIdleStateExplore(stateMechine, this);
    }
    protected override void Start()
    {
        base.Start();
        decisionSystem = new DecisionSystem(world, utilityAI, this);
        stateMechine.Initialize(idleStateExplore);

        GameEvent.onDeploymentStart += () =>
        {
            stateMechine.ChangeState(deploymentState);
        };
    }
    protected override void Update()
    {
        base.Update();
        stateMechine.currentState.Update();
    }

    public override void SetAStarMovePos(Vector3 targetPosition)
    {
        throw new System.NotImplementedException();
    }
    public override void SetAStarMovePos(Vector3Int targetPosition)
    {
        throw new System.NotImplementedException();
    }
    public override void TeleportToNodeDeployble(GameNode targetNode)
    {
        if (targetNode != null)
        {
            SetSelfToNode(targetNode, 0.5f);
            stateMechine.ChangeState(deploymentState);
        }
    }
    public override void TeleportToNodeFree(GameNode targetNode)
    {
        if (targetNode != null)
        {
            SetSelfToNode(targetNode, 0.5f);
            stateMechine.ChangeState(idleStateExplore);
        }
    }

    public override void ReadyBattle()
    {
        stateMechine.ChangeState(battleState);
    }
    public override void ExitBattle()
    {
        stateMechine.ChangeState(idleStateExplore);
    }
}


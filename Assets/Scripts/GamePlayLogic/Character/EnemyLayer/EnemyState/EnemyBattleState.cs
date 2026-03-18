using System;
using UnityEngine;
public enum AgentBattlePhase
{
    Ready, Wait, Thinking, Move, SkillCast,
    ReleaseMoveThinking,
    ReleaseSkillThinking,
    End
}
public class EnemyBattleState : EnemyBaseState
{
    private AgentBattlePhase currentPhase;
    private float phaseStartTime;
    private GameNode confirmMoveNode;

    private GameNode targetNode;
    private SkillData currentSkill;
    private Orientation orientation;

    private bool movedConfirmed = false;
    private bool skillCastConfirmed = false;

    private bool freezeState = false;

    public EnemyBattleState(EnemyStateMachine stateMachine, EnemyCharacter character) : base(stateMachine, character)
    {
    }

    public override void Enter()
    {
        base.Enter();
        character.ResetVisualTilemap();
        currentPhase = AgentBattlePhase.Ready;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
        phaseStartTime += Time.deltaTime;

        switch (currentPhase)
        {
            case AgentBattlePhase.Ready:
                character.PathToTarget();
                if (character.pathRoute == null)
                {
                    ChangePhase(AgentBattlePhase.Wait);
                }
                break;
            case AgentBattlePhase.Wait:
                if (character.IsYourTurn(character))
                {
                    ChangePhase(AgentBattlePhase.Thinking);
                }
                break;
            case AgentBattlePhase.Thinking:
                if (phaseStartTime > 0.5f && !freezeState)
                {
                    if (character.pathRoute != null && character.pathRoute.pathNodeVectorList.Count > 0)
                        ChangePhase(AgentBattlePhase.Move);
                    else if (currentSkill != null)
                        ChangePhase(AgentBattlePhase.SkillCast);
                    else
                        ChangePhase(AgentBattlePhase.End);
                }
                break;
            case AgentBattlePhase.ReleaseMoveThinking:
                if (phaseStartTime > 0.5f && !freezeState)
                {
                    if (character.pathRoute != null && character.pathRoute.pathNodeVectorList.Count > 0
                        )
                        ChangePhase(AgentBattlePhase.Move);
                    else
                        ChangePhase(AgentBattlePhase.End);
                }
                break;
            case AgentBattlePhase.Move:
                character.PathToTarget();
                if (character.pathRoute == null)
                {
                    movedConfirmed = true;
                    if (!skillCastConfirmed && currentSkill != null)
                        ChangePhase(AgentBattlePhase.SkillCast);
                    else if (!skillCastConfirmed && currentSkill == null)
                        ChangePhase(AgentBattlePhase.ReleaseSkillThinking);
                    else
                        ChangePhase(AgentBattlePhase.End);
                }
                break;
            case AgentBattlePhase.ReleaseSkillThinking:
                if (phaseStartTime > 0.5)
                {
                    if (currentSkill != null)
                        ChangePhase(AgentBattlePhase.SkillCast);
                    else
                        ChangePhase(AgentBattlePhase.End);
                }
                break;
            case AgentBattlePhase.SkillCast:
                skillCastConfirmed = true;
                if (freezeState) return;

                if (!movedConfirmed)
                    ChangePhase(AgentBattlePhase.ReleaseMoveThinking);
                else
                {
                    ChangePhase(AgentBattlePhase.End);
                }
                break;
            case AgentBattlePhase.End:
                if (freezeState) return;

                if (phaseStartTime > 0.5f)
                {
                    BattleManager.instance.OnLoadNextTurn();
                    ChangePhase(AgentBattlePhase.Wait);
                }
                break;
        }
    }

    public void ChangePhase(AgentBattlePhase newPhase)
    {
        ExitPhase(currentPhase);
        currentPhase = newPhase;
        phaseStartTime = 0;
        EnterPhase(newPhase);
        if (character.debugMode)
            Debug.Log($"{character} enter to {newPhase}");
    }

    public void EnterPhase(AgentBattlePhase phase)
    {
        switch (phase)
        {
            case AgentBattlePhase.Ready:
                break;
            case AgentBattlePhase.Wait:
                confirmMoveNode = null;
                currentSkill = null;
                targetNode = null;
                movedConfirmed = false;
                skillCastConfirmed = false;
                break;
            case AgentBattlePhase.Thinking:
                try
                {
                    freezeState = true;
                    Thinking(true, true, () =>
                    {
                        CameraController.instance.ChangeFollowTarget(character.transform);
                        character.decisionSystem.GetResult(out currentSkill, out confirmMoveNode,
                        out targetNode, out orientation);

                        if (confirmMoveNode != null)
                        {
                            character.SetPathRoute(confirmMoveNode);
                        }
                    });
                }
                finally
                {
                    freezeState = false;
                }
                break;
            case AgentBattlePhase.ReleaseMoveThinking:
                try
                {
                    freezeState = true;
                    Thinking(true, false, () =>
                    {
                        CameraController.instance.ChangeFollowTarget(character.transform);
                        character.decisionSystem.GetResult(out currentSkill, out confirmMoveNode, 
                            out targetNode, out orientation);

                        if (confirmMoveNode != null)
                        {
                            character.SetPathRoute(confirmMoveNode);
                        }
                    });
                }
                finally
                {
                    freezeState = false;
                }
                break;
            case AgentBattlePhase.Move:
                if (confirmMoveNode != null)
                {
                    character.ShowDangerMovableAndTargetTilemap(confirmMoveNode);
                    CameraController.instance.ChangeFollowTarget(character.transform);
                }
                break;
            case AgentBattlePhase.SkillCast:
                if (currentSkill != null)
                {
                    freezeState = true;
                    character.ShowSkillTargetTilemap(character.currentNode, targetNode, currentSkill);
                    BattleManager.instance.CastSkill(character, currentSkill, confirmMoveNode, 
                        targetNode, () => { freezeState = false; });
                }
                break;
            case AgentBattlePhase.End:
                freezeState = true;
                character.ResetVisualTilemap();
                BattleManager.instance.SetupOrientationArrow(character, character.currentNode);
                BattleManager.instance.SwitchToOrientationWithArrow(character, orientation, 0.05f, 
                    () => { freezeState = false; });
                break;
        }
    }
    public void ExitPhase(AgentBattlePhase phase)
    {
        switch (phase)
        {
            case AgentBattlePhase.End:
                BattleManager.instance.HideOrientationArrow();
                break;
        }
    }

    public void Thinking(bool allowMove = true, bool allowSkill = true, Action onFinish = null)
    {
        character.decisionSystem.MakeDecision(allowMove, allowSkill);
        onFinish?.Invoke();
    }
}
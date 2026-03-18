using UnityEngine;
using System;
using System.Collections.Generic;

public enum PlayerBattlePhase
{
    Ready, Wait, MoveComand, SkillComand, SkillTarget, Move, SkillCast,
    ReleaseMoveComand,
    ReleaseSkillComandViewMode, ReleaseSkillComand, ReleaseSkillComandEnd, 
    End
}

public class PlayerBattleState : PlayerBaseState
{
    private PlayerBattlePhase currentPhase;

    private Action changedHandler;

    private GameNode confirmMoveNode;
    private GameNode targetNode;
    private SkillData selectedSkill;

    private bool moveTargetConfirmed = false;
    private bool skillCastConfirmed = false;
    private bool endTurnConfirmed = false;
    private bool movedConfirmed = false;

    private bool freezeState = false;

    public PlayerBattleState(PlayerStateMachine stateMachine, PlayerCharacter character) : base(stateMachine, character)
    {
    }

    public override void Enter()
    {
        base.Enter();
        character.ResetVisualTilemap();
        currentPhase = PlayerBattlePhase.Ready;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        switch (currentPhase)
        {
            case PlayerBattlePhase.Ready:
                character.PathToTarget();
                if (character.pathRoute == null)
                {
                    ChangePhase(PlayerBattlePhase.Wait);
                }
                break;
            case PlayerBattlePhase.Wait:
                if (character.IsYourTurn(character))
                {
                    ChangePhase(PlayerBattlePhase.MoveComand);
                }
                break;
            case PlayerBattlePhase.MoveComand:
                if (BattleManager.instance.IsSelectedNodeChange())
                {
                    GameNode selectedNode = BattleManager.instance.GetSelectedGameNode();
                    BattleManager.instance.ShowPathLine(character, 
                        character.GetCharacterNodePos(), selectedNode.GetNodeVector());

                    List<GameNode> movableNodes = character.GetMovableNodes();
                    character.ShowDangerMovableAndTargetTilemap(selectedNode, movableNodes);
                    CTTurnUIManager.instance.TargetCursorNodeCharacterUI(selectedNode);
                    
                    if (selectedNode.character != null)
                    {
                        BattleUIManager.instance.SwitchInfoPanel();
                    }
                    else if (character.IsInMovableRange(selectedNode, movableNodes) && 
                        selectedNode.character == null)
                    {
                        BattleUIManager.instance.SwitchActionPanel();
                    }
                    else if (!character.IsInMovableRange(selectedNode, movableNodes) 
                        && selectedNode.character == null)
                    {
                        BattleUIManager.instance.OffCursorPanel();
                    }
                }

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    BattleManager.instance.SetGridCursorAt(character.GetCharacterTransformToNode());
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    GameNode moveTargetNode = BattleManager.instance.GetSelectedGameNode();
                    if (!character.IsInMovableRange(moveTargetNode)) return;

                    confirmMoveNode = moveTargetNode;
                    if (confirmMoveNode != character.GetCharacterTransformToNode())
                    {
                        moveTargetConfirmed = true;
                    }
                    BattleManager.instance.GeneratePreviewCharacterInMovableRange(character);
                    ChangePhase(PlayerBattlePhase.SkillComand);
                }
                else if (Input.GetKeyDown(KeyCode.Z))
                {
                    confirmMoveNode = character.GetCharacterTransformToNode();
                    endTurnConfirmed = true;
                    ChangePhase(PlayerBattlePhase.End);
                }
                break;
            case PlayerBattlePhase.SkillComand:
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    if (moveTargetConfirmed)
                    {
                        moveTargetConfirmed = false;
                    }
                    ChangePhase(PlayerBattlePhase.MoveComand);
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (BattleManager.instance.IsValidSkillSelection(character, selectedSkill))
                    {
                        ChangePhase(PlayerBattlePhase.SkillTarget);
                    }
                    else
                    {
                        Debug.Log("Invalid Skill Selection");
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Z))
                {
                    endTurnConfirmed = true;
                    if (moveTargetConfirmed)
                        ChangePhase(PlayerBattlePhase.Move);
                    else
                        ChangePhase(PlayerBattlePhase.ReleaseSkillComandEnd);
                }
                break;
            case PlayerBattlePhase.SkillTarget:
                if (BattleManager.instance.IsSelectedNodeChange())
                {
                    GameNode selectedNode = BattleManager.instance.GetSelectedGameNode();
                    targetNode = character.GetSkillTargetShowTilemap(confirmMoveNode, selectedNode, selectedSkill);
                    
                    if (confirmMoveNode != null)
                    {
                        BattleManager.instance.ShowProjectileParabola(character, selectedSkill, confirmMoveNode, selectedNode);
                    }

                    if (selectedNode.character != null)
                    {
                        BattleUIManager.instance.SwitchInfoPanel();
                    }
                    else if (selectedNode.character == null)
                    {
                        BattleUIManager.instance.OffCursorPanel();
                    }
                }
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    if (!movedConfirmed)
                        ChangePhase(PlayerBattlePhase.SkillComand);
                    else
                        ChangePhase(PlayerBattlePhase.ReleaseSkillComand);
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (BattleManager.instance.IsValidSkillTarget(character, selectedSkill, confirmMoveNode, targetNode))
                    {
                        if (moveTargetConfirmed)
                            ChangePhase(PlayerBattlePhase.Move);
                        else
                            ChangePhase(PlayerBattlePhase.SkillCast);
                    }
                }
                break;
            case PlayerBattlePhase.ReleaseMoveComand:
                if (BattleManager.instance.IsSelectedNodeChange())
                {
                    GameNode selectedNode = BattleManager.instance.GetSelectedGameNode();
                    BattleManager.instance.ShowPathLine(character, 
                        character.GetCharacterNodePos(), selectedNode.GetNodeVector());
                    character.ShowDangerMovableAndTargetTilemap(selectedNode);
                    CTTurnUIManager.instance.TargetCursorNodeCharacterUI(selectedNode);
                }
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z))
                {
                    GameNode moveTargetNode = BattleManager.instance.GetSelectedGameNode();
                    if (!character.IsInMovableRange(moveTargetNode)) return;

                    confirmMoveNode = moveTargetNode;
                    if (confirmMoveNode != character.GetCharacterTransformToNode())
                    {
                        moveTargetConfirmed = true;
                    }
                    BattleManager.instance.GeneratePreviewCharacterInMovableRange(character);
                    if (moveTargetConfirmed)
                        ChangePhase(PlayerBattlePhase.Move);
                    else
                        ChangePhase(PlayerBattlePhase.End);
                }
                break;
            case PlayerBattlePhase.Move:
                character.PathToTarget();
                if (!skillCastConfirmed && !endTurnConfirmed)
                {
                    if (character.pathRoute == null)
                    {
                        ChangePhase(PlayerBattlePhase.SkillCast);
                    }
                }
                else if (!skillCastConfirmed && moveTargetConfirmed && endTurnConfirmed)
                {
                    if (character.pathRoute == null)
                    {
                        ChangePhase(PlayerBattlePhase.ReleaseSkillComandEnd);
                    }
                }
                else if (skillCastConfirmed)
                {
                    if (character.pathRoute == null)
                    {
                        ChangePhase(PlayerBattlePhase.End);
                    }
                }
                break;
            case PlayerBattlePhase.ReleaseSkillComandViewMode:
                if (BattleManager.instance.IsSelectedNodeChange())
                {
                    GameNode selectedNode = BattleManager.instance.GetSelectedGameNode();
                    CTTurnUIManager.instance.TargetCursorNodeCharacterUI(selectedNode);

                    if (selectedNode.character != null)
                        BattleUIManager.instance.SwitchInfoPanel();
                    if (selectedNode.character == null)
                        BattleUIManager.instance.OffCursorPanel();
                }
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    ChangePhase(PlayerBattlePhase.ReleaseSkillComand);
                }
                break;
            case PlayerBattlePhase.ReleaseSkillComand:
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ChangePhase(PlayerBattlePhase.ReleaseSkillComandViewMode);
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    ChangePhase(PlayerBattlePhase.SkillTarget);
                }
                else if (Input.GetKeyDown(KeyCode.Z))
                {
                    endTurnConfirmed = true;
                    ChangePhase(PlayerBattlePhase.ReleaseSkillComandEnd);
                }
                break;
            case PlayerBattlePhase.SkillCast:;
                if (!freezeState)
                {
                    SkillCastEnd();
                }
                break;
            case PlayerBattlePhase.ReleaseSkillComandEnd:
                if (BattleManager.instance.IsOrientationChanged())
                {
                    Orientation orientation = BattleManager.instance.GetSelectedOrientation();
                    character.SetTransfromOrientation(orientation);
                }
                else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    endTurnConfirmed = false;
                    BattleManager.instance.HideOrientationArrow();
                    ChangePhase(PlayerBattlePhase.ReleaseSkillComand);
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    BattleManager.instance.HideOrientationArrow();
                    BattleManager.instance.OnLoadNextTurn();
                    ChangePhase(PlayerBattlePhase.Wait);
                }
                break;
            case PlayerBattlePhase.End:
                if (BattleManager.instance.IsOrientationChanged())
                {
                    Orientation orientation = BattleManager.instance.GetSelectedOrientation();
                    character.SetTransfromOrientation(orientation);
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if (!movedConfirmed && skillCastConfirmed)
                    {
                        ChangePhase(PlayerBattlePhase.ReleaseMoveComand);
                    }
                    else if (movedConfirmed && !skillCastConfirmed)
                    {
                        ChangePhase(PlayerBattlePhase.ReleaseSkillComand);
                    }
                    else if (!movedConfirmed && !skillCastConfirmed)
                    {
                        ChangePhase(PlayerBattlePhase.MoveComand);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    BattleManager.instance.HideOrientationArrow();
                    BattleManager.instance.OnLoadNextTurn();
                    ChangePhase(PlayerBattlePhase.Wait);
                }
                break;
        }
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
        character.CharacterPassWay();
    }

    public void ChangePhase(PlayerBattlePhase newPhase)
    {
        ExitPhase(currentPhase);
        currentPhase = newPhase;
        EnterPhase(newPhase);
        if (character.debugMode)
            Debug.Log($"{character} Enter {currentPhase}");
    }

    public void ExitPhase(PlayerBattlePhase phase)
    {
        switch (phase)
        {
            case PlayerBattlePhase.SkillComand:
                GameEvent.onListOptionChanged -= changedHandler;
                BattleManager.instance.CloseAllProjectileParabola();
                BattleUIManager.instance.CloseSkillUI();
                break;
            case PlayerBattlePhase.SkillTarget:
                BattleManager.instance.CloseAllProjectileParabola();
                break;
            case PlayerBattlePhase.Move:
                BattleManager.instance.ClosePathLine();
                break;
            case PlayerBattlePhase.ReleaseSkillComand:
                GameEvent.onListOptionChanged -= changedHandler;
                BattleManager.instance.CloseAllProjectileParabola();
                BattleUIManager.instance.CloseSkillUI();
                break;
            case PlayerBattlePhase.SkillCast:
                break;
            case PlayerBattlePhase.End:
                BattleManager.instance.HideOrientationArrow();
                break;
        }
    }

    private void EnterPhase(PlayerBattlePhase phase)
    {
        switch (phase)
        {
            case PlayerBattlePhase.Ready:
                break;
            case PlayerBattlePhase.Wait:
                confirmMoveNode = null;
                targetNode = null;
                selectedSkill = null;
                moveTargetConfirmed = false;
                skillCastConfirmed = false;
                movedConfirmed = false;
                endTurnConfirmed = false;
                break;
            case PlayerBattlePhase.MoveComand:
                character.ShowDangerAndMovableTileFromNode();
                BattleManager.instance.SetGridCursorAt(character.GetCharacterTransformToNode());

                //  If get return to move command phase
                BattleManager.instance.DestroyPreviewModel();
                BattleUIManager.instance.CloseSkillUI();
                BattleUIManager.instance.ActiveAllCharacterInfoTip(false);
                break;
            case PlayerBattlePhase.SkillComand:
                SkillComandInstruction();
                break;
            case PlayerBattlePhase.SkillTarget:
                BattleManager.instance.ActivateMoveCursorAndHide(true, false);
                BattleManager.instance.ShowOppositeTeamParabola(character, confirmMoveNode);
                BattleUIManager.instance.ActiveAllCharacterInfoTip(true);

                GameNode selectedNode = BattleManager.instance.GetSelectedGameNode();
                targetNode = character.GetSkillTargetShowTilemap(confirmMoveNode, selectedNode, selectedSkill);
                break;
            case PlayerBattlePhase.ReleaseMoveComand:
                character.ShowDangerAndMovableTileFromNode();
                BattleManager.instance.SetGridCursorAt(character.GetCharacterTransformToNode());
                BattleUIManager.instance.ActiveAllCharacterInfoTip(false);
                break;
            case PlayerBattlePhase.Move:
                BattleManager.instance.ActivateMoveCursorAndHide(false, true);
                BattleUIManager.instance.CloseSkillUI();
                BattleUIManager.instance.ActiveAllCharacterInfoTip(false);
                CameraController.instance.ChangeFollowTarget(character.transform);
                character.SetPathRoute(confirmMoveNode);
                character.ShowDangerMovableAndTargetTilemap(confirmMoveNode);
                movedConfirmed = true;
                break;
            case PlayerBattlePhase.ReleaseSkillComandViewMode:
                BattleManager.instance.ActivateMoveCursorAndHide(true, false);
                BattleUIManager.instance.CloseSkillUI();
                break;
            case PlayerBattlePhase.ReleaseSkillComand:
                SkillComandInstruction();
                break;
            case PlayerBattlePhase.SkillCast:
                CastSkillInstruction();
                break;
            case PlayerBattlePhase.ReleaseSkillComandEnd:
                EndInstruction();
                break;
            case PlayerBattlePhase.End:
                EndInstruction();
                break;
        }
    }

    private void SkillComandInstruction()
    {
        CTTurnUIManager.instance.TargetCursorCharacterUI(character);
        BattleManager.instance.SetGridCursorAt(confirmMoveNode);
        BattleManager.instance.ActivateMoveCursorAndHide(false, false);
        BattleManager.instance.ShowOppositeTeamParabola(character, confirmMoveNode);
        BattleUIManager.instance.OffCursorPanel();
        BattleUIManager.instance.OpenUpdateSkillUI(character);
        BattleUIManager.instance.ActiveAllCharacterInfoTip(true);

        changedHandler = () =>
        {
            selectedSkill = SkillUIManager.instance.GetCurrentSelectedSkill();
            character.ShowSkillTilemap(confirmMoveNode, selectedSkill);
        };
        GameEvent.onListOptionChanged += changedHandler;
        //  First designate skill
        selectedSkill = SkillUIManager.instance.GetCurrentSelectedSkill();

        character.ShowSkillTilemap(confirmMoveNode, selectedSkill);
    }

    private void CastSkillInstruction()
    {
        freezeState = true;
        character.ShowSkillTargetTilemap(confirmMoveNode, targetNode, selectedSkill);
        BattleManager.instance.DestroyPreviewModel();
        BattleManager.instance.CastSkill(character, selectedSkill, confirmMoveNode, 
            targetNode, () => { freezeState = false; });
    }
    private void SkillCastEnd()
    {
        if (moveTargetConfirmed && movedConfirmed)
        {
            endTurnConfirmed = true;
            skillCastConfirmed = true;
            ChangePhase(PlayerBattlePhase.End);
        }
        else if (!moveTargetConfirmed && !movedConfirmed)
        {
            skillCastConfirmed = true;
            ChangePhase(PlayerBattlePhase.ReleaseMoveComand);
        }
    }

    private void EndInstruction()
    {
        character.ResetVisualTilemap();
        CameraController.instance.ChangeFollowTarget(character.transform);
        BattleManager.instance.ActivateMoveCursorAndHide(false, true);
        BattleManager.instance.SetupOrientationArrow(character, confirmMoveNode);
        BattleManager.instance.DestroyPreviewModel();
        BattleManager.instance.ClosePathLine();
        BattleUIManager.instance.ActiveAllCharacterInfoTip(false);
        BattleUIManager.instance.OffCursorPanel();
    }
}
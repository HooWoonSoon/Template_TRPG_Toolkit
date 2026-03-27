using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : Entity
{
    #region Team
    public enum TeamStatus { Active, Defeated }
    private List<TeamDeployment> battleTeams;

    private Dictionary<TeamDeployment, TeamStatus> oppositeTeamStatus;
    private Dictionary<TeamDeployment, TeamStatus> allyTeamStatus;
    private Dictionary<TeamDeployment, TeamStatus> playerTeamStatus;
    #endregion

    private List<CharacterBase> joinedBattleUnits = new List<CharacterBase>();
    private List<CharacterBase> knockOutCharacter = new List<CharacterBase>();

    public bool isBattleStarted = false;

    [Header("Grid Gizmos")]
    public GridCursor gridCursor;
    private GameNode lastSelectedNode;

    [Header("Path Line Gizmos")]
    public PathRenderer pathRenderer;

    [Header("Orientation")]
    public BattleOrientationArrow orientationArrow;
    private Orientation lastOrientation;

    [Header("Preview")]
    public Material previewMaterial;
    private GameObject previewCharacter;

    public event Action onConfrimCallback;
    public event Action onCancelCallback;

    public bool debugMode = false;
    public static BattleManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }
    protected override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnEnter();
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            OnCancel();
        }
    }

    #region Event Callback
    private void OnEnter()
    {
        onConfrimCallback?.Invoke();
        onConfrimCallback = null;
    }
    private void OnCancel()
    {
        onConfrimCallback = null;
        onCancelCallback?.Invoke();
        onCancelCallback = null;
    }
    #endregion

    #region Setup Battle
    public void SetJoinedBattleUnit(HashSet<CharacterBase> joinedBattleUnits)
    {
        SetJoinedBattleUnit(joinedBattleUnits.ToList());
    }
    public void SetJoinedBattleUnit(List<CharacterBase> joinedBattleUnits)
    {
        this.joinedBattleUnits = joinedBattleUnits;
        GameEvent.onBattleUnitKnockout += HandleUnitKnockout;
    }
    #endregion
    
    #region Battle Unit Refine Path
    private void EnterBattleUnitRefinePath()
    {
        //pathFindingJobThread.Initialize();
        List<PathRoute> pathRoutes = GetBattleUnitRefinePath();

        for (int i = 0; i < joinedBattleUnits.Count; i++)
        {
            joinedBattleUnits[i].ReadyBattle();
        }
        for (int i = 0; i < pathRoutes.Count; i++)
        {
            pathRoutes[i].pathFinder.SetPathRoute(pathRoutes[i]);
        }
    }
    private List<PathRoute> GetBattleUnitRefinePath()
    {
        List<PathRoute> pathRoutes = new List<PathRoute>();
        HashSet<Vector3Int> occupiedPos = new HashSet<Vector3Int>();
        foreach (CharacterBase character in joinedBattleUnits)
        {
            int iteration = 1;
            bool found = false;
            Vector3Int unitPosition = character.GetCharacterNodePos();
            while (iteration <= 16 && !found)
            {
                List<Vector3Int> optionPos = character.GetCustomizedSizeMovablePos(iteration, occupiedPos);
                List<Vector3Int> sortPos = Utils.SortTargetRangeByDistance(unitPosition, optionPos);

                for (int i = 0; i < sortPos.Count; i++)
                {
                    Vector3Int target = sortPos[i];
                    if (occupiedPos.Contains(target)) { continue; }

                    PathRoute route = pathFinding.GetPathRoute(unitPosition, target, character, 1, 1);
                    if (route == null || route.pathNodeVectorList == null || route.pathNodeVectorList.Count == 0)
                        continue;

                    pathRoutes.Add(new PathRoute
                    {
                        pathFinder = character,
                        targetPosition = sortPos[i],
                        pathNodeVectorList = new List<Vector3>(route.pathNodeVectorList),
                        pathIndex = 0
                    });
                    occupiedPos.Add(sortPos[i]);
                    found = true;
                    //Debug.Log($"Adding PathRoute for {character.name} to {sortPos[i]}, route count {route.pathRouteList.Count}");

                    break;
                }
                iteration++;
            }

            if (!found)
            {
                Debug.LogError($"{character.name} Not Found Path, Origin Position {unitPosition}");
                return null;
            }
        }
        return pathRoutes;
    }
    #endregion
    
    #region Direct Battle
    public void PreapreBattleContent()
    {
        if (isBattleStarted) { return; }
        FindJoinedTeam();
        EnterBattleUnitRefinePath();
        BattleUIManager.instance.PrepareBattleUI();
        CTTimeline.instance.SetJoinedBattleUnit(joinedBattleUnits);
        CTTimeline.instance.SetupTimeline();
        GameEvent.onBattleUIFinish += () =>
        {
            GameEvent.onBattleStart?.Invoke();
            isBattleStarted = true;
        };
    }
    private void FindJoinedTeam()
    {
        battleTeams = new List<TeamDeployment>();

        oppositeTeamStatus = new Dictionary<TeamDeployment, TeamStatus>();
        allyTeamStatus = new Dictionary<TeamDeployment, TeamStatus>();
        playerTeamStatus = new Dictionary<TeamDeployment, TeamStatus>();

        foreach (CharacterBase character in joinedBattleUnits)
        {
            TeamDeployment team = character.currentTeam;
            if (!battleTeams.Contains(team))
            {
                battleTeams.Add(team);
                switch (team.teamType)
                {
                    case TeamType.Player:
                        if (!playerTeamStatus.ContainsKey(team))
                            playerTeamStatus.Add(team, TeamStatus.Active);
                        break;
                    case TeamType.Opposite:
                        if (!oppositeTeamStatus.ContainsKey(team))
                            oppositeTeamStatus.Add(team, TeamStatus.Active);
                        break;
                    case TeamType.Allay:
                        if (!allyTeamStatus.ContainsKey(team))
                            allyTeamStatus.Add(team, TeamStatus.Active);
                        break;
                }
            }
        }
    }
    #endregion
    
    #region Battle Request
    public void RequestBattle(List<CharacterBase> allBattleCharacter, 
        Action confirmAction = null, Action cancelAction = null)
    {
        onConfrimCallback = () =>
        {
            MapDeploymentManager.instance.CreateTempoTeam();
            SetJoinedBattleUnit(allBattleCharacter);
            if (debugMode)
            {
                string allJoined = string.Join("All Battle Character", allBattleCharacter.ConvertAll(c => c.ToString()));
                Debug.Log(allJoined);
            }
            PreapreBattleContent();
            confirmAction?.Invoke();
        };

        onCancelCallback = () =>
        {
            cancelAction?.Invoke();
        };
    }
    public void ClearEventCallback(Action action = null)
    {
        onConfrimCallback = null;
        onCancelCallback = null;
        action?.Invoke();
    }
    #endregion

    public void CheckBattleState()
    {
        foreach (TeamDeployment team in battleTeams)
        {
            bool allKnockedOut = true;

            foreach (CharacterBase character in team.teamCharacter)
            {
                if (!knockOutCharacter.Contains(character))
                {
                    allKnockedOut = false;
                    break;
                }
            }

            if (allKnockedOut)
            {
                switch (team.teamType)
                {
                    case TeamType.Player:
                        if (playerTeamStatus.ContainsKey(team))
                        {
                            playerTeamStatus[team] = TeamStatus.Defeated;
                            BattleDefeat();
                        }
                        break;
                    case TeamType.Opposite:
                        if (oppositeTeamStatus.ContainsKey(team))
                            oppositeTeamStatus[team] = TeamStatus.Defeated;
                        break;
                    case TeamType.Allay:
                        if (allyTeamStatus.ContainsKey(team))
                            allyTeamStatus[team] = TeamStatus.Defeated;
                        break;
                }
                if (debugMode)
                    Debug.Log($"{team} has lost the battle!");
            }
        }

        bool allOppositeDefeated = oppositeTeamStatus.Values.All(status => status == TeamStatus.Defeated);
        if (allOppositeDefeated)
        {
            BattleVictory();
        }
    }

    public void BattleVictory()
    {
        if (debugMode)
            Debug.Log("Battle Victory");
        ActivateMoveCursorAndHide(false, true);
        HideOrientationArrow();
        CTTimeline.instance.EndTimeline();

        BattleUIManager.instance.CompleteBattleUI();
        BattleUIManager.instance.ActiveAllCharacterInfoTip(false);

        foreach (CharacterBase character in joinedBattleUnits)
        {
            character.ExitBattle();
        }
        EndBattle();
    }
    public void BattleDefeat()
    {
        if (debugMode)
            Debug.Log("Battle Defeat");
    }

    public void EndBattle()
    {
        GameEvent.onBattleEnd?.Invoke();
        isBattleStarted = false;
        battleTeams = new List<TeamDeployment>();
        joinedBattleUnits.Clear();
        GridTilemapVisual.instance.SetAllTileSprite(GameNode.TilemapSprite.None);
        ActivateMoveCursorAndHide(false, true);
    }

    #region Cursor Gizmos
    public void ActivateMoveCursorAndHide(bool allowControl, bool hide)
    {
        gridCursor.ActivateMoveCursor(allowControl, hide);
    }
    public void SetGridCursorAt(GameNode target)
    {
        gridCursor.SetGridCursorAt(target);
        CTTurnUIManager.instance.TargetCursorNodeCharacterUI(target);
    }
    public GameNode GetSelectedGameNode()
    {
        return gridCursor.currentNode;
    }
    public bool IsSelectedNodeChange()
    {
        if (gridCursor.currentNode != lastSelectedNode)
        {
            lastSelectedNode = gridCursor.currentNode;
            return true;
        }
        return false;
    }
    #endregion

    #region Path Line Gizmos
    public void ShowPathLine(CharacterBase character, Vector3 start, Vector3 end)
    {
        if (pathRenderer == null)
        {
            Debug.LogWarning("Path Renderer is null!"); return;
        }
        PathRoute pathRoute = pathFinding.GetPathRoute(start, end, character, 1, 1);
        if (pathRoute == null)
        {
            Debug.LogWarning("PathRoute is null!");
            pathRenderer.ClearPath();
            return;
        }
        if (pathRoute.pathNodeVectorList == null || pathRoute.pathNodeVectorList.Count == 0)
        {
            Debug.LogWarning("PathNode Vector List is null or empty!");
            pathRenderer.ClearPath();
            return;
        }
        pathRenderer.RenderPath(pathRoute.pathNodeVectorList);

        //Testing
        //pathFindingJobThread.Initialize();
        //PathRoute pathRoute = pathFindingJobThread.GetPathRoute(start, end, character, 1, 1);
        //if (pathRoute == null)
        //{
        //    Debug.LogWarning("PathRoute is null!");
        //    pathRenderer.ClearPath();
        //    return;
        //}
        //if (pathRoute.pathNodeVectorList == null || pathRoute.pathNodeVectorList.Count == 0)
        //{
        //    Debug.LogWarning("PathNode Vector List is null or empty!");
        //    pathRenderer.ClearPath();
        //    return;
        //}
        //pathRenderer.RenderPath(pathRoute.pathNodeVectorList);
    }
    public void ClosePathLine()
    {
        pathRenderer.ClearPath();
    }
    #endregion

    #region Skill Selection
    public bool IsValidSkillSelection(CharacterBase character, SkillData selectedSkill)
    {
        if (selectedSkill == null)
        {
            Debug.LogWarning("Missing selected Skill");
            return false;
        }

        int skillRequireMP = selectedSkill.MPAmount;
        if (character.currentMental < skillRequireMP)
            return false;
        else
            return true;
    }
    #endregion

    #region Skill Target
    public bool IsValidSkillTarget(CharacterBase character,
        SkillData currentSkill, GameNode characterMoveTargetNode, GameNode targetNode)
    {
        if (targetNode == null) { return false; }

        CharacterBase targetCharacter = targetNode.GetUnitGridCharacter();

        if (characterMoveTargetNode != null && targetNode == character.currentNode)
        {
            targetCharacter = null;
            Debug.LogWarning("Invalid node, character leave already");
        }

        //  If character has ready to move and his skill target node is same with his move node.
        //  Its meant his skill target is himself.
        if (characterMoveTargetNode != null && characterMoveTargetNode == targetNode)
            targetCharacter = character;

        if (targetCharacter == null) { return false; }

        TeamDeployment team = character.currentTeam;

        bool isAlly = team.teamCharacter.Contains(targetCharacter);

        if (currentSkill == null)
        {
            Debug.LogWarning($"CurrentSkill is null! Character = {character.name}");
            return false;
        }
        switch (currentSkill.skillTargetType)
        {
            case SkillTargetType.Self:
                return targetCharacter == character;

            case SkillTargetType.Both:
                return true;

            case SkillTargetType.Opposite:
                if (isAlly)
                {
                    Debug.Log("Invalid Target - Same team member");
                    return false;
                }
                return true;

            case SkillTargetType.Our:
                if (!isAlly)
                {
                    Debug.Log("Invalid Target - Non team member");
                    return false;
                }
                return true;

            default:
                Debug.LogWarning("Not define target");
                return false;
        }
    }
    #endregion

    #region Preview Parabola
    /// <summary>
    /// Show projectile parabola form origin node position to target node position, 
    /// if input current skill aren't equal projectile series skill it would happend any thing.
    /// </summary>
    public void ShowProjectileParabola(CharacterBase selfCharacter,
        SkillData currentSkill, GameNode originNode, GameNode targetNode,
        bool forceOffset = false)
    {
        if (currentSkill == null || !currentSkill.isProjectile)
        {
            CloseProjectileParabola(selfCharacter);
            return;
        }

        if (!TryGetParabolaData(selfCharacter, originNode, targetNode, forceOffset,
            out ParabolaRenderer parabola, out Vector3 originPos, out Vector3 targetPos))
            return;

        parabola.DrawProjectileVisual(originPos + new Vector3(0, 1.5f, 0), targetPos, currentSkill.initialElevationAngle);
    }
    /// <summary>
    /// Find selfCharacter skill responsitory, if include any projectile series skill then
    /// show projectile parabola form origin node position to target node position, overwise it would
    /// happend any thing.
    /// </summary>
    public void ShowAnyProjectileParabola(CharacterBase selfCharacter,
        GameNode originNode, GameNode targetNode,
        bool checkInRange = false, bool forceOffset = false)
    {
        if (originNode == null)
        {
            Debug.LogWarning("Missing originNode");
            return; 
        }

        if (!TryGetParabolaData(selfCharacter, originNode, targetNode, forceOffset,
            out ParabolaRenderer parabola, out Vector3 originPos, out Vector3 targetPos))
            return;

        foreach (SkillData skill in selfCharacter.skillDatas)
        {
            if (!skill.isProjectile)
                continue;

            if (checkInRange)
            {
                List<GameNode> influenceNodes = skill.GetInflueneNode(world, originNode);
                if (!influenceNodes.Contains(targetNode))
                    continue;
            }

            parabola.DrawProjectileVisual(originPos + new Vector3(0, 1.5f, 0), targetPos, skill.initialElevationAngle);
            return;
        }
    }
    private bool TryGetParabolaData(CharacterBase selfCharacter,
    GameNode originNode, GameNode targetNode, bool forceOffset,
    out ParabolaRenderer parabolaRenderer, out Vector3 originPos, out Vector3 targetPos)
    {
        LineRenderer lineRenderer = selfCharacter.GetComponentInChildren<LineRenderer>();
        parabolaRenderer = null;

        originPos = Vector3.zero;
        targetPos = Vector3.zero;

        if (lineRenderer == null) return false;

        parabolaRenderer = new ParabolaRenderer(world, lineRenderer);

        if (targetNode == null)
        {
            Debug.Log("No obtained node");
            return false;
        }

        if (parabolaRenderer == null)
        {
            Debug.LogWarning("Missing Parabola Component in character");
            return false;
        }

        if (originNode == null)
        {
            originNode = selfCharacter.GetCharacterTransformToNode();
        }

        originPos = originNode.GetNodeVector();
        targetPos = targetNode.GetNodeVector();

        CharacterBase targetCharacter = targetNode.GetUnitGridCharacter();
        if (targetCharacter != null)
            targetPos = targetCharacter.transform.position + new Vector3(0, 1.5f, 0);
        else if (forceOffset)
            targetPos += new Vector3(0, 1.5f, 0);

        if (originPos == targetPos)
        {
            Debug.Log("Invalid parabola, target to self");
            CloseProjectileParabola(selfCharacter);
            return false;
        }

        return true;
    }

    public void CloseAllProjectileParabola()
    {
        for (int i = 0; i < joinedBattleUnits.Count; i++)
            CloseProjectileParabola(joinedBattleUnits[i]);
    }
    public void CloseProjectileParabola(CharacterBase character)
    {
        LineRenderer lineRenderer = character.GetComponentInChildren<LineRenderer>();
        if (lineRenderer == null) { return; }
        lineRenderer.positionCount = 0;
    }
    public void ShowOppositeTeamParabola(CharacterBase targetCharacter, GameNode targetNode)
    {
        List<CharacterBase> oppositeUnit = GetCharacterOpposites(targetCharacter);
        for (int i = 0; i < oppositeUnit.Count; i++)
        {
            GameNode originNode = oppositeUnit[i].GetCharacterTransformToNode();
            if (targetNode == null)
                targetNode = targetCharacter.GetCharacterTransformToNode();
            ShowAnyProjectileParabola(oppositeUnit[i], originNode, targetNode, true, true);
        }
    }
    #endregion

    public void CastSkill(CharacterBase selfCharacter, SkillData currentSkill, GameNode castAtNode,
        GameNode targetNode, Action onSkillFinished)
    {
        if (selfCharacter == null)
        {
            Debug.LogError("CastSkill failed: selfCharacter is null");
            return;
        }
        if (castAtNode == null)
        {
            Debug.LogError("CastSkill failed: castAtNode is null");
            return;
        }
        if (currentSkill == null)
        {
            Debug.LogError("CastSkill failed: currentSkill is null");
            return;
        }
        if (targetNode == null)
        {
            Debug.LogError("CastSkill failed: targetNode is null");
            return;
        }

        Vector3 direction = (targetNode.GetNodeVector() - selfCharacter.transform.position);
        selfCharacter.SetOrientation(direction);

        StartCoroutine(SkillCastCoroutine(selfCharacter, currentSkill, castAtNode, targetNode, onSkillFinished));
    }

    private IEnumerator SkillCastCoroutine(CharacterBase selfCharacter, SkillData currentSkill,
        GameNode originNode, GameNode targetNode, Action onFinished)
    {
        bool isFinish = false;

        int costMP = currentSkill.MPAmount;
        int targetMental = selfCharacter.currentMental - costMP;

        CharacterBase targetUICharacter = CTTurnUIManager.instance.currentTargetCharacter;

        if (selfCharacter == targetUICharacter)
            CTTurnUIManager.instance.ChangeUICurrentMentalTo(selfCharacter, targetMental);

        selfCharacter.currentMental = targetMental;

        GameEvent.onSkillCastStart?.Invoke(currentSkill);

        if (currentSkill.isProjectile)
        {
            Projectile projectile = CastSkillProjectile(selfCharacter, currentSkill, originNode, targetNode);
            if (projectile == null)
            {
                Debug.LogWarning("Missing projectile");
                isFinish = true;
            }
            else
            {
                projectile.onHitCompleted += () => 
                { 
                    isFinish = true;
                    Debug.Log($"Projectile hit in {projectile.transform.position}");
                };
                yield return new WaitUntil(() => isFinish);
            }
        }
        else
        {
            CastSkil(selfCharacter, currentSkill, targetNode);
            yield return StartCoroutine(SkillEventCoroutine(currentSkill));

            isFinish = true;
        }

        yield return new WaitUntil(() => isFinish);
        GameEvent.onSkillCastEnd?.Invoke();
        onFinished?.Invoke();
    }
    private IEnumerator SkillEventCoroutine(SkillData skill)
    {   
        if (skill.skillCastTime < 0)
        {
            Debug.LogWarning("Skill Cast Time Issue! less than 0!");
            yield return null;
        }

        float skillTime = skill.skillCastTime;
        float elapsedTime = 0;

        while (elapsedTime < skillTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void CastSkil(CharacterBase selfCharacter, SkillData currentSkill, GameNode targetNode)
    {
        CharacterBase targetCharacter = targetNode.GetUnitGridCharacter();

        if (currentSkill.skillType == SkillType.Acttack)
        {
            int baseDamage = currentSkill.damageAmount;
            if (targetCharacter != null)
            {
                selfCharacter.DoDamage(baseDamage, targetCharacter);
            }
        }
        else if (currentSkill.skillType == SkillType.Heal)
        {
            int heal = currentSkill.healAmount;
            if (targetCharacter != null)
            {
                targetCharacter.TakeHeal(heal);
            }
        }
    }
    private Projectile CastSkillProjectile(CharacterBase selfCharacter, SkillData currentSkill, GameNode originNode, GameNode targetNode)
    {
        if (!currentSkill.isProjectile) { return null; }

        GameObject projectilePrefab = Instantiate(currentSkill.projectTilePrefab, originNode.GetNodeVector(), Quaternion.identity);

        CameraController.instance.ChangeFollowTarget(projectilePrefab.transform);
        if (debugMode)
            Debug.Log($"Instantiate projectile {currentSkill.projectTilePrefab.name} at {originNode.GetNodeVector()}");
        Projectile projectile = projectilePrefab.GetComponent<Projectile>();

        Vector3 targetPos = targetNode.GetNodeVector();
        CharacterBase targetCharacter = targetNode.GetUnitGridCharacter();
        if (targetCharacter != null)
            targetPos = targetCharacter.transform.position + new Vector3(0, 1.5f, 0);

        if (projectile != null)
        {
            if (originNode == null)
                projectile.LaunchToTarget(selfCharacter, currentSkill, 
                    selfCharacter.transform.position + new Vector3(0, selfCharacter.shootOffsetHeight, 0), targetPos);
            else
                projectile.LaunchToTarget(selfCharacter, currentSkill,
                    originNode.GetNodeVector() + new Vector3(0, selfCharacter.shootOffsetHeight, 0), targetPos);
        }
        return projectile;
    }

    #region Preview Character
    public void GeneratePreviewCharacterInMovableRange(CharacterBase character)
    {
        List<GameNode> gameNodes = character.GetMovableNodes();
        if (gameNodes.Contains(lastSelectedNode))
        {
            GeneratePreviewCharacter(character);
        }
    }
    public void GeneratePreviewCharacter(CharacterBase character)
    {
        DestroyPreviewModel();
        if (lastSelectedNode.character != null) { return; }
        Vector3 offset = character.transform.position - character.GetCharacterTranformToNodePos();
        previewCharacter = Instantiate(character.characterModel);
        previewCharacter.transform.position = lastSelectedNode.GetNodeVector() + offset;
        if (previewMaterial != null)
        {
            MeshRenderer[] renderers = previewCharacter.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.material = previewMaterial;
            }
        }
    }
    public void DestroyPreviewModel()
    {
        if (previewCharacter != null)
        {
            Destroy(previewCharacter);
            previewCharacter = null;
        }
    }
    #endregion
    
    #region Orientation Gizmos
    /// <summary>
    /// Setup up orientation arrow at target node position with character current orientation
    /// </summary>
    public void SetupOrientationArrow(bool allowControl, CharacterBase character, GameNode targetNode)
    {
        Orientation orientation = character.selfOrientation;
        orientationArrow.ShowArrows(allowControl, orientation, targetNode);
    }

    private Orientation[] clockwiseOrder =
    {
        Orientation.right,
        Orientation.back,
        Orientation.left,
        Orientation.forward
    };
    public void SwitchToOrientationWithArrow(CharacterBase character, Orientation targetOrientation, 
        float stepTime = 0.1f, Action onRotateFinished = null)
    {
        StartCoroutine(OrientationWithArrouwCoroutine(character, targetOrientation, stepTime, onRotateFinished));
    }
    private IEnumerator OrientationWithArrouwCoroutine(CharacterBase character, Orientation target,
    float stepTime, Action onFinished)
    {
        Orientation current = character.selfOrientation;
        int currentIndex = Array.IndexOf(clockwiseOrder, current);
        int targetIndex = Array.IndexOf(clockwiseOrder, target);

        int total = clockwiseOrder.Length;

        int clockwiseDist = (targetIndex - currentIndex + total) % total;
        int counterDist = (currentIndex - targetIndex + total) % total;

        bool useClockwise = clockwiseDist <= counterDist;

        while (currentIndex != targetIndex)
        {
            if (useClockwise)
                currentIndex = (currentIndex + 1) % total;
            else
                currentIndex = (currentIndex - 1 + total) % total;

            Orientation newOrientation = clockwiseOrder[currentIndex];

            orientationArrow.currentOrientation = newOrientation;
            orientationArrow.HighlightOrientationArrow(newOrientation);
            character.SetTransfromOrientation(newOrientation);

            yield return new WaitForSeconds(stepTime);
        }

        onFinished?.Invoke();
    }

    public void HideOrientationArrow()
    {
        orientationArrow.HideAll();
    }
    public bool IsOrientationChanged()
    {
        if (orientationArrow.currentOrientation != lastOrientation)
        {
            lastOrientation = orientationArrow.currentOrientation;
            return true;
        }
        return false;
    }
    public Orientation GetSelectedOrientation()
    {
        return orientationArrow.currentOrientation;
    }
    #endregion

    public void OnLoadNextTurn()
    {
        CTTimeline.instance.NextCharacterTurn();
    }
    private void HandleUnitKnockout(CharacterBase character)
    {
        CTTimeline.instance.RemoveCharacter(character);
        TeamDeployment characterTeam = character.currentTeam;
        knockOutCharacter.Add(character);
        CheckBattleState();
    }

    public List<CharacterBase> GetCharacterOpposites(CharacterBase allyCharacter)
    {
        List<CharacterBase> oppositeUnit = new List<CharacterBase>();
        foreach (TeamDeployment team in battleTeams)
        {
            if (allyCharacter.currentTeam == team) { continue; }
            foreach (CharacterBase opposite in team.teamCharacter)
            {
                oppositeUnit.Add(opposite);
            }
        }
        return oppositeUnit;
    }
    public List<TeamDeployment> GetBattleTeam() => battleTeams;
    public List<CharacterBase> GetBattleUnits() => joinedBattleUnits;
}
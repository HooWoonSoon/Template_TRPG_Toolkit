using System;
using System.Collections.Generic;
using UnityEngine;

public enum Orientation { right, left, forward, back }
public enum UnitState { Active, Knockout, Dead }

[RequireComponent(typeof(UnitDetectable))]
public abstract class CharacterBase : Entity
{
    public GameObject characterModel;

    public TeamDeployment currentTeam;
    public UnitDetectable unitDetectable;

    [Header("Character Information")]
    public SelfCanvasController selfCanvasController;
    public CharacterData data;
    public List<SkillData> skillDatas;

    public int currentHealth { get; set; }
    public int currentMental { get; set; }
    public float moveSpeed = 6f;

    public GameNode currentNode { get; private set; }
    public PathRoute pathRoute { get; private set; }

    public Orientation selfOrientation = Orientation.right;
    public UnitState unitState = UnitState.Active;

    protected override void Start()
    {
        base.Start();
        unitDetectable = GetComponent<UnitDetectable>();

        currentHealth = data.health;
        currentMental = data.mental;
    }

    protected virtual void Update()
    {
        SetGridPos();
    }

    #region Orientation
    public void SetOrientation(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        direction = Vector3Int.RoundToInt(direction);

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            if (direction.x < 0)
                selfOrientation = Orientation.left;
            else
                selfOrientation = Orientation.right;
        }
        else
        {
            if (direction.z < 0)
                selfOrientation = Orientation.back;
            else
                selfOrientation = Orientation.forward;
        }
        SetTransfromOrientation(selfOrientation);
    }

    public void SetTransfromOrientation(Orientation orientation, bool is2D = false)
    {
        this.selfOrientation = orientation;
        switch (orientation)
        {
            case Orientation.left:
                transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
            case Orientation.right:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case Orientation.forward:
                if (is2D) { return; }
                transform.rotation = Quaternion.Euler(0, 270, 0);
                break;
            case Orientation.back:
                if (is2D) { return; }
                transform.rotation = Quaternion.Euler(0, 90, 0);
                break;
        }
    }
    public Vector3Int GetOrientationDirection()
    {
        return GetOrientationDirection(selfOrientation);
    }
    public Vector3Int GetOrientationDirection(Orientation orientation)
    {
        switch (orientation)
        {
            case Orientation.left:
                return new Vector3Int(-1, 0, 0);
            case Orientation.right:
                return new Vector3Int(1, 0, 0);
            case Orientation.back:
                return new Vector3Int(0, 0, -1);
            case Orientation.forward:
                return new Vector3Int(0, 0, 1);
            default:
                return Vector3Int.zero;
        }
    }
    private Orientation GetOrientation(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            if (direction.x > 0)
                return Orientation.right;
            else
                return Orientation.left;
        }
        else
        {
            if (direction.z > 0)
                return Orientation.forward;
            else
                return Orientation.back;
        }
    }
    public Vector3Int GetDirConvertOrientationDir(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            if (direction.x > 0)
                return new Vector3Int(1, 0, 0);
            else if (direction.x < 0)
                return new Vector3Int(-1, 0, 0);
        }
        else
        {
            if (direction.z > 0)
                return new Vector3Int(0, 0, 1);
            else if (direction.z < 0)
                return new Vector3Int(0, 0, -1);
        }
        return Vector3Int.zero;
    }
    #endregion

    public void SetNode(GameNode newNode)
    {
        if (currentNode == newNode) return;

        if (newNode.character == null || newNode.character == this)
        {
            currentNode?.SetUnitGridCharacter(null);
            currentNode = newNode;
            currentNode?.SetUnitGridCharacter(this);
        }
    }
    public void SetGridPos()
    {
        if (world == null) return;

        GameNode nextGridNode = world.GetNode(Utils.RoundXZFloorYInt(transform.position));

        if (nextGridNode == currentNode || nextGridNode == null) { return; }

        SetNode(nextGridNode);
    }
    public Vector3Int GetCharacterTranformToNodePos()
    {
        return Utils.RoundXZFloorYInt(transform.position);
    }
    public Vector3Int GetCharacterNodePos()
    {
        if (currentNode == null) 
        { 
            Debug.LogWarning("not recorded character current node");
        }
        return currentNode.GetNodeVectorInt();
    }
    public GameNode GetCharacterTransformToNode()
    {
        return world.GetNode(GetCharacterTranformToNodePos());
    }

    public void SetSelfToNode(GameNode targetNode, float offsetY = 0.5f)
    {
        Vector3 targetPos = targetNode.GetNodeVector();
        transform.position = targetPos + new Vector3(0, offsetY, 0);
        currentNode?.SetUnitGridCharacter(null);
        currentNode = targetNode;
        currentNode.SetUnitGridCharacter(this);
    }

    public abstract void SetAStarMovePos(Vector3 targetPosition);
    public abstract void SetAStarMovePos(Vector3Int targetPosition);
    public void SetPathRoute(PathRoute pathRoute)
    {
        this.pathRoute = pathRoute;
        //pathRoute.DebugPathRoute();
    }
    public void SetPathRoute(GameNode targetNode)
    {
        if (targetNode == null) return;
        pathRoute = GetPathRoute(targetNode);
        //pathRoute.DebugPathRoute();
    }
    public PathRoute GetPathRoute(GameNode targetNode)
    {
        Vector3 selfPos = GetCharacterTranformToNodePos();
        Vector3 targetPos = targetNode.GetNodeVector();

        if (selfPos == targetPos) return null;

        PathRoute pathRoute = pathFinding.GetPathRoute(selfPos, targetPos, this, 1, 1);
        if (pathRoute == null)
        {
            Debug.Log("No route has found");
            return null;
        }

        List<Vector3> pathVectorList = pathRoute.pathNodeVectorList;
        return new PathRoute
        {
            pathFinder = this,
            targetPosition = targetNode.GetNodeVectorInt(),
            pathNodeVectorList = pathVectorList,
            pathIndex = 0
        };
    }
    public void PathToTarget()
    {
        if (pathRoute == null) return;

        if (pathRoute.pathIndex != -1)
        {
            Vector3 nextPathPosition = pathRoute.pathNodeVectorList[pathRoute.pathIndex];

            Vector3 currentPos = pathRoute.pathFinder.transform.position;
            Vector3 direction = (nextPathPosition - currentPos).normalized;

            float heightDifferent = nextPathPosition.y - currentPos.y;
            if (Mathf.Abs(heightDifferent) > 0.1f)
            {
                Vector3 heightPos = new Vector3(currentPos.x, currentPos.y + heightDifferent, currentPos.z);
                pathRoute.pathFinder.transform.position = Vector3.MoveTowards(currentPos, heightPos, moveSpeed * 2 * Time.deltaTime);
                currentPos = pathRoute.pathFinder.transform.position;
            }

            SetOrientation(direction);

            pathRoute.pathFinder.transform.position = Vector3.MoveTowards(currentPos, nextPathPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(pathRoute.pathFinder.transform.position, nextPathPosition) <= 0.1f)
            {
                pathRoute.pathFinder.transform.position = nextPathPosition;

                pathRoute.pathIndex++;
                if (pathRoute.pathIndex >= pathRoute.pathNodeVectorList.Count)
                {
                    //Debug.Log($"Reached target {pathRoute.targetPosition}");
                    pathRoute.pathIndex = -1;
                    pathRoute = null;
                }
            }
        }
    }

    public void CharacterPassWay()
    {
        Vector3 targetLocalPos;

        CharacterBase blockingUnit = GetBlockingUnitAtMyNode();
        if (blockingUnit != null)
        {
            Vector3 offset = GetOffsetDirection(blockingUnit) * 0.5f;
            targetLocalPos = offset;
        }
        else
        {
            targetLocalPos = Vector3.zero;
        }
        characterModel.transform.localPosition = Vector3.Lerp(characterModel.transform.localPosition, targetLocalPos, Time.deltaTime * 10f);
    }
    private CharacterBase GetBlockingUnitAtMyNode()
    {
        foreach (CharacterBase other in currentTeam.teamCharacter)
        {
            if (other == this) continue;
            if (other.unitState == UnitState.Knockout || 
                other.unitState == UnitState.Dead) continue;
            if (other.currentNode == currentNode)
                return other;
        }
        return null;
    }
    private Vector3 GetOffsetDirection(CharacterBase other)
    {
        Vector3 direction = (transform.position - other.transform.position).normalized;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
        {
            if (direction.x > 0)
                return Vector3.right;
            else
                return Vector3.left;
        }
        else
        {
            if (direction.z > 0)
                return Vector3.forward;
            else
                return Vector3.back;
        }
    }

    public List<SkillData> GetAvaliableSkills()
    {
        List<SkillData> availabelSkills = new List<SkillData>();
        foreach (var skill in skillDatas)
        {
            if (availabelSkills.Contains(skill)) continue;

            if (skill.MPAmount <= currentMental)
            {
                availabelSkills.Add(skill);
            }
        }
        return availabelSkills;
    }

    public bool CheckHitTargetBack(CharacterBase targetCharacter)
    {
        Vector3 origin = currentNode.GetNodeVector();
        Vector3 target = targetCharacter.currentNode.GetNodeVector();

        Vector3 direction = (target - origin).normalized;

        Vector3Int hitOrientationDir = GetDirConvertOrientationDir(direction);
        Vector3 targetOrientationDir = targetCharacter.GetOrientationDirection();

        if (hitOrientationDir == targetOrientationDir)
        {
            return true;
        }

        return false;
    }
    public bool CriticalSuccess(float criticalChance = 0.1f)
    {
        float randomFloat = UnityEngine.Random.Range(0f, 1f);
        return criticalChance > randomFloat;
    }
    public void DoDamage(int baseDamage, CharacterBase target)
    {
        int damage = baseDamage;
        float criticalChance = data.criticalChance;

        bool backHit = CheckHitTargetBack(target);
        if (backHit)
        {
            criticalChance += 0.3f;
        }
        bool isCritical = CriticalSuccess(criticalChance);
        if (isCritical)
        {
            damage = Mathf.CeilToInt(baseDamage * 1.5f);
        }
        target.TakeDamage(damage, isCritical);
    }
    public void TakeDamage(int damage, bool isCritical = false)
    {
        int targetHealth = currentHealth - damage;

        CharacterBase ctUICharacter = CTTurnUIManager.instance.currentTargetCharacter;
        if (this == ctUICharacter)
            CTTurnUIManager.instance.ChangeUICurrentHealthTo(ctUICharacter, targetHealth);

        if (selfCanvasController != null)
            selfCanvasController.ExecuteHealthChange(this, -damage);

        currentHealth = targetHealth;

        string damageText = damage.ToString();
        if (isCritical)
            UniversalUIManager.instance.CreateCriticalCountText(this, damageText);
        else
            UniversalUIManager.instance.CreateText(this, damageText);
        TakeDamageEffect();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (BattleManager.instance.isBattleStarted)
            {
                GameEvent.onBattleUnitKnockout?.Invoke(this);
            }
            KnockOut();
        }
    }
    private void TakeDamageEffect()
    {
        if (characterModel == null) { return; }

        if (selfOrientation == Orientation.right || selfOrientation == Orientation.left)
        {
            StartCoroutine(Utils.VibrationCorroutine(transform, new Vector3(-0.1f, 0, 0), new Vector3(0.1f, 0, 0), 3, 0.2f));
        }
        else if (selfOrientation == Orientation.forward || selfOrientation == Orientation.back)
        {
            StartCoroutine(Utils.VibrationCorroutine(transform, new Vector3(0, 0, -0.1f), new Vector3(0, 0, 0.1f), 3, 0.2f));
        }
    }
    
    public void TakeHeal(int heal)
    {
        int missingHealth = data.health - currentHealth;
        heal = Mathf.Min(heal, missingHealth);
        int targetHealth = Mathf.Min(currentHealth + heal, data.health);

        CharacterBase ctUICharacter = CTTurnUIManager.instance.currentTargetCharacter;
        if (this == ctUICharacter)
            CTTurnUIManager.instance.ChangeUICurrentHealthTo(ctUICharacter, targetHealth);

        if (selfCanvasController != null)
            selfCanvasController.ExecuteHealthChange(this, heal);

        currentHealth = targetHealth;
        string healText = heal.ToString();
        UniversalUIManager.instance.CreateText(this, healText);
    }
    private void KnockOut()
    {
        unitState = UnitState.Knockout;
        currentNode.SetUnitGridCharacter(null);
        gameObject.SetActive(false);
    }

    public bool IsYourTurn(CharacterBase character)
    {
        if (!BattleManager.instance.isBattleStarted) { return false; }
        if (character == CTTimeline.instance.GetCurrentCharacter()) return true;
        return false;
    }

    public List<CharacterBase> GetMapCharacterExceptSelf()
    {
        List<CharacterBase> characters = new List<CharacterBase>();
        foreach (GameNode node in world.loadedNodes.Values)
        {
            CharacterBase nodeCharacter = node.character;
            if (nodeCharacter != null && nodeCharacter != this)
                characters.Add(nodeCharacter);
        }
        return characters;
    }

    public List<Vector3Int> GetCustomizedSizeMovablePos(int size, HashSet<Vector3Int> occupiedPos)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        Vector3Int selfPos = GetCharacterNodePos();

        List<GameNode> coverage = pathFinding.GetCostDijkstraCoverangeNodes(selfPos, 2, size, 1, 1);
        foreach (var node in coverage)
        {
            Vector3Int nodePos = node.GetNodeVectorInt();

            if (node.character == null || node.character == this
                && !occupiedPos.Contains(nodePos))
            {
                result.Add(node.GetNodeVectorInt());
            }
        }
        return result;
    }
    public List<GameNode> GetCustomizedSizeMovableNodes(int size, int occlusion)
    {
        List<GameNode> sizeRangeNode = pathFinding.GetCostDijkstraCoverangeNodes(this, size, 1, 1);
        List<GameNode> occlusionRangeNode = pathFinding.GetCostDijkstraCoverangeNodes(this, occlusion, 1, 1);
        sizeRangeNode.RemoveAll(n => occlusionRangeNode.Contains(n));
        return sizeRangeNode;
    }
    public List<GameNode> GetMovableNodes()
    {
        List<GameNode> result = new List<GameNode>();
        int movableRange = data.movementValue;
        List<GameNode> coverage = pathFinding.GetCostDijkstraCoverangeNodes(this, movableRange, 1, 1);
        foreach (var node in coverage)
        {
            if (node.character == null || node.character == this)
            {
                result.Add(node);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Get overlap opposite movable range based on self movable range
    /// </summary>
    /// <returns></returns>
    public List<GameNode> GetConflictNode()
    {
        List<GameNode> movableNodes = GetMovableNodes();
        return GetConflictNode(movableNodes);
    }
    public List<GameNode> GetConflictNode(List<GameNode> movableNodes)
    {
        int selfRange = data.movementValue;
        Vector3Int selfPos = currentNode.GetNodeVectorInt();
        List<GameNode> selfRangeExtend = pathFinding.GetCalculateDijkstraCostNodes(selfPos, 2, 1, 1);
        HashSet<GameNode> selfMovableNodeSet = new HashSet<GameNode>(movableNodes);
        List<GameNode> conflictNodes = new List<GameNode>();
        foreach (GameNode node in selfRangeExtend)
        {
            if (node.character == null) continue;
            CharacterBase character = node.character;
            TeamDeployment characterTeam = character.currentTeam;
            if (characterTeam != currentTeam)
            {
                int oppositeRange = character.data.movementValue;
                List<GameNode> oppositeRangeNodes = 
                    pathFinding.GetCostDijkstraCoverangeNodes(
                        character, oppositeRange, 1, 1);
                foreach (GameNode rangeNode in oppositeRangeNodes)
                {
                    if (selfMovableNodeSet.Contains(rangeNode))
                    {
                        conflictNodes.Add(rangeNode);
                    }
                }
            }
        }
        return conflictNodes;
    }
    /// <summary>
    /// Get overlap teammate movable range based on self movable range
    /// </summary>
    /// <returns></returns>
    public List<GameNode> GetSupportNode()
    {
        int selfRange = data.movementValue;
        Vector3Int selfPos = currentNode.GetNodeVectorInt();
        List<GameNode> selfRangeExtend = pathFinding.GetCalculateDijkstraCostNodes(selfPos, 2, 1, 1);
        List<GameNode> selfMovableNode = pathFinding.GetCostDijkstraCoverangeNodes(this, selfRange, 1, 1);
        HashSet<GameNode> selfMovableNodeSet = new HashSet<GameNode>(selfMovableNode);
        List<GameNode> supportNodes = new List<GameNode>();
        foreach (GameNode node in selfRangeExtend)
        {
            if (node.character == null) continue;
            CharacterBase character = node.character;
            TeamDeployment characterTeam = character.currentTeam;
            if (characterTeam == currentTeam)
            {
                int teammateRange = character.data.movementValue;
                List<GameNode> teammateRangeNodes =
                    pathFinding.GetCostDijkstraCoverangeNodes(
                        character, teammateRange, 1, 1);
                foreach (GameNode rangeNode in teammateRangeNodes)
                {
                    supportNodes.Add(rangeNode);
                }
            }
        }
        return supportNodes;
    }

    public List<GameNode> GetSkillRangeFromNode(SkillData skill, GameNode gameNode)
    {
        return skill.GetInflueneNode(world, gameNode);
    }

    #region Visual Tilemap
    public void ResetVisualTilemap()
    {
        GridTilemapVisual.instance.SetAllTileSprite(GameNode.TilemapSprite.None);
    }
    public void ShowMovableTilemap()
    {
        ResetVisualTilemap();
        int selfRange = data.movementValue;
        List<GameNode> movableNode = GetMovableNodes();
        foreach (GameNode node in movableNode)
        {
            Vector3Int position = node.GetNodeVectorInt();
            GridTilemapVisual.instance.SetTilemapSprite(position, GameNode.TilemapSprite.Blue);
        }
    }
    public void ShowDangerAndMovableTileFromNode()
    {
        ResetVisualTilemap();
        List<GameNode> selfMovableNodes = GetMovableNodes();
        List<GameNode> dangerNodes = GetConflictNode(selfMovableNodes);
        ShowDangerAndMovableTileFromNode(selfMovableNodes, dangerNodes);
    }
    public void ShowDangerAndMovableTileFromNode(List<GameNode> movableNodes, List<GameNode> dangerNodes)
    {
        ResetVisualTilemap();
        foreach (GameNode node in movableNodes)
        {
            GridTilemapVisual.instance.SetTilemapSprite(node, GameNode.TilemapSprite.Blue);
        }
        foreach (GameNode rangeNode in dangerNodes)
        {
            GridTilemapVisual.instance.SetTilemapSprite(rangeNode, GameNode.TilemapSprite.Purple);
        }
    }
    public void ShowDangerMovableAndTargetTilemap(GameNode targetNode, List<GameNode> movableNodes = null)
    {
        ResetVisualTilemap();
        if (movableNodes == null)
        {
            movableNodes = GetMovableNodes();
        }
        List<GameNode> dangerNodes = GetConflictNode(movableNodes);
        if (movableNodes.Contains(targetNode))
        {
            ShowDangerAndMovableTileFromNode(movableNodes, dangerNodes);
            GridTilemapVisual.instance.SetTilemapSprite(targetNode.GetNodeVectorInt(), GameNode.TilemapSprite.TinyBlue);
        }
        else
        {
            ShowDangerAndMovableTileFromNode();
        }
    }

    public void ShowSkillTargetTilemap(GameNode originNode, GameNode targetNode, SkillData skill)
    {
        if (originNode == null) { Debug.LogWarning("Missing originNode"); }
        ShowSkillTilemap(originNode, skill);
        List<GameNode> skillRangeNodes = GetSkillRangeFromNode(skill, originNode);
        if (skillRangeNodes.Contains(targetNode))
        {
            GridTilemapVisual.instance.SetTilemapSprite(targetNode, GameNode.TilemapSprite.Red);
        }
    }
    public GameNode GetSkillTargetShowTilemap(GameNode originNode, GameNode targetNode, SkillData skill)
    {
        if (originNode == null) { Debug.LogWarning("Missing originNode"); }
        ShowSkillTilemap(originNode, skill);
        List<GameNode> skillRangeNodes = GetSkillRangeFromNode(skill, originNode);
        if (skillRangeNodes.Contains(targetNode))
        {
            GridTilemapVisual.instance.SetTilemapSprite(targetNode, GameNode.TilemapSprite.Red);
            return targetNode;
        }
        return null;
    }
    public void ShowSkillTilemap(GameNode startFormNode, SkillData skill)
    {
        if (skill == null) return;
        ResetVisualTilemap();
        List<GameNode> influenceNodes = GetSkillRangeFromNode(skill, startFormNode);
        foreach (GameNode node in influenceNodes)
        {
            GridTilemapVisual.instance.SetTilemapSprite(node, GameNode.TilemapSprite.TinyBlue);
        }
    }
    #endregion

    public bool IsInMovableRange(GameNode gameNode, List<GameNode> movableNodes = null)
    {
        if (movableNodes == null)
            movableNodes = GetMovableNodes();
        
        if (movableNodes.Contains(gameNode))
        {
            return true;
        }
        return false;
    }
    public float GetCurrentHealthPercentage()
    {
        float maxHeath = data.health;
        float percentage = (float)Math.Round((currentHealth / maxHeath), 2);
        return percentage;
    }

    public abstract void TeleportToNodeDeployble(GameNode targetNode);
    public abstract void TeleportToNodeFree(GameNode targetNode);
    public abstract void ReadyBattle();
    public abstract void ExitBattle();
}
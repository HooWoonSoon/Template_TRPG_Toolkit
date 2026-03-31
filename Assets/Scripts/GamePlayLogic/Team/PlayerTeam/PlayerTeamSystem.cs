using System.Collections.Generic;
using UnityEngine;
public class PlayerTeamSystem : TeamSystem
{
    public TeamDeployment teamDeployment;
    public List<TeamFollower> linkMembers;
    private List<PlayerCharacter> unlinkMember = new List<PlayerCharacter>();
    public PlayerCharacter currentControl { get; private set; }

    public int spacingDistance = 2;
    [SerializeField] private int historyLimit = 15;

    public TeamStateMachine stateMachine;
    public PlayerTeamIdleState teamIdleState { get; private set; }
    public PlayerTeamActionState teamActionState { get; private set; }
    public PlayerTeamDeployment teamDeploymentState { get; private set; } // Tempo fix
    public TeamSortPathFindingState teamSortPathFindingState { get; private set; }

    private void OnEnable()
    {
        GameEvent.onLeaderChangedRequest += SetTeamControl;
        GameEvent.onTeamSortExchange += SortTeamFollower;
        GameEvent.onTeamSortExchange += ClearAllHistory;

        GameEvent.onEnterDeployMap += () => stateMachine.ChangeState(teamDeploymentState); // Tempo fix
        GameEvent.onEnterMap += () => stateMachine.ChangeState(teamIdleState); // Tempo fix
    }

    private void OnDisable()
    {
        GameEvent.onLeaderChangedRequest -= SetTeamControl;
        GameEvent.onTeamSortExchange -= SortTeamFollower;
        GameEvent.onTeamSortExchange -= ClearAllHistory;

        GameEvent.onEnterDeployMap -= () => stateMachine.ChangeState(teamDeploymentState); // Tempo fix
        GameEvent.onEnterMap -= () => stateMachine.ChangeState(teamIdleState); // Tempo fix
    }

    private void Awake()
    {
        Initialize();
        stateMachine = new TeamStateMachine();
        
        teamIdleState = new PlayerTeamIdleState(stateMachine, this);
        teamActionState = new PlayerTeamActionState(stateMachine, this);
        teamDeploymentState = new PlayerTeamDeployment(stateMachine, this);
        teamSortPathFindingState = new TeamSortPathFindingState(stateMachine, this);
    }

    protected override void Start()
    {
        base.Start();
        SetTeamControl();
        stateMachine.Initialize(teamIdleState);
    }

    private void Update()
    {
        stateMachine.currentPlayerTeamState.Update();
        //FindMouseTargetPath(currentLeader);
    }

    /// <summary>
    /// Process A* path finding for specific character, the destination would be the mouse target
    /// </summary>
    private void FindMouseTargetPath(CharacterBase character)
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameNode hitNode = Utils.GetRaycastHitNode(world.loadedNodes);
            if (hitNode == null) { return; }
            Vector3Int targetPosition = hitNode.GetNodeVectorInt();
            if (targetPosition == new Vector3Int(-1, -1, -1)) return;

            character.SetAStarMovePos(targetPosition);
        }
    }

    #region Initialize
    private void Initialize()
    {
        InitializeTeamFollower();
    }

    //  Summary
    //      Initialize the team follower list by setting the target to follow for each unit character.
    private void InitializeTeamFollower()
    {
        for (int i = 0; i < teamDeployment.teamCharacter.Count; i++)
        {
            PlayerCharacter character = teamDeployment.teamCharacter[i] as PlayerCharacter;

            if (i == 0)
                linkMembers[i].Initialize(character, null);
            else
            {
                PlayerCharacter prevCharacter = teamDeployment.teamCharacter[i - 1] as PlayerCharacter;
                linkMembers[i].Initialize(character, prevCharacter);
            }

            linkMembers[i].character.historyLimit = historyLimit;
        }
    }
    #endregion

    #region Manage team follower
    //  Summary
    //      Sort the team follower list by the index of the unit character.
    private void SortTeamFollower()
    {
        List<TeamFollower> sortedList = new List<TeamFollower>();
        List<TeamFollower> unsortList = new List<TeamFollower>(linkMembers);

        if (unsortList.Count == 0) return;

        while (unsortList.Count > 0)
        {
            TeamFollower minFollower = unsortList[0];
            for (int i = 0; i < unsortList.Count; i++)
            {
                if (unsortList[i].character.index < minFollower.character.index)
                {
                    minFollower = unsortList[i];
                }
            }

            sortedList.Add(minFollower);
            unsortList.Remove(minFollower);
        }

        linkMembers = sortedList;
        RefreshTeamFollower();
        SetTeamControl();
    }

    //  Summary
    //      Refresh the team follower list by setting the target to follow for each unit character.
    private void RefreshTeamFollower()
    {
        for (int i = 0; i < linkMembers.Count; i++)
        {
            if (i == 0)
                linkMembers[i].Initialize(linkMembers[i].character, null);
            else
                linkMembers[i].Initialize(linkMembers[i].character, linkMembers[i - 1].character);
        }
    }

    /// <summary>
    /// Add a new character to the team follower list and remove it from the unlink character.
    /// </summary>
    public void InsertTeamFollower(PlayerCharacter unitCharacter)
    {
        TeamFollower teamFollower = new TeamFollower();
        teamFollower.character = unitCharacter;
        unlinkMember.Remove(unitCharacter);

        linkMembers.Add(teamFollower);
        SortTeamFollower();
    }

    /// <summary>
    /// Set the leader of the team by checking the index of each unit character.
    /// The first character in the list is set as the leader.
    /// </summary>
    public void SetTeamControl()
    {
        currentControl = null;

        for (int i = 0; i < linkMembers.Count; i++)
        {
            PlayerCharacter unitCharacter = linkMembers[i].character;

            if (linkMembers[i].character.index == 0) 
            { 
                unitCharacter.setControl = true;
                currentControl = unitCharacter;
                GameEvent.onLeaderChanged?.Invoke(currentControl);
            }
            else { unitCharacter.setControl = false; }
        }
    }
    #endregion

    #region External call manage team follower
    /// <summary>
    /// External call to remove the character from the team follower list
    /// and add it to the unlink character list.
    /// </summary>
    public void RemoveUnlinkCharacterFromTeam(PlayerCharacter unitCharacter)
    {
        for (int i = 0; i < linkMembers.Count; i++)
        {
            if (linkMembers[i].character == unitCharacter)
            {
                linkMembers.RemoveAt(i);
                RefreshTeamFollower();
                break;
            }
        }
    }
    /// <summary>
    /// External call to add a character to the unlink character list.
    /// </summary>
    public void AddCharacterToUnlinkList(PlayerCharacter character)
    {
        if (!unlinkMember.Contains(character)) 
        { 
            character.ForceStopVelocity();
            unlinkMember.Add(character);
        }
    }
    #endregion

    private void ClearAllHistory()
    {
        for (int i = 0; i < linkMembers.Count; i++)
        {
            linkMembers[i].character.CleanAllHistory();
        }
    }

    #region Logic handle team follower
    /// <summary>
    /// Follow the target character with the nearest index member.
    /// </summary>
    public void FollowWithNearIndexMember(PlayerCharacter member, PlayerCharacter follower)
    {
        if (member.isLink == false || follower == null) return;
        GetFollowTargetDirection(member, follower, out Vector3 direciton);

        member.SetMoveDirection(direciton);
    }
    /// <summary>
    /// Get the direction to follow the target character.
    /// </summary>
    private void GetFollowTargetDirection(PlayerCharacter member, PlayerCharacter follower, 
        out Vector3 direction)
    {
        direction = Vector3.zero;

        if (!currentControl.isMoving) { return; }
        if (member == null || follower.positionHistory.Count < 2) return;

        List<Vector3> history = follower.positionHistory;

        for (int i = history.Count - 1; i > 0; i--)
        {
            float distance = Vector3.Distance(member.transform.position, follower.transform.position);
            if (distance >= spacingDistance)
            {
                Vector3 targetPosition = history[i];
                direction = (targetPosition - member.transform.position).normalized;
                return;
            }
        }
    }
    #endregion

    #region Team Sort Path Finding
    public void EnableTeamPathFinding()
    {
        teamPathRoutes.Clear();

        List<PathRoute> teamSortRoute = GetTeamSortPath(linkMembers, spacingDistance);
        if (teamSortRoute == null) 
        {
            Debug.LogWarning("Can't execute team path finding because not found any executable path");
            return; 
        }
        if (IsTeamSortPathAvaliable(teamSortRoute))
        {
            teamPathRoutes = teamSortRoute;
            for (int i = 0; i < teamSortRoute.Count; i++)
            {
                teamSortRoute[i].pathFinder.SetPathRoute(teamSortRoute[i]);
            }
            for (int i = 0; i < linkMembers.Count; i++)
            {
                PlayerCharacter character = linkMembers[i].character;
                character.ForceStopVelocity();
                character.stateMechine.ChangeState(character.movePathStateExplore);
            }
        }
    }
    public List<PathRoute> GetTeamSortPath(List<TeamFollower> linkMembers, int spacing)
    {
        List<PathRoute> teamPathRoute = new List<PathRoute>();
        HashSet<Vector3Int> usedTargetPositions = new HashSet<Vector3Int>();

        Vector3Int lastTargetPosition = Utils.RoundXZFloorYInt(linkMembers[0].character.transform.position);

        for (int i = 1; i < linkMembers.Count; i++)
        {
            CharacterBase character =linkMembers[i].character;
            Vector3Int fromPosition = Utils.RoundXZFloorYInt(character.transform.position);
            GameNode currentNode = character.currentNode;
            if (currentNode != null)
                fromPosition = currentNode.GetNodeVectorInt();

            if (IsWithinFollowRange(fromPosition, lastTargetPosition))
            {
                List<Vector3Int> unitRange = world.GetManhattas3DGameNodePosition(lastTargetPosition, 2);

                unitRange.RemoveAll(pos => usedTargetPositions.Contains(pos));
                teamPathRoute.Add(new PathRoute
                {
                    targetRangeList = unitRange,
                    pathFinder = character,
                });
            }
            else
            {
                Debug.Log($"Target {lastTargetPosition} is too far from {fromPosition}");
                return null;
            }

            bool foundPath = IsClosestTargetExist(character, fromPosition, teamPathRoute[i - 1]);
            if (!foundPath)
            {
                Debug.Log($"No path found from {fromPosition} to {lastTargetPosition} break!");
                return null;
            }
            usedTargetPositions.Add(teamPathRoute[i - 1].targetPosition.Value);
            lastTargetPosition = teamPathRoute[i - 1].targetPosition.Value;
        }
        return teamPathRoute;
    }
    private bool IsWithinFollowRange(Vector3Int fromPosition, Vector3Int targetPosition, float maxDistance = 16f)
    {
        return Vector3.Distance(fromPosition, targetPosition) <= maxDistance;
    }
    private bool IsClosestTargetExist(CharacterBase character, Vector3Int fromPosition, PathRoute pathRoute)
    {
        if (pathRoute.targetRangeList == null || pathRoute.targetRangeList.Count == 0)
            return false;

        var sortedTarget = Utils.SortTargetRangeByDistance(fromPosition, pathRoute.targetRangeList);

        for (int i = 0; i < sortedTarget.Count; i++)
        {
            PathRoute route = pathFinding.GetPathRoute(fromPosition, sortedTarget[i], character, 1, 1);
            if (route == null) { return false; }
            List<Vector3> pathVectorList = route.pathNodeVectorList;

            bool existSameTarget = IsTargetPositionExist(pathRoute, sortedTarget[i]);
            if (existSameTarget == true)
            {
                Debug.Log($"Target {sortedTarget[i]} is already exist");
                continue;
            }

            if (pathVectorList.Count != 0)
            {
                pathRoute.pathNodeVectorList = pathVectorList;
                pathRoute.targetPosition = sortedTarget[i];
                pathRoute.pathIndex = 0;
                Debug.Log($" {fromPosition} to target {pathRoute.targetPosition}");
                return true;
            }
        }
        return false;
    }
    private bool IsTeamSortPathAvaliable(List<PathRoute> teamPathRoutes)
    {
        if (teamPathRoutes.Count == 0) return false;

        for (int i = 0; i < teamPathRoutes.Count; i++)
        {
            if (!teamPathRoutes[i].targetPosition.HasValue)
            {
                return false;
            }
        }
        return true;
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (world == null) { return; }

        GameNode hitNode = Utils.GetRaycastHitNode(world.loadedNodes);
        if (hitNode == null) { return; }
        Vector3Int targetPosition = hitNode.GetNodeVectorInt();
        if (targetPosition == new Vector3Int(-1, -1, -1)) return;

        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(targetPosition + new Vector3(0, 1, 0), Vector3.one);
    }
}
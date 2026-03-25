using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System.Linq;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

public struct NodeData
{
    public int3 position;

    public bool isWalkable;
    public int gCost;
    public int hCost;
    public int fCost;

    public int cameFromIndex;   // "-1" mean invalid node
    public int characterIndex;  // "-1" mean invalid character ID

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }
}

public class PathFindingJobThread
{
    private World world;
    private NativeArray<NodeData> nodeArray;
    private NativeHashMap<int3, int> positionToIndex;
    private NativeHashMap<int, TeamType> teamCharacterIndex; // key = character ID, value = teamType

    private List<CharacterBase> mapCharacter;

    public PathFindingJobThread(World world)
    {
        this.world = world;
        Initialize();
    }

    public void Initialize()
    {
        if (nodeArray.IsCreated) nodeArray.Dispose();
        if (positionToIndex.IsCreated) positionToIndex.Dispose();
        if (teamCharacterIndex.IsCreated) teamCharacterIndex.Dispose();

        int count = world.loadedNodes.Count;
        nodeArray = new NativeArray<NodeData>(count, Allocator.Persistent);
        positionToIndex = new NativeHashMap<int3, int>(count, Allocator.Persistent);

        mapCharacter = new List<CharacterBase>();
        foreach (GameNode node in world.loadedNodes.Values)
        {
            CharacterBase nodeCharacter = node.GetUnitGridCharacter();
            if (nodeCharacter != null)
            {
                mapCharacter.Add(nodeCharacter);
            }
        }
        Debug.Log($"Construct Character Count {mapCharacter.Count}");

        teamCharacterIndex = new NativeHashMap<int, TeamType>(mapCharacter.Count, Allocator.Persistent);
        teamCharacterIndex.Clear();
        foreach (CharacterBase character in mapCharacter)
        {
            int characterID = character.data.ID;
            TeamType teamType = character.data.type;
            teamCharacterIndex.Add(characterID, teamType);
        }

        int i = 0;
        foreach (GameNode node in world.loadedNodes.Values)
        {
            int3 pos = new int3(node.x, node.y, node.z);

            nodeArray[i] = new NodeData
            {
                position = pos,
                isWalkable = node.isWalkable,
                gCost = int.MaxValue,
                cameFromIndex = -1,
                characterIndex = node.character != null ? node.character.data.ID : -1
            };
            positionToIndex.Add(pos, i);
            i++;
        }
    }
    private void InitializeNeighbourDir(NativeList<int3> directions, int riseLimit, int lowerLimit)
    {
        directions.Add(new int3(-1, 0, 0)); // Left
        directions.Add(new int3(1, 0, 0));  // Right
        directions.Add(new int3(0, 0, -1)); // Back
        directions.Add(new int3(0, 0, 1));  // Forward
        directions.Add(new int3(0, 1, 0));  // Up
        directions.Add(new int3(0, -1, 0)); // Down
        directions.Add(new int3(1, 0, 1));  // Diagonal Forward-Right
        directions.Add(new int3(-1, 0, 1)); // Diagonal Forward-Left
        directions.Add(new int3(1, 0, -1)); // Diagonal Backward-Right
        directions.Add(new int3(-1, 0, -1));// Diagonal Backward-Left

        for (int y = 1; y <= riseLimit; y++)
        {
            directions.Add(new int3(1, y, 0));
            directions.Add(new int3(-1, y, 0));
            directions.Add(new int3(0, y, 1));
            directions.Add(new int3(0, y, -1));
        }

        for (int y = 1; y <= lowerLimit; y++)
        {
            directions.Add(new int3(1, -y, 0));
            directions.Add(new int3(-1, -y, 0));
            directions.Add(new int3(0, -y, 1));
            directions.Add(new int3(0, -y, -1));
        }
    }

    public List<PathRoute> FindPathRouteJob(GameNode moveNode, List<GameNode> targetAroundNodes,
        CharacterBase character, int riseLimit, int lowerLimit)
    {
        Initialize(); // Temporarily
        int targetCount = targetAroundNodes.Count;

        NativeList<int3> directions = new NativeList<int3>(Allocator.TempJob);
        InitializeNeighbourDir(directions, riseLimit, lowerLimit);

        NativeArray<NativeArray<NodeData>> jobNodesArray =
            new NativeArray<NativeArray<NodeData>>(targetCount, Allocator.Temp);
        NativeList<JobHandle> jobHandleArray = new NativeList<JobHandle>(Allocator.Temp);
        NativeArray<NativeList<int>> resultIndex = new NativeArray<NativeList<int>>(targetCount, Allocator.TempJob);

        Vector3Int start = moveNode.GetNodeVectorInt();
        
        for (int i = 0; i < targetAroundNodes.Count; i++)
        {
            Vector3Int end = targetAroundNodes[i].GetNodeVectorInt();
            NativeList<int> pathResult = new NativeList<int>(Allocator.TempJob);
            resultIndex[i] = pathResult;

            JobHandle jobHandle = ScheduleFindPathJob(start.x, start.y, start.z,
                end.x, end.y, end.z, character, directions, pathResult, out NativeArray<NodeData> jobNodes);

            jobNodesArray[i] = jobNodes;
            jobHandleArray.Add(jobHandle);
        }

        JobHandle.CombineDependencies(jobHandleArray.AsArray()).Complete();

        for (int i = 0; i < targetCount; i++)
        {
            jobNodesArray[i].Dispose();
        }
        jobNodesArray.Dispose();

        List<PathRoute> routes = new List<PathRoute>();
        for (int i = 0; i < targetCount; i++)
        {
            NativeList<int> pathIndices = resultIndex[i];
            if (pathIndices.Length == 0) continue;

            List<Vector3Int> result = new List<Vector3Int>();
            for (int j = 0; j < pathIndices.Length; j++)
            {
                int nodeIndex = pathIndices[j];
                int3 pos = nodeArray[nodeIndex].position;
                result.Add(new Vector3Int(pos.x, pos.y, pos.z));
            }
            PathRoute route = new PathRoute(result);
            routes.Add(route);
        }

        for (int i = 0; i < targetCount; i++)
        {
            resultIndex[i].Dispose();
        }
        directions.Dispose();
        resultIndex.Dispose();
        jobHandleArray.Dispose();

        return routes;
    }
    public int FindRoutesBestScore(GameNode moveNode, List<GameNode> targetAroundNodes,
    CharacterBase character, int riseLimit, int lowerLimit)
    {
        Initialize(); // Temporarily
        int targetCount = targetAroundNodes.Count;

        NativeList<int3> directions = new NativeList<int3>(Allocator.TempJob);
        InitializeNeighbourDir(directions, riseLimit, lowerLimit);

        NativeArray<NativeArray<NodeData>> jobNodesArray =
            new NativeArray<NativeArray<NodeData>>(targetCount, Allocator.Temp);
        NativeList<JobHandle> jobHandleArray = new NativeList<JobHandle>(Allocator.Temp);
        NativeArray<NativeList<int>> resultIndex = new NativeArray<NativeList<int>>(targetCount, Allocator.TempJob);

        Vector3Int start = moveNode.GetNodeVectorInt();

        for (int i = 0; i < targetAroundNodes.Count; i++)
        {
            Vector3Int end = targetAroundNodes[i].GetNodeVectorInt();
            NativeList<int> pathResult = new NativeList<int>(Allocator.TempJob);
            resultIndex[i] = pathResult;

            JobHandle jobHandle = ScheduleFindPathJob(start.x, start.y, start.z,
                end.x, end.y, end.z, character, directions, pathResult, out NativeArray<NodeData> jobNodes);

            jobNodesArray[i] = jobNodes;
            jobHandleArray.Add(jobHandle);
        }

        JobHandle.CombineDependencies(jobHandleArray.AsArray()).Complete();

        for (int i = 0; i < targetCount; i++)
        {
            jobNodesArray[i].Dispose();
        }
        jobNodesArray.Dispose();

        int bestScore = int.MaxValue;

        for (int i = 0; i < targetCount; i++)
        {
            NativeList<int> pathIndices = resultIndex[i];
            if (pathIndices.Length > 0 && pathIndices.Length < bestScore)
            {
                bestScore = pathIndices.Length;
            }
            resultIndex[i].Dispose();
        }

        directions.Dispose();
        resultIndex.Dispose();
        jobHandleArray.Dispose();

        return bestScore;
    }

    public JobHandle ScheduleFindPathJob(int startWorldX, int startWorldY, int startWorldZ,
        int endWorldX, int endWorldY, int endWorldZ, CharacterBase pathFinder,
        NativeList<int3> directions, NativeList<int> outIndex, out NativeArray<NodeData> jobNodes)
    {
        jobNodes = new NativeArray<NodeData>(nodeArray, Allocator.TempJob);

        int3 start = new int3(startWorldX, startWorldY, startWorldZ);
        int3 end = new int3(endWorldX, endWorldY, endWorldZ);

        positionToIndex.TryGetValue(start, out int startIndex);
        positionToIndex.TryGetValue(end, out int endIndex);

        PathFindingJob pathFindingJob = new PathFindingJob
        {
            nodes = jobNodes,
            positionToIndex = positionToIndex,
            teamCharacterIndex = teamCharacterIndex,
            startIndex = startIndex,
            endIndex = endIndex,
            pathfinderID = pathFinder.data.ID,

            directions = directions,

            pathResult = outIndex
        };

        JobHandle jobHandle = pathFindingJob.Schedule();

        return jobHandle;
    }
    private List<Vector3Int> FindPathJob(Vector3 start, Vector3 end, 
        CharacterBase pathFinder, int riseLimit, int lowerLimit)
    {
        world.GetWorldPosition(start, out int startX, out int startY, out int startZ);
        world.GetWorldPosition(end, out int endX, out int endY, out int endZ);
        return FindPathJob(startX, startY, startZ, endX, endY, endZ, pathFinder,
            riseLimit, lowerLimit);
    }
    private List<Vector3Int> FindPathJob(int startWorldX, int startWorldY, int startWorldZ,
        int endWorldX, int endWorldY, int endWorldZ, CharacterBase pathFinder,
        int riseLimit, int lowerLimit)
    {
        Initialize(); // Temporarily
        int3 start = new int3(startWorldX, startWorldY, startWorldZ);
        int3 end = new int3(endWorldX, endWorldY, endWorldZ);

        positionToIndex.TryGetValue(start, out int startIndex);
        positionToIndex.TryGetValue(end, out int endIndex);

        NativeList<int3> directions = new NativeList<int3>(Allocator.TempJob);
        InitializeNeighbourDir(directions, riseLimit, lowerLimit);

        PathFindingJob pathFindingJob = new PathFindingJob
        {
            nodes = nodeArray,
            positionToIndex = positionToIndex,
            teamCharacterIndex = teamCharacterIndex,
            startIndex = startIndex,
            endIndex = endIndex,
            pathfinderID = pathFinder.data.ID,

            directions = directions,

            pathResult = new NativeList<int>(Allocator.TempJob)
        };

        JobHandle jobHandle = pathFindingJob.Schedule();
        //JobHandle.ScheduleBatchedJobs();
        jobHandle.Complete();

        List<Vector3Int> result = new List<Vector3Int>();
        foreach (var nodeIndex in pathFindingJob.pathResult)
        {
            int3 positionInt3 = nodeArray[nodeIndex].position;
            Vector3Int position = new Vector3Int(positionInt3.x, positionInt3.y, positionInt3.z);
            result.Add(position);
        }
        pathFindingJob.pathResult.Dispose();
        return result;
    }

    public List<PathRoute> GetBatchPathRouteJob(GameNode startNode,
        List<GameNode> targetNodeList, CharacterBase pathFinder,
        int riseLimit, int lowerLimit)
    {
        Vector3Int startPos = startNode.GetNodeVectorInt();
        List<Vector3Int> targetPosList = new List<Vector3Int>();
        for (int i = 0; i < targetNodeList.Count; i++)
        {
            Vector3Int targetPos = targetNodeList[i].GetNodeVectorInt();
            targetPosList.Add(targetPos);
        }

        return GetBatchPathRouteJob(startPos, targetPosList, pathFinder, riseLimit, lowerLimit);
    }

    public List<PathRoute> GetBatchPathRouteJob(Vector3Int startPos,
        List<Vector3Int> targetPosList, CharacterBase pathFinder,
        int riseLimit, int lowerLimit)
    {
        Initialize();

        NativeArray<int> targetsIndexArray = new NativeArray<int>(targetPosList.Count, Allocator.TempJob);

        int count = targetPosList.Count;
        int maxPath = 256;

        NativeArray<int> pathBuffer = new NativeArray<int>(count * maxPath, Allocator.TempJob);
        NativeArray<int> pathOffsets = new NativeArray<int>(count, Allocator.TempJob);
        NativeArray<int> pathLengths = new NativeArray<int>(count, Allocator.TempJob);

        int3 start = new int3(startPos.x, startPos.y, startPos.z);

        positionToIndex.TryGetValue(start, out int startIndex);
        for (int i = 0; i < targetPosList.Count; i++)
        {
            int3 end = new int3(targetPosList[i].x, targetPosList[i].y, targetPosList[i].z);
            positionToIndex.TryGetValue(end, out int endIndex);
            targetsIndexArray[i] = endIndex;
        }

        NativeList<int3> directions = new NativeList<int3>(Allocator.TempJob);
        InitializeNeighbourDir(directions, riseLimit, lowerLimit);

        BatchPathFindingJob batchPathFinding = new BatchPathFindingJob()
        {
            nodes = nodeArray,
            positionToIndex = positionToIndex,
            teamCharacterIndex = teamCharacterIndex,
            startIndex = startIndex,
            targetIndexArray = targetsIndexArray,
            pathfinderID = pathFinder.data.ID,
            directions = directions,

            pathBuffer = pathBuffer,
            pathOffsets = pathOffsets,
            pathLengths = pathLengths
        };

        JobHandle jobHandle = batchPathFinding.Schedule(targetPosList.Count, 32);
        jobHandle.Complete();

        targetsIndexArray.Dispose();
        directions.Dispose();

        List<PathRoute> routes = new List<PathRoute>();
        for (int i = 0; i < count; i++)
        {
            int offset = pathOffsets[i];
            int length = pathLengths[i];

            List<Vector3Int> pathList = new List<Vector3Int>();

            for (int j = 0; j < length; j++)
            {
                int nodeIndex = pathBuffer[offset + j];
                int3 pos = nodeArray[nodeIndex].position;
                pathList.Add(new Vector3Int(pos.x, pos.y, pos.z));
            }

            if (pathList.Count > 0)
                routes.Add(new PathRoute(pathList));
        }

        pathBuffer.Dispose();
        pathOffsets.Dispose();
        pathLengths.Dispose();

        return routes;
    }
}

[BurstCompile]
public struct BatchPathFindingJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<NodeData> nodes;
    [ReadOnly] public NativeHashMap<int3, int> positionToIndex;
    [ReadOnly] public NativeHashMap<int, TeamType> teamCharacterIndex;

    public int startIndex;
    [ReadOnly] public NativeArray<int> targetIndexArray;
    public int pathfinderID;

    [ReadOnly] public NativeList<int3> directions;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> pathBuffer;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> pathOffsets;

    [NativeDisableParallelForRestriction]
    public NativeArray<int> pathLengths;

    public void Execute(int index)
    {
        int endIndex = targetIndexArray[index];

        NativeArray<NodeData> jobNodes = new NativeArray<NodeData>(nodes, Allocator.Temp);

        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeHashSet<int> closeSet = new NativeHashSet<int>(positionToIndex.Count, Allocator.Temp);

        openList.Add(startIndex);

        for (int i = 0; i < jobNodes.Length; i++)
        {
            if (!jobNodes[i].isWalkable) { continue; }
            NodeData node = jobNodes[i];
            node.gCost = int.MaxValue;
            node.CalculateFCost();
            node.cameFromIndex = -1;
            jobNodes[i] = node;
        }

        NodeData startNode = jobNodes[startIndex];
        startNode.gCost = 0;
        startNode.hCost = CalaculateDistanceCost(jobNodes, startIndex, endIndex);
        startNode.CalculateFCost();
        jobNodes[startIndex] = startNode;

        while (openList.Length > 0)
        {
            int currentIndex = GetLowestFCostIndex(jobNodes, openList);

            if (currentIndex == endIndex)
            {
                int maxPath = 256;
                int offset = index * maxPath;

                int length = 0;
                int pathIndex = currentIndex;

                while (pathIndex != -1 && length < maxPath)
                {
                    pathBuffer[offset + length] = pathIndex;
                    pathIndex = jobNodes[pathIndex].cameFromIndex;
                    length++;
                }

                for (int i = 0, j = length - 1; i < j; i++, j--)
                {
                    int tmp = pathBuffer[offset + i];
                    pathBuffer[offset + i] = pathBuffer[offset + j];
                    pathBuffer[offset + j] = tmp;
                }

                pathOffsets[index] = offset;
                pathLengths[index] = length;

                break;
            }

            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i] == currentIndex)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }
            closeSet.Add(currentIndex);

            int3 currentPos = jobNodes[currentIndex].position;
            for (int i = 0; i < directions.Length; i++)
            {
                int3 neighbourPos = currentPos + directions[i];

                if (!positionToIndex.TryGetValue(neighbourPos, out int neighbourIndex))
                    continue;

                if (directions[i].x != 0 && directions[i].z != 0)
                {
                    int3 horizontalPos = currentPos + new int3(directions[i].x, 0, 0);
                    int3 verticalPos = currentPos + new int3(0, 0, directions[i].z);

                    if (!positionToIndex.ContainsKey(horizontalPos) ||
                        !positionToIndex.ContainsKey(verticalPos))
                        continue;
                }

                if (closeSet.Contains(neighbourIndex)) continue;

                if (!jobNodes[neighbourIndex].isWalkable)
                {
                    closeSet.Add(neighbourIndex);
                    continue;
                }

                int neighbourCharacterID = jobNodes[neighbourIndex].characterIndex;
                if (neighbourCharacterID != -1) // means don't have character
                {
                    if (!teamCharacterIndex.TryGetValue(pathfinderID, out TeamType pathFinderTeam))
                    {
                        closeSet.Add(neighbourIndex);
                        continue;
                    }

                    if (!teamCharacterIndex.TryGetValue(neighbourCharacterID, out TeamType neighbourTeam))
                    {
                        closeSet.Add(neighbourIndex);
                        continue;
                    }

                    if (pathFinderTeam != neighbourTeam)
                    {
                        closeSet.Add(neighbourIndex);
                        continue;
                    }
                    //Debug.Log($"Pathfinder: {pathfinderID}, Neighbour: {neighbourCharacterID}");
                }

                int tentativeGCost = jobNodes[currentIndex].gCost + CalaculateDistanceCost(jobNodes, currentIndex, neighbourIndex);
                if (tentativeGCost < jobNodes[neighbourIndex].gCost)
                {
                    NodeData neighbourNode = jobNodes[neighbourIndex];
                    neighbourNode.cameFromIndex = currentIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalaculateDistanceCost(jobNodes, neighbourIndex, endIndex);
                    neighbourNode.CalculateFCost();
                    jobNodes[neighbourIndex] = neighbourNode;

                    if (!openList.Contains(neighbourIndex))
                        openList.Add(neighbourIndex);
                }
            }
        }
        jobNodes.Dispose();
        openList.Dispose();
        closeSet.Dispose();
    }
    private int CalaculateDistanceCost(NativeArray<NodeData> nodes, int a, int b)
    {
        int3 startPos = nodes[a].position;
        int3 endPos = nodes[b].position;

        int xCost = Mathf.Abs(endPos.x - startPos.x);
        int yCost = Mathf.Abs(endPos.y - startPos.y);
        int zCost = Mathf.Abs(endPos.z - startPos.z);

        return (xCost + yCost + zCost);
    }
    private int GetLowestFCostIndex(NativeArray<NodeData> nodes, NativeList<int> openList)
    {
        int lowestCostIndex = openList[0];
        for (int i = 0; i < openList.Length; i++)
        {
            int index = openList[i];
            if (nodes[index].fCost < nodes[lowestCostIndex].fCost)
            {
                lowestCostIndex = openList[i];
            }
        }
        return lowestCostIndex;
    }
}

[BurstCompile]
public struct PathFindingJob : IJob
{
    public NativeArray<NodeData> nodes;
    [ReadOnly] public NativeHashMap<int3, int> positionToIndex;
    [ReadOnly] public NativeHashMap<int, TeamType> teamCharacterIndex;

    public int startIndex;
    public int endIndex;
    public int pathfinderID;

    [ReadOnly] public NativeList<int3> directions;

    public NativeList<int> pathResult;
    public void Execute()
    {
        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeHashSet<int> closeSet = new NativeHashSet<int>(positionToIndex.Count, Allocator.Temp);

        openList.Add(startIndex);

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].isWalkable) { continue; }
            NodeData node = nodes[i];
            node.gCost = int.MaxValue;
            node.CalculateFCost();
            node.cameFromIndex = -1;
            nodes[i] = node;
        }

        NodeData startNode = nodes[startIndex];
        startNode.gCost = 0;
        startNode.hCost = CalaculateDistanceCost(startIndex, endIndex);
        startNode.CalculateFCost();
        nodes[startIndex] = startNode;

        while (openList.Length > 0)
        {
            int currentIndex = GetLowestFCostIndex(openList);

            if (currentIndex == endIndex)
            {
                BuildPath(endIndex);
                break;
            }

            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i] == currentIndex)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }
            closeSet.Add(currentIndex);

            int3 currentPos = nodes[currentIndex].position;
            foreach (var direction in directions)
            {
                int3 neighbourPos = currentPos + direction;

                if (!positionToIndex.TryGetValue(neighbourPos, out int neighbourIndex))
                    continue;

                if (direction.x != 0 && direction.z != 0)
                {
                    int3 horizontalPos = currentPos + new int3(direction.x, 0, 0);
                    int3 verticalPos = currentPos + new int3(0, 0, direction.z);

                    if (!positionToIndex.ContainsKey(horizontalPos) ||
                        !positionToIndex.ContainsKey(verticalPos))
                        continue;
                }

                if (closeSet.Contains(neighbourIndex)) continue;

                if (!nodes[neighbourIndex].isWalkable)
                {
                    closeSet.Add(neighbourIndex);
                    continue;
                }

                int neighbourCharacterID = nodes[neighbourIndex].characterIndex;
                if (neighbourCharacterID != -1) // means don't have character
                {
                    if (!teamCharacterIndex.TryGetValue(pathfinderID, out TeamType pathFinderTeam))
                    {
                        closeSet.Add(neighbourIndex);
                        continue;
                    }

                    if (!teamCharacterIndex.TryGetValue(neighbourCharacterID, out TeamType neighbourTeam))
                    {
                        closeSet.Add(neighbourIndex);
                        continue;
                    }

                    if (pathFinderTeam != neighbourTeam)
                    {
                        closeSet.Add(neighbourIndex);
                        continue;
                    }
                    //Debug.Log($"Pathfinder: {pathfinderID}, Neighbour: {neighbourCharacterID}");
                }

                int tentativeGCost = nodes[currentIndex].gCost + CalaculateDistanceCost(currentIndex, neighbourIndex);
                if (tentativeGCost < nodes[neighbourIndex].gCost)
                {
                    NodeData neighbourNode = nodes[neighbourIndex];
                    neighbourNode.cameFromIndex = currentIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost = CalaculateDistanceCost(neighbourIndex, endIndex);
                    neighbourNode.CalculateFCost();
                    nodes[neighbourIndex] = neighbourNode;

                    if (!openList.Contains(neighbourIndex))
                        openList.Add(neighbourIndex);
                }
            }
        }
        openList.Dispose();
        closeSet.Dispose();
    }

    private void BuildPath(int endIndex)
    {
        NativeList<int> temp = new NativeList<int>(Allocator.Temp);
        int currentIndex = endIndex;

        while (currentIndex != -1)
        {
            temp.Add(currentIndex);
            currentIndex = nodes[currentIndex].cameFromIndex;
        }

        pathResult.Clear();
        for (int i = temp.Length - 1; i >= 0; i--)
        {
            pathResult.Add(temp[i]);
        }
        temp.Dispose();
    }
    private int CalaculateDistanceCost(int a, int b)
    {
        int3 startPos = nodes[a].position;
        int3 endPos = nodes[b].position;

        int xCost = Mathf.Abs(endPos.x - startPos.x);
        int yCost = Mathf.Abs(endPos.y - startPos.y);
        int zCost = Mathf.Abs(endPos.z - startPos.z);

        return (xCost + yCost + zCost);
    }
    private int GetLowestFCostIndex(NativeList<int> openList)
    {
        int lowestCostIndex = openList[0];
        for (int i = 0; i < openList.Length; i++)
        {
            int index = openList[i];
            if (nodes[index].fCost < nodes[lowestCostIndex].fCost)
            {
                lowestCostIndex = openList[i];
            }
        }
        return lowestCostIndex;
    }
}
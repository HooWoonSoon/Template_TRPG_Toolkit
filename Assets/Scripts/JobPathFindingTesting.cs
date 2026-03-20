//using UnityEngine;
//using Unity.Mathematics;
//using Unity.Collections;
//using Unity.Jobs;

//public struct NodeData
//{
//    public int3 position;

//    public bool isWalkable;
//    public int gCost;
//    public int hCost;
//    public int fCost;

//    public int cameFromIndex;
//    public int characterIndex;

//    public void CalculateFCost()
//    {
//        fCost = gCost + hCost;
//    }
//}

//public class JobPathFindingTesting : MonoBehaviour
//{
//    public World world;

//    public void Start()
//    {
//        int count = world.loadedNodes.Count;
//        NativeArray<NodeData> nodeArray = new NativeArray<NodeData>(count, Allocator.TempJob);
//        NativeHashMap<int3, int> positionToIndex = new NativeHashMap<int3, int>(count, Allocator.TempJob);

//        int i = 0;
//        foreach (GameNode node in world.loadedNodes.Values)
//        {
//            int3 pos = new int3(node.x, node.y, node.z);

//            nodeArray[i] = new NodeData
//            {
//                position = pos,
//                isWalkable = node.isWalkable,
//                gCost = int.MaxValue,
//                cameFromIndex = -1,
//                characterIndex = node.character != null ? node.character.data.ID : -1
//            };
//            positionToIndex.Add(pos, i);
//            i++;
//        }

//    }

//    public JobHandle FindPathTaskJob()
//    {
//        PathFindingJob jobHandle = new PathFindingJob();
//        return jobHandle.Schedule();
//    }
//}

//public struct PathFindingJob : IJob
//{
//    public NativeArray<NodeData> nodes;
//    public NativeHashMap<int3, int> positionToIndex;

//    public int startIndex;
//    public int endIndex;
//    public int pathfinderID;

//    public NativeList<int> openList;
//    public NativeHashSet<int> closeHashSet;

//    public void Execute()
//    {
//        NodeData startNode = nodes[startIndex];

//        startNode.gCost = 0;
//        startNode.hCost = CalaculateDistanceCost(startIndex, endIndex);
//        startNode.CalculateFCost();
//        nodes[startIndex] = startNode;

//        while (openList.Length > 0)
//        {
//            int currentIndex = GetLowestFCostIndex();

//            if (currentIndex == endIndex)
//            {
//                //  return calculated path
//            }


//            for (int i = 0; i < openList.Length; i++)
//            {
//                if (openList[i] == currentIndex)
//                {
//                    openList.RemoveAtSwapBack(i);
//                    break;
//                }
//            }
//            closeHashSet.Add(currentIndex);

//            int3 currentPos = nodes[currentIndex].position;
//            NativeList<int> neighbourIndexs = GetNeighbourIndexs(currentPos, 1, 1);

//            foreach (int neighbourIndex in neighbourIndexs)
//            {
//                if (closeHashSet.Contains(neighbourIndex)) continue;

//                if (!nodes[neighbourIndex].isWalkable)
//                {
//                    closeHashSet.Add(neighbourIndex);
//                    continue;
//                }

//                //int neighbourCharacterID = nodes[neighbourIndex].characterIndex;

//                int tentativeGCost = nodes[currentIndex].gCost + CalaculateDistanceCost(currentIndex, neighbourIndex);
//                if (tentativeGCost < nodes[neighbourIndex].gCost)
//                {
//                    NodeData neighbourNode = nodes[neighbourIndex];
//                    neighbourNode.cameFromIndex = currentIndex;
//                    neighbourNode.gCost = tentativeGCost;
//                    neighbourNode.hCost = CalaculateDistanceCost(neighbourIndex, endIndex);
//                    neighbourNode.CalculateFCost();
//                    nodes[neighbourIndex] = neighbourNode;

//                    if (!openList.Contains(neighbourIndex)) 
//                        openList.Add(neighbourIndex);
//                }
//            }
//            neighbourIndexs.Dispose();
//        }
//    }

//    private int CalaculateDistanceCost(int a, int b)
//    {
//        int3 startPos = nodes[a].position;
//        int3 endPos = nodes[b].position;

//        int xCost = Mathf.Abs(endPos.x - startPos.x);
//        int yCost = Mathf.Abs(endPos.y - startPos.y);
//        int zCost = Mathf.Abs(endPos.z - startPos.z);

//        return (xCost + yCost + zCost);
//    }

//    private int GetLowestFCostIndex()
//    {
//        int lowestCostIndex = openList[0];
//        for (int i = 0; i < openList.Length; i++)
//        {
//            int index = openList[i];
//            if (nodes[index].fCost < nodes[lowestCostIndex].fCost)
//            {
//                lowestCostIndex = openList[i];
//            }
//        }
//        return lowestCostIndex;
//    }

//    private NativeList<int> GetNeighbourIndexs(int3 currentPos, int riseLimit, int lowerLimit)
//    {
//        NativeList<int> neighbourList = new NativeList<int>(Allocator.Temp);

//        NativeList<int3> directions = new NativeList<int3>(Allocator.Temp)
//        {
//            new int3(-1, 0, 0), // Left
//            new int3(1, 0, 0), // Right
//            new int3(0, 0, -1), // Back
//            new int3(0, 0, 1), // Forward
//            new int3(0, 1, 0), // Up
//            new int3(0, -1, 0), // Down
//            new int3(1, 0, 1), // Diagonal Forward-Right
//            new int3(-1, 0, 1), // Diagonal Forward-Left
//            new int3(1, 0, -1), // Diagonal Backward-Right
//            new int3(-1, 0, -1) // Diagonal Backward-Left
//        };

//        for (int y = 1; y <= riseLimit; y++)
//        {
//            directions.Add(new int3(1, y, 0));
//            directions.Add(new int3(-1, y, 0));
//            directions.Add(new int3(0, y, 1));
//            directions.Add(new int3(0, y, -1));
//        }

//        for (int y = 1; y <= lowerLimit; y++)
//        {
//            directions.Add(new int3(1, -y, 0));
//            directions.Add(new int3(-1, -y, 0));
//            directions.Add(new int3(0, -y, 1));
//            directions.Add(new int3(0, -y, -1));
//        }
        
//        foreach (var direction in directions)
//        {
//            int3 neighbourPos = currentPos + direction;

//            if (!positionToIndex.TryGetValue(neighbourPos, out int neighbourIndex))
//                continue;

//            if (direction.x != 0 && direction.z != 0)
//            {
//                int3 horizontalPos = currentPos + new int3(direction.x, 0, 0);
//                int3 verticalPos = currentPos + new int3(0, 0, direction.z);

//                if (!positionToIndex.ContainsKey(horizontalPos) ||
//                    !positionToIndex.ContainsKey(verticalPos))
//                    continue;
//            }
//            neighbourList.Add(neighbourIndex);
//        }
//        return neighbourList;
//    }
//}
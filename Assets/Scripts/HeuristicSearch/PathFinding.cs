using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinding
{
    public World world;
    private List<GameNode> openList;
    private HashSet<GameNode> closedList;
    private List<GameNode> processedPath;

    public PathFinding(World world)
    {
        this.world = world;
    }

    /// <summary>
    /// Input the world position to the pathfinding function,
    /// the pathfinding receive the world position and just check for the grid world position without interaction with the chunk
    /// </summary>
    private List<GameNode> FindPath(int startWorldX, int startWorldY, int startWorldZ, 
        int endWorldX, int endWorldY, int endWorldZ,
        CharacterBase pathfinder, int riseLimit, int lowerLimit)
    {
        float startTime = Time.realtimeSinceStartup;
        List<GameNode> ret = new List<GameNode>();

        GameNode startNode = world.GetNode(startWorldX, startWorldY, startWorldZ);
        GameNode endNode = world.GetNode(endWorldX, endWorldY, endWorldZ);

        if (NodeDistance(startNode, endNode) > 200)
        {
            Debug.Log("Pathfinding distance too long");
            return ret;
        }

        if (pathfinder == null) { Debug.LogWarning("Non_character execute find path"); }

        openList = new List<GameNode> { startNode };
        closedList = new HashSet<GameNode>();

        foreach (GameNode pathNode in world.loadedNodes.Values.ToList())
        {
            if (!pathNode.isWalkable) { continue; }
            pathNode.gCost = int.MaxValue;
            pathNode.CalculateFCost();
            pathNode.cameFromNode = null;
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, endNode);
        startNode.CalculateFCost();

        List<Vector3Int> directions = GetNeighbourDirection(riseLimit, lowerLimit);

        while (openList.Count > 0)
        {
            GameNode currentNode = GetLowestFCostNode(openList);
            if (currentNode == endNode)
            {
                float endTime = Time.realtimeSinceStartup;
                //Debug.Log($"Find path completed in {endTime - startTime:F4} seconds");
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (var direction in directions)
            {
                Vector3Int neighbourPos = new Vector3Int(currentNode.x, currentNode.y, currentNode.z) + direction;
                if (world.loadedNodes.TryGetValue(neighbourPos, out GameNode neighbourNode))
                {
                    if (direction.x != 0 && direction.z != 0)
                    {
                        //  To prevent the empty two slide then process diagonal walk cross
                        Vector3Int horizontalPos = currentNode.GetNodeVectorInt() + new Vector3Int(direction.x, 0, 0);
                        Vector3Int verticalPos = currentNode.GetNodeVectorInt() + new Vector3Int(0, 0, direction.z);

                        if (!world.loadedNodes.ContainsKey(horizontalPos) ||
                            !world.loadedNodes.ContainsKey(verticalPos))
                            continue;
                    }

                    if (closedList.Contains(neighbourNode)) continue;

                    if (!neighbourNode.isWalkable)
                    {
                        closedList.Add(neighbourNode);
                        continue;
                    }

                    CharacterBase neighbourCharacter = neighbourNode.GetUnitGridCharacter();
                    if (pathfinder != null && neighbourCharacter != null)
                    {
                        //  pathfinder has no team, cannot pass through any character
                        if (pathfinder.currentTeam == null)
                        {
                            closedList.Add(neighbourNode);
                            continue;
                        }

                        //  neighbour has no team, cannot pass through
                        if (neighbourCharacter.currentTeam == null)
                        {
                            closedList.Add(neighbourNode);
                            continue;
                        }

                        //  cannot pass through different team character
                        if (pathfinder.currentTeam.teamType != neighbourCharacter.currentTeam.teamType)
                        {
                            closedList.Add(neighbourNode);
                            continue;
                        }
                    }

                    Vector3Int offset = neighbourNode.GetNodeVectorInt() - currentNode.GetNodeVectorInt();
                    bool isDiagnols = Mathf.Abs(offset.x) + Mathf.Abs(offset.z) > 1;

                    if (isDiagnols)
                    {
                        Vector3Int horizontalPos = new Vector3Int(currentNode.x + offset.x, currentNode.y, currentNode.z);
                        Vector3Int verticalPos = new Vector3Int(currentNode.x, currentNode.y, currentNode.z + offset.z);

                        world.loadedNodes.TryGetValue(horizontalPos, out GameNode horizontalNode);
                        world.loadedNodes.TryGetValue(verticalPos, out GameNode verticalNode);

                        bool horizontalBlocked = CheckBlockNode(pathfinder, horizontalNode);
                        bool verticalBlocked = CheckBlockNode(pathfinder, verticalNode);

                        if (horizontalBlocked && verticalBlocked)
                        {
                            closedList.Add(neighbourNode);
                            continue;
                        }
                    }

                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighbourNode);
                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNode = currentNode;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.hCost = CalculateDistanceCost(neighbourNode, endNode);
                        neighbourNode.CalculateFCost();

                        if (!openList.Contains(neighbourNode))
                            openList.Add(neighbourNode);
                    }
                }
            }
        }
        // Out of nodes on the openList 
        return ret;
    }

    public int NodeDistance(GameNode a, GameNode b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }

    /// <summary>
    /// In the distance between a and b, first of all need to finding the most shorter distance of x, y, z line
    /// in order to calculate the distance cost, then found the most shorter distance of line.
    /// Note that the A* algorithm triggers this function once every time the cell is moved.
    /// </summary>
    private int CalculateDistanceCost(GameNode a, GameNode b)
    {
        int xCost = Mathf.Abs(b.x - a.x);
        int yCost = Mathf.Abs(b.y - a.y);
        int zCost = Mathf.Abs(b.z - a.z);
        return (xCost + yCost + zCost);
    }

    /// <summary>
    /// Comparing every node in the current calculated nodes, 
    /// get the most lowest f Cost node be the next ideally target.
    /// </summary>
    /// <param name="nodeList"></param>
    /// <returns></returns>
    private GameNode GetLowestFCostNode(List<GameNode> nodeList)
    {
        GameNode lowestFCostNode = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            if (nodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = nodeList[i];
            }
        }
        return lowestFCostNode;
    }

    private List<GameNode> CalculatePath(GameNode endnode)
    {
        List<GameNode> path = new List<GameNode>();
        path.Add(endnode);
        GameNode currentNode = endnode;
        //  Summary
        //      Take the node from the previously saved cameFromNode to return and
        //      path points step by step and generate separate and complete total paths.
        //      Can think like gamenode cameFromNode inside have another gamenode till the start gamenode.cameFromNode is null
        while (currentNode.cameFromNode != null)
        {
            path.Insert(0, currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        return path;
    }

    private List<Vector3Int> GetNeighbourDirection(int riseLimit, int lowerLimit)
    {
        List<Vector3Int> directions = new List<Vector3Int>
        {
            new Vector3Int(-1, 0, 0), // Left
            new Vector3Int(1, 0, 0),  // Right
            new Vector3Int(0, 0, -1), // Back
            new Vector3Int(0, 0, 1),  // Forward
            new Vector3Int(0, 1, 0),  // Up
            new Vector3Int(0, -1, 0), // Down
            new Vector3Int(1, 0, 1),  // Diagonal Forward-Right
            new Vector3Int(-1, 0, 1), // Diagonal Forward-Left
            new Vector3Int(1, 0, -1), // Diagonal Backward-Right
            new Vector3Int(-1, 0, -1) // Diagonal Backward-Left
        };

        for (int y = 1; y <= riseLimit; y++)
        {
            directions.AddRange(new[]
            {
            new Vector3Int(1, y, 0),
            new Vector3Int(-1, y, 0),
            new Vector3Int(0, y, 1),
            new Vector3Int(0, y, -1)
        });
        }

        for (int y = 1; y <= lowerLimit; y++)
        {
            directions.AddRange(new[]
            {
            new Vector3Int(1, -y, 0),
            new Vector3Int(-1, -y, 0),
            new Vector3Int(0, -y, 1),
            new Vector3Int(0, -y, -1)
        });
        }

        return directions;
    }
    private List<GameNode> GetNeighbourList(GameNode currentNode, List<Vector3Int> directions)
    {
        List<GameNode> neighbourList = new List<GameNode>();

        foreach (var direction in directions)
        {
            Vector3Int neighbourPos = new Vector3Int(currentNode.x, currentNode.y, currentNode.z) + direction;
            if (world.loadedNodes.TryGetValue(neighbourPos, out GameNode neighbourNode))
            {
                if (direction.x != 0 && direction.z != 0)
                {
                    //  To prevent the empty two slide then process diagonal walk cross
                    Vector3Int horizontalPos = currentNode.GetNodeVectorInt() + new Vector3Int(direction.x, 0, 0);
                    Vector3Int verticalPos = currentNode.GetNodeVectorInt() + new Vector3Int(0, 0, direction.z);

                    if (!world.loadedNodes.ContainsKey(horizontalPos) ||
                        !world.loadedNodes.ContainsKey(verticalPos))
                        continue;
                }
                neighbourList.Add(neighbourNode);
            }
        }
        return neighbourList;
    }
    public List<GameNode> GetNeighbourNodeCustomized(GameNode currentNode, int size)
    {
        List<GameNode> neighbourList = new List<GameNode>();

        if (size <= 0) { return neighbourList; }

        List<Vector3Int> neighbourPosList = GetNeighbourPosCustomized
            (new Vector3Int(currentNode.x, currentNode.y, currentNode.z), size);

        if (neighbourPosList == null) { return neighbourList;}

        foreach (var neighbourPos in neighbourPosList)
        {
            if (world.loadedNodes.TryGetValue(neighbourPos, out GameNode neighbourNode))
            {
                neighbourList.Add(neighbourNode);
            }
        }
        return neighbourList;
    }
    public List<Vector3Int> GetNeighbourPosCustomized(Vector3Int currentPos, int size)
    {
        if (size <= 0) { return null; }

        List<Vector3Int> neighbourPosList = new List<Vector3Int>();

        for (int dx = -size; dx <= size; dx++)
        {
            for (int dz = -size; dz <= size; dz++)
            {
                // Manhattan distance check
                if (Mathf.Abs(dx) + Mathf.Abs(dz) <= size)
                {
                    // Exclude the center (0,0)
                    if (dx != 0 || dz != 0)
                    {
                        neighbourPosList.Add(
                            new Vector3Int(currentPos.x + dx, currentPos.y, currentPos.z + dz)
                        );
                    }
                }
            }
        }
        return neighbourPosList;
    }
    public void SetProcessPath(Vector3 currentPosition, Vector3 targetPosition, 
        CharacterBase pathFinder, int riseLimit, int lowerLimit)
    {   
        float startTime = Time.realtimeSinceStartup;

        world.GetWorldPosition(targetPosition, out int endX, out int endY, out int endZ);
        world.GetWorldPosition(currentPosition, out int startX, out int startY, out int startZ);

        if (!world.IsValidNode(startX, startY, startZ) || !world.IsValidNode(endX, endY, endZ))
        {
            Debug.Log("Invalid node position");
            processedPath = null;
            return;
        }

        processedPath = FindPath(startX, startY, startZ, endX, endY, endZ, pathFinder, riseLimit, lowerLimit);

        float endTime = Time.realtimeSinceStartup;
        //Debug.Log($"Set process path completed in {endTime - startTime:F4} seconds");
    }
    public PathRoute GetPathRoute(Vector3 start, Vector3 end, 
        CharacterBase pathFinder, int riseLimit, int lowerLimit)
    {
        SetProcessPath(start, end, pathFinder, riseLimit, lowerLimit);
        if (processedPath.Count == 0) return null;
        return new PathRoute(processedPath);
    }
    public int GetTargetNodeCost(GameNode startNode, GameNode endNode, 
        CharacterBase pathFinder, int riseLimit, int lowerLimit)
    {
        Vector3 start = startNode.GetNodeVector();
        Vector3 end = endNode.GetNodeVector();
        SetProcessPath(start, end, pathFinder, riseLimit, lowerLimit);
        if (processedPath.Count == 0) { Debug.Log("No path"); return int.MaxValue; }
        return processedPath.Count;
    }

    #region Dijkstra Region Search
    public List<GameNode> GetLowestCostNodes(GameNode targetNode, List<GameNode> reachableNodes)
    {
        List<GameNode> compareNodes = GetCalculateDijkstraCostNodes(targetNode, 2, 1, 1);

        int lowestCost = int.MaxValue;
        foreach (GameNode node in compareNodes)
        {
            if (!reachableNodes.Contains(node)) continue;
            if (node.dijkstraCost < lowestCost)
                lowestCost = node.dijkstraCost;
        }

        List<GameNode> lowestCostNodes = new List<GameNode>();
        foreach (GameNode node in compareNodes)
        {
            if (!reachableNodes.Contains(node)) continue;
            if (node.dijkstraCost == lowestCost)
                lowestCostNodes.Add(node);
        }
        return lowestCostNodes;
    }

    /// <summary>
    /// Get the coverange from input position then use the dijkstra algorithm
    /// to calculate the cost of each node check if the cost is lower than the
    /// movable range cost then add to the result list
    /// </summary>
    public List<Vector3Int> GetCostDijkstraCoverangePos(Vector3 start, int heightCheck, 
        int movableRangeCost, int riseLimit, int lowerLimit)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        List<GameNode> costNodes = GetCalculateDijkstraCostNodes(start, heightCheck, riseLimit, lowerLimit);
        foreach (GameNode node in costNodes)
        {
            if (node.dijkstraCost <= movableRangeCost)
            {
                result.Add(new Vector3Int(node.x, node.y, node.z));
            }
        }
        return result;
    }
    public List<GameNode> GetCostDijkstraCoverangeNodes(Vector3 start, int heightCheck, 
        int movableRangeCost, int riseLimit, int lowerLimit)
    {
        List<GameNode> result = new List<GameNode>();
        List<GameNode> costNodes = GetCalculateDijkstraCostNodes(start, heightCheck, riseLimit, lowerLimit);
        foreach (GameNode node in costNodes)
        {
            if (node.dijkstraCost <= movableRangeCost)
            {
                result.Add(node);
            }
        }
        return result;
    }

    /// <summary>
    /// Get the coverange gamenode from input position then use the dijkstra algorithm
    /// to calculate the cost of each node till all the walkable node is calculated
    /// or the cost is over the 200 limit
    /// </summary>
    public List<GameNode> GetCalculateDijkstraCostNodes(Vector3 start, int heightCheck, 
        int riseLimit, int lowerLimit)
    {
        GameNode startNode = world.GetNode(start);
        return GetCalculateDijkstraCostNodes(startNode, heightCheck, riseLimit, lowerLimit);
    }
    public List<GameNode> GetCalculateDijkstraCostNodes(GameNode startNode, int heightCheck,
    int riseLimit, int lowerLimit)
    {
        if (startNode == null)
        {
            Debug.LogWarning("Invalid start node position");
            return new List<GameNode>();
        }
        foreach (GameNode gameNode in world.loadedNodes.Values)
        {
            if (!gameNode.isWalkable) { continue; }
            gameNode.dijkstraCost = int.MaxValue;
            gameNode.cameFromNode = null;
        }
        startNode.dijkstraCost = 0;

        List<GameNode> openList = new List<GameNode> { startNode };
        List<GameNode> calcualtedNode = new List<GameNode> { startNode };

        List<Vector3Int> directions = GetNeighbourDirection(riseLimit, lowerLimit);

        while (openList.Count > 0)
        {
            GameNode currentNode = openList[0];
            openList.RemoveAt(0);

            List<GameNode> neighbourNodes = GetNeighbourList(currentNode, directions);
            foreach (GameNode neighbourNode in neighbourNodes)
            {
                if (!neighbourNode.isWalkable) { continue; }

                if (!CheckIsStandableNode(neighbourNode, heightCheck)) { continue; }

                int tentativeGCost = currentNode.dijkstraCost + CalculateSlopeCost(currentNode, neighbourNode);

                //  Limit the searching range to avoid the long pathfinding time
                if (tentativeGCost > 200)
                    continue;

                if (tentativeGCost < neighbourNode.dijkstraCost)
                {
                    neighbourNode.dijkstraCost = tentativeGCost;
                    neighbourNode.cameFromNode = currentNode;
                    calcualtedNode.Add(neighbourNode);
                    if (!openList.Contains(neighbourNode))
                        openList.Add(neighbourNode);
                }
            }
        }

        return calcualtedNode;
    }

    public List<GameNode> GetCostDijkstraCoverangeNodes(CharacterBase pathfinder, 
        int movableRangeCost, int riseLimit, int lowerLimit)
    {
        List<GameNode> result = new List<GameNode>();
        List<GameNode> costNodes = GetCalculateDijkstraCostNodes(pathfinder, riseLimit, lowerLimit, movableRangeCost);
        foreach (GameNode node in costNodes)
        {
            if (node.dijkstraCost <= movableRangeCost)
            {
                result.Add(node);
            }
        }
        return result;
    }
    public List<GameNode> GetCalculateDijkstraCostNodes(CharacterBase pathfinder, 
        int riseLimit, int lowerLimit, int seacrhTillCost = 200)
    {
        GameNode startNode = pathfinder.currentNode;

        foreach (GameNode gameNode in world.loadedNodes.Values)
        {
            CharacterBase unit = gameNode.GetUnitGridCharacter();

            if (!gameNode.isWalkable) continue;
            if (unit != null && unit.currentTeam.teamType 
                != pathfinder.currentTeam.teamType) continue;

            gameNode.dijkstraCost = int.MaxValue;
            gameNode.cameFromNode = null;
        }
        startNode.dijkstraCost = 0;

        List<GameNode> openList = new List<GameNode> { startNode };
        List<GameNode> calcualtedNodes = new List<GameNode> { startNode };

        List<Vector3Int> directions = GetNeighbourDirection(riseLimit, lowerLimit);

        while (openList.Count > 0)
        {
            GameNode currentNode = openList[0];
            openList.RemoveAt(0);

            List<GameNode> neighbourNodes = GetNeighbourList(currentNode, directions);

            foreach (GameNode neighbourNode in neighbourNodes)
            {
                if (!neighbourNode.isWalkable) { continue; }
                if (neighbourNode.character != null && neighbourNode.character.currentTeam.teamType 
                    != pathfinder.currentTeam.teamType) 
                { 
                    continue; 
                }

                if (!CheckIsStandableNode(neighbourNode, 2)) { continue; }

                Vector3Int offset = neighbourNode.GetNodeVectorInt() - currentNode.GetNodeVectorInt();
                bool isDiagonal = Mathf.Abs(offset.x) + Mathf.Abs(offset.z) > 1;
                if (isDiagonal) 
                { 
                    Vector3Int horizontalPos = new Vector3Int(currentNode.x + offset.x, currentNode.y, currentNode.z); 
                    Vector3Int verticalPos = new Vector3Int(currentNode.x, currentNode.y, currentNode.z + offset.z);

                    world.loadedNodes.TryGetValue(horizontalPos, out GameNode horizontalNode);
                    world.loadedNodes.TryGetValue(verticalPos, out GameNode verticalNode);

                    bool horizontalBlocked = CheckBlockNode(pathfinder, horizontalNode);
                    bool verticalBlocked = CheckBlockNode(pathfinder, verticalNode);

                    if (horizontalBlocked && verticalBlocked)
                        continue;
                }

                int tentativeGCost = currentNode.dijkstraCost + CalculateSlopeCost(currentNode, neighbourNode);

                //  Limit the searching range to avoid the long pathfinding time
                if (tentativeGCost > seacrhTillCost)
                    continue;

                if (tentativeGCost < neighbourNode.dijkstraCost)
                {
                    neighbourNode.dijkstraCost = tentativeGCost;
                    neighbourNode.cameFromNode = currentNode;
                    calcualtedNodes.Add(neighbourNode);
                    if (!openList.Contains(neighbourNode))
                        openList.Add(neighbourNode);
                }
            }
        }
        return calcualtedNodes;
    }

    private int CalculateSlopeCost(GameNode a, GameNode b)
    {
        int xCost = Mathf.Abs(b.x - a.x);
        int height = b.y - a.y;
        int zCost = Mathf.Abs(b.z - a.z);
        if (height > 0)
        {
            return height + xCost + zCost;
        }
        else
        {
            return xCost + zCost;
        }
    }

    private bool CheckIsStandableNode(GameNode node, int heightCheck)
    {
        if (!node.isWalkable) { return false; }

        for (int offset = 1; offset <= heightCheck; offset++)
        {
            GameNode above = world.GetNode(node.x, node.y + offset, node.z);
            if (above != null && above.hasCube)
            {
                //Debug.Log($"Node at {above.GetVector()} is the obstacle from node {node.GetVector()}.");
                return false;
            }
        }
        return true;
    }
    private bool CheckBlockNode(CharacterBase pathfinder, GameNode node)
    {
        if (node != null)
        {
            if (!node.isWalkable) return true;

            CharacterBase nodeCharacter = node.GetUnitGridCharacter();
            if (nodeCharacter == null)
                return false;

            if (pathfinder.currentTeam.teamType != nodeCharacter.currentTeam.teamType)
                return true;
        }
        return false;
    }
    #endregion
}
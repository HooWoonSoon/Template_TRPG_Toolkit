using System.Collections.Generic;
using UnityEngine;

public class World
{
    //  Summary
    //      To store the loaded nodes in the world, which nodes could not be the local position of the chunk
    public Dictionary<Vector3Int, GameNode> loadedNodes = new Dictionary<Vector3Int, GameNode>();

    public int worldMaxX, worldMaxZ;
    public int worldMinX, worldMinZ;
    public int worldHeight;
    public float cellSize = 1f;

    public GameObject combinedMesh;

    public void UpdateAndReleaseMapNode(List<GameNodeData> nodeDataList)
    {
        ResetMapNode();
        UpdateMapNode(nodeDataList);
    }

    public void ResetMapNode()
    {
        loadedNodes.Clear();

        worldMaxX = int.MinValue;
        worldMaxZ = int.MinValue;
        worldMinX = int.MaxValue;
        worldMinZ = int.MaxValue;
        worldHeight = 0;
    }

    public void UpdateMapNode(List<GameNodeData> nodeDataList)
    {
        for (int i = 0; i < nodeDataList.Count; i++)
        {
            int x = nodeDataList[i].x;
            int y = nodeDataList[i].y;
            int z = nodeDataList[i].z;
            bool isWalkable = nodeDataList[i].isWalkable;
            bool hasCube = nodeDataList[i].hasCube;
            bool isDeployable = nodeDataList[i].isDeployable;
            if (!loadedNodes.ContainsKey(new Vector3Int(x, y, z)))
            {
                GameNode gameNode = new GameNode(x, y, z, isWalkable, hasCube, isDeployable);
                loadedNodes.Add(new Vector3Int(x, y, z), gameNode);
                UpdateWorldSize(x, y, z);
            }
        }
    }
    private void UpdateWorldSize(int x, int height, int z)
    {
        worldMaxX = Mathf.Max(worldMaxX, x);
        worldMaxZ = Mathf.Max(worldMaxZ, z);

        worldMinX = Mathf.Min(worldMinX, x);
        worldMinZ = Mathf.Min(worldMinZ, z);

        worldHeight = Mathf.Max(worldHeight, height);
        //Debug.Log($"World size: {worldMaxX} {worldMaxZ}");
    }

    public void GenerateNode(int x, int height, int z)
    {
        if (AddNode(x, height, z))
        {
            AdjustCoverNode(x, height, z);
        }
    }

    private bool AddNode(int x, int y, int z)
    {
        if (!loadedNodes.ContainsKey(new Vector3Int(x, y, z)))
        {
            GameNode gameNode = new GameNode(x, y, z, true, true, true);
            loadedNodes.Add(new Vector3Int(x, y, z), gameNode);
            UpdateWorldSize(x, y, z);
            return true;
        }
        return false;
    }

    private void AdjustCoverNode(int x, int y, int z)
    {
        GameNode node = GetNode(x, y, z);
        GameNode aboveNode = GetNode(x, y + 1, z);
        if (aboveNode != null && aboveNode != null && aboveNode.hasCube)
        {
            node.isWalkable = false;
        }
    }

    public void GetWorldPosition(Vector3 worldPosition, out int x, out int y, out int z)
    {
        x = Mathf.FloorToInt(worldPosition.x);
        y = Mathf.FloorToInt(worldPosition.y);
        z = Mathf.FloorToInt(worldPosition.z);
    }
    public GameNode GetNode(Vector3 position)
    {
        GameNode node;
        if (loadedNodes.TryGetValue(new Vector3Int((int)position.x, (int)position.y, (int)position.z), out node))
            return node;
        return null;
    } 
    public GameNode GetNode(int x, int y, int z)
    {
        GameNode node;
        if (loadedNodes.TryGetValue(new Vector3Int(x, y, z), out node))
            return node;
        return null;
    }

    public GameNode GetHeightNodeWithCube(int x, int z)
    {
        if (x > worldMaxX || x < worldMinX || z > worldMaxZ || z < worldMinZ) { return null; }

        for (int y = worldHeight; y >= 0; y--)
        {
            if (loadedNodes.TryGetValue(new Vector3Int(x, y, z), out GameNode node))
            {
                if (node.hasCube) return node;
            }   
        }
        return null;
    }

    public Vector3 GetCellOffsetPosition(Vector3 position)
    {
        float halfCell = cellSize / 2f;
        float y = position.y - halfCell;
        return new Vector3(position.x, y, position.z);
    }

    #region External
    ///  <summary>
    /// To check if the input position is valid in the world.
    /// </summary>
    public bool IsValidWorldRange(Vector3 position)
    {
        Vector3 localPosition = GetCellOffsetPosition(position);
        if (worldMinX <= localPosition.x && 0 <= localPosition.y && worldMinZ <= localPosition.z
        && worldMaxX >= localPosition.x && worldHeight >= localPosition.y && worldMaxZ >= localPosition.z)
        {
            //Debug.Log($"{localPosition} is valid");
            return true;
        }
        //Debug.Log($"{localPosition} is invalid");
        return false;
    }
    ///  <summary>
    /// To check if the input x, y, z is valid in the world.
    /// </summary>
    public bool IsValidNode(int x, int y, int z)
    {
        return loadedNodes.ContainsKey(new Vector3Int(x, y, z));
    }

    public bool CheckSolidNodeBound(Bounds bounds)
    {
        int minX = Mathf.FloorToInt((bounds.min.x + cellSize * 0.5f) / cellSize);
        int minY = Mathf.FloorToInt((bounds.min.y + cellSize * 0.5f) / cellSize);
        int minZ = Mathf.FloorToInt((bounds.min.z + cellSize * 0.5f) / cellSize);

        int maxX = Mathf.FloorToInt((bounds.max.x + cellSize * 0.5f) / cellSize);
        int maxY = Mathf.FloorToInt((bounds.max.y + cellSize * 0.5f) / cellSize);
        int maxZ = Mathf.FloorToInt((bounds.max.z + cellSize * 0.5f) / cellSize);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (loadedNodes.TryGetValue(pos, out GameNode node))
                    {
                        if (node.hasCube)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public bool CheckSolidNodeLine(Vector3 start, Vector3 end)
    {
        return CheckSolidNodeLine(start.x, start.y, start.z, end.x, end.y, end.z);
    }

    public bool CheckSolidNodeLine(float startX, float startY, float startZ,
        float endX, float endY, float endZ)
    {
        Vector3 start = new Vector3(startX, startY, startZ);
        Vector3 end = new Vector3(endX, endY, endZ);

        Vector3 delta = end - start;
        float distance = delta.magnitude;
        
        if (distance < 0.0001f) return false;

        Vector3 direction = delta / distance;

        float step = cellSize * 0.25f;
        float current = 0f;

        while (current <= distance)
        {
            Vector3 sample = start + direction * current;

            if (CheckSolidNode(sample))
                return true;

            current += step;
        }

        if (CheckSolidNode(end))
            return true;

        return false;
    }

    public bool CheckSolidNode(Vector3 position)
    {
        return CheckSolidNode(position.x, position.y, position.z);
    }

    public bool CheckSolidNode(float x, float y, float z)
    {
        int bx = Mathf.FloorToInt((x + cellSize * 0.5f) / cellSize);
        int by = Mathf.FloorToInt((y + cellSize * 0.5f) / cellSize);
        int bz = Mathf.FloorToInt((z + cellSize * 0.5f) / cellSize);

        Vector3Int position = new Vector3Int(bx, by, bz);
        if (loadedNodes.TryGetValue(position, out GameNode gameNode))
        {
            return gameNode.hasCube;
        }
        return false;
    }

    public List<GameNode> GetAllSolidNodeList()
    {
        List<GameNode> solidNodes = new List<GameNode>();
        foreach (var node in loadedNodes.Values)
        {
            if (node.hasCube)
                solidNodes.Add(node);
        }
        return solidNodes;
    }
    public HashSet<GameNode> GetAllSolidNodeSet()
    {
        HashSet<GameNode> solidNodes = new HashSet<GameNode>();
        foreach (var node in loadedNodes.Values)
        {
            if (node.hasCube)
                solidNodes.Add(node);
        }
        return solidNodes;
    }
    public List<Vector3> GetAllSolidPos()
    {
        List<Vector3> solidPos = new List<Vector3>();

        foreach (var kvp in loadedNodes)
        {
            GameNode node = kvp.Value;
            if (node.hasCube)
            {
                Vector3Int gridPos = kvp.Key;

                Vector3 worldCenter = new Vector3(
                    gridPos.x * cellSize,
                    gridPos.y * cellSize,
                    gridPos.z * cellSize
                );

                solidPos.Add(worldCenter);
            }
        }

        return solidPos;
    }
    public List<GameNode> GetAllWalkableNodeList()
    {
        List<GameNode> walkableNode = new List<GameNode>();
        foreach (var node in loadedNodes.Values)
        {
            if (node.isWalkable)
                walkableNode.Add(node);
        }
        return walkableNode;
    }
    public HashSet<GameNode> GetAllWalkableNodeSet()
    {
        HashSet<GameNode> walkableNode = new HashSet<GameNode>();
        foreach (var node in loadedNodes.Values)
        {
            if (node.isWalkable)
                walkableNode.Add(node);
        }
        return walkableNode;
    }
    #endregion

    #region Manhattan Distance Logic
    /// <summary>
    /// Get the Manhattan distance range walkable position in 3D space
    /// </summary>
    public List<Vector3Int> GetManhattas3DGameNodePosition(
    Vector3Int position,
    int size,
    bool limitY = false,
    bool checkWalkable = false,
    int yLength = 0
    )
    {
        List<Vector3Int> coverage = new List<Vector3Int>();
        List<Vector3Int> positions = GetManhattas3DRangePosition(position, size, limitY, yLength);
        foreach (Vector3Int pos in positions)
        {
            GameNode node = GetNode(pos);
            if (node == null) continue;
            if (checkWalkable && !node.isWalkable) continue;
            coverage.Add(pos);
        }
        return coverage;
    }

    /// <summary>
    /// Get the Manhattan distance range node in 3D space
    /// </summary>
    public List<GameNode> GetManhattas3DGameNode(
        Vector3Int unitPosition,
        int size,
        bool limitY = false,
        bool checkWalkable = false,
        int yLength = 0
        )
    {
        List<GameNode> coverage = new List<GameNode>();
        List<Vector3Int> positions = GetManhattas3DRangePosition(unitPosition, size, limitY, yLength);
        foreach (Vector3Int pos in positions)
        {
            GameNode node = GetNode(pos);
            if (node == null) continue;
            if (checkWalkable && !node.isWalkable) continue;
            coverage.Add(node);
        }
        return coverage;
    }
    /// <summary>
    /// Get the Manhattan distance range position in 3D space
    /// </summary>
    public List<Vector3Int> GetManhattas3DRangePosition(
        Vector3Int unitPosition,
        int size,
        bool limitY = false,
        int yLength = 0)
    {
        List<Vector3Int> coverage = new List<Vector3Int>();
        
        if (limitY && size > yLength) { size = yLength; }

        //  To avoid the unit origin position would extend the uneccesary range
        if (size <= 0) { return coverage; }
        int excludeOriginExtent = size - 1;

        int minX = unitPosition.x - excludeOriginExtent;
        int maxX = unitPosition.x + excludeOriginExtent;
        int minY = unitPosition.y - excludeOriginExtent;
        int maxY = unitPosition.y + excludeOriginExtent;
        int minZ = unitPosition.z - excludeOriginExtent;
        int maxZ = unitPosition.z + excludeOriginExtent;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    // Check if the current position is within the Manhattan distance range
                    int manhattasDistance = Mathf.Abs(unitPosition.x - x)
                             + Mathf.Abs(unitPosition.y - y)
                             + Mathf.Abs(unitPosition.z - z);
                    if (manhattasDistance > excludeOriginExtent) continue;

                    coverage.Add(new Vector3Int(x, y, z));
                }
            }
        }
        return coverage;
    }
    #endregion
}
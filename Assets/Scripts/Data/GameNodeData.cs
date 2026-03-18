using System;
using System.Collections.Generic;
using UnityEngine;

public struct BlockFace
{
    public Vector3 position;
    public List<Vector3Int> normal;
    public Material material;
}

[Serializable]
public class GameNodeData
{
    public int x, y, z;
    public bool isWalkable;
    public bool hasCube;
    public bool isDeployable;

    public GameNodeData(int x, int y, int z, bool isWalkable, bool hasCube, bool isDeployable)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.isWalkable = isWalkable;
        this.hasCube = hasCube;
        this.isDeployable = isDeployable;
    }
}
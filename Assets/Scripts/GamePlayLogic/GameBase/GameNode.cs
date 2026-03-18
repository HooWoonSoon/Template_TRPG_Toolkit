using System;
using UnityEngine;

[Serializable]
public class GameNode
{
    public event EventHandler<OnWorldNodesChange> onWorldNodesChange;
    public class OnWorldNodesChange : EventArgs
    {
        public int x;
        public int y;
        public int z;
    }

    public int x, y, z;

    public bool isWalkable;
    public bool hasCube;
    public bool isDeployable;

    #region Dijkstra Pathfinding Data
    public int dijkstraCost;
    #endregion

    #region A * Pathfinding Data
    public int gCost;
    public int hCost;
    public int fCost;
    #endregion

    public GameNode cameFromNode;

    public GameNode(int x, int y, int z, bool isWalkable, bool hasCube, bool isDeployable)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.isWalkable = isWalkable;
        this.hasCube = hasCube;
        this.isDeployable = isDeployable;
    }

    //  Summary
    //      Logic to calculate the f cost of the node used in A * pathfinding algorithm
    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    public Vector3 GetNodeVector()
    {
        return new Vector3(x, y, z);
    }
    public Vector3Int GetNodeVectorInt()
    {
        return new Vector3Int(x, y, z);
    }

    //  Summary
    //      Logic to set the tile
    private TilemapSprite tilemapSprite;
    public enum TilemapSprite
    {
        None, Blue, Red, Purple, TinyBlue
    }
    public void SetTilemapSprite(TilemapSprite tilemapSprite)
    {
        this.tilemapSprite = tilemapSprite;
        TriggerWorldNodeChanged(x, y, z);
    }
    public TilemapSprite GetTilemapSprite()
    {
        return tilemapSprite;
    }

    public CharacterBase character { get; private set; }
    public void SetUnitGridCharacter(CharacterBase character)
    {
        this.character = character;
    }
    public CharacterBase GetUnitGridCharacter()
    {
        return character;
    }

    public void TriggerWorldNodeChanged(int x, int y, int z)
    {
        if (onWorldNodesChange != null) onWorldNodesChange(this, new OnWorldNodesChange { x = x, y = y, z = z });
    }
}

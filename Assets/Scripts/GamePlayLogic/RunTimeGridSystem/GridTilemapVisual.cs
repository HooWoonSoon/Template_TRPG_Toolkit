using System.Collections.Generic;
using UnityEngine;

public class GridTilemapVisual : Entity
{
    [System.Serializable]
    public struct TilemapSpriteUV
    {
        public GameNode.TilemapSprite tilemapSprite;
        public Vector2Int uv00Pixels;
        public Vector2Int uv11Pixels;
    }

    private struct UVsCoords
    {
        public Vector2 uv00;
        public Vector2 uv11;
    }

    [SerializeField] private TilemapSpriteUV[] tileSpriteUVArray;
    private Dictionary<GameNode.TilemapSprite, UVsCoords> uvCoordsDictionary;
    private GameNode node;
    private Mesh mesh;
    private bool updateMesh;
    public static GridTilemapVisual instance { get; private set; }

    private void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Texture texture = GetComponent<MeshRenderer>().material.mainTexture;
        float textureWidth = texture.width;
        float textureHeight = texture.height;

        uvCoordsDictionary = new Dictionary<GameNode.TilemapSprite, UVsCoords>();

        foreach (TilemapSpriteUV tilemapSpriteUV in tileSpriteUVArray)
        {
            uvCoordsDictionary[tilemapSpriteUV.tilemapSprite] = new UVsCoords
            {
                uv00 = new Vector2(tilemapSpriteUV.uv00Pixels.x / textureWidth, tilemapSpriteUV.uv00Pixels.y / textureHeight),
                uv11 = new Vector2(tilemapSpriteUV.uv11Pixels.x / textureWidth, tilemapSpriteUV.uv11Pixels.y / textureHeight)
            };
        }
        instance = this;
    }
    protected override void Start()
    {
        base.Start();
        SubscribeAllNodes();
    }

    private void LateUpdate()
    {
        if (updateMesh)
        {
            updateMesh = false;
            UpdateTilemapVisual();
        }
    }

    public void DeSubcribeAllNodes()
    {
        foreach (var kvp in world.loadedNodes)
        {
            GameNode node = kvp.Value;
            if (node != null)
                node.onWorldNodesChange -= OnWorldTileChanged;
        }
    }

    public void SubscribeAllNodes()
    {
        foreach (var kvp in world.loadedNodes)
        {
            GameNode node = kvp.Value;
            if (node != null)
                node.onWorldNodesChange += OnWorldTileChanged;
        }
    }

    private void OnWorldTileChanged(object sender, GameNode.OnWorldNodesChange e)
    {
        updateMesh = true;
    }
    private void UpdateTilemapVisual()
    {
        Utils.CreateEmptyMeshArrays(world.loadedNodes.Count, out Vector3[] vertices, out Vector2[] uvs, out int[] triangles);

        int index = 0;
        foreach (var kvp in world.loadedNodes)
        {
            Vector3Int pos = kvp.Key;
            GameNode node = kvp.Value;
            if (node == null) continue;

            Vector3 cubeSize = Vector3.one;
            GameNode.TilemapSprite tilemapSprite = node.GetTilemapSprite();
            Vector2 gridValueUV00, gridValueUV11;
            if (tilemapSprite == GameNode.TilemapSprite.None)
            {
                gridValueUV00 = Vector2.zero;
                gridValueUV11 = Vector2.zero;
                cubeSize = Vector2.zero;
            }
            else
            {
                UVsCoords uvCoords = uvCoordsDictionary[tilemapSprite];
                gridValueUV00 = uvCoords.uv00;
                gridValueUV11 = uvCoords.uv11;
            }
            Utils.AddToMeshArrays(vertices, uvs, triangles, index, pos, 0, cubeSize, gridValueUV00, gridValueUV11);
            index++;
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
    }

    #region External Method
    public void SetAllTileSprite(World world, GameNode.TilemapSprite tilemapSprite)
    {
        foreach (GameNode gameNode in world.loadedNodes.Values)
        {
            SetTilemapSprite(gameNode, tilemapSprite);
        }
    }
    public void SetAllTileSprite(GameNode.TilemapSprite tilemapSprite)
    {
        foreach (GameNode gameNode in world.loadedNodes.Values)
        {
            SetTilemapSprite(gameNode, tilemapSprite);
        }
    }
    public void SetTilemapSprite(Vector3Int worldPosition, GameNode.TilemapSprite tilemapSprite)
    {
        GameNode tilemapNode = world.GetNode(worldPosition);
        if (tilemapNode != null)
        {
            tilemapNode.SetTilemapSprite(tilemapSprite);
        }
    }
    public void SetTilemapSprite(GameNode gameNode, GameNode.TilemapSprite tilemapSprite)
    {
        if (gameNode != null)
        {
            gameNode.SetTilemapSprite(tilemapSprite);
        }
    }
    public void SetTilemapSprites(List<GameNode> gameNodes, GameNode.TilemapSprite tilemapSprite)
    {
        foreach (GameNode gameNode in gameNodes)
        {
            SetTilemapSprite(gameNode, tilemapSprite);
        }
    }
    #endregion
}
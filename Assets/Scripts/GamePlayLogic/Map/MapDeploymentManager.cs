using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class MapDeploymentManager : Entity
{
    [Header("Deployable Character")]
    public CharacterBase[] allCharacter;
    private List<CharacterBase> selectedCharacters = new List<CharacterBase>();

    private List<GameNode> deployableNodes = new List<GameNode>();
    private Dictionary<CharacterBase, GameNode> occupiedNodes = new Dictionary<CharacterBase, GameNode>();

    public int maxDeploymentCount { get; private set; }
    private int currentDeploymentCount;

    [Header("Deployment Gizmos")]
    [SerializeField] private GridCursor gridCursor;
    private GameNode lastNode;

    private CharacterBase lasSelectedCharacter;

    public Material previewMaterial;
    private GameObject previewCharacter;

    private bool enableEditing = false;
    public static MapDeploymentManager instance { get; private set;}

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
        if (!enableEditing) { return; }

        if (gridCursor.currentNode != lastNode)
        {
            lastNode = gridCursor.currentNode;

            if (previewCharacter != null && gridCursor.currentNode != null)
            {
                Vector3 offset = lasSelectedCharacter.transform.position - lasSelectedCharacter.GetCharacterTranformToNodePos();
                previewCharacter.transform.position = gridCursor.currentNode.GetNodeVector() + offset;
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            GameNode targetNode = gridCursor.currentNode;
            CharacterBase character = targetNode.GetUnitGridCharacter();

            if (lasSelectedCharacter == null)
            {
                if (character != null)
                {
                    lasSelectedCharacter = character;
                    GeneratePreviewCharacter(character);
                }
            }
            else
            {         
                ReplaceCharacterNode(lasSelectedCharacter, targetNode);
                lasSelectedCharacter = null;
                DestroyPreviewModel();
            }
        }
    }

    private void ResetModifier()
    {
        occupiedNodes = new Dictionary<CharacterBase, GameNode>();
        deployableNodes = new List<GameNode>();
        selectedCharacters = new List<CharacterBase>();
        maxDeploymentCount = 0;
        currentDeploymentCount = 0;
    }

    public void StartDeployment(MapData mapData)
    {
        ResetModifier();

        List<GameNode> deployableNode = FindDeployableNodes(mapData);
        this.deployableNodes = deployableNode;

        maxDeploymentCount = mapData.maxDeployUnitCount;
        if (maxDeploymentCount >= deployableNode.Count)
        {
            maxDeploymentCount = deployableNode.Count;
        }

        GridTilemapVisual.instance.SetAllTileSprite(world, GameNode.TilemapSprite.None);
        GridTilemapVisual.instance.SetTilemapSprites(this.deployableNodes, GameNode.TilemapSprite.TinyBlue);

        CasualPutGridCursorAtLoadedMap();

        GameEvent.onDeploymentStart?.Invoke();
    }

    //  Summary
    //      Find all deployable nodes from original map data
    private List<GameNode> FindDeployableNodes(MapData mapData)
    {
        List<GameNode> deployableNodes = new List<GameNode>();

        string mapDataPath = mapData.mapDataPath;
        string fullPath = Path.Combine(Application.persistentDataPath, mapDataPath);
        string json = File.ReadAllText(fullPath);
        List<GameNodeData> nodeDataList = JsonConvert.DeserializeObject<List<GameNodeData>>(json);
        foreach (GameNodeData nodeData in nodeDataList)
        {
            if (nodeData.isDeployable)
            {
                GameNode node = world.GetNode(nodeData.x, nodeData.y, nodeData.z);
                deployableNodes.Add(node);
            }
        }
        return deployableNodes;
    }

    public void RandomDeploymentCharacter(CharacterBase character)
    {
        if (character == null)
        {
            Debug.LogWarning("Character is null, cannot deploy!");
            return;
        }

        if (deployableNodes == null || deployableNodes.Count == 0)
        {
            Debug.LogWarning("No deployable nodes found!");
            return;
        }

        List<GameNode> availableNodes = deployableNodes.FindAll(n => !occupiedNodes.ContainsValue(n) && n.isWalkable 
        && n.GetUnitGridCharacter() == null);

        if (availableNodes.Count == 0)
        {
            Debug.LogWarning("No available nodes to deploy the character!");
            return;
        }

        if (maxDeploymentCount <= currentDeploymentCount)
        {
            Debug.Log($"Max deploy character not beyond {maxDeploymentCount}");
            return;
        }

        GameNode randomNode = availableNodes[Random.Range(0, availableNodes.Count)];
        character.gameObject.SetActive(true);
        character.TeleportToNodeDeployble(randomNode);
        occupiedNodes[character] = randomNode;
        currentDeploymentCount++;

        if (!selectedCharacters.Contains(character))
        {
            selectedCharacters.Add(character);
        }

        Debug.Log($"Deployed {character.data.characterName} at {randomNode.x},{randomNode.y},{randomNode.z}");
    }
    public void RemoveCharacterDeployment(CharacterBase character)
    {
        if (!occupiedNodes.TryGetValue(character, out GameNode node)) return;

        node.SetUnitGridCharacter(null);
        character.gameObject.SetActive(false);
        occupiedNodes.Remove(character);
        selectedCharacters.Remove(character);
        currentDeploymentCount--;

        if (lasSelectedCharacter == character)
        {
            lasSelectedCharacter = null;
            DestroyPreviewModel();
        }
    }

    #region Edit Character Deploy Placement
    //  Summary
    //      Replace selected character to target node, if target node occupied, exchange their position
    public void ReplaceCharacterNode(CharacterBase selectedCharacter, GameNode targetNode)
    {
        if (selectedCharacter == null || targetNode == null) return;
        if (!deployableNodes.Contains(targetNode)) { return; }

        CharacterBase targetNodeCharacter = targetNode.GetUnitGridCharacter();
        if (targetNodeCharacter == null)
        {
            ChangeCharacterNode(selectedCharacter, targetNode);
        }
        else
        {
            ExchangeNodeCharacter(selectedCharacter, targetNodeCharacter);
        }
    }
    //  Summary
    //      Move a character to target node and modified its occupied node
    private void ChangeCharacterNode(CharacterBase character, GameNode targetNode)
    {
        occupiedNodes.TryGetValue(character, out GameNode currentNode);
        if (currentNode == null) return;

        character.SetSelfToNode(targetNode, 0.5f);
        targetNode.SetUnitGridCharacter(character);
        occupiedNodes[character] = targetNode;
        currentNode.SetUnitGridCharacter(null);
        Debug.Log($"Moved {character.name} to {targetNode.x},{targetNode.y},{targetNode.z}");
    }
    //  Summary
    //      Swap two characters' positions on the grid and modified their occupied nodes
    private void ExchangeNodeCharacter(CharacterBase character, CharacterBase otherCharacter)
    {
        GameNode currentNode = occupiedNodes[character];
        GameNode otherNode = occupiedNodes[otherCharacter];

        character.SetSelfToNode(otherNode, 0.5f);
        otherCharacter.SetSelfToNode(currentNode, 0.5f);
        occupiedNodes[character] = otherNode;
        occupiedNodes[otherCharacter] = currentNode;
        currentNode.SetUnitGridCharacter(otherCharacter);
        otherNode.SetUnitGridCharacter(character);
        Debug.Log($"Swapped {character.name} <-> {otherCharacter.name}");
    }
    #endregion

    public void GeneratePreviewCharacter(CharacterBase character)
    {
        DestroyPreviewModel();

        Vector3 offset = character.transform.position - character.GetCharacterTranformToNodePos();
        previewCharacter = Instantiate(character.characterModel);
        previewCharacter.transform.position = gridCursor.currentNode.GetNodeVector() + offset;

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

    public void ActivateMoveCursorAndHide(bool active, bool hide)
    {
        gridCursor.ActivateMoveCursor(active, hide);
    }
    public void SetGridCursorAt(GameNode target)
    {
        gridCursor.SetGridCursorAt(target);
    }
    private void CasualPutGridCursorAtLoadedMap()
    {
        if (deployableNodes.Count > 0)
        {
            GameNode node = deployableNodes[0];
            SetGridCursorAt(node);
        }
        ActivateMoveCursorAndHide(false, true);
    }

    public void EnableEditingMode(bool active)
    {
        if (active) 
            enableEditing = true;
        else
            enableEditing = false;
    }

    public void CreateTempoTeam()
    {
        MapTeamManager.instance.GenerateTeam(selectedCharacters, TeamType.Player, true);
    }

    public void EndDeployment()
    {
        selectedCharacters.Clear();
        deployableNodes.Clear();
        occupiedNodes.Clear();
        lastNode = null;
        lasSelectedCharacter = null;
        EnableEditingMode(false);
        DestroyPreviewModel();
        ActivateMoveCursorAndHide(false, true);
        GameEvent.onDeploymentEnd?.Invoke();
    }

}
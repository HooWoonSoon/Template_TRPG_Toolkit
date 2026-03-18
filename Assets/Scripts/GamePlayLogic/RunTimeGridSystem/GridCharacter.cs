using UnityEngine;

public class GridCharacter : Entity
{
    public CharacterPools characterPools;
    public static GridCharacter instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    protected override void Start()
    {
        base.Start();
        GameEvent.onMapSwitchedTrigger += InitializeGridCharacter;
        InitializeGridCharacter();
    }

    public void InitializeGridCharacter()
    {
        ResetAllGridCharacter();
        foreach (CharacterBase character in characterPools.allCharacter)
        {
            UpdateGridCharacter(character);
        }
    }

    private void UpdateGridCharacter(CharacterBase character)
    {
        if (character == null || !character.gameObject.activeSelf)
            return;

        Vector3Int position = Utils.RoundXZFloorYInt(character.transform.position);
        SetGridCharacter(position, character);
    }
    public void SetGridCharacter(Vector3Int characterPos, CharacterBase character)
    {
        if (character == null || !character.gameObject.activeSelf)
            return;

        GameNode gameNode = world.GetNode(characterPos);
        if (gameNode == null) 
        { 
            //Debug.Log("Invalid node update"); 
            return; 
        }

        if (gameNode != null)
        {
            gameNode.SetUnitGridCharacter(character);
        }
    }
    public void SetGridCharacter(GameNode characterNode, CharacterBase character)
    {
        if (character == null || !character.gameObject.activeSelf)
            return;

        if (characterNode == null)
        {
            Debug.Log("Invalid node update");
            return;
        }

        if (characterNode != null)
        {
            characterNode.SetUnitGridCharacter(character);
        }
    }
    private void ResetAllGridCharacter()
    {
        foreach (GameNode node in world.loadedNodes.Values)
        {
            if (node != null)
            {
                node.SetUnitGridCharacter(null);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (world == null) return;

        foreach (GameNode node in world.loadedNodes.Values)
        {
            if (node.character != null)
            {
                Gizmos.color = new Color(0, 0, 0, 0.5f);
                Gizmos.DrawCube(node.GetNodeVector() + Vector3.up, Vector3.one);
            }
        }
    }
}
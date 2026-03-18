using System.Collections.Generic;
using UnityEngine;

public enum TeamType
{
    Player, Opposite, Allay, Neutral
}

public class TeamDeployment : MonoBehaviour
{
    public List<CharacterBase> teamCharacter = new List<CharacterBase>();
    public List<InventoryData> inventoryDatas = new List<InventoryData>();
    public TeamType teamType;

    private void Start()
    {
        // Initialize team characters
        for (int i = 0; i < teamCharacter.Count; i++)
        {
            if (teamCharacter[i] != null)
            {
                teamCharacter[i].currentTeam = this;
            }
        }
    }

    public List<Type> GetAllOfType<Type>() where Type : CharacterBase
    {
        List<Type> result = new List<Type>();
        foreach (var type in teamCharacter)
        {
            if (type is Type tCharacter)
            {
                result.Add(tCharacter);
            }
        }
        return result;
    }

    public Type GetCharacterData<Type>() where Type : CharacterBase
    {
        for (int i = 0; i < teamCharacter.Count; i++)
        {
            if (teamCharacter[i] is Type tCharacter)
            {
                return tCharacter;
            }
        }
        Debug.Log("Character with index "  + " not found in team.");
        return null;
    }
}
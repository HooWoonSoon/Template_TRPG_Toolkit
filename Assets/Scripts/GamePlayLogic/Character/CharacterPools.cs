using System.Collections.Generic;
using UnityEngine;

public class CharacterPools : MonoBehaviour
{
    public List<CharacterBase> allCharacter;

    public void Start()
    {
        GameEvent.onMapSwitchedTrigger += HideAllCharacter;
    }

    public void AddCharacter(CharacterBase character)
    {
        allCharacter.RemoveAll(empty => empty == null);

        if (!allCharacter.Contains(character))
            allCharacter.Add(character);
    }

    private void HideAllCharacter()
    {
        for (int i = 0; i < allCharacter.Count; i++)
        {
            if (allCharacter[i] != null)
                allCharacter[i].gameObject.SetActive(false);
        }
        Debug.Log("All character hidden from the map after deployment.");
    }
}
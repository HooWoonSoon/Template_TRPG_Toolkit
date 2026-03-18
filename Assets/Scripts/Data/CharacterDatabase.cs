using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Tactics/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> allCharacterDatas;

    public void AddCharacterData(CharacterData characterData)
    {
        allCharacterDatas.RemoveAll(data => data == null);
        if (!allCharacterDatas.Contains(characterData))
            allCharacterDatas.Add(characterData);
    }

    public void RemoveData(CharacterData characterData)
    {
        allCharacterDatas.Remove(characterData);
    }
}

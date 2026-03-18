using System;
using System.Collections.Generic;

[Serializable]
public class CTTurn
{
    public CharacterBase character;
    public int turnCount;
    public bool isExecuted;

    public CTTurn(CharacterBase character, int turnCount)
    {
        this.character = character;
        this.turnCount = turnCount;
        isExecuted = false;
    }
}

[Serializable]
public class CTRound
{
    public List<CTTurn> cTTurnQueue = new List<CTTurn>();
    public int roundCount;

    public CTRound(List<CTTurn> cTTurnQueue, int roundCount)
    {
        this.cTTurnQueue = cTTurnQueue;
        this.roundCount = roundCount;
    }

    public CharacterBase GetCharacterAt(int index)
    {
        if (index < 0 || index >= cTTurnQueue.Count) return null;
        return cTTurnQueue[index].character;
    }

    public List<CharacterBase> GetCharacterQueue() 
    { 
        List<CharacterBase> characterQueue = new List<CharacterBase>();
        for (int i = 0; i < cTTurnQueue.Count; i++)
        {
            characterQueue.Add(GetCharacterAt(i));
        }
        return characterQueue; 
    }
}
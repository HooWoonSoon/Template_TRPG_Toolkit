using System;
using System.Collections.Generic;
using UnityEngine;

public class CTTimeline : MonoBehaviour
{
    [Serializable]
    public class CharacterTacticsTime
    {
        public CharacterBase character;
        public int increaseValue;
        public int CTValue = 0;
        public int accumulatedTime = 0;
        public bool isQueue;

        private const int BASE_INCREASE = 2;

        public CharacterTacticsTime(CharacterBase character)
        {
            this.character = character;
            increaseValue = character.data.speed + BASE_INCREASE;
            CTValue = 0;
            accumulatedTime = 0;
            isQueue = false;
        }
        public void IncreaseCT()
        {
            CTValue += increaseValue;
        }
        public void CompleteCT()
        {
            accumulatedTime++;
            increaseValue /= (accumulatedTime + 1);
            CTValue -= 100;
            isQueue = true;
        }
        public void Reset()
        {
            increaseValue = character.data.speed + BASE_INCREASE;
            CTValue = 0;
            accumulatedTime = 0;
            isQueue = false;
        }
    }

    public Dictionary<CharacterBase, CharacterTacticsTime> battleCharacter;
    public List<CharacterBase> leaveBattleCharacter;

    private List<CTRound> cTRounds;
    private const int INITIAL_ROUND = 4;

    private CTRound currentCTRound;
    private int currentRoundIndex = 0;

    private CTTurn currentCTTurn;
    private int currentTurnIndex = 0;

    private CharacterBase currentCharacter;

    [SerializeField] private UITransitionToolkit uITransitionToolkit;

    public static CTTimeline instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public void SetJoinedBattleUnit(List<CharacterBase> characters)
    {
        battleCharacter = new Dictionary<CharacterBase, CharacterTacticsTime>();
        for (int i = 0; i < characters.Count; i++)
        {
            InsertCharacter(characters[i]);
        }
    }
    public void InsertCharacter(CharacterBase character)
    {
        if (!battleCharacter.ContainsKey(character))
        {
            CharacterTacticsTime tactics = new CharacterTacticsTime(character);
            battleCharacter.Add(character, tactics);
        }
    }
    public void SetupTimeline()
    {
        if (battleCharacter.Count == 0) { return; }

        HashSet<CharacterBase> characters = new HashSet<CharacterBase>(battleCharacter.Keys);
        cTRounds = new List<CTRound>();

        for (int i = 0; i < INITIAL_ROUND; i++)
        {
            List<CTTurn> completeQueue = GetCalculateCTTurn();
            CTRound turnHistory = new CTRound(completeQueue, i);
            
            cTRounds.Add(turnHistory);

            //  Reset all battle character last turn accumulated value
            foreach (var tactics in battleCharacter.Values)
            {
                tactics.Reset();
            }
        }
        currentCTRound = cTRounds[0];
        currentRoundIndex = 0;

        currentCTTurn = cTRounds[0].cTTurnQueue[0];
        currentTurnIndex = 0;

        currentCharacter = currentCTTurn.character;
        CTTurnUIManager.instance.GenerateTimelineUI();;
    }

    private void ExtentTimeline()
    {
        List<CTTurn> completeQueue = GetCalculateCTTurn();
        CTRound turnHistory = new CTRound(completeQueue, cTRounds.Count);
        cTRounds.Add(turnHistory);

        foreach (var tactics in battleCharacter.Values)
        {
            tactics.Reset();
        }
    }

    public void AdjustTimelineStartRound(int roundIndex)
    {
        CTRound cTRound = cTRounds[roundIndex];
        List<CTTurn> turns = cTRound.cTTurnQueue;
        foreach (var character in leaveBattleCharacter)
        { 
            for (int i = turns.Count - 1; i >= 0; i--)
            {
                if (turns[i].isExecuted) continue;
                if (turns[i].character != character) continue;
                turns.RemoveAt(i);
            }
        }

        int nextRoundIndex = roundIndex + 1;
        int maxAdjustRounds = cTRounds.Count;
        int removeCount = maxAdjustRounds - nextRoundIndex;
        cTRounds.RemoveRange(nextRoundIndex, removeCount);

        for (int i = nextRoundIndex; i < maxAdjustRounds; i++)
        {
            List<CTTurn> completeQueue = GetCalculateCTTurn();
            CTRound turnHistory = new CTRound(completeQueue, cTRounds.Count);
            cTRounds.Add(turnHistory);

            foreach (var tactics in battleCharacter.Values)
            {
                tactics.Reset();
            }
        }
    }
    public void RemoveCharacter(CharacterBase character)
    {
        if (battleCharacter.ContainsKey(character))
        {
            battleCharacter.Remove(character);
            leaveBattleCharacter.Add(character);
            AdjustTimelineStartRound(currentRoundIndex);
            CTTurnUIManager.instance.AdjustTurnUIStartRound(currentRoundIndex);
        }
    }

    public void EndTimeline()
    {
        battleCharacter = null;
        CTTurnUIManager.instance.ResetAllTurnUI();
    }

    private bool IsAllCharacterQueue(List<CharacterTacticsTime> tacticsList)
    {
        foreach (CharacterTacticsTime tactics in tacticsList)
        {
            if (!tactics.isQueue) { return false; }
        }
        return true;
    }
    private List<CTTurn> GetCalculateCTTurn()
    {
        List<CTTurn> cTTurnQueue = new List<CTTurn>();
        List<CharacterTacticsTime> tacticsList = new List<CharacterTacticsTime>(battleCharacter.Values);

        int turnCount = 0;

        while (!IsAllCharacterQueue(tacticsList))
        {
            foreach (CharacterTacticsTime tactics in tacticsList)
            {
                tactics.IncreaseCT();
                if (tactics.CTValue >= 100)
                {
                    CTTurn cTTurn = new CTTurn(tactics.character, turnCount);
                    cTTurnQueue.Add(cTTurn);
                    tactics.CompleteCT();
                    turnCount++;
                }
            }
        }
        return cTTurnQueue;
    }
    public void NextCharacterTurn()
    {
        if (currentCTRound == null) { return; }
        NextNumber();
        currentCharacter = currentCTRound.cTTurnQueue[currentTurnIndex].character;
        CTTurnUIManager.instance.TargetCurrentCTTurnUI(currentCTRound, currentCTTurn);
    }
    private void NextNumber()
    {
        currentCTTurn.isExecuted = true;
        CTTurnUIManager.instance.RecordPastTurnUI(currentCTRound, currentCTTurn);

        if (currentTurnIndex < currentCTRound.cTTurnQueue.Count - 1)
        {
            currentTurnIndex++;
            currentCTTurn = currentCTRound.cTTurnQueue[currentTurnIndex];
        }
        else
        {
            if (currentRoundIndex < cTRounds.Count - 1)
            {
                currentRoundIndex++;
                currentCTRound = cTRounds[currentRoundIndex];

                currentTurnIndex = 0;
                currentCTTurn = currentCTRound.cTTurnQueue[currentTurnIndex];

                ExtentTimeline();
            }
        }
        //Debug.Log($"currentRound: {currentRoundIndex}, currentTurnIndex: {currentTurnIndex}");
        CTTurnUIManager.instance.AppendTurnUI();
    }
    private void TargetCharacterUIUpdate()
    {
        uITransitionToolkit.ResetUIFormToTargetPos(1);
    }

    #region Search
    public int GetCharacterCurrentQueue(CharacterBase character)
    {
        if (cTRounds.Count == 0) 
        {
            Debug.LogWarning("No Round implemented");
            return -1; 
        }

        int index = 0;
        for (int r = currentRoundIndex; r < cTRounds.Count; r++)
        {
            CTRound searchRound = cTRounds[r];
            List<CTTurn> cTTurnQueues = searchRound.cTTurnQueue;

            int startTurn = (r == currentRoundIndex) ? currentTurnIndex : 0;

            for (int t = startTurn; t < cTTurnQueues.Count; t++)
            {
                if (character == cTTurnQueues[t].character)
                    return index;
                
                index++;
            }
        }
        return -1;
    }
    public List<CTRound> GetAllRound() => cTRounds;
    public CTRound GetCurrentCTRound() => currentCTRound;
    public CTTurn GetCurrentCTTurn() => currentCTTurn;
    public int GetCurrentCTTurnIndex() => currentTurnIndex;
    public int GetCurrentCTRoundIndex() => currentRoundIndex;
    public CharacterBase GetCurrentCharacter() => currentCharacter;
    public Dictionary<CharacterBase, CharacterTacticsTime> GetBattleCharacter() => battleCharacter;
    #endregion
}


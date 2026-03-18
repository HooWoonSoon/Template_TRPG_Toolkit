using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UtilityAIDatabase", menuName = "Tactics/Utility AI Database")]
public class UtilityAIDatabase : ScriptableObject
{
    public List<UtilityAIScoreConfig> dataSet = new List<UtilityAIScoreConfig>();

    public void AddData(UtilityAIScoreConfig utilityAI)
    {
        dataSet.RemoveAll(data => data == null);
        if (!dataSet.Contains(utilityAI))
            dataSet.Add(utilityAI);
    }

    public void RemoveData(UtilityAIScoreConfig utilityAI)
    {
        dataSet.Remove(utilityAI);
        dataSet.RemoveAll(data => data == null);
    }
}

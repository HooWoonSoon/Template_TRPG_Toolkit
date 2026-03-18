using System.Collections.Generic;
using Tactics.AI;
using UnityEngine;

public class DefenseBackRule : ScoreRuleBase
{
    public DefenseBackRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules, 
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }

    protected override bool DebugMode => DebugManager.IsDebugEnabled(context);

    public override float CalculateOrientationScore(CharacterBase character, 
        List<CharacterBase> opposites, GameNode originNode, Orientation orientation)
    {
        float score = 0;

        List<Vector3Int> neighbourPosList = decisionSystem.pathFinding.GetNeighbourPosCustomized(originNode.GetNodeVectorInt(), 1);
        string neighbourNodesMsg = string.Join(", ", neighbourPosList);
        Vector3Int orientationDir = character.GetOrientationDirection(orientation);
        World world = decisionSystem.world;

        foreach (Vector3Int neighbourPos in neighbourPosList)
        {
            GameNode neighbourNode = world.GetNode(neighbourPos);

            if (neighbourNode == null || neighbourNode != null && !neighbourNode.isWalkable)
            {
                Vector3 originPos = originNode.GetNodeVector();

                Vector3 direction = (originPos - neighbourPos).normalized;
                Vector3Int nodeOrientationDir = character.GetDirConvertOrientationDir(direction);
                if (orientationDir == nodeOrientationDir)
                {
                    score += 1;
                }
            }
        }

        //  Tempo logic
        foreach (CharacterBase opposite in opposites)
        {
            Vector3 selfPos = character.transform.position;
            Vector3 oppositesPos = opposite.transform.position;

            float distance = Vector3.Distance(oppositesPos, selfPos);
            Vector3 direction = (oppositesPos - selfPos).normalized;
            Vector3Int nodeOrientationDir = character.GetDirConvertOrientationDir(direction);
            if (orientationDir == nodeOrientationDir)
            {
                score += 0.5f / Mathf.Max(distance, 1f); ;
            }
        }


        if (DebugMode)
            Debug.Log(
                $"<color=purple>[DefenseBackRule]</color> " +
                $"{character.data.characterName}, " +
                $"Origin Node: {originNode.GetNodeVectorInt()}, " +
                $"Neighbour Nodes: {neighbourNodesMsg}, " +
                $"to orientation {orientation}, " +
                $"plus Score bonus: {score}");

        return score;
    }
}

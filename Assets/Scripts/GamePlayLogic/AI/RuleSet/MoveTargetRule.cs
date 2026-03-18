using System.Collections.Generic;
using Tactics.AI;
using UnityEngine;
public class MoveTargetRule : ScoreRuleBase
{
    public MoveTargetRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules, 
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }

    protected override bool DebugMode => DebugManager.IsDebugEnabled(context);

    public override float CalculateMoveToTargetScore(CharacterBase character, float frontLineIndex,
        CharacterBase targetCharacter, List<GameNode> targetAroundNodes, GameNode moveNode, 
        List<CharacterBase> teammates, List<CharacterBase> opposites, 
        DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes)
    {
        PathFinding pathFinding = decisionSystem.pathFinding;
        CharacterData data = character.data;
        //  No Join the Rule
        if (data == null) return 0;
        if (targetAroundNodes == null || targetAroundNodes.Count == 0) return 0;
        if (moveNode == null) return 0;

        if (targetAroundNodes == null || targetAroundNodes.Count == 0)
            return 0;

        int bestCost = int.MaxValue;
        GameNode bestTargetNode = null;

        foreach (var targetNode in targetAroundNodes)
        {
            int cost = pathFinding.GetNodesBetweenCost(moveNode, 
                targetNode, character, 1, 1);

            if (cost < bestCost)
            {
                bestCost = cost;
                bestTargetNode = targetNode;
            }
        }

        if (bestCost == int.MaxValue)
        {
            if (DebugMode)
                Debug.Log("No valid path to any target node");
            return -100; // Dead Path
        }

        float costFactor = Mathf.Sqrt(bestCost);
        float distanceFactor = 1f / (1f + costFactor);
        float score = Mathf.Lerp(0f, scoreBonus, distanceFactor);

        Dictionary<SkillData, List<GameNode>> oppositeSkillInflunce;

        foreach (var opposite in opposites)
        {
            oppositeSkillInflunce = characterSkillInfluenceNodes.oppositeInfluence[opposite];
            if (oppositeSkillInflunce != null)
            {
                foreach (SkillData skill in oppositeSkillInflunce.Keys)
                {
                    if (skill.skillType != SkillType.Acttack) continue;
                    if (oppositeSkillInflunce[skill].Contains(moveNode))
                    {
                        int damage = skill.damageAmount;
                        if (damage >= character.currentHealth)
                        {
                            if (frontLineIndex > 0)
                                score -= scoreBonus / 12;
                            else
                                score -= scoreBonus / 8;
                        }
                    }
                }
            }
        }

        foreach (var subRule in scoreSubRules)
            score += subRule.CalculateTargetScore(character, targetCharacter, teammates, opposites);

        if (DebugMode)
            Debug.Log(
                $"<color=black>[MoveTargetRule]</color> " +
                $"{character.data.characterName}, " +
                $"StartNode: {character.currentNode.GetNodeVectorInt()}, " +
                $"MoveNode: {moveNode.GetNodeVectorInt()}, " +
                $"Route actual cost: {bestCost}, " +
                $"TargetNode: {bestTargetNode.GetNodeVectorInt()}, " +
                $"get Score bonus: {score}");
        return score;
    }
}

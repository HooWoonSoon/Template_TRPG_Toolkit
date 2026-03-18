using System.Collections.Generic;
using Tactics.AI;
using UnityEngine;
public class FatalHitRule : ScoreRuleBase
{
    public FatalHitRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules, 
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }

    protected override bool DebugMode => DebugManager.IsDebugEnabled(context);

    public override float CalculateSkillScore(CharacterBase character, SkillData skill, 
        GameNode targetNode, int maxHealthAmongOpposites)
    {
        //  No Join the Rule
        if (skill == null) return 0;

        CharacterBase target = targetNode.GetUnitGridCharacter();
        if (target == null) return 0;

        if (DebugMode)
        {
            Debug.Log("Execute Fatal Hit Rule");
            if (target != null)
                Debug.Log($"{character.data.characterName} consider to use " +
                    $"{skill.skillName} on {target.data.characterName}");
        }

        if (skill.damageAmount >= target.currentHealth)
        {
            if (DebugMode)
                Debug.Log($"Skill: {skill.skillName} is fatal skill, " +
                    $"plus Score Bonus : {scoreBonus}");
            return scoreBonus;
        }
        return 0;
    }
}

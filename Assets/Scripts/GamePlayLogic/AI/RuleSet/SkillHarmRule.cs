using System.Collections.Generic;
using Tactics.AI;
using UnityEngine;
public class SkillHarmRule : ScoreRuleBase
{
    public SkillHarmRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules, 
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }

    protected override bool DebugMode => DebugManager.IsDebugEnabled(context);

    public override float CalculateSkillScore(CharacterBase character, SkillData skill, 
        GameNode targetNode, int highestHealthAmongCharacters)
    {
        if (skill == null) 
            return 0;
        if (skill.skillType != SkillType.Acttack)
            return 0;
        if (skill.MPAmount > character.currentMental) return 0;

        CharacterBase target = targetNode.GetUnitGridCharacter();
        if (target == null) return 0;

        int damage = skill.damageAmount;
        int targetReleaseHealth = target.currentHealth;

        // Damage overflow
        int actualDamage = Mathf.Min(damage, targetReleaseHealth);

        if (actualDamage <= 0)
        {
            if (DebugMode)
                Debug.Log($"Skill: {skill.skillName} damage skill," +
                    $" cannot deal damage to {target.data.characterName}, " +
                    $" Get Score bonus: Min Value");
            return 0;
        }

        float healthFactor = 1f - ((float)target.currentHealth / highestHealthAmongCharacters);
        float priorityHealthFactor = Mathf.Max(utilityAI.minHealthPriority, healthFactor);

        float damageFactor = (float)actualDamage / target.data.health;
        float priorityDamageFactor = Mathf.Max(utilityAI.minHarmPriority, damageFactor);

        float mpCost = skill.MPAmount;
        float maxMp = Mathf.Max(1f, character.data.mental);
        float mpFactor = 1f - Mathf.Lerp(utilityAI.mpMinReductionRatio, 
            utilityAI.mpMaxReductionRatio, mpCost / maxMp);

        float t = priorityDamageFactor * mpFactor + priorityHealthFactor * utilityAI.priorityHealthFactor;

        float score = Mathf.Lerp(0f, scoreBonus, t);

        if (scoreSubRules != null && scoreSubRules.Count > 0)
        {
            foreach (var subRule in scoreSubRules)
                score += subRule.CalculateSkillScore(character, skill, targetNode, highestHealthAmongCharacters);
        }

        if (score > scoreBonus)
            score = scoreBonus;

        if (DebugMode)
            Debug.Log(
                $"<color=red>[HarmRule]</color> " +
                $"{character.data.characterName}, " +
                $"<b>{skill.skillName}</b> damage skill, " +
                $"deal damage to {target.data.characterName}" +
                $"at Target Node {targetNode.GetNodeVectorInt()}, " + 
                $"plus Score bonus: {score}");

        return score;
    }
}

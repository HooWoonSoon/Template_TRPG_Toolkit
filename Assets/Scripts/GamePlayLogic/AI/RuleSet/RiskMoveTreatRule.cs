using System.Collections.Generic;
using Tactics.AI;
using UnityEngine;

public class RiskMoveTreatRule : ScoreRuleBase
{
    public RiskMoveTreatRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules,
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }

    public override float CalculateRiskMoveSkillScore(CharacterBase character, SkillData skill, 
        DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes, 
        GameNode moveNode, GameNode targetNode, int highestHealthAmongCharacters)
    {
        if (skill == null) return 0;
        if (skill.skillType != SkillType.Heal) return 0;
        if (skill.MPAmount > character.currentMental) return 0;

        CharacterBase target = targetNode.GetUnitGridCharacter();
        if (target == null) return 0;

        float oppositeSkillScore = GetOppositeHarmSkillScore(character,
            characterSkillInfluenceNodes, moveNode, highestHealthAmongCharacters);

        float skillScore = GetHealSkillScore(character, skill, target);

        // Extra Score from Risk would not highly impact the
        // final score and inside the range of scoreBonus
        float priorityRiskScore = oppositeSkillScore * 0.02f;
        float score = skillScore + priorityRiskScore;

        if (DebugMode)
            Debug.Log(
                $"<color=#00BFFF>[MoveTreatRule]</color> " +
                $"{character.data.characterName}, " +
                $"<b>{skill.skillName}</b> heal skill," +
                $"at Move Node {moveNode.GetNodeVectorInt()} " +
                $"deal heal to {target.data.characterName}," +
                $"at Target Node {targetNode.GetNodeVectorInt()} " +
                $"plus Score bonus: {score}");

        return score;
    }
    private float GetHealSkillScore(CharacterBase character, SkillData skill, CharacterBase target)
    {
        int missingHealth = target.data.health - target.currentHealth;
        if (missingHealth <= 0) return 0;

        int actualHeal = Mathf.Min(skill.healAmount, missingHealth);
        if (actualHeal == 0) return 0;

        float healFactor = (float)actualHeal / skill.healAmount;
        float missingFactor = (float)missingHealth / target.data.health;
        float priorityFactor = Mathf.Max(utilityAI.minHealPriority, missingFactor * healFactor);

        float mpCost = skill.MPAmount;
        float maxMp = Mathf.Max(1f, character.data.mental);
        float mpFactor = 1f - Mathf.Lerp(utilityAI.mpMinReductionRatio, utilityAI.mpMaxReductionRatio, mpCost / maxMp);

        float t = priorityFactor * mpFactor;

        float score = Mathf.Lerp(0f, scoreBonus, t);
        return score;
    }
    private float GetOppositeHarmSkillScore(CharacterBase character,
        DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes,
        GameNode moveNode, int highestHealthAmongCharacters)
    {
        float score = 0;
        foreach (var enemy in characterSkillInfluenceNodes.oppositeInfluence.Keys)
        {
            float skillBestScore = 0;

            foreach (var enemySkill in characterSkillInfluenceNodes.oppositeInfluence[enemy].Keys)
            {
                float skillScore = 0;

                var nodeList = characterSkillInfluenceNodes.oppositeInfluence[enemy][enemySkill];

                GameNode currentNode = character.currentNode;
                currentNode.SetUnitGridCharacter(null);
                moveNode.SetUnitGridCharacter(character);

                if (!nodeList.Contains(moveNode))
                {
                    currentNode.SetUnitGridCharacter(character);
                    moveNode.SetUnitGridCharacter(null);
                    continue;
                }

                if (DebugMode)
                    Debug.Log(
                        $"{enemy.data.characterName}, " +
                        $"Skill: {enemySkill}, " +
                        $"Attackable: {moveNode.GetNodeVectorInt()}");

                skillScore += GetHarmSkillScore(enemy, enemySkill, highestHealthAmongCharacters, character);

                currentNode.SetUnitGridCharacter(character);
                moveNode.SetUnitGridCharacter(null);

                if (skillScore > skillBestScore)
                {
                    skillBestScore = skillScore;
                }
            }
            score -= skillBestScore;
        }
        return score;
    }
    private float GetHarmSkillScore(CharacterBase character, SkillData skill,
        int highestHealthAmongCharacters, CharacterBase target)
    {
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
        return score;
    }
}

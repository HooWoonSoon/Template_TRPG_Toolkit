using UnityEngine;
using System.Collections.Generic;
using Tactics.AI;
public class RiskMoveHarmRule : ScoreRuleBase
{
    public RiskMoveHarmRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules, 
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }

    protected override bool DebugMode => DebugManager.IsDebugEnabled(context);

    public override float CalculateRiskMoveSkillScore(CharacterBase character, SkillData skill,
        DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes,
        GameNode moveNode, GameNode targetNode, int highestHealthAmongCharacters)
    {
        if (skill == null)
            return 0;
        if (skill.skillType != SkillType.Acttack)
            return 0;
        if (skill.MPAmount > character.currentMental) return 0;

        CharacterBase target = targetNode.GetUnitGridCharacter();
        if (target == null) return 0;

        float oppositeSkillScore = GetOppositeHarmSkillScore(character, 
            characterSkillInfluenceNodes, moveNode, highestHealthAmongCharacters);

        float skillScore = GetHarmSkillScore(character, skill, highestHealthAmongCharacters, target);

        float score = 0;
        float subScore = 0;

        if (scoreSubRules != null && scoreSubRules.Count > 0)
        {
            foreach (var subRule in scoreSubRules)
                subScore += subRule.CalculateSkillScore(character, skill, targetNode, highestHealthAmongCharacters);
        }
        score += subScore;

        // Extra Score from Risk would not highly impact the
        // final score and inside the range of scoreBonus
        float priorityRiskScore = oppositeSkillScore * 0.02f;
        score += skillScore + priorityRiskScore;

        if (DebugMode)
            Debug.Log(
                $"<color=red>[RiskMoveHarmRule]</color> " +
                $"{character.data.characterName}, " +
                $"MoveNode: {moveNode.GetNodeVectorInt()}, " +
                $"<b>{skill.skillName}</b> damage skill, " +
                $"deal damage to {target.data.characterName}, " +
                $"at Target Node {targetNode.GetNodeVectorInt()}," +
                $"SubScore: {subScore} " +
                $"Skill score: {skillScore}, " +
                $"Priority risk score: {priorityRiskScore}, " +
                $"plus Score bonus: {score}");

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

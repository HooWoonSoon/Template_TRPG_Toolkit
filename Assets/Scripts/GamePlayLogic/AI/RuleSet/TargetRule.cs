using System.Collections.Generic;
using Tactics.AI;
using UnityEngine;

public class TargetRule : ScoreRuleBase
{
    public TargetRule(DecisionSystem decisionSystem, UtilityAIScoreConfig utilityAI, List<IScoreRule> scoreSubRules, 
        int scoreBonus, RuleDebugContext context) : base(decisionSystem, utilityAI, scoreSubRules, scoreBonus, context)
    {
    }
    protected override bool DebugMode => DebugManager.IsDebugEnabled(context);

    public override float CalculateTargetScore(CharacterBase selfCharacter, 
        CharacterBase targetCharacter, List<CharacterBase> teammates, List<CharacterBase> opposites)
    {
        float score = 0;

        if (targetCharacter.currentTeam == selfCharacter.currentTeam)
        {
            return -10;
        }
        
        if (targetCharacter.unitState == UnitState.Knockout
            || targetCharacter.unitState == UnitState.Dead)
        {
            return -100;
        }
        
        float t = CalculateOppositeTargetValueFactor(targetCharacter, opposites);
        score = Mathf.Lerp(0, scoreBonus, t);

        if (DebugMode)
            Debug.Log(
                $"<color=yellow>[TargetRule]</color> " +
                $"{selfCharacter.data.characterName}, " +
                $"Target Character: {targetCharacter.data.characterName}, " +
                $"plus Score bonus: {score}");

        return score;
    }

    private float CalculateOppositeTargetValueFactor(CharacterBase targetCharacter, List<CharacterBase> opposites)
    {
        float healthWeight = 1f;
        float movementWeight = 1.2f;
        float speedWeight = 0.8f;

        int oppositeHighestHealth = GetHighestHealth(opposites);
        int currentHealth = targetCharacter.currentHealth;

        float priorityHealthFactor = (1 - (float)currentHealth / oppositeHighestHealth) * healthWeight ;

        int oppositeHighestMovementValue = GetHighestMovementValue(opposites);
        //  low movement value more easy to pursue
        int movementValue = targetCharacter.data.movementValue;
        float priorityMovementFactor = (1 - (float)movementValue / oppositeHighestMovementValue) * movementWeight;

        int oppositeHighestSpeed = GetHighestSpeed(opposites);
        int speed = targetCharacter.data.speed;

        float prioritySpeedFactor = (float)speed / oppositeHighestSpeed * speedWeight;

        float totalWeight = healthWeight + movementWeight + speedWeight;
        float totalPriorityFactor = (priorityHealthFactor + priorityMovementFactor + prioritySpeedFactor) / totalWeight;
        return totalPriorityFactor;
    }

    private int GetHighestHealth(List<CharacterBase> characters)
    {
        int highestHealth = int.MinValue;
        foreach (CharacterBase character in characters)
        {
            int currentHealth = character.currentHealth;
            if (currentHealth > highestHealth)
            {
                highestHealth = currentHealth;
            }
        }
        return highestHealth;
    }
    private int GetHighestSpeed(List<CharacterBase> characters)
    {
        int highestSpeed = int.MinValue;
        foreach (CharacterBase character in characters)
        {
            int speed = character.data.speed;
            if (speed > highestSpeed)
            {
                highestSpeed = speed;
            }
        }
        return highestSpeed;
    }
    private int GetHighestMovementValue(List<CharacterBase> characters)
    {
        int highestMovementValue = int.MinValue;
        foreach (CharacterBase character in characters)
        {
            int movementValue = character.data.movementValue;
            if (movementValue > highestMovementValue)
            {
                highestMovementValue = movementValue;
            }
        }
        return highestMovementValue;
    }
}

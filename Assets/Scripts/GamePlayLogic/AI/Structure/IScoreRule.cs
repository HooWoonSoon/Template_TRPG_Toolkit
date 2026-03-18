using System.Collections.Generic;
using Tactics.AI;

public interface IScoreRule
{
    float CalculateTargetScore(CharacterBase selfCharacter, 
        CharacterBase targetCharacter, List<CharacterBase> teammates, 
        List<CharacterBase> opposites);
    float CalculateMoveToTargetScore(CharacterBase character, float frontLineIndex, 
        CharacterBase targetCharacter, List<GameNode> targetAroundNodes, 
        GameNode moveNode, List<CharacterBase> teammates, List<CharacterBase> opposites, 
        DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes);
    float CalculateRiskMoveScore(CharacterBase character,
        DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes,
        GameNode moveNode);
    float CalculateSkillScore(CharacterBase character, SkillData skill,
        GameNode targetNode, int highestHealthAmongCharacters);
    float CalculateMoveSkillScore(CharacterBase character, SkillData skill,
        GameNode moveNode, GameNode targetNode, int highestHealthAmongCharacters);
    float CalculateRiskMoveSkillScore(CharacterBase character, SkillData skill,
    DecisionSystem.CharacterSkillInfluenceNodes characterSkillInfluenceNodes,
    GameNode moveNode, GameNode targetNode, int highestHealthAmongCharacters);
    float CalculateOrientationScore(CharacterBase character, List<CharacterBase> opposites,
        GameNode originNode, Orientation orientation);
}

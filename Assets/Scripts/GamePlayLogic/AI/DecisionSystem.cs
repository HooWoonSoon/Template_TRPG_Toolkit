using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Tactics.AI
{
    public class DecisionSystem
    {
        public enum Decision
        {
            OriginCastSkill,
            Move,
            MoveAndCastSkill
        }
        public World world;
        public PathFinding pathFinding;
        public UtilityAIScoreConfig utilityAI;
        public CharacterBase decisionMaker;

        private List<IScoreRule> targetRules = new List<IScoreRule>();
        private List<IScoreRule> skillRules = new List<IScoreRule>();
        private List<IScoreRule> moveSkillRules = new List<IScoreRule>();
        private List<IScoreRule> moveToTargetRules = new List<IScoreRule>();
        private List<IScoreRule> moveRules = new List<IScoreRule>();
        private List<IScoreRule> orientationRule = new List<IScoreRule>();

        private SkillData skill;
        private GameNode moveNode;
        private GameNode skillTargetNode;
        private Orientation orientation;

        private Dictionary<(GameNode, GameNode), int> pathCostCache
        = new Dictionary<(GameNode, GameNode), int>();

        private bool debugMode = false;

        public DecisionSystem(World world, UtilityAIScoreConfig utilityAI, CharacterBase decisionMaker,
            bool debugMode = false)
        {
            this.world = world;
            this.decisionMaker = decisionMaker;
            this.utilityAI = utilityAI;
            this.debugMode = debugMode;

            PathFinding pathfinding = new PathFinding(world);
            this.pathFinding = pathfinding;

            //  Move Rule
            List<IScoreRule> moveTargetSubRules = new List<IScoreRule>()
            {
                new TargetRule(this, this.utilityAI, null, utilityAI.targeRuleScore,
                RuleDebugContext.MoveTarget)
            };
            moveToTargetRules.Add(new MoveTargetRule(this, this.utilityAI, moveTargetSubRules,
                utilityAI.moveTargetRuleScore, RuleDebugContext.MoveTarget));

            List<IScoreRule> moveSubRules = new List<IScoreRule>()
            {
                new SkillHarmRule(this, this.utilityAI, null, utilityAI.harmRuleScore, RuleDebugContext.RiskMove),
                new SkillTreatRule(this, this.utilityAI, null, utilityAI.treatRuleScore, RuleDebugContext.RiskMove)
            };
            moveRules.Add(new RiskMoveRule(this, this.utilityAI, moveSubRules, utilityAI.riskMoveRuleScore,
                RuleDebugContext.RiskMove));

            List<IScoreRule> harmSubRules = new List<IScoreRule>()
            {
                new FatalHitRule(this, this.utilityAI, null, utilityAI.fatalHitRuleScore,
                RuleDebugContext.Origin_Harm)
            };
            skillRules.Add(new SkillHarmRule(this, this.utilityAI, harmSubRules, utilityAI.originHarmRuleScore,
                RuleDebugContext.Origin_Harm));
            List<IScoreRule> treatSubRules = new List<IScoreRule>()
            {
            };
            skillRules.Add(new SkillTreatRule(this, this.utilityAI, treatSubRules, utilityAI.originTreatRuleScore,
                RuleDebugContext.Origin_Treat));

            //  Move Skill Rule
            List<IScoreRule> moveHarmSubRules = new List<IScoreRule>()
            {
                new FatalHitRule(this, this.utilityAI, null, utilityAI.riskFatalHitRuleScore,
                RuleDebugContext.RiskMove_Harm)
            };
            moveSkillRules.Add(new RiskMoveHarmRule(this, this.utilityAI, moveHarmSubRules,
                utilityAI.riskMoveHarmRuleScore, RuleDebugContext.RiskMove_Harm));

            List<IScoreRule> moveTreatSubRules = new List<IScoreRule>()
            {
            };
            moveSkillRules.Add(new RiskMoveTreatRule(this, this.utilityAI, moveTreatSubRules,
                utilityAI.riskMoveTreatRuleScore, RuleDebugContext.RiskMove_Treat));

            orientationRule.Add(new DefenseBackRule(this, this.utilityAI, null,
                utilityAI.defenseBackRuleScore, RuleDebugContext.DefenseBack));
        }


        public struct SkillEvaluationResult
        {
            public float skillScore;
            public SkillData originSkill;
            public GameNode skillCastNode;
            public GameNode originSkillTargetNode;
            public string sourceSkill;
        }
        public struct MoveAndSkillEvaluateResult
        {
            public float skillScore;
            public SkillData moveSkill;
            public GameNode moveSkillMoveNode;
            public GameNode moveSkillTargetNode;
            public string sourceMoveAndSkill;
        }
        public struct MoveEvaluateResult
        {
            public float moveScore;
            public GameNode moveOnlyNode;
            public string sourceMove;
        }

        public IEnumerator MakeDecisionCorroutine(MonoBehaviour mono, bool allowMove = true, bool allowSkill = true)
        {
            float startTime = Time.realtimeSinceStartup;

            SkillEvaluationResult skillResult = default;
            yield return mono.StartCoroutine(SkillEvaluateCoroutine(allowSkill, r => skillResult = r));
            pathCostCache.Clear();  //Release historical path cache
            MoveAndSkillEvaluateResult moveAndSkillResult = default;
            yield return mono.StartCoroutine(MoveAndSkillEvaluateCoroutine(mono, allowMove, allowSkill, 
                r => moveAndSkillResult = r));
            pathCostCache.Clear();  //Release historical path cache
            MoveEvaluateResult moveResult = default;
            yield return mono.StartCoroutine(MoveEvaluateCoroutine(allowMove, allowSkill, r => moveResult = r));
            pathCostCache.Clear();  //Release historical path cache

            float ORIGIN_SKILL_BONUS = utilityAI.ORIGIN_SKILL_BONUS;
            float MOVE_SKILL_BONUS = utilityAI.MOVE_SKILL_BONUS;
            float MOVE_ONLY_BONUS = utilityAI.MOVE_ONLY_BONUS;

            float finalOriginSkillScore = skillResult.skillScore + ORIGIN_SKILL_BONUS;
            float finalMoveSkillScore = moveAndSkillResult.skillScore + MOVE_SKILL_BONUS;
            float finalMoveScore = moveResult.moveScore + MOVE_ONLY_BONUS;

            float bestFinalScore = finalOriginSkillScore;
            Decision decision = Decision.OriginCastSkill;

            if (finalMoveSkillScore > bestFinalScore)
            {
                bestFinalScore = finalMoveSkillScore;
                decision = Decision.MoveAndCastSkill;
            }
            if (finalMoveScore > bestFinalScore)
            {
                bestFinalScore = finalMoveScore;
                decision = Decision.Move;
            }

            string executeLog = "";
            if (decision == Decision.OriginCastSkill)
            {
                skill = skillResult.originSkill;
                moveNode = skillResult.skillCastNode;
                skillTargetNode = skillResult.originSkillTargetNode;

                if (skillResult.originSkill != null && skillResult.originSkillTargetNode != null)
                {
                    executeLog =
                    $"Decision: Origin Cast Skill, " +
                    $"Execute Option: {skillResult.sourceSkill}, " +
                    $"Skill: {skill.skillName} at {skillTargetNode.GetNodeVectorInt()}";
                }
            }
            else if (decision == Decision.MoveAndCastSkill)
            {
                skill = moveAndSkillResult.moveSkill;
                moveNode = moveAndSkillResult.moveSkillMoveNode;
                skillTargetNode = moveAndSkillResult.moveSkillTargetNode;

                executeLog =
                    $"Decision: Move And Cast Skill, " +
                    $"Execute Option: {moveAndSkillResult.sourceMoveAndSkill}, " +
                    $"Move: {moveNode.GetNodeVectorInt()}, " +
                    $"Skill: {skill.skillName} at {skillTargetNode.GetNodeVectorInt()}";
            }
            else if (decision == Decision.Move)
            {
                skill = null;
                moveNode = moveResult.moveOnlyNode;
                skillTargetNode = null;

                if (moveNode != null)
                {
                    executeLog =
                        $"Decision: Move, " +
                        $"Execute Option: {moveResult.sourceMove}, " +
                        $"Move: {moveNode.GetNodeVectorInt()}";
                }
            }

            Orientation orientation = decisionMaker.selfOrientation;
            if (moveNode == null)
                yield return mono.StartCoroutine(OrientationEvaluateCoroutine(decisionMaker.currentNode, o => orientation = o));
            else
                yield return mono.StartCoroutine(OrientationEvaluateCoroutine(moveNode, o => orientation = o));
            this.orientation = orientation;

            pathCostCache.Clear();  //Release historical path cache

            float endTime = Time.realtimeSinceStartup;

            if (debugMode)
                Debug.Log($"<color=green>[Make Decision]</color> " +
                    $"{decisionMaker.data.characterName}, " +
                    $"{executeLog}, " +
                    $" completed in {endTime - startTime:F4} seconds");
        }

        private IEnumerator SkillEvaluateCoroutine(bool allowSkill, Action<SkillEvaluationResult> onComplete)
        {
            SkillEvaluate(allowSkill, out float skillBestScore,
            out SkillData originSkill, out GameNode skillCastNode,
            out GameNode originSkillTargetNode,
            out string sourceSkill);

            yield return null;

            SkillEvaluationResult result = new SkillEvaluationResult
            {
                skillScore = skillBestScore,
                originSkill = originSkill,
                skillCastNode = skillCastNode,
                originSkillTargetNode = originSkillTargetNode,
                sourceSkill = sourceSkill
            };

            onComplete?.Invoke(result);
        }
        private IEnumerator MoveAndSkillEvaluateCoroutine(MonoBehaviour mono, bool allowMove, bool allowSkill, 
            Action<MoveAndSkillEvaluateResult> onComplete)
        {
            MoveAndSkillEvaluateResult moveAndSkillResult = default;
            yield return mono.StartCoroutine(EvaluateMoveAndSkillCoroutine(r => moveAndSkillResult = r));

            onComplete?.Invoke(moveAndSkillResult);
        }
        private IEnumerator MoveEvaluateCoroutine(bool allowMove, bool allowSkill, Action<MoveEvaluateResult> onComplete)
        {
            MoveEvaluate(allowMove, allowSkill, out float moveBestScore,
                out GameNode moveOnlyNode, out string sourceMove);
            yield return null;

            MoveEvaluateResult result = new MoveEvaluateResult()
            {
                moveScore = moveBestScore,
                moveOnlyNode = moveOnlyNode,
                sourceMove = sourceMove
            };
            onComplete?.Invoke(result);
        }
        private IEnumerator OrientationEvaluateCoroutine(GameNode node, Action<Orientation> onComplete)
        {
            OrientationEvaluate(node, out orientation);
            yield return null;
            onComplete?.Invoke(orientation);
        }

        public void MakeDecision(bool allowMove = true, bool allowSkill = true)
        {
            float startTime = Time.realtimeSinceStartup;

            SkillEvaluate(allowSkill, out float skillBestScore,
                out SkillData originSkill, out GameNode skillCastNode,
                out GameNode originSkillTargetNode,
                out string sourceSkill);

            MoveAndSkillEvaluate(allowMove, allowSkill, out float moveAndSkillBestScore,
                out SkillData moveSkill, out GameNode moveSkillMoveNode,
                out GameNode moveSkillTargetNode, out string sourceMoveAndSkill);

            MoveEvaluate(allowMove, allowSkill, out float moveBestScore,
                out GameNode moveOnlyNode, out string sourceMove);

            float ORIGIN_SKILL_BONUS = utilityAI.ORIGIN_SKILL_BONUS;
            float MOVE_SKILL_BONUS = utilityAI.MOVE_SKILL_BONUS;
            float MOVE_ONLY_BONUS = utilityAI.MOVE_ONLY_BONUS;

            float finalOriginSkillScore = skillBestScore + ORIGIN_SKILL_BONUS;
            float finalMoveSkillScore = moveAndSkillBestScore + MOVE_SKILL_BONUS;
            float finalMoveScore = moveBestScore + MOVE_ONLY_BONUS;

            float bestFinalScore = finalOriginSkillScore;
            Decision decision = Decision.OriginCastSkill;

            if (finalMoveSkillScore > bestFinalScore)
            {
                bestFinalScore = finalMoveSkillScore;
                decision = Decision.MoveAndCastSkill;
            }
            if (finalMoveScore > bestFinalScore)
            {
                bestFinalScore = finalMoveScore;
                decision = Decision.Move;
            }

            string executeLog = "";
            if (decision == Decision.OriginCastSkill)
            {
                skill = originSkill;
                moveNode = skillCastNode;
                skillTargetNode = originSkillTargetNode;

                if (originSkill != null && originSkillTargetNode != null)
                {
                    executeLog =
                    $"Decision: Origin Cast Skill, " +
                    $"Execute Option: {sourceSkill}, " +
                    $"Skill: {skill.skillName} at {skillTargetNode.GetNodeVectorInt()}";
                }
            }
            else if (decision == Decision.MoveAndCastSkill)
            {
                skill = moveSkill;
                moveNode = moveSkillMoveNode;
                skillTargetNode = moveSkillTargetNode;

                executeLog =
                    $"Decision: Move And Cast Skill, " +
                    $"Execute Option: {sourceMoveAndSkill}, " +
                    $"Move: {moveNode.GetNodeVectorInt()}, " +
                    $"Skill: {skill.skillName} at {skillTargetNode.GetNodeVectorInt()}";
            }
            else if (decision == Decision.Move)
            {
                skill = null;
                moveNode = moveOnlyNode;
                skillTargetNode = null;

                if (moveNode != null)
                {
                    executeLog =
                        $"Decision: Move, " +
                        $"Execute Option: {sourceMove}, " +
                        $"Move: {moveNode.GetNodeVectorInt()}";
                }
            }

            Orientation orientation = decisionMaker.selfOrientation;
            if (moveNode == null)
                OrientationEvaluate(decisionMaker.currentNode, out orientation);
            else
                OrientationEvaluate(moveNode, out orientation);
            this.orientation = orientation;

            float endTime = Time.realtimeSinceStartup;

            if (debugMode)
                Debug.Log($"<color=green>[Make Decision]</color> " +
                    $"{decisionMaker.data.characterName}, " +
                    $"{executeLog}, " +
                    $" completed in {endTime - startTime:F4} seconds");
        }

        public int GetCachedPathCost(GameNode start, GameNode target, CharacterBase pathfinder,
            int riseLimit, int lowerLimit)
        {
            var key = (start, target);
            if (pathCostCache.TryGetValue(key, out int cacheCost))
                return cacheCost;

            int cost = pathFinding.GetTargetNodeCost(start, target, pathfinder, riseLimit, lowerLimit);
            pathCostCache[key] = cost;
            return cost;
        }

        #region Evaluation Methods
        private void SkillEvaluate(bool allowSkill, out float skillBestScore,
            out SkillData originSkill, out GameNode skillCastNode,
            out GameNode originSkillTargetNode,
            out string source)
        {
            skillBestScore = float.MinValue;
            originSkill = null;
            skillCastNode = null;
            originSkillTargetNode = null;
            source = null;

            if (allowSkill)
            {
                EvaluateOriginSkillOption(
                    ref skillBestScore,
                    ref originSkill,
                    ref skillCastNode,
                    ref originSkillTargetNode);
                source = "Evaluate Origin Skill Option";
            }
        }
        private void MoveAndSkillEvaluate(bool allowMove, bool allowSkill,
            out float moveAndSkillBestScore, out SkillData moveSkill,
            out GameNode moveSkillMoveNode, out GameNode moveSkillTargetNode,
            out string source)
        {
            moveAndSkillBestScore = float.MinValue;
            moveSkill = null;
            moveSkillMoveNode = decisionMaker.currentNode;
            moveSkillTargetNode = null;
            source = null;

            if (allowMove && allowSkill)
            {
                EvaluateMoveAndSkillOption(
                    ref moveAndSkillBestScore,
                    ref moveSkill,
                    ref moveSkillMoveNode,
                    ref moveSkillTargetNode);
                source = "Evaluate Move And Skill Option";
            }
        }
        private void MoveEvaluate(bool allowMove, bool allowSkill,
            out float moveBestScore, out GameNode moveOnlyNode,
            out string soure)
        {
            moveBestScore = float.MinValue;
            moveOnlyNode = decisionMaker.currentNode;
            soure = null;
            orientation = decisionMaker.selfOrientation;

            if (allowMove && allowSkill)
            {
                EvaluateMoveTargetOption(
                    ref moveBestScore,
                    ref moveOnlyNode);
                soure = "Evaluate Move Target Option";
            }
            else if (allowMove && !allowSkill)
            {
                EvaluateMoveOption(
                    ref moveBestScore,
                    ref moveOnlyNode);
                soure = "Evaluate Move Option";
            }
        }
        private void OrientationEvaluate(GameNode node, out Orientation bestOrientation)
        {
            bestOrientation = decisionMaker.selfOrientation;
            float bestOrientationScore = int.MinValue;

            Orientation[] orientations =
            {
                Orientation.right,
                Orientation.left,
                Orientation.back,
                Orientation.forward
            };

            List<CharacterBase> mapCharacters = GetMapCharacters();
            List<CharacterBase> opposites = GetOppositeCharacter(decisionMaker, mapCharacters);

            foreach (var orientation in orientations)
            {
                float score = 0;
                foreach (var rule in orientationRule)
                {
                    score += rule.CalculateOrientationScore(decisionMaker, opposites, node, orientation);
                }

                if (score > bestOrientationScore)
                {
                    bestOrientation = orientation;
                    bestOrientationScore = score;
                }
            }
        }
        #endregion

        #region Evaluate Option Methods
        private void EvaluateOriginSkillOption(ref float bestScore,
            ref SkillData bestSkill, ref GameNode skillCastNode, ref GameNode bestSkillTargetNode)
        {
            float startTime = Time.realtimeSinceStartup;

            GameNode originNode = decisionMaker.currentNode;

            bestSkill = null;
            bestSkillTargetNode = null;
            skillCastNode = originNode;

            List<CharacterBase> mapCharacters = GetMapCharacters();
            if (mapCharacters == null || mapCharacters.Count == 0) return;

            int maxHealthAmongOpposite = GetCharactersHighestHealth(mapCharacters);

            foreach (SkillData skill in decisionMaker.skillDatas)
            {
                if (!skill.isTargetTypeSkill) continue;

                var skilltargetNodes = decisionMaker.GetSkillRangeFromNode(skill, originNode);
                foreach (var skillTargetNode in skilltargetNodes)
                {
                    float totalScore = 0;

                    if (!IsProjectileAchievableSingle(skill, originNode, skillTargetNode))
                        continue;

                    if (!IsValidSkillTargetNodeSingle(skill, skillTargetNode))
                        continue;

                    foreach (var rule in skillRules)
                    {
                        totalScore += rule.CalculateSkillScore(decisionMaker, skill,
                            skillTargetNode, maxHealthAmongOpposite);
                    }

                    if (totalScore > bestScore)
                    {
                        bestScore = totalScore;
                        bestSkill = skill;
                        bestSkillTargetNode = skillTargetNode;
                    }
                }
            }

            float endTime = Time.realtimeSinceStartup;

            if (debugMode)
                Debug.Log($"<color=green>[Evaluate Origin Skill Option]</color> " +
                    $" completed in {endTime - startTime:F4} seconds, " +
                    $"score: {bestScore}");
        }

        private IEnumerator EvaluateMoveAndSkillCoroutine(Action<MoveAndSkillEvaluateResult> onComplete)
        {
            float bestScore = 0;
            SkillData bestSkill = null;
            GameNode bestMoveNode = null;
            GameNode bestSkillTargetNode = null;

            List<CharacterBase> mapCharacters = GetMapCharacters();
            if (mapCharacters == null || mapCharacters.Count == 0) yield break;

            int highestHealthAmongCharacters = GetCharactersHighestHealth(mapCharacters);
            CharacterSkillInfluenceNodes characterSkillInfluenceNodes =
                new CharacterSkillInfluenceNodes(this,
                GetOppositeCharacter(decisionMaker, decisionMaker.GetMapCharacterExceptSelf()),
                GetSameTeamCharacter(decisionMaker, decisionMaker.GetMapCharacterExceptSelf()));

            List<SkillData> skills = decisionMaker.skillDatas;
            if (skills == null || skills.Count == 0) yield break;

            float frameStartTime;

            foreach (SkillData skill in skills)
            {
                if (!skill.isTargetTypeSkill) continue;

                List<GameNode> skillTargetableMovableNode = GetSkillTargetableMovableNode(decisionMaker, skill);

                if (debugMode)
                {
                    string log = string.Join(",",
                        skillTargetableMovableNode.ConvertAll(s => s.GetNodeVector().ToString()));
                    Debug.Log(
                        $"Character: {decisionMaker.data.characterName}, " +
                        $"Origin Node: {decisionMaker.currentNode.GetNodeVector()}, " +
                        $"Skill: {skill.skillName}, " +
                        $"Skill Targetable Movable Node: {log}");
                }

                foreach (var moveNode in skillTargetableMovableNode)
                {
                    var skilltargetNodes = decisionMaker.GetSkillRangeFromNode(skill, moveNode);
                    foreach (var skillTargetNode in skilltargetNodes)
                    {
                        frameStartTime = Time.realtimeSinceStartup;

                        float totalScore = 0;

                        if (!IsProjectileAchievableSingle(skill, moveNode, skillTargetNode))
                            continue;

                        if (!IsValidSkillTargetNodeSingle(skill, skillTargetNode))
                            continue;

                        foreach (var rule in moveSkillRules)
                        {
                            totalScore += rule.CalculateRiskMoveSkillScore(decisionMaker, skill,
                                characterSkillInfluenceNodes, moveNode, skillTargetNode,
                                highestHealthAmongCharacters);
                        }

                        if (totalScore > bestScore)
                        {
                            bestScore = totalScore;
                            bestSkill = skill;
                            bestMoveNode = moveNode;
                            bestSkillTargetNode = skillTargetNode;
                        }

                        if (Time.realtimeSinceStartup - frameStartTime > 0.002f)
                        {
                            yield return null;
                        }
                    }
                }
            }

            onComplete?.Invoke(new MoveAndSkillEvaluateResult
            {
                skillScore = bestScore,
                moveSkill = bestSkill,
                moveSkillMoveNode = bestMoveNode,
                moveSkillTargetNode = bestSkillTargetNode
            });
        }
        private void EvaluateMoveAndSkillOption(ref float bestScore,
            ref SkillData bestSkill, ref GameNode bestMoveNode,
            ref GameNode bestSkillTargetNode)
        {
            float startTime = Time.realtimeSinceStartup;

            bestSkill = null;
            bestMoveNode = null;
            bestSkillTargetNode = null;

            List<CharacterBase> mapCharacters = GetMapCharacters();
            if (mapCharacters == null || mapCharacters.Count == 0) return;

            int highestHealthAmongCharacters = GetCharactersHighestHealth(mapCharacters);
            CharacterSkillInfluenceNodes characterSkillInfluenceNodes =
                new CharacterSkillInfluenceNodes(this,
                GetOppositeCharacter(decisionMaker, decisionMaker.GetMapCharacterExceptSelf()),
                GetSameTeamCharacter(decisionMaker, decisionMaker.GetMapCharacterExceptSelf()));

            List<SkillData> skills = decisionMaker.skillDatas;
            if (skills == null || skills.Count == 0) return;

            foreach (SkillData skill in skills)
            {
                if (!skill.isTargetTypeSkill) continue;

                List<GameNode> skillTargetableMovableNode = GetSkillTargetableMovableNode(decisionMaker, skill);

                if (debugMode)
                {
                    string log = string.Join(",",
                        skillTargetableMovableNode.ConvertAll(s => s.GetNodeVector().ToString()));
                    Debug.Log(
                        $"Character: {decisionMaker.data.characterName}, " +
                        $"Origin Node: {decisionMaker.currentNode.GetNodeVector()}, " +
                        $"Skill: {skill.skillName}, " +
                        $"Skill Targetable Movable Node: {log}");
                }

                foreach (var moveNode in skillTargetableMovableNode)
                {
                    var skilltargetNodes = decisionMaker.GetSkillRangeFromNode(skill, moveNode);
                    foreach (var skillTargetNode in skilltargetNodes)
                    {
                        float totalScore = 0;

                        if (!IsProjectileAchievableSingle(skill, moveNode, skillTargetNode))
                            continue;

                        if (!IsValidSkillTargetNodeSingle(skill, skillTargetNode))
                            continue;

                        foreach (var rule in moveSkillRules)
                        {
                            totalScore += rule.CalculateRiskMoveSkillScore(decisionMaker, skill,
                                characterSkillInfluenceNodes, moveNode, skillTargetNode,
                                highestHealthAmongCharacters);
                        }

                        if (totalScore > bestScore)
                        {
                            bestScore = totalScore;
                            bestSkill = skill;
                            bestMoveNode = moveNode;
                            bestSkillTargetNode = skillTargetNode;
                        }
                    }
                }
            }
            float endTime = Time.realtimeSinceStartup;

            if (debugMode)
                Debug.Log($"<color=green>[Evaluate Move And Skill Option]</color> " +
                    $" completed in {endTime - startTime:F4} seconds, " +
                    $"score: {bestScore}");
        }
        private void EvaluateMoveTargetOption(ref float bestScore,
            ref GameNode bestNode)
        {
            float startTime = Time.realtimeSinceStartup;

            List<CharacterBase> mapCharactersExceptSelf = decisionMaker.GetMapCharacterExceptSelf();
            if (mapCharactersExceptSelf == null || mapCharactersExceptSelf.Count == 0) return;

            List<CharacterBase> opposites = GetOppositeCharacter(decisionMaker, mapCharactersExceptSelf);
            List<CharacterBase> teammates = GetSameTeamCharacter(decisionMaker, mapCharactersExceptSelf);
            float frontLineIndex = CalculateFrontPosIndex(decisionMaker, opposites);

            CharacterSkillInfluenceNodes characterSkillInfluenceNodes =
                new CharacterSkillInfluenceNodes(this, opposites, teammates);

            List<GameNode> movableNodes = decisionMaker.GetMovableNodes();
            foreach (CharacterBase targetCharacter in mapCharactersExceptSelf)
            {
                List<GameNode> targetAroundNodes = GetTargetAroundNodes(targetCharacter);

                if (targetAroundNodes == null || targetAroundNodes.Count == 0) continue;

                foreach (GameNode targetNode in targetAroundNodes)
                {
                    List<GameNode> reachableLowCostNodes = pathFinding.GetLowestCostNodes(targetNode, movableNodes);

                    foreach (GameNode moveNode in reachableLowCostNodes)
                    {
                        float totalScore = 0;

                        foreach (var rule in moveToTargetRules)
                            totalScore += rule.CalculateMoveToTargetScore(decisionMaker, frontLineIndex,
                                targetCharacter, targetAroundNodes, moveNode, teammates,
                                opposites, characterSkillInfluenceNodes);

                        if (totalScore > bestScore)
                        {
                            bestScore = totalScore;
                            bestNode = moveNode;
                        }
                    }
                }
            }

            float endTime = Time.realtimeSinceStartup;

            if (debugMode)
                Debug.Log($"<color=green>[Evaluate Move Target Option]</color> " +
                    $" completed in {endTime - startTime:F4} seconds, " +
                    $"score: {bestScore}");
        }
        private void EvaluateMoveOption(ref float bestScore,
            ref GameNode bestMoveNode)
        {
            float startTime = Time.realtimeSinceStartup;
            Debug.Log("Execute Evaluate Move Option");
            List<GameNode> movableNodes = decisionMaker.GetMovableNodes();

            List<CharacterBase> mapCharactersExceptSelf =
                decisionMaker.GetMapCharacterExceptSelf();
            if (mapCharactersExceptSelf == null || mapCharactersExceptSelf.Count == 0) return;

            List<CharacterBase> opposites =
                GetOppositeCharacter(decisionMaker, mapCharactersExceptSelf);
            List<CharacterBase> teammates =
                GetSameTeamCharacter(decisionMaker, mapCharactersExceptSelf);

            CharacterSkillInfluenceNodes characterSkillInfluenceNodes =
                new CharacterSkillInfluenceNodes(this, opposites, teammates);

            float bestRiskMoveScore = 0;
            GameNode bestRiskMoveNode = null;

            foreach (var moveNode in movableNodes)
            {
                float riskMoveScore = 0;

                foreach (var rule in moveRules)
                {
                    riskMoveScore += rule.CalculateRiskMoveScore(decisionMaker,
                        characterSkillInfluenceNodes, moveNode);
                }
                if (riskMoveScore > bestScore)
                {
                    bestRiskMoveScore = riskMoveScore;
                    bestRiskMoveNode = moveNode;
                }
            }

            int highestBlocked = 0;
            GameNode bestBlockNode = null;
            foreach (var opposite in opposites)
            {
                int oppositeMovableNodeCount = opposite.GetMovableNodes().Count;
                foreach (var moveNode in movableNodes)
                {
                    int blockCount = CalculateBlockScore(decisionMaker, opposite, oppositeMovableNodeCount, moveNode);
                    //Debug.Log($"Block Count: {blockCount} in {moveNode.GetNodeVectorInt()}");

                    if (blockCount > highestBlocked)
                    {
                        highestBlocked = blockCount;
                        bestBlockNode = moveNode;
                    }
                }
            }

            float selfFrontPosIndex = CalculateFrontPosIndex(decisionMaker, opposites);

            if (selfFrontPosIndex > 0 && bestBlockNode != null)
            {
                bestScore = 25;
                bestMoveNode = bestBlockNode;
            }

            if (selfFrontPosIndex < 0)
            {
                bestScore = bestRiskMoveScore;
                bestMoveNode = bestRiskMoveNode;
            }

            float endTime = Time.realtimeSinceStartup;

            if (debugMode)
                Debug.Log($"<color=green>[Evaluate Move Option]</color> " +
                    $" completed in {endTime - startTime:F4} seconds, " +
                    $"score: {bestScore}");
        }
        #endregion
        public int CalculateBlockScore(CharacterBase character, CharacterBase opposite,
            int oppositeMovableNodesCount, GameNode moveNode)
        {
            GameNode currentNode = character.currentNode;
            currentNode.SetUnitGridCharacter(null);
            moveNode.SetUnitGridCharacter(character);

            int currentOppositeMovableCount = opposite.GetMovableNodes().Count;

            currentNode.SetUnitGridCharacter(character);
            moveNode.SetUnitGridCharacter(null);

            return oppositeMovableNodesCount - currentOppositeMovableCount;
        }

        #region Front Score
        public CharacterBase GetMostHighFrontScoreCharacter(List<CharacterBase> characters,
            List<CharacterBase> opposites)
        {
            float bestScore = float.MinValue;
            CharacterBase bestCharacter = null;
            foreach (var character in characters)
            {
                float score = CalculateFrontPosIndex(character, opposites);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCharacter = character;
                }
            }
            return bestCharacter;
        }

        /// <summary>
        /// Calculate the index of character are able to stand at the front line. 
        /// The index is based on the character's current health, damage absorption capacity, 
        /// and ranged combat capability.
        /// Sample position like tank = high, worrior = medium high and ranger = tiny or negative.
        /// </summary>
        /// <param name="character">The character required to evaluate</param>
        /// <param name="opposites">List of characters in the opposite team</param>
        /// <returns></returns>
        public float CalculateFrontPosIndex(CharacterBase character,
            List<CharacterBase> opposites)
        {
            float score = 0f;

            int maxHealth = character.data.health;
            int currentHealth = character.currentHealth;
            float healthFactor = (float)currentHealth / maxHealth;
            float healthModifier = Mathf.Lerp(0.5f, 1, healthFactor);

            //  At least One Range
            float rangedAbility = CalculateRangedAbility(character);
            int abosoluteRangerIndex = 10;
            float tR = rangedAbility / abosoluteRangerIndex;
            float rangedModifier = Mathf.Lerp(0, 8, tR);

            //  At least One-time
            int takeDamageAbility = CalculateSurvivAbility(character, opposites);
            float absoluteSurvivalIndex = 8;
            float tS = takeDamageAbility / absoluteSurvivalIndex;
            float takeDamageModifier = Mathf.Lerp(0, 10, tS);

            score = healthModifier * takeDamageAbility - rangedModifier;

            if (debugMode)
                Debug.Log(
                        $"<color=purple>[FrontPosIndex]</color> " +
                        $"{character.data.characterName}, " +
                        $"plus Score bonus: {score}");

            return score;
        }

        private float CalculateRangedAbility(CharacterBase character)
        {
            float rangeScore = 0;
            List<SkillData> skillData = character.skillDatas;
            int availableSkillCount = 0;
            foreach (var skill in skillData)
            {
                int mpCost = skill.MPAmount;
                float currentMetal = character.currentMental;
                if (mpCost > currentMetal) continue;
                availableSkillCount++;

                int range = skill.skillRange;
                int occulsiveRange = skill.occlusionRange;

                rangeScore += (range + occulsiveRange);
            }

            if (availableSkillCount == 0) return 1;
            if (rangeScore == 0) return 1;

            float skillScore = rangeScore / availableSkillCount;
            return skillScore;
        }
        /// <summary>
        /// Calculate the average damage of opposites team character available skills,
        /// then estimates the number of attacks the current character can withstand 
        /// before exhausting their health based on this average damage.
        /// </summary>
        /// <param name="character">The character required to evaluate</param>
        /// <param name="opposites">List of characters in the opposite team</param>
        /// <returns></returns>
        private int CalculateSurvivAbility(CharacterBase character,
            List<CharacterBase> opposites)
        {
            float combinedDamage = 0;
            foreach (CharacterBase opposite in opposites)
            {
                List<SkillData> skills = opposite.GetAvaliableSkills();

                var damageSkills = skills.Where(s => s.damageAmount > 0).ToList();

                if (damageSkills.Count > 0)
                {
                    float totalDamage = 0;
                    foreach (SkillData skill in damageSkills)
                    {
                        totalDamage += skill.damageAmount;
                    }
                    float averageDamage = totalDamage / damageSkills.Count;
                    combinedDamage += averageDamage;
                }
            }

            float combinedAverangeDamage = combinedDamage / opposites.Count;

            if (combinedAverangeDamage <= 0)
                return 999;

            int takeDamageTime = 0;
            float startHealth = character.data.health;
            while (startHealth > 0)
            {
                startHealth -= combinedAverangeDamage;
                takeDamageTime++;
            }

            return takeDamageTime;
        }
        #endregion

        public class CharacterSkillInfluenceNodes
        {
            public Dictionary<CharacterBase, Dictionary<SkillData, HashSet<GameNode>>> oppositeInfluence;
            public Dictionary<CharacterBase, Dictionary<SkillData, HashSet<GameNode>>> teammateInfluence;

            public CharacterSkillInfluenceNodes(DecisionSystem decisionSystem, List<CharacterBase> opposites,
                List<CharacterBase> teammates, bool debugMode = false)
            {
                oppositeInfluence = new Dictionary<CharacterBase, Dictionary<SkillData, HashSet<GameNode>>>();
                teammateInfluence = new Dictionary<CharacterBase, Dictionary<SkillData, HashSet<GameNode>>>();

                foreach (var opposite in opposites)
                {
                    Dictionary<SkillData, HashSet<GameNode>> skillCanAttackSet =
                    decisionSystem.GetOppositeSkillInfluence(opposite);

                    if (debugMode)
                    {
                        foreach (var kvp in skillCanAttackSet)
                        {
                            string skillName = kvp.Key.skillName;
                            string nodeLog = string.Join(", ", kvp.Value.Select(n => n.GetNodeVectorInt()));

                            Debug.Log(
                                $"Opposite: {opposite.data.characterName}, " +
                                $"Skill: {skillName}, " +
                                $"Influence Nodes: {nodeLog}");
                        }
                    }
                    oppositeInfluence[opposite] = skillCanAttackSet;
                }

                foreach (var teammate in teammates)
                {
                    Dictionary<SkillData, HashSet<GameNode>> skillCanSupportSet =
                    decisionSystem.GetTeammateSkillInfluence(teammate);

                    if (debugMode)
                    {
                        foreach (var kvp in skillCanSupportSet)
                        {
                            string skillName = kvp.Key.skillName;
                            string nodeLog = string.Join(", ", kvp.Value.Select(n => n.GetNodeVectorInt()));

                            Debug.Log(
                                $"Teammate: {teammate.data.characterName}, " +
                                $"Skill: {skillName}, " +
                                $"Influence Nodes: {nodeLog}");
                        }
                    }
                    teammateInfluence[teammate] = skillCanSupportSet;
                }
            }
        }
        private Dictionary<SkillData, HashSet<GameNode>> GetOppositeSkillInfluence
            (CharacterBase opposite)
        {
            Dictionary<SkillData, HashSet<GameNode>> skillCanAttackSet = new Dictionary<SkillData, HashSet<GameNode>>();

            int currentMental = opposite.currentMental;
            List<GameNode> oppositeMovableNodes = opposite.GetMovableNodes();

            foreach (var skill in opposite.skillDatas)
            {
                if (skill.MPAmount > currentMental) continue;
                if (skill.skillTargetType != SkillTargetType.Opposite) continue;

                HashSet<GameNode> canAttackNodes = new HashSet<GameNode>();

                foreach (var fromNode in oppositeMovableNodes)
                {
                    List<GameNode> skillScope = opposite.GetSkillRangeFromNode(skill, fromNode);

                    foreach (var scopeNode in skillScope)
                    {
                        canAttackNodes.Add(scopeNode);
                    }
                }
                skillCanAttackSet[skill] = canAttackNodes;
            }
            return skillCanAttackSet;
        }
        private Dictionary<SkillData, HashSet<GameNode>> GetTeammateSkillInfluence
            (CharacterBase teammate)
        {
            Dictionary<SkillData, HashSet<GameNode>> skillCanSupportSet = new Dictionary<SkillData, HashSet<GameNode>>();

            int currentMental = teammate.currentMental;
            List<GameNode> teammateMovableNodes = teammate.GetMovableNodes();

            foreach (var skill in teammate.skillDatas)
            {
                if (currentMental > skill.MPAmount) continue;
                if (skill.skillTargetType == SkillTargetType.Opposite) continue;

                HashSet<GameNode> canAttackNodes = new HashSet<GameNode>();

                foreach (var fromNode in teammateMovableNodes)
                {
                    List<GameNode> skillScope = teammate.GetSkillRangeFromNode(skill, fromNode);

                    foreach (var scopeNode in skillScope)
                    {
                        canAttackNodes.Add(scopeNode);
                    }
                }
                skillCanSupportSet[skill] = canAttackNodes;
            }
            return skillCanSupportSet;
        }

        private int GetCharactersHighestHealth(List<CharacterBase> characters)
        {
            int highestHealth = 0;
            foreach (var character in characters)
            {
                int health = character.data.health;
                if (health > highestHealth)
                {
                    highestHealth = health;
                }
            }
            return highestHealth;
        }

        public List<CharacterBase> GetMapCharacters()
        {
            List<CharacterBase> characters = new List<CharacterBase>();
            foreach (GameNode node in world.loadedNodes.Values)
            {
                CharacterBase nodeCharacter = node.GetUnitGridCharacter();
                if (nodeCharacter != null)
                    characters.Add(nodeCharacter);
            }
            return characters;
        }
        public List<CharacterBase> GetOppositeCharacter(CharacterBase character,
        List<CharacterBase> characterList)
        {
            List<CharacterBase> oppositeCharacter = new List<CharacterBase>();
            foreach (var otherCharacter in characterList)
            {
                if (otherCharacter.currentTeam != character.currentTeam)
                {
                    oppositeCharacter.Add(otherCharacter);
                }
            }
            return oppositeCharacter;
        }
        public List<CharacterBase> GetSameTeamCharacter(CharacterBase character,
        List<CharacterBase> characterList)
        {
            List<CharacterBase> sameTeamCharacter = new List<CharacterBase>();
            foreach (var otherCharacter in characterList)
            {
                if (otherCharacter.currentTeam == character.currentTeam)
                {
                    sameTeamCharacter.Add(otherCharacter);
                }
            }
            return sameTeamCharacter;
        }

        private List<GameNode> GetTargetAroundNodes(CharacterBase targetCharacter)
        {
            List<GameNode> primaryTargetAroundNodes = new List<GameNode>();

            int iteration = 1;
            int maxIteration = 10;

            while ((primaryTargetAroundNodes == null || primaryTargetAroundNodes.Count == 0) && iteration <= maxIteration)
            {
                primaryTargetAroundNodes = targetCharacter.GetCustomizedSizeMovableNodes(iteration, 0);
                if (debugMode)
                    Debug.Log($"Iteration {iteration}, nodes count: {primaryTargetAroundNodes.Count}");
                iteration++;
            }

            if (primaryTargetAroundNodes.Count != 0) return primaryTargetAroundNodes;
            return null;
        }

        #region Skill Target Validation
        private bool IsValidSkillTargetNodeSingle(SkillData skill, GameNode targetNode)
        {
            TeamType selfTeam = decisionMaker.data.type;
            CharacterBase targetNodeCharacter = targetNode.GetUnitGridCharacter();

            if (targetNodeCharacter == null) return false;

            UnitState unitState = targetNodeCharacter.unitState;
            if (unitState == UnitState.Knockout || unitState == UnitState.Dead)
            {
                if (debugMode)
                    Debug.Log("Skill target character is knockout or dead");
                return false;
            }

            switch (skill.skillTargetType)
            {
                case SkillTargetType.Opposite:
                    if (targetNodeCharacter.data.type != selfTeam)
                        return true;
                    break;

                case SkillTargetType.Our:
                    if (targetNodeCharacter.data.type == selfTeam)
                        return true;
                    break;

                case SkillTargetType.Self:
                    if (targetNodeCharacter == decisionMaker)
                        return true;
                    break;

                case SkillTargetType.Both:
                    return true;
            }
            return false;
        }
        private bool IsProjectileAchievableSingle(SkillData skill, GameNode startNode,
            GameNode targetNode)
        {
            if (skill.isProjectile)
            {
                Parabola parabola = new Parabola(world);
                if (targetNode.GetUnitGridCharacter() != null)
                {
                    UnitDetectable projectileDetect = skill.projectTilePrefab.GetComponent<UnitDetectable>();
                    List<UnitDetectable> ignoreUnits = new List<UnitDetectable>()
                    {
                        decisionMaker.GetComponent<UnitDetectable>()
                    };

                    CharacterBase targetCharacter = targetNode.GetUnitGridCharacter();
                    Vector3 target = targetNode.GetNodeVector();
                    if (targetCharacter != null)
                        target = targetCharacter.transform.position + new Vector3(0, 1.5f, 0);
                    
                    UnitDetectable unit = parabola.GetParabolaHitUnit
                        (projectileDetect, startNode.GetNodeVector() + 
                        new Vector3(0, decisionMaker.shootOffsetHeight, 0),
                        target, skill.initialElevationAngle, ignoreUnits);

                    Vector3 start = startNode.GetNodeVector() +
                        new Vector3(0, decisionMaker.shootOffsetHeight, 0);
                    if (unit == null) return false;

                    //Debug.Log($"Start: {start}, Target: {target}, Detected: {unit.GetComponent<CharacterBase>()}");

                    CharacterBase hitCharacter = unit.GetComponent<CharacterBase>();

                    if (hitCharacter == null) return false;
                    if (hitCharacter.currentTeam == decisionMaker.currentTeam)
                        return false;
                    if (hitCharacter != targetCharacter) return false;

                    return true;
                }
            }
            else
            {
                if (debugMode)
                    Debug.LogWarning("Called on non-projectile skill");
                return true;
            }
            return false;
        }
        #endregion

        private List<GameNode> GetSkillTargetableMovableNode(CharacterBase character, SkillData skill)
        {
            List<GameNode> movableNodes = character.GetMovableNodes();
            HashSet<GameNode> result = new HashSet<GameNode>();

            if (!skill.isTargetTypeSkill) return movableNodes;

            GameNode originNode = character.currentNode;

            if (movableNodes == null || movableNodes.Count == 0) return null;

            foreach (GameNode moveNode in movableNodes)
            {
                if (moveNode == character.currentNode) continue;

                CharacterBase originNodeCharacter = originNode.GetUnitGridCharacter();
                CharacterBase moveNodeCharacter = moveNode.GetUnitGridCharacter();

                //  Simulate character change occupyied node
                if (moveNode != originNode)
                {
                    originNode.SetUnitGridCharacter(null);
                    moveNode.SetUnitGridCharacter(character);
                }
                List<GameNode> influenceNodes = skill.GetInflueneNode(world, moveNode);

                foreach (GameNode node in influenceNodes)
                {
                    if (!IsValidSkillTargetNodeSingle(skill, node)) continue;

                    if (debugMode)
                    {
                        Debug.Log(
                            $"MovableNode: {moveNode.GetNodeVectorInt()}, " +
                            $"Skill: {skill.skillName}, " +
                            $"Influence Node {node.GetNodeVectorInt()}");
                    }
                    result.Add(moveNode);
                }

                //  Revert character occupyied node
                originNode.SetUnitGridCharacter(character);
                moveNode.SetUnitGridCharacter(moveNodeCharacter);
            }
            return result.ToList();
        }

        #region External Methods
        public void GetResult(out SkillData skill, out GameNode moveToNode,
            out GameNode skillTargetNode, out Orientation orientation)
        {
            skill = this.skill;
            moveToNode = this.moveNode;
            skillTargetNode = this.skillTargetNode;
            orientation = this.orientation;
        }
        #endregion
    }
}
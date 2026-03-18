using UnityEngine;

[CreateAssetMenu(fileName = "AI Score", menuName = "Tactics/Utility AI")]
public class UtilityAIScoreConfig : ScriptableObject
{
    #region Rule Scores
    [Header("Head Move Target Rule")]
    [Tooltip("Move Target Rule Score (default = 25)")]
    public int moveTargetRuleScore = 25;
    [Tooltip("Sub rule scores associated with target (default = 5).")]
    public int targeRuleScore = 5;

    [Header("Head Risk Move Rule")]
    [Tooltip("Head Risk Move Rule Score (default = 25)")]
    public int riskMoveRuleScore = 25;
    [Tooltip("Sub rule scores associated with harm (default = 15).")]
    public int harmRuleScore = 15;
    [Tooltip("Sub rule scores associated with treat (default = 15).")]
    public int treatRuleScore = 15;

    [Header("Head Origin Harm Rule")]
    [Tooltip("Head Origin Harm Rule Score (default = 188)")]
    public int originHarmRuleScore = 188;
    [Tooltip("Sub rule scores associated with fatal hit (default = 25).")]
    public int fatalHitRuleScore = 25;

    [Header("Head Origin Treat Rule")]
    [Tooltip("Head Origin Treat Rule Score (default = 188)")]
    public int originTreatRuleScore = 188;

    [Header("Head Risk Move Harm Rule")]
    [Tooltip("Head Risk Move Harm Rule Score (default = 188)")]
    public int riskMoveHarmRuleScore = 188;
    [Tooltip("Sub rule scores associated with fatal hit (default = 25).")]
    public int riskFatalHitRuleScore = 25;

    [Header("Head Risk Move Treat Rule")]
    [Tooltip("Head Risk Move Treat Rule Score (default = 188)")]
    public int riskMoveTreatRuleScore = 188;

    [Header("Head Defense Back Rule")]
    [Tooltip("Defense Back Rule Score (default = 188)")]
    public int defenseBackRuleScore = 25;
    #endregion

    [Header("Harm formula parameter")]
    [Tooltip("parameter min health priority (default = 0.2)")]
    public float minHealthPriority = 0.2f;
    [Tooltip("parameter min harm priority (default = 0.2)")]
    public float minHarmPriority = 0.2f;
    [Tooltip("parameter health priority factor (default = 0.3)")]
    public float priorityHealthFactor = 0.3f;

    [Header("Treat formula parameter")]
    [Tooltip("parameter min heal priority (default = 0.19)")]
    public float minHealPriority = 0.19f;

    [Header("Opposite Risk formula parameter")]
    public float riskInfluenceFactor = 0.02f;

    [Header("Decision Extra Bonus")]
    public float ORIGIN_SKILL_BONUS = 0;
    public float MOVE_SKILL_BONUS = 0;
    public float MOVE_ONLY_BONUS = 0;

    [Header("General Mp formula parameter")]
    [Tooltip("parameter min mp reduction ratio (default = 0)")]
    public float mpMinReductionRatio = 0f;
    [Tooltip("parameter max mp reduction ratio (default = 0.2)")]
    public float mpMaxReductionRatio = 0.2f;
}

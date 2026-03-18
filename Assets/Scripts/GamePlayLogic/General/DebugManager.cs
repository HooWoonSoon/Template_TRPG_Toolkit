using UnityEngine;

public enum RuleDebugContext
{
    None,
    MoveTarget,
    RiskMove,
    Origin_Harm,
    Origin_Treat,
    RiskMove_Harm,
    RiskMove_Treat,
    DefenseBack
}

public static class DebugManager
{
    [Tooltip("Move target rule parent and sub script debug mode")]
    public static bool moveTargetRDebug;
    [Tooltip("Move risk move rule parent and sub script debug mode")]
    public static bool riskMoveRDebug;
    [Tooltip("Harm rule parent and sub script debug mode")]
    public static bool harmRDebug;
    [Tooltip("Treat rule parent and sub script debug mode")]
    public static bool treatRDebug;
    [Tooltip("Risk harm rule parent and sub script debug mode")]
    public static bool riskMoveHarmRDebug;
    [Tooltip("Risk treat rule parent and sub script debug mode")]
    public static bool riskMoveTreatRDebug;
    [Tooltip("Defense back rule parent and sub script debug mode")]
    public static bool defenseBackRDebug;

    public static bool IsDebugEnabled(RuleDebugContext context)
    {
        switch (context)
        {
            case RuleDebugContext.MoveTarget: return moveTargetRDebug;
            case RuleDebugContext.RiskMove: return riskMoveRDebug;
            case RuleDebugContext.Origin_Harm: return harmRDebug;
            case RuleDebugContext.Origin_Treat: return treatRDebug;
            case RuleDebugContext.RiskMove_Harm: return riskMoveHarmRDebug;
            case RuleDebugContext.RiskMove_Treat: return riskMoveTreatRDebug;
            case RuleDebugContext.DefenseBack: return defenseBackRDebug;
            default: return false;
        }
    }
}

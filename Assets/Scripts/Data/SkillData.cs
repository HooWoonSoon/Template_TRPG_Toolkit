using System.Collections.Generic;
using UnityEngine;
public enum SkillTargetType
{
    Self, Our, Opposite, Both
}
public enum SkillType
{
    Acttack, Heal
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Tactics/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Skill Info")]
    public string skillName;
    public Sprite skillIcon;
    public string description;
    public AbilityType abilityType;
    public SkillType skillType;
    public bool isTargetTypeSkill;
    public SkillTargetType skillTargetType;
    public int skillRange;
    public int occlusionRange;
    public int aoeRadius = 1; // If not aoe skill, set to 1

    public bool isProjectile;
    public GameObject projectTilePrefab;
    [Range(0, 90)] public int initialElevationAngle;

    public Sprite MPIcon;
    public bool requireMP;
    public int MPAmount;

    //  If skillType is Attack
    public int damageAmount;

    //  If skillType is Heal
    public int healAmount;

    public float skillCastTime = 1f;

    public List<GameNode> GetInflueneNode(World world, GameNode origin)
    {
        if (world == null)
        {
            Debug.LogWarning("Missing World");
            return null;
        }
        if (origin == null)
        {
            Debug.LogWarning("Missing origin");
            return null;
        }

        List<GameNode> result = new List<GameNode>();
        List<GameNode> coverange = world.GetManhattas3DGameNode(origin.GetNodeVectorInt(), skillRange);
        List<GameNode> occulusion = world.GetManhattas3DGameNode(origin.GetNodeVectorInt(), occlusionRange);

        if (coverange == null && coverange.Count == 0) return result;

        foreach (GameNode node in coverange)
        {
            if (occulusion.Contains(node)) continue;
            result.Add(node);
        }
        return result;
    }
}


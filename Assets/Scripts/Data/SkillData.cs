using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;
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
            if (!HasBlockByVerticalNode(world, origin, node)) continue;
            result.Add(node);
        }
        return result;
    }
    private bool HasBlockByVerticalNode(World world, GameNode from, GameNode to)
    {
        Vector3Int start = from.GetNodeVectorInt();
        Vector3Int end = to.GetNodeVectorInt();

        // Only check vertical nodes (same x and z, different y)
        if (start.x != end.x || start.z != end.z)
            return true;

        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int y = minY + 1; y <= maxY; y++)
        {
            Vector3Int checkPos = new Vector3Int(start.x, y, start.z);

            if (world.loadedNodes.TryGetValue(checkPos, out GameNode node))
            {
                if (node.hasCube)
                    return false;
            }
        }
        return true;
    }

    public List<GameNode> GetInflueneHasNode(World world, GameNode origin)
    {
        Dictionary<GameNode, int> distance = new Dictionary<GameNode, int>();

        List<GameNode> queue = new List<GameNode> {origin};
        List<GameNode> visited = new List<GameNode>{origin};
        distance[origin] = 0;

        int index = 0;

        while (index < queue.Count)
        {
            GameNode currentNode = queue[index];
            index++;

            int currentDistance = distance[currentNode];
            if (currentDistance > skillRange) continue;

            visited.Add(currentNode);
            List<GameNode> neighbourNodes = GetNeighbourNode(world, currentNode);

            foreach (GameNode neighbourNode in neighbourNodes)
            {
                if (visited.Contains(neighbourNode)) continue;
                if (!neighbourNode.hasCube) continue;

                visited.Add(neighbourNode);
                queue.Add(neighbourNode);
                distance[neighbourNode] = currentDistance + 1;
            }
        }
        return visited;
    }
    private List<GameNode> GetNeighbourNode(World world, GameNode currentNode)
    {
        List<GameNode> neighbourNodes = new List<GameNode>();
        Vector3Int currentPos = currentNode.GetNodeVectorInt();
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0)
        };
        foreach (Vector3Int direction in directions)
        {
            Vector3Int neighbourPos = currentPos + direction;
            world.loadedNodes.TryGetValue(neighbourPos, out GameNode neighbourNode);
            if (neighbourNode != null)
            {
                neighbourNodes.Add(neighbourNode);
            }
        }
        return neighbourNodes;
    }
}


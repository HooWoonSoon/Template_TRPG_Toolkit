using UnityEngine;
using System.Collections.Generic;

public class UnitDetectable : Entity
{
    public Vector3 center;
    public Vector3 size;

    private int mahhatassRange;

    public static List<UnitDetectable> all = new List<UnitDetectable>();

    private void OnEnable() { all.Add(this); }
    private void OnDisable() { all.Remove(this); }

    protected override void Start()
    {
        base.Start();
    }

    public UnitDetectable[] OverlapSelfRange()
    {
        List<UnitDetectable> hits = new List<UnitDetectable>();

        Bounds selfBounds = GetRotatedBounds(transform.position, transform.rotation, center, size);

        foreach (UnitDetectable unit in all)
        {
            if (unit == this) continue;

            Bounds otherBounds = GetRotatedBounds(unit.transform.position, unit.transform.rotation, unit.center, unit.size);

            if (selfBounds.Intersects(otherBounds))
            {
                hits.Add(unit);
            }
        }

        return hits.ToArray();
    }

    public Bounds GetRotatedBoundSelf()
    {
        return GetRotatedBounds(transform.position, transform.rotation, center, size);
    }
    public Bounds GetRotatedBounds(Vector3 position, Quaternion rotation, Vector3 center, Vector3 size)
    {
        Vector3 worldCenter = position + rotation * center;
        Vector3 half = size * 0.5f;

        Vector3[] corners = new Vector3[8]
        {
            worldCenter + rotation * new Vector3(-half.x, -half.y, -half.z),
            worldCenter + rotation * new Vector3( half.x, -half.y, -half.z),
            worldCenter + rotation * new Vector3(-half.x,  half.y, -half.z),
            worldCenter + rotation * new Vector3( half.x,  half.y, -half.z),
            worldCenter + rotation * new Vector3(-half.x, -half.y,  half.z),
            worldCenter + rotation * new Vector3( half.x, -half.y,  half.z),
            worldCenter + rotation * new Vector3(-half.x,  half.y,  half.z),
            worldCenter + rotation * new Vector3( half.x,  half.y,  half.z),
        };

        Vector3 min = corners[0];
        Vector3 max = corners[0];
        for (int i = 1; i < 8; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        Bounds b = new Bounds();
        b.SetMinMax(min, max);
        return b;
    }

    /// <summary>
    /// Start from the unit center extend with 3D mahhatass range to obtain other unit detectable
    /// </summary>
    public UnitDetectable[] OverlapMahhatassRange(int mahhatassRange)
    {
        this.mahhatassRange = mahhatassRange;
        List<UnitDetectable> hits = new List<UnitDetectable>();

        foreach (UnitDetectable unit in all)
        {
            if (unit == this) { continue; }

            Vector3Int selfCenter = Utils.RoundXZFloorYInt(transform.position);

            Vector3 otherCenter = unit.transform.position + unit.center;
            Vector3 otherSize = unit.size * 0.5f;
            Vector3 otherMax = otherCenter + otherSize;
            Vector3 otherMin = otherCenter - otherSize;

            Vector3Int closet = new Vector3Int(
                Mathf.Clamp(selfCenter.x, Mathf.FloorToInt(otherMin.x), Mathf.FloorToInt(otherMax.x)),
                Mathf.Clamp(selfCenter.y, Mathf.FloorToInt(otherMin.y), Mathf.FloorToInt(otherMax.y)),
                Mathf.Clamp(selfCenter.z, Mathf.FloorToInt(otherMin.z), Mathf.FloorToInt(otherMax.z))
                );

            int mahhatassDistance = Mathf.Abs(selfCenter.x - closet.x) + Mathf.Abs(selfCenter.y - closet.y) + Mathf.Abs(selfCenter.z - closet.z);
            if (mahhatassDistance <= this.mahhatassRange)
            {
                hits.Add(unit);
            }
        }
        return hits.ToArray();
    }

    #region Check AABB Collision
    public bool CheckBottomForward()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkBottom = centerPos.y - half.y;
        float checkForward = centerPos.z + half.z;

        if (world.CheckSolidNode(transform.position.x, checkBottom, checkForward))
            return true;
        else
            return false;
    }
    public bool CheckBottomBackward()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkBottom = centerPos.y - half.y;
        float checkBackward = centerPos.z - half.z;

        if (world.CheckSolidNode(transform.position.x, checkBottom, checkBackward))
            return true;
        else
            return false;
    }
    public bool CheckBottomRight()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkBottom = centerPos.y - half.y;
        float checkRight = centerPos.x + half.x;

        if (world.CheckSolidNode(checkRight, checkBottom, transform.position.z))
            return true;
        else
            return false;
    }
    public bool CheckBottomLeft()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkBottom = centerPos.y - half.y;
        float checkLeft = centerPos.x - half.x;

        if (world.CheckSolidNode(checkLeft, checkBottom, transform.position.z))
            return true;
        else
            return false;
    }
    public bool CheckCenterForwardCenter()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkForward = centerPos.z + half.z;

        if (world.CheckSolidNode(transform.position.x, centerPos.y, checkForward))
            return true;
        else
            return false;
    }
    public bool CheckCenterBackwardCenter()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkBackward = centerPos.z - half.z;

        if (world.CheckSolidNode(transform.position.x, centerPos.y, checkBackward))
            return true;
        else
            return false;
    }
    public bool CheckCenterRightCenter()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkRight = centerPos.x + half.x;

        if (world.CheckSolidNode(checkRight, centerPos.y, transform.position.z))
            return true;
        else
            return false;
    }
    public bool CheckCenterLeftCenter()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkRight = centerPos.x - half.x;

        if (world.CheckSolidNode(checkRight, centerPos.y, transform.position.z))
            return true;
        else
            return false;
    }
    public bool CheckCenterForwardBothCorner()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkForward = centerPos.z + half.z;
        float checkRight = centerPos.x + half.x;
        float checkLeft = centerPos.x - half.x;

        if (world.CheckSolidNode(checkRight, centerPos.y, checkForward) ||
            world.CheckSolidNode(checkLeft, centerPos.y, checkForward))
            return true;
        else
            return false;
    }
    public bool CheckCenterBackwardConer()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkBackward = centerPos.z - half.z;
        float checkRight = centerPos.x + half.x;
        float checkLeft = centerPos.x - half.x;

        if (world.CheckSolidNode(checkRight, centerPos.y, checkBackward) ||
            world.CheckSolidNode(checkLeft, centerPos.y, checkBackward))
            return true;
        else
            return false;
    }
    public bool CheckCenterRightCorner()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkRight = centerPos.x + half.x;
        float checkForward = centerPos.z + half.z;
        float checkBackward = centerPos.z - half.z;

        if (world.CheckSolidNode(checkRight, centerPos.y, checkForward) ||
            world.CheckSolidNode(checkRight, centerPos.y, checkBackward))
            return true;
        else
            return false;
    }
    public bool CheckCenterLeftCorner()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkLeft = centerPos.x - half.x;
        float checkForward = centerPos.z + half.z;
        float checkBackward = centerPos.z - half.z;

        if (world.CheckSolidNode(checkLeft, centerPos.y, checkForward) ||
            world.CheckSolidNode(checkLeft, centerPos.y, checkBackward))
            return true;
        else
            return false;
    }
    public bool CheckUp()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkUp = centerPos.y + half.y;

        if (world.CheckSolidNode(transform.position.x, checkUp, transform.position.z))
            return true;
        else
            return false;
    }
    public bool CheckUpForward()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkUp = centerPos.y + half.y;
        float checkForward = centerPos.z + half.z;

        if (world.CheckSolidNode(transform.position.x, checkUp, checkForward))
            return true;
        else
            return false;
    }
    public bool CheckUpBackward()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkUp = centerPos.y + half.y;
        float checkBackward = centerPos.z - half.z;

        if (world.CheckSolidNode(transform.position.x, checkUp, checkBackward))
            return true;
        else
            return false;
    }
    public bool CheckUpRight()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkUp = centerPos.y + half.y;
        float checkRight = centerPos.x + half.x;

        if (world.CheckSolidNode(checkRight, checkUp, transform.position.z))
            return true;
        else
            return false;
    }
    public bool CheckUpLeft()
    {
        Vector3 half = size * 0.5f;
        Vector3 centerPos = transform.position + center;
        float checkUp = centerPos.y + half.y;
        float checkLeft = centerPos.x - half.x;

        if (world.CheckSolidNode(checkLeft, checkUp, transform.position.z))
            return true;
        else
            return false;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(center, size);
        
        Gizmos.matrix = oldMatrix;

        if (world != null && mahhatassRange > 0)
        {
            List<Vector3Int> coverage = world.GetManhattas3DRangePosition(Utils.RoundXZFloorYInt(transform.position), mahhatassRange, false);

            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            for (int i = 0; i < coverage.Count; i++)
            {
                Gizmos.DrawWireCube(coverage[i], Vector3.one);
            }
        }
    }
}

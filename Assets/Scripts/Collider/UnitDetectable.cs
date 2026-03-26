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
    public UnitDetectable[] OverlapOBBSelfRange()
    {
        List<UnitDetectable> hits = new List<UnitDetectable>();

        Vector3 half = size * 0.5f;
        Vector3 localCenter = center;

        Vector3[] corners = new Vector3[]
        {
        new Vector3(+half.x, +half.y, +half.z),
        new Vector3(+half.x, -half.y, +half.z),
        new Vector3(-half.x, +half.y, +half.z),
        new Vector3(-half.x, -half.y, +half.z),
        new Vector3(+half.x, +half.y, -half.z),
        new Vector3(+half.x, -half.y, -half.z),
        new Vector3(-half.x, +half.y, -half.z),
        new Vector3(-half.x, -half.y, -half.z),
        };

        foreach (UnitDetectable unit in all)
        {
            if (unit == this) continue;

            Bounds otherBounds = GetBounds(
                unit.transform.position,
                unit.transform.rotation,
                unit.center,
                unit.size
            );

            foreach (var corner in corners)
            {
                Vector3 worldPoint = transform.TransformPoint(localCenter + corner);

                if (otherBounds.Contains(worldPoint))
                {
                    hits.Add(unit);
                    break;
                }
            }
        }

        return hits.ToArray();
    }
    public bool IsOBBOverlapViaCorners(UnitDetectable other)
    {
        Vector3[] myCorners = GetCornersWorld();
        Vector3[] otherCorners = other.GetCornersWorld();

        // self Bounds min/max
        Vector3 minA = myCorners[0], maxA = myCorners[0];
        for (int i = 1; i < 8; i++)
        {
            minA = Vector3.Min(minA, myCorners[i]);
            maxA = Vector3.Max(maxA, myCorners[i]);
        }

        // 求对方盒子的 min/max
        Vector3 minB = otherCorners[0], maxB = otherCorners[0];
        for (int i = 1; i < 8; i++)
        {
            minB = Vector3.Min(minB, otherCorners[i]);
            maxB = Vector3.Max(maxB, otherCorners[i]);
        }

        // 三个方向都重叠就算碰撞
        bool overlapX = maxA.x >= minB.x && minA.x <= maxB.x;
        bool overlapY = maxA.y >= minB.y && minA.y <= maxB.y;
        bool overlapZ = maxA.z >= minB.z && minA.z <= maxB.z;

        return overlapX && overlapY && overlapZ;
    }
    private Vector3[] GetCornersWorld()
    {
        Vector3 half = size * 0.5f;
        Vector3[] corners = new Vector3[]
        {
        new Vector3(+half.x, +half.y, +half.z),
        new Vector3(+half.x, -half.y, +half.z),
        new Vector3(-half.x, +half.y, +half.z),
        new Vector3(-half.x, -half.y, +half.z),
        new Vector3(+half.x, +half.y, -half.z),
        new Vector3(+half.x, -half.y, -half.z),
        new Vector3(-half.x, +half.y, -half.z),
        new Vector3(-half.x, -half.y, -half.z),
        };

        for (int i = 0; i < corners.Length; i++)
            corners[i] = transform.TransformPoint(center + corners[i]);

        return corners;
    }

    public UnitDetectable[] OverlapSelfRange()
    {
        List<UnitDetectable> hits = new List<UnitDetectable>();

        Bounds selfBounds = GetBounds(transform.position, transform.rotation, center, size);

        foreach (UnitDetectable unit in all)
        {
            if (unit == this) continue;

            Bounds otherBounds = GetBounds(unit.transform.position, unit.transform.rotation, unit.center, unit.size);

            if (selfBounds.Intersects(otherBounds))
            {
                hits.Add(unit);
            }
        }

        return hits.ToArray();
    }

    public Bounds GetBoundSelf()
    {
        return GetBounds(transform.position, transform.rotation, center, size);
    }
    public Bounds GetBounds(Vector3 position, Quaternion rotation, Vector3 center, Vector3 size)
    {
        return GetBounds(position, rotation, center, size, out Vector3[] corners);
    }
    public Bounds GetBounds(Vector3 position, Quaternion rotation, Vector3 center, Vector3 size, out Vector3[] corners)
    {
        Vector3 worldCenter = position + rotation * center;
        Vector3 half = size * 0.5f;

        corners = new Vector3[8]
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

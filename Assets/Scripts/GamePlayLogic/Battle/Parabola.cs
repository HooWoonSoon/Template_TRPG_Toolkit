using System.Collections.Generic;
using UnityEngine;


public class Parabola
{
    private World world;
    private bool debugMode;

    public Parabola(World world, bool debugMode = false)
    {
        this.world = world;
        this.debugMode = debugMode;
    }

    public List<UnitDetectable> GetParabolaHitUnits(UnitDetectable projectileDetectable,
        Vector3 start, Vector3 target, int elevationAngle,
        List<UnitDetectable> ingoreUnits)
    {
        List<UnitDetectable> collectedUnits = new List<UnitDetectable>();
        ParabolaHitUnitInternel(projectileDetectable, start,
            target, elevationAngle, collectedUnits, ingoreUnits);
        return collectedUnits;
    }
    public List<UnitDetectable> GetParabolaHitUnits(UnitDetectable projectileDetectable, 
        GameNode start, GameNode target, int elevationAngle, 
        List<UnitDetectable> ingoreUnits)
    {
        List<UnitDetectable> collectedUnits = new List<UnitDetectable>();
        ParabolaHitUnitInternel(projectileDetectable, start.GetNodeVector(), 
            target.GetNodeVector(), elevationAngle, collectedUnits, ingoreUnits);
        return collectedUnits;
    }

    public UnitDetectable GetParabolaHitUnit(UnitDetectable projectileDetectable, 
        GameNode start, GameNode target, int elevationAngle, List<UnitDetectable> ingoreUnits)
    {
        return ParabolaHitUnitInternel(projectileDetectable, start.GetNodeVector(), 
            target.GetNodeVector(), elevationAngle, null, ingoreUnits);
    }

    public UnitDetectable GetParabolaHitUnit(UnitDetectable projectileDetectable, 
        Vector3 start, Vector3 target, int elevationAngle, List<UnitDetectable> ingoreUnits)
    {
        return ParabolaHitUnitInternel(projectileDetectable, start, target, elevationAngle, null, ingoreUnits);
    }

    public UnitDetectable ParabolaHitUnitInternel(UnitDetectable projectileDetectable, 
        Vector3 start, Vector3 target, int elevationAngle, List<UnitDetectable> collections, 
        List<UnitDetectable> ingoreUnits)
    {
        Vector3 displacementXZ = new Vector3(target.x - start.x, 0, target.z - start.z);
        float distanceXZ = displacementXZ.magnitude;
        float heightDifference = target.y - start.y;

        float g = 9.81f;
        float angleRad = elevationAngle * Mathf.Deg2Rad;

        float numerator = g * distanceXZ * distanceXZ;
        float denominator = 2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) * (distanceXZ * Mathf.Tan(angleRad) - heightDifference);

        if (denominator <= 0f)
        {
            Debug.LogWarning("Invalid parameters: Target is too close or below trajectory path.");
            return null;
        }
        float velocity = Mathf.Sqrt(numerator / denominator);

        float rotationY = Mathf.Atan2(displacementXZ.x, displacementXZ.z) * Mathf.Rad2Deg;
        Vector3 forwardDir = Quaternion.Euler(0, rotationY, 0) * Vector3.forward;

        float minSize = Mathf.Min(projectileDetectable.size.x, projectileDetectable.size.y, projectileDetectable.size.z);
        int segments = Mathf.CeilToInt(distanceXZ / (minSize * 0.5f));
        segments = Mathf.Clamp(segments, 50, 1000);

        Vector3 previousPoint = start;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments; // 0~1
            float time = t * (distanceXZ / (velocity * Mathf.Cos(angleRad)));

            //  Parabolic equation
            float x = velocity * Mathf.Cos(angleRad) * time;
            float y = velocity * Mathf.Sin(angleRad) * time + 0.5f * -9.81f * time * time;

            Vector3 nextPoint = start + forwardDir * x + Vector3.up * y;

            Vector3 direction = nextPoint - previousPoint;
            Vector3 center = projectileDetectable.center;
            Vector3 size = projectileDetectable.size;
            if (direction != Vector3.zero)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                Bounds projectileBound = projectileDetectable.GetBounds
                    (nextPoint, rotation, center, size, out Vector3[] corners);

                if (i % 5 == 0)
                    Debug.DrawCube(corners, Color.red, 1.5f);

                if (world.CheckSolidNodeBound(projectileBound))
                {
                    if (debugMode)
                        Debug.Log("Hit Solid break!");
                    break;
                }

                UnitDetectable unit = GetHitUnitDetectable(projectileBound);

                if (unit != null && (ingoreUnits == null || !ingoreUnits.Contains(unit)))
                {
                    if (collections == null)
                    {
                        // Debug.Log($"Target: {target}, Detected: {unit.GetComponent<CharacterBase>()} at point {nextPoint}");
                        return unit;
                    }

                    if (!collections.Contains(unit))
                        collections.Add(unit);
                }
            }

            previousPoint = nextPoint;
        }
        return null;
    }

    public UnitDetectable GetHitUnitDetectable(Bounds bounds)
    {
        List<UnitDetectable> unitDetectables = UnitDetectable.all;

        foreach (UnitDetectable unit in unitDetectables)
        {
            Bounds selfBound = unit.GetBoundSelf();

            if (bounds.Intersects(selfBound))
            {
                return unit;
            }
        }
        return null;
    }


}
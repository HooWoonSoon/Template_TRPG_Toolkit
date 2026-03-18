using UnityEngine;

public class ParabolaRenderer
{
    private World world;
    public LineRenderer lineRenderer;

    public ParabolaRenderer(World world, LineRenderer lineRenderer)
    {
        this.world = world;
        this.lineRenderer = lineRenderer;
    }

    /// <summary>
    /// Draw and projectile parabola visual from start to target with given elevation angle
    /// </summary>
    public void DrawProjectileVisual(Vector3 start, Vector3 target, int elevationAngle)
    {
        if (!CheckParabolaTarget(start, target)) 
        {
            ResetParabolaVisual();
            return; 
        }
        lineRenderer.enabled = true;

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
            return;
        }
        float velocity = Mathf.Sqrt(numerator / denominator);

        float rotationY = Mathf.Atan2(displacementXZ.x, displacementXZ.z) * Mathf.Rad2Deg;
        Vector3 forwardDir = Quaternion.Euler(0, rotationY, 0) * Vector3.forward;

        int segments = Mathf.Clamp((int)(distanceXZ * 8f), 50, 800);

        lineRenderer.positionCount = segments;
        Vector3 previousPoint = start;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments; // 0~1
            float time = t * (distanceXZ / (velocity * Mathf.Cos(angleRad)));

            //  Parabolic equation
            float x = velocity * Mathf.Cos(angleRad) * time;
            float y = velocity * Mathf.Sin(angleRad) * time + 0.5f * -9.81f * time * time;

            Vector3 nextPoint = start + forwardDir * x + Vector3.up * y;
            lineRenderer.SetPosition(i, nextPoint);
            if (world.CheckSolidNodeLine(previousPoint, nextPoint))
            {
                lineRenderer.positionCount = i + 1;
                return;
            }
            previousPoint = nextPoint;
        }
        if (world.CheckSolidNode(target))
        {
            return;
        }
    }

    private bool CheckParabolaTarget(Vector3 start, Vector3 end)
    {
        if (start == end) 
        {
            Debug.Log("Invalid parabola, target to self");
            return false; 
        }
        return true;
    }
    private void ResetParabolaVisual()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }
}
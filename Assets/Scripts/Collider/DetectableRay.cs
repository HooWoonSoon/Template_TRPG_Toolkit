using UnityEngine;
public static class DetectableRay
{
    public static UnitDetectable RayDetectionFollowTarget(Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        float distance = delta.magnitude;
        //Debug.Log($"Current Distance: {distance}");

        if (distance < 0.0001f) return null;

        Vector3 direction = delta / distance;
        float step = 0.5f;
        float current = 0;
        Vector3 lastSample = start;

        while (current < distance)
        {
            Vector3 sample = start + direction * current;
            Debug.DrawLine(lastSample, sample, Color.blue);

            foreach (UnitDetectable unit in UnitDetectable.all)
            {
                if (unit.CheckPositionInSelf(sample))
                {
                    //Debug.Log($"Hit {unit.gameObject.name}");
                    return unit;
                }
            }
            lastSample = sample;
            current += step;
        }
        //Debug.Log("Hit Nothing");
        return null;
    }
}
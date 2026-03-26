using UnityEngine;
public class Debug : UnityEngine.Debug
{
    public static void DrawCube(Vector3[] corners, Color color, float duration)
    {
        DrawLine(corners[0], corners[1], color, duration);
        DrawLine(corners[1], corners[3], color, duration);
        DrawLine(corners[3], corners[2], color, duration);
        DrawLine(corners[2], corners[0], color, duration);
        DrawLine(corners[4], corners[5], color, duration);
        DrawLine(corners[5], corners[7], color, duration);
        DrawLine(corners[7], corners[6], color, duration);
        DrawLine(corners[6], corners[4], color, duration);
        DrawLine(corners[0], corners[4], color, duration);
        DrawLine(corners[1], corners[5], color, duration);
        DrawLine(corners[2], corners[6], color, duration);
        DrawLine(corners[3], corners[7], color, duration);
    }
}

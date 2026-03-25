using UnityEngine;
using System.Collections.Generic;

public class PathRenderer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Vector3 offset = new Vector3(0, 0.1f, 0);
    
    public void RenderPath(List<Vector3> pathVectorList)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = pathVectorList.Count;
        for (int i = 0; i < pathVectorList.Count; i++)
        {
            lineRenderer.SetPosition(i, pathVectorList[i] + offset);
        }
    }
    public void RenderPath(List<Vector3Int> pathVectorList)
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = pathVectorList.Count;
        for (int i = 0; i < pathVectorList.Count; i++)
        {
            lineRenderer.SetPosition(i, pathVectorList[i] + offset);
        }
    }

    public void ClearPath()
    {
        lineRenderer.positionCount = 0;
        lineRenderer.enabled = false;
    }
}
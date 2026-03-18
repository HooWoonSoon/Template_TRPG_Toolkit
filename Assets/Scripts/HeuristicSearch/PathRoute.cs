using System.Collections.Generic;
using UnityEngine;

public class PathRoute
{
    public CharacterBase pathFinder;
    public List<Vector3Int> targetRangeList;

    public List<Vector3> pathNodeVectorList;

    public Vector3Int? targetPosition;
    public int pathIndex = -1;
    private float offset = 0.5f;

    public PathRoute() { }

    public PathRoute(List<GameNode> pathNodeList, Vector3 worldOrigin)
    {
        pathNodeVectorList = new List<Vector3>();

        foreach (GameNode pathNode in pathNodeList)
            pathNodeVectorList.Add(pathNode.GetNodeVector() + new Vector3(0, offset, 0));
        if (pathNodeVectorList.Count > 0)
        {
            pathIndex = 0;
        }
    }

    public void DebugPathRoute()
    {
        if (pathNodeVectorList == null || pathNodeVectorList.Count == 0)
        {
            Debug.Log("PathRoute is empty");
            return;
        }

        string pathLog = string.Join(" -> ", pathNodeVectorList.ConvertAll(p => p.ToString()));
        Debug.Log($"{pathFinder} to PathTarget: {targetPosition},  PathRoute: {pathLog}");
    }
}
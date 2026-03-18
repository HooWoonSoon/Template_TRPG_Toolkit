using System.Collections.Generic;
using UnityEngine;

public class TeamSystem : Entity
{
    public List<PathRoute> teamPathRoutes = new List<PathRoute>();

    public bool IsTargetPositionExist(PathRoute pathRoute, Vector3 targetPosition)
    {
        if (pathRoute.targetPosition.HasValue &&
            targetPosition == pathRoute.targetPosition.Value)
        {
            return true;
        }
        return false;
    }
    private void OnDrawGizmos()
    {
        if (teamPathRoutes.Count == 0) return;
        for (int i = 0; i < teamPathRoutes.Count; i++)
        {
            if (teamPathRoutes[i].targetPosition.Value == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawCube(teamPathRoutes[i].targetPosition.Value + new Vector3(0, 1, 0), Vector3.one);
        }
    }
}
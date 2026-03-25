using UnityEngine;

public class Entity : MonoBehaviour
{
    protected World world;
    protected PathFinding pathFinding;
    protected PathFindingJobThread pathFindingJobThread;

    protected virtual void Start()
    {
        world = MapManager.instance.world;
        pathFinding = MapManager.instance.pathFinding;
        pathFindingJobThread = MapManager.instance.pathFindingJobThread;
    }
}
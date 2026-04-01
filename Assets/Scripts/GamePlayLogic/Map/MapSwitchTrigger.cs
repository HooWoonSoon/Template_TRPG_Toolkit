using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(UnitDetectable))]
public class MapSwitchTrigger : Entity
{
    public string switchMapID;
    public Transform teleportPoint;
    public Transform returnPoint;
    private UnitDetectable selfDetectable;
    private HashSet<UnitDetectable> previousDetected = new HashSet<UnitDetectable>();

    public static MapSwitchTrigger currentTrigger;

    public void Update()
    {
        UnitDetectable selfDetectable = GetComponentInChildren<UnitDetectable>();
        UnitDetectable[] unitDetectables = selfDetectable.OverlapOBBSelfRange();

        bool leaderLeft = CheckPlayerLeaderExit(unitDetectables);
        if (leaderLeft)
        {
            MapTransitionManager.instance.CancelMapTransition(() => currentTrigger = null);
        }

        if (currentTrigger != null) { return; }

        foreach (UnitDetectable detectable in unitDetectables)
        {
            PlayerCharacter playerCharacter = detectable.GetComponent<PlayerCharacter>();
            if (playerCharacter == null) { continue; }
            if (playerCharacter.setLeader)
            {
                if (teleportPoint != null)
                {
                    currentTrigger = this;
                    MapTransitionManager.instance.SaveSnapShot(MapManager.instance.currentActivatedMap, returnPoint, playerCharacter);
                    MapTransitionManager.instance.RequestMapTransition(switchMapID,
                        teleportPoint.position, returnPoint.position, playerCharacter,
                        () => { currentTrigger = null; },
                        () => { currentTrigger = null; });
                }
                return;
            }
        }
    }

    private bool CheckPlayerLeaderExit(UnitDetectable[] unitDetectables)
    {
        previousDetected.RemoveWhere(item => item == null);

        HashSet<UnitDetectable> currentDetected = new HashSet<UnitDetectable>(unitDetectables);
        bool leaderLeft = false;

        foreach (UnitDetectable unit in previousDetected)
        {
            if (!currentDetected.Contains(unit))
            {
                PlayerCharacter playerCharacter = unit.GetComponent<PlayerCharacter>();
                if (playerCharacter == null) { continue; }

                if (playerCharacter.setLeader)
                {
                    Debug.Log("Leader left range");
                    leaderLeft = true;
                }
            }
        }
        previousDetected = currentDetected;
        return leaderLeft;
    }

    private void OnDrawGizmos()
    {
        if (teleportPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(teleportPoint.position, 0.5f);
        }
        if (returnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(returnPoint.position, 0.4f);
        }
    }
}
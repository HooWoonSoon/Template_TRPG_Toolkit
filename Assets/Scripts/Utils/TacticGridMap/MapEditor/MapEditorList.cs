using System.Collections.Generic;
using UnityEngine;

public class MapEditorList : MonoBehaviour
{
    public List<MapEditorLevelList> mapList;

    public void OnEnable()
    {
        RefreshMapList();
    }

    public void AddMap(MapEditorLevelList map)
    {
        mapList.Add(map);
        mapList.RemoveAll(item => item == null);
    }

    private void RefreshMapList()
    {
        mapList = new List<MapEditorLevelList>();
        foreach (MapEditorLevelList map in mapList)
        {
            mapList.Add(map);
        }
    }
}
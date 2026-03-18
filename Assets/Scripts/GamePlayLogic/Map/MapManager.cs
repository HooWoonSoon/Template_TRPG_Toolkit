using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class PresetUnit
{
    public CharacterBase character;
    public Vector3Int deployPos;
}

[Serializable]
public class PresetTeam
{
    public PresetUnit[] presetUnits;
    public TeamType teamType;
}

[Serializable]
public class MapData
{
    public string ID;
    public string mapDataPath;
    public GameObject mapModel;
    public bool requireDeployment;
    public int maxDeployUnitCount;
    public PresetTeam[] presetTeams;
}
public class MapManager : MonoBehaviour
{
    public World world;
    public MapData[] mapDatas;
    public MapData currentActivatedMap { get; private set; }
    public static MapManager instance { get; private set;}

    private void Awake()
    {
        instance = this;
        InitializeMap(mapDatas[0]);
    }

    private void Start()
    {
        if (mapDatas[0].requireDeployment)
            PrepareTeamUnitsDeployState(mapDatas[0]);
        else
            PrepareTeamUnitsFreeState(mapDatas[0]);
    }

    public void InitializeMap(MapData mapData)
    {
        InitializeMap(mapData.mapDataPath, mapData.mapModel);
        currentActivatedMap = mapData;
    }
    private void InitializeMap(string mapDataPath, GameObject mapModel)
    {
        SaveAndLoad.LoadMap(mapDataPath, out world);

        foreach (MapData mapData in mapDatas)
        {
            if (mapData != null)
                mapData.mapModel.SetActive(false);
        }

        if (mapModel != null)
            mapModel.SetActive(true);
    }
   

    public void SwitchMap(MapData mapData)
    {
        SwitchMap(mapData.mapDataPath, mapData.mapModel);
        currentActivatedMap = mapData;
    }
    private void SwitchMap(string mapDataPath, GameObject mapModel)
    {
        GridTilemapVisual.instance.DeSubcribeAllNodes();
        SaveAndLoad.LoadMap(world, mapDataPath, () =>
        {
            GridTilemapVisual.instance.SubscribeAllNodes();
            GameEvent.onMapSwitchedTrigger?.Invoke();
        });

        foreach (MapData mapData in mapDatas)
        {
            if (mapData != null)
                mapData.mapModel.SetActive(false);
        }

        if (mapModel != null)
            mapModel.SetActive(true);
    }

    public MapData GetMapData(string ID)
    {
        foreach (MapData mapData in mapDatas)
        {
            if (mapData.ID == ID)
                return mapData;
        }
        return null;
    }

    public void PrepareTeamUnitsFreeState(MapData mapData)
    {
        MapTeamManager.instance.RemoveTeam(TeamType.Opposite);
        PrepareTeamUnits(mapData, false);
    }
    public void PrepareTeamUnitsDeployState(MapData mapData)
    {
        MapTeamManager.instance.RemoveTeam(TeamType.Opposite);
        PrepareTeamUnits(mapData, true);
    }
    private void PrepareTeamUnits(MapData mapData, bool isDeployState)
    {
        if (mapData.presetTeams == null || mapData.presetTeams.Length == 0)
            return;

        foreach (var presetTeam in mapData.presetTeams)
        {
            List<CharacterBase> teamCharacters = new List<CharacterBase>();

            foreach (var presetUnit in presetTeam.presetUnits)
            {
                CharacterBase character = presetUnit.character;
                GameNode node = world.GetNode(presetUnit.deployPos);

                if (node == null)
                {
                    Debug.LogWarning("Node could not be found");
                }
                else
                {
                    character.gameObject.SetActive(true);

                    if (isDeployState)
                        character.TeleportToNodeDeployble(node);
                    else
                        character.TeleportToNodeFree(node);
                    teamCharacters.Add(character);
                }
            }

            if (isDeployState)
            {
                MapDeploymentUIManager.instance.InsertCharactersInMap(teamCharacters);
                MapTeamManager.instance.GenerateTeam(teamCharacters, presetTeam.teamType, true);
            }
            else
            {
                MapTeamManager.instance.GenerateTeam(teamCharacters, presetTeam.teamType);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (world != null)
        {
            Gizmos.color = Color.green;
            List<Vector3> solids = world.GetAllSolidPos();

            foreach (Vector3 pos in solids)
            {
                Gizmos.DrawWireCube(pos, Vector3.one);
            }
        }
    }
}
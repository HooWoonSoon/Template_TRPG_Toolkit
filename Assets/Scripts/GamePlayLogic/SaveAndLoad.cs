using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;

public static class SaveAndLoad
{
    public static void LoadMap(string mapDataPath, out World world)
    {
        world = new World();
        string fullPath = Path.Combine(Application.persistentDataPath, mapDataPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Map data file not found at path: {fullPath}");
            return;
        }
        string json = File.ReadAllText(fullPath);
        List<GameNodeData> nodeDataList = JsonConvert.DeserializeObject<List<GameNodeData>>(json);
        world.UpdateAndReleaseMapNode(nodeDataList);
        //Debug.Log("Map Loaded");
    }
    public static void LoadMap(World world, string mapDataPath, Action onLoad = null)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, mapDataPath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Map data file not found at path: {fullPath}");
            return;
        }
        string json = File.ReadAllText(fullPath);
        List<GameNodeData> nodeDataList = JsonConvert.DeserializeObject<List<GameNodeData>>(json);
        world.UpdateAndReleaseMapNode(nodeDataList);
        //Debug.Log("Map Loaded");

        onLoad?.Invoke();
    }
}
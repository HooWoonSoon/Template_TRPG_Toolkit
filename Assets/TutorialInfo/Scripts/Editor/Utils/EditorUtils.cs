using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class EditorUtils
{
    public static void DrawScriptLink(string className, Dictionary<string, MonoScript> scriptCache)
    {
        if (!scriptCache.TryGetValue(className, out MonoScript script))
        {
            string[] guids = AssetDatabase.FindAssets($"{className} t:Script");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"Script not found: {className}");
                return;
            }
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            scriptCache[className] = script;
        }

        EditorGUILayout.ObjectField(script, typeof(MonoScript), false);
    }
}
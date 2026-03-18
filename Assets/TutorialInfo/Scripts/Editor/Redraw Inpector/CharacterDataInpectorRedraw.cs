using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

[CustomEditor(typeof(CharacterData))]
public class CharacterDataInpectorRedraw : Editor
{
    private static Dictionary<string, MonoScript> scriptCache =
        new Dictionary<string, MonoScript>();
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Redraw Source Script", EditorStyles.boldLabel);
        EditorUtils.DrawScriptLink(typeof(CharacterDataInpectorRedraw).FullName, scriptCache);

        EditorGUILayout.Space(5);

        CharacterData data = (CharacterData)target;
        CharacterEditorDrawer.DrawCharacterEditor(data);
    }
}
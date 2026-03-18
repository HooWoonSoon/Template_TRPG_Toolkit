using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillData))]
public class SkillDataInpectorRedraw : Editor
{
    private static Dictionary<string, MonoScript> scriptCache =
    new Dictionary<string, MonoScript>();
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Redraw Source Script", EditorStyles.boldLabel);
        EditorUtils.DrawScriptLink(typeof(CharacterDataInpectorRedraw).FullName, scriptCache);

        EditorGUILayout.Space(5);

        SkillData data = (SkillData)target;
        SkillEditorDrawer.DrawSkillEditor(data);
    }
}
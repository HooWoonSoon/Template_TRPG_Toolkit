using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(MapManager))]
public class MapManagerInpectorRedraw : Editor
{
    Dictionary<string, MonoScript> scriptCache = new Dictionary<string, MonoScript>();

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Label("Redraw Source Script", EditorStyles.boldLabel);
        EditorUtils.DrawScriptLink(typeof(MapManagerInpectorRedraw).FullName, scriptCache);
    }
}


[CustomPropertyDrawer(typeof(MapData))]
public class MapDataDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Foldout Control
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded,
            label,
            true
        );

        if (property.isExpanded)
        {
            float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;
            float lineHeight = EditorGUIUtility.singleLineHeight;

            void DrawField(SerializedProperty prop)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), prop);
                y += lineHeight + Spacing;
            }

            DrawField(property.FindPropertyRelative(nameof(MapData.ID)));
            DrawField(property.FindPropertyRelative(nameof(MapData.mapDataPath)));
            DrawField(property.FindPropertyRelative(nameof(MapData.mapModel)));

            SerializedProperty requireProp = property.FindPropertyRelative(nameof(MapData.requireDeployment));
            DrawField(requireProp);

            if (requireProp.boolValue)
            {
                DrawField(property.FindPropertyRelative(nameof(MapData.maxDeployUnitCount)));
            }

            // presetTeams auto height
            SerializedProperty presetTeamsProp = property.FindPropertyRelative(nameof(MapData.presetTeams));
            float presetHeight = EditorGUI.GetPropertyHeight(presetTeamsProp, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, presetHeight), presetTeamsProp, true);
            y += presetHeight + Spacing;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (property.isExpanded)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = Spacing;

            // ID, path, model, requireDeployment
            height += lineHeight * 4 + spacing * 3;

            SerializedProperty requireProp = property.FindPropertyRelative(nameof(MapData.requireDeployment));
            if (requireProp.boolValue)
                height += lineHeight + spacing;

            SerializedProperty presetTeamsProp = property.FindPropertyRelative(nameof(MapData.presetTeams));
            height += EditorGUI.GetPropertyHeight(presetTeamsProp, true) + spacing;
        }

        return height;
    }
}
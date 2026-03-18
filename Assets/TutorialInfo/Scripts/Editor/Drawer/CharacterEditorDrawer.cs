using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CharacterEditorDrawer
{
    public static void DrawCharacterEditor(CharacterBase character)
    {
        if (character == null)
        {
            Debug.LogWarning("Missing Character");
            return;
        }
        GameObject characterGO = character.gameObject;
        if (characterGO != null)
        {
            characterGO.name = EditorGUILayout.TextField("GameObject Name", characterGO.name);
        }

        EditorGUILayout.Space(10);
        if (character.skillDatas == null)
            character.skillDatas = new List<SkillData>();

        for (int i = 0; i < character.skillDatas.Count; i++)
        {
            character.skillDatas[i] = (SkillData)EditorGUILayout.ObjectField(
                $"Skill {i + 1}", character.skillDatas[i], typeof(SkillData), false);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                character.skillDatas.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add Skill"))
        {
            character.skillDatas.Add(null);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        GUIStyle dataGUI = new GUIStyle(EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Character Data", dataGUI);  
        character.data = (CharacterData)EditorGUILayout.ObjectField
            (character.data, typeof(CharacterData), false);
        EditorGUILayout.EndHorizontal();
    }
    public static void DrawCharacterEditor(CharacterData data)
    {
        EditorGUI.BeginChangeCheck();

        data.characterName = EditorGUILayout.TextField("Name", data.characterName);
        data.profile = (Sprite)EditorGUILayout.ObjectField("Profile Image", data.profile,
            (typeof(Sprite)), false);
        data.turnUISprite = (Sprite)EditorGUILayout.ObjectField("Turn UI Sprite", data.turnUISprite,
            (typeof(Sprite)), false);
        data.isometricIcon = (Sprite)EditorGUILayout.ObjectField("Isometric Icon", data.isometricIcon,
            (typeof(Sprite)), false);
        data.type = (TeamType)EditorGUILayout.EnumPopup("Team Type", data.type);

        data.health = EditorGUILayout.IntField("Health", data.health);
        data.mental = EditorGUILayout.IntField("Mental", data.mental);
        data.physicAttack = EditorGUILayout.IntField("Phys ATK", data.physicAttack);
        data.magicAttack = EditorGUILayout.IntField("Mag ATK", data.magicAttack);
        data.speed = EditorGUILayout.IntField("Speed", data.speed);
        data.movementValue = EditorGUILayout.IntField("Movement", data.movementValue);
        data.criticalChance = EditorGUILayout.Slider("Critical Chance", data.criticalChance, 0, 1);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(data);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save", GUILayout.Width(60)))
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
        EditorGUILayout.EndHorizontal();
    }
}
using UnityEditor;
using UnityEngine;
using System;

public static class SkillEditorDrawer
{
    public static void DrawSkillEditor(SkillData data)
    {
        data.skillName = EditorGUILayout.TextField("Skill Name", data.skillName);
        data.skillIcon = (Sprite)EditorGUILayout.ObjectField("Skill Icon", data.skillIcon, typeof(Sprite), false);
        data.description = EditorGUILayout.TextField("Description", data.description);

        AbilityType[] allowedTypes = { AbilityType.Skill, AbilityType.Spell };
        int currentIndex = Array.IndexOf(allowedTypes, data.abilityType);
        if (currentIndex < 0) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup("Type", currentIndex, Array.ConvertAll(allowedTypes, t => t.ToString()));
        data.abilityType = allowedTypes[newIndex];

        data.skillType = (SkillType)EditorGUILayout.EnumPopup("Skill Type", data.skillType);
        
        data.isTargetTypeSkill = EditorGUILayout.Toggle("Is Target Type Skill", data.isTargetTypeSkill);
        if (data.isTargetTypeSkill)
            data.skillTargetType = (SkillTargetType)EditorGUILayout.EnumPopup("Target Type", data.skillTargetType);

        data.skillRange = EditorGUILayout.IntField("Skill Range", data.skillRange);
        data.occlusionRange = EditorGUILayout.IntField("Range Occulsion From Center", data.occlusionRange);
        if (data.occlusionRange > data.skillRange)
        {
            data.occlusionRange = data.skillRange;
            Debug.LogWarning("Occlussion range are not allow to execess the skill range");
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skill Details", EditorStyles.boldLabel);

        if (data.abilityType == AbilityType.Skill)
        {
            data.requireMP = EditorGUILayout.Toggle("Require MP", data.requireMP);
            if (data.requireMP)
                data.MPAmount = EditorGUILayout.IntField("MP Amount", data.MPAmount);
        }
        else if (data.abilityType == AbilityType.Spell)
        {
            data.requireMP = true;
            data.MPAmount = EditorGUILayout.IntField("MP Amount", data.MPAmount);
        }

        if (data.skillType == SkillType.Acttack)
        {
            DrawProjectileSection(data);
            data.damageAmount = EditorGUILayout.IntField("Damage Amount", data.damageAmount);
        }
        else if (data.skillType == SkillType.Heal)
        {
            DrawProjectileSection(data);
            data.healAmount = EditorGUILayout.IntField("Heal Amount", data.healAmount);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Visual Preference", EditorStyles.boldLabel);

        data.skillCastTime = EditorGUILayout.FloatField("Skill Cast Time", data.skillCastTime);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }

    private static void DrawProjectileSection(SkillData data)
    {
        if (data.skillTargetType == SkillTargetType.Self)
        {
            data.skillRange = 1;
        }
        else if (data.skillTargetType == SkillTargetType.Our ||
            data.skillTargetType == SkillTargetType.Both ||
            data.skillTargetType == SkillTargetType.Opposite)
        {
            data.isProjectile = EditorGUILayout.Toggle("Is Projectile", data.isProjectile);
            if (data.isProjectile)
            {
                data.projectTilePrefab = (GameObject)EditorGUILayout.ObjectField("Projectile Prefab", data.projectTilePrefab, typeof(GameObject), false);
                data.initialElevationAngle = EditorGUILayout.IntSlider("Initial Elevation Angle", data.initialElevationAngle, 0, 90);
            }
        }
    }
}
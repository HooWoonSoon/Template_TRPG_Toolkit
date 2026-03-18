using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class SkillManagerEditor : EditorWindow
{
    private Texture2D bannerImage;
    private SkillDatabase skillDatabase;
    private Vector2 scrollPos;
    private SkillData selectedSkill;
    private string searchFilter = "";
    private string updateName = "";

    [MenuItem("Tactics/Skill Manager Editor")]
    private static void ShowWindow()
    {
        SkillManagerEditor window = GetWindow<SkillManagerEditor>();
        window.minSize = new Vector2(800, 700);
    }

    private void OnEnable()
    {
        LoadBannerImage();
        LoadDatabase();
    }

    private void LoadBannerImage()
    {
        bannerImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EditorAssets/Banner_SkillMangerEditor.jpg");
        if (bannerImage == null)
            Debug.LogWarning("Banner image not found. Make sure it's in EditorAssets folder.");
    }

    private void LoadDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != null)
            {
                skillDatabase = AssetDatabase.LoadAssetAtPath<SkillDatabase>(path);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        GUIStyle tileGUI = new GUIStyle(EditorStyles.boldLabel);
        tileGUI.fontSize = 26;
        tileGUI.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Skill Editor", tileGUI);
        EditorGUILayout.Space(5);

        if (bannerImage != null)
        {
            float bannerHeight = 100;
            float bannerWidth = position.width - 10;

            Rect bannerRect = new Rect(5, 10 + tileGUI.fontSize, bannerWidth, bannerHeight);
            GUI.DrawTexture(bannerRect, bannerImage, ScaleMode.ScaleAndCrop);

            EditorGUILayout.Space(bannerHeight + 10f);
        }

        #region UIRegion 1
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(80), GUILayout.Height(35)))
        {
            LoadBannerImage();
            LoadDatabase();
        }

        if (GUILayout.Button("Create Skill Database", GUILayout.Width(150), GUILayout.Height(35)))
        {
            CreateSkillDatabase();
        }

        if (GUILayout.Button("Select Skill Database", GUILayout.Width(150), GUILayout.Height(35)))
        {
            if (skillDatabase != null) Selection.activeObject = skillDatabase;
        }
        EditorGUILayout.EndHorizontal();
        #endregion

        searchFilter = GUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);

        #region UIRegion 2
        EditorGUILayout.BeginHorizontal();

        #region UIRegion 2-1
        GUILayout.BeginVertical("box", GUILayout.Width(215));
        if (GUILayout.Button("Create Skill", EditorStyles.toolbarButton)) CreateSkill();
        GUILayout.Label("Skill List", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        List<SkillData> list = skillDatabase.allSkills
            .Where(s => s != null && (string.IsNullOrEmpty(searchFilter) || s.skillName.ToLower().Contains(searchFilter.ToLower())))
            .ToList();

        if (list.Count == 0)
        {
            EditorGUILayout.HelpBox("Skill Database is empty. Please create more skill", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                DrawSkillCard(list[i]);
            }
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
        #endregion

        #region UIRegion 2-2
        GUILayout.BeginVertical("box");
        GUILayout.Label("Editor", EditorStyles.boldLabel);

        if (selectedSkill != null)
            SkillEditorDrawer.DrawSkillEditor(selectedSkill);
        else
            EditorGUILayout.HelpBox("Press Edit button to select skill to edit", MessageType.Info);

        GUILayout.EndVertical();
        #endregion

        EditorGUILayout.EndHorizontal();
        #endregion

        #region UIRegion 3
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Update Skill Asset Name", GUILayout.Width(180), GUILayout.Height(20)))
        {
            UpdateSkillAssetName(updateName);
        }
        updateName = GUILayout.TextField(updateName, GUILayout.Height(20));
        GUILayout.EndHorizontal();
        #endregion
    }

    private void CreateSkillDatabase()
    {
        if (skillDatabase != null) { return; }
        SkillDatabase newDatabase = CreateInstance<SkillDatabase>();
        AssetDatabase.CreateAsset(newDatabase, "Assets/ScriptableData/SkillDatabase.asset");
        AssetDatabase.SaveAssets();
        skillDatabase = newDatabase;
    }
    private void CreateSkill()
    {
        if (skillDatabase == null) { return; }
        SkillData newSkill = CreateInstance<SkillData>();
        int count = skillDatabase.allSkills.Count;
        
        AssetDatabase.CreateAsset(newSkill, $"Assets/ScriptableData/Skill/Skill({count}).asset");
        skillDatabase.AddSkill(newSkill);
        selectedSkill = newSkill;

        EditorUtility.SetDirty(skillDatabase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    private void DrawSkillCard(SkillData skill)
    {
        if (skill == null) return;

        GUILayout.BeginVertical("box", GUILayout.Width(200));

        Texture2D icon;
        if (skill.skillIcon != null)
            icon = skill.skillIcon.texture;
        else
            icon = Texture2D.grayTexture;

        GUILayout.Label(icon, GUILayout.Width(64), GUILayout.Height(64));

        string nameToShow = "(Unnamed)";
        if (skill.skillName != null)
        {
            nameToShow = skill.skillName;
        }
        GUILayout.Label(nameToShow, EditorStyles.boldLabel);

        string descriptionToShow = "(No description has been written)";
        if (skill.description != null)
        {
            descriptionToShow = skill.description;
        }
        GUILayout.Label(descriptionToShow, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", GUILayout.Width(60)))
            Selection.activeObject = skill;
        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            selectedSkill = skill;
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            if (EditorUtility.DisplayDialog("Delete Skill", $"Are you sure you want to delete skill: {skill.skillName}?", "Yes", "No"))
            {
                skillDatabase.RemoveSkill(skill);
                string path = AssetDatabase.GetAssetPath(skill);
                AssetDatabase.DeleteAsset(path);
                selectedSkill = null;

                EditorUtility.SetDirty(skillDatabase);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void UpdateSkillAssetName(string newName)
    {
        if (string.IsNullOrEmpty(newName)) { return; }

        if (selectedSkill == null) { return; }

        string path = AssetDatabase.GetAssetPath(selectedSkill);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Could not find asset path for selected skill.");
            return;
        }
        string folderPath = System.IO.Path.GetDirectoryName(path);

        string newPath = System.IO.Path.Combine(folderPath, newName + ".asset");

        if (AssetDatabase.LoadAssetAtPath<SkillData>(newPath) != null)
        {
            Debug.LogWarning("A skill with that name already exists.");
            return;
        }

        string error = AssetDatabase.RenameAsset(path, newName);

        if (!string.IsNullOrEmpty(error))
            Debug.LogError($"Failed to rename asset: {error}");
        else
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Skill asset renamed to: {newName}");
        }
    }
}
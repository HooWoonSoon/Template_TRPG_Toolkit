using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class CharacterConfigurationDrawer
{
    public static CharacterData selectedData;
    public static TeamType selectedTeamType;
    public static List<SkillData> configuraSkills;
    public static GameObject characterPreferance;
    public static string gameObjectName = "";

    private static Vector2 scrollPos;
    private static Editor previewEditor;

    public static void DrawCharacterConfiguration(CharacterPools characterPoolsManager)
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (characterPoolsManager == null)
        {
            EditorGUILayout.HelpBox("Missing character pool manager", MessageType.Info);
            return;
        }

        // Implementation for drawing character configuration in the editor
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Character Configuration", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Please input the generated character name");
        gameObjectName = EditorGUILayout.TextField("GameObject Name", gameObjectName);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Please select a character data:");
        selectedData = (CharacterData)EditorGUILayout.ObjectField(
            "Character Data", selectedData, typeof(CharacterData), false);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Please select the unit team:");
        selectedTeamType = (TeamType)EditorGUILayout.EnumPopup("Team Type", selectedTeamType);

        EditorGUILayout.Space(10);
        #region Skill List Configuration
        EditorGUILayout.LabelField("Please configure the skills for the character:");
        EditorGUILayout.LabelField("Configure Skills");
        if (configuraSkills == null)
            configuraSkills = new List<SkillData>();
        int removeIndex = -1;
        for (int i = 0; i < configuraSkills.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            configuraSkills[i] = (SkillData)EditorGUILayout.ObjectField(
                $"Skill {i}", configuraSkills[i], typeof(SkillData), false);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            configuraSkills.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("Add Skill"))
        {
            configuraSkills.Add(null);
        }
        #endregion

        EditorGUILayout.Space(10);
        #region Character Model Setup and Preview
        EditorGUILayout.LabelField("Please setup a character model:");
        var newCharacter = (GameObject)EditorGUILayout.ObjectField(
            "Character Model", characterPreferance, typeof(GameObject), false);

        if (newCharacter != characterPreferance)
        {
            characterPreferance = newCharacter;

            if (previewEditor != null)
            {
                Object.DestroyImmediate(previewEditor);
                previewEditor = null;
            }

            if (characterPreferance != null)
            {
                previewEditor = Editor.CreateEditor(characterPreferance);
            }
        }

        if (previewEditor != null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));
            previewEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.helpBox);
        }
        #endregion

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Character"))
        {
            GenerateCharacter(characterPoolsManager);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private static void GenerateCharacter(CharacterPools characterPoolsManager)
    {
        if (CheckAllInputValid())
        {
            GameObject characterObject = new GameObject($"{gameObjectName}");
            characterObject.SetActive(false);
            characterObject.transform.SetParent(characterPoolsManager.transform, false);

            GameObject modelInstance = null;
            if (PrefabUtility.IsPartOfPrefabAsset(characterPreferance))
            {
                modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterPreferance);
            }
            else
            {
                modelInstance = Object.Instantiate(characterPreferance);
            }

            modelInstance.transform.SetParent(characterObject.transform, false);

            UnitDetectable unitDetectable = characterObject.AddComponent<UnitDetectable>();

            if (selectedTeamType == TeamType.Player)
            {
                PlayerCharacter playerCharacter = characterObject.AddComponent<PlayerCharacter>();
                playerCharacter.unitDetectable = unitDetectable;
                playerCharacter.characterModel = characterPreferance;
                playerCharacter.data = selectedData;
                playerCharacter.skillDatas = configuraSkills;
            }
            else if (selectedTeamType == TeamType.Opposite)
            {
                EnemyCharacter enemyCharacter = characterObject.AddComponent<EnemyCharacter>();
                enemyCharacter.unitDetectable = unitDetectable;
                enemyCharacter.characterModel = characterPreferance;
                enemyCharacter.data = selectedData;
                enemyCharacter.skillDatas = configuraSkills;
            }
            LineRenderer lineRenderer = characterObject.AddComponent<LineRenderer>();
            lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 0.05f);
            CharacterBase characterBase = characterObject.GetComponent<CharacterBase>();
            if (characterBase != null)
            {
                characterPoolsManager.AddCharacter(characterBase);
            }

            ConfrimGenerateDialog();
        }
    }
    private static void ConfrimGenerateDialog()
    {
        if (selectedTeamType == TeamType.Player 
            || selectedTeamType == TeamType.Opposite)
        {
            EditorUtility.DisplayDialog(
                "Generate Success", 
                $"Generate Character {gameObjectName}", 
                "Okay");
        }
    }
    private static bool CheckAllInputValid()
    {
        if (string.IsNullOrEmpty(gameObjectName))
        {
            EditorUtility.DisplayDialog(
                "Invalid Name",// title
                "GameObject Name missing. " +
                "Please input a valid object name",// message
                "Okay"// Button text
            );
            return false;
        }
        if (selectedData == null)
        {
            EditorUtility.DisplayDialog(
                "Invalid Character Data",// title
                "Character Data missing. " +
                "Please select a valid character data",// message
                "Okay"// Button text
            );
            return false;
        }
        if (characterPreferance == null)
        {
            EditorUtility.DisplayDialog(
                "Invalid Character Model",// title
                "Character Model missing. " +
                "Please setup a valid character model",// message
                "Okay"// Button text
            );
            return false;
        }
        return true;
    }
}
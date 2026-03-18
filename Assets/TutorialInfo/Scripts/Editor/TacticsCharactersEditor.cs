using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
public class TacticsCharactersEditor : EditorWindow
{
    private enum PanelState
    {
        CharacterPool,
        CharacterData,
        UtilityAI
    }

    private Texture2D bannerImage;
    private string searchFilter = "";
    private Vector2 scrollPos;
    private Vector2 scrollPos2;
    private Vector2 scrollPos3;
    private Vector2 scrollPos4;

    private CharacterPools characterPool;
    private CharacterBase selectedCharacter;
    private bool enabledCharacterConfiguration = false;

    private CharacterDatabase characterDatabase;
    private CharacterData selectedCharacterData;
    private string updateName = "";

    private UtilityAIDatabase utilityAIDatabase;
    private UtilityAIScoreConfig selectedUtilityAI;

    private PanelState currentPanel = PanelState.CharacterPool;

    private Dictionary<string, MonoScript> scriptCache =
        new Dictionary<string, MonoScript>();

    [MenuItem("Tactics/Tactic Characters Editor")]
    private static void ShowWindow()
    {
        TacticsCharactersEditor window = GetWindow<TacticsCharactersEditor>();
        window.minSize = new Vector2(800, 700);
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        LoadBannerImage();
        LoadCharacterPoolManager();
        LoadCharacterDatabase();
        ClearFilter();
        LoadUtilityAIDatabase();
    }

    private void LoadBannerImage()
    {
        bannerImage = AssetDatabase.LoadAssetAtPath<Texture2D>
            ("Assets/EditorAssets/Banner_CharactersPoolEditor.jpg");
        if (bannerImage == null)
            Debug.LogWarning("Banner image not found. Make sure it's in EditorAssets folder.");
    }

    private void LoadCharacterPoolManager()
    {
        characterPool = GameObject.Find("Characters Pool").GetComponent<CharacterPools>();
        if (characterPool == null)
            Debug.LogWarning("Character pool not found. Make sure it's in Inpectors");
    }

    private void LoadCharacterDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:CharacterDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != null)
            {
                characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(path);
            }
        }
    }

    private void LoadUtilityAIDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:UtilityAIDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != null)
            {
                utilityAIDatabase = AssetDatabase.LoadAssetAtPath<UtilityAIDatabase>(path);
            }
        }
    }

    private void ClearFilter()
    {
        searchFilter = "";
        GUI.FocusControl(null);
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        GUIStyle tileGUI = new GUIStyle(EditorStyles.boldLabel);
        tileGUI.alignment = TextAnchor.MiddleCenter;
        tileGUI.fontSize = 26;
        EditorGUILayout.LabelField("Tactic Character Editor", tileGUI);
        GUILayout.Space(5);

        if (bannerImage != null)
        {
            float bannerHeight = 100;
            float bannerWidth = position.width - 10;

            Rect bannerRect = new Rect(5, 10 + tileGUI.fontSize, bannerWidth, bannerHeight);

            GUI.DrawTexture(bannerRect, bannerImage, ScaleMode.ScaleAndCrop);

            EditorGUILayout.Space(bannerHeight + 10f);
        }

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Editor Script", EditorStyles.boldLabel);
        EditorUtils.DrawScriptLink((typeof(TacticsCharactersEditor)).FullName, scriptCache);
        EditorGUILayout.EndVertical();

        #region UIRegion 1
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical("box");
        GUILayout.Label("Option Mode Interface", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Character Pool"))
        {
            currentPanel = PanelState.CharacterPool;
        }
        if (GUILayout.Button("Character Data"))
        {
            currentPanel = PanelState.CharacterData;
        }
        if (GUILayout.Button("Utility AI"))
        {
            currentPanel = PanelState.UtilityAI;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        #endregion

        GUILayout.BeginHorizontal("box");
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            Refresh();
        }
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        GUILayout.EndHorizontal();

        switch (currentPanel)
        {
            case PanelState.CharacterPool:
                GUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Create Characters Pool", GUILayout.Width(160), GUILayout.Height(25)))
                {
                    CreateCharacterPool();
                }
                if (GUILayout.Button("Select Character Pool", GUILayout.Width(160), GUILayout.Height(25)))
                {
                    if (characterPool != null) Selection.activeObject = characterPool;
                }
                GUILayout.EndHorizontal();

                #region UIRegion Character Pool
                GUILayout.BeginHorizontal();

                #region UIRegion Character Pool - 1
                DrawCharacterListPanel();
                #endregion

                GUILayout.BeginVertical("box");
                scrollPos2 = EditorGUILayout.BeginScrollView(scrollPos2);
                GUILayout.Label("Editor", EditorStyles.boldLabel);

                if (enabledCharacterConfiguration)
                {
                    CharacterConfigurationDrawer.DrawCharacterConfiguration(characterPool);
                }
                else
                {
                    if (selectedCharacter != null)
                    {
                        CharacterEditorDrawer.DrawCharacterEditor(selectedCharacter);
                        if (selectedCharacter.data != null)
                            CharacterEditorDrawer.DrawCharacterEditor(selectedCharacter.data);
                        else
                            EditorGUILayout.HelpBox("Missing character data", MessageType.Info);
                    }
                    else
                        EditorGUILayout.HelpBox("Press Edit button to select character to edit", MessageType.Info);
                }
                GUILayout.EndScrollView();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                #endregion
                break;
            case PanelState.CharacterData:
                GUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Create Character Database", GUILayout.Width(180), GUILayout.Height(25)))
                {
                    CreateCharacterDatabase();
                }
                if (GUILayout.Button("Select Character Database", GUILayout.Width(180), GUILayout.Height(25)))
                {
                    if (characterDatabase != null) Selection.activeObject = characterDatabase;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical("box", GUILayout.Width(220));
                GUILayout.Label("Data List", EditorStyles.boldLabel);

                if (GUILayout.Button("Create New Character Data", EditorStyles.toolbarButton)) CreateData();
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                if (characterDatabase != null && characterDatabase.allCharacterDatas != null)
                {
                    var validList = characterDatabase.allCharacterDatas.Where
                    (c => c != null && (string.IsNullOrEmpty(searchFilter) ||
                        c.name.ToLower().Contains(searchFilter.ToLower()))).ToList();

                    if (validList.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Character datas is empty. Please create more data.", MessageType.Info);
                    }
                    else
                    {
                        foreach (var data in validList)
                        {
                            DrawDataCard(data);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No character data loaded or empty.", MessageType.Warning);
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Scriptable Name", EditorStyles.boldLabel);
                updateName = EditorGUILayout.TextField(updateName);

                if (GUILayout.Button("Update Name"))
                {
                    AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(selectedCharacterData), updateName);
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                GUILayout.Label("Editor", EditorStyles.boldLabel);

                scrollPos3 = GUILayout.BeginScrollView(scrollPos3);
                if (selectedCharacterData != null)
                {
                    CharacterEditorDrawer.DrawCharacterEditor(selectedCharacterData);
                }
                else
                    EditorGUILayout.HelpBox("Press Edit button to select data to edit", MessageType.Info);
                GUILayout.EndScrollView();

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                break;
            case PanelState.UtilityAI:
                GUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Create Utility AI Database", GUILayout.Width(180), GUILayout.Height(25)))
                {
                    CreateUtiltiyDatabase();
                }
                if (GUILayout.Button("Select Utility AI Database", GUILayout.Width(180), GUILayout.Height(25)))
                {
                    if (utilityAIDatabase != null) Selection.activeObject = utilityAIDatabase;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("box", GUILayout.Height(80));
                if (GUILayout.Button("Create New Utility AI", EditorStyles.toolbarButton)) CreateUtilityData();
                scrollPos4 = EditorGUILayout.BeginScrollView(scrollPos4);
                if (utilityAIDatabase != null && utilityAIDatabase.dataSet != null)
                {
                    var validList = utilityAIDatabase.dataSet.Where(d => d != null &&
                    (string.IsNullOrEmpty(searchFilter)) || d.name.ToLower().Contains(searchFilter.ToLower())).
                    ToList();

                    if (validList.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Character datas is empty. Please create more data.", MessageType.Info);
                    }
                    else
                    {
                        foreach (var data in validList)
                        {
                            UtilityAIDrawer.DrawUtilityCard(ref utilityAIDatabase, ref selectedUtilityAI, data);
                        }
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                if (selectedUtilityAI != null)
                {
                    UtilityAIDrawer.DrawAIScoringPanel(selectedUtilityAI);
                }
                else
                    EditorGUILayout.HelpBox("Press Edit button to select data to edit", MessageType.Info);
                break;
        }
    }

    private void DrawCharacterListPanel()
    {
        GUILayout.BeginVertical("box", GUILayout.Width(220));
        GUILayout.Label("Character List", EditorStyles.boldLabel);
        if (GUILayout.Button("New Character Configuration", EditorStyles.toolbarButton))
        {
            enabledCharacterConfiguration = !enabledCharacterConfiguration;
        }
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        if (characterPool == null)
        {
            EditorGUILayout.HelpBox("Character pool manager is missing. Please create one.", MessageType.Info);
            selectedCharacter = null;
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            return;
        }

        List<CharacterBase> allCharacterList = characterPool.allCharacter.ToList();
        List<CharacterBase> list = allCharacterList.Where
            (c => c != null && (string.IsNullOrEmpty(searchFilter) ||
                c.name.ToLower().Contains(searchFilter.ToLower()))).ToList();

        if (list.Count == 0)
        {
            EditorGUILayout.HelpBox("Character Pool is empty. Please create more character", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < list.Count; i++)
            {
                DrawCharacterCard(list[i]);
            }
        }
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void CreateCharacterPool()
    {
        if (characterPool != null)
        {
            EditorUtility.DisplayDialog("Character Pool Exist", "Character pool has been setup properly", "Okay");
            return;
        }

        GameObject poolGameObject = GameObject.Find("Characters Pool");
        if (poolGameObject == null)
            characterPool = new GameObject("Characters Pool").AddComponent<CharacterPools>();
        else
        {
            CharacterPools characterPoolsManager = poolGameObject.GetComponent<CharacterPools>();
            if (characterPoolsManager == null)
                characterPool = poolGameObject.AddComponent<CharacterPools>();
        }
        Selection.activeObject = characterPool;
    }
    private void DrawCharacterCard(CharacterBase character)
    {
        if (character == null) return;

        CharacterData data = character.data;

        if (data == null) return;

        GUILayout.BeginVertical("box", GUILayout.Width(200), GUILayout.Height(150));
        Texture2D icon;
        if (data.turnUISprite != null)
            icon = data.turnUISprite.texture;
        else
            icon = Texture2D.grayTexture;

        Rect imageRect = GUILayoutUtility.GetRect(180, 100, GUILayout.ExpandWidth(false));
        GUI.DrawTexture(imageRect, icon, ScaleMode.ScaleAndCrop);

        string nameToShow = "(Unnamed)";
        nameToShow = character.name;
        
        GUILayout.Label(nameToShow, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", GUILayout.Width(60)))
            Selection.activeObject = character;
        if (GUILayout.Button("Edit", GUILayout.Width(60)))
        {
            selectedCharacter = character;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void CreateCharacterDatabase()
    {
        if (characterDatabase != null)
        {
            EditorUtility.DisplayDialog("Database Exist", "Character database has been setup properly", "Okay");
            return;
        }
        CharacterDatabase database = CreateInstance<CharacterDatabase>();

        AssetDatabase.CreateAsset(database, $"Assets/ScriptableData/CharacterDatabase.asset");
        characterDatabase = database;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = characterDatabase;
    }
    private void CreateData()
    {
        if (characterDatabase == null) { return; }
        CharacterData newData = CreateInstance<CharacterData>();
        int count = characterDatabase.allCharacterDatas.Count;

        AssetDatabase.CreateAsset(newData, $"Assets/ScriptableData/Character/CharacterData({count}).asset");
        characterDatabase.AddCharacterData(newData);
        selectedCharacterData = newData;

        EditorUtility.SetDirty(characterDatabase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// IMGUI to draw character data card
    /// </summary>
    private void DrawDataCard(CharacterData data)
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        Texture2D icon;
        if (data.isometricIcon != null)
            icon = data.isometricIcon.texture;
        else
            icon = Texture2D.grayTexture;

        Rect imageRect = GUILayoutUtility.GetRect(30, 30, GUILayout.ExpandWidth(false));
        GUI.DrawTexture(imageRect, icon, ScaleMode.ScaleAndCrop);

        GUILayout.BeginVertical();
        GUILayout.Label("Name: " + data.name, EditorStyles.boldLabel);
        GUILayout.Label("Type: " + data.type);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        #region Layout
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select", GUILayout.Width(60)))
            Selection.activeObject = data;
        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            selectedCharacterData = data;
            updateName = data.name;
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            /// Confirm delete dialog, pop out warning
            if (EditorUtility.DisplayDialog("Delete Data", $"Are you sure you want to delete data: {data.name}?", "Yes", "No"))
            {
                characterDatabase.RemoveData(data);
                string path = AssetDatabase.GetAssetPath(data);
                AssetDatabase.DeleteAsset(path);
                selectedCharacterData = null;

                EditorUtility.SetDirty(characterDatabase);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        GUILayout.EndHorizontal();
        #endregion
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void CreateUtiltiyDatabase()
    {
        if (utilityAIDatabase != null)
        {
            EditorUtility.DisplayDialog("Database Exist", "Utility AI database has been setup properly", "Okay");
            return;
        }
        UtilityAIDatabase database = CreateInstance<UtilityAIDatabase>();

        AssetDatabase.CreateAsset(database, $"Assets/ScriptableData/AI Score.asset");
        utilityAIDatabase = database;

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = utilityAIDatabase;
    }
    private void CreateUtilityData()
    {
        if (utilityAIDatabase == null) { return; }
        UtilityAIScoreConfig newData = CreateInstance<UtilityAIScoreConfig>();
        int count = utilityAIDatabase.dataSet.Count;

        AssetDatabase.CreateAsset(newData, $"Assets/ScriptableData/AI Score/AI Score({count}).asset");
        utilityAIDatabase.AddData(newData);
        selectedUtilityAI = newData;

        EditorUtility.SetDirty(characterDatabase);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

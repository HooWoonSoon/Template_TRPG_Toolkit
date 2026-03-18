using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MapDeploymentUIManager : Entity
{
    public class UIImage
    {
        public GameObject tagObject;
        public Image image;
        public UIImage(Transform parent, CharacterData data, Sprite tagSprite)
        {
            RectTransform imageRect = new GameObject($"{data.characterName} Deployment Image").AddComponent<RectTransform>();
            imageRect.SetParent(parent, false);
            imageRect.sizeDelta = new Vector2(230, 120);

            image = imageRect.AddComponent<Image>();
            image.sprite = data.turnUISprite;

            tagObject = new GameObject("Arrow");
            RectTransform tag = tagObject.AddComponent<RectTransform>();
            tag.SetParent(imageRect, false);
            tag.anchoredPosition = new Vector2(0, -50);
            tag.sizeDelta = new Vector2(40, 40);
            Image tagImage = tag.gameObject.AddComponent<Image>();
            tagImage.sprite = tagSprite;
            tagObject.SetActive(false);
        }
    }

    public GameObject characterDeploymentPanel;
    public GameObject deploymentScrollView;
    public GameObject characterDeploymentInformation;
    public GameObject startBattleNotificationPanel;
    public GameObject leaveBattlefieldNotifactionPanel;
    public Transform deploymentContent;
    private bool allowToggleDeploymentUI = false;
    private bool enableDeployment = false;

    public List<UIImage> uIImages = new List<UIImage>();
    private List<UIImage> activatedUIImange = new List<UIImage>();
    private UIImage currentSelectedUIImage;

    [SerializeField] private Sprite tagSprite;
    [SerializeField] private int columns;

    [SerializeField] private TextMeshProUGUI maxDeploymentTextUI;
    private int maxDeploymentCount;
    [SerializeField] private TextMeshProUGUI currentDeploymentTextUI;
    private int currentDeploymentCount;

    /// <summary>
    /// Characters available to select and deploy in tactics map
    /// </summary>
    private CharacterBase[] candidateCharacters;
    private List<CharacterBase> allCharactersInMap = new List<CharacterBase>();
    
    private CharacterBase currentSelectedCharacter;

    public int selectedIndex { get; private set; } = -1;
    public static MapDeploymentUIManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    protected override void Start()
    {
        base.Start();
        characterDeploymentPanel.SetActive(false);
        startBattleNotificationPanel.SetActive(false);
        leaveBattlefieldNotifactionPanel.SetActive(false);
        GameEvent.onDeploymentStart += ShowDeploymentUI;
        GridLayoutGroup layoutGroup = deploymentContent.GetComponent<GridLayoutGroup>();
        if (layoutGroup != null)
        {
            columns = layoutGroup.constraintCount;
        }
    }

    public void Update()
    {
        if (!allowToggleDeploymentUI) { return; }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            MapTransitionManager.instance.ClearEventCallback(() =>
            {
                leaveBattlefieldNotifactionPanel.SetActive(false);
            });
            BattleManager.instance.ClearEventCallback(() =>
            {
                startBattleNotificationPanel.SetActive(false);
            });

            if (characterDeploymentPanel.activeSelf)
            {
                characterDeploymentPanel.SetActive(false);
                MapDeploymentManager.instance.ActivateMoveCursorAndHide(true, false);
                MapDeploymentManager.instance.EnableEditingMode(true);
                enableDeployment = false;

                PreviewBattleUI();
            }
            else
            {
                characterDeploymentPanel.SetActive(true);
                MapDeploymentManager.instance.ActivateMoveCursorAndHide(false, true);
                MapDeploymentManager.instance.EnableEditingMode(false);
                enableDeployment = true;

                CloseBattleUI();
            }
        }

        HandleRequestBattle();
        HandleRequestReturnPreviousMap();

        if (!enableDeployment) { return; }
        HandleSelectionInput();
    }

    private void HandleRequestReturnPreviousMap()
    {
        if (leaveBattlefieldNotifactionPanel.activeSelf ||
            startBattleNotificationPanel.activeSelf) { return; }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            leaveBattlefieldNotifactionPanel.SetActive(true);

            MapTransitionManager.instance.RequestReturnPreviousMap(() =>
            {
                ResetModified();
                allCharactersInMap = new List<CharacterBase>();
                CloseBattleUI();
                leaveBattlefieldNotifactionPanel.SetActive(false);
                characterDeploymentPanel.SetActive(false);
                BattleManager.instance.EndBattle();
            }, () =>
            {
                leaveBattlefieldNotifactionPanel.SetActive(false);
            });
        }
    }

    private void HandleRequestBattle()
    {
        if (leaveBattlefieldNotifactionPanel.activeSelf ||
            startBattleNotificationPanel.activeSelf) { return; }

        if (Input.GetKeyDown(KeyCode.E))
        {
            startBattleNotificationPanel.SetActive(true);

            BattleManager.instance.RequestBattle(allCharactersInMap, () =>
            {
                ResetModified();
                allCharactersInMap = new List<CharacterBase>();
                startBattleNotificationPanel.SetActive(false);
                characterDeploymentPanel.SetActive(false);
                MapDeploymentManager.instance.EndDeployment();
            }, () =>
            {
                startBattleNotificationPanel.SetActive(false);
            });
        }
    }

    private void HandleSelectionInput()
    {
        if (leaveBattlefieldNotifactionPanel.activeSelf) { return; }

        if (Input.GetKeyDown(KeyCode.D))
            selectedIndex++;
        else if (Input.GetKeyDown(KeyCode.A))
            selectedIndex--;
        else if (Input.GetKeyDown(KeyCode.S))
            selectedIndex += columns;
        else if (Input.GetKeyDown(KeyCode.W))
            selectedIndex -= columns;

        //  limited the selected range
        selectedIndex = Mathf.Clamp(selectedIndex, 0, uIImages.Count - 1);
        FocusOnCurrentCharacterUI();

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ActivatedCharacterUI();
        }
    }

    private void ActivatedCharacterUI()
    {
        if (activatedUIImange.Contains(currentSelectedUIImage))
        {
            if (currentDeploymentCount <= 0) return;

            Debug.Log("Remove " + currentSelectedCharacter.data.characterName);
            MapDeploymentManager.instance.RemoveCharacterDeployment(currentSelectedCharacter);
            allCharactersInMap.Remove(currentSelectedCharacter);
            activatedUIImange.Remove(currentSelectedUIImage);
            currentSelectedUIImage.image.color = new Color(1, 1, 1, 1f);
            currentDeploymentCount--;
        }
        else
        {
            if (currentDeploymentCount >= maxDeploymentCount) return;

            Debug.Log("Deploy " + currentSelectedCharacter.data.characterName);
            MapDeploymentManager.instance.RandomDeploymentCharacter(currentSelectedCharacter);
            allCharactersInMap.Add(currentSelectedCharacter);
            activatedUIImange.Add(currentSelectedUIImage);
            currentSelectedUIImage.image.color = new Color(1, 1, 1, 0.5f);
            currentDeploymentCount++;
        }
        currentDeploymentTextUI.text = currentDeploymentCount.ToString();
    }

    private void FocusOnCurrentCharacterUI()
    {
        if (selectedIndex == -1 || uIImages.Count == 0) { return; }

        foreach (UIImage image in uIImages)
        {
            image.tagObject.SetActive(false);
        }
        uIImages[selectedIndex].tagObject.SetActive(true);
        currentSelectedUIImage = uIImages[selectedIndex];
        currentSelectedCharacter = candidateCharacters[selectedIndex];
    }

    private void ShowDeploymentUI()
    {
        ResetModified();
        allowToggleDeploymentUI = true;
        enableDeployment = true;
        characterDeploymentPanel.SetActive(true);
        candidateCharacters = MapDeploymentManager.instance.allCharacter;
        maxDeploymentCount = MapDeploymentManager.instance.maxDeploymentCount;
        maxDeploymentTextUI.text = maxDeploymentCount.ToString();
        foreach (CharacterBase character in candidateCharacters)
        {
            if (character == null) { continue; }
            CreateCharacterUI(character);
        }
        FocusOnCurrentCharacterUI();
    }

    private void CreateCharacterUI(CharacterBase character)
    {
        CharacterData data = character.data;
        UIImage characterUI = new UIImage(deploymentContent, data, tagSprite);
        uIImages.Add(characterUI);
    }

    private void ResetModified()
    {
        allowToggleDeploymentUI = false;
        enableDeployment = false;
        uIImages = new List<UIImage>();
        foreach (Transform child in deploymentContent)
        {
            Destroy(child.gameObject);
        }
        maxDeploymentCount = 0;
        currentDeploymentCount = 0;
    }

    #region BattleUI Method
    private void PreviewBattleUI()
    {
        BattleUIManager.instance.battleStatePanel.SetActive(true);
        BattleUIManager.instance.cTTimelineUI.SetActive(true);

        CTTimeline.instance.SetJoinedBattleUnit(allCharactersInMap);
        CTTimeline.instance.SetupTimeline();
    }

    private void CloseBattleUI()
    {
        BattleUIManager.instance.battleStatePanel.SetActive(false);
        BattleUIManager.instance.cTTimelineUI.SetActive(false);
    }
    #endregion

    #region External Methods
    /// <summary>
    /// Record character into the deployment UI system
    /// </summary>
    public void InsertCharactersInMap(List<CharacterBase> characters)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (!allCharactersInMap.Contains(characters[i]))
                allCharactersInMap.Add(characters[i]);
        }
    }
    #endregion
}
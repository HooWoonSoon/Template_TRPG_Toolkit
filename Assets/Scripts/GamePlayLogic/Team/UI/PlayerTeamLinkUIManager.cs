using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerTeamLinkUIManager : MonoBehaviour
{
    public TeamDeployment teamDeployment;

    [SerializeField] private Canvas canvas;
    public TeamLinkUI[] teamLinkUIs;
    private TeamLinkUI currentTeamLinkUI;
    private TeamLinkUI markedTeamLinkUI;

    public MiniUISetTooltip miniUISetTooltip;
    public Vector2[] miniUIPopUpPos = new Vector2[]
    {
        new Vector2(-720, 460),
        new Vector2(-640, 380),
        new Vector2(-720, 300),
        new Vector2(-800, 380)
    };
    public TeamLinkButton teamLinkButton;

    [SerializeField] private LayerMask layerMask;

    private Image currentInteractImage;
    private RectTransform objectRectTransform;
    private Vector2 lastMousePosition;

    private bool isDragging = false;
    private bool isTinyUIPopUp = false;

    private int prevPopUpIndex = -1;
    private GameObject prevInteractObject;

    [Header("Team UI Effect")]
    //[SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private Vector2 UIAdjustedOffset = new Vector2(5, 0);

    private bool activation = true;
    public static PlayerTeamLinkUIManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
        Initialize();
    }

    private void OnEnable()
    {
        GameEvent.onBattleUnitKnockout += (c) => { AutoExchangeSort(); };
    }

    private void Initialize()
    {
        List<PlayerCharacter> characters = teamDeployment.GetAllOfType<PlayerCharacter>();
        int count = Mathf.Min(characters.Count, teamLinkUIs.Length);

        for (int i = 0; i < count; i++)
        {
            teamLinkUIs[i].Initialize(characters[i], i);
        }

        ////  Remove more UI than character
        //for (int i = count; i < teamLinkUIs.Length; i++)
        //{
        //    teamLinkUIs[i].Initialize(null, i);
        //}
    }

    private void Update()
    {
        if (activation == false) { return; }
        //  Summary
        //      Always update the closest UIClass in order to sort and swap the UI
        TeamLinkUI closestUIClass = GetClosestUIClass();
        
        if (Input.GetMouseButtonDown(0))
        {
            //  Summary
            //      Reset all before function
            ResetTeamLinkObject();

            GetTeamLinkUIObject();
            RecordActivePositionInUI();

            ResetTeamLinkClass();
        }
       
        if (Input.GetMouseButton(0))
        {
            DragUI();
            PopInTeamLinkOptionContentDrag();
            PromptTeamUIWhenDrag(closestUIClass);
        }

        if (Input.GetMouseButtonUp(0))
        {
            ExchangeSorts(closestUIClass);
        }

        #region UI Prompt Effect
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ProcessTeamLinkOption(0);
            CameraToOptionCharacter(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ProcessTeamLinkOption(1);
            CameraToOptionCharacter(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ProcessTeamLinkOption(2);
            CameraToOptionCharacter(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            ProcessTeamLinkOption(3);
            CameraToOptionCharacter(3);
        }
        #endregion

        //  Summary
        //      Always update the UI tooltip content
        UpdateUILinkTooltip();
    }

    #region UI Update Content
    private void UpdateUILinkTooltip()
    {
        UpdateUILinkTooltipLinkText();
    }

    private void UpdateUILinkTooltipLinkText()
    {
        if (prevPopUpIndex == -1) { return; }

        List<PlayerCharacter> character = teamDeployment.GetAllOfType<PlayerCharacter>();

        for (int i = 0; i < character.Count; i++)
        {
            if (character[i].index == prevPopUpIndex)
            {
                bool isLink = character[i].isLink;
                miniUISetTooltip.ChangeOptionUILinkText(isLink);
            }
        }
    }
    #endregion

    #region Reset Methods
    private void ResetTeamLinkObject()
    {
        //  Summary
        //      Reset all previous UI object and control state
        currentInteractImage = null;
        objectRectTransform = null;
        currentTeamLinkUI = null;
        lastMousePosition = Input.mousePosition;
        isDragging = false;
    }

    private void ResetTeamLinkClass()
    {
        if (currentTeamLinkUI == null)
        {
            currentInteractImage = null;
            objectRectTransform = null;
            return;
        }
    }
    #endregion

    #region UI Gain Object Methods
    private void GetTeamLinkUIObject()
    {
        if (currentInteractImage != null) { return; }

        GameObject getObject = Utils.GetMouseOverUIElement(canvas);
        if (getObject == null) { return; }
        currentInteractImage = getObject.GetComponent<Image>();
        if (currentInteractImage == null) { return; }

        foreach (TeamLinkUI teamUI in teamLinkUIs)
        {
            if (teamUI.controlImage == currentInteractImage)
            {
                if (teamUI.canDrag)
                {
                    currentTeamLinkUI = teamUI;
                    objectRectTransform = currentInteractImage.GetComponent<RectTransform>();
                    break;
                }
            }
        }
    }

    private void ProcessTeamLinkOption(int index = -1)
    {
        ResetTeamLinkObject();
        GetTeamLinkUI(index);
        PopOutTeamLinkOptionContent();
        ResetTeamLinkClass();
    }

    private void GetTeamLinkUI(int index)
    {
        currentTeamLinkUI = teamLinkUIs[index];
    }

    private TeamLinkUI GetClosestUIClass()
    {
        //  Summary
        //      Get the closest UIClass to the current object, in order to sort and swap the UI
        if (currentTeamLinkUI == null) { return null; }

        TeamLinkUI closestUIClass = null;
        float closestDistance = float.MaxValue;

        foreach (TeamLinkUI teamUI in teamLinkUIs)
        {
            if (objectRectTransform != null && teamUI != currentTeamLinkUI)
            {
                float distance = Vector2.Distance(objectRectTransform.anchoredPosition, teamUI.rectPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestUIClass = teamUI;
                }
            }
        }
        return closestUIClass;
    }
    #endregion

    #region UI Control State
    private void RecordActivePositionInUI()
    {
        if (currentTeamLinkUI == null) { return; }

        lastMousePosition = Input.mousePosition;
    }
    #endregion

    #region Drag Methods
    private void DragUI()
    {
        Vector2 currentMousePosition = Input.mousePosition;
        if (currentMousePosition != lastMousePosition && isDragging == false && objectRectTransform != null) 
        { isDragging = true; }
        if (objectRectTransform == null) { return; }
        //Debug.Log(" currentMousePosition compare to lastMousePosition so is Dragging: " + isDragging);
        Vector2 delta = currentMousePosition - lastMousePosition;
        objectRectTransform.anchoredPosition += new Vector2(delta.x, delta.y);
        lastMousePosition = currentMousePosition;
    }
    #endregion
    private void ExchangeSorts(TeamLinkUI closestUIClass)
    {
        if (closestUIClass == null || isDragging == false) return;

        //Debug.Log($"Closest UIClass Image {closestUIClass.image}");
        bool didSwap = currentTeamLinkUI.Swap(closestUIClass);

        if (didSwap) { ResetTeamLinkObject(); }
    }
    private void AutoExchangeSort()
    {
        int lastIndex = teamLinkUIs.Length - 1;

        for (int i = 0; i < teamLinkUIs.Length; i++)
        {
            TeamLinkUI teamLinkUI = teamLinkUIs[i];
            CharacterBase character = teamLinkUI.character;

            if (character.unitState == UnitState.Knockout ||
                character.unitState == UnitState.Dead)
            {
                ChangeAndSort(teamLinkUI, lastIndex);
                DisableTeamUIDrag(teamLinkUI);
            }
        }
    }
    private void ChangeAndSort(TeamLinkUI teamLinkUI, int toIndex)
    {
        while (teamLinkUI.index < toIndex)
        {
            int nextIndex = teamLinkUI.index + 1;
            if (nextIndex >= teamLinkUIs.Length) { return; }

            TeamLinkUI nextUI = teamLinkUIs[nextIndex];
            teamLinkUI.Swap(nextUI);

            teamLinkUIs[nextIndex] = teamLinkUI;
            teamLinkUIs[nextIndex - 1] = nextUI;
        }
    }

    #region Pop Out /In Methods
    private void PopOutTeamLinkOptionContent()
    {
        if (currentTeamLinkUI == null) { return; }

        //  Summary
        //      Check if the tooltip is already active, if not, activate it
        if (!miniUISetTooltip.gameObject.activeSelf) 
        {
            miniUISetTooltip.gameObject.SetActive(true);
        }

        int currentIndex = currentTeamLinkUI.index;
        miniUISetTooltip.PopOut(miniUIPopUpPos[currentIndex]);
        Debug.Log($"currentIndex: {currentIndex} characterID {currentTeamLinkUI.character}");
        teamLinkButton.Initialize(currentTeamLinkUI);
        
        prevPopUpIndex = currentIndex;
        isTinyUIPopUp = true;
    }

    private void PopInTeamLinkOptionContentDrag()
    {
        if (isDragging == true)
        {
            PopInTeamLinkOptionContent();
        }
    }
    public void PopInTeamLinkOptionContent()
    {
        if (prevPopUpIndex != -1 && isTinyUIPopUp == true)
        {
            miniUISetTooltip.PopIn(miniUIPopUpPos[prevPopUpIndex]);
            prevPopUpIndex = -1;

            isTinyUIPopUp = false;
        }
    }
    #endregion

    #region Prompt UI
    //  Summary
    //      Prompt the UI to show the closest UIClass
    private void PromptTeamUIWhenDrag(TeamLinkUI closestUIClass)
    {
        if (isDragging) { MarkUI(closestUIClass); }
    }

    private void MarkUI(TeamLinkUI closestUIClass)
    {
        if (closestUIClass == null) { return; }

        if (markedTeamLinkUI != closestUIClass)
        {
            if (markedTeamLinkUI != null) { UnmarkUI(); }

            markedTeamLinkUI = closestUIClass;
            closestUIClass.AdjustOffsetToPosition(UIAdjustedOffset);
        }
    }

    private void UnmarkUI()
    {
        if (markedTeamLinkUI == null) { return; }
        
        markedTeamLinkUI.ResetPosition();
        markedTeamLinkUI = null;
    }
    #endregion
    private void CameraToOptionCharacter(int index)
    {
        CharacterBase character = teamLinkUIs[index].character;
        CameraController.instance.ChangeFollowTarget(character.transform);
    }
    private void DisableTeamUIDrag(TeamLinkUI teamLinkUI)
    {
        teamLinkUI.characterIcon.color = new Color(1, 1, 1, 0.5f);
        teamLinkUI.canDrag = false;
    }

    #region External Method
    public void ActivateTeamLinkUI(bool active)
    {
        activation = active;
    }
    #endregion
}
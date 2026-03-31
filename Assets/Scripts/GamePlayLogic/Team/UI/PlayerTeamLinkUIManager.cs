using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class PlayerTeamLinkUIManager : MonoBehaviour
{
    public TeamDeployment teamDeployment;
    public PlayerTeamSystem teamSystem;

    [SerializeField] private Canvas canvas;
    public TeamLinkUI[] teamLinkUIs;
    private TeamLinkUI currentTeamLinkUI;
    private TeamLinkUI markedTeamLinkUI;

    public TeamLinkUIMiniUISet miniUISetTooltip;

    public List<Vector2> queueSelectUIPos = new List<Vector2>
    {
        new Vector2(-800, 410),
        new Vector2(-720, 330),
        new Vector2(-800, 250),
        new Vector2(-880, 330)
    };

    private Image currentInteractImage;
    private RectTransform objectRectTransform;
    private Vector2 lastMousePosition;

    private bool isDragging = false;

    [Header("Team UI Effect")]
    //[SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private Vector2 UIAdjustedOffset = new Vector2(5, 0);

    private enum Method { Drag, KeyOnly }
    [SerializeField] private Method method;

    [Serializable]
    public class UIOptionMode
    {
        public bool isPopOut;
        public bool isPopping;
        public int currentPopOutIndex = -1;
        public int lastPopOutIndex = -1;
        public int selectedIndex = -1;
    }
    private UIOptionMode optionMode = new UIOptionMode();
    public Action<bool> OnOptionalUIState;

    [Serializable]
    public class UIQueueMode
    {
        public bool isQueueMode;
        public int currentHoverIndex = -1;
        public int selectQueueIndex = -1;
    }
    private UIQueueMode queueMode = new UIQueueMode();
    public Action<bool> OnQueueState;

    private string currentInputHandler = "";

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
    
    private void Update()
    {
        if (activation == false) { return; }

        if (method == Method.Drag)
        {
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
                DragToExchangeSorts(closestUIClass);
            }
        }
        else if (method == Method.KeyOnly) 
        {
            HandleQKey();
            HandleQueueModeInput();
            HandlePromptInput();
            HandleOptionModeInput();
            HandleEscape();
        }

        //  Summary
        //      Always update the UI tooltip content
        UpdateUILinkTooltip();
    }

    private void Initialize()
    {
        List<PlayerCharacter> characters = teamDeployment.GetAllOfType<PlayerCharacter>();
        int count = Mathf.Min(characters.Count, teamLinkUIs.Length);

        for (int i = 0; i < count; i++)
        {
            teamLinkUIs[i].Initialize(characters[i], i);
        }
    }

    private void HandleQKey()
    {
        if (!currentInputHandler.Equals("")) return;
        if (optionMode.isPopping) { return; }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentInputHandler = "Q";

            if (!queueMode.isQueueMode && !optionMode.isPopOut)
            {
                currentTeamLinkUI = teamLinkUIs[0];
                optionMode.lastPopOutIndex = currentTeamLinkUI.index;
                EnterQueueMode(currentTeamLinkUI.index);
                PopInOption(() =>
                {
                    currentInputHandler = "";
                });
            }
            else if (!queueMode.isQueueMode && optionMode.isPopOut)
            {
                EnterQueueMode(currentTeamLinkUI.index);
                PopInOption(() =>
                {
                    currentInputHandler = "";
                });
            }
            else if (queueMode.isQueueMode && queueMode.currentHoverIndex == queueMode.selectQueueIndex)
            {
                ExitQueueMode();
                if (!optionMode.isPopOut)
                {
                    PopOutOption(optionMode.lastPopOutIndex, () =>
                    {
                        currentInputHandler = "";
                    });
                }
            }
            else if (queueMode.isQueueMode && queueMode.currentHoverIndex != queueMode.selectQueueIndex)
            {
                ConfirmQueue();
                CloseAllMode();
                currentInputHandler = "";
            }
        }
    }
    private void HandleQueueModeInput()
    {
        if (queueMode.isQueueMode)
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                MoveQueueSelection(1);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                MoveQueueSelection(-1);
            }
        }
    }
    private void HandlePromptInput()
    {
        if (!currentInputHandler.Equals("")) return;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentInputHandler = "Alpha1";
            CameraToOptionCharacter(0);
            ProcessInternalUIBy(0);
            currentInputHandler = "";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentInputHandler = "Alpha2";
            CameraToOptionCharacter(1);
            ProcessInternalUIBy(1);
            currentInputHandler = "";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentInputHandler = "Alpha3";
            CameraToOptionCharacter(2);
            ProcessInternalUIBy(2);
            currentInputHandler = "";
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            currentInputHandler = "Alpha4";
            CameraToOptionCharacter(3);
            ProcessInternalUIBy(3);
            currentInputHandler = "";
        }
    }
    private void HandleOptionModeInput()
    {
        if (optionMode.isPopOut)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                MoveOption(1);
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                MoveOption(-1);
            }
            else if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmOption();
                CloseAllMode();
            }
        }
    }
    private void HandleEscape()
    {
        if (!currentInputHandler.Equals("")) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            currentInputHandler = "Escape";
            CloseAllMode();
            CameraToOptionCharacter(0);
            currentInputHandler = "";
        }
    }

    #region UI Update Content
    private void UpdateUILinkTooltip()
    {
        UpdateUILinkTooltipLinkText();
    }
    private void UpdateUILinkTooltipLinkText()
    {
        if (!optionMode.isPopOut) { return; }

        List<PlayerCharacter> character = teamDeployment.GetAllOfType<PlayerCharacter>();

        for (int i = 0; i < character.Count; i++)
        {
            if (character[i].index == optionMode.currentPopOutIndex)
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
                if (teamUI.character.unitState == UnitState.Active)
                {
                    currentTeamLinkUI = teamUI;
                    objectRectTransform = currentInteractImage.GetComponent<RectTransform>();
                    break;
                }
            }
        }
    }
    private void ProcessInternalUIBy(int index, Action onComplete = null)
    {
        if (queueMode.isQueueMode && optionMode.currentPopOutIndex == index)
        {
            ExitQueueMode();
            PopOutOption(index, onComplete);
        }
        else if (optionMode.currentPopOutIndex == index)
        {
            PopInOption(onComplete);
            return;
        }

        if (!queueMode.isQueueMode)
        {
            if (currentTeamLinkUI != null)
            {
                currentTeamLinkUI.EnableSway(false);
            }

            ResetTeamLinkObject();
            currentTeamLinkUI = teamLinkUIs[index];
            PopOutOption(index, () =>
            {
                ResetTeamLinkClass();
                onComplete?.Invoke();
            });
        }
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
                float distance = Vector2.Distance(objectRectTransform.anchoredPosition, teamUI.locatedPos);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestUIClass = teamUI;
                }
            }
        }
        return closestUIClass;
    }

    #region Queue Method
    private void EnterQueueMode(int index)
    {
        queueMode.isQueueMode = true;
        queueMode.currentHoverIndex = index;
        queueMode.selectQueueIndex = index;
        currentTeamLinkUI = teamLinkUIs[index];

        OnQueueState?.Invoke(true);

        currentTeamLinkUI.EnableSway(true);
        miniUISetTooltip.SetQueueFromUI(queueSelectUIPos[queueMode.currentHoverIndex]);
        miniUISetTooltip.ShowTipLeftAndRight(true);
        miniUISetTooltip.SetQueueTip_Select_And_Confirm();
        Debug.Log("Enable queue sort");
    }
    private void ExitQueueMode()
    {
        queueMode.isQueueMode = false;
        queueMode.currentHoverIndex = -1;
        queueMode.selectQueueIndex = -1;

        OnQueueState?.Invoke(false);

        foreach (var teamLinkUI in teamLinkUIs)
        {
            teamLinkUI.EnableSway(false);
        }
        miniUISetTooltip.queueFromUIObject.SetActive(false);
        miniUISetTooltip.queueSelectUIObject.SetActive(false);
        miniUISetTooltip.ShowTipLeftAndRight(false);
        miniUISetTooltip.SetQueueTip_Select_And_Change();
        Debug.Log("Diasble queue sort");
    }
    private void MoveQueueSelection(int input)
    {
        queueMode.selectQueueIndex += input;
        if (queueMode.selectQueueIndex > teamLinkUIs.Length - 1)
        {
            queueMode.selectQueueIndex = 0;
        }
        else if (queueMode.selectQueueIndex < 0)
        {
            queueMode.selectQueueIndex = teamLinkUIs.Length - 1;
        }

        if (queueMode.currentHoverIndex != queueMode.selectQueueIndex)
            miniUISetTooltip.SetQueueSelectUI(queueSelectUIPos[queueMode.selectQueueIndex]);
        else
            miniUISetTooltip.queueSelectUIObject.SetActive(false);
    }
    private void ConfirmQueue()
    {
        if (queueMode.currentHoverIndex != queueMode.selectQueueIndex)
        {
            int from = queueMode.currentHoverIndex;
            int to = queueMode.selectQueueIndex;

            currentTeamLinkUI.SwapUI(teamLinkUIs[to]);
            Debug.Log($"Swap {from} -> {to}");
            
            currentTeamLinkUI = teamLinkUIs[to];

            TeamLinkUI tempTeamLinkUI = teamLinkUIs[from];
            teamLinkUIs[from] = teamLinkUIs[to];
            teamLinkUIs[to] = tempTeamLinkUI;
        }
        ExitQueueMode();
    }
    #endregion
    private void CloseAllMode()
    {
        ExitQueueMode();
        PopInOption();
    }

    #region Drag Methods
    private void RecordActivePositionInUI()
    {
        if (currentTeamLinkUI == null) { return; }

        lastMousePosition = Input.mousePosition;
    }
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
    private void PopInTeamLinkOptionContentDrag()
    {
        if (isDragging == true)
        {
            PopInOption();
        }
    }
    #endregion
    private void DragToExchangeSorts(TeamLinkUI closestUIClass)
    {
        if (closestUIClass == null) return;

        if (method == Method.Drag && isDragging == false) return;

        //Debug.Log($"Closest UIClass Image {closestUIClass.image}");
        bool didSwap = currentTeamLinkUI.SwapUI(closestUIClass);

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
            teamLinkUI.SwapUI(nextUI);

            teamLinkUIs[nextIndex] = teamLinkUI;
            teamLinkUIs[nextIndex - 1] = nextUI;
        }
    }

    #region Option Methods
    public void PopOutOption(int index, Action onComplete = null)
    {
        optionMode.isPopping = true;
        PopInOption(() =>
        {
            optionMode.lastPopOutIndex = index;
            optionMode.selectedIndex = 0;
            miniUISetTooltip.SelectOptionUI(optionMode.selectedIndex);
            
            miniUISetTooltip.PopOut(currentTeamLinkUI.uiPopupPos, () =>
            {
                optionMode.isPopOut = true;
                optionMode.currentPopOutIndex = index;
                optionMode.isPopping = false;

                OnOptionalUIState?.Invoke(true);
                onComplete?.Invoke();
            });
        });
    }
    public void PopInOption(Action onComplete = null)
    {
        if (!optionMode.isPopOut)
        {
            onComplete?.Invoke();
            return;
        }

        if (optionMode.isPopOut)
        {
            miniUISetTooltip.PopIn(currentTeamLinkUI.uiPopupPos, () =>
            {
                optionMode.isPopOut = false;
                optionMode.currentPopOutIndex = -1;
                optionMode.selectedIndex = -1;

                miniUISetTooltip.DeselectOptionUI();

                OnOptionalUIState?.Invoke(false);
                onComplete?.Invoke();
            });
        }
    }
    public void MoveOption(int input)
    {
        optionMode.selectedIndex += input;
        int maxIndex = miniUISetTooltip.options.Length - 1;
        if (optionMode.selectedIndex < 0) 
        {
            optionMode.selectedIndex = 0;
        }
        else if (optionMode.selectedIndex > maxIndex) 
        {
            optionMode.selectedIndex = maxIndex;
        }
        miniUISetTooltip.SelectOptionUI(optionMode.selectedIndex);
    }
    public void ConfirmOption()
    {
        int index = optionMode.selectedIndex;
        miniUISetTooltip.options[index].Confirm(teamSystem, currentTeamLinkUI);
    }
    #endregion

    /// <summary>
    /// Prompt the UI to show the closest UIClass
    /// </summary>
    /// <param name="closestUIClass"></param>
    private void PromptTeamUIWhenDrag(TeamLinkUI closestUIClass)
    {
        if (isDragging) 
        {
            if (closestUIClass == null) { return; }

            if (markedTeamLinkUI != closestUIClass)
            {
                if (markedTeamLinkUI != null) 
                {
                    markedTeamLinkUI.ResetPosition();
                    markedTeamLinkUI = null;
                }

                markedTeamLinkUI = closestUIClass;
                closestUIClass.AdjustOffsetToPosition(UIAdjustedOffset);
            }
        }
    }

    private void CameraToOptionCharacter(int index)
    {
        CharacterBase character = teamLinkUIs[index].character;
        CameraController.instance.ChangeFollowTarget(character.transform);
    }
    private void DisableTeamUIDrag(TeamLinkUI teamLinkUI)
    {
        teamLinkUI.characterIcon.color = new Color(1, 1, 1, 0.5f);
    }

    #region External Method
    public void ActivateTeamLinkUI(bool active)
    {
        activation = active;
    }
    #endregion
}
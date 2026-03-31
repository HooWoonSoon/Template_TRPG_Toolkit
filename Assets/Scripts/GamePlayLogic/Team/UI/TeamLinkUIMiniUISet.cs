using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TeamLinkUIMiniUISet : MonoBehaviour
{
    public RectTransform parent;
    public TeamLinkOption[] options;
    [SerializeField] private float spacing = 3f;

    public GameObject tipLeftA;
    public GameObject tipRightD;

    [Header("Queue Tip UI")]
    public GameObject queueEnableTipUIObject;
    public GameObject queueConfrimTipUIObject;

    [Header("Queue UI")]
    public GameObject queueFromUIObject;
    public GameObject queueSelectUIObject;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (options == null || options.Length == 0) return;
        for (int i = 0; i < options.Length; i++)
        {
            options[i].Initialize();
        }
        SetOptionUI();
    }
    #endif

    private void Start()
    {
        if (options == null || options.Length == 0) return;
        for (int i = 0; i < options.Length; i++)
        {
            options[i].Initialize();
            options[i].UnEnable();
        }
        ShowTipLeftAndRight(false);
        SetQueueTip_Select_And_Change();
        queueFromUIObject.SetActive(false);
        queueSelectUIObject.SetActive(false);
    }

    #region OptionUI
    private void SetOptionUI()
    {
        //  Summary
        //      Calculate every button height
        float totalHeight = 0;

        for (int i = 0; i < options.Length; i++)
        {
            totalHeight += options[i].rectTransform.rect.height;
            options[i].gameObject.name = "UI Team Tooltip" + "-" + options[i].optionName.ToString();
        }
        totalHeight += spacing * (options.Length - 1);

        float startY = totalHeight * 0.5f;

        //  Summary
        //      For loop every single option and update UI position and text
        for (int i = 0; i < options.Length; i++)
        {
            float buttonHeight = options[i].rectTransform.rect.height;
            float currentY = startY - (buttonHeight * 0.5f);

            options[i].rectTransform.anchoredPosition = new Vector2(options[i].rectTransform.anchoredPosition.x, currentY);
            options[i].textUI.text = options[i].optionName[0];

            startY -= (buttonHeight + spacing);
        }
    }
    public void ChangeOptionUILinkText(bool isLink)
    {
        if (isLink)
            options[0].textUI.text = options[0].optionName[0];
        else
            options[0].textUI.text = options[0].optionName[1];
    }

    private List<Coroutine> popOutCoroutines = new List<Coroutine>();
    public void PopOut(Vector2 popUpPos, Action onComplete = null)
    {
        foreach (var coroutine in popOutCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        parent.anchoredPosition = popUpPos;

        int finished = 0;
        int total = options.Length;

        void OnOneFinished()
        {
            finished++;

            if (finished >= total)
            {
                onComplete?.Invoke();
            }
        }

        for (int i = 0; i < options.Length; i++)
        {
            options[i].gameObject.SetActive(true);
            Utils.ApplyAnimation(this, options[i].rectTransform, options[i].offsetPosLeft, 
                options[i].initialPos, new Vector2(0, 0), options[i].initialScale, 0.25f, true, OnOneFinished, 
                out Coroutine popOutCoroutine);
            popOutCoroutines.Add(popOutCoroutine);
        }
    }
    
    private List<Coroutine> popInCoroutines = new List<Coroutine>();
    public void PopIn(Vector2 popInPos, Action onComplete = null)
    {
        foreach (var coroutine in popInCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        popInCoroutines.Clear();

        parent.anchoredPosition = popInPos;

        int finished = 0;
        int total = options.Length;

        void OnOneFinished()
        {
            finished++;

            if (finished >= total)
            {
                onComplete?.Invoke();
            }
        }

        for (int i = 0; i < options.Length; i++)
        {
            options[i].gameObject.SetActive(true);
            Utils.ApplyAnimation(this, options[i].rectTransform, options[i].initialPos,
                options[i].offsetPosLeft, options[i].initialScale, new Vector2(0, 0), 0.1f, true, OnOneFinished,
                out Coroutine popInCoroutine);
            popInCoroutines.Add(popInCoroutine);
        }
    }

    public void SelectOptionUI(int index)
    {
        if (index < 0 || index >= options.Length) return;

        DeselectOptionUI();
        options[index].Hover(true);
    }
    public void DeselectOptionUI()
    {
        for (int i = 0; i < options.Length; i++) 
            options[i].Hover(false);
    }
    public void ConfirmOption(int index, PlayerTeamSystem teamSystem, TeamLinkUI teamLinkUI)
    {
        options[index].Confirm(teamSystem, teamLinkUI);
    }
    #endregion

    #region QueueSelectedUI
    public void SetQueueSelectUI(Vector2 popUpPos)
    {
        RectTransform rect = queueSelectUIObject.GetComponent<RectTransform>();
        if (rect == null) 
        {
            Debug.LogWarning("Missing Componennt RectTransfrom !");
            return; 
        }
        rect.anchoredPosition = popUpPos;
        queueSelectUIObject.SetActive(true);
    }
    public void SetQueueFromUI(Vector2 popUpPos)
    {
        RectTransform rect = queueFromUIObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("Missing Componennt RectTransfrom !");
            return;
        }
        rect.anchoredPosition = popUpPos;
        queueFromUIObject.SetActive(true);
    }
    #endregion

    public void ShowTipLeftAndRight(bool enable)
    {
        tipLeftA.SetActive(enable);
        tipRightD.SetActive(enable);
    }
    public void SetQueueTip_Select_And_Change()
    {
        queueEnableTipUIObject.SetActive(true);
        queueConfrimTipUIObject.SetActive(false);
    }
    public void SetQueueTip_Select_And_Confirm()
    {
        queueConfrimTipUIObject.SetActive(true);
        queueEnableTipUIObject.SetActive(false);
    }
}
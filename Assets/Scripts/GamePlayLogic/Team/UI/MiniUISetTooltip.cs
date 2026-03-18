using UnityEngine;
using UnityEngine.Events;

public class MiniUISetTooltip : MonoBehaviour
{
    public RectTransform parent;
    public MiniOptionUI[] options;
    [SerializeField] private float spacing = 3f;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (options == null || options.Length == 0) return;
        for (int i = 0; i < options.Length; i++)
        {
            options[i].Initialize();
        }
        SetUI();
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
    }

    private void SetUI()
    {
        //  Summary
        //      Calculate every button height
        float totalHeight = 0;

        for (int i = 0; i < options.Length; i++)
        {
            totalHeight += options[i].rectTransform.rect.height;
            options[i].button.gameObject.name = "UI Team Tooltip" + "-" + options[i].optionName;
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
        {
            options[0].textUI.text = options[0].optionName[0];
        }
        else
        {
            options[0].textUI.text = options[0].optionName[1];
        }
    }

    public void PopOut(Vector2 popUpPos, UnityEvent onComplete = null)
    {
        parent.anchoredPosition = popUpPos;

        for (int i = 0; i < options.Length; i++)
        {
            Utils.ApplyAnimation(this, options[i].rectTransform, options[i].offsetPosLeft, 
                options[i].initialPos, new Vector2(0, 0), options[i].initialScale, 0.3f, true, onComplete);
        }
    }

    public void PopIn(Vector2 popInPos, UnityEvent onComplete = null)
    {
        parent.anchoredPosition = popInPos;

        for (int i = 0; i < options.Length; i++)
        {
            Utils.ApplyAnimation(this, options[i].rectTransform, options[i].initialPos,
                options[i].offsetPosLeft, options[i].initialScale, new Vector2(0, 0), 0.1f, true, onComplete);
        }
    }
}
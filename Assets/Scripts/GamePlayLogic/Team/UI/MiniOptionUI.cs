using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MiniOptionUI
{
    public Button button;
    public TextMeshProUGUI textUI;
    public string[] optionName;

    public RectTransform rectTransform { get; private set; }
    public Vector2 offsetPosLeft { get; private set; }
    public Vector2 offsetPosRight { get; private set; }
    public Vector2 initialPos { get; private set; }
    public Vector2 initialScale { get; private set; }

    public void Initialize()
    {
        if (rectTransform == null)
        {
            rectTransform = button.GetComponent<RectTransform>();
        }

        initialPos = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;

        offsetPosLeft = initialPos - new Vector2(rectTransform.rect.width / 2, 0);
        offsetPosRight = initialPos + new Vector2(rectTransform.rect.width / 2, 0);
    }

    public void UnEnable()
    {
        rectTransform.anchoredPosition = offsetPosLeft;
        rectTransform.localScale = new Vector2(0, 0);
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamLinkOption : MonoBehaviour
{
    public RectTransform rectTransform;
    public TeamLinkOptionMethod optionMethod;
    public Image image;
    public TextMeshProUGUI textUI;
    public string[] optionName;

    public Vector2 offsetPosLeft { get; private set; }
    public Vector2 offsetPosRight { get; private set; }
    public Vector2 initialPos { get; private set; }
    public Vector2 initialScale { get; private set; }

    public void Initialize()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
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

    public void Hover(bool enable)
    {
        //  Tempo use color change
        if (enable)
        {
            image.color = new Color32(86, 70, 0, 220);
        }
        else
        {
            image.color = new Color32(0, 0, 0, 220);
        }
    }

    public void Confirm(PlayerTeamSystem teamSystem, TeamLinkUI teanlinkUI)
    {
        optionMethod.ExecuteOption(teamSystem, teanlinkUI);
    }
}
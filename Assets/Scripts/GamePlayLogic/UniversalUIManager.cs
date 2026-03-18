using UnityEngine;
using TMPro;
using System.Collections;
using Tactics.InputHelper;

public class UniversalUIManager : MonoBehaviour
{
    public GameObject universalPanel;

    public CanvasGroup castSkillNoticeCanvasGroup;
    public TextMeshProUGUI castSkillNoticeText;

    public GameObject UITipGameObject;
    private bool forceEnableUITip = false;
    
    public bool debugMode = false;
    public static UniversalUIManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        GameEvent.onSkillCastStart += (SkillData skill) => ShowSkillCastNotice(skill);
        GameEvent.onSkillCastEnd += CloseSkillCastNotice;
        GameEvent.onBattleUIStart += () => FocesEnableUITip(true);
        GameEvent.onBattleEnd += () => FocesEnableUITip(false);
    }

    private void LateUpdate()
    {
        UITip();
    }

    public void CreateText(CharacterBase character, string text)
    {
        TextMeshProUGUI damangeTextUI = Utils.CreateCanvasText(text, universalPanel.transform, character.transform.position + new Vector3(0, 1, 0), 35, Color.white, TextAlignmentOptions.Center);
        StartCoroutine(UIFadeCoroutine(damangeTextUI, 0f, 1f, 0.2f, false));
        StartCoroutine(UIFadeCoroutine(damangeTextUI, 1f, 0f, 1.5f, true));
        if (debugMode)
            Debug.Log("Generated Text");
    }
    public void CreateCriticalCountText(CharacterBase character, string value)
    {
        string criticalValueText = $"Critical\n{value}";
        CreateText(character, criticalValueText);
    }
    private IEnumerator UIFadeCoroutine(TextMeshProUGUI textUI, float startAlpha, float endAlpha, float duration, bool destroyOnComplete = false)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            textUI.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }
        textUI.alpha = endAlpha;

        if (destroyOnComplete)
        {
            Destroy(textUI.gameObject);
        }
    }

    #region Skill Cast Notice
    private void ShowSkillCastNotice(SkillData skill)
    {
        castSkillNoticeCanvasGroup.gameObject.SetActive(true);
        castSkillNoticeText.text = skill.skillName;
        if (skill.skillCastTime > 0)
        {
            StartCoroutine(Utils.UIFadeCoroutine(castSkillNoticeCanvasGroup, 0, 1, 0.2f));
        }
    }
    private void CloseSkillCastNotice()
    {
        StartCoroutine(CloseSkillCastNoticeCoroutine());
    }
    private IEnumerator CloseSkillCastNoticeCoroutine()
    {
        yield return Utils.UIFadeCoroutine(castSkillNoticeCanvasGroup, 1, 0, 0.2f);
        castSkillNoticeCanvasGroup.gameObject.SetActive(false);
    }
    #endregion

    private void FocesEnableUITip(bool enable)
    {
        forceEnableUITip = enable;
        UITipGameObject.SetActive(enable);
    }
    private void UITip()
    {
        if (forceEnableUITip) { return; }
        if (InputKeyHelper.GetKeyModifier(KeyCode.LeftControl))
        { 
            UITipGameObject.SetActive(true);
        }
        else
        {
            UITipGameObject.SetActive(false);
        }
    }
}
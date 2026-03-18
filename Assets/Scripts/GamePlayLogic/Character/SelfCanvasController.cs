using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SelfCanvasController : MonoBehaviour
{
    public Canvas selfCanvas;
    public Image healtUI;
    public TextMeshProUGUI queueTextUI;

    public void ActiveAll(bool active)
    {
        selfCanvas.gameObject.SetActive(active);
        healtUI.gameObject.SetActive(active);
        queueTextUI.gameObject.SetActive(active);
    }

    public void SetQueue(int number)
    {
        selfCanvas.gameObject.SetActive(true);
        queueTextUI.gameObject.SetActive(true);

        queueTextUI.text = number.ToString();
    }

    public void SetHeathPercetange(float percentage)
    {
        selfCanvas.gameObject.SetActive(true);
        healtUI.gameObject.SetActive(true);

        healtUI.type = Image.Type.Filled;
        healtUI.fillMethod = Image.FillMethod.Horizontal;
        healtUI.fillAmount = percentage;
        //Debug.Log($"Current percent: {percentage}");
    }

    public void ExecuteHealthChange(CharacterBase character, int value)
    {
        StopAllCoroutines();
        StartCoroutine(HealthChangeCorroutine(character, value));
    }

    private IEnumerator HealthChangeCorroutine(CharacterBase character, int value)
    {
        selfCanvas.gameObject.SetActive(true);
        healtUI.gameObject.SetActive(true);

        yield return StartCoroutine(Utils.UIFilledValueChangeCoroutine(
        healtUI, character.data.health, character.currentHealth, value, 0.5f));

        healtUI.gameObject.SetActive(false);
        selfCanvas.gameObject.SetActive(false);
    }
}

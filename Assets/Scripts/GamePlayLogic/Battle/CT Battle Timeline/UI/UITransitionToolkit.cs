using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UITransitionToolkit : MonoBehaviour
{
    public UITransitionMove[] uITransitionMove;

    [Serializable]
    public class UITransitionMove
    {
        public GameObject moveObject;
        public Vector2 fromPosition;
        public Vector2 targetPosition;
        public float transitionDuration = 0.2f;
    }

    private void OnEnable()
    {
        TargetUIShowUpAll();
    }

    private void TargetUIShowUp(int index)
    {
        RectTransform rectTransform = uITransitionMove[index].moveObject.GetComponent<RectTransform>();
        StartCoroutine(TransitionMove(rectTransform, uITransitionMove[index].fromPosition, uITransitionMove[index].targetPosition, uITransitionMove[index].transitionDuration));
    }
    private void TargetUIShowUpAll()
    {
        for (int i = 0; i < uITransitionMove.Length; i++)
        {
            RectTransform rectTransform = uITransitionMove[i].moveObject.GetComponent<RectTransform>();
            StartCoroutine(TransitionMove(rectTransform, uITransitionMove[i].fromPosition, uITransitionMove[i].targetPosition, uITransitionMove[i].transitionDuration));
        }
    }

    private IEnumerator TransitionMove(RectTransform rectTransform, Vector2 from, Vector2 target, float duration)
    {
        if (rectTransform == null) { rectTransform = GetComponent<RectTransform>(); }

        float elapsedTime = 0f;
        rectTransform.anchoredPosition = from; 

        while (elapsedTime < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(from, target, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = target;
    }

    #region External Call
    public void ResetUIFormToTargetPos(int uiIndex)
    {
        TargetUIShowUp(uiIndex);
    }
    #endregion
}

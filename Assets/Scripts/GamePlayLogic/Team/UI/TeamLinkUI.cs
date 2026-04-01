using UnityEngine;
using UnityEngine.UI;

public class TeamLinkUI : MonoBehaviour
{
    public PlayerCharacter character { get; private set; }
    public int index { get; private set; }

    #region Image
    public Vector2 locatedPos;
    public Image controlImage;
    public Image characterIcon;
    #endregion

    public Vector2 uiPopupPos;

    private bool enableSway;

    private void Update()
    {
        if (enableSway)
            SwayUI();
    }

    #region TeamLink UI Management
    public void Initialize(PlayerCharacter character, int index)
    {
        this.character = character;
        controlImage.rectTransform.anchoredPosition = locatedPos;
        this.index = index;
        character.index = index;

        if (characterIcon != null)
        {
            CharacterData data = character.data;
            Sprite sprite = data.isometricIcon;
            if (sprite != null)
            {
                characterIcon.sprite = sprite;
            }
        }
        LinkCharacter(true);
    }
    public void EnableSway(bool enable)
    {
        controlImage.rectTransform.localRotation = Quaternion.Euler(Vector3.zero);
        enableSway = enable;
    }
    private void SwayUI()
    {
        float rotateZAngle = 3f;
        Quaternion rotateAngleForward = Quaternion.Euler(new Vector3(0, 0, rotateZAngle));
        Quaternion rotateAngleReverse = Quaternion.Euler(new Vector3(0, 0, -rotateZAngle));
        float t = Mathf.PingPong(Time.time * 10f, 1f);
        controlImage.rectTransform.localRotation = Quaternion.Lerp(rotateAngleForward, rotateAngleReverse, t);
    }
    public bool SwapUI(TeamLinkUI other)
    {
        if (other == null) return false;

        bool changed = false;

        if (locatedPos != other.locatedPos)
        {
            SwapLocatedPos(other);
            changed = true;
        }

        if (uiPopupPos != other.uiPopupPos)
        {
            SwapUIPopPos(other);
        }

        if (index != other.index)
        {
            SwapIndex(other);
            changed = true;
        }

        if (changed) 
        {
            GameEvent.onLeaderChangedRequest?.Invoke();
            GameEvent.onTeamSortExchange?.Invoke();
        }

        return changed;
    }
    private void SwapLocatedPos(TeamLinkUI other)
    {
        Vector2 tempPosition = other.locatedPos;
        other.UpdatePosition(locatedPos);
        UpdatePosition(tempPosition);
    }
    private void SwapUIPopPos(TeamLinkUI other)
    {
        Vector2 tempUIPopPos = other.uiPopupPos;
        other.uiPopupPos = uiPopupPos;
        uiPopupPos = tempUIPopPos;
    }
    private void SwapIndex(TeamLinkUI other)
    {
        int tempIndex = other.index;
        other.SetIndex(index);
        SetIndex(tempIndex);
    }
    public void SetIndex(int newIndex)
    {
        index = newIndex;
        if (character != null)
            character.index = newIndex;
    }
    private void UpdatePosition(Vector2 newPosition)
    {
        locatedPos = newPosition;
        controlImage.rectTransform.anchoredPosition = locatedPos;
    }

    public void AdjustOffsetToPosition(Vector2 newPosition)
    {
        controlImage.rectTransform.anchoredPosition += newPosition;
    }
    public void ResetPosition()
    {
        if (controlImage != null)
            controlImage.rectTransform.anchoredPosition = locatedPos;
    }

    public void LinkCharacter(bool boolean)
    {
        character.isLink = boolean;
    }
    #endregion
}
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TeamLinkUI
{
    public PlayerCharacter character { get; private set; }
    public int index { get; private set; }
    public int Index
    {
        get => character != null ? character.index : index;
        set
        {
            if (character != null)
            {
                character.index = value;
            }
            index = value;
        }
    }

    #region Image
    public Vector2 rectPosition;
    public Image controlImage;
    public Image characterIcon;
    #endregion
    public bool canDrag = false; 

    #region TeamLink UI Management
    public void Initialize(PlayerCharacter character, int index)
    {
        this.character = character;
        controlImage.rectTransform.anchoredPosition = rectPosition;

        if (characterIcon != null)
        {
            CharacterData data = character.data;
            Sprite sprite = data.isometricIcon;
            if (sprite != null)
            {
                characterIcon.sprite = sprite;
            }
        }

        Index = index;

        if (character.unitState == UnitState.Active)
            canDrag = true;

        LinkCharacter();
    }

    public void UpdatePosition(Vector2 newPosition)
    {
        rectPosition = newPosition;
        controlImage.rectTransform.anchoredPosition = rectPosition;
    }

    public void AdjustOffsetToPosition(Vector2 newPosition)
    {
        controlImage.rectTransform.anchoredPosition += newPosition;
    }

    public bool Swap(TeamLinkUI other)
    {
        if (other == null) return false;

        bool changed = false;

        if (rectPosition != other.rectPosition)
        {
            SwapPosition(other);
            changed = true;
        }

        if (Index != other.Index)
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

    private void SwapPosition(TeamLinkUI other)
    {
        Vector2 tempPosition = other.rectPosition;
        other.UpdatePosition(rectPosition);
        UpdatePosition(tempPosition);
    }

    private void SwapIndex(TeamLinkUI other)
    {
        int tempIndex = other.Index;
        other.Index = Index;
        Index = tempIndex;
    }

    public void ResetPosition()
    {
        if (controlImage != null)
            controlImage.rectTransform.anchoredPosition = rectPosition;
    }

    public void UnlinkCharacter()
    {
        character.isLink = false;
    }

    public void LinkCharacter()
    {
        character.isLink = true;
    }
    #endregion
}
using UnityEngine;
using UnityEngine.UI;

public class TeamLinkButton : MonoBehaviour
{
    public PlayerTeamSystem teamSystem;
    public MiniUISetTooltip uILinkTooltip;
    private TeamLinkUI currentTeamLinkUI;

    public Button linkOrUnlinkButton;
    public Button detailButton;

    public void Initialize(TeamLinkUI currentTeamUIClass)
    {
        this.currentTeamLinkUI = currentTeamUIClass;

        linkOrUnlinkButton.onClick.RemoveAllListeners();
        detailButton.onClick.RemoveAllListeners();

        linkOrUnlinkButton.onClick.AddListener(() => OnClickLinkUnlinkButton());
        detailButton.onClick.AddListener(() => OnClickDetailButton());
    }

    public void OnClickLinkUnlinkButton()
    {
        Debug.Log($"Unlink button clicked with index: {currentTeamLinkUI.index}");
        if (currentTeamLinkUI.character.isLink == true)
        {
            currentTeamLinkUI.UnlinkCharacter();
            teamSystem.RemoveUnlinkCharacterFromTeam(currentTeamLinkUI.character);
            teamSystem.AddCharacterToUnlinkList(currentTeamLinkUI.character);
        }
        else
        {
            currentTeamLinkUI.LinkCharacter();
            teamSystem.InsertTeamFollower(currentTeamLinkUI.character);
            teamSystem.stateMachine.ChangeState(teamSystem.teamSortPathFindingState);
        }
    }

    public void OnClickDetailButton()
    {
        Debug.Log("Detail button clicked with index: " + currentTeamLinkUI.index);
    }
}
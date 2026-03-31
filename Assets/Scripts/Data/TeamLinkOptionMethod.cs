using UnityEngine;

[CreateAssetMenu(menuName = "Tactics/Team Link UI Option")]
public class TeamLinkOptionMethod : ScriptableObject
{
    public enum OptionType
    {
        LinkAndUnlink,
        Detail,
        Control
    }
    public OptionType optionType;

    public void ExecuteOption(PlayerTeamSystem teamSystem, TeamLinkUI teamLinkUI)
    {
        switch (optionType)
        {
            case OptionType.LinkAndUnlink:
                Debug.Log($"Unlink button clicked with index: {teamLinkUI.index}");
                if (teamLinkUI.character.isLink == true)
                {
                    teamLinkUI.LinkCharacter(false);
                    teamSystem.RemoveUnlinkCharacterFromTeam(teamLinkUI.character);
                    teamSystem.AddCharacterToUnlinkList(teamLinkUI.character);
                }
                else
                {
                    teamLinkUI.LinkCharacter(true);
                    teamSystem.InsertTeamFollower(teamLinkUI.character);
                    teamSystem.stateMachine.ChangeState(teamSystem.teamSortPathFindingState);
                }
                break;
            case OptionType.Detail:
                Debug.Log("Detail button clicked with index: " + teamLinkUI.index);
                break;
            case OptionType.Control:
                Debug.Log("Control button clicked with index: " + teamLinkUI.index);
                break;
        }
    }
}
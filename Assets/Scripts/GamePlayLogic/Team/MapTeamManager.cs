using System.Collections.Generic;
using UnityEngine;

public class MapTeamManager : MonoBehaviour
{
    public List<TeamDeployment> allTeam = new List<TeamDeployment>();
    public static MapTeamManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    public void RemoveTeam(TeamType teamType)
    {
        List<TeamDeployment> toRemove = new List<TeamDeployment>();

        foreach (TeamDeployment team in allTeam)
        {
            if (team.teamType == teamType)
            {
                toRemove.Add(team);
            }
        }
        RemoveTeams(toRemove);
    }
    public void RemoveTeams(List<TeamDeployment> teams)
    {
        foreach (TeamDeployment team in teams)
        {
            RemoveTeam(team);
        }
    }
    public void RemoveTeam(TeamDeployment team)
    {
        if (allTeam.Contains(team))
        {
            allTeam.Remove(team);
            if (team != null && team.gameObject != null)
            {
                Destroy(team.gameObject);
            }
        }
    }

    public void GenerateTeam(List<CharacterBase> characters, TeamType teamType, bool isDeploy = false)
    {
        if (characters == null || characters.Count == 0) { return; }

        GameObject team = new GameObject($"Teams System");
        team.transform.SetParent(transform, false);
        TeamDeployment teamDeployment = team.AddComponent<TeamDeployment>();
        teamDeployment.teamCharacter = new List<CharacterBase>(characters);
        allTeam.Add(teamDeployment);

        foreach (CharacterBase character in characters)
        {
            character.currentTeam = teamDeployment;
        }

        switch (teamType)
        {
            case TeamType.Opposite:
                teamDeployment.teamType = TeamType.Opposite;
                if (!isDeploy)
                {
                    team.name = "Explore " + team.name + " (Opposite) " + allTeam.Count.ToString();
                    EnemyTeamSystem enemyTeamSystem = team.AddComponent<EnemyTeamSystem>();
                    enemyTeamSystem.Initialize<TeamScoutingState>(teamDeployment);
                    //Debug.Log("Generate Team Scouting");
                }
                else
                {
                    team.name = "Deploy " + team.name + " (Opposite) Tempo"; 
                }
                break;
            case TeamType.Player:
                teamDeployment.teamType = TeamType.Player;
                team.name = "Deploy " + team.name + " (Player) Tempo";
                break;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTeamSystem : TeamSystem
{
    public TeamDeployment teamDeployment;
    public TeamStateMachine stateMachine;

    public TeamIdleState teamIdleState { get; private set; }
    public TeamScoutingState teamScoutingState { get; private set; }

    private Dictionary<Type, EnemyTeamState> stateCache = new Dictionary<Type, EnemyTeamState>();

    public HashSet<TeamDeployment> detectedTeam = new HashSet<TeamDeployment>();
    public List<CharacterBase> detectedCharacters = new List<CharacterBase>();
    public HashSet<CharacterBase> lastUnit = new HashSet<CharacterBase>();
     
    private Vector3 lastPosition;

    private void Awake()
    {
        stateMachine = new TeamStateMachine();

        RegisterState(new TeamIdleState(stateMachine, this));
        RegisterState(new TeamScoutingState(stateMachine, this));
    }
    protected override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        stateMachine.currentEnemyTeamState?.Update();
    }

    private void RegisterState(EnemyTeamState state)
    {
        var type = state.GetType();
        if (!stateCache.ContainsKey(type))
            stateCache.Add(type, state);
    }

    public void Initialize<T>(TeamDeployment teamDeployment) where T : EnemyTeamState
    {
        this.teamDeployment = teamDeployment;
        InitializeState<T>();
    }

    public void InitializeState<T>() where T : EnemyTeamState
    {
        if (stateCache.TryGetValue(typeof(T), out var state))
        {
            stateMachine.Initialize(state);
        }
        else
        {
            var newState = (T)Activator.CreateInstance(typeof(T), stateMachine, this);
            RegisterState(newState);
            stateMachine.Initialize(newState);
        }
    }

    #region Scouting
    public void TeamSouting()
    {
        foreach (CharacterBase character in teamDeployment.teamCharacter)
        {
            DetectedEntireTeamCharacter(character);
        }

        if (detectedCharacters.Count == 0 || detectedTeam.Count == 0) { return; }
        HashSet<CharacterBase> allUnit = GetDetectableAndSelfTeamUnit();
        if (lastUnit.SetEquals(allUnit)) { return; }
        //Debug.Log("Last Unit different");
        lastUnit = new HashSet<CharacterBase>(allUnit);
        BattleManager.instance.SetJoinedBattleUnit(allUnit);
        BattleManager.instance.PreapreBattleContent();
    }

    private HashSet<CharacterBase> GetDetectableAndSelfTeamUnit()
    {
        HashSet<CharacterBase> result = new HashSet<CharacterBase>();
        foreach (CharacterBase character in teamDeployment.teamCharacter)
        {
            if (!result.Contains(character))
            {
                result.Add(character);
            }
        }
        foreach (CharacterBase character in detectedCharacters)
        {
            if (!result.Contains(character))
            {
                result.Add(character);
            }
        }
        return result;
    }

    private void DetectedEntireTeamCharacter(CharacterBase character)
    {
        UnitDetectable[] unitDetectable = character.unitDetectable.OverlapMahhatassRange(5);

        foreach (UnitDetectable hit in unitDetectable)
        {
            CharacterBase detectedCharacter = hit.GetComponent<CharacterBase>();

            if (detectedCharacter == null) { continue; }
            if (IsSameTeamMember(detectedCharacter)) { continue; }

            TeamDeployment dectectTeam = detectedCharacter.currentTeam;
            if (IsSameTeam(dectectTeam)) { continue;}

            List<CharacterBase> dectectTeamCharacter = dectectTeam.teamCharacter;
            if (!detectedTeam.Contains(dectectTeam))
            {
                detectedTeam.Add(dectectTeam);
            }
            foreach (CharacterBase teamCharacter in dectectTeamCharacter)
            {
                if (!detectedCharacters.Contains(teamCharacter))
                {
                    detectedCharacters.Add(teamCharacter);
                }
            }
        }
    }

    private bool IsSameTeam(TeamDeployment team)
    {
        if (teamDeployment == team) { return true; }
        else if (team == null) { return false; }
        return false;
    }

    private bool IsSameTeamMember(CharacterBase character)
    {
        if (character.currentTeam == teamDeployment) { return true; }
        return false;
    }
    #endregion

    public void SwitchScoutingMode(bool active)
    {
        if (active)
            stateMachine.ChangeState(teamScoutingState);
        else
            stateMachine.ChangeState(teamIdleState);
    }
}
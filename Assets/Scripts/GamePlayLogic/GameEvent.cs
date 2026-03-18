using System;

public static class GameEvent
{
    //  Team Related Events
    public static Action onLeaderChangedRequest;
    public static Action<CharacterBase> onLeaderChanged;
    public static Action onTeamSortExchange;

    //  Skill UI Related Event
    public static Action onListOptionChanged;

    //  Battle Related Event
    public static Action onBattleStart;
    public static Action onBattleEnd;
    public static Action<CharacterBase> onBattleUnitKnockout;

    //  Battle UI Related Event
    public static Action onBattleUIStart;
    public static Action onBattleUIFinish;

    public static Action onMapSwitchedTrigger;
    public static Action onEnterMap;
    public static Action onEnterDeployMap;

    //  Deployment Related Event
    public static Action onDeploymentStart;
    public static Action onDeploymentEnd;

    public static Action<SkillData> onSkillCastStart;
    public static Action onSkillCastEnd;
}
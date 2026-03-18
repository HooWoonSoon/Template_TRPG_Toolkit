using System;

[Serializable]
public class TeamFollower
{
    public PlayerCharacter character;
    public PlayerCharacter targetToFollow;

    public void Initialize(PlayerCharacter unitCharacter, PlayerCharacter targetToFollow)
    {
        this.character = unitCharacter;
        this.targetToFollow = targetToFollow;
    }
}
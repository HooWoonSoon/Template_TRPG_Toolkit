using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Tactics/Character")]
public class CharacterData : ScriptableObject
{
    [Header("Character Information")]
    public string characterName;
    public Sprite profile;
    public Sprite turnUISprite;
    public Sprite isometricIcon;
    public TeamType type;

    [Header("Properties")]
    public int health;
    public int mental;
    public int physicAttack;
    public int magicAttack;
    public int speed;
    public int movementValue;
    public float criticalChance = 0.1f;
}

using UnityEngine;

public enum ConsumableType
{
    Health, Damage
}

[CreateAssetMenu(fileName = "InventoryData", menuName = "Tactics/Inventory")]
public class InventoryData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public string description;
    public AbilityType type;

    #region Consumble
    //  Only used if itemType is Consumable
    public ConsumableType consumableType;
    public int range;

    //  Only used if consumableType is Health
    public int healthAmount;

    //  Only used if consumableType is Damage
    public int damageAmount;
    #endregion
}


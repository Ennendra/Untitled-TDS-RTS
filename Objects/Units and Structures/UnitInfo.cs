using Godot;
using System;
public enum UnitInfoType
{
    UNIT_COMBATANT,
    UNIT_OTHER,
    STRUCTURE_COMBATANT,
    STRUCTURE_BUILDER,
    STRUCTURE_NETWORKHUB,
    OTHER
}

public partial class UnitInfo : Resource
{
    [Export] public string unitName;
    [Export] public string unitDescription;
    [Export] public float maxHealth;
    [Export] public float energyCost, metalCost;
    [Export] public Texture2D iconTex;
    [Export] public UnitInfoType unitInfoType;
    [Export] public string damage;

    public UnitInfo() : this("","",120,10,10,null, UnitInfoType.OTHER, "") { }

    public UnitInfo(string unitName, string unitDescription, float maxHealth, float energyCost, float metalCost, Texture2D iconTex, UnitInfoType unitInfoType, string damage)
    {
        this.unitName = unitName;
        this.unitDescription = unitDescription;
        this.maxHealth = maxHealth;
        this.energyCost = energyCost;
        this.metalCost = metalCost;
        this.iconTex = iconTex;
        this.unitInfoType = unitInfoType;
        this.damage = damage;
    }

}

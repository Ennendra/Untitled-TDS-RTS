using Godot;
using System;
public enum UnitInfoType
{
    UNIT_COMBATANT,
    UNIT_OTHER,
    STRUCTURE_COMBATANT,
    STRUCTURE_BUILDER,
    OTHER
}

public partial class UnitInfo : Resource
{
    [Export] public string unitName;
    [Export] public string unitDescription;
    [Export] public float energyCost, metalCost;
    [Export] public Texture2D iconTex;
    [Export] public UnitInfoType unitInfoType;

    public UnitInfo() : this("","",10,10,null, UnitInfoType.OTHER) { }

    public UnitInfo(string unitName, string unitDescription, float energyCost, float metalCost, Texture2D iconTex, UnitInfoType unitInfoType)
    {
        this.unitName = unitName;
        this.unitDescription = unitDescription;
        this.energyCost = energyCost;
        this.metalCost = metalCost;
        this.iconTex = iconTex;
        this.unitInfoType = unitInfoType;
    }

}

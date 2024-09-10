using Godot;
using System;

//Determines the type of construction. Will be used to help dictate whether a building placement is valid
//e.g. Network cannot be near another network, all other buildings do, and miners need to be placed specifically on a node
public enum ConstructType
{
    NETWORK,
    MINER,
    BUILDING,
    WALL,
    UNIT
}

public partial class ConstructInfo : Resource
{
    [Export] public PackedScene objectToSpawn; //The blueprint to create when starting construction. NOT the actual unit/building itself
    [Export] public ConstructType type;
    [Export] public Vector2 objectGridSize; //Used for buildings, will be 0,0 for units
    [Export] public UnitInfo unitInfo;

    public ConstructInfo() : this(null, ConstructType.UNIT, Vector2.Zero, null) { }

    public ConstructInfo(PackedScene objectToSpawn, ConstructType type, Vector2 objectGridSize, UnitInfo unitInfo)
    {
        this.objectToSpawn = objectToSpawn;
        this.type = type;
        this.objectGridSize = objectGridSize;
        this.unitInfo = unitInfo;
    }
}

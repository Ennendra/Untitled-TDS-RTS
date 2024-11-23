using Godot;
using System;

//An enum linked to the minimap markers that all relevant objects will use

public enum MinimapMarkerTag
{
    PLAYER,
    ALLYUNIT,
    ALLYBUILDING,
    ENEMYUNIT,
    ENEMYBUILDING,
    METALNODE
}


public partial class MinimapMarkerComponent : Node2D
{
	//A tag to determine what is drawn on the minimap
	[Export] public MinimapMarkerTag markerTag = MinimapMarkerTag.PLAYER;

    //Determines what factions can 'see' this object
    public bool[] spottedByFaction = new bool[5] { false, false, false, false, false };
}

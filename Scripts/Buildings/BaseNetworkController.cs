using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using Godot.Collections;
using System.Linq;

public partial class BaseNetworkController : Area2D
{
    public bool isNetworkOnline = true;
    [Export] FactionComponent factionComponent;

    public int GetNetworkFaction()
    {
        return factionComponent.faction;
    }

    public void OnBodyEntered(Node2D body)
    {
        //check that the collision is a building (it could otherwise be a blueprint, which we want to ignore)
        if (body.IsInGroup("Building")) {
            BuildingParent buildingCast = (BuildingParent)body;

            if (buildingCast.GetFactionComponent().faction == GetNetworkFaction())
            {
                buildingCast.AddToNetwork(this);
            }
        }

        
    }

    public void OnBodyExited(Node2D body)
    {
        //check that the collision is a building (it could otherwise be a blueprint, which we want to ignore)
        if (body.IsInGroup("Building"))
        {
            BuildingParent buildingCast = (BuildingParent)body;

            if (buildingCast.GetFactionComponent().faction == GetNetworkFaction())
            {
                buildingCast.RemoveFromNetwork(this);
            }
        }
    }
}
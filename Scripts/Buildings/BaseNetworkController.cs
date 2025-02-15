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
    List<BuildingParent> buildingsInNetwork = new();

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
                buildingsInNetwork.Add(buildingCast);
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
                buildingsInNetwork.Remove(buildingCast);
                buildingCast.RemoveFromNetwork(this);
            }
        }
    }

    //Runs when the hub is removed: Make sure all buildings no longer register it as an active network
    public override void _ExitTree()
    {
        base._ExitTree();

        foreach (var building in buildingsInNetwork)
        {
            building.RemoveFromNetwork(this);
        }
    }
}
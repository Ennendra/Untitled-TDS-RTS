using Godot;
using System;

public partial class BuildingNetworkHub : BuildingParent
{
    //bool isNetworkInitialized = false;
    BaseNetworkController networkController;

    public override void _Ready()
    {
        base._Ready();

        networkController = GetNode<BaseNetworkController>("NetworkController");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
    }
}

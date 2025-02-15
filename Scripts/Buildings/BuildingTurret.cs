using Godot;
using System;

public partial class BuildingTurret : BuildingParent
{
    public override void _Process(double delta)
    {
        base._Process(delta);

        //Disable the turret AI if the turret itself is offline
        if (IsInstanceValid(aiComponent))
        {
            aiComponent.isAIActive = IsBuildingOnline();
        }
    }
}

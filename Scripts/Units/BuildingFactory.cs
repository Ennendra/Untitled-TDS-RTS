using Godot;
using System;

public partial class BuildingFactory : BuildingParent
{
    public override void ProcessBuildingOperation(double delta)
    {
        base.ProcessBuildingOperation(delta);

        if (IsInstanceValid(factoryComponent))
        {   
            if (factoryComponent.GetBuildQueue().Count > 0)
            {
                constructorComponent.factoryTarget = factoryComponent;
                constructorComponent.isActive = true;
            }
            else
            {
                constructorComponent.isActive = false;
            }
        }
    }
}

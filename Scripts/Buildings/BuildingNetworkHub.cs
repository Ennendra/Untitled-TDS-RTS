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

        if (!IsBuildingOnline()) { constructorComponent.isActive = false; }
    }


    public void GenerateUnitScaffold(int index)
    {
        //instantiate the buildable unit on the offset point
        //Set the constructor target to this new item
        //activate the constructor


        //place the blueprint
        //BlueprintParent newUnit = (BlueprintParent)buildableUnits[index].objectToSpawn.Instantiate();
        //GetTree().CurrentScene.AddChild(newUnit);
        //newUnit.GlobalPosition = GlobalPosition + buildCenterOffset;
        //newUnit.SetNewFaction(factionComponent.faction);
        //newUnit.GetDamageComponent().SetHealthPercentage(1);

        //constructorComponent.blueprintTarget = newUnit;
        //constructorComponent.isActive = true;

        //currentBuild = newUnit;
    }

    public void CancelConstruction()
    {

    }

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

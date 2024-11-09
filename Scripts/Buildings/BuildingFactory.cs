using Godot;
using System;
using System.Collections.Generic;

public partial class BuildingFactory : BuildingParent
{
	//Set as the blueprints for said units. These will be instantiated
	[Export] ConstructInfo[] buildableUnits;
	[Export] Vector2 buildCenterOffset;

	BlueprintParent currentBuild;
	List<int> buildIndexQueue = new();
	bool isLooping = false; //A setting that can be toggled. If true, instead of removing a build item from the queue when finished, it will instead be placed at the end of the queue instead

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
	}

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }

	public void GenerateUnitScaffold(int index)
	{
        //instantiate the buildable unit on the offset point
        //Set the constructor target to this new item
        //activate the constructor


        //place the blueprint
        BlueprintParent newUnit = (BlueprintParent)buildableUnits[index].objectToSpawn.Instantiate();
        GetTree().CurrentScene.AddChild(newUnit);
        newUnit.GlobalPosition = GlobalPosition+buildCenterOffset;
        newUnit.SetNewFaction(factionComponent.faction);
        newUnit.GetDamageComponent().SetHealthPercentage(1);

		constructorComponent.blueprintTarget = newUnit;
		constructorComponent.isActive = true;

		currentBuild = newUnit;
    }

	public void CancelConstruction()
	{

	}
}

using Godot;
using System;
using System.Collections.Generic;

public partial class MainAIController : Node2D
{
	[Export] public int AIFaction = 2;
	//All the units and buildings that this team can control
	public List<BuildingParent> buildingsInTeam = new();
	public List<UnitParent> unitsInTeam = new();

	float AITickTimer = 0;
	float AISleepTimer = 0;
	[Export] float SleepTimeDuration = 180;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	//Adds units to this AI controllers list for use.
	//Special code strings may be added to set the result in a particular way (e.g. setting a set as part of a defensive group that should be replenished)
	public virtual void AddUnitsInArea(Rect2 rectArea, string specialCode)
	{
		PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;

        //Set up the shape and position of the rectangle area
        Vector2 rectangleCenter = rectArea.Position + (rectArea.Size / 2);
		GD.Print(rectangleCenter);
        RectangleShape2D shape = new();
		shape.Size = rectArea.Size;

        //Set the casting shape
        PhysicsShapeQueryParameters2D areaCast = new();
        areaCast.CollideWithAreas = true; //set collide with areas to true, so it will register building areas
        areaCast.Shape = shape;
        areaCast.Transform = new Transform2D(0, rectangleCenter);
        areaCast.CollisionMask = GetAlliedCollisionMask(); //The collision layers for buildings and unit physics

        //Run the cast
        List<FactionComponent> selectionHits = new();
        var collisionResult = spaceState.IntersectShape(areaCast, 100);
        if (collisionResult.Count > 0)
		{
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent newComponent = (FactionComponent)collidedObject;
                    selectionHits.Add(newComponent);
                }
            }
        }

		//Organise all hits into units and buildings
		foreach (FactionComponent hit in selectionHits)
		{
			if (hit.GetParent().IsInGroup("Unit"))
			{
				UnitParent newUnit = hit.GetParent<UnitParent>();
				AddUnit(newUnit);
			}
			else if (hit.GetParent().IsInGroup("Building"))
            {
				BuildingParent newBuilding = hit.GetParent<BuildingParent>();
				AddBuilding(newBuilding);
            }
        }

        //Special string code functionality will be added afterwards in the level-specific controller scripts
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		AITickTimer += (float)delta;
		AISleepTimer += (float)delta;
		if (AITickTimer > 0.5f && AISleepTimer > SleepTimeDuration) 
		{
			ProcessAITick();
			AITickTimer = 0;
		}
	}

	//Runs the actions that the AI wants to take
	//This will be set in separate child scripts
	public virtual void ProcessAITick()
	{

	}

	//List Manipulation functions
	public void AddUnit(UnitParent unit)
	{
		unitsInTeam.Add(unit);
	}
	public void RemoveUnit(UnitParent unit)
	{
		unitsInTeam.Remove(unit);
	}
	public void AddBuilding(BuildingParent building)
	{
		buildingsInTeam.Add(building);
	}
	public void RemoveBuilding(BuildingParent building)
	{
		buildingsInTeam.Remove(building);
	}

	//Finding a factory that is free to build the item supplied. Returns null if no factories are found
	public BuildingParent FindIdleFactory(ConstructInfo unitToBuild)
	{
		return null;
	}

    //Functions to quickly access the collision masks for allied or enemy units. e.g. Faction 1 would mean layer 5 for allied and layer 6-7 for enemy
    public uint GetAlliedCollisionMask()
    {
        switch (AIFaction)
        {
            case 1: return 16;
            case 2: return 32;
            case 3: return 64;
        }

        GD.Print("Error with AI controller faction check");
        return 0;
    }
    public uint GetEnemyCollisionMask()
    {
        switch (AIFaction)
        {
            case 1: return 96;
            case 2: return 80;
            case 3: return 48;
        }
        GD.Print("Error with AI controller faction check");
        return 0;
    }
}

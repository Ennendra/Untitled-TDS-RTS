using Godot;
using System;

public partial class ToolBuilder : ToolParent
{
	BlueprintParent toolTarget;
	FactionComponent toolFactionTarget;
	[Export] GpuParticles2D particles;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (particles != null) { particles.Restart(); }
		
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (isActive)
		{
			if (particles != null) 
			{
				particles.GlobalRotation = GlobalRotation;
				particles.Emitting = true;
			}
		}
		else
		{
            if (particles != null) { particles.Emitting = false; }
        }
	}

	public override void UseTool()
	{
		//reset the values, in case the tool is being used but the target is different or no longer available
        StopTool();

        Vector2 targetPoint = GetGlobalMousePosition();
		//abort the check if not firing in range of the tool;
		//if (targetPoint.DistanceTo(GlobalPosition) > toolRange) return;
		//Check for any blueprint collisions at the mouse point
		var spaceState = GetWorld2D().DirectSpaceState;
		PhysicsPointQueryParameters2D pointCast = new();
		pointCast.Position = targetPoint;
		pointCast.CollisionMask = 131072; //The collision mask for blueprints (layer 18)
		pointCast.CollideWithAreas = true;

		var collisionResult = spaceState.IntersectPoint(pointCast);

		//Check if the collision result (if any) is a blueprint that is of the same faction as this tool's user
		if (collisionResult.Count > 0)
		{
			foreach (var collision in collisionResult)
			{
				Variant collidedObject;
				bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

				if (colliderCheck)
				{
					BlueprintParent targetBlueprint = (BlueprintParent)collidedObject;
					if (targetBlueprint.GlobalPosition.DistanceTo(GlobalPosition)<=(toolRange + targetBlueprint.GetBuildingRadius()) && targetBlueprint.GetCurrentFaction() == factionComponent.faction)
					{
						GD.Print(GlobalPosition);
						isActive = true;
                        constructorComponent.blueprintTarget = targetBlueprint;
                        constructorComponent.isActive = true;
						return;
                    }
					
				}
			}
        }

		//No blueprint results, so we shall do another check for other items
        //Do another point check at the same place, but instead checking the faction component
        pointCast.CollisionMask = GetAlliedFactionLayer(); 

        collisionResult = spaceState.IntersectPoint(pointCast);
        if (collisionResult.Count > 0)
        {
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

				//Check that the target is a faction component
                if (colliderCheck)
                {
                    FactionComponent targetComponent = (FactionComponent)collidedObject;
                    if (targetComponent.GlobalPosition.DistanceTo(GlobalPosition) <= (toolRange))
                    {
						//Check if the target is a factory that is currently building a unit
						if (targetComponent.GetFactoryComponent() != null)
						{
							if (targetComponent.GetFactoryComponent().GetBuildQueue().Count > 0)
							{
                                isActive = true;
                                constructorComponent.factoryTarget = targetComponent.GetFactoryComponent();
                                constructorComponent.isActive = true;
                                return;
                            }
						}
                        //If the above check for factory building returned false, we will instead check to heal instead.
						if (targetComponent.GetDamageComponent().GetCurrentHealthPercent() < 100)
						{
                            isActive = true;
                            constructorComponent.miscUnitTarget = targetComponent;
                            constructorComponent.isActive = true;
                            return;
                        }
                    }

                }
            }
        }
    }
    public override void StopTool()
    {
		isActive = false;
		constructorComponent.isActive = false;
		constructorComponent.blueprintTarget = null;
    }
}

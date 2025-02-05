using Godot;
using System;

enum ToolTargetType 
{
	NONE,
	ALLY,
	ENEMY
}


public partial class ToolBuilder : ToolParent
{
	//possibly deprecated
	BlueprintParent toolTarget;

	BuildingQueue buildQueueTarget;
	FactionComponent toolFactionTarget;
	ToolTargetType toolTargetType;

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


		//Reset the contructor component targets, in case the target changes or dies
		ResetConstructorComponent();
		//Is the tool enabled?
		if (isActive)
		{
            //Do we have a defined and living target?
            if (IsInstanceValid(toolFactionTarget))
            {
				//Check if we are close enough to work on the target
				if (GlobalPosition.DistanceTo(toolFactionTarget.GlobalPosition) <= toolRange)
				{
                    //Check if the target is damaged, and use the tool to repair them if so
                    if (toolFactionTarget.GetDamageComponent().GetCurrentHealthPercent() < 100)
                    {
                        constructorComponent.miscUnitTarget = toolFactionTarget;
                        constructorComponent.isActive = true;
                    }
                    else
                    {
                        //No repairs needed. Instead, check if the target is a factory that is currently building a unit
                        if (toolFactionTarget.GetFactoryComponent() != null)
                        {
                            if (toolFactionTarget.GetFactoryComponent().GetBuildQueue().Count > 0)
                            {
                                constructorComponent.factoryTarget = toolFactionTarget.GetFactoryComponent();
                                constructorComponent.isActive = true;
                            }
                        }
                    }
                }
				else //Out of range, reset
				{
					ResetToolTarget();
				}
            }
            else //No direct target, see about working on the build queue instead
            {
				if (buildQueueTarget!=null)
				{
                    //Only supply if the building still requires supply
                    if (buildQueueTarget.totalSupplied < buildQueueTarget.totalCost)
                    {
                        constructorComponent.buildQueueTarget = buildQueueTarget;
                        constructorComponent.isActive = true;
                    }
                }
				
            }
        }
		else //tool is disabled, reset
		{
			ResetToolTarget();
            //if (particles != null) { particles.Emitting = false; }
        }
		
	}

	public void SetFactionTarget(FactionComponent toolFactionTarget)
	{
		this.toolFactionTarget = toolFactionTarget;
    }
	public void ResetToolTarget()
	{
		toolFactionTarget = null;
	}

	public override void UseTool()
	{
		//Reset the tool target value before attempting to find another one
		ResetToolTarget();

        Vector2 targetPoint = GetGlobalMousePosition();
		//abort the check if not firing in range of the tool;
		//if (targetPoint.DistanceTo(GlobalPosition) > toolRange) return;
		//Check for any blueprint collisions at the mouse point
		var spaceState = GetWorld2D().DirectSpaceState;
		PhysicsPointQueryParameters2D pointCast = new();
		pointCast.Position = targetPoint;
		pointCast.CollideWithAreas = true;

        //Do another point check at the same place, but instead checking the faction component
        pointCast.CollisionMask = GetAlliedFactionLayer();
        var collisionResult = spaceState.IntersectPoint(pointCast);
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
						SetFactionTarget(targetComponent);
                    }
                }
            }
        }
    }

	public void SetBuildQueueTarget(BuildingQueue target)
	{
		buildQueueTarget = target;
    }

	public bool ToggleActive()
	{
		isActive = !isActive;
		return isActive;
	}


	public void ResetConstructorComponent()
	{
        constructorComponent.isActive = false;
		constructorComponent.miscUnitTarget = null;
        constructorComponent.factoryTarget = null;
        constructorComponent.blueprintTarget = null;
    }
}

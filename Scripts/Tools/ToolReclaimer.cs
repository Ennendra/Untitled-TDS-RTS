using Godot;
using System;

public partial class ToolReclaimer : ToolParent
{
    DamageComponent toolTarget;
    [Export] DamageComponent componentToIgnore; //used to make sure the player doesn't reclaim themselves
    [Export] GpuParticles2D particles;

    [Export] float maxReclaimRate = 5;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        //Reset the target in case the target changes or dies
        resourceComponent.SetNewGenerationValues(new float[2] { 0, 0 });

        if (isActive)
        {
            if (IsInstanceValid(toolTarget))
            {
                if (toolTarget.GlobalPosition.DistanceTo(GlobalPosition) <= (toolRange + toolTarget.toolRangeGrace))
                {
                    //Set the resource generation from the target's cost values
                    float[] ratio = toolTarget.GetEnergyMetalRatio();
                    float[] resourceReclaimRate = new float[2] { maxReclaimRate * ratio[0], maxReclaimRate * ratio[1] };
                    resourceComponent.SetNewGenerationValues(resourceReclaimRate);

                    //Set the damage the component we are reclaiming takes
                    float targetDamagePerSecond = 0;
                    if (resourceComponent.GenEnergy > 0) //If we are generating energy from the reclaiming (ie. the target has an energy reclaim value)
                    {
                        float healthToResourceRatio = toolTarget.maxHealth / toolTarget.energyReclaimValue;
                        targetDamagePerSecond = resourceComponent.GenEnergy * healthToResourceRatio;
                    }
                    else //If no energy to reclaim, there must be metal instead
                    {
                        float healthToResourceRatio = toolTarget.maxHealth / toolTarget.metalReclaimValue;
                        targetDamagePerSecond = resourceComponent.GenMetal * healthToResourceRatio;
                    }
                    toolTarget.TakeDamage(targetDamagePerSecond * (float)delta, DamageType.RECLAIMING);

                    //Emit particles from the tool if applicable
                    if (particles != null)
                    {
                        particles.GlobalRotation = GlobalRotation;
                        particles.Emitting = true;
                    }
                }
                else { ResetTarget(); } //Target out of range, reset
            }
            else { ResetTarget(); } //Target no longer valid (ie, dead), reset
        }
        else //Tool disabled, reset
        {
            ResetTarget();
        }
    }

    //Reclaimer Usetool: Run a collision point for damage components
    //If one is found, check that the target is in range.
    //If it is, mark it as the current active target for the tool and determine how much energy and metal it will reclaim
    //Note: the "damage" dealt via reclaiming will be dealt in the _process function
    public override void UseTool()
    {
        //Run a collision point, using all damagecomponent layers as a mask

        //If hitting something, set resourceComponent values to a new amount (check ratios on damagecomponent values)
        //Have the entity take reclaim damage porportional to resources taken

        //reset the values, in case the tool is being used but the target is different or no longer available
        StopTool();

        Vector2 targetPoint = GetGlobalMousePosition();
        //abort the check if not firing in range of the tool;
        //if (targetPoint.DistanceTo(GlobalPosition) > toolRange) return;
        //Check for any blueprint collisions at the mouse point
        var spaceState = GetWorld2D().DirectSpaceState;
        PhysicsPointQueryParameters2D pointCast = new();
        pointCast.Position = targetPoint;
        pointCast.CollisionMask = GetAlliedFactionLayer() + 4; //The collision mask for allied components and 'wreckage'
        pointCast.CollideWithAreas = true;

        var collisionResult = spaceState.IntersectPoint(pointCast);

        if (collisionResult.Count > 0)
        {
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    DamageComponent targetComponent = (DamageComponent)collidedObject;
                    if (targetComponent.GlobalPosition.DistanceTo(GlobalPosition) <= (toolRange + targetComponent.toolRangeGrace) && targetComponent != componentToIgnore)
                    {
                        isActive = true;
                        toolTarget = targetComponent;
                    }

                }
            }
        }
    }

    public void ResetTarget()
    {
        toolTarget = null;
        resourceComponent.SetNewGenerationValues(new float[2] { 0, 0 });

        //Disable particles
        if (particles != null) { particles.Emitting = false; }
    }
}

using Godot;
using System;

public partial class UnitParent : CombatantParent
{

    public Node2D motherFactory;
    [Export] CollisionShape2D physicsCollider;

    //Setting an initial direction of the unit when spawned
    public void SetInitialDirection(float directionDegrees)
    {
        movementComponent.SetMoveDirection(directionDegrees);
        aimComponent.SetCurrentAimDirection(directionDegrees);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (motherFactory != null)
        {
            if (GlobalPosition.DistanceTo(motherFactory.GlobalPosition) > 90)
            {
                physicsCollider.Disabled = false;
            }
        }
        else
        {
            physicsCollider.Disabled = false;
        }
    }


    //Defining functions for moving and attacking when they are spawned in via factories
    public void SetMoveOrder(Vector2 targetPosition)
    {
        aiComponent.SetNewMoveOrder(targetPosition);
    }
    public void SetAttackOrder(FactionComponent target)
    {
        aiComponent.SetNewAttackOrder(target);
    }
}

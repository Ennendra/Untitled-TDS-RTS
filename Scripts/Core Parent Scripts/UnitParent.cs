using Godot;
using System;

public partial class UnitParent : CombatantParent
{

    //Setting an initial direction of the unit when spawned
    public void SetInitialDirection(float directionDegrees)
    {
        movementComponent.SetMoveDirection(directionDegrees);
        aimComponent.SetCurrentAimDirection(directionDegrees);
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

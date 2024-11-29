using Godot;
using System;

public enum NavigationCheckResult
{
	CONTINUE,
	NAVPOINTREACHED,
	PATHCOMPLETED,
	PATHINVALID
}

//Todo:
//Set a nav ragion (made in the level?)
//
public partial class PathfindingComponent : Node2D
{
	Globals globals;

	//A reference to the movement component. Used to determine the unit's movement type, which determines which navigation mesh to use.
	[Export] MovementComponent movement;

	//The currently assigned path.
	public Vector2[] currentPath;
	public int currentPathIndex;
	float pathDesiredDistance = 50;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		globals = GetNode<Globals>("/root/Globals");
	}

	public bool SetNewPath(Vector2 newTargetPosition)
	{
		if (!IsInstanceValid(movement)) 
		{
			GD.Print("Unit's pathing component does not have movement assigned!: " + GetParent().Name);
			return false;
		}

		Rid navToUse;
		//Determinine which navigation map to use
		switch (movement.GetMovementType()) 
		{ 
			case MovementType.GROUND:
                navToUse = globals.navigationAreaGround.GetNavigationMap();
                break;
			case MovementType.HOVER:
                navToUse = globals.navigationAreaHover.GetNavigationMap();
                break;
            default:
				GD.Print("Error in pathcomponent with bad movement type");
				navToUse = globals.navigationAreaGround.GetNavigationMap();
				break;
		}


		Vector2 safeDestinationPoint = NavigationServer2D.MapGetClosestPoint(navToUse, newTargetPosition);
        currentPath = NavigationServer2D.MapGetPath(navToUse, GlobalPosition, safeDestinationPoint, true);

        if (!currentPath.IsEmpty())
		{
			currentPathIndex = 0;
            return true;
        }

		return false;
	}

	public Vector2 GetNextPointPosition()
	{
        if (!currentPath.IsEmpty())
		{
			return currentPath[currentPathIndex];
		}
		return GlobalPosition;
    }
	public Vector2 GetFinalPointPosition()
	{
        if (!currentPath.IsEmpty())
        {
            return currentPath[currentPath.Length-1];
        }
        return GlobalPosition;
    }

	public NavigationCheckResult CheckIfNavPointReached()
	{
        if (!currentPath.IsEmpty())
		{
			if (GlobalPosition.DistanceTo(currentPath[currentPathIndex]) < pathDesiredDistance)
			{
				currentPathIndex++;
				if (currentPathIndex > currentPath.Length - 1) 
				{
					currentPath = null;
					currentPathIndex = 0;
					return NavigationCheckResult.PATHCOMPLETED;
				}
				return NavigationCheckResult.NAVPOINTREACHED;
			}
			return NavigationCheckResult.CONTINUE;
		}
		return NavigationCheckResult.PATHINVALID;
    }

	//Pathfinding Signals
	public void OnPathPointReached()
	{

	}
	public void OnNavigationFinished()
	{

	}
}

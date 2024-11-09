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
	//The battle area that this node can move in
	//NavigationRegion2D navRegion;

	//The currently assigned path.
	public Vector2[] currentPath;
	public int currentPathIndex;
	float pathDesiredDistance = 20;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		globals = GetNode<Globals>("/root/Globals");

		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public bool SetNewPath(Vector2 newTargetPosition)
	{
		Vector2 safeDestinationPoint = NavigationServer2D.MapGetClosestPoint(GetWorld2D().NavigationMap, newTargetPosition);
        currentPath = NavigationServer2D.MapGetPath(GetWorld2D().NavigationMap, GlobalPosition, safeDestinationPoint, true);
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

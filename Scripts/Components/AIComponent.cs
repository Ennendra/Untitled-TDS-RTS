using Godot;
using System;
using System.Collections.Generic;
public enum AIUnitState
{
    IDLE,
	MOVE,
	ATTACK,
	ATTACKMOVE,
	GUARD,
	HOLD
}

public partial class AIComponent : Node2D
{
	//Components
	[Export] PathfindingComponent pathComponent;
    [Export] FactionComponent factionComponent;
	[Export] MovementComponent movementComponent;
	[Export] AimingComponent aimComponent;

	float standardFireRange;
	//overrides the standardFireRange from being the equipped weapon's range. if 0, no override is given.
	//May be used for artillery units, so they don't go chasing enemies too readily.
	[Export] float fireRangeOverride = 0;

    //tells us whether the AI is 'active', used to control turrets when they are offline
    public bool isAIActive = true;

    public AIUnitState unitState { get; protected set; } = AIUnitState.IDLE;

	//Targets for orders
	//order target can include enemies for attack, or allies for guarding
	//target position can include move position or hold position
	FactionComponent orderTarget;
    Vector2 orderTargetPosition;

    //Which target we are firing/facing at (can be a different enemy if normal target isn't in range or we are idle
    FactionComponent fireTarget;
    //Tells us whether the unit has (or had) a target to attack. When the target dies, this will stay true for one process tick and will force another scan immediately
    bool hasFireTarget = false;
	float distanceToFireTarget { get => GlobalPosition.DistanceTo(fireTarget.GlobalPosition); }
	//An incrementing timer for scanning for targets, and the gap between each scan
	float scanTimer = 0, timeToScan = 0.5f;
	float pathTimer = 0, timeToNewPath = 1.0f;

    public override void _Ready()
    {
        base._Ready();
		if (fireRangeOverride == 0)
			standardFireRange = aimComponent.equippedWeapon.range;
		else
			standardFireRange = fireRangeOverride;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		base._Process(delta);

        //Process only if the game isn't paused
        if (!GetTree().Paused && isAIActive)
        {
		    //Set aim direction towards a target to fire at, or towards forward movement if none is available
		    bool isFireTargetValid = CheckFireTarget();
		    if (isFireTargetValid)
		    {
                //TrackFireTarget(delta);
            }
		    else //Fire target is not valid, set aiming towards movement instead and increment scan timer
		    {
                if (!IsUnitStatic()) { aimComponent.SetTargetDirection(movementComponent.GetTargetDirection()); }
                scanTimer += (float)delta;
            }
        }

    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        //Process only if the game isn't paused
        if (!GetTree().Paused && isAIActive)
        {
            pathTimer += (float)delta;
            //Process orders based on current order state
            switch (unitState)
            {
                case AIUnitState.IDLE:
                    ProcessOrderIdle(delta);
                    break;
                case AIUnitState.MOVE:
                    ProcessOrderMove(delta);
                    break;
                case AIUnitState.ATTACK:
                    ProcessOrderAttack(delta);
                    break;
                case AIUnitState.ATTACKMOVE:
                    ProcessOrderAttackMove(delta);
                    break;
                case AIUnitState.GUARD:
                    ProcessOrderGuard(delta);
                    break;
                case AIUnitState.HOLD:
                    ProcessOrderHold(delta);
                    break;

            }
        }
    }

    //AI Order Functions --------
    public void ProcessOrderIdle(double delta)
	{
		//Set the ranges for movement and pursuit. Set the max pursuit range to the weapon range if no movement component is found (ie. it is a static emplacement)
		float minimumMovementRange = standardFireRange - 100.0f;
		float stopTargetRange = standardFireRange;
        if (!IsUnitStatic()) { stopTargetRange += 200.0f; }

		if (movementComponent == null)
		{
            stopTargetRange = standardFireRange;
        }

		//If there is a target to fire at, move towards it and fire unless it goes out of range
        if (IsInstanceValid(fireTarget))
		{
			//Movement
			if (!IsUnitStatic())
			{
				if (pathTimer > timeToNewPath)
				{
					pathComponent.SetNewPath(fireTarget.GlobalPosition);
					pathTimer = 0;
				}
				if (GlobalPosition.DistanceTo(fireTarget.GlobalPosition) > minimumMovementRange) 
				{
                    MoveTowardsTargetPosition(pathComponent.GetNextPointPosition(), delta);
                    pathComponent.CheckIfNavPointReached();
                }
				else
				{
					movementComponent.Decelerate(delta);
				}
                //MoveTowardsTargetRange(fireTarget, minimumMovementRange, delta);
            }

            //Firing
            TrackFireTarget(delta);
            //Stop targeting the firetarget if beyond range
            CancelTargetIfOutOfRange(stopTargetRange);
		}
		else
		{
            if (!IsUnitStatic()) { movementComponent.Decelerate(delta); }
            ScanForTarget(GlobalPosition, standardFireRange + 100.0f, hasFireTarget);
            hasFireTarget = false;
        }
	}
	public void ProcessOrderMove(double delta)
	{
        //Aim towards an enemy if within range
        float stopTargetRange = standardFireRange;
        if (IsInstanceValid(fireTarget))
        {
            //Firing
            TrackFireTarget(delta);
            //Stop targeting the firetarget if beyond range
            CancelTargetIfOutOfRange(stopTargetRange);
        }
        else
        {
            ScanForTarget(GlobalPosition, standardFireRange, hasFireTarget);
            hasFireTarget = false;
        }

        //move along the path.
        MoveTowardsTargetPosition(pathComponent.GetNextPointPosition(), delta);
        NavigationCheckResult result = pathComponent.CheckIfNavPointReached();
        //Have we reached our location?
        if (result == NavigationCheckResult.PATHCOMPLETED)
        {
            unitState = AIUnitState.IDLE;
        }
    }
	public void ProcessOrderAttack(double delta)
	{
        float minimumMovementRange = standardFireRange - 100.0f;
        float stopTargetRange = standardFireRange;
        if (!IsUnitStatic()) { stopTargetRange += 200.0f; }

        //Pursue the target until they die
        if (IsInstanceValid(orderTarget))
		{
            //Recalculate a path if the target has moved far enough away from the position
			if (orderTarget.GlobalPosition.DistanceTo(pathComponent.GetFinalPointPosition()) > 250)
			{
				pathComponent.SetNewPath(orderTarget.GlobalPosition);
			}
            //Path movement
            if (GlobalPosition.DistanceTo(orderTarget.GlobalPosition) > minimumMovementRange)
            {
                MoveTowardsTargetPosition(pathComponent.GetNextPointPosition(), delta);
                pathComponent.CheckIfNavPointReached();
            }
            else
            {
                movementComponent.Decelerate(delta);
            }
            //Firing
            fireTarget = orderTarget;
            TrackFireTarget(delta);
        }
		else //Target down, reset to idle
		{
			unitState = AIUnitState.IDLE;
		}
    }
	public void ProcessOrderAttackMove(double delta) 
	{
        float stopTargetRange = standardFireRange;

        if (IsInstanceValid(fireTarget))
        {
			if (!IsUnitStatic()) { movementComponent.Decelerate(delta); }
            //Firing
            TrackFireTarget(delta);
            //Stop targeting the firetarget if beyond range
            CancelTargetIfOutOfRange(stopTargetRange);
        }
        else
        {
            //Movement
            if (!IsUnitStatic())
            {
                if (pathTimer > timeToNewPath)
                {
                    pathComponent.SetNewPath(orderTargetPosition);
                    pathTimer = 0;
                }
				MoveTowardsTargetPosition(pathComponent.GetNextPointPosition(), delta);
				pathComponent.CheckIfNavPointReached();

            }

            if (GlobalPosition.DistanceTo(orderTargetPosition) < 50.0f)
			{
				unitState = AIUnitState.IDLE;
			}
            ScanForTarget(GlobalPosition, standardFireRange, hasFireTarget);
            hasFireTarget = false;
        }
    }
	public void ProcessOrderGuard(double delta)
	{
		//This stop range will be centered on the target to defend
		float stopTargetRange = standardFireRange + 100.0f;
		float minimumMovementFireRange = standardFireRange - 100.0f;
		float preferredGuardDistance = 150.0f;

		//Check that the target to defend is still alive, otherwise revert to idle
		if (IsInstanceValid(orderTarget))
		{
            if (IsInstanceValid(fireTarget))
            {
                if (!IsUnitStatic()) 
				{
                    if (pathTimer > timeToNewPath)
                    {
                        pathComponent.SetNewPath(fireTarget.GlobalPosition);
                        pathTimer = 0;
                    }
                    if (GlobalPosition.DistanceTo(fireTarget.GlobalPosition) > minimumMovementFireRange)
                    {
                        MoveTowardsTargetPosition(pathComponent.GetNextPointPosition(), delta);
                        pathComponent.CheckIfNavPointReached();
                    }
                    else
                    {
                        movementComponent.Decelerate(delta);
                    }
                }
                //Firing
                TrackFireTarget(delta);
                //Stop targeting the firetarget if beyond range
                if (orderTarget.GlobalPosition.DistanceTo(fireTarget.GlobalPosition) > stopTargetRange)
                {
                    fireTarget = null;
                }
            }
            else
            {
                ScanForTarget(orderTarget.GlobalPosition, standardFireRange, false);
                if (GlobalPosition.DistanceTo(orderTarget.GlobalPosition) > preferredGuardDistance)
                {
                    if (!IsUnitStatic())
                    {
                        if (pathTimer > timeToNewPath)
                        {
                            pathComponent.SetNewPath(orderTarget.GlobalPosition);
                            pathTimer = 0;
                        }
                        MoveTowardsTargetPosition(pathComponent.GetNextPointPosition(), delta);
                        pathComponent.CheckIfNavPointReached();
                    }
                }
                else
                {
                    if (!IsUnitStatic()) { movementComponent.Decelerate(delta); }
                }
            }
        }
		else //guard target dead
		{
			unitState = AIUnitState.IDLE;
		}
        
    }
	public void ProcessOrderHold(double delta)
	{
        float stopTargetRange = standardFireRange;
        //Decelerate, as the object is to strictly not move
        if (!IsUnitStatic())
        {
            movementComponent.Decelerate(delta);
        }

        if (IsInstanceValid(fireTarget))
        {
            //Firing
            TrackFireTarget(delta);
            //Stop targeting the firetarget if beyond range
            CancelTargetIfOutOfRange(stopTargetRange);
        }
        else
        {
            ScanForTarget(GlobalPosition, standardFireRange, false);
        }
    }

	//First checks whether the time since last tick has been long enough
	//then, pings for enemies that aren't part of this unit's faction and marks them as a target.
	public void ScanForTarget(Vector2 centerPosition, float radius, bool forceScan)
	{
		if (scanTimer > timeToScan || forceScan)
		{
			//Init the space state
			var spaceState = GetWorld2D().DirectSpaceState;
			//Create the collision shape for the scan
			CircleShape2D sightCheckShape = new();
			sightCheckShape.Radius = radius;

			//Create the cast query
			PhysicsShapeQueryParameters2D areaCast = new();
			areaCast.CollideWithAreas = true;
			areaCast.Shape = sightCheckShape;
			areaCast.Transform = new Transform2D(0, centerPosition);
			//Get the collision layers for the factions that this one is *not* a part of
			//Faction1 = 16 --- Faction2 = 32 --- Faction3 = 64
			switch (factionComponent.faction)
			{
				case 1:
					areaCast.CollisionMask = 96;
					break;
				case 2:
					areaCast.CollisionMask = 80;
					break;
				case 3:
					areaCast.CollisionMask = 48;
					break;
			}

			//Execute the check
			var collisionResult = spaceState.IntersectShape(areaCast);

			List<FactionComponent> componentCollisions = new();
			if (collisionResult.Count > 0)
			{
                PhysicsRayQueryParameters2D lineCast = new();
                lineCast.CollideWithAreas = true;
                lineCast.From = centerPosition;
                lineCast.CollisionMask = 1;


                //Generate a list of all potential targets in range
                foreach (var collision in collisionResult)
				{

                    Variant collidedObject;
					bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

					if (colliderCheck)
					{
                        FactionComponent potentialTarget = (FactionComponent)collidedObject;

                        if (potentialTarget.spottedByFaction[factionComponent.faction])
                        {
                            lineCast.To = potentialTarget.GlobalPosition;
                            var lineCollisionResult = spaceState.IntersectRay(lineCast);

                            if (lineCollisionResult.Count == 0)
                            {
                                componentCollisions.Add(potentialTarget);
                            }
                        }
						
					}
				}
			}

			//Find nearest of the targets in list and mark it as the main target (As long as it's spotted by the team)
			FactionComponent closestTarget = null;
			float closestDistance = 0;
			foreach (var targetCheck in componentCollisions)
			{
				float distanceToTarget = GlobalPosition.DistanceTo(targetCheck.GlobalPosition);
				if (distanceToTarget < closestDistance || closestTarget == null)
				{
					closestTarget = targetCheck;
					closestDistance = distanceToTarget;
				}
			}
			//Set the target to fire at as the closest target (or null if there was none)
			fireTarget = closestTarget;

			scanTimer = 0;
		}
    }

    public bool IsWithinLOS(Vector2 fromPos, Vector2 toPos)
    {
        //Init the space state
        var spaceState = GetWorld2D().DirectSpaceState;
        PhysicsRayQueryParameters2D lineCast = new();
        lineCast.CollideWithAreas = true;
        lineCast.From = fromPos;
        lineCast.To = toPos;
        lineCast.CollisionMask = 1;

        var lineCollisionResult = spaceState.IntersectRay(lineCast);

        if (lineCollisionResult.Count > 0)
        {
            return false;
        }
        return true;
    }

	//Checks that the current fire target is valid (ie, is not dead or non-existant)
	//Returns true if valid, false otherwise
	public bool CheckFireTarget()
	{
        if (IsInstanceValid(fireTarget))
        {
            //Make sure the target is still inside a tree (ie, has not been "queuefree')
            if (fireTarget.IsInsideTree())
            {
                return true;
            }
        }
        fireTarget = null;
        return false;
	}

    public void TrackFireTarget(double delta)
    {

        aimComponent.SetTargetDirection(fireTarget.GlobalPosition);
        
        //has the target moved?
        if (fireTarget.projectedPosition != fireTarget.GlobalPosition)
        {
            //Trace their movement based on projected speed and projected projectile speed
            float projectileToTargetTime = (distanceToFireTarget / aimComponent.equippedWeapon.weapon.projectileSpeed); //If the result = 1, that means it will take 1 second to reach the target
            Vector2 trackVector = fireTarget.projectedPosition - fireTarget.GlobalPosition;
            //Update the aimcomponents target position based on the track vector
            aimComponent.SetTargetDirection(fireTarget.GlobalPosition + (trackVector * projectileToTargetTime));
        }

        //If we're nearly aiming directly at the target, fire
        float angleDifference = Vector2.FromAngle(aimComponent.currentAimDirection).Dot(Vector2.FromAngle(aimComponent.targetAimDirection));
        if (angleDifference > 0.999) 
        {
            FireWeaponIfInRange();
        }
    }

    //Checks if the weapon is within range of the fire target and fires if so
    public void FireWeaponIfInRange()
	{
        hasFireTarget = true;
        if (distanceToFireTarget < standardFireRange)
        {
            
			aimComponent.FireWeapons();
        }
    }
	//Checks if the target is outside a given range and nullify the fire target if it is
	public void CancelTargetIfOutOfRange(float cancelRange)
	{
        if (distanceToFireTarget > cancelRange || !IsWithinLOS(GlobalPosition, fireTarget.GlobalPosition))
        {
            fireTarget = null;
            hasFireTarget = false;
        }
    }
	//Checks if a movement component doesn't exist (ie, the AI is a static emplacement
	public bool IsUnitStatic()
	{
		return (movementComponent == null);
	}

	//Sets movement towards a target position. Used with pathfinding
	public void MoveTowardsTargetPosition(Vector2 newPosition, double delta)
	{
		//check that the point we're moving to isn't where we're standing. This is so that there isn't unnecessary movement with pathfinding
        float angleToTarget = GlobalPosition.DirectionTo(newPosition).Angle();
        movementComponent.SetTargetDirection(angleToTarget);
		

        if (movementComponent.GetMovementType() == MovementType.GROUND)
        {
            float angleDifference = Vector2.FromAngle(movementComponent.GetCurrentDirection()).Dot(GlobalPosition.DirectionTo(newPosition));
            if (angleDifference > 0.8) //If we're within ~45 degrees of facing the target
                movementComponent.Accelerate(delta);
            else
                movementComponent.Decelerate(delta);
        }
        else if (movementComponent.GetMovementType() == MovementType.HOVER)
        {
            float currentMoveAngle = movementComponent.GetMovementVectorHover(1).Angle();
            float angleDifference = Vector2.FromAngle(currentMoveAngle).Dot(GlobalPosition.DirectionTo(newPosition));
            //If we are hovering, we don't need to worry about rotation
            if (angleDifference > 0.5 || movementComponent.GetMovementVectorHover(1).Length() <= 10) //If our current movement is close enough to the direction we want to go, or if we've slowed down enought
                movementComponent.Accelerate(delta);
            else
                movementComponent.Decelerate(delta);
        }

    }



	//Functions for setting a new order
	public void SetNewMoveOrder(Vector2 targetPosition)
	{
        if (IsUnitStatic()) { return; }
		bool pathCheck = false;
		if (IsInstanceValid(pathComponent)) { pathCheck = pathComponent.SetNewPath(targetPosition); }
        if (pathCheck)
		{
			unitState = AIUnitState.MOVE;
			orderTargetPosition = targetPosition;
		}
    }
	public void SetNewAttackOrder(FactionComponent target)
	{
		if (IsInstanceValid(pathComponent)) { pathComponent.SetNewPath(target.GlobalPosition); }
		unitState = AIUnitState.ATTACK;
		orderTarget = target;
	}
	public void SetNewGuardOrder(FactionComponent target)
	{
        if (IsUnitStatic()) { return; }
        if (IsInstanceValid(pathComponent)) { pathComponent.SetNewPath(target.GlobalPosition); }
        unitState = AIUnitState.GUARD;
        orderTarget = target;
	}
	public void SetNewAttackMoveOrder(Vector2 targetPosition)
    {
        if (IsUnitStatic()) { return; }
        bool pathCheck = false;
        if (IsInstanceValid(pathComponent)) { pathCheck = pathComponent.SetNewPath(targetPosition); }
        if (pathCheck)
        {
            unitState = AIUnitState.ATTACKMOVE;
            orderTargetPosition = targetPosition;
        }
    }
	public void SetNewHoldOrder()
	{
		unitState = AIUnitState.HOLD;
	}

	public void CancelOrder()
	{
		unitState = AIUnitState.IDLE;
	}
}

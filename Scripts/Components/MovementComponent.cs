using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

public enum MovementType
{
	GROUND,
	HOVER,
	AIR
}

public partial class MovementComponent : Area2D
{

	//The movement type (which can dictate how the unit moves and what count as obstacles
	[Export] MovementType movementType = MovementType.GROUND;
	public MovementType GetMovementType() { return movementType; }

	//Speed variables
	[Export] float maxSpeed = 60;
	float currentSpeed = 0;
	[Export] float timeToMaxSpeed = 0.5f;
	[Export] float timeToZeroSpeed = 0.5f;
    float AccelerateFactor { get => 1 / timeToMaxSpeed; }
    float DecelerateFactor { get => 1 / timeToZeroSpeed; }
    bool confirmMovement = false;
    Vector2 knockBack = Vector2.Zero;
    float knockBackFriction = 1;
    public float knockBackAmount = 2.25f;

	List<Area2D> collidingPhysicsAreas = new();

    //Sprite that will be manipulated by movement rotation if applicable
    [Export] Sprite2D legSprite;

	//Direction of movement (for ground)
	float targetDirection = 0, groundMoveDirection=0;
	//Direction of movement (for hover)
	Vector2 hoverMoveVector = new Vector2(0,0);
	//The rotation speed of the unit
	[Export] float rotationSpeedDegrees = 200.0f;
	
	//The rotation speed converted to radians for use in the _process function
	float rotationSpeed;

	//The old lerp rotation factor, will remove if confirming to use linear instead
    float rotationFactor = 2.5f;

    public override void _Ready()
    {
        base._Ready();

		knockBackAmount = maxSpeed / 120 * 2.25f;
		rotationSpeed = Mathf.DegToRad(rotationSpeedDegrees);
    }

    public override void _Process(double delta)
	{
		if (movementType == MovementType.HOVER)
		{
            if (IsInstanceValid(legSprite) && hoverMoveVector.Length() > 1.0f)
            {
				legSprite.GlobalRotation = Mathf.RotateToward(legSprite.GlobalRotation, targetDirection, rotationSpeed * (float)delta);
            }
        }
		else if (movementType == MovementType.AIR)
		{
			if (IsInstanceValid(legSprite))
			{

			}
		}
        else //ground/default
        {
			//Rotate the movement towards the target direction
            groundMoveDirection = Mathf.RotateToward(groundMoveDirection, targetDirection, rotationSpeed * (float)delta);
            if (IsInstanceValid(legSprite))
            {
                legSprite.GlobalRotation = groundMoveDirection;
            }
			
        }
		
	}
    public override void _PhysicsProcess(double delta)
    {
        Node2D parent = (Node2D)this.GetParent();

		//Process adding or removing knockback
		knockBack = knockBack.MoveToward(Vector2.Zero, knockBackFriction);
		foreach (var area in collidingPhysicsAreas)
		{
            SetKnockback(area.GlobalPosition.DirectionTo(GlobalPosition) * knockBackAmount);
        }

        //Final movements
        if (movementType == MovementType.HOVER)
        {
            Vector2 movement = GetMovementVectorHover(delta);
            movement += knockBack;
            parent.GlobalPosition += movement;
        }
        else //Ground/default
		{ 
			Vector2 movement = GetMovementVectorGround(delta);
            movement += knockBack;
            parent.GlobalPosition += movement;
        }
    }

    //Movement Functions
    public void Accelerate(double delta)
	{
        float speedAddAmount = maxSpeed * AccelerateFactor * (float)delta;
        if (movementType == MovementType.HOVER)
		{
			//Add acceleration to the movement vector, and cap the length to the maxspeed
			hoverMoveVector += Vector2.FromAngle(targetDirection) * speedAddAmount;
			if (hoverMoveVector.Length() > maxSpeed)
			{
				hoverMoveVector = hoverMoveVector.Normalized() * maxSpeed;
			}
        }
		else //ground/default
		{
			currentSpeed += speedAddAmount;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }
        }
		
	}
	public void Decelerate(double delta)
	{
		float speedReduceAmount = maxSpeed * DecelerateFactor * (float)delta;

        if (movementType == MovementType.HOVER)
		{
			hoverMoveVector = hoverMoveVector.MoveToward(Vector2.Zero, speedReduceAmount);
		}
		else //ground/default
		{
			currentSpeed -= speedReduceAmount;
			if (currentSpeed < 0) { currentSpeed = 0; }
		}
    }
	public Vector2 GetMovementVectorHover(double delta)
	{
		return hoverMoveVector * (float)delta;
	}
	public Vector2 GetMovementVectorGround(double delta)
	{
		return Vector2.FromAngle(groundMoveDirection) * currentSpeed * (float)delta;
	}
	public float GetTargetDirection()
	{
		return targetDirection;
	}
	public float GetCurrentDirection()
	{
		return groundMoveDirection;
	}
	public void SetMoveDirection(float directionDegrees)
	{
		groundMoveDirection = Mathf.DegToRad(directionDegrees);
	}

	//Rotation functions
	public void SetTargetDirection(float direction)
	{
		targetDirection = direction;
	}
	public void SetInitialRotation(float rotation)
	{
		groundMoveDirection = rotation;
		targetDirection = rotation;
	}

    public void SetKnockback(Vector2 amount)
    {
        knockBack += amount;
		if (knockBack.Length() > knockBackAmount)
		{
			knockBack = knockBack.Normalized() * knockBackAmount;
		}
    }

    public void OnAreaEntered(Area2D area)
    {
        collidingPhysicsAreas.Add(area);
    }
	/*
    public void OnAreaEntered(Area2D area)
    {
        var overlappingAreas = GetOverlappingAreas();

        foreach (Area2D a in overlappingAreas)
        {
            if (a is MovementComponent)
            {
                MovementComponent otherComponent = (MovementComponent)a;
                otherComponent.SetKnockback(GlobalPosition.DirectionTo(otherComponent.GlobalPosition) * knockBackAmount);
            }
			else //The collision is likely the environment or a building, which isn't wanting to get moved
			{
				SetKnockback(area.GlobalPosition.DirectionTo(GlobalPosition) * knockBackAmount*2);
            }
        }
    }
	*/
	public void OnAreaExited(Area2D area)
	{
        collidingPhysicsAreas.Remove(area);
    }
}

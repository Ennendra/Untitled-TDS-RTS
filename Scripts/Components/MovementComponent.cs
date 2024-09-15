using Godot;
using System;
using System.Collections.Generic;

public partial class MovementComponent : Area2D
{

	//Speed variables
	[Export] float maxSpeed = 60;
	float currentSpeed = 0;
	[Export] float accelerateFactor = 0.5f; //measured in seconds from 0->Max Speed
	bool confirmMovement = false;
    Vector2 knockBack = Vector2.Zero;
    float knockBackFriction = 1;
    public float knockBackAmount = 2.25f;

	List<Area2D> collidingPhysicsAreas = new();

    //Sprite that will be manipulated by movement rotation if applicable
    [Export] Sprite2D legSprite;

	//Direction of movement
	float targetDirection = 0, moveDirection=0;
	//The rotation speed of the unit
	[Export] float rotationSpeedDegrees = 200.0f;
	//The rotation speed converted to radians for use in the _process function
	float rotationSpeed;

    public override void _Ready()
    {
        base._Ready();

		knockBackAmount = maxSpeed / 120 * 2.25f;

		rotationSpeed = Mathf.DegToRad(rotationSpeedDegrees);
    }

    public override void _Process(double delta)
	{
		moveDirection = Mathf.RotateToward(moveDirection, targetDirection, rotationSpeed * (float)delta);
		//moveDirection = Mathf.LerpAngle(moveDirection,targetDirection,rotationFactor*(float)delta); 
		if (legSprite != null )
		{
			legSprite.GlobalRotation = moveDirection;
		}
	}
    public override void _PhysicsProcess(double delta)
    {
        Node2D parent = (Node2D)this.GetParent();
		knockBack = knockBack.MoveToward(Vector2.Zero, knockBackFriction);

		foreach (var area in collidingPhysicsAreas)
		{
            SetKnockback(area.GlobalPosition.DirectionTo(GlobalPosition) * knockBackAmount);
        }

		Vector2 movement = GetMovementVector(delta);
		movement += knockBack;
		parent.GlobalPosition += movement;
    }

    //Movement Functions
    public void Accelerate(double delta)
	{
		currentSpeed += maxSpeed * accelerateFactor * (float)delta;
		if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }
	}
	public void Decelerate(double delta)
	{
        currentSpeed -= maxSpeed * accelerateFactor * (float)delta;
		if (currentSpeed < 0) { currentSpeed = 0; }
    }
	public Vector2 GetMovementVector(double delta)
	{
		return Vector2.FromAngle(moveDirection) * currentSpeed * (float)delta;
	}
	public float GetTargetDirection()
	{
		return targetDirection;
	}
	public float GetCurrentDirection()
	{
		return moveDirection;
	}
	public void SetMoveDirection(float directionDegrees)
	{
		moveDirection = Mathf.DegToRad(directionDegrees);
	}

	//Rotation functions
	public void SetTargetDirection(float direction)
	{
		targetDirection = direction;
	}
	public void SetInitialRotation(float rotation)
	{
		moveDirection = rotation;
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

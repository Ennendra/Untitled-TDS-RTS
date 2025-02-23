using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Reflection.Metadata.Ecma335;

public enum MovementType
{
	GROUND,
	HOVER,
	AIR
}

public partial class MovementComponent : Node2D
{

	//The movement type (which can dictate how the unit moves and what count as obstacles
	[Export] MovementType movementType = MovementType.GROUND;
	public MovementType GetMovementType() { return movementType; }

	[Export] CollisionShape2D collisionShape;

	//Speed variables
	[Export] float maxSpeed = 60;
	float currentSpeed = 0;
	[Export] float timeToMaxSpeed = 0.5f;
	[Export] float timeToZeroSpeed = 0.5f;
    float AccelerateFactor { get => 1 / timeToMaxSpeed; }
    float DecelerateFactor { get => 1 / timeToZeroSpeed; }

	//Tells us whether the unit is "moving"
	//This will stop it from pushing units if it's only motion is from being pushed itself
    public bool isMoving { get; private set; } = false;

    //Sprite that will be manipulated by movement rotation if applicable
    [Export] Sprite2D legSprite;

	//Direction of movement (for ground)
	float targetDirection = 0, groundMoveDirection=0;
	//Direction of movement (for hover)
	Vector2 hoverMoveVector = new Vector2(0,0);

	Vector2 currentMoveVector = new Vector2(0,0);
	//The rotation speed of the unit

	[Export] float rotationSpeedDegrees = 200.0f;
	
	//The rotation speed converted to radians for use in the _process function
	float rotationSpeed { get => Mathf.DegToRad(rotationSpeedDegrees); }

	//The old lerp rotation factor, will remove if confirming to use linear instead
    float rotationFactor = 2.5f;

    public override void _Ready()
    {
        base._Ready();

    }

    public override void _Process(double delta)
	{
		if (movementType == MovementType.HOVER)
		{
            if (IsInstanceValid(legSprite) && currentMoveVector.Length() > 1.0f)
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
        CharacterBody2D parent = (CharacterBody2D)this.GetParent();

		//Get the base movement vector

		//If we're grounded, rotate the current movement slightly towards the target direction (to reduce the 'slipperiness' of grounded units)
		if (movementType == MovementType.GROUND) 
		{
			float currentMoveAngle = currentMoveVector.Angle();
			float currentMoveSpeed = currentMoveVector.Length();
			float angleCorrectionSpeed = Mathf.DegToRad(180);

			float newMoveAngle = Mathf.RotateToward(currentMoveAngle, targetDirection, angleCorrectionSpeed * (float)delta);

			currentMoveVector = Vector2.FromAngle(newMoveAngle) * currentMoveSpeed;
		}

		if (currentMoveVector.Length() > 0) 
		{
			parent.Velocity = currentMoveVector;
			bool isCollision = parent.MoveAndSlide();

			//Process additional physics if there was a collision
			if (isCollision)
			{
				for (int i = 0; i < parent.GetSlideCollisionCount(); i++)
				{
					KinematicCollision2D collision = parent.GetSlideCollision(i);
					Node2D collisionObject = (Node2D)collision.GetCollider();
					if (collisionObject.IsInGroup("Unit"))
					{
						//Get the movement component of this unit
						UnitParent collidedUnit = (UnitParent)collisionObject;
						MovementComponent mComponent = collidedUnit.GetMovementComponent();

						//Add a push to the unit's movement vector based on the collision and if it was from this object's movement
						if (!mComponent.isMoving)
						{
							float pushAngle = GlobalPosition.DirectionTo(collision.GetPosition()).Angle();
							float pushAmount = collision.GetRemainder().Length();
							Vector2 pushVector = Vector2.FromAngle(pushAngle) * pushAmount;
							mComponent.AddMovementForce(pushVector, delta);
							
						}
					}
				}
			}
		}
    }

    //Movement Functions
    public void Accelerate(double delta)
	{
        float speedAddAmount = maxSpeed * AccelerateFactor * (float)delta;
        if (movementType == MovementType.HOVER)
		{
            //Add acceleration to the movement vector, and cap the length to the maxspeed
            currentMoveVector += Vector2.FromAngle(targetDirection) * speedAddAmount;
			if (currentMoveVector.Length() > maxSpeed)
			{
                currentMoveVector = currentMoveVector.Normalized() * maxSpeed;
			}
        }
		else //ground/default
		{
			currentMoveVector += Vector2.FromAngle(GetCurrentDirection()) * speedAddAmount;
            if (currentMoveVector.Length() > maxSpeed)
            {
                currentMoveVector = currentMoveVector.Normalized() * maxSpeed;
            }
        }
		isMoving = true;

    }
	public void Decelerate(double delta)
	{
		float speedReduceAmount = maxSpeed * DecelerateFactor * (float)delta;

        if (movementType == MovementType.HOVER)
		{
            currentMoveVector = currentMoveVector.MoveToward(Vector2.Zero, speedReduceAmount);
		}
		else //ground/default
		{
            currentMoveVector = currentMoveVector.MoveToward(Vector2.Zero, speedReduceAmount);
            //currentSpeed -= speedReduceAmount;
			//if (currentSpeed < 0) { currentSpeed = 0; }
		}

		if (currentMoveVector.Length() < 1) { isMoving = false; }
    }
	public void AddMovementForce(Vector2 pushForce, double delta)
	{
		currentMoveVector += pushForce / (float)delta;
        if (currentMoveVector.Length() > maxSpeed)
        {
            currentMoveVector = currentMoveVector.Normalized() * maxSpeed;
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

}

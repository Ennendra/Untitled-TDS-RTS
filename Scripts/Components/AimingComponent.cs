using Godot;
using System;



//Note: The sprite that rotates with the unit's aiming, and all weapons, will be a child of this node, which wil rotate accordingly
public partial class AimingComponent : Node2D
{
    //Signals for equipping a tool or weapon
    [Signal] public delegate void EquipToolEventHandler(ToolParent tool);
    [Signal] public delegate void EquipWeaponEventHandler(WeaponParent weapon);

    //The weapons equipped on the component
    [Export] public WeaponParent equippedWeapon { get; protected set; }
    [Export] public ToolParent equippedTool { get; protected set; }

    //Rotation speed in degrees per second
    [Export] float rotationSpeed = 2.5f;
    //The rotation speed converted to radians for use in the _process function
    float rotationSpeedRadians;
    float currentAimDirection = 0, targetAimDirection = 0;

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Ready()
    {
        base._Ready();

        //Connect the signals to functions
        EquipTool += EquipNewTool;
        EquipWeapon += EquipNewWeapon;

        rotationSpeedRadians = Mathf.DegToRad(rotationSpeed);
    }
    public override void _Process(double delta)
    {
        //rotating towards the target direction by a set amount (commented was a more logarithmic rotation but may be out of date)
        currentAimDirection = Mathf.RotateToward(currentAimDirection, targetAimDirection,rotationSpeedRadians * (float)delta);
        //currentAimDirection = Mathf.LerpAngle(currentAimDirection, targetAimDirection, aimRotationFactor * (float)delta);
        GlobalRotation = currentAimDirection;
    }

    public void FireWeapons()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.FireWeapon();
        }
    }
    public void UseTool()
    {
        if (equippedTool != null)
        {
            equippedTool.UseTool();
        }
    }
    public void StopTool()
    {
        if (equippedTool != null)
        {
            equippedTool.StopTool();
        }
    }

    //Setting the target direction (either directly or via a position)
    public void SetTargetDirection(float targetAngle)
    {
        targetAimDirection = targetAngle;
    }
    public void SetTargetDirection(Vector2 targetPosition)
    {
        targetAimDirection = GlobalPosition.DirectionTo(targetPosition).Angle();
    }

    public void SetCurrentAimDirection(float directionDegrees)
    {
        currentAimDirection = Mathf.DegToRad(directionDegrees);
    }

    //Signal functions for equipping new tools or weapons
    public void EquipNewTool(ToolParent tool)
    {
        equippedTool = tool;
    }
    public void EquipNewWeapon(WeaponParent weapon)
    {
        equippedWeapon = weapon;
    }
}

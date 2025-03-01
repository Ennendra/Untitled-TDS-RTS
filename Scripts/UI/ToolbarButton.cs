using Godot;
using System;

public partial class ToolbarButton : Button
{
	//Tells us whether the button is for housing a tool, or a weapon
	[Export] bool isButtonTool = false;

    //The aim component this tool or weapon input is linked to (ie. when pressed, it will equip the tool to this)
    public AimingComponent equipLink { get; protected set; }

	//The tool or weapon that this button is linked to.
    ToolParent tool;
	WeaponParent weapon;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        //Check if the tool or weapon we are linking to is equipped and alter the texture accordingly
        if (equipLink != null)
        {
            Color equippedColor = new Color(0, 1, 1);
            Color nonEquippedColor = new Color(0, 0.3f, 0.3f);

            if (isButtonTool)
            {
                if (equipLink.equippedTool == tool)
                    { Modulate = equippedColor; }
                else
                    { Modulate = nonEquippedColor; }
            }
            else
            {
                if (equipLink.equippedWeapon == weapon)
                    { Modulate = equippedColor; }
                else
                    { Modulate = nonEquippedColor; }
            }
        }
    }

    public void SetAimComponentLink(AimingComponent link)
    {
        equipLink = link;
    }
    public void SetNewToolLink(ToolParent tool)
    {
        if (tool == null)
        {
            this.tool = null;
            return;
        }

        this.tool = tool;
        Icon = tool.toolIcon;
    }
    public void SetNewWeaponLink(WeaponParent weapon)
    {
        if (weapon == null)
        {
            this.weapon = null;
            return;
        }

        this.weapon = weapon;
        Icon = weapon.weaponIcon;
    }

    public void OnButtonPressed()
    {
        ExecuteEquipInput();
    }

    public void ExecuteEquipInput()
    {
        if (equipLink != null && !Input.IsActionPressed("HideUI"))
        {
            if (isButtonTool && tool != null)
            {
                equipLink.EmitSignal("EquipTool", tool);
            }
            else if (weapon != null)
            {
                equipLink.EmitSignal("EquipWeapon", weapon);
            }

        }
    }
}

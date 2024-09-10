using Godot;
using System;

public partial class UI_PersonalToolbar : Control
{
    [Export] BuildButton[] buildingButtons;
    [Export] ToolbarButton[] toolbarButtons;
    ProgressBar healthBar;

	//The list of items that the building buttons will be linked to
    [Export] ConstructInfo[] buildingList_tier1;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        healthBar = GetNode<ProgressBar>("HealthBar");

        SetBuildingButtons(buildingList_tier1);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    //Functions for the Equipment Bar
    public void SetAimComponentLink(AimingComponent link)
    {
        foreach (ToolbarButton button in toolbarButtons)
        {
            button.SetAimComponentLink(link);
        }
    }
    public void SetNewToolLink(ToolParent tool, int index)
    {
        toolbarButtons[index].SetNewToolLink(tool);
    }
    public void SetNewWeaponLink(WeaponParent weapon, int index)
    {
        toolbarButtons[index + 3].SetNewWeaponLink(weapon);
    }
    public void ExecuteEquipInput(int inputIndex)
    {
        //0-2 = tools 3-5 = weapons
        toolbarButtons[inputIndex].ExecuteEquipInput();
    }

    //Functions for the building buttons
    public void SetBuildingButtons(ConstructInfo[] buildingList)
    {
        for (int i=0; i<buildingButtons.Length; i++)
        {
            if (i < buildingList.Length)
            {
                buildingButtons[i].SetButtonConstructInfo(buildingList[i]);
                buildingButtons[i].Visible = true;
            }
            else
            {
                buildingButtons[i].Visible = false;
            }
        }
    }
    public void SetBuildButtonConnection(Node2D connection)
    {
        foreach (BuildButton button in buildingButtons)
        {
            button.SetButtonConnection(connection);
        }
    }

    //Functions for the healthbar
    public void GetNewHealthData(float healthPercent)
    {
        healthBar.Value = healthPercent;
    }
}

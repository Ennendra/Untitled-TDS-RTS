using Godot;
using System;

public partial class UI_RTSControlGroupBar : Control
{
	[Export] UI_ControlGroupButton[] cgButtons;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		for (int i = 0; i < cgButtons.Length; i++)
		{
			cgButtons[i].SetIcon(null);
			cgButtons[i].SetGroupIndex(i);
            cgButtons[i].Disabled = true;
        }
	}

	public void SetButtonConnections(Node2D connection)
	{
        for (int i = 0; i < cgButtons.Length; i++)
        {
            cgButtons[i].SetButtonConnection(connection);
        }
    }

	public void SetControlGroupButtonInfo(int index, ControlGroup group)
	{
		if (group.units.Count > 0)
		{
            cgButtons[index].SetIcon(group.frontUnit.iconTex);
            cgButtons[index].Disabled = false;
        }
		else
		{
            cgButtons[index].SetIcon(null);
            cgButtons[index].Disabled = true;
        }
        
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

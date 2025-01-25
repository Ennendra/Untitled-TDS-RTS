using Godot;
using System;
using System.Collections.Generic;

public partial class UI_BuildQueueBar : Control
{
	[Export] BuildQueueButton[] buttons;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

    public void SetBuildButtonConnection(Node2D connection)
    {
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].SetButtonConnection(connection, i);
        }
    }

	public void UpdateButtonQueueInfo(List<BuildingQueue> queueInfo)
	{
		for (int i = 0; i < buttons.Length; i++)
		{
            if (i < queueInfo.Count)
                { buttons[i].SetBuildInfo(queueInfo[i]); }
			else 
				{ buttons[i].SetBuildInfo(null); }
		}
	}
}

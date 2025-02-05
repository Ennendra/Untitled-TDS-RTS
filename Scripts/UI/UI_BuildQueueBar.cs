using Godot;
using System;
using System.Collections.Generic;

public partial class UI_BuildQueueBar : Control
{
    //The node that these buttons will emit signals to (ie. the mainlevelcontroller)
    Node2D inputConnection;

    [Export] BuildQueueButton[] buttons;
	Button toggleButton;
	TextureRect greenLight, redLight;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		toggleButton = GetNode<Button>("MainTex/ToggleButton");
        redLight = GetNode<TextureRect>("MainTex/RedLight");
        greenLight = GetNode<TextureRect>("MainTex/GreenLight");
    }

	public void ToggleToolActivity(bool activity)
	{
		//Tool is turned on
		if (activity)
		{
			greenLight.Visible = true;
			redLight.Visible = false;
		}
        else
        {
            greenLight.Visible = false;
            redLight.Visible = true;
        }
    }

    public void SetBuildButtonConnection(Node2D connection)
    {
		inputConnection = connection;
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

	public void OnToggleButtonPressed()
	{
        if (inputConnection != null)
        {
            inputConnection.EmitSignal("ToggleBuildActivity");
        }
    }
}

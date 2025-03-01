using Godot;
using System;

public partial class UI_ControlGroupButton : Button
{
	TextureRect icon;
	Label groupSize;
	int index;
	Node2D connection;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		icon = GetNode<TextureRect>("CGIcon");
        groupSize = GetNode<Label>("CGGroup");

		groupSize.Text = "";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetButtonConnection(Node2D connection)
	{
		this.connection = connection;
	}

	public void SetIcon(Texture2D tex)
	{
		icon.Texture = tex;
	}

	public void SetGroupIndex(int index)
	{
		//if (index == 9) group.Text = "0";
		//else group.Text = (index+1).ToString();
		this.index = index;
	}

	public void SetControlGroupData(Texture2D tex, int size)
	{
        icon.Texture = tex;
		if (size > 0)
		{
			groupSize.Text = size.ToString();
		}
		else
		{
			groupSize.Text = "";
		}

	}

	public void OnButtonPressed()
	{
        if (connection != null && !Input.IsActionPressed("HideUI"))
        {
            connection.EmitSignal("SelectControlGroupButton", index);
        }
    }
}

using Godot;
using System;

public partial class UI_ControlGroupButton : TextureButton
{
	TextureRect icon;
	Label group;
	int index;
	Node2D connection;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		icon = GetNode<TextureRect>("CGIcon");
		group = GetNode<Label>("CGGroup");
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
		if (index == 9) group.Text = "0";
		else group.Text = (index+1).ToString();
		this.index = index;
	}

	public void OnButtonPressed()
	{
        if (connection != null)
        {
            connection.EmitSignal("SelectControlGroupButton", index);
        }
        else GD.Print("Control Group button has no connection!");
    }
}

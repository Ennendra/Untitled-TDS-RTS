using Godot;
using System;

public partial class TutorialWaypoint : Area2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnAreaEntered (Area2D area)
	{
		CallDeferred("queue_free");
	}
}

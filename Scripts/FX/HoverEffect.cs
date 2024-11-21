using Godot;
using System;

public partial class HoverEffect : Sprite2D
{
	//used to manage the state of the animation
	float timeProcess = 0;
	float currentAlpha = 1;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timeProcess += (float)delta;

		currentAlpha = (Mathf.Sin(timeProcess * 4) / 2 - 0.5f) + 1;

		this.Modulate = new Color(1,1,1,currentAlpha);
	}
}

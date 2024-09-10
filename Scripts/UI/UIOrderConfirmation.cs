using Godot;
using System;

public partial class UIOrderConfirmation : Sprite2D
{
	float alpha = 1, scale = 1;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		scale+=(float)delta * 0.3f;
		alpha -= (float)delta * 2.2f;

		this.Modulate = new Color(1,1,1, alpha);

		if (alpha<=0) { QueueFree(); }
	}
}

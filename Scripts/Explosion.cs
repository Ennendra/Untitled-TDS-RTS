using Godot;
using System;

public partial class Explosion : Node2D
{
	[Export] AnimatedSprite2D sprite;
    [Export] GpuParticles2D particles;
    [Export] AudioStreamPlayer2D audio;

	bool animFinished = false;
	bool particleFinished = false;
	bool audioFinished = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();
		//sprite = GetNode<AnimatedSprite2D>("Sprite");
		//particles = GetNode<GpuParticles2D>("Particles");
		//audio = GetNode<AudioStreamPlayer2D>("Audio");

		//run the sprite animation and emit particles if they exist
		if (IsInstanceValid(sprite)) { sprite.Play(); }
		if (IsInstanceValid(particles)) 
		{
			particles.Emitting = true; 
		}
		//Audio will be set to autoplay and does not need to be applied here
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//auto set items to finish if they are not set
		if (!IsInstanceValid(sprite)) { animFinished = true; }
		if (!IsInstanceValid(particles)) { particleFinished = true; }
		if (!IsInstanceValid(audio)) { audioFinished = true; }

		if (animFinished && particleFinished && audioFinished) { QueueFree(); }
	}

	public void OnAnimationFinished()
	{
		animFinished = true;
		sprite.Visible = false;
	}
	public void OnParticlesFinished()
	{
		particleFinished = true;
	}

	public void OnAudioFinished()
	{
		audioFinished = true;
	}
}

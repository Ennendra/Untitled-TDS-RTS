using Godot;
using System;

public partial class Explosion : Node2D
{
	AnimatedSprite2D sprite;
	GpuParticles2D particles;
	AudioStreamPlayer2D audio;

	bool animFinished = false;
	bool particleFinished = false;
	bool audioFinished = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("Sprite");
		particles = GetNode<GpuParticles2D>("Particles");
		audio = GetNode<AudioStreamPlayer2D>("Audio");

		sprite.Play();
		particles.Emitting = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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

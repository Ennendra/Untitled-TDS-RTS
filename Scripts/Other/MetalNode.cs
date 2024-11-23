using Godot;
using System;

public partial class MetalNode : Area2D
{
	[Export] public MinimapMarkerComponent marker { get; private set; }
}

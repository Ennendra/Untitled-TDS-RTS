using Godot;
using System;

public partial class ObstacleMapEdge : StaticBody2D
{
	[Export] CollisionPolygon2D polygon;

	public void SetCollisionPolygon(Vector2[] newPolygon)
	{
		polygon.Polygon = newPolygon;
	}
}

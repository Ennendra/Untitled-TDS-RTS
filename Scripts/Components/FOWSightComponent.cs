using Godot;
using System;

public partial class FOWSightComponent : Node2D
{
	//The range (as a radius in px) of the unit's vision
	[Export] int sightRange = 500;
	//The faction component, so the controller can check whether it's an allied unit
	[Export] FactionComponent factionComponent;
	[Export] Texture2D visionTexture;
	public Rect2I sightRect { get; protected set; }
	public Image visionImage { get; protected set; }
	public Vector2 offset { get; protected set; }

	//Tells us whether the elements here are 
	public bool componentRescaled = false;

	//Rescales the rect to match the compute scale in 
	public void InitSightComponent(float computeScale)
	{
        visionImage = visionTexture.GetImage();

        sightRect = new Rect2I(Vector2I.Zero, (int)(sightRange * 2 * computeScale), (int)(sightRange * 2 * computeScale));
		visionImage.Resize((int)(sightRange * 2 * computeScale), (int)(sightRange * 2 * computeScale));
		offset = new Vector2(sightRange, sightRange);
	}

    public int GetSightFaction()
	{
		return factionComponent.faction;
	}

}

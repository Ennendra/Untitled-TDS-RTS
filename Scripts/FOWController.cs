using Godot;
using System;
using System.Collections.Generic;

public partial class FOWController : Node2D
{
	[Export] Sprite2D visionFog, shroudFog;
	[Export] Texture2D visionTexture;
    [Export] float calculateScale = 0.05f;

	int playerFaction = 1;
	List<FOWSightComponent> sightComponents = new();

    //debouncer
    float timeSinceFowUpdate = 0;
    float fowUpdatesPerSecond = 15;
    float fowDebounceTime { get => 1 / fowUpdatesPerSecond; }

    //Defining the map boundaries
    Vector2 topLeft, mapSize;

	Image fogImage, shroudImage, visionImage;
	ImageTexture fogTexture, shroudTexture;

	public ImageTexture[] InitFOW(Vector2 topLeft, Vector2 mapSize)
	{
        //Define the map boundaries
        this.topLeft = topLeft;
        this.mapSize = mapSize;

		//Set the vision texture to an image
        visionImage = visionTexture.GetImage();
        visionImage.Resize((int)(visionTexture.GetWidth() * calculateScale), (int)(visionTexture.GetHeight() * calculateScale));

        visionFog.Scale = new Vector2(1 / calculateScale, 1 / calculateScale);
        shroudFog.Scale = new Vector2(1 / calculateScale, 1 / calculateScale);
        GlobalPosition = topLeft;

        //Create the shroud
        shroudImage = Image.CreateEmpty((int)(mapSize.X * calculateScale), (int)(mapSize.Y * calculateScale), false, Image.Format.Rgba8);
        shroudImage.Fill(Colors.Black);
        shroudTexture = ImageTexture.CreateFromImage(shroudImage);
        shroudFog.Texture = shroudTexture;

        //Run an initial process of the FOW (Running call deferred to prevent it from getting sight component data before they are ready)
        return ProcessFOW();
    }

	public void AddSightComponent(FOWSightComponent component)
	{
		sightComponents.Add(component);
        component.InitSightComponent(calculateScale);
	}
    public void RemoveSightComponent(FOWSightComponent component)
    {
        sightComponents.Remove(component);
    }

	public ImageTexture[] ProcessFOW()
	{
        //Refresh the vision fog
        fogImage = Image.CreateEmpty((int)(mapSize.X * calculateScale), (int)(mapSize.Y * calculateScale), false, Image.Format.Rgba8);
        fogImage.Fill(new Color(0.3f, 0.3f, 0.3f, 0.3f));
        fogTexture = ImageTexture.CreateFromImage(fogImage);
        visionFog.Texture = fogTexture;

        foreach (FOWSightComponent component in sightComponents)
        {
            //Only apply the light mask if the unit's faction aligns with the player
            if (component.GetSightFaction() == playerFaction)
            {
                shroudImage.BlendRect(component.visionImage, component.sightRect, (Vector2I)((component.GlobalPosition - topLeft - component.offset) * calculateScale));
                fogImage.BlendRect(component.visionImage, component.sightRect, (Vector2I)((component.GlobalPosition - topLeft - component.offset) * calculateScale));
            }

            //Update the textures of the fog and shroud
            shroudTexture.Update(shroudImage);
            fogTexture.Update(fogImage);
        }
        ImageTexture[] returnTex = new ImageTexture[2];
        returnTex[0] = ImageTexture.CreateFromImage(fogImage);
        returnTex[1] = ImageTexture.CreateFromImage(shroudImage);
        return returnTex;
	}

    public ImageTexture[] ProcessCall(double delta)
    {
        //Debounce the FOW process
        timeSinceFowUpdate += (float)delta;
        if (timeSinceFowUpdate >= fowDebounceTime)
        {
            timeSinceFowUpdate -= fowDebounceTime;

            return ProcessFOW();
        }
        return null;
    }

}

/*using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

public partial class FogOfWarElement : TextureRect
{

	//debouncer
	float timeSinceFowUpdate = 0;
	float fowUpdatesPerSecond = 15;
	float fowDebounceTime { get => 1/fowUpdatesPerSecond;  }

	//Sight components belonging to units of this faction will be visible
	int sightFaction = 1;

	//Textures of the basic fog and the unexplored fog
	[Export] Texture2D fogTexture;
	//Determines whether to re-add the fog in non-lit areas once lights move
	[Export] bool persistentFog = false;

	[Export] float fowCalculateScale = 0.2f;

	string lightSightGroup = "FOWSight";
	string lightOccludeGroup = "FOWBlock";

	SubViewport lightSV, maskSV;
	TextureRect fogMask, background;

	Image maskImage;
	Texture2D maskTexture;
	Vector2I sizeVec;

    //A dictionary that links a sight component in realspace to a duplicated sightComponent in the subviewport
    Dictionary sightComponents = new();
	Dictionary blockerComponents = new();

	Vector2 topLeft;

	public void InitFOW(Vector2 topLeft, Vector2 mapSize)
	{
		//Assign the top-left of the FOW to match the world map
		this.topLeft = topLeft * fowCalculateScale;

        lightSV = GetNode<SubViewport>("LightSubViewport");
        maskSV = GetNode<SubViewport>("MaskSubViewport");
        fogMask = GetNode<TextureRect>("MaskSubViewport/FogMask");
        background = GetNode<TextureRect>("LightSubViewport/Background");

        sizeVec = (Vector2I)(mapSize * fowCalculateScale);
		GD.Print(sizeVec);
		//Setting the viewports and texturerects to the mapsize
		//this.Size = sizeVec;
		//this.Position = topLeft * fowCalculateScale;
        this.Size = mapSize;
        this.Position = topLeft;
        lightSV.Size = sizeVec;
		maskSV.Size = sizeVec;
        fogMask.Size = sizeVec;
        background.Size = sizeVec;

		//Mask
		maskImage = Image.CreateEmpty((int)mapSize.X, (int)mapSize.Y, false, Image.Format.Rgba8);
		maskImage.Fill(Colors.Black);
		maskTexture= ImageTexture.CreateFromImage(maskImage);

		//Setting the shader parameters
		ShaderMaterial fogMaskMaterial = (ShaderMaterial)fogMask.Material;
        ShaderMaterial mainMaterial = (ShaderMaterial)Material;
		fogMaskMaterial.SetShaderParameter("persistent_reveal", persistentFog);

		mainMaterial.SetShaderParameter("fog_texture", fogTexture);
		mainMaterial.SetShaderParameter("mask_texture", maskTexture);
		if (persistentFog)
		{
			fogMaskMaterial.SetShaderParameter("mask_texture", maskTexture);
        }


    }

	//Add the sight component to the dictionary and create a duplicate within the subviewport
	public void AddSightComponent(FOWSightComponent component)
	{
		FOWSightComponent dupComponent = (FOWSightComponent)component.Duplicate();
		dupComponent.Color = Colors.White;
		dupComponent.GlobalPosition = component.GlobalPosition * fowCalculateScale;
		dupComponent.Scale = new Vector2(1.0f, 1.0f) * fowCalculateScale;
		lightSV.AddChild(dupComponent);
		sightComponents.Add(component.GetInstanceId(), dupComponent);
	}

	public void AddBlockerComponent(FOWBlockerComponent component)
	{
        FOWBlockerComponent dupComponent = (FOWBlockerComponent)component.Duplicate();
        dupComponent.GlobalPosition = component.GlobalPosition * fowCalculateScale;
        dupComponent.Scale = new Vector2(1, 1) * fowCalculateScale;
        lightSV.AddChild(dupComponent);
        blockerComponents.Add(component.GetInstanceId(), dupComponent);
	}

    public void RemoveSightComponent(FOWSightComponent component)
    {
        sightComponents.Remove(component.GetInstanceId());
    }

    public void RemoveBlockerComponent(FOWBlockerComponent component)
    {
        blockerComponents.Remove(component.GetInstanceId());
    }

	public void ProcessFOW()
	{
        //Go through each dictionary entry and update the positions and set visibility based on faction
        foreach (KeyValuePair<Variant, Variant> entry in sightComponents)
        {
            FOWSightComponent component = (FOWSightComponent)entry.Value;
            FOWSightComponent realComponent = (FOWSightComponent)InstanceFromId((ulong)entry.Key);
            component.Position = (realComponent.GlobalPosition * fowCalculateScale) - topLeft;
			if (realComponent.GetParent().Name == "Player")
			{
				GD.Print("Real Position: " + realComponent.GlobalPosition);
				GD.Print("FOW Position: " + component.Position);
				GD.Print("Real Scaled: " + realComponent.GlobalPosition * fowCalculateScale);
			}

            //Determine whether this vision is active based on faction alignment
            if (realComponent.GetSightFaction() == sightFaction) 
				component.Visible = true;
			else
				component.Visible = false;
            
        }

        //apply the new positions to the shaders
        maskImage = maskSV.GetTexture().GetImage();
        maskTexture = ImageTexture.CreateFromImage(maskImage);
        ShaderMaterial mainMaterial = (ShaderMaterial)Material;
        mainMaterial.SetShaderParameter("mask_texture", maskTexture);
        if (persistentFog)
        {
            ShaderMaterial fogMaskMaterial = (ShaderMaterial)fogMask.Material;
            fogMaskMaterial.SetShaderParameter("mask_texture", maskTexture);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		//Debounce the FOW process
		timeSinceFowUpdate += (float)delta;
		if (timeSinceFowUpdate >= fowDebounceTime)
		{
			ProcessFOW();

            timeSinceFowUpdate -= fowDebounceTime;
		}
	}
}
*/
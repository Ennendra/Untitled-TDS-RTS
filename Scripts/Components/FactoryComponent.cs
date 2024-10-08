using Godot;
using System;
using System.Collections.Generic;



public partial class FactoryComponent : Node2D
{
	[Export] public ConstructInfo[] buildableUnits { get; protected set; }

    [Export] FactionComponent factionComponent;

	List<ConstructInfo> buildQueue = new();
	ConstructorComponent component;

    //Variables for the unit build graphics
    float buildTimer = 0;
    ProgressBar progressBar;
    Sprite2D mainSprite, scaffoldSprite;
    ShaderMaterial scaffoldShader;
    
    [Export] Vector2 buildOffset;
    [Export] float unitStartRotation = -90;
    [Export] Vector2 initialRallyPoint = new Vector2(0,-150);

    //Info on the current build item
    float energySupplied = 0, metalSupplied = 0;
    float totalCost { get => GetCurrentBuildItem().unitInfo.energyCost + GetCurrentBuildItem().unitInfo.metalCost; }
    float totalSupplied { get => energySupplied + metalSupplied; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        progressBar = GetNode<ProgressBar>("BuildProgressBar");

        progressBar.Value = 0;

        mainSprite = GetNode<Sprite2D>("Sprite");
        scaffoldSprite = GetNode<Sprite2D>("SpriteScaffold");

        scaffoldSprite.Texture = mainSprite.Texture;
        scaffoldShader = scaffoldSprite.Material as ShaderMaterial;

        mainSprite.RotationDegrees = unitStartRotation;
        scaffoldSprite.RotationDegrees = unitStartRotation;
        mainSprite.Position = buildOffset;
        scaffoldSprite.Position = buildOffset;

        factionComponent.SetRallyPoint(this.GlobalPosition + initialRallyPoint);


    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        progressBar.Value = totalSupplied;
        buildTimer += (float)delta * 3;

        //TODO: Remove this once the tool stuff is properly implemented
        //float[] ratios = GetEnergyMetalRatio();
        //float tempMaxSupply = 5;
        //SupplyResources(tempMaxSupply * ratios[0] * (float)delta, tempMaxSupply * ratios[1] * (float)delta);
        // ----

		if (buildQueue.Count > 0)
		{
            progressBar.Visible = true;
            mainSprite.Visible = true;
            scaffoldSprite.Visible = true;

            progressBar.MaxValue = GetCurrentBuildItem().unitInfo.energyCost + GetCurrentBuildItem().unitInfo.metalCost;

            mainSprite.Texture = GetCurrentBuildItem().unitInfo.iconTex;
            scaffoldSprite.Texture = GetCurrentBuildItem().unitInfo.iconTex;

            float currentProgress = totalSupplied / totalCost;
            float waveTime = Mathf.Sin(buildTimer);
            float shaderFillLevel = Mathf.Clamp(1 - currentProgress + (waveTime / 5), 0, 1.2f);

            scaffoldShader.SetShaderParameter("effect_filling", shaderFillLevel);

            if (energySupplied >= GetCurrentBuildItem().unitInfo.energyCost && metalSupplied >= GetCurrentBuildItem().unitInfo.metalCost)
                CompleteItem();
        }
        else
        {
            progressBar.Visible = false;
            mainSprite.Visible = false;
            scaffoldSprite.Visible = false;
        }
        
    }

    //Gets the energy/metal ratio of the unit currently being built
    public float[] GetEnergyMetalRatio()
    {
        float energyCost = GetCurrentBuildItem().unitInfo.energyCost;
        float metalCost = GetCurrentBuildItem().unitInfo.metalCost;
        float[] ratio = new float[2] { 1, 1 };
        float energyAmount, metalAmount;
        if (metalCost > 0) { energyAmount = energyCost / metalCost; } else { energyAmount = 1; }
        if (energyCost > 0) { metalAmount = metalCost / energyCost; } else { metalAmount = 1; }

        if (energyAmount < 1) { ratio[0] = energyAmount; }
        if (metalAmount < 1) { ratio[1] = metalAmount; }

        return ratio;
    }

    //Build queue functions
    public void AddToBuildQueue(ConstructInfo itemToAdd)
	{
		buildQueue.Add(itemToAdd);
	}
	public void RemoveFromBuildQueue(ConstructInfo itemToRemove)
	{
		buildQueue.Remove(itemToRemove);
	}
	public List<ConstructInfo> GetBuildQueue()
	{
		return buildQueue;
	}
	public ConstructInfo GetCurrentBuildItem()
	{
		return buildQueue[0];
	}
	public void SupplyItem(float energy, float metal)
	{
        energySupplied += energy;
        metalSupplied += metal;
    }
	public void CompleteItem()
	{
        UnitParent newUnit = (UnitParent)GetCurrentBuildItem().objectToSpawn.Instantiate();
        GetTree().CurrentScene.AddChild(newUnit);
        newUnit.GlobalPosition = GlobalPosition + buildOffset;
        newUnit.SetInitialDirection(unitStartRotation);
        newUnit.SetNewFaction(factionComponent.faction);
        newUnit.SetMoveOrder(factionComponent.GetRallyPoint());
        
        buildQueue.RemoveAt(0);
        energySupplied = 0;
        metalSupplied=0;
    }
}

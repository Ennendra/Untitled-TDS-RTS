using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class BlueprintParent : Area2D
{
    [ExportCategory("Unit Information Resource")]
    [Export] UnitInfo unitInfo;

    //The total cost of the blueprint and how much has been supplied. Full supply will "complete" the blueprint
	float energyCost=10, metalCost=10;
	float energySupplied=0, metalSupplied=0;

    //A simple incrementing timer for shader effect use
    float blueprintTimer = 0;

    float totalCost { get => energyCost + metalCost; }
    float totalSupplied {  get => energySupplied + metalSupplied; }
    [ExportCategory("Building Spawned on Completion")]
    [Export] PackedScene objectToSpawn;
    [ExportCategory("Components")]
    [Export] DamageComponent damageComponent;
    [Export] FactionComponent factionComponent;
    [Export] FOWSightComponent sightComponent;
    public MinimapMarkerComponent markerComponent { get; protected set; }

    [ExportCategory("Building Size")]
    [Export] public Polygon2D buildingObstacleBounds { get; protected set; }

    ProgressBar progressBar;

    Sprite2D mainSprite, scaffoldSprite;
    ShaderMaterial scaffoldShader;
	//gives the ratio of metal and energy needed to build this object. 
	//Index 0 is energy, index 1 is metal
	//The final result will return one index as '1' and the other at a ratio point less than 1.
	

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        //Define variable values based on unitInfo resource
        energyCost = unitInfo.energyCost;
        metalCost = unitInfo.metalCost;
        SetReclaimValue(energyCost, metalCost);
        damageComponent.SetMaxHealth(unitInfo.maxHealth);
        //Set up initial supplied values and health
        energySupplied = energyCost / 10;
        metalSupplied = metalCost / 10;
        damageComponent.SetHealthPercentage(10);
        //Set the progressbar values
        progressBar = GetNode<ProgressBar>("BuildProgressBar");
        progressBar.Value = totalSupplied;
        progressBar.MaxValue = totalCost;

        //Define general nodes within the blueprint
        markerComponent = GetNode<MinimapMarkerComponent>("MinimapMarkerComponent");
        progressBar = GetNode<ProgressBar>("BuildProgressBar");
        mainSprite = GetNode<Sprite2D>("Sprite");
        scaffoldSprite = GetNode<Sprite2D>("SpriteScaffold");
        //Define the scaffold (the holographic part) texture and shader
        scaffoldSprite.Texture = mainSprite.Texture;
        scaffoldShader = scaffoldSprite.Material as ShaderMaterial;

        //Define signals
        AddUserSignal("OnDamageKill");
        Connect("OnDamageKill", new Callable(this, "OnDamageKill"));
        AddUserSignal("OnReclaimKill");
        Connect("OnReclaimKill", new Callable(this, "OnReclaimKill"));

        damageComponent.SetHealthbarOffset(GetBuildingRadius());
        damageComponent.toolRangeGrace = GetBuildingRadius();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        progressBar.Value = totalSupplied;
        blueprintTimer += (float)delta*3;

        float currentProgress = totalSupplied / totalCost;
        float waveTime = Mathf.Sin(blueprintTimer);
        float shaderFillLevel = Mathf.Clamp(1 - currentProgress + (waveTime / 5), 0, 1.2f);

        scaffoldShader.SetShaderParameter("effect_filling", shaderFillLevel);

        //Complete the construction once enough resources have been supplied
		if (energySupplied >= energyCost &&  metalSupplied >= metalCost)
			CompleteBlueprint();


	}

    //Component functions
    public int GetCurrentFaction()
    {
        return factionComponent.faction;
    }
    public void SetNewFaction(int newFaction)
    {
        factionComponent.faction = newFaction;
    }
    public DamageComponent GetDamageComponent()
    {
        return damageComponent;
    }
    public FactionComponent GetFactionComponent()
    {
        return factionComponent;
    }
    public FOWSightComponent GetSightComponent()
    {
        return sightComponent;
    }
    public void SetReclaimValue(float energy, float metal)
    {
        damageComponent.SetReclaimValue(energy, metal);
    }
    //Gets the energy to metal ratio of this blueprint
    public float[] GetEnergyMetalRatio()
    {
        float[] ratio = new float[2] { 1, 1 };
        float energyAmount, metalAmount;
        if (metalCost > 0) { energyAmount = energyCost / metalCost; } else { energyAmount = 1; }
        if (energyCost > 0) { metalAmount = metalCost / energyCost; } else { metalAmount = 1; }

        if (energyAmount < 1) { ratio[0] = energyAmount; }
        if (metalAmount < 1) { ratio[1] = metalAmount; }

        return ratio;
    }
    public void SupplyResources(float energy, float metal)
    {
        energySupplied += energy;
        metalSupplied += metal;

        float costPercentSupplied = (energy + metal) / totalCost;

        damageComponent.HealDamage(costPercentSupplied * damageComponent.maxHealth);
    }
    public void CompleteBlueprint()
	{
        BuildingParent newBuilding = (BuildingParent)objectToSpawn.Instantiate();
        GetTree().CurrentScene.AddChild(newBuilding);
        newBuilding.GlobalPosition = GlobalPosition;
        newBuilding.SetNewFaction(factionComponent.faction);
        newBuilding.GetDamageComponent().SetHealthPercentage(damageComponent.GetCurrentHealthPercent());
        QueueFree();
    }
    public float GetBuildingRadius()
    {
        return mainSprite.Texture.GetHeight() / 2;
    }

    public void OnDamageKill()
    {
        GetTree().CurrentScene.RemoveChild(this);
        QueueFree();
    }
    public void OnReclaimKill()
    {
        GetTree().CurrentScene.RemoveChild(this);
        QueueFree();
    }
}

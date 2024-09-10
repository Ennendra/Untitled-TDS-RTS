using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class BlueprintParent : Area2D
{
    //A reference to the type of building this will be (e.g. MINER, WALL)
	public ConstructType blueprintType;

    

    //The total cost of the blueprint and how much has been supplied. Full supply will "complete" the blueprint
	float energyCost=10, metalCost=10;
	float energySupplied=0, metalSupplied=0;

    //A simple incrementing timer for shader effect use
    float blueprintTimer = 0;

    float totalCost { get => energyCost + metalCost; }
    float totalSupplied {  get => energySupplied + metalSupplied; }

	[Export] PackedScene objectToSpawn;
	[Export] DamageComponent damageComponent;
    [Export] FactionComponent factionComponent;
    public MinimapMarkerComponent markerComponent { get; protected set; }

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
        markerComponent = GetNode<MinimapMarkerComponent>("MinimapMarkerComponent");

        progressBar = GetNode<ProgressBar>("BuildProgressBar");

        progressBar.Value = 0;

        mainSprite = GetNode<Sprite2D>("Sprite");
        scaffoldSprite = GetNode<Sprite2D>("SpriteScaffold");

        scaffoldSprite.Texture = mainSprite.Texture;
        scaffoldShader = scaffoldSprite.Material as ShaderMaterial;


        energySupplied = energyCost / 10;
        metalSupplied = metalCost / 10;
        damageComponent.SetHealthPercentage(10);

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

    public void SetReclaimValue(float energy, float metal)
    {
        damageComponent.SetReclaimValue(energy, metal);
    }
    public void InitItemDetails(ConstructInfo newInfo)
	{
		energyCost = newInfo.unitInfo.energyCost;
		metalCost = newInfo.unitInfo.metalCost;
		blueprintType = newInfo.type;

        progressBar.MaxValue = totalCost;

        SetReclaimValue(energyCost, metalCost);
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

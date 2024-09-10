using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

//Denotes the type of building. Helps the network controller control which buildings to affect performance on
public enum BuildingType
{
	NETWORKHUB,
	MINER,
	REFINERY,
	GENERATOR,
	ENERGYSTORAGE,
	METALSTORAGE,
	FACTORY
}

public partial class BuildingParent : Area2D
{
	Sprite2D mainSprite;

	//variables related to before the building has finished construction
	protected bool buildingConstructed = false;
	float buildProgress = 1;
	[Export] float energyReclaimValue = 1;
    [Export] float metalReclaimValue = 1;
    [Export] BuildingType buildingType;
	[Export] public Polygon2D buildingObstacleBounds { get; protected set; }

    //Components
    [Export] protected DamageComponent damageComponent;
	[Export] protected ResourceComponent resourceComponent;
	[Export] protected ConstructorComponent constructorComponent;
	[Export] protected FactionComponent factionComponent;
	[Export] protected FactoryComponent factoryComponent;
    public MinimapMarkerComponent markerComponent { get; protected set; }

    //performance multiplier of the building
    float currentPerformance = 1;

	//Notes whether the building is 'turned on' or not. Will be toggled manually by the player
	public bool isOnline = true;
	//The network pylons this building is inside. The building will also be considered "offline" if not in any, or if all the pylons it is near are also offline
	List<BaseNetworkController> networksInArea = new();

    public override void _Ready()
    {
        base._Ready();
        markerComponent = GetNode<MinimapMarkerComponent>("MinimapMarkerComponent");

        mainSprite = GetNode<Sprite2D>("MainSprite");
		SetReclaimValue(energyReclaimValue, metalReclaimValue);

		//Set signals for colliding with network areas
		AreaEntered += OnNetworkEntered;
		AreaExited += OnNetworkExited;
		//Set death signals
        AddUserSignal("OnDamageKill");
        Connect("OnDamageKill", new Callable(this, "OnDamageKill"));
        AddUserSignal("OnReclaimKill");
        Connect("OnReclaimKill", new Callable(this, "OnReclaimKill"));

        damageComponent.SetHealthbarOffset(GetBuildingRadius());
        damageComponent.toolRangeGrace = GetBuildingRadius();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);	
		ProcessBuildingTick(delta);
    }

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
    public BuildingType GetBuildingType()
	{
		return buildingType;
	}
	public void ProcessBuildingTick(double delta)
	{
		if (isOnline)
		{
			ProcessBuildingOperation(delta);
		}
	}
	public ResourceComponent GetResourceComponent() 
	{ 
		if (IsInstanceValid(resourceComponent)) return resourceComponent; 
		else return null;
	}
    public ConstructorComponent GetConstructorComponent()
    {
        if (IsInstanceValid(constructorComponent)) return constructorComponent;
        else return null;
    }

    public virtual void ProcessBuildingOperation(double delta)
	{
		//Process anything specific to buildings here. E.g. factories building a unit
	}
	public float GetBuildingPerformance()
	{
		return currentPerformance;
	}
	public void SetBuildingPerformance(float value)
	{
		currentPerformance = value;
	}
	public void SetReclaimValue(float energy, float metal)
	{
        damageComponent.SetReclaimValue(energy, metal);
    }
    public float GetBuildingRadius()
    {
        return mainSprite.Texture.GetHeight() / 2;
    }
	public bool IsBuildingOnline()
	{
		if (!isOnline) return false;
		
		if (buildingType != BuildingType.NETWORKHUB)
		{
			foreach (BaseNetworkController controller in networksInArea)
			{
				if (controller.IsNetworkOnline()) return true;
			}
		}
		else if (isOnline) return true;

        return false;
	}

    //Signal functions
    public void OnNetworkEntered(Area2D area)
    {
		//Add this network area to the list that this building is in
        BaseNetworkController controller = (BaseNetworkController)area;
		networksInArea.Add(controller);
    }
	public void OnNetworkExited(Area2D area)
	{
        //Add this network area to the list that this building is in
        BaseNetworkController controller = (BaseNetworkController)area;
        networksInArea.Remove(controller);
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
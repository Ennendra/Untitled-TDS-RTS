using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	FACTORY,
	TURRET
}

public partial class BuildingParent : CombatantParent
{
	//variables related to before the building has finished construction
	protected bool buildingConstructed = false;
	float buildProgress = 1;

    [ExportCategory("Building Definitions")]
    [Export] BuildingType buildingType; //Defines what type of building this is. Currently only checks if the building is a NetworkHub in relation to being online
	[Export] public Polygon2D buildingObstacleBounds { get; protected set; }

    //Components
    [ExportCategory("Components - Building")]
	[Export] protected ConstructorComponent constructorComponent;
	[Export] protected FactoryComponent factoryComponent;

    //performance multiplier of the building
    float currentPerformance = 1;

	//Notes whether the building is 'turned on' or not. Will be toggled manually by the player
	public bool isOnline = true;
	//The network pylons this building is inside. The building will also be considered "offline" if not in any, or if all the pylons it is near are also offline
	List<BaseNetworkController> networksInArea = new();

    public override void _Ready()
    {
        base._Ready();

        mainSprite = GetNode<Sprite2D>("MainSprite");

        //Set signals for colliding with network areas
        AreaEntered += OnNetworkEntered;
        AreaExited += OnNetworkExited;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);	
		ProcessBuildingTick(delta);
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
}
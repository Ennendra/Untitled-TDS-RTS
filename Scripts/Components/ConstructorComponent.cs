using Godot;
using System;

public partial class ConstructorComponent : Area2D
{
    //An attached faction component, to help base controllers track what faction we are in
    [Export] public FactionComponent factionComponent;

    //[Export] ResourceComponent resourceComponent;
    [Export] float maxSupplyRate;

	float currentMetalConsumptionRate, currentEnergyConsumptionRate;
	float currentPerformance = 1;

	//Whether we are attached to a network
	//public bool isAttachedToNetwork = false;

	public BuildingQueue buildQueueTarget; //Any items in a players building queue that the tool may supply to
    public BlueprintParent blueprintTarget; //blueprints that this tool may be supplying to build
	public FactoryComponent factoryTarget; //factories that this tool may help build units for
	public FactionComponent miscUnitTarget;
	public bool isActive = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsInstanceValid(blueprintTarget) && isActive)
		{
			SupplyBlueprint((float)delta);
		}
		else if (IsInstanceValid(factoryTarget) && isActive)
		{
			SupplyFactory((float)delta);
		}
        else if (IsInstanceValid(miscUnitTarget) && isActive)
        {
            SupplyMiscUnit((float)delta);
        }
        else if (buildQueueTarget!=null && isActive)
        {
            SupplyBuildQueue((float)delta);
        }
        else
		{
            currentEnergyConsumptionRate = 0;
            currentMetalConsumptionRate = 0;
		}
	}

	public void SetResourceConsumptionRate(float energy, float metal)
	{
		currentEnergyConsumptionRate = energy;
		currentMetalConsumptionRate = metal;
	}
	public float[] GetResourceComsumptionRate()
	{
		float[] consumption = new float[2] { currentEnergyConsumptionRate, currentMetalConsumptionRate };
		return consumption;
	}
	public void SupplyBlueprint(float delta)
	{
        float[] supplyRatio = blueprintTarget.GetEnergyMetalRatio();

        currentEnergyConsumptionRate = maxSupplyRate * supplyRatio[0];
        currentMetalConsumptionRate = maxSupplyRate * supplyRatio[1];

        blueprintTarget.SupplyResources(currentEnergyConsumptionRate * currentPerformance * delta, currentMetalConsumptionRate * currentPerformance * delta);
    }
    public void SupplyFactory(float delta)
    {
        float[] supplyRatio = factoryTarget.GetEnergyMetalRatio();

        currentEnergyConsumptionRate = maxSupplyRate * supplyRatio[0];
        currentMetalConsumptionRate = maxSupplyRate * supplyRatio[1];

        factoryTarget.SupplyItem(currentEnergyConsumptionRate * currentPerformance * delta, currentMetalConsumptionRate * currentPerformance * delta);
    }
	public void SupplyMiscUnit(float delta)
	{
        float[] supplyRatio = miscUnitTarget.GetEnergyMetalRatio();

        currentEnergyConsumptionRate = maxSupplyRate * supplyRatio[0];
        currentMetalConsumptionRate = maxSupplyRate * supplyRatio[1];

        miscUnitTarget.ReceiveHealingSupply(currentEnergyConsumptionRate * currentPerformance * delta, currentMetalConsumptionRate * currentPerformance * delta);
    }
	public void SupplyBuildQueue(float delta)
	{
		float[] supplyRatio = buildQueueTarget.GetEnergyMetalRatio();

        currentEnergyConsumptionRate = maxSupplyRate * supplyRatio[0];
        currentMetalConsumptionRate = maxSupplyRate * supplyRatio[1];

		buildQueueTarget.SupplyResources(currentEnergyConsumptionRate * currentPerformance * delta, currentMetalConsumptionRate * currentPerformance * delta);

    }
    //Sets new performance multiplier, returns the change in energy and metal consumption as a result of the change
    public float[] SetNewPerformance(float newPerformance)
    {
		float[] beforePerformance = new float[2] { currentEnergyConsumptionRate * currentPerformance, currentMetalConsumptionRate * currentPerformance };
        currentPerformance = newPerformance;
        float[] afterPerformance = new float[2] { currentEnergyConsumptionRate * currentPerformance, currentMetalConsumptionRate * currentPerformance };

		float[] consumptionVariance = new float[2] { afterPerformance[0] - beforePerformance[0], afterPerformance[1] - beforePerformance[1] };

		return consumptionVariance;
    }
    public float GetMetalRate()
	{
		return currentMetalConsumptionRate * currentPerformance;
	}
	public float GetEnergyRate()
	{
		return currentEnergyConsumptionRate * currentPerformance;
	}


    //Signal functions
    public void OnNetworkEntered(Area2D area)
    {
		//if (!isAttachedToNetwork)
		//{
  //          BaseNetworkController controller = (BaseNetworkController)area;
  //          isAttachedToNetwork = true;
  //          controller.AddConstructorComponentToNetwork(this);
  //      }
    }
    public void OnNetworkExited(Area2D area)
    {
        BaseNetworkController controller = (BaseNetworkController)area;

		//remove self from the network only if this component is part of that specific network
		//if (controller.GetConstructorComponentList().Contains(this))
		//{
  //          isAttachedToNetwork = false;
  //          controller.RemoveConstructorComponentFromNetwork(this);
  //      }
        
    }
}

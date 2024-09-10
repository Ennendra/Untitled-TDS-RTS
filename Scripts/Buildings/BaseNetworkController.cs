using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks.Sources;
using Godot.Collections;
using System.Linq;

public partial class BaseNetworkController : Area2D
{


    //this variable will be false if it is the sub network class (ie, it can be connected to the main network)
    protected bool isMainNetwork = true;

    //Variables to control base operation ticks (
    protected float processTickTimer = 0;
    protected float processTicksPerSecond = 10;
    protected float processTickNext { get { return 1 / processTicksPerSecond; } }

    //the resource amounts on the controller at each tick, used for UI purposes
    protected int[] UITickResults = new int[7];
    protected float[] totalGeneration = new float[3] { 0, 0, 0 };
    protected float[] totalConsumption = new float[3] { 0, 0, 0 };
    protected float[] totalVariance = new float[3] { 0, 0, 0 };
    protected float currentNetworkPerformance = 1;

    //protected CollisionShape2D NetworkAreaCollider;

    protected List<ResourceComponent> resourceComponentsInNetwork = new();
    protected List<ConstructorComponent> constructorComponentsInNetwork = new();
    protected List<SubNetworkController> subNetworks = new();

    //The central building that this controller is attached to (usually parent node)
    [Export] protected FactionComponent networkFaction;
    //How many "Tiles" (typically 50x50 pixels) that the network operates in
    [Export] protected int networkTileRadius = 25;
    CollisionShape2D networkCollider;
    Sprite2D networkAreaSprite;
    //the building that this network controller is attached to. IE. it's parent node. Used to monitor when the building itself is set to online or offline
    [Export] BuildingParent attachedBuilding;

    //The total storage of the network (0=energy, 1=metal)
    protected float[] currentStorage = new float[2] { 0, 0 };

    public override void _Ready()
    {
        base._Ready();
        networkCollider = GetNode<CollisionShape2D>("NetworkCollider");
        if (isMainNetwork)
        {
            networkAreaSprite = GetNode<Sprite2D>("NetworkAreaSprite");
            Vector2 newSize = new Vector2(networkTileRadius * 50, networkTileRadius * 50);
            RectangleShape2D shape = (RectangleShape2D)networkCollider.Shape;
            shape.Size = newSize;
            networkAreaSprite.RegionRect = new Rect2(GlobalPosition, newSize);
        }
    }
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
            processTickTimer += (float)delta;
            if (processTickTimer > processTickNext) 
            { 
                InitiateProcessTick(); 
                processTickTimer -= processTickNext; 
            }
	}
    public void InitiateProcessTick()
    {
        //The following resource array indexes are based on their respective resource:
        //0 -- Energy, 1 -- Metal, 2 -- Raw Metal
        //First, generate the variables needed for the calculations
        //Split the resource components based on what they are consuming
        totalGeneration = new float[3] { 0, 0, 0 };
        totalConsumption = new float[3] { 0, 0, 0 };
        totalVariance = new float[3] { 0, 0, 0 };
        List<ResourceComponent> compsConsumingEnergy = new();
        List<ResourceComponent> compsConsumingMetal = new();
        List<ResourceComponent> compsConsumingRawMetal = new();
        currentNetworkPerformance = 1;

        //Get the generation and consumption from all resource components at full performance
        //Add them to the lists if they are consuming the respective resource
        foreach (ResourceComponent component in resourceComponentsInNetwork)
        {
            component.SetNewPerformance(1);
            totalGeneration[0] += component.GenEnergy;
            totalGeneration[1] += component.GenMetal;
            totalGeneration[2] += component.GenRawMetal;
            totalConsumption[0] += component.ConEnergy;
            totalConsumption[1] += component.ConMetal;
            totalConsumption[2] += component.ConRawMetal;
            if (component.ConEnergy > 0)
                compsConsumingEnergy.Add(component);
            if (component.ConMetal > 0)
                compsConsumingMetal.Add(component);
            if (component.ConRawMetal > 0)
                compsConsumingRawMetal.Add(component);
        }

        //Get the added consumption from all constructors at full performance
        float[] constructorConsumption = new float[2] { 0, 0 };
        foreach (ConstructorComponent component in constructorComponentsInNetwork)
        {
            component.SetNewPerformance(1);
            constructorConsumption[0] += component.GetEnergyRate();
            constructorConsumption[1] += component.GetMetalRate();
        }

        //Get the info from any connected sub-network
        foreach (SubNetworkController controller in subNetworks)
        {
            float[] resourceInfo = controller.GetResourceComponentInfo();
            totalGeneration[0] += resourceInfo[0];
            totalGeneration[1] += resourceInfo[1];
            totalConsumption[0] += resourceInfo[2];
            totalConsumption[1] += resourceInfo[3];

            float[] constructorInfo = controller.GetConstructorComponentInfo();
            constructorConsumption[0] += constructorInfo[0];
            constructorConsumption[1] += constructorInfo[1];
        }

        //Check raw metal variance and set resource components performance of those consuming it to match
        //Change the generation and comsumption tracking to match the new performance
        totalVariance[2] = totalGeneration[2] - totalConsumption[2];
        if (totalVariance[2] < 0)
        {
            float rawMetalPerformance = totalGeneration[2] / totalConsumption[2];
            ResourcePerformanceValue componentChanges;
            foreach (ResourceComponent component in compsConsumingRawMetal)
            {
                componentChanges = component.SetNewPerformance(rawMetalPerformance);
                totalGeneration[0] += componentChanges.genEnergy;
                totalGeneration[1] += componentChanges.genMetal;
                totalGeneration[2] += componentChanges.genRawMetal;
                totalConsumption[0] += componentChanges.conEnergy;
                totalConsumption[1] += componentChanges.conMetal;
                totalConsumption[2] += componentChanges.conRawMetal;
            }
        }

        //Check the variance of the energy and metal
        totalVariance[0] = totalGeneration[0] - totalConsumption[0] - constructorConsumption[0];
        totalVariance[1] = totalGeneration[1] - totalConsumption[1] - constructorConsumption[1];

        //Project what the result for storage will be based on the variances
        float projectedEnergyStorage = currentStorage[0] + (totalVariance[0] * processTickNext);
        float projectedMetalStorage = currentStorage[1] + (totalVariance[1] * processTickNext);
        //Will either resource be in deficit?
        if (projectedEnergyStorage < 0 || projectedMetalStorage < 0)
        {
            //Get the possible performance 
            float energyPerformance = 1;
            float metalPerformance = 1;
            //Get the performance of each resource to determine which is suffering the most
            if (projectedEnergyStorage < 0)
                energyPerformance = GetResourcePerformance(totalGeneration[0], totalConsumption[0] + constructorConsumption[0]);
            if (projectedMetalStorage < 0)
                metalPerformance = GetResourcePerformance(totalGeneration[1], totalConsumption[1] + constructorConsumption[1]);

            //get the lowest performing resource
            int lowestPerformanceIndex=0;
            currentNetworkPerformance = energyPerformance;
            if (metalPerformance < energyPerformance)
            {
                lowestPerformanceIndex = 1;
                currentNetworkPerformance = metalPerformance;
            }
                

            

            //Modify performance based on the lowest performing resource
            //First, use any storage that may be left and get the remainder
            //Second, check is the constructor consumption will satisfy the last variance, drop performance accordingly
            //If that isn't enough, then drop performance of other buildings consuming the resource
            float[] storageVariance = new float[2] { currentStorage[0], currentStorage[1] };

            storageVariance[lowestPerformanceIndex] += totalVariance[lowestPerformanceIndex];
            //Get the final projected variance of the lower performer, this will be used to determine performance altering calculations
            float absFinalVariance = Math.Abs(storageVariance[lowestPerformanceIndex]);

            //Modify constructor performance to attempt to satisfy the variance
            if (constructorConsumption[lowestPerformanceIndex] >= absFinalVariance && constructorConsumption[lowestPerformanceIndex] > 0)
            {
                //ISSUE WITH MATHS HERE -- Constructor component is not reducing enough performance to balance consumption
                //Variance was -8, only dropped consumption by -4
                float constructorPerformance = (totalGeneration[lowestPerformanceIndex] - totalConsumption[lowestPerformanceIndex]) / constructorConsumption[lowestPerformanceIndex];
                foreach (ConstructorComponent component in constructorComponentsInNetwork)
                {
                    float[] consumptionVariance = component.SetNewPerformance(constructorPerformance);
                    constructorConsumption[0] += consumptionVariance[0];
                    constructorConsumption[1] += consumptionVariance[1];
                }
                foreach (SubNetworkController network in subNetworks)
                {
                    foreach (ConstructorComponent component in network.constructorComponentsInNetwork)
                    {
                        float[] consumptionVariance = component.SetNewPerformance(constructorPerformance);
                        constructorConsumption[0] += consumptionVariance[0];
                        constructorConsumption[1] += consumptionVariance[1];
                    }
                }
                if (Math.Abs(constructorConsumption[0]) < 0.3f) { constructorConsumption[0] = 0; }
                if (Math.Abs(constructorConsumption[1]) < 0.3f) { constructorConsumption[1] = 0; }
                absFinalVariance = 0;
            }
            else
            {
                foreach (ConstructorComponent component in constructorComponentsInNetwork)
                {
                    component.SetNewPerformance(0);
                }
                foreach (SubNetworkController network in subNetworks)
                {
                    foreach (ConstructorComponent component in network.constructorComponentsInNetwork)
                    {
                        component.SetNewPerformance(0);
                    }
                }
                absFinalVariance -= constructorConsumption[lowestPerformanceIndex];
                constructorConsumption[0] = 0;
                constructorConsumption[1] = 0;
            }

            //Check if there still is any variance (ie. Constructors were not enough)
            if (absFinalVariance > 0)
            {
                //Check what projected performance there will need to be for the remaining buildings
                float newPerformanceCheck = GetResourcePerformance(totalGeneration[lowestPerformanceIndex], totalConsumption[lowestPerformanceIndex]);

                foreach(ResourceComponent component in resourceComponentsInNetwork)
                {
                    float consumptionCheck;
                    if (lowestPerformanceIndex == 0) consumptionCheck = component.ConEnergy; else consumptionCheck = component.ConMetal;
                    if (consumptionCheck > 0)
                    {
                        ResourcePerformanceValue resourceChange = component.SetNewPerformance(component.GetCurrentPerformance() * newPerformanceCheck);
                        totalGeneration[0] += resourceChange.genEnergy;
                        totalGeneration[1] += resourceChange.genMetal;
                        totalConsumption[0] += resourceChange.conEnergy;
                        totalConsumption[1] += resourceChange.conMetal;
                    }
                }
                foreach (SubNetworkController network in subNetworks)
                {
                    foreach (ResourceComponent component in network.resourceComponentsInNetwork)
                    {
                        float consumptionCheck;
                        if (lowestPerformanceIndex == 0) consumptionCheck = component.ConEnergy; else consumptionCheck = component.ConMetal;
                        if (consumptionCheck > 0)
                        {
                            ResourcePerformanceValue resourceChange = component.SetNewPerformance(component.GetCurrentPerformance() * newPerformanceCheck);
                            totalGeneration[0] += resourceChange.genEnergy;
                            totalGeneration[1] += resourceChange.genMetal;
                            totalConsumption[0] += resourceChange.conEnergy;
                            totalConsumption[1] += resourceChange.conMetal;
                        }
                    }
                }
            }

        } //End of resource deficit check

        //Recalculate the total variance based on the new values that may have been modified above and modify the storage accordingly
        float[] maxStorage = new float[2];
        for (int i = 0; i < 2; i++)
        {
            totalVariance[i] = totalGeneration[i] - totalConsumption[i] - constructorConsumption[i];
            totalVariance[i] *= processTickNext;
            currentStorage[i] += totalVariance[i];
            maxStorage[i] = GetTotalStorage(i);
            if (currentStorage[i] > maxStorage[i]) { currentStorage[i] = maxStorage[i]; }
        }

        //update the UI tick values
        UpdateTickValues();
    }

    public bool IsNetworkOnline()
    {
        return attachedBuilding.IsBuildingOnline();
    }

    //UI Tick Value functions
    public int[] GetCurrentTickValues()
    {
        return UITickResults;
    }
    public void UpdateTickValues()
    {
        UITickResults[0] = (int)currentStorage[0];
        UITickResults[1] = (int)GetTotalStorage(0);
        UITickResults[2] = (int)(totalVariance[0] / processTickNext);
        UITickResults[3] = (int)currentStorage[1];
        UITickResults[4] = (int)GetTotalStorage(1);
        UITickResults[5] = (int)(totalVariance[1] / processTickNext);
        UITickResults[6] = (int)(currentNetworkPerformance*100);
    }


    //Building->Network related functions
    public void AddResourceComponentToNetwork(ResourceComponent component)
    {
        resourceComponentsInNetwork.Add(component);
    }
    public void RemoveResourceComponentFromNetwork(ResourceComponent component)
    {
        resourceComponentsInNetwork.Remove(component);
    }
    public void AddConstructorComponentToNetwork(ConstructorComponent component)
    {
        constructorComponentsInNetwork.Add(component);
    }
    public void RemoveConstructorComponentFromNetwork(ConstructorComponent component)
    {
        constructorComponentsInNetwork.Remove(component);
    }
    public void AddSubNetwork(SubNetworkController subNetworkController)
    {
        subNetworks.Add(subNetworkController);
    }
    public void RemoveSubNetwork(SubNetworkController subNetworkController)
    {
        subNetworks.Remove(subNetworkController);
    }
    public int GetNetworkFaction()
    {
        return networkFaction.faction;
    }
    public List<ResourceComponent> GetResourceComponentList()
    {
        return resourceComponentsInNetwork;
    }
    public List<ConstructorComponent> GetConstructorComponentList()
    {
        return constructorComponentsInNetwork;
    }
    public List<SubNetworkController> GetSubNetworks()
    {
        return subNetworks;
    }

    //Resource-related functions ---
    public float GetResourcePerformance(float generation, float consumption)
    {
        if (consumption > 0)
        {
            return (generation / consumption);
        }
        else
        {
            return 1;
        }
    }
    public float GetTotalStorage(int resourceIndex)
    {
        float totalStorage = 0;
        foreach (var component in resourceComponentsInNetwork)
        {
            if (resourceIndex == 0) { totalStorage += component.StoEnergy; }
            if (resourceIndex == 1) { totalStorage += component.StoMetal; }
        }
        foreach (SubNetworkController controller in subNetworks)
        {
            float[] networkInfo = controller.GetResourceComponentInfo();

            if (resourceIndex == 0) totalStorage += networkInfo[4];
            if (resourceIndex == 1) totalStorage += networkInfo[5];
        }

        return totalStorage;
    }
    public float[] GetCurrentStorage()
    {
        return currentStorage;
    }
    public void AddToStorage(int index, float value)
    {
        currentStorage[index] += value;
    }
    public void RemoveFromStorage(int index, float value)
    {
        currentStorage[index] -= value;
    }

    public void OnNetworkDeath()
    {
        foreach (var component in resourceComponentsInNetwork)
        {
            component.isAttachedToNetwork = false;
        }
        foreach (var component in constructorComponentsInNetwork)
        {
            component.isAttachedToNetwork = false;
        }
    }
    public void OnComponentExited(Area2D area)
    {

    }
}
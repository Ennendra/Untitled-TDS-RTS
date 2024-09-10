using Godot;
using System;
using System.Collections.Generic;


/// <summary>
/// This class manages and tracks the resource management for a team and tracks all combatants the team has.
/// If the player is part of this team, it marks the player in the team which marks this class for the UI to track resources
/// </summary>
public partial class FactionController
{

    //
    // - Resource-related variables
    //
    protected float processTickTimer = 0;
    protected float processTicksPerSecond = 10;
    protected float processTickNext { get { return 1 / processTicksPerSecond; } }
    //The total storage of the network (0=energy, 1=metal)
    protected float[] currentStorage = new float[2] { 0, 0 };
    protected int[] UITickResults = new int[7];
    protected float[] totalGeneration = new float[3] { 0, 0, 0 };
    protected float[] totalConsumption = new float[3] { 0, 0, 0 };
    protected float[] totalVariance = new float[3] { 0, 0, 0 };
    protected float currentNetworkPerformance = 1;

    //Trackers for all buildings and units belonging to this faction
    List<BuildingParent> buildings = new();
    List<BlueprintParent> blueprints = new();
    List<UnitParent> units = new();
    //The player itself, if it is part of this faction
    Player playerUnit;


    //Resource-related functions ---
    public void InitiateResourceProcessTick()
    {
        //initiate the list of all resource and constructor components this faction is currently using.
        List<ResourceComponent> activeResourceComponents = new();
        List<ConstructorComponent> activeConstructorComponents = new();

        //Tally up all active resource and constructor components
        foreach (BuildingParent building in buildings)
        {
            if (building.IsBuildingOnline())
            {
                if (building.GetResourceComponent() != null) { activeResourceComponents.Add(building.GetResourceComponent()); }
                if (building.GetConstructorComponent() != null) { activeConstructorComponents.Add(building.GetConstructorComponent()); }
            }
        }
        foreach (UnitParent unit in units)
        {
                if (unit.GetResourceComponent() != null) { activeResourceComponents.Add(unit.GetResourceComponent()); }
        }
        if (playerUnit != null)
        {
            activeResourceComponents.AddRange(playerUnit.GetResourceComponents());
            activeConstructorComponents.AddRange(playerUnit.GetConstructorComponents());
        }


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
        foreach (ResourceComponent component in activeResourceComponents)
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
        foreach (ConstructorComponent component in activeConstructorComponents)
        {
            component.SetNewPerformance(1);
            constructorConsumption[0] += component.GetEnergyRate();
            constructorConsumption[1] += component.GetMetalRate();
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
            int lowestPerformanceIndex = 0;
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
                foreach (ConstructorComponent component in activeConstructorComponents)
                {
                    float[] consumptionVariance = component.SetNewPerformance(constructorPerformance);
                    constructorConsumption[0] += consumptionVariance[0];
                    constructorConsumption[1] += consumptionVariance[1];
                }
                if (Math.Abs(constructorConsumption[0]) < 0.3f) { constructorConsumption[0] = 0; }
                if (Math.Abs(constructorConsumption[1]) < 0.3f) { constructorConsumption[1] = 0; }
                absFinalVariance = 0;
            }
            else
            {
                foreach (ConstructorComponent component in activeConstructorComponents)
                {
                    component.SetNewPerformance(0);
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

                foreach (ResourceComponent component in activeResourceComponents)
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
        UpdateResourceTickValues();
    }
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
        //initiate the list of all resource and constructor components this faction is currently using.
        List<ResourceComponent> activeResourceComponents = new();

        //Tally up all active resource and constructor components
        foreach (BuildingParent building in buildings)
        {
            if (building.IsBuildingOnline() && building.GetResourceComponent() != null)
              activeResourceComponents.Add(building.GetResourceComponent());
        }
        foreach (UnitParent unit in units)
        {
            if (unit.GetResourceComponent() != null) { activeResourceComponents.Add(unit.GetResourceComponent()); }
        }
        if (playerUnit != null)
        {
            activeResourceComponents.AddRange(playerUnit.GetResourceComponents());
        }
        foreach (var component in activeResourceComponents)
        {
            
            if (resourceIndex == 0) { totalStorage += component.StoEnergy; }
            if (resourceIndex == 1) { totalStorage += component.StoMetal; }
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

    //UI Tick Value functions
    public void UpdateResourceTickValues()
    {
        UITickResults[0] = (int)currentStorage[0];
        UITickResults[1] = (int)GetTotalStorage(0);
        UITickResults[2] = (int)(totalVariance[0] / processTickNext);
        UITickResults[3] = (int)currentStorage[1];
        UITickResults[4] = (int)GetTotalStorage(1);
        UITickResults[5] = (int)(totalVariance[1] / processTickNext);
        UITickResults[6] = (int)(currentNetworkPerformance * 100);
    }
    public int[] GetCurrentTickValues()
    {
        return UITickResults;
    }

    //List manipulation functions
    public void AddBuilding(BuildingParent building)
    {
        buildings.Add(building);
    }
    public void RemoveBuilding(BuildingParent building)
    {
        buildings.Remove(building);
    }
    public void AddBlueprint(BlueprintParent blueprint)
    {
        blueprints.Add(blueprint);
    }
    public void RemoveBlueprint(BlueprintParent blueprint)
    {
        blueprints.Remove(blueprint);
    }
    public void AddUnit(UnitParent unit)
    {
        units.Add(unit);
    }
    public void RemoveUnit(UnitParent unit)
    {
        units.Remove(unit);
    }
    public void AddPlayer(Player player)
    {
        playerUnit = player;
    }
    public Player GetPlayer()
    {
        if (playerUnit != null) 
            return playerUnit;
        else
        {
            GD.Print("Error: No player unit in this faction controller!");
            return null;
        }
    }
    public int GetPlayerFaction()
    {
        if (playerUnit != null)
            return playerUnit.factionComponent.faction;
        else
        {
            GD.Print("Error: No player unit in this faction controller when checking for faction number!");
            return -1;
        }
    }

    //Function to get the minimap marker component from all units and buildings
    public void SetAllFactionMinimapMarkerComponents(MinimapMarkerTag buildingTag, MinimapMarkerTag blueprintTag, MinimapMarkerTag unitTag)
    {
        foreach(BuildingParent building in buildings)
        {
            building.markerComponent.markerTag = buildingTag;
        }
        foreach (BlueprintParent blueprint in blueprints)
        {
            blueprint.markerComponent.markerTag = blueprintTag;
        }
        foreach (UnitParent unit in units)
        {
            unit.markerComponent.markerTag = unitTag;
        }

    }

    //A separate process function that is run within the MainLevelController's _Process function
    public void Process(double delta)
    {
        processTickTimer += (float)delta;
        if (processTickTimer > processTickNext)
        {
            InitiateResourceProcessTick();
            processTickTimer -= processTickNext;
        }
    }

}

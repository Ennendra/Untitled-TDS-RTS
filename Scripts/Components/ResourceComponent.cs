using Godot;
using System;

public struct ResourcePerformanceValue
{
    public float genEnergy, genMetal, genRawMetal;
    public float conEnergy, conMetal, conRawMetal;

    public ResourcePerformanceValue(float genEnergy, float genMetal, float genRawMetal, float conEnergy, float conMetal, float conRawMetal)
    {
        this.genEnergy = genEnergy;
        this.genMetal = genMetal;
        this.genRawMetal = genRawMetal;
        this.conEnergy = conEnergy;
        this.conMetal = conMetal;
        this.conRawMetal = conRawMetal;
    }
}

public partial class ResourceComponent : Area2D
{
    //An attached faction component, to help base controllers track what faction we are in
    [Export] public FactionComponent factionComponent;

    //Resource-tracking variables (tracking in "per second")
    //gen = Passive Generation, sto = Storage, con = Passive Consumption
    [Export] float genEnergy;
    [Export] float genMetal;
    [Export] float genRawMetal;
    [Export] float stoEnergy;
    [Export] float stoMetal;
    [Export] float conEnergy;
    [Export] float conMetal;
    [Export] float conRawMetal;
    public float GenEnergy { get => genEnergy * currentPerformance; protected set => genEnergy=value; }
    public float GenMetal { get => genMetal * currentPerformance; protected set => genMetal = value; }
    public float GenRawMetal { get => genRawMetal * currentPerformance; protected set => genRawMetal = value; }
    public float StoEnergy { get => stoEnergy; protected set => stoEnergy = value; }
    public float StoMetal { get => stoMetal; protected set => stoMetal = value; }
    public float ConEnergy { get => conEnergy * currentPerformance; protected set => conEnergy = value; }
    public float ConMetal { get => conMetal * currentPerformance; protected set => conMetal = value; }
    public float ConRawMetal { get => conRawMetal * currentPerformance; protected set => conRawMetal = value; }
    public float CurrentStoEnergy { get; protected set; }
    public float CurrentStoMetal { get; protected set; }
    float currentPerformance = 1;

    //Whether we are attached to a network
    public bool isAttachedToNetwork = false;

    //The bonus from adjacent buildings in percentage (can be production bonus or consumption reduction)
    //float energyAdjacencyBonus = 0, metalAdjacencyBonus = 0;
    //[Export] float maxEnergyAdjacencyBonus = 20, maxMetalAdjacencyBonus = 20;

    public float GetCurrentPerformance()
    {
        return currentPerformance;
    }
    public ResourcePerformanceValue SetNewPerformance(float newPerformance)
    {
        ResourcePerformanceValue beforePerformance = new ResourcePerformanceValue(GenEnergy, GenMetal, GenRawMetal, ConEnergy, ConMetal, ConRawMetal);
        currentPerformance = newPerformance;
        ResourcePerformanceValue afterPerformance = new ResourcePerformanceValue(GenEnergy, GenMetal, GenRawMetal, ConEnergy, ConMetal, ConRawMetal);

        ResourcePerformanceValue performanceVariance = new ResourcePerformanceValue(
            afterPerformance.genEnergy - beforePerformance.genEnergy,
            afterPerformance.genMetal - beforePerformance.genMetal,
            afterPerformance.genRawMetal - beforePerformance.genRawMetal,
            afterPerformance.conEnergy - beforePerformance.conEnergy,
            afterPerformance.conMetal - beforePerformance.conMetal,
            afterPerformance.conRawMetal - beforePerformance.conRawMetal
            );

        return performanceVariance;
    }
    public void SetCurrentEnergyStorage(float value)
    {
        CurrentStoEnergy = Mathf.Clamp(value, 0, StoEnergy);
    }
    public void SetCurrentMetalStorage(float value)
    {
        CurrentStoMetal = Mathf.Clamp(value, 0, StoMetal);
    }

    //Functions to set new generation values (used for the reclaimer tool)
    public void SetNewGenerationValues(float[] amount)
    {
        GenEnergy = amount[0];
        GenMetal = amount[1];
    }

    //Signal functions
    public void OnNetworkEntered(Area2D area)
    {
        //Add this component to the network if it isn't already in one.
        if (!isAttachedToNetwork)
        {
            BaseNetworkController controller = (BaseNetworkController)area;
            isAttachedToNetwork = true;
            controller.AddResourceComponentToNetwork(this);
            if (GetParent().IsInGroup("Player"))
                GD.Print("Player Resource Component attached");
        }
        
        
    }
    public void OnNetworkExited(Area2D area)
    {
        
        BaseNetworkController controller = (BaseNetworkController)area;

        if (controller.GetResourceComponentList().Contains(this))
        {
            isAttachedToNetwork = false;
            controller.RemoveResourceComponentFromNetwork(this);
        }
        
    }
}

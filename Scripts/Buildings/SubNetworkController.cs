using Godot;
using System;

//This class is to be used with mobile entities that have network capabilities (ie. the player)
//It is aimed to run like a regular network, UNLESS it is within another full network, in which it will take all details from this network and pass them to the main network
public partial class SubNetworkController : BaseNetworkController
{
    public bool isAttachedToNetwork { get; protected set; } = false;

    public override void _Ready()
    {
        isMainNetwork = false;

        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (!isAttachedToNetwork)
        {
            processTickTimer += (float)delta;
            if (processTickTimer > processTickNext)
            {
                InitiateProcessTick();
                processTickTimer -= processTickNext;
            }
        }
    }

    //Gets the resource info from all constructor and resource components attached to this sub network to be given to the main network
    //0 - Energy Generation
    //1 - Metal Generation
    //2 - Energy Consumption
    //3 - Metal Consumption
    //4 - Energy Storage
    //5 - Metal Storage
    public float[] GetResourceComponentInfo()
    {
        float[] totalInfo = new float[6] { 0, 0, 0, 0, 0, 0 };
        foreach (ResourceComponent component in resourceComponentsInNetwork)
        {
            totalInfo[0] += component.GenEnergy;
            totalInfo[1] += component.GenMetal;
            totalInfo[2] += component.ConEnergy;
            totalInfo[3] += component.ConMetal;
            totalInfo[4] += component.StoEnergy;
            totalInfo[5] += component.StoMetal;
        }

        return totalInfo;
    }
    public float[] GetConstructorComponentInfo()
    {
        float[] totalInfo = new float[2] { 0, 0 };
        foreach (ConstructorComponent component in constructorComponentsInNetwork)
        {
            component.SetNewPerformance(1);
            totalInfo[0] += component.GetEnergyRate();
            totalInfo[1] += component.GetMetalRate();
        }
        return totalInfo;
    }

    public void OnNetworkEntered(Area2D area)
    {
        if (!isAttachedToNetwork)
        {
            BaseNetworkController controller = (BaseNetworkController)area;
            isAttachedToNetwork = true;
            controller.AddSubNetwork(this);
            if (GetParent().IsInGroup("Player"))
                GetParent().EmitSignal("NetworkEntered", controller);
        }
    }
    public void OnNetworkExited(Area2D area)
    {
        BaseNetworkController controller = (BaseNetworkController)area;

        //remove self from the network only if this component is part of that specific network
        if (controller.GetSubNetworks().Contains(this))
        {
            isAttachedToNetwork = false;
            controller.RemoveSubNetwork(this);
            if (GetParent().IsInGroup("Player"))
                GetParent().EmitSignal("NetworkExited", controller);
        }

    }
}

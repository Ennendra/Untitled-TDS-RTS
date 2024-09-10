using Godot;
using System;

public partial class UnitParent : Area2D
{

    [Export] Sprite2D mainSprite;
    [Export] DamageComponent damageComponent;
    [Export] AimingComponent aimComponent;
    [Export] MovementComponent movementComponent;
    [Export] FactionComponent factionComponent;
    [Export] ResourceComponent resourceComponent;
    [Export] AIComponent aiComponent;
    public MinimapMarkerComponent markerComponent { get; protected set; }
    //Used to set the faction of the unit in a level editor. 0 = no override/default value
    [Export] int factionOverride = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        markerComponent = GetNode<MinimapMarkerComponent>("MinimapMarkerComponent");

        factionComponent.AddWeapon(aimComponent.equippedWeapon);
        if (factionOverride != 0)
        {
            factionComponent.faction = factionOverride;
        }
        

        AddUserSignal("OnDamageKill");
        Connect("OnDamageKill", new Callable(this, "OnDamageKill"));
        AddUserSignal("OnReclaimKill");
        Connect("OnReclaimKill", new Callable(this, "OnReclaimKill"));

        damageComponent.SetHealthbarOffset(GetUnitRadius());
        damageComponent.toolRangeGrace = GetUnitRadius();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public int GetCurrentFaction()
    {
        return factionComponent.faction;
    }
    public DamageComponent GetDamageComponent()
    {
        return damageComponent;
    }
    public ResourceComponent GetResourceComponent()
    {
        if (IsInstanceValid(resourceComponent)) return resourceComponent;
        else return null;
    }
    public AIComponent GetAIComponent()
    {
        if (IsInstanceValid(aiComponent)) return aiComponent;
        else return null;
    }
    public FactionComponent GetFactionComponent()
    {
        return factionComponent;
    }
    public void SetReclaimValue(float energy, float metal)
    {
        damageComponent.SetReclaimValue(energy, metal);
    }
    public void SetNewFaction(int faction)
    {
        factionComponent.faction = faction;
    }
    public float GetUnitRadius()
    {
        return mainSprite.Texture.GetHeight() / 2;
    }

    public void SetInitialDirection(float directionDegrees)
    {
        movementComponent.SetMoveDirection(directionDegrees);
        aimComponent.SetCurrentAimDirection(directionDegrees);
    }

    public void OnDamageKill()
    {
        QueueFree();
    }
    public void OnReclaimKill()
    {
        QueueFree();
    }


    //Functions related to sending orders to the units
    public void SetMoveOrder(Vector2 targetPosition)
    {
        aiComponent.SetNewMoveOrder(targetPosition);
    }
    public void SetAttackOrder(FactionComponent target)
    {
        aiComponent.SetNewAttackOrder(target);
    }
}

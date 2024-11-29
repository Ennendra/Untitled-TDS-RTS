using Godot;
using System;
using System.Runtime;

//This class embodies every unit and structure on the map
public partial class CombatantParent : CharacterBody2D
{
    public MinimapMarkerComponent markerComponent { get; protected set; }

    [ExportCategory("Unit Information Resource")]
    [Export] protected UnitInfo unitInfo;

    [ExportCategory("Death Items")]
    [Export] protected PackedScene deathExplosion;

    [ExportCategory("Components")]
    [Export] protected Sprite2D mainSprite;
    [Export] protected DamageComponent damageComponent;
    [Export] protected AimingComponent aimComponent;
    [Export] protected AIComponent aiComponent;
    [Export] protected FactionComponent factionComponent;
    [Export] protected MovementComponent movementComponent;
    [Export] protected ResourceComponent resourceComponent;
    [Export] protected FOWSightComponent sightComponent;

    [ExportCategory("Faction Definition")]
    [Export] int factionOverride = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        //Define common components
        markerComponent = GetNode<MinimapMarkerComponent>("MinimapMarkerComponent");

        //Equip a weapon if applicable
        if (IsInstanceValid(aimComponent)) { factionComponent.AddWeapon(aimComponent.equippedWeapon); }

        //Set the new unit faction if applicable
        if (factionOverride != 0) { factionComponent.faction = factionOverride; }

        //Define damagecomponent values
        damageComponent.SetReclaimValue(unitInfo.energyCost, unitInfo.metalCost);
        damageComponent.SetMaxHealth(unitInfo.maxHealth);
        damageComponent.SetHealthPercentage(100);
        damageComponent.SetHealthbarOffset(GetUnitRadius());
        damageComponent.toolRangeGrace = GetUnitRadius();

        //Set signals
        AddUserSignal("OnDamageKill");
        Connect("OnDamageKill", new Callable(this, "OnDamageKill"));
        AddUserSignal("OnReclaimKill");
        Connect("OnReclaimKill", new Callable(this, "OnReclaimKill"));
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

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
    public MovementComponent GetMovementComponent() { return movementComponent; }
    public DamageComponent GetDamageComponent()
    {
        return damageComponent;
    }
    public FactionComponent GetFactionComponent()
    {
        return factionComponent;
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
    public FOWSightComponent GetSightComponent()
    {
        return sightComponent;
    }

    public void SetReclaimValue(float energy, float metal)
    {
        damageComponent.SetReclaimValue(energy, metal);
    }

    public float GetUnitRadius()
    {
        return mainSprite.Texture.GetHeight() / 2;
    }
    //Creating explosion on death
    public void CreateExplosion()
    {
        if (deathExplosion != null)
        {
            var explosion = deathExplosion.Instantiate() as Explosion;
            GetTree().CurrentScene.AddChild(explosion);
            explosion.GlobalPosition = GlobalPosition;
            explosion.GlobalRotation = GD.Randf() * 6.3f; //using radians, this is approximately 360 degrees with a bit extra
        }
    }

    //Signal functions
    public void OnDamageKill()
    {
        CreateExplosion();
        QueueFree();
    }
    public void OnReclaimKill()
    {
        QueueFree();
    }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Runtime;

//This class embodies every unit and structure on the map
public partial class CombatantParent : CharacterBody2D
{
    public MinimapMarkerComponent markerComponent { get; protected set; }

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

    //Signal for responding to damage from an attacker
    [Signal] public delegate void AttackResponseEventHandler(Node2D attackSource);

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

        CallDeferred("SetUnitInfo");
        damageComponent.SetHealthbarOffset(GetUnitRadius());
        damageComponent.toolRangeGrace = GetUnitRadius();

        //Set signals
        AddUserSignal("OnDamageKill");
        Connect("OnDamageKill", new Callable(this, "OnDamageKill"));
        AddUserSignal("OnReclaimKill");
        Connect("OnReclaimKill", new Callable(this, "OnReclaimKill"));

        AttackResponse += RespondToAttack;
    }

    public void SetUnitInfo()
    {
        damageComponent.SetReclaimValue(factionComponent.unitInfo.energyCost, factionComponent.unitInfo.metalCost);
        damageComponent.SetMaxHealth(factionComponent.unitInfo.maxHealth);
        damageComponent.SetHealthPercentage(100);
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
        GD.Print("New Faction: "+newFaction);
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
    
    public void RespondToAttack(Node2D attackSource)
    {
        if (IsInstanceValid(attackSource))
        {
            CircleShape2D shape = new();
            shape.Radius = 400;

            var spaceState = GetWorld2D().DirectSpaceState;
            PhysicsShapeQueryParameters2D areaCast = new();
            areaCast.CollideWithAreas = true; //set collide with areas to true, so it will register building areas
            areaCast.Shape = shape;
            areaCast.CollisionMask = factionComponent.CollisionLayer; //For marking any allies of this unit
            areaCast.Transform = new Transform2D(0, this.GlobalPosition);

            List<FactionComponent> alliesInRange = new();
            //Run the collision check
            var collisionResult = spaceState.IntersectShape(areaCast, 100);
            if (collisionResult.Count > 0)
            {
                //Cast each hit to a faction component and add to the list
                foreach (var collision in collisionResult)
                {
                    Variant collidedObject;
                    bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                    if (colliderCheck)
                    {
                        FactionComponent newComponent = (FactionComponent)collidedObject;
                        if (IsInstanceValid(newComponent.GetAIComponent())) //Only check with units that can respond to the attack
                        {
                            //Only respond if the unit is idle
                            if (newComponent.GetAIComponent().unitState == AIUnitState.IDLE)
                            {
                                newComponent.GetAIComponent().SetNewAttackMoveOrder(attackSource.GlobalPosition);
                            }
                        
                        }
                    }
                }
            }
        }
    }
}

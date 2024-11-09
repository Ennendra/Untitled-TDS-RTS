using Godot;
using System;

public enum DamageType
{
	COMBATDAMAGE,
	RECLAIMING
}

public partial class DamageComponent : Area2D
{
	//These can be set via construct info when creating the unit
	public float energyReclaimValue=10, metalReclaimValue=10;

	//Healthbar variables
	[Export] ProgressBar healthBar;
	[Export] float healthBarOffset; //Y-position offset
	[Export] bool healthBarDisplayed = true; //This will be false for the player and for environment/wreckages, which healthbars are not necessary for
	float timeSinceLastDamage = 10; //Used to hide the healthbar when the object hasn't taken damage in a while
	public float toolRangeGrace = 50;

	public float maxHealth { get; private set; }
	float health;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        health = maxHealth;

		healthBar.MaxValue = maxHealth;
		healthBar.Value = health;

        //SetHealthbarOffset(healthBarOffset);
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timeSinceLastDamage += (float)delta;

		if (timeSinceLastDamage > 5) 
			{ healthBar.Visible = false; }
		else if (healthBarDisplayed)
			{ healthBar.Visible = true; }
    }

    //Misc functions ---
    public float GetCurrentHealthPercent()
	{
		return health / maxHealth * 100;
	}
    public float[] GetEnergyMetalRatio()
    {
        float[] ratio = new float[2] { 1, 1 };
        float energyAmount, metalAmount;
        if (metalReclaimValue > 0) { energyAmount = energyReclaimValue / metalReclaimValue; } else { energyAmount = 1; }
        if (energyReclaimValue > 0) { metalAmount = metalReclaimValue / energyReclaimValue; } else { metalAmount = 1; }

        if (energyAmount < 1) { ratio[0] = energyAmount; }
        if (metalAmount < 1) { ratio[1] = metalAmount; }

        return ratio;
    }
	public void SetReclaimValue(float energy, float metal)
	{
		energyReclaimValue = energy;
		metalReclaimValue = metal;
	}

    //Layer and Mask manipulation functions ---
    public void SetCollisionLayers(int[] layers)
	{
		int layerResult = 0;
		foreach (int layer in layers)
		{
			layerResult += (int)Math.Pow(2, layer - 1);
		}

		CollisionLayer = (uint)layerResult;
	}
    public void SetCollisionMasks(int[] masks)
    {
        int maskResult = 0;
        foreach (int mask in masks)
        {
            maskResult += (int)Math.Pow(2, mask - 1);
        }

        CollisionMask = (uint)maskResult;
    }

    //health manipulation functions ---
	public void SetHealthbarOffset(float amount)
	{
		healthBar.Position = new Vector2(-40,amount);
	}
	public void SetMaxHealth(float amount)
	{
		maxHealth = amount;
		if (health > maxHealth) {  health = maxHealth; }
        healthBar.MaxValue = maxHealth;
        healthBar.Value = health;
    }
	public void SetHealthPercentage(float percentage)
	{
		health = maxHealth / 100 * percentage;
        healthBar.Value = health;
		//timeSinceLastDamage = 0;
    }
    public void TakeDamage(float amount, DamageType type)
	{
		health -= amount;
		healthBar.Value = health;
        timeSinceLastDamage = 0;
        if (health <= 0)
		{
			health = 0;
			TriggerDeath(type);
		}
	}
	public void HealDamage(float amount)
	{
		health += amount;
        healthBar.Value = health;
        timeSinceLastDamage = 0;
        if (health > maxHealth) health = maxHealth;
	}

	//Death function
	public void TriggerDeath(DamageType type)
	{
		if (type == DamageType.COMBATDAMAGE)
			GetParent().EmitSignal("OnDamageKill");
		else
            GetParent().EmitSignal("OnReclaimKill");
    }
}

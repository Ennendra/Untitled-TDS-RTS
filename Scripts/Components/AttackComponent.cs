using Godot;
using System;

public partial class AttackComponent : Area2D
{
	float damage;

	//Damage functions ---
	public void SetDamage(float newDamage)
	{
		damage = newDamage;
	}
	public void DealDamage(DamageComponent componentToDamage)
	{
		componentToDamage.TakeDamage(damage, DamageType.COMBATDAMAGE);
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
        //start result at 1, since we want it to react to general environment obstacles
        int maskResult = 1;

        foreach (int mask in masks)
        {
            int nextMask = (int)Math.Pow(2, mask - 1);
            maskResult += nextMask;
        }

        CollisionMask = (uint)maskResult;
    }
    //Collision function ---
    public void OnAreaEntered(Area2D area)
	{
		if (area.IsInGroup("DamageComponent"))
		{
			DamageComponent component = (DamageComponent)area;

			DealDamage(component);
		}
        GetParent().EmitSignal("ProjectileHit");
    }
}

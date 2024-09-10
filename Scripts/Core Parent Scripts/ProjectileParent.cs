using Godot;
using System;

public partial class ProjectileParent : Area2D
{
	[Export] AttackComponent attackComponent;

    [Export] float projectileSpeed;
    float damage;
	float distanceTravelled, maxDistance;
    bool isDirectFire;

    public override void _Ready()
    {
        base._Ready();

        AddUserSignal("ProjectileHit");
        Connect("ProjectileHit", new Callable(this, "OnProjectileHit"));
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
        base._Process(delta);


	}

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

		Vector2 moveVector = Transform.X * projectileSpeed * (float)delta;

        //cast a ray to see if it hits anything in its path
        var spaceState = GetWorld2D().DirectSpaceState;
        var cast = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + moveVector, attackComponent.CollisionMask);
        cast.CollideWithAreas = true; //set collide with areas to true, so it will register hitting a damagebox
        cast.CollisionMask = attackComponent.CollisionMask;
        var collisionResult = spaceState.IntersectRay(cast);

        if (collisionResult.Count > 0)
        {
            //set the position at the point of contact
            GlobalPosition = (Vector2)collisionResult["position"];
        }
        else
        {
            //move the projectile forward based on speed
            GlobalPosition += moveVector;

            //process the distance and destroy the projectile if it's reached its limit
            distanceTravelled += moveVector.Length();
            if (distanceTravelled >= maxDistance)
            {
                DestroyProjectile();
            }
        }
    }

    public void SetProjectileStats(float damage, float range, bool isDirectFire)
    {
        this.damage = damage;
        maxDistance = range;
        this.isDirectFire = isDirectFire;

        attackComponent.SetDamage(damage);
    }

    public void OnProjectileHit()
    {
        DestroyProjectile();
    }

    public void DestroyProjectile()
    {
        QueueFree();
    }

    //collision alteration functions ---
    public void SetProjectileCollisions(int[] layer, int[] mask)
	{
		SetCollisionLayer(layer);
		SetCollisionMask(mask);
	}
	public void SetCollisionLayer(int[] newLayer)
	{
        attackComponent.SetCollisionLayers(newLayer);
	}
    public void SetCollisionMask(int[] newMask)
    {
        attackComponent.SetCollisionMasks(newMask);
    }
}

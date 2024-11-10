using Godot;
using System;

public partial class ProjectileParent : Area2D
{
    [ExportCategory("Projectile Explosion")]
    [Export] PackedScene explosionToInstance;

    [ExportCategory("Components")]
    [Export] AttackComponent attackComponent;

    [ExportCategory("ProjectileStats")]
    float projectileSpeed;
    float damage;
	float distanceTravelled, maxDistance;
    bool isDirectFire;

    Sprite2D mainSprite;

    public override void _Ready()
    {
        base._Ready();

        mainSprite = GetNode<Sprite2D>("Sprite");

        AddUserSignal("ProjectileHit");
        Connect("ProjectileHit", new Callable(this, "OnProjectileHit"));
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
    public void SetProjectileStats(float damage, float range, bool isDirectFire, float projectileSpeed)
    {
        this.damage = damage;
        maxDistance = range;
        this.isDirectFire = isDirectFire;
        this.projectileSpeed = projectileSpeed;

        attackComponent.SetDamage(damage);
    }
    public void SetProjectileTexture(Texture2D texture)
    {
        mainSprite.Texture = texture;
    }

    //Signals
    public void OnProjectileHit()
    {
        CreateProjectileExplosion();
        DestroyProjectile();
    }

    //Destroying the projectile
    public void CreateProjectileExplosion()
    {
        if (explosionToInstance != null)
        {
            var explosion = explosionToInstance.Instantiate() as Explosion;
            GetTree().CurrentScene.AddChild(explosion);
            explosion.GlobalPosition = GlobalPosition;
        }
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

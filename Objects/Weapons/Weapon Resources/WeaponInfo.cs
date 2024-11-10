using Godot;
using System;

public partial class WeaponInfo : Resource
{

    [Export] public float damage = 10; //damage per hit
    [Export] public float rateOfFire = 4; //How many shots this weapon can fire per second
    [Export] public float range = 400; //The maximum distance the projectile can travel
    [Export] public float scatter = 3; //How far in degrees the projectile can fly from the original direction
    [Export] public int shotsFired = 1; //How many projectiles are spawned every time the weapon fires
    [Export] public PackedScene projectileToSpawn; //The projectile object that will be spawned
    [Export] public bool isDirectFire = true; //Determines whether the projectile hits directly or will 'land' on a position (ie. artillery)
    [Export] public float projectileSpeed = 800; //How fast the projectile moves
    [Export] public float projectileSpeedVariance = 0; //How much the projectile speed can vary each way (useful for scatter weapons)
    [Export] public Texture2D projectileTexture; //Altering the base texture of the projectile (if applicable)

    public WeaponInfo() : this(10, 4, 400, 3, 1, null, true, 800, 0, null) { }

    public WeaponInfo(float damage, float rateOfFire, float range, float scatter, int shotsFired, PackedScene projectileToSpawn, bool isDirectFire, float projectileSpeed, float projectileSpeedVariance, Texture2D projectileTexture)
    {
        this.damage = damage;
        this.rateOfFire = rateOfFire;
        this.range = range;
        this.scatter = scatter;
        this.shotsFired = shotsFired;
        this.projectileToSpawn = projectileToSpawn;
        this.isDirectFire = isDirectFire;
        this.projectileSpeed = projectileSpeed;
        this.projectileSpeedVariance = projectileSpeedVariance;
    }
}
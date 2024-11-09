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

    public WeaponInfo() : this(10, 4, 400, 3, 1, null, true, 800, 0) { }

    public WeaponInfo(float vDamage, float vRateOfFire, float vRange, float vScatter, int vShotsFired, PackedScene vProjectileToSpawn, bool vIsDirectFire, float vProjectileSpeed, float vProjectileSpeedVariance)
    {
        damage = vDamage;
        rateOfFire = vRateOfFire;
        range = vRange;
        scatter = vScatter;
        shotsFired = vShotsFired;
        projectileToSpawn = vProjectileToSpawn;
        isDirectFire = vIsDirectFire;
        projectileSpeed = vProjectileSpeed;
        projectileSpeedVariance = vProjectileSpeedVariance;
    }
}
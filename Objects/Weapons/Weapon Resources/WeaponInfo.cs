using Godot;
using System;

public partial class WeaponInfo : Resource
{

    [Export] public float damage;
    [Export] public float rateOfFire;
    [Export] public float range;
    [Export] public float scatter;
    [Export] public int shotsFired;
    [Export] public PackedScene projectileToSpawn;
    [Export] public bool isDirectFire;

    public WeaponInfo() : this(10, 4, 400, 3, 1, null, true) { }

    public WeaponInfo(float vDamage, float vRateOfFire, float vRange, float vScatter, int vShotsFired, PackedScene vProjectileToSpawn, bool vIsDirectFire)
    {
        damage = vDamage;
        rateOfFire = vRateOfFire;
        range = vRange;
        scatter = vScatter;
        shotsFired = vShotsFired;
        projectileToSpawn = vProjectileToSpawn;
        isDirectFire = vIsDirectFire;
    }
}
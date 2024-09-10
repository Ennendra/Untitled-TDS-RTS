using Godot;
using System;
using System.Net;

enum WeaponFireType
{
    SINGLE,
    ALTERNATE,
    MULTIFIRE
}

public partial class WeaponParent : Node2D
{

    [Export] public Texture2D weaponIcon { get; private set; }

    //hold all the general stats like rate of fire, damage etc
    [Export] public WeaponInfo weapon { get; protected set; }
    [Export] WeaponFireType weaponFireType;
    //The array of nodes that will be used for their position of projectile spawns (Added as child nodes in the editor)
    Node2D[] weaponFirePoints;
    //Which of the weapons to fire (relevant for ALTERNATE fire)
    int weaponFireIndex = 0;

    //The WeaponInfo stats
    public float damage { get => weapon.damage; protected set => weapon.damage = value; }
    public float rateOfFire { get => weapon.rateOfFire; protected set => weapon.rateOfFire = value; }
    public float range { get => weapon.range; protected set => weapon.range = value; }
    public float scatter { get => weapon.scatter * (Mathf.Pi / 180); protected set => weapon.scatter = value; }
    int shotsFired { get => weapon.shotsFired; set => weapon.shotsFired = value; }
    PackedScene projectileToSpawn { get => weapon.projectileToSpawn; set => weapon.projectileToSpawn = value; }
    public bool isDirectFire { get => weapon.isDirectFire; protected set => weapon.isDirectFire = value; }

    //Checks if the weapon is equipped (normally only really necessary on the player)
    public bool isEquipped = true;
    //the time since it last fired
    float fireTime = 0;

    [Export] int[] attackCollisionLayers, attackCollisionMasks;

    public override void _Ready()
    {
        base._Ready();

        //Get the child nodes inside this node and add them to the weapon fire point array
        var childNodes = GetChildren();
        weaponFirePoints = new Node2D[childNodes.Count];
        for (int i = 0; i < weaponFirePoints.Length; i++)
        {
            weaponFirePoints[i] = (Node2D)childNodes[i];
        }
    }

    public override void _Process(double delta)
    {
        fireTime += (float)delta;
    }

    //Misc functions ---
    public float GetRateOfFireTime()
    {
        return 1 / rateOfFire;
    }

    //Collision manipulation functions (setting values to put onto projectiles)
    public void SetNewCollisionLayers(int[] newCollisionLayers)
    {
        attackCollisionLayers = newCollisionLayers;
    }
    public void SetNewCollisionMasks(int[] newCollisionMasks)
    {
        attackCollisionMasks = newCollisionMasks;
    }



    //Weapon Firing functions ---
    bool CanFire()
    {
        if (fireTime >= GetRateOfFireTime())
        {
            return true;
        }
        return false;
    }
    public bool FireWeapon()
    {
        if (CanFire())
        {
            switch (weaponFireType)
            {
                case WeaponFireType.SINGLE:
                    SpawnProjectiles(weaponFirePoints[0].GlobalPosition, GlobalRotation);
                    break;
                case WeaponFireType.ALTERNATE:
                    SpawnProjectiles(weaponFirePoints[weaponFireIndex].GlobalPosition, GlobalRotation);
                    weaponFireIndex++;
                    if (weaponFireIndex >= weaponFirePoints.Length)
                        weaponFireIndex = 0;
                    break;
                case WeaponFireType.MULTIFIRE:
                    for (int i = 0; i < weaponFirePoints.Length; i++)
                    {
                        SpawnProjectiles(weaponFirePoints[i].GlobalPosition, GlobalRotation);
                    }
                    break;
            }
            fireTime = 0;
            return true;
        }
        return false;
    }
    public void SpawnProjectiles(Vector2 spawnPosition, float spawnRotation)
    {
        float scatterAmount;
        for (int i = 0; i < shotsFired; i++)
        {
            scatterAmount = -scatter + (GD.Randf() * 2 * scatter);

            var newProjectile = projectileToSpawn.Instantiate() as ProjectileParent;
            GetTree().CurrentScene.AddChild(newProjectile);

            newProjectile.GlobalPosition = spawnPosition;
            newProjectile.Rotation = spawnRotation + scatterAmount;


            newProjectile.SetProjectileStats(damage, range, isDirectFire);
            //Set the projectile collision layers and masks
            newProjectile.CallDeferred("SetProjectileCollisions", attackCollisionLayers, attackCollisionMasks);
        }
    }

}

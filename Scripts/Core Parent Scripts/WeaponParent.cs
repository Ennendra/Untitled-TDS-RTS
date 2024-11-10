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
    [ExportCategory("UI References")]
    [Export] public Texture2D weaponIcon { get; private set; }

    [ExportCategory("Weapon Stats")]
    //hold all the general stats like rate of fire, damage etc
    [Export] public WeaponInfo weapon { get; protected set; }
    [Export] WeaponFireType weaponFireType;
    //The array of nodes that will be used for their position of projectile spawns (Added as child nodes in the editor)
    
    Node2D[] weaponFirePoints;
    AudioStreamPlayer2D weaponFireSound;
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
    public float projectileSpeed { get => weapon.projectileSpeed; protected set => weapon.projectileSpeed = value; }
    public float projectileSpeedVariance { get => weapon.projectileSpeedVariance; protected set => weapon.projectileSpeedVariance = value; }
    public Texture2D projectileTexture { get => weapon.projectileTexture; protected set => weapon.projectileTexture = value; }

    //Checks if the weapon is equipped (normally only really necessary on the player)
    public bool isEquipped = true;
    //the time since it last fired
    float fireTime = 0;
    //The layers and masks that spawned projectiles will inherit
    int[] attackCollisionLayers, attackCollisionMasks;

    public override void _Ready()
    {
        base._Ready();

        //Get the weapon fire point nodes and add them to the array
        var weaponPointChildNodes = GetNode("Firepoints").GetChildren();
        weaponFirePoints = new Node2D[weaponPointChildNodes.Count];
        for (int i = 0; i < weaponFirePoints.Length; i++)
        {
            weaponFirePoints[i] = (Node2D)weaponPointChildNodes[i];
        }

        //define the weapon fire sound player
        weaponFireSound = GetNode<AudioStreamPlayer2D>("WeaponFireSound");
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
            weaponFireSound.Play();
            return true;
        }
        return false;
    }
    public void SpawnProjectiles(Vector2 spawnPosition, float spawnRotation)
    {
        float scatterAmount;
        for (int i = 0; i < shotsFired; i++)
        {
            //Set scatter rotation and speed of projectile
            scatterAmount = -scatter + (GD.Randf() * 2 * scatter);
            float newProjectileSpeed = projectileSpeed - projectileSpeedVariance + (GD.Randf() * (projectileSpeedVariance * 2));

            //Instantiate the projectile itself
            var newProjectile = projectileToSpawn.Instantiate() as ProjectileParent;
            GetTree().CurrentScene.AddChild(newProjectile);

            //Position, rotation and (if applicable) texture
            newProjectile.GlobalPosition = spawnPosition;
            newProjectile.Rotation = spawnRotation + scatterAmount;
            if (projectileTexture != null) { newProjectile.SetProjectileTexture(projectileTexture); }
            
            newProjectile.SetProjectileStats(damage, range, isDirectFire, newProjectileSpeed);
            //Set the projectile collision layers and masks
            newProjectile.CallDeferred("SetProjectileCollisions", attackCollisionLayers, attackCollisionMasks);
        }
    }

}

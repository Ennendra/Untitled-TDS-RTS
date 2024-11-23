using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

public partial class FactionComponent : Area2D
{
    Sprite2D unitSprite, unitScaffoldSprite;

    //Tracks the last position of the unit/component and where it may be in 1 second's time
    public Vector2 lastPos { get; private set; }
    public Vector2 projectedPosition;
    [Export] private int _faction = 1;

    [Export] DamageComponent damageComponent;
    [Export] AIComponent aiComponent;
    [Export] FactoryComponent factoryComponent;

    //Tells us if this item is a unit or not, for the purposes of unit selection (ie, filter out buildings from selection)
    [Export] public bool isAUnit = false;
    [Export] public bool isPlayer = false;
    bool isSelected = false;
    [Export] Texture2D selectedTexture;
    Sprite2D selectSprite;

    //Determines what factions can 'see' this object
    public bool[] spottedByFaction { get; protected set; } = {false, false, false, false, false};

    [Export] public UnitInfo unitInfo { get; private set; }

    //A position for units to move to once they finish buildilng
    [Export] public bool CanSetRallyPoint { get; protected set; } = false;
    protected Vector2 rallyPoint;

    List<WeaponParent> weapons = new();
    List<ToolParent> tools = new();

    public int faction
    {
        get => _faction;
        set
        {
            _faction = value;
            EmitSignal("FactionChanged");
        }
    }

    public override void _Ready()
    {
        base._Ready();

        //Set all spotted values to 0

        lastPos = GlobalPosition;

        //Define the selection box around the unit and then hide it initially
        selectSprite = GetNode<Sprite2D>("SelectionSprite");
        selectSprite.Texture = selectedTexture;
        selectSprite.Visible = false;

        //Signal and method to automatically alter collisions for weapons and taking damage when the unit or building changes side
        AddUserSignal("FactionChanged");
        Connect("FactionChanged", new Callable(this, "OnFactionChanged"));
        EmitSignal("FactionChanged");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        //Create a projected location on the component in 1 second's time
        Vector2 moveVector = (GlobalPosition - lastPos) / (float)delta;
        projectedPosition = GlobalPosition + moveVector;

        if (moveVector.Length() < 1.0f) { projectedPosition = GlobalPosition; }
        //Reset the last position
        lastPos = GlobalPosition;
    }

    //Getting the AI component (used to send orders through the RTS controller)
    public AIComponent GetAIComponent()
    {
        return aiComponent;
    }
    public FactoryComponent GetFactoryComponent()
    {
        return factoryComponent;
    }
    public DamageComponent GetDamageComponent()
    {
        return damageComponent;
    }

    //Gets the energy to metal ratio of the unit or building, based on it's unitInfo
    public float[] GetEnergyMetalRatio()
    {
        float energyCost = unitInfo.energyCost;
        float metalCost = unitInfo.metalCost;
        float[] ratio = new float[2] { 1, 1 };
        float energyAmount, metalAmount;
        if (metalCost > 0) { energyAmount = energyCost / metalCost; } else { energyAmount = 1; }
        if (energyCost > 0) { metalAmount = metalCost / energyCost; } else { metalAmount = 1; }

        if (energyAmount < 1) { ratio[0] = energyAmount; }
        if (metalAmount < 1) { ratio[1] = metalAmount; }

        return ratio;
    }
    //Called when receiving healing from a constructor component
    public void ReceiveHealingSupply(float energyAmount, float metalAmount)
    {
        //Get the total cost of the unit and the amount being supplied, then get the ratio on a 0-1 scale from that (with 1 = 100% of supply)
        float totalCost = unitInfo.energyCost + unitInfo.metalCost;
        float totalSupply = energyAmount + metalAmount;
        float supplyRatio = totalSupply/ totalCost;

        //Heal the target by a percentage amount based on the above ratio, with a 50% bonus to healing (ie. it's cheaper to heal than build a new unit)
        damageComponent.HealDamage(damageComponent.maxHealth * supplyRatio * 1.5f);
    }

    //Functions related to RTS input
    public void SetSelectionStatus(bool selectionStatus)
    {
        isSelected = selectionStatus;
        if (selectedTexture != null)
        {
            if (selectionStatus) selectSprite.Visible = true; else selectSprite.Visible = false;
        }
    }
    public bool GetSelectionStatus()
    {
        return isSelected;
    }
    public void SetRallyPoint(Vector2 rallyPoint)
    {
        this.rallyPoint = rallyPoint;
        //setting this to true here, so when factory components set the initial rally point, it tells the RTS controller that this building has rally points
        CanSetRallyPoint = true; 
    }
    public Vector2 GetRallyPoint() { return this.rallyPoint; }

    //Functions related to spotting and FOW
    public void ResetSpottedValues()
    {
        //don't reset the spotted value if the object is a building (They will maintain spotted status)
        if (isAUnit)
        {
            for (int i = 0; i < spottedByFaction.Length; i++) 
            {
                spottedByFaction[i] = false;
            }

        }
        //make sure they are spotted by their own faction
        spottedByFaction[faction] = true;
    }
    public void SetAsSpotted(int spottingFaction)
    {
        spottedByFaction[spottingFaction] = true;
    }


    //Weapon and tool list editing functions
    public void AddWeapon(WeaponParent weapon)
    {
        weapons.Add(weapon);
    }
    public void RemoveWeapon(WeaponParent weapon)
    {
        weapons.Remove(weapon);
    }
    public void AddTool(ToolParent tool)
    {
        tools.Add(tool);
    }
    public void RemoveTool(ToolParent tool)
    {
        tools.Remove(tool);
    }


    //collision manipulation functions
    public void SetNewWeaponAttackLayers(int[] newLayers, int[] newMasks)
    {
        foreach (WeaponParent weapon in weapons)
        {
            weapon.SetNewCollisionLayers(newLayers);
            weapon.SetNewCollisionMasks(newMasks);
        }
    }
    public void SetNewDamageComponentLayers(int[] newLayers, int[] newMasks)
    {
        if (damageComponent != null)
        {
            damageComponent.SetCollisionLayers(newLayers);
            damageComponent.SetCollisionMasks(newMasks);
        }
    }
    public void SetFactionLayer(int layer)
    {
        int layerResult = (int)Math.Pow(2, layer - 1);
        this.CollisionLayer = (uint)layerResult;
    }

    
    public void OnFactionChanged()
    {
        

        int[] attackLayers, attackMasks, damageLayers, damageMasks;
        int factionLayer;
        switch (faction)
        {
            case 1:
                attackLayers = new int[] { 12 };
                attackMasks = new int[] { 9, 10, 11 };
                damageLayers = new int[] { 8 };
                damageMasks = new int[] { 13, 14 };
                factionLayer = 5;
                break;
            case 2:
                attackLayers = new int[] { 13 };
                attackMasks = new int[] { 8, 10, 11 };
                damageLayers = new int[] { 9 };
                damageMasks = new int[] { 12, 14 };
                factionLayer = 6;
                break;
            case 3:
                attackLayers = new int[] { 14 };
                attackMasks = new int[] { 8, 9, 11 };
                damageLayers = new int[] { 10 };
                damageMasks = new int[] { 12, 13 };
                factionLayer = 7;
                break;
            default:
                GD.Print("Error on faction change: " + faction);
                attackLayers = new int[] { 12 };
                attackMasks = new int[] { 9, 10, 11 };
                damageLayers = new int[] { 8 };
                damageMasks = new int[] { 13, 14 };
                factionLayer = 5;
                break;
        }

        CallDeferred("SetNewWeaponAttackLayers", attackLayers, attackMasks);
        CallDeferred("SetNewDamageComponentLayers", damageLayers, damageMasks);
        CallDeferred("SetFactionLayer", factionLayer);
    }
}

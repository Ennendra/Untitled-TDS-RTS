using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public enum PlayerState
{
	COMBAT,
	BUILDPLACING,
}

//A reference to buildings in the players build queue and how much they have been supplied
public class BuildingQueue 
{
    public ConstructInfo building;
    public float energySupplied;
    public float metalSupplied;
    public float totalSupplied { get => energySupplied + metalSupplied; }
    public float totalCost { get => building.unitInfo.energyCost + building.unitInfo.metalCost; }

    public BuildingQueue(ConstructInfo building, float energySupplied, float metalSupplied)
    {
        this.building = building;
        this.energySupplied = energySupplied;
        this.metalSupplied = metalSupplied;
    }

    public BuildingQueue(ConstructInfo building)
    {
        this.building = building;
        this.energySupplied = 0;
        this.metalSupplied = 0;
    }

    public float[] GetEnergyMetalRatio()
    {
        float[] ratio = new float[2] { 1, 1 };
        float energyAmount, metalAmount;
        if (building.unitInfo.metalCost > 0) { energyAmount = building.unitInfo.energyCost / building.unitInfo.metalCost; } else { energyAmount = 1; }
        if (building.unitInfo.energyCost > 0) { metalAmount = building.unitInfo.metalCost / building.unitInfo.energyCost; } else { metalAmount = 1; }

        if (energyAmount < 1) { ratio[0] = energyAmount; }
        if (metalAmount < 1) { ratio[1] = metalAmount; }

        return ratio;
    }
    public void SupplyResources(float energy, float metal)
    {
        energySupplied += energy;
        metalSupplied += metal;

        float costPercentSupplied = (energy + metal) / totalCost;
    }
}

public partial class Player : UnitParent
{
    PlayerState state = PlayerState.COMBAT;
    public MainLevelController levelController;
    Camera2D camera;

    //UI related
    MainUI playerUI;
    [Export] Sprite2D spriteAimCursor;
    float mouseDistanceFromPlayer = 0;
    float CameraDistanceFromPlayer = 0;

    //the info sent to the UI if not in a network
    //0=energy generation, 1=metal generation
    public int[] resourceUITick { get; protected set; } = new int[6];

    //The buildings the player has queued
    public List<BuildingQueue> buildingQueue = new();
    //nodes for tools and weapons
    [Export] Node2D weaponFolder, toolFolder;
	List<WeaponParent> weapons = new();
    List<ToolParent> tools = new();
    //Define the build tool that is used for general buildqueue use
    [Export] ToolBuilder buildTool;
    
    //Determines whether we can't shoot weapons. Used to prevent weapons firing after placing a building
    public bool weaponsDisabled = false;
    //Determines whether the mouse is over the UI while the LMB is *not* pressed. This should allow for the player weapon to fire if they were holding the button down
    //when starting to hover over UI
    bool uiInputCheck = false;


    
	//General process functions
    public override void _Ready()
	{

        base._Ready();

        camera = GetNode<Camera2D>("Camera");

        factionComponent.SetAsSpotted(factionComponent.faction);

        MinimapMarkerComponent marker = GetNode<MinimapMarkerComponent>("MinimapMarkerComponent");
        marker.spottedByFaction[factionComponent.faction] = true;
    }
	public override void _Process(double delta)
	{
        
        //If not currently firing, disable mouse interactions if the mouse is over the player UI
        if (!Input.IsActionPressed("Personal_Use_Fire")) { uiInputCheck = playerUI.IsOverActiveUI(); }

        if (IsLevelControllerSet())
        {
            //Determining the direction that the player aims
            aimComponent.SetTargetDirection(GetGlobalMousePosition());

            //Slide an indicator for the aiming based on current aim direction and distance from player to cursor
            //mouseDistanceFromPlayer = Mathf.Lerp(mouseDistanceFromPlayer, GlobalPosition.DistanceTo(GetGlobalMousePosition()), 4 * (float)delta);
            mouseDistanceFromPlayer = GlobalPosition.DistanceTo(GetGlobalMousePosition());
            spriteAimCursor.GlobalPosition = GlobalPosition + (Vector2.FromAngle(aimComponent.GlobalRotation) * mouseDistanceFromPlayer);

            //Slide the camera based on camera and mouse position from the center of the screen, as long as the pause menu isn't up
            if (levelController.mainLevelState == GeneralLevelState.INGAME || levelController.mainLevelState == GeneralLevelState.WINTIMER || levelController.mainLevelState == GeneralLevelState.LOSSTIMER)
            {
                float viewportMovementScale = 0.1f; //The higher this value, the more the camera will move based on cursor movement
                Vector2 viewportTargetPos = (GetViewport().GetMousePosition() - (GetViewportRect().Size / 2)) * viewportMovementScale;
                float cameraMovementAmount = Mathf.Lerp(0, camera.Position.DistanceTo(viewportTargetPos), 4 * (float)delta);
                camera.Position = camera.Position.MoveToward(viewportTargetPos, cameraMovementAmount);
            }

            SendHealthBarData();

            
        }

        //Check which building is currently active and send that object data to the building tool
        BuildingQueue currentQueueItem = DetermineCurrentBuildQueueItem();
        buildTool.SetBuildQueueTarget(currentQueueItem);
    }
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        Vector2 targetDirection = Vector2.Zero;

        //Processing movement and firing (only when not paused)
        if (!GetTree().Paused)
        {
            if (IsLevelControllerSet())
            {
                if (levelController.techController.control_movement)
                    { targetDirection = Input.GetVector("Personal_MoveLeft", "Personal_MoveRight", "Personal_MoveUp", "Personal_MoveDown"); }
            }

            if (targetDirection != Vector2.Zero)
            {
                float directionTo = targetDirection.Angle();
                movementComponent.SetTargetDirection(directionTo);

                

                movementComponent.Accelerate(delta);
            }
            else
            {
                movementComponent.Decelerate(delta);
            }

            if (Input.IsActionJustReleased("Personal_Use_Fire")) { weaponsDisabled = false; }
        }
    }

    public AimingComponent GetAimComponent()
    {
        return aimComponent;
    }
    public void SetPlayerUI(MainUI playerUI)
	{
		this.playerUI = playerUI;
	}
	public void CheckMouseInputs()
	{
        //General combat controls
        if (playerUI != null)
        {
            if (Input.IsActionPressed("Personal_Use_Fire") && !uiInputCheck && !weaponsDisabled && levelController.techController.control_Shooting)
            {
                //TODO: Check whether player UI is in the mouse point, and not fire if so
                aimComponent.FireWeapons();
            }

            if (Input.IsActionJustPressed("Personal_UseTool") && !uiInputCheck)
            {
                aimComponent.UseTool();
            }
        }
    }

    //Functions related to the building queue ----
    //Goes through each building in the queue in order to see if they are not ready to deploy and return that item
    public BuildingQueue DetermineCurrentBuildQueueItem()
    {
        if (buildingQueue.Count == 0) { return null; }

        for (int i = 0; i<buildingQueue.Count; i++)
        {
            if (buildingQueue[i] != null)
            {
                if (!IsBuildingReady(i)) { return buildingQueue[i]; }
            }
        }
        return null;
    }
    //Checks the buildingQueue item at the specified index and checks that it's ready to deploy (ie. all resources supplied)
    public bool IsBuildingReady(int index)
    {
        if (buildingQueue[index].totalSupplied >= buildingQueue[index].totalCost)
        {
            return true;
        }
        return false;
    } 
    public void AddBuildingToQueue(ConstructInfo newitem)
    {
        BuildingQueue newQueueItem = new BuildingQueue(newitem);
        buildingQueue.Add(newQueueItem);
    }
    public void RemoveBuildingFromQueue(BuildingQueue item)
    {
        buildingQueue.Remove(item);
    }
    public void RemoveBuildingQueueAtIndex(int index, bool isCancelled)
    {
        //Refund the resources spent on the building back if it was cancelled (false=it was placed on the field)
        if (isCancelled)
        {
            GetFactionController().AddToStorage(0, buildingQueue[index].energySupplied);
            GetFactionController().AddToStorage(1, buildingQueue[index].metalSupplied);
        }
        buildingQueue.RemoveAt(index);
    }
    public bool ToggleBuildToolActive()
    {
        return buildTool.ToggleActive();
    }


    public bool IsLevelControllerSet()
    {
        if (IsInstanceValid(levelController))
        {
            if (levelController.playState == LevelControllerPlayState.PERSONALPLAYER)
            {
                return true;
            }
        }
        return false;
    }
    public void SetMainCamera()
    {
        camera.MakeCurrent();
    }
    public void SetCameraMapLimits(float top, float bottom, float left, float right)
    {
        camera.LimitTop = (int)top;
        camera.LimitBottom = (int)bottom;
        camera.LimitLeft = (int)left;
        camera.LimitRight = (int)right;
    }
    public void SetPlayerState(PlayerState state)
    {
        this.state = state;
    }
    public FactionController GetFactionController()
    {
        return levelController.GetFactionController(factionComponent.faction - 1);
    }

    public void SetMainBuildTool(ToolBuilder buildTool)
    {
        this.buildTool = buildTool;
    }

    #region Signal Functions
    public void OnWeaponAdded(Node node)
    {
        WeaponParent weapon = (WeaponParent)node;
        weapons.Add(weapon);
        factionComponent.AddWeapon(weapon);
        if (aimComponent.equippedWeapon == null) { aimComponent.EquipNewWeapon(weapon); }
        CallDeferred("SendNewToolbarUIData");
    }
    public void OnWeaponRemoved(Node node)
    {
        WeaponParent weapon = (WeaponParent)node;
        weapons.Remove(weapon);
        factionComponent.RemoveWeapon(weapon);
        CallDeferred("SendNewToolbarUIData");
    }
    public void OnToolAdded(Node node)
    {
        ToolParent tool = (ToolParent)node;
        tools.Add(tool);
        if (aimComponent.equippedTool == null) { aimComponent.EquipNewTool(tool); }
        CallDeferred("SendNewToolbarUIData");
    }
    public void OnToolRemoved(Node node)
    {
        ToolParent tool = (ToolParent)node;
        tools.Remove(tool);
        CallDeferred("SendNewToolbarUIData");
    } 
    #endregion

    //Function to send data on all weapons and tools now on hand to the UI
    public void SendNewToolbarUIData()
    {
        if (playerUI != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i < tools.Count)
                {
                    playerUI.GetPersonalToolbar().SetNewToolLink(tools[i], i);
                }
                else
                {
                    playerUI.GetPersonalToolbar().SetNewToolLink(null, i);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (i < weapons.Count)
                {
                    playerUI.GetPersonalToolbar().SetNewWeaponLink(weapons[i], i);
                }
                else
                {
                    playerUI.GetPersonalToolbar().SetNewWeaponLink(null, i);
                }
            }
        }
    }
    public void SendHealthBarData()
    {
        if (playerUI != null)
        {
            playerUI.GetPersonalToolbar().GetNewHealthData(damageComponent.GetCurrentHealthPercent());
            playerUI.GetRTSToolbar().GetNewHealthData(damageComponent.GetCurrentHealthPercent());
        }
    }

    public List<ResourceComponent> GetPlayerResourceComponents()
    {
        List<ResourceComponent> allComponents = new();
        if (IsInstanceValid(resourceComponent)) allComponents.Add(resourceComponent);
        foreach (ToolParent tool in tools)
        {
            if (IsInstanceValid(tool.GetResourceComponent())) allComponents.Add(tool.GetResourceComponent());
        }

        return allComponents;
    }
    public List<ConstructorComponent> GetPlayerConstructorComponents()
    {
        List<ConstructorComponent> allComponents = new();
        foreach (ToolParent tool in tools)
        {
            if (IsInstanceValid(tool.GetConstructorComponent())) allComponents.Add(tool.GetConstructorComponent());
        }
        return allComponents;
    }

    //Slight override of if the player dies, to make sure the game flicks to RTS mode
    public new void OnDamageKill()
    {
        CreateExplosion();

        //Force perspective to RTS mode
        if (levelController.playState == LevelControllerPlayState.PERSONALPLAYER) { levelController.ToggleRTSMode(); }
        //Reset the UI for the build queue
        playerUI.GetBuildQueueBar().UpdateButtonQueueInfo(new List<BuildingQueue>());

        QueueFree();
    }
}

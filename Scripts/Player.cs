using Godot;
using System;
using System.Collections.Generic;

public enum PlayerState
{
	COMBAT,
	BUILDPLACING,
}

//A reference to buildings in the players build queue and how much they have been supplied
public struct BuildingQueue 
{
    UnitInfo building;
    float energySupplied;
    float metalSupplied;

    public BuildingQueue(UnitInfo building, float energySupplied, float metalSupplied)
    {
        this.building = building;
        this.energySupplied = energySupplied;
        this.metalSupplied = metalSupplied;
    }

    public BuildingQueue(UnitInfo building)
    {
        this.building = building;
        this.energySupplied = 0;
        this.metalSupplied = 0;
    }
}


public partial class Player : CombatantParent
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
    public BuildingQueue[] buildingQueue = new BuildingQueue[5];

    //nodes for tools and weapons
    [Export] Node2D weaponFolder, toolFolder;
	List<WeaponParent> weapons = new();
    List<ToolParent> tools = new();
    
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
    }
	public override void _Process(double delta)
	{
        if (!Input.IsActionPressed("Personal_Use_Fire")) { uiInputCheck = playerUI.mouseOverUI; }

        if (IsLevelControllerSet())
        {
            //Determining the direction that the player aims
            aimComponent.SetTargetDirection(GetGlobalMousePosition());

            //Slide an indicator for the aiming based on current aim direction and distance from player to cursor
            //mouseDistanceFromPlayer = Mathf.Lerp(mouseDistanceFromPlayer, GlobalPosition.DistanceTo(GetGlobalMousePosition()), 4 * (float)delta);
            mouseDistanceFromPlayer = GlobalPosition.DistanceTo(GetGlobalMousePosition());
            spriteAimCursor.GlobalPosition = GlobalPosition + (Vector2.FromAngle(aimComponent.GlobalRotation) * mouseDistanceFromPlayer);

            //Slide the camera based on camera and mouse position from the center of the screen
            float viewportMovementScale = 0.1f; //The higher this value, the more the camera will move based on cursor movement
            Vector2 viewportTargetPos = (GetViewport().GetMousePosition() - (GetViewportRect().Size / 2)) * viewportMovementScale;
            float cameraMovementAmount = Mathf.Lerp(0, camera.Position.DistanceTo(viewportTargetPos), 4 * (float)delta);
            camera.Position = camera.Position.MoveToward(viewportTargetPos, cameraMovementAmount);
            
            SendHealthBarData();
        }
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
                targetDirection = Input.GetVector("Personal_MoveLeft", "Personal_MoveRight", "Personal_MoveUp", "Personal_MoveDown");
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
            if (Input.IsActionPressed("Personal_Use_Fire") && !uiInputCheck && !weaponsDisabled)
            {
                //TODO: Check whether player UI is in the mouse point, and not fire if so
                aimComponent.FireWeapons();
            }

            if (Input.IsActionPressed("Personal_UseTool") && !uiInputCheck)
            {
                //TODO: Check whether player UI is in the mouse point, and not fire if so
                aimComponent.UseTool();
            }
            else aimComponent.StopTool();
        }
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

        if (levelController.playState == LevelControllerPlayState.PERSONALPLAYER) { levelController.ToggleRTSMode(); }

        QueueFree();
    }
}

using Godot;
using System;
using System.Collections.Generic;

public enum PlayerState
{
	COMBAT,
	BUILDPLACING,
}

public partial class Player : Area2D
{
    PlayerState state = PlayerState.COMBAT;
    public MainLevelController levelController;
    Camera2D camera;

    [Export] public DamageComponent damageComponent { get; private set; }
    [Export] public AimingComponent aimComponent { get; private set; }
    [Export] public MovementComponent movementComponent { get; private set; }
    [Export] public FactionComponent factionComponent { get; private set; }
    [Export] public ResourceComponent resourceComponent { get; private set; }

    //UI related
    MainUI playerUI;
    [Export] Sprite2D spriteAimCursor;
    float mouseDistanceFromPlayer = 0;

    //the info sent to the UI if not in a network
    //0=energy generation, 1=metal generation
    public int[] resourceUITick { get; protected set; } = new int[6];

	//nodes for tools and weapons
	[Export] Node2D weaponFolder, toolFolder;
	List<WeaponParent> weapons = new();
    List<ToolParent> tools = new();
    //Determines whether we can't shoot weapons. Used to prevent weapons firing after placing a building
    public bool weaponsDisabled = false;
    
	//General process functions
    public override void _Ready()
	{
        camera = GetNode<Camera2D>("Camera");
        var mapEvent = InputMap.ActionGetEvents("CancelBuildPlacement");
        GD.Print(mapEvent);
        GD.Print(mapEvent[0]);

        var e = new InputEventKey();
        
    }
	public override void _Process(double delta)
	{
        if (IsLevelControllerSet())
        {
            aimComponent.SetTargetDirection(GetGlobalMousePosition());
            mouseDistanceFromPlayer = Mathf.Lerp(mouseDistanceFromPlayer, GlobalPosition.DistanceTo(GetGlobalMousePosition()), 4 * (float)delta);

            spriteAimCursor.GlobalPosition = GlobalPosition + (Vector2.FromAngle(aimComponent.GlobalRotation) * mouseDistanceFromPlayer);
            
            SendHealthBarData();
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        Vector2 targetDirection = Vector2.Zero;

        if (IsLevelControllerSet())
        {
            targetDirection = Input.GetVector("Personal_MoveLeft", "Personal_MoveRight", "Personal_MoveUp", "Personal_MoveDown");
        }
		
		if (targetDirection!=Vector2.Zero)
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
    public float GetResourcePerformance(float generation, float consumption)
    {
        if (consumption > 0)
        {
            return generation / consumption;
        }
        else
        {
            return 1;
        }
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
            if (Input.IsActionPressed("Personal_Use_Fire") && !playerUI.mouseOverUI && !weaponsDisabled)
            {
                //TODO: Check whether player UI is in the mouse point, and not fire if so
                aimComponent.FireWeapons();
            }

            if (Input.IsActionPressed("Personal_UseTool") && !playerUI.mouseOverUI)
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

    public List<ResourceComponent> GetResourceComponents()
    {
        List<ResourceComponent> allComponents = new();
        if (IsInstanceValid(resourceComponent)) allComponents.Add(resourceComponent);
        foreach (ToolParent tool in tools)
        {
            if (IsInstanceValid(tool.GetResourceComponent())) allComponents.Add(tool.GetResourceComponent());
        }

        return allComponents;
    }
    public List<ConstructorComponent> GetConstructorComponents()
    {
        List<ConstructorComponent> allComponents = new();
        foreach (ToolParent tool in tools)
        {
            if (IsInstanceValid(tool.GetConstructorComponent())) allComponents.Add(tool.GetConstructorComponent());
        }
        return allComponents;
    }

}

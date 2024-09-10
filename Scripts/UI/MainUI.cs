using Godot;
using GodotPlugins.Game;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public partial class MainUI : CanvasLayer
{
	public bool mouseOverUI { get; private set; } = false;

	FactionController playerFactionController;

	//Resource tracking UI
	UI_ResourceTracker resourceTracker;
	//PersonalToolbar
	UI_PersonalToolbar personalToolbar;
	//Build Placement Ghost
	[Export] PackedScene ghostPlacementScene;
	UI_BuildPlacement buildPlacementGhost;
	//RTSToolbar
	UI_RTSToolbar rtsToolbar;
	//Control Group hotbar
	UI_RTSControlGroupBar controlGroupBar;
	//Minimap
	UI_Minimap minimap;
	//Minimap zoom buttons
	TextureButton minimapZoomButtonPlus, minimapZoomButtonMinus;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		resourceTracker = GetNode<UI_ResourceTracker>("UI_ResourceTracker");
		personalToolbar = GetNode<UI_PersonalToolbar>("UI_PersonalToolbar");
		rtsToolbar = GetNode<UI_RTSToolbar>("UI_RTSToolbar");
		controlGroupBar = GetNode<UI_RTSControlGroupBar>("UI_ControlGroupBar");
		minimap = GetNode<UI_Minimap>("UI_Minimap");
		minimapZoomButtonPlus = GetNode<TextureButton>("UI_MinimapZoomPlus");
        minimapZoomButtonMinus = GetNode<TextureButton>("UI_MinimapZoomMinus");

        CallDeferred("InitBuildGhost");
    }
	public override void _Process(double delta)
	{
		base._Process(delta);

		//Toggle disabling the minimap buttons depending on the current zoom level
		if (minimap.IsMinimapFullMap())
			minimapZoomButtonMinus.Disabled = true;
		else
			minimapZoomButtonMinus.Disabled = false;

		
        if (minimap.IsMinimapFullZoom())
            minimapZoomButtonPlus.Disabled = true;
        else
            minimapZoomButtonPlus.Disabled = false;
    }
	public void InitBuildGhost()
	{
		buildPlacementGhost = (UI_BuildPlacement)ghostPlacementScene.Instantiate();
		GetTree().CurrentScene.AddChild(buildPlacementGhost);
	}

    public void SetPlayerFactionController(FactionController playerFactionController)
	{
		this.playerFactionController = playerFactionController;
	}
	public FactionController GetPlayerFactionController() 
	{  
		return this.playerFactionController; 
	}

	//Function compiling each set of buttons to link to the mainlevelcontroller script (ie. where to send signals to when pressed)
	public void SetButtonConnections(Node2D connection)
	{
		//The buttons to select a building to place
        personalToolbar.SetBuildButtonConnection(connection);
        rtsToolbar.SetBuildButtonConnection(connection);
		//The buttons to select a unit to build when a factory is selected
        rtsToolbar.SetFactoryBuildButtonConnection(connection);
		//The buttons that display the selected units
        rtsToolbar.SetUnitSelectButtonConnection(connection);
		//The buttons to designate specific orders
        rtsToolbar.SetOrderHotbarButtonConnection(connection);
		//The control group buttons
        controlGroupBar.SetButtonConnections(connection);
    }

	//Switch the toolbar depending on whether we are going to personal or RTS mode
	public void SetToolbarVisibility(bool isSwitchingToPersonal)
	{
		if (isSwitchingToPersonal)
		{
			personalToolbar.Visible = true;
			rtsToolbar.Visible = false;
            controlGroupBar.Visible = false;
        }
		else
		{
            personalToolbar.Visible = false;
            rtsToolbar.Visible = true;
			controlGroupBar.Visible = true;
        }
	}

    //Functions for the build ghost placement
    public void SetBuildGhostVisibility(bool visibility)
    {
        buildPlacementGhost.Visible = visibility;
    }
    public void ProcessBuildingPlacement(Vector2 buildLocationCheck)
	{
        buildPlacementGhost.ProcessBuildingPlacement(GetPlayerFactionController(), buildLocationCheck);
    }
	public bool AttemptBuildingPlacement()
	{
		return buildPlacementGhost.PlaceBuilding(GetPlayerFactionController());
	}

	//Functions for other scripts to get the specific UI modules
	public UI_ResourceTracker GetResourceTracker() { return resourceTracker; }
	public UI_PersonalToolbar GetPersonalToolbar() { return personalToolbar; }
	public UI_RTSToolbar GetRTSToolbar() { return rtsToolbar; }
	public UI_RTSControlGroupBar GetControlGroupBar() { return controlGroupBar; }
	public UI_BuildPlacement GetBuildPlacementGhost() { return buildPlacementGhost; }
	public UI_Minimap GetMinimap() { return minimap; }

    //Signal functions
    public void OnMouseEntered() { mouseOverUI = true; }
	public void OnMouseExited() {  mouseOverUI = false; }

	public void OnMinimapZoomPlusButtonPressed()
	{
		minimap.IncreaseZoomLevel();
	}
    public void OnMinimapZoomMinusButtonPressed()
    {
        minimap.DecreaseZoomLevel();
    }
}

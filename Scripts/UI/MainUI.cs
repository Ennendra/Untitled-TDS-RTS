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
    //Build Queue bar
    UI_BuildQueueBar buildQueueBar;
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
	TextureRect minimapZoomButtonBG;
	//Paused indicator
	TextureRect pausedIcon;
	//The text label used for unique mission text/dialogue
	Label missionText;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		resourceTracker = GetNode<UI_ResourceTracker>("UI_ResourceTracker");
		buildQueueBar = GetNode<UI_BuildQueueBar>("UI_BuildQueueBar");
		personalToolbar = GetNode<UI_PersonalToolbar>("UI_PersonalToolbar");
		rtsToolbar = GetNode<UI_RTSToolbar>("UI_RTSToolbar");
		controlGroupBar = GetNode<UI_RTSControlGroupBar>("UI_ControlGroupBar");
		minimap = GetNode<UI_Minimap>("UI_Minimap");
		minimapZoomButtonPlus = GetNode<TextureButton>("UI_MinimapZoomPlus");
        minimapZoomButtonMinus = GetNode<TextureButton>("UI_MinimapZoomMinus");
		minimapZoomButtonBG = GetNode<TextureRect>("MinimapButtonBG");
		pausedIcon = GetNode<TextureRect>("UI_Paused");
		missionText = GetNode<Label>("UI_MissionTextLabel");

        CallDeferred("InitBuildGhost");
    }
	public override void _Process(double delta)
	{
		base._Process(delta);

		//Toggle the visibility of the pause icon depending on whether or not the game is paused
		pausedIcon.Visible = GetTree().Paused;

		//Toggle disabling the minimap buttons depending on the current zoom level
		if (minimap.IsMinimapFullMap())
			minimapZoomButtonMinus.Disabled = true;
		else
			minimapZoomButtonMinus.Disabled = false;

		
        if (minimap.IsMinimapFullZoom())
            minimapZoomButtonPlus.Disabled = true;
        else
            minimapZoomButtonPlus.Disabled = false;

		if (Input.IsActionPressed("HideUI"))
		{
			resourceTracker.Modulate = new Color(1, 1, 1, 0.03f);
			personalToolbar.Modulate = new Color(1, 1, 1, 0.03f);
			rtsToolbar.Modulate = new Color(1, 1, 1, 0.03f);
			controlGroupBar.Modulate = new Color(1, 1, 1, 0.03f);
			minimap.Modulate = new Color(1, 1, 1, 0.03f);
			minimapZoomButtonMinus.Modulate = new Color(1, 1, 1, 0.03f);
			minimapZoomButtonPlus.Modulate = new Color(1, 1, 1, 0.03f);
            minimapZoomButtonBG.Modulate = new Color(1, 1, 1, 0.03f);
            buildQueueBar.Modulate = new Color(1, 1, 1, 0.03f);
        }
		else
		{
            resourceTracker.Modulate = new Color(1, 1, 1, 1);
            personalToolbar.Modulate = new Color(1, 1, 1, 1);
            rtsToolbar.Modulate = new Color(1, 1, 1, 1);
            controlGroupBar.Modulate = new Color(1, 1, 1, 1);
            minimap.Modulate = new Color(1, 1, 1, 1);
            minimapZoomButtonMinus.Modulate = new Color(1, 1, 1, 1);
            minimapZoomButtonPlus.Modulate = new Color(1, 1, 1, 1);
            minimapZoomButtonBG.Modulate = new Color(1, 1, 1, 1);
            buildQueueBar.Modulate = new Color(1, 1, 1, 1);
        }


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

	//tells us whether the mouse is over the UI and the UI is not being mostly hidden by the hideUI keybind
	public bool IsOverActiveUI()
	{
		bool uiHiddenByKeybind = Input.IsActionPressed("HideUI");

		return (mouseOverUI && !uiHiddenByKeybind);
	}

	//Function compiling each set of buttons to link to the mainlevelcontroller script (ie. where to send signals to when pressed)
	public void SetButtonConnections(Node2D connection)
	{
		//The buttons to select a building to place
        personalToolbar.SetBuildButtonConnection(connection);
        rtsToolbar.SetBuildButtonConnection(connection);
		buildQueueBar.SetBuildButtonConnection(connection);
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
    public void ProcessBuildingPlacement(Vector2 buildLocationCheck, FactionComponent builder)
	{
        buildPlacementGhost.ProcessBuildingPlacement(GetPlayerFactionController(), buildLocationCheck, builder);
    }
	public bool AttemptBuildingPlacement()
	{
		return buildPlacementGhost.PlaceBuilding(GetPlayerFactionController());
	}

	//Function to edit the mission text
	public void SetNewMissionText(string text)
	{
		missionText.Text = text;
	}

	//Functions for other scripts to get the specific UI modules
	public UI_ResourceTracker GetResourceTracker() { return resourceTracker; }
	public UI_BuildQueueBar GetBuildQueueBar() { return buildQueueBar; }
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

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using static Godot.WebSocketPeer;

public enum LevelControllerPlayState
{
    PERSONALPLAYER,
    RTSCOMMAND
}

public enum PersonalPlayState
{
    STANDARD,
    BUILDPLACEMENT
}

public enum RTSPlayState
{
    STANDARD,
    BUILDPLACEMENT,
    SETORDER_MOVE,
    SETORDER_ATTACK
}

public struct ResourceStats
{
    public float[] totalGeneration = new float[3] { 0, 0, 0 };
    public float[] totalConsumption = new float[3] { 0, 0, 0 };
    public float[] totalVariance = new float[3] { 0, 0, 0 };

    public ResourceStats(float[] totalGeneration, float[] totalConsumption, float[] totalVariance)
    {
        this.totalGeneration = totalGeneration;
        this.totalConsumption = totalConsumption;
        this.totalVariance = totalVariance;
    }
}

public partial class MainLevelController : Node2D
{
    Globals globals;

    //signals
    [Signal] public delegate void GetNewBuildInfoEventHandler(ConstructInfo buildInfo);
    [Signal] public delegate void GetBuildQueueItemEventHandler(int index);
    [Signal] public delegate void CancelBuildQueueItemEventHandler(int index);
    //Signals when the select unit UI button on the RTS toolbar is pressed
    [Signal] public delegate void SelectThisUnitEventHandler(UnitInfo unitInfo);
    [Signal] public delegate void DeselectThisUnitEventHandler(UnitInfo unitInfo);
    //Signal for when an order button is pressed
    [Signal] public delegate void SetOrderStateEventHandler(string buttonCode);
    //Signals for factory build buttons
    [Signal] public delegate void NewFactoryBuildEventHandler(ConstructInfo buildInfo, int amount);
    [Signal] public delegate void CancelFactoryBuildEventHandler(ConstructInfo buildInfo, int amount);
    //Signal for when a Control Group button is pressed
    [Signal] public delegate void SelectControlGroupButtonEventHandler(int index);

    FactionController[] factionController = new FactionController[3];

    public LevelControllerPlayState playState { get; private set; } = LevelControllerPlayState.PERSONALPLAYER;
    public PersonalPlayState personalPlayState { get; private set; } = PersonalPlayState.STANDARD;
    public RTSPlayState rtsPlayState { get; private set; } = RTSPlayState.STANDARD; 
    
    Player player;
    int playerFaction = 1;
	MainUI mainUI;
    RTSController rtsController;
    //Which building in the player's build queue is selected to deploy between 0-4. -1 = none selected
    int buildQueueSelected = -1;

    //Timer-based variables to prevent certain items ticking every frame
    //Minimap update timer
    float minimapTimer = 10, minimapTickPerSecond = 30;
    //Whether a special order (ie. one using the selection button instead of the executeorder button) was used. Used to prevent accidental minimap camera movement
    bool specialOrderJustExecuted = false;

    //the main battle area for pathfinding.
    //lowRemoved is the same navregion, but removes the obstacles (like 'water') which will allow for projectiles and hover units to move over
    [Export] NavigationRegion2D navigationRegionGround, navigationRegionHover;
    [Export] float top = -5000, left = -5000, bottom = 5000, right = 5000;
    Vector2[] mapBounds;

    //Trackers for all buildings and units currently in the scene
    List<BuildingParent> buildingsInScene = new();
    List<BlueprintParent> blueprintsInScene = new();
    List<UnitParent> unitsInScene = new();

    //Trackers for each faction's resources
    ResourceStats[] factionResources = new ResourceStats[2];

    //Fog of war tracking
    [Export] FOWController fowController;
    ImageTexture[] fowTextures;

    //Tracker for all misc environment obstacles used when baking new navmeshes
    [Export] TileMapLayer[] terrainTilemapLayers;
    List<CollisionPolygon2D> miscObstaclesInScene = new(); //walls and high obstacles
    List<CollisionPolygon2D> lowObstaclesInScene = new(); //water and obstacles that hover can go over
    
    bool navMapInitialised = false;

    PackedScene mapEdgeScene = ResourceLoader.Load<PackedScene>("res://Objects/Other/ObstacleMapEdge.tscn");
    ObstacleMapEdge[] obstacleMapEdges = new ObstacleMapEdge[4]; //the edges of the map

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        //Set signal functions
        GetNewBuildInfo += OnNewBuildInfo;
        GetBuildQueueItem += OnBuildQueueSelect;
        CancelBuildQueueItem += OnBuildQueueCancel;

        SelectThisUnit += SelectUnitOfType;
        DeselectThisUnit += DeselectUnitOfType;

        NewFactoryBuild += OnNewFactoryBuild;
        CancelFactoryBuild += OnRemoveFactoryBuild;


        SetOrderState += OnOrderStatePress;
        SelectControlGroupButton += OnControlGroupSelect;

        //Get the globals node for static use
        globals = GetNode<Globals>("/root/Globals");

        //Initialise the navigation regions and their respective navigation maps
        navigationRegionGround = new();
        navigationRegionHover = new();
        Rid navMapGround = NavigationServer2D.MapCreate();
        Rid navMapHover = NavigationServer2D.MapCreate();
        NavigationServer2D.MapSetCellSize(navMapGround, 1);
        NavigationServer2D.MapSetCellSize(navMapHover, 1);
        navigationRegionGround.SetNavigationMap(navMapGround);
        navigationRegionHover.SetNavigationMap(navMapHover);
        NavigationServer2D.MapSetActive(navMapGround, true);
        NavigationServer2D.MapSetActive(navMapHover, true);

        mainUI = GetNode<MainUI>("MainUIPersonal");
        rtsController = GetNode<RTSController>("RTSController");

        //Get the player node if it exists
        player = GetNodeOrNull<Player>("Player");

        

        //instantiate the faction controllers
        for (int i = 0; i < factionController.Count(); i++)
        {
            factionController[i] = new();
            factionController[i].SetFaction(i + 1);
        }

        //Set relevant faction info on controllers
        mainUI.SetPlayerFactionController(factionController[playerFaction - 1]);
        rtsController.levelController = this;
        rtsController.SetFaction(playerFaction);

        //Add the player to the list if it exists
        //If not, make sure we aren't in personal mode at start
        if (player != null) { AddPlayer(player); }
        else { CallDeferred("ToggleRTSMode"); }
        
        //Add all buildings, blueprints and units spawned in the scene into their respective lists
        //Also, Find all misc environment obstacles and add their polygons to a misc obstacle list
        var childNodes = GetChildren();
        foreach (var child in childNodes)
        {
            if (child.IsInGroup("Building"))
            {
                BuildingParent childCast = (BuildingParent)child;
                buildingsInScene.Add(childCast);
                factionController[childCast.GetCurrentFaction() - 1].AddBuilding(childCast);
                fowController.AddSightComponent(childCast.GetSightComponent());
            }
            else if (child.IsInGroup("Blueprint"))
            {
                BlueprintParent childCast = (BlueprintParent)child;
                blueprintsInScene.Add(childCast);
                factionController[childCast.GetCurrentFaction() - 1].AddBlueprint(childCast);
                fowController.AddSightComponent(childCast.GetSightComponent());
            }
            else if (child.IsInGroup("Unit"))
            {
                UnitParent childCast = (UnitParent)child;
                unitsInScene.Add(childCast);
                factionController[childCast.GetCurrentFaction() - 1].AddUnit(childCast);
                fowController.AddSightComponent(childCast.GetSightComponent());
            }
            if (child.IsInGroup("EnvironmentObstacles"))
            {
                CollisionPolygon2D childCast = child.GetNode<CollisionPolygon2D>("ObstacleBounds");
                miscObstaclesInScene.Add(childCast);
            }
            if (child.IsInGroup("LowEnvironmentObstacles"))
            {
                CollisionPolygon2D childCast = child.GetNode<CollisionPolygon2D>("ObstacleBounds");
                lowObstaclesInScene.Add(childCast);
            }
            navMapInitialised = true;
        }

        //Init the Fog of War and its textures, and prepare a set of them for the minimap
        fowTextures = fowController.InitFOW(new Vector2(left, top), GetMapSize());

        mainUI.SetButtonConnections(this);

        //Set the map bounds
        UpdateMapBounds();
    }

    public Vector2 GetMapSize()
    {
        float width = right - left;
        float height = bottom - top;
        return new Vector2(width, height);
    }
    //Updates relevant items when the map's edges are changed
    // - Change the navigation map's area
    // - Change the minimap's scan sizes
    // - Change the RTS controller's camera limits
    public void UpdateMapBounds()
    {
        mapBounds = GetMapCorners();
        mainUI.GetMinimap().SetFullMapZone(top, bottom, left, right);
        //Set the minimaps initial localmode tracking item
        mainUI.GetMinimap().SetLocalCenterNode(player);
        //Setting the RTS camera bounds
        rtsController.SetCameraMapLimits(top, bottom, left, right);
        if (IsInstanceValid(player))
        {
            player.SetCameraMapLimits(top, bottom, left, right);
        }

        //generating the map edge obtacles
        for (int i = 0; i < obstacleMapEdges.Length; i++) 
        {
            //Generate the new object instance if it doesn't already exist
            if (!IsInstanceValid(obstacleMapEdges[i]))
            {
                obstacleMapEdges[i] = (ObstacleMapEdge)mapEdgeScene.Instantiate();
                GetTree().CurrentScene.AddChild(obstacleMapEdges[i]);
                obstacleMapEdges[i].GlobalPosition = Vector2.Zero;
            }

            //Assigning each polygon vertice (always as a 4-vertice rectangle)
            Vector2[] newCollisionPolygon = new Vector2[4];
            if (i == 0) //Top
            {
                newCollisionPolygon[0] = new Vector2(left - 100, top);
                newCollisionPolygon[1] = new Vector2(right + 100, top);
                newCollisionPolygon[2] = new Vector2(right + 100, top - 100);
                newCollisionPolygon[3] = new Vector2(left - 100, top - 100);
            }
            else if (i == 1) //Left
            {
                newCollisionPolygon[0] = new Vector2(left, top - 100);
                newCollisionPolygon[1] = new Vector2(left, bottom + 100);
                newCollisionPolygon[2] = new Vector2(left - 100, bottom + 100);
                newCollisionPolygon[3] = new Vector2(left - 100, top - 100);
            }
            else if (i == 2) //Bottom
            {
                newCollisionPolygon[0] = new Vector2(left - 100, bottom);
                newCollisionPolygon[1] = new Vector2(right + 100, bottom);
                newCollisionPolygon[2] = new Vector2(right + 100, bottom + 100);
                newCollisionPolygon[3] = new Vector2(left - 100, bottom + 100);
            }
            else //Right
            {
                newCollisionPolygon[0] = new Vector2(right, top - 100);
                newCollisionPolygon[1] = new Vector2(right, bottom + 100);
                newCollisionPolygon[2] = new Vector2(right + 100, bottom + 100);
                newCollisionPolygon[3] = new Vector2(right + 100, top - 100);
            }
            obstacleMapEdges[i].SetCollisionPolygon(newCollisionPolygon);
        }

        //Set the new navigation pathing based on map bounds
        UpdateNavigationMap();
    }
    public Vector2[] GetMapCorners()
    {
        Vector2[] corners = new Vector2[4];
        corners[0] = new Vector2 (left, top);
        corners[1] = new Vector2 (right,top); 
        corners[2] = new Vector2 (right,bottom);
        corners[3] = new Vector2 (left,bottom);
        return corners;
    }
    public float[] GetMapSides()
    {
        return new float[] { top, bottom, left, right };
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        //Process resources when not paused
        if (!GetTree().Paused)
        {
            foreach (var controller in factionController)
            {
                controller.Process(delta);
            }
        }

        //Processing Fog of War
        UpdateFOW(delta);
        //Processing resource ticks and minimap display
        UpdateResourceUI(false);
        UpdateMinimap((float)delta);

        //Check whether we are in a build-placement state and set the UI to be visible accordingly
        CheckBuildPlacementVisibility();
        

        //General inputs when in personal player mode
        if (playState == LevelControllerPlayState.PERSONALPLAYER)
        {
            globals.SetNewCustomCursor("Personal");

            //Deprecating the equipment inputs. These will be replaced with building queue inputs
            //if (Input.IsActionJustPressed("Personal_SelectTool1")) { mainUI.GetPersonalToolbar().ExecuteEquipInput(0); }
            //if (Input.IsActionJustPressed("Personal_SelectTool2")) { mainUI.GetPersonalToolbar().ExecuteEquipInput(1); }
            //if (Input.IsActionJustPressed("Personal_SelectTool3")) { mainUI.GetPersonalToolbar().ExecuteEquipInput(2); }

            //if (Input.IsActionJustPressed("Personal_SelectWeapon1")) { mainUI.GetPersonalToolbar().ExecuteEquipInput(3); }
            //if (Input.IsActionJustPressed("Personal_SelectWeapon2")) { mainUI.GetPersonalToolbar().ExecuteEquipInput(4); }
            //if (Input.IsActionJustPressed("Personal_SelectWeapon3")) { mainUI.GetPersonalToolbar().ExecuteEquipInput(5); }

            ProcessBuildingQueueInputs();


            if (personalPlayState == PersonalPlayState.STANDARD)
            {
                if (!GetTree().Paused) { player.CheckMouseInputs(); }
            }
            if (personalPlayState == PersonalPlayState.BUILDPLACEMENT)
            {
                player.weaponsDisabled = true;
                ProcessBuildPlacementChecks();
            }
        }

        //General inputs when in RTS mode
        if (playState == LevelControllerPlayState.RTSCOMMAND)
        {
            if (rtsController.isCurrentlySelecting || mainUI.mouseOverUI)
                { globals.SetNewCustomCursor("Personal"); }
            else
                { 
                string cursorHoverCode = rtsController.ScanCursorPosition(GetGlobalMousePosition());
                globals.SetNewCustomCursor(cursorHoverCode); 
                }

            //Run this when only factories are selected and their build menu is shown, so it shows accurate info on the UI
            if (rtsController.factoryBuildMenuEnabled)
            {
                UpdateFactoryButtonQueueInfo();
            }

            if (rtsPlayState == RTSPlayState.STANDARD)
            {
                //pressing selection button
                if (Input.IsActionJustPressed("RTS_Select"))
                {
                    if (!mainUI.mouseOverUI) //Are we not over any UI?
                    {
                        rtsController.SetInitialSelectionPoint();
                    }
                    else if (mainUI.GetMinimap().IsInputWithinMinimapBounds(GetViewport().GetMousePosition())) //We are over UI, is it the minimap?
                    {
                        //Set the camera position to the relative map position to the minimap
                        rtsController.SetCameraPosition(GetMinimapToMapPosition());
                    }
                }
                //Releasing selection button
                if (Input.IsActionJustReleased("RTS_Select"))
                {
                    if (rtsController.isCurrentlySelecting)
                    {
                        List<RTSSelectionType> selection;

                        if (Input.IsKeyPressed(Key.Shift))
                            selection = rtsController.ExecuteSelection(true);
                        else
                            selection = rtsController.ExecuteSelection(false);
                        //send the unit selection data to the UI to display
                        mainUI.GetRTSToolbar().SetUnitSelectionUI(selection);
                    }
                    specialOrderJustExecuted = false;
                }
                //Pressing execute order button
                if (Input.IsActionJustPressed("RTS_ExecuteOrder"))
                {
                    if (!mainUI.mouseOverUI) //Are we not over any UI?
                    {
                        rtsController.ExecuteStandardOrder(GetGlobalMousePosition());
                    }
                    else if (mainUI.GetMinimap().IsInputWithinMinimapBounds(GetViewport().GetMousePosition())) //We are over UI, is it the minimap?
                    {
                        //Execute order to the relative map position to the minimap
                        rtsController.ExecuteStandardOrder(GetMinimapToMapPosition());
                    }
                    
                }

                //Checking general 'selection' mouse press for the minimap, when minimap is in full mode
                if (Input.IsActionPressed("RTS_Select") && !specialOrderJustExecuted && !rtsController.isCurrentlySelecting && mainUI.mouseOverUI)
                {
                    if (mainUI.GetMinimap().IsInputWithinMinimapBounds(GetViewport().GetMousePosition()) && mainUI.GetMinimap().IsMinimapFullMap())
                    {
                        //Set the camera position to the relative map position to the minimap
                        rtsController.SetCameraPosition(GetMinimapToMapPosition());
                    }
                }

                //Pressing any of the order hotbar hotkeys
                if (Input.IsActionJustPressed("RTS_SetMoveOrder"))
                {
                    OnOrderStatePress("Move");
                }
                if (Input.IsActionJustPressed("RTS_SetAttackOrder"))
                {
                    OnOrderStatePress("Attack");
                }
                if (Input.IsActionJustPressed("RTS_SetStopOrder"))
                {
                    OnOrderStatePress("Stop");
                }
                if (Input.IsActionJustPressed("RTS_SetHoldOrder"))
                {
                    OnOrderStatePress("Hold");
                }
                

            }
            else if (rtsPlayState == RTSPlayState.BUILDPLACEMENT)
            {
                globals.SetNewCustomCursor("Personal");
                ProcessBuildPlacementChecks();
            }
            else if (rtsPlayState == RTSPlayState.SETORDER_MOVE)
            {
                if (Input.IsActionJustPressed("RTS_Select"))
                {
                    if (!mainUI.mouseOverUI) //Are we not over any UI?
                    {
                        //Execute Move at the mouse position
                        rtsController.ExecuteMoveOrder(GetGlobalMousePosition());
                        SetStateToStandard();
                    }
                    else if (mainUI.GetMinimap().IsInputWithinMinimapBounds(GetViewport().GetMousePosition())) //We are over UI, is it the minimap?
                    {
                        //Execute Move at the relative map position to the minimap
                        rtsController.ExecuteMoveOrder(GetMinimapToMapPosition());
                        specialOrderJustExecuted = true;
                        SetStateToStandard();
                    }
                }
                if (Input.IsActionJustPressed("RTS_ExecuteOrder"))
                {
                    SetStateToStandard();
                }
            }
            else if (rtsPlayState == RTSPlayState.SETORDER_ATTACK)
            {
                if (Input.IsActionJustPressed("RTS_Select"))
                {
                    if (!mainUI.mouseOverUI) //Are we not over any UI?
                    {
                        //Execute Attack/Attack-Move at the mouse position
                        rtsController.ExecuteAttackOrder(GetGlobalMousePosition());
                        SetStateToStandard();
                    }
                    else if (mainUI.GetMinimap().IsInputWithinMinimapBounds(GetViewport().GetMousePosition())) //We are over UI, is it the minimap?
                    {
                        //Execute Attack/Attack-Move at the relative map position to the minimap
                        rtsController.ExecuteAttackOrder(GetMinimapToMapPosition());
                        specialOrderJustExecuted = true;
                        SetStateToStandard();
                    }
                }
                if (Input.IsActionJustPressed("RTS_ExecuteOrder"))
                {
                    SetStateToStandard();
                }
            }

            //Control Group interaction
            ProcessControlGroupInputs();

            
        }

        //Check minimap zoom keybinds
        ProcessMinimapInputs();
        //Toggling RTS Mode
        if (Input.IsActionJustPressed("RTSModeToggle"))
        {
            CallDeferred("ToggleRTSMode");
        }

        //Pausing and unpausing
        if (Input.IsActionJustPressed("PauseGame"))
        {
            GD.Print("Pause?");
            PauseGame();
        }
    }

    public void SetBuildPlacementItem()
    {

    }

    //Setting the pause state
    public void PauseGame()
    {
        GD.Print(GetTree().Paused);
        GetTree().Paused = !GetTree().Paused;
        GD.Print(GetTree().Paused);
    }

    public Vector2 GetMinimapToMapPosition()
    {
        //Get the relative position of the mouse input  (result will vary in size depending on the size of the scan size on the minimap, but 0,0 means the center)
        Vector2 minimapInputPos = mainUI.GetMinimap().CheckForMinimapInput(GetViewport().GetMousePosition());

        //If we're in full-map mode
        if (mainUI.GetMinimap().IsMinimapFullMap())
        {
            //Place RTS camera in a position based on the minimap click
            //Add the map's center from the true global center position to the click
            Vector2 mapCenter = new Vector2(
                right - ((right - left) / 2),
                bottom - ((bottom - top) / 2)
                );
            return minimapInputPos + mapCenter;
        }
        else //We are in a local scan mode
        {
            //Move the RTS camera based on the minimap click
            return rtsController.camera.Position + minimapInputPos;
        }
    }

    //Input functions
    //Control Group interaction in RTS mode
    public void ProcessControlGroupInputs()
    {
        int controlGroupSelected = -1;
        if (rtsPlayState == RTSPlayState.STANDARD)
        {
            if (Input.IsActionJustPressed("RTS_ControlGroup1")) { controlGroupSelected = 0; }
            if (Input.IsActionJustPressed("RTS_ControlGroup2")) { controlGroupSelected = 1; }
            if (Input.IsActionJustPressed("RTS_ControlGroup3")) { controlGroupSelected = 2; }
            if (Input.IsActionJustPressed("RTS_ControlGroup4")) { controlGroupSelected = 3; }
            if (Input.IsActionJustPressed("RTS_ControlGroup5")) { controlGroupSelected = 4; }
            if (Input.IsActionJustPressed("RTS_ControlGroup6")) { controlGroupSelected = 5; }
            if (Input.IsActionJustPressed("RTS_ControlGroup7")) { controlGroupSelected = 6; }
            if (Input.IsActionJustPressed("RTS_ControlGroup8")) { controlGroupSelected = 7; }
            if (Input.IsActionJustPressed("RTS_ControlGroup9")) { controlGroupSelected = 8; }
            if (Input.IsActionJustPressed("RTS_ControlGroup10")) { controlGroupSelected = 9; }
        }




        if (controlGroupSelected != -1)
        {
            if (Input.IsPhysicalKeyPressed(Key.Shift) && !Input.IsPhysicalKeyPressed(Key.Ctrl)) //Only shift is pressed, add these units to selection without clearing list
            {
                SelectControlGroup(controlGroupSelected, true);
            }
            else if (Input.IsPhysicalKeyPressed(Key.Ctrl) && !Input.IsPhysicalKeyPressed(Key.Shift)) //Only ctrl is pressed, set this selection as the new control group
            {
                rtsController.SetControlGroup(rtsController.selectedItems, controlGroupSelected);
                mainUI.GetControlGroupBar().SetControlGroupButtonInfo(controlGroupSelected, rtsController.GetControlGroups()[controlGroupSelected]);
            }
            else if (Input.IsPhysicalKeyPressed(Key.Ctrl) && Input.IsPhysicalKeyPressed(Key.Shift)) //both are pressed, add selected items to the existing control group
            {
                rtsController.AddToControlGroup(rtsController.selectedItems, controlGroupSelected);
                mainUI.GetControlGroupBar().SetControlGroupButtonInfo(controlGroupSelected, rtsController.GetControlGroups()[controlGroupSelected]);
            }
            else //neither are pressed
            {
                SelectControlGroup(controlGroupSelected, false);
            }
        }
    }
    public void ProcessMinimapInputs()
    {
        if (Input.IsActionJustPressed("MinimapZoomIncrease")) mainUI.GetMinimap().IncreaseZoomLevel();
        if (Input.IsActionJustPressed("MinimapZoomDecrease")) mainUI.GetMinimap().DecreaseZoomLevel();
    }
    public void ProcessBuildingQueueInputs()
    {
        //When hitting the button, run the OnBuildQueueSelect function on their respective index
        int buildQueueInputIndex = -1;
        if (Input.IsActionJustPressed("Personal_SelectBQ1")) { buildQueueInputIndex = 0; }
        if (Input.IsActionJustPressed("Personal_SelectBQ2")) { buildQueueInputIndex = 1; }
        if (Input.IsActionJustPressed("Personal_SelectBQ3")) { buildQueueInputIndex = 2; }
        if (Input.IsActionJustPressed("Personal_SelectBQ4")) { buildQueueInputIndex = 3; }
        if (Input.IsActionJustPressed("Personal_SelectBQ5")) { buildQueueInputIndex = 4; }

        if (buildQueueInputIndex!=-1)
        {
            //Execute the signal function for selecting the build queue item
            OnBuildQueueSelect(buildQueueInputIndex);
        }

    }

    //Selecting a building in the build queue
    //This can be triggered either by hotkeys or the build queue buttons
    public void OnBuildQueueSelect(int index)
    {

        //Only set building placement if in the personal player mode
        if (playState == LevelControllerPlayState.PERSONALPLAYER)
        {
            //Deselect the item if it was already selected
            if (buildQueueSelected == index || index > player.buildingQueue.Count-1)
            {
                ResetBuildQueueSelect();
            }
            else
            {
                //Prepare the building for placement if it is ready
                if (player.IsBuildingReady(index))
                {
                    buildQueueSelected = index;
                    mainUI.GetBuildPlacementGhost().SetNewBuildInfo(player.buildingQueue[index]);
                    SetPersonalPlayState(PersonalPlayState.BUILDPLACEMENT);
                }
            }
        }
        
    }

    public void OnBuildQueueCancel(int index)
    {
        //Check whether there is an item to cancel at this index (checked by seeing if the buildqueue array is within bounds
        if (!(index > player.buildingQueue.Count - 1))
        {
            player.RemoveBuildingQueueAtIndex(index, true);
            ResetBuildQueueSelect();
        }
    }
    public void ResetBuildQueueSelect()
    {
        buildQueueSelected = -1;
        mainUI.GetBuildQueueBar().UpdateButtonQueueInfo(player.buildingQueue);
        SetStateToStandard();
    }

    //Selecting a control group (And the control group button signal)
    public void OnControlGroupSelect(int index)
    {
        bool isAdditive = false;
        if (Input.IsPhysicalKeyPressed(Key.Shift)) { isAdditive = true; }

        SelectControlGroup(index, isAdditive);
    }
    public void SelectControlGroup(int index, bool isAdditive)
    {
        if (rtsController.controlGroups[index].units.Count > 0)
        {
            List<RTSSelectionType> selection = rtsController.SelectControlGroup(index, isAdditive);
            mainUI.GetRTSToolbar().SetUnitSelectionUI(selection);
            SetStateToStandard();
        }
    }

    public void CheckBuildPlacementVisibility()
    {
        if (playState == LevelControllerPlayState.PERSONALPLAYER)
        {
            if (personalPlayState == PersonalPlayState.BUILDPLACEMENT)
                mainUI.SetBuildGhostVisibility(true);
            else mainUI.SetBuildGhostVisibility(false);
        }
        else
        {
            if (rtsPlayState == RTSPlayState.BUILDPLACEMENT)
                mainUI.SetBuildGhostVisibility(true);
            else mainUI.SetBuildGhostVisibility(false);
        }
    }

    public void ProcessBuildPlacementChecks()
    {
        //TODO: Ensure this function places the building 
        mainUI.ProcessBuildingPlacement(GetGlobalMousePosition(), player.GetFactionComponent());

        if (!mainUI.mouseOverUI)
        {
            if (Input.IsActionJustPressed("PlaceBuilding"))
            {
                //If returning true, the building will be placed in that function
                if (mainUI.AttemptBuildingPlacement())
                {
                    //Remove the building from the build queue now that it is placed
                    //POSSIBLE TODO - When pressing shift, will find the next item in the queue that is ready
                    player.RemoveBuildingQueueAtIndex(buildQueueSelected, false);
                    ResetBuildQueueSelect();
                }
            }
        }
        if (Input.IsActionJustPressed("CancelBuildPlacement"))
        {
            ResetBuildQueueSelect();
        }
    }
    public void SetStateToStandard()
    {
        if (playState == LevelControllerPlayState.PERSONALPLAYER)
            SetPersonalPlayState(PersonalPlayState.STANDARD);
        else if (playState == LevelControllerPlayState.RTSCOMMAND)
            SetRTSPlayState(RTSPlayState.STANDARD);
    }

    public FactionController GetFactionController(int index)
    {
        return factionController[index];
    }

    public void ToggleRTSMode()
    {
        //reset the secondary play states
        SetPersonalPlayState(PersonalPlayState.STANDARD);
        SetRTSPlayState(RTSPlayState.STANDARD);

        //Reset the unit selection on the RTS controller and the UI for it
        rtsController.ClearUnitSelection();
        mainUI.GetRTSToolbar().SetUnitSelectionUI(new List<RTSSelectionType>());
        ResetUIToBuildingButtons();

        if (playState == LevelControllerPlayState.PERSONALPLAYER)
        {
            playState = LevelControllerPlayState.RTSCOMMAND;
            mainUI.SetToolbarVisibility(false);
            rtsController.SetCameraPosition(player.GlobalPosition);
            rtsController.SetMainCamera();
            mainUI.GetMinimap().SetLocalCenterNode(rtsController.camera);
        }
        else
        {
            if (IsInstanceValid(player))
            {
                playState = LevelControllerPlayState.PERSONALPLAYER;
                mainUI.SetToolbarVisibility(true);
                player.SetMainCamera();
                mainUI.GetMinimap().SetLocalCenterNode(player);
            }
        }
    }
    public void SetPersonalPlayState(PersonalPlayState state)
    {
        personalPlayState = state;
        switch (state) 
        {
            case PersonalPlayState.STANDARD: { player.SetPlayerState(PlayerState.COMBAT); break; }
            case PersonalPlayState.BUILDPLACEMENT: { player.SetPlayerState(PlayerState.BUILDPLACING); break; }
        }
    }
    public void SetRTSPlayState(RTSPlayState state)
    {
        rtsPlayState = state;

        if (rtsPlayState == RTSPlayState.SETORDER_MOVE) mainUI.GetRTSToolbar().SetOrderLabel("Designate Move Position");
        else if (rtsPlayState == RTSPlayState.SETORDER_ATTACK) mainUI.GetRTSToolbar().SetOrderLabel("Designate Attack");
        else mainUI.GetRTSToolbar().DisableOrderLabel();
    }


    public void OnOrderStatePress(string buttonCode)
    {
        //only activate if we actually have something selected and are not currently dragging a selection box
        if (rtsController.selectedItems.Count > 0 && !rtsController.isCurrentlySelecting)
        {
            switch (buttonCode)
            {
                case "Move": SetRTSPlayState(RTSPlayState.SETORDER_MOVE); break;
                case "Attack": SetRTSPlayState(RTSPlayState.SETORDER_ATTACK); break;
                case "Hold": rtsController.ExecuteHoldOrder(); break;
                case "Stop": rtsController.ExecuteCancelOrder(); break;
            }
        }
        
    }

    public void UpdateResourceUI(bool forceRefresh)
	{
        //Get the resource values of the current network
        int[] newValues;

        newValues = factionController[playerFaction - 1].GetCurrentTickValues(forceRefresh);

        mainUI.GetResourceTracker().GetNewResourceValues(newValues);
    }

    //Functions for the fog of war
    public void UpdateFOW(double delta)
    {
        ImageTexture[] newFOWTextures = fowController.ProcessCall(delta);
        //Will only return textures when the actual FOW updates
        if (newFOWTextures != null) 
        { 
            //set the new textures for use with the minimap
            fowTextures = newFOWTextures; 

            //Reset and process the 'spotting' process for units
            foreach (UnitParent unit in unitsInScene)
            {
                unit.GetFactionComponent().ResetSpottedValues();
            }
            foreach (BlueprintParent blueprint in blueprintsInScene)
            {
                blueprint.GetFactionComponent().ResetSpottedValues();
            }
            foreach (BuildingParent building in buildingsInScene)
            {
                building.GetFactionComponent().ResetSpottedValues();
            }

            //Run the scan process for all sight components. Doing this via the fowController which has access to all sight components
            fowController.ExecuteSightScans();

            //Hide any non-allied units
            var fComponentGroup = GetTree().GetNodesInGroup("FactionCombatant");
            foreach (var fComponent in fComponentGroup)
            {
                FactionComponent combatant = fComponent as FactionComponent;
                if (combatant.spottedByFaction[playerFaction])
                {
                    Node2D parent = (Node2D)combatant.GetParent();
                    parent.Visible = true;
                }
                else
                {
                    Node2D parent = (Node2D)combatant.GetParent();
                    parent.Visible = false;
                }
            }

        }
    }
    

    //Functions for the minimap
    public void UpdateMinimap(float delta)
    {
        minimapTimer += delta;

        if (minimapTimer >= (1 / minimapTickPerSecond))
        {
            minimapTimer = 0;
            //Get all minimap marker components in the scene
            List<MinimapMarkerComponent> minimapMarkers = new();
            var markerNodes = GetTree().GetNodesInGroup("MinimapMarker");
            //Set the minimap markers in case the player's or another unit's faction has changed
            //Also sets the spotted status to the markers to tell whether to display them or not
            SetFactionMinimapMarkers();

            //Cast the nodes obtained into the specific minimap marker component and add to the list
            foreach (var node in markerNodes)
            {
                MinimapMarkerComponent nodeCast = (MinimapMarkerComponent)node;
                
                minimapMarkers.Add(nodeCast);
            }

            //Send the list to the minimap UI to process and draw
            if (fowTextures[0] != null)
            {
                mainUI.GetMinimap().ProcessMinimapTick(minimapMarkers, fowTextures);
            }
            else
            {
                mainUI.GetMinimap().ProcessMinimapTick(minimapMarkers, null);
            }
        }
    }
    //Update the minimap marker tags of all combatants and buildings based on the player's faction
    public void SetFactionMinimapMarkers()
    {
        for (int i = 0; i<factionController.Length; i++)
        {
            //If the coming factionController will be the player's units
            if (i == rtsController.playerFaction-1)
            {
                factionController[i].SetAllFactionMinimapMarkerComponents(MinimapMarkerTag.ALLYBUILDING, MinimapMarkerTag.ALLYBUILDING, MinimapMarkerTag.ALLYUNIT);
            }
            else //all others/enemies
            {
                factionController[i].SetAllFactionMinimapMarkerComponents(MinimapMarkerTag.ENEMYBUILDING, MinimapMarkerTag.ENEMYBUILDING, MinimapMarkerTag.ENEMYUNIT);
            }
        }
    }


    //List manipulation functions
    public void AddPlayer(Player player)
    {
        this.player = player;
        player.levelController = this;

        //Set the player's link to the UI and elements of the personal toolbar to the player's toolkit
        player.SetPlayerUI(mainUI);
        mainUI.GetPersonalToolbar().SetAimComponentLink(player.GetAimComponent());

        //Set the RTS Controller faction to the player, so it checks the correct collision masks
        player.SetMainCamera();

        //Set the player and its resource and contructor components to the relevant faction controller
        //(Set as playerfaction - 1 since faction 1 will be index 0 on the controller list)
        factionController[playerFaction - 1].AddPlayer(player);
        //Add the player's sight component to the FOW Controller
        fowController.AddSightComponent(player.GetSightComponent());
    }
    public void RemovePlayer(Player player)
    {
        fowController.RemoveSightComponent(player.GetSightComponent());
        factionController[player.GetCurrentFaction() - 1].RemovePlayer();
        this.player = null;
    }
    public void AddBuilding(BuildingParent building)
    {
        buildingsInScene.Add(building);
        UpdateNavigationMap();
        factionController[building.GetCurrentFaction() - 1].AddBuilding(building);
        fowController.AddSightComponent(building.GetSightComponent());
    }
    public void RemoveBuilding(BuildingParent building)
    {
        buildingsInScene.Remove(building);
        UnitDeathSelectionAndControlGroupCheck(building.GetFactionComponent());

        UpdateNavigationMap();
        factionController[building.GetCurrentFaction() - 1].RemoveBuilding(building);
        fowController.RemoveSightComponent(building.GetSightComponent());
    }
    public void AddBlueprint(BlueprintParent blueprint)
    {
        blueprintsInScene.Add(blueprint);
        UpdateNavigationMap();
        factionController[blueprint.GetCurrentFaction() - 1].AddBlueprint(blueprint);
        fowController.AddSightComponent(blueprint.GetSightComponent());
    }
    public void RemoveBlueprint(BlueprintParent blueprint)
    {
        blueprintsInScene.Remove(blueprint);
        UnitDeathSelectionAndControlGroupCheck(blueprint.GetFactionComponent());

        UpdateNavigationMap();
        factionController[blueprint.GetCurrentFaction() - 1].RemoveBlueprint(blueprint);
        fowController.RemoveSightComponent(blueprint.GetSightComponent());
    }
    public void AddUnit(UnitParent unit)
    {
        unitsInScene.Add(unit);
        factionController[unit.GetCurrentFaction() - 1].AddUnit(unit);
        fowController.AddSightComponent(unit.GetSightComponent());
    }
    public void RemoveUnit(UnitParent unit)
    {
        unitsInScene.Remove(unit);
        UnitDeathSelectionAndControlGroupCheck(unit.GetFactionComponent());
        

        factionController[unit.GetCurrentFaction() - 1].RemoveUnit(unit);
        fowController.RemoveSightComponent(unit.GetSightComponent());
    }
    public void UnitDeathSelectionAndControlGroupCheck(FactionComponent unitComponent)
    {
        List<RTSSelectionType> selection = rtsController.RemoveDeadUnitFromSelection(unitComponent);
        mainUI.GetRTSToolbar().SetUnitSelectionUI(selection);
        bool unitInControlGroup = rtsController.RemoveFromControlGroupOnDeath(unitComponent);
        if (unitInControlGroup)
        {
            ControlGroup[] cGroups = rtsController.GetControlGroups();
            for (int i = 0; i < cGroups.Length; i++)
            {
                mainUI.GetControlGroupBar().SetControlGroupButtonInfo(i, cGroups[i]);
            }
        }
    }

    //Functions for the factory UI
    public void SetFactoryButtonInfo(ConstructInfo[] factoryInfo) //Run to initialise the buttons
    {
        mainUI.GetRTSToolbar().SetFactoryBuildingButtons(factoryInfo);
    }
    public void UpdateFactoryButtonQueueInfo() //run to update the button amounts
    {
        List<ConstructInfo> queueList = rtsController.GetSelectedFactoryQueue();
        mainUI.GetRTSToolbar().SetFactoryBuildingQueueAmounts(queueList);
    }
    public void ResetUIToBuildingButtons()
    {
        mainUI.GetRTSToolbar().ResetBuildingButtons();
    }


    public List<BuildingParent> GetAllBuildingsOfFaction(int factionIndex)
    {
        List<BuildingParent> factionBuildings = new();

        foreach(BuildingParent building in buildingsInScene)
        {
            if (building.GetCurrentFaction() == factionIndex)
            {
                factionBuildings.Add(building);
            }
        }

        return factionBuildings;
    }

    //Signals for adding or removing children from the scene
    public void OnObjectAddedToScene(Node node)
    {
        if (navMapInitialised) 
        {
            //Add the new node to a respective tracker list if they are part of the right group
            if (node.IsInGroup("Building"))
            {
                BuildingParent nodeCast = (BuildingParent)node;
                CallDeferred("AddBuilding", nodeCast);
            }
            else if (node.IsInGroup("Blueprint"))
            {
                BlueprintParent nodeCast = (BlueprintParent)node;
                CallDeferred("AddBlueprint", nodeCast);
            }
            else if (node.IsInGroup("Unit"))
            {
                UnitParent nodeCast = (UnitParent)node;
                CallDeferred("AddUnit", nodeCast);
            }
            else if (node.IsInGroup("Player"))
            {
                Player playerCast = (Player)node;
                CallDeferred("AddPlayer", playerCast);
            }
        }
    }
    public void OnObjectRemovedToScene(Node node)
    {
        //Remove the new node from the respective tracker list if they are part of the right group
        if (node.IsInGroup("Building"))
        {
            BuildingParent nodeCast = (BuildingParent)node;
            RemoveBuilding(nodeCast);
        }
        else if (node.IsInGroup("Blueprint"))
        {
            BlueprintParent nodeCast = (BlueprintParent)node;
            RemoveBlueprint(nodeCast);
        }
        else if (node.IsInGroup("Unit"))
        {
            UnitParent nodeCast = (UnitParent)node;
            RemoveUnit(nodeCast);
        }
        else if (node.IsInGroup("Player"))
        {
            Player playerCast = (Player)node;
            RemovePlayer(playerCast);
        }
    }
    //Signal for when a button for placing a building is pressed
    public void OnNewBuildInfo(ConstructInfo buildInfo)
    {
        //Add an item to the player's build queue if able
        if (IsInstanceValid(player))
        {
            if (player.buildingQueue.Count < 5)
            {
                player.AddBuildingToQueue(buildInfo);
                mainUI.GetBuildQueueBar().UpdateButtonQueueInfo(player.buildingQueue);
            }
        }

        //Old deprecated build function
        //mainUI.GetBuildPlacementGhost().SetNewBuildInfo(buildInfo);
        //if (playState == LevelControllerPlayState.PERSONALPLAYER)
        //{
        //    SetPersonalPlayState(PersonalPlayState.BUILDPLACEMENT);
        //}
        //else if (playState == LevelControllerPlayState.RTSCOMMAND)
        //{
        //    SetRTSPlayState(RTSPlayState.BUILDPLACEMENT);
        //}
    }
    //Signals to relay to the RTS controller when selecting or deselecting units from the UI
    public void SelectUnitOfType(UnitInfo unitType)
    {
        List<RTSSelectionType> selection = rtsController.SelectUnitOfType(unitType);
        //send the unit selection data to the UI to display
        mainUI.GetRTSToolbar().SetUnitSelectionUI(selection);
    }
    public void DeselectUnitOfType(UnitInfo unitType)
    {
        List<RTSSelectionType> selection = rtsController.DeselectUnitOfType(unitType);
        //send the unit selection data to the UI to display
        mainUI.GetRTSToolbar().SetUnitSelectionUI(selection);
    }
    //Signals for when a factory build button is pressed
    public void OnNewFactoryBuild(ConstructInfo buildInfo, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            rtsController.AddFactoryItemToQueue(buildInfo);
        }
        UpdateFactoryButtonQueueInfo();
    }
    public void OnRemoveFactoryBuild(ConstructInfo buildInfo, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            rtsController.RemoveFactoryItemFromQueue(buildInfo);
        }
        UpdateFactoryButtonQueueInfo();
    }
    

    //Baking a new navigation mesh
    public void UpdateNavigationMap()
    {
        //Generate the initial nav polygon region bounds
        NavigationPolygon newNavMesh = new();
        newNavMesh.AddOutline(mapBounds);
        newNavMesh.AgentRadius = 20;

        //Create the nav geometry data that will hold all the obstacle data
        NavigationMeshSourceGeometryData2D navGeometryData = new();

        //Get the geometry obstacle data from all buildings and blueprints
        foreach (BuildingParent building in buildingsInScene)
        {
            Vector2[] polygonVertices = building.buildingObstacleBounds.Polygon;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                polygonVertices[i] += building.GlobalPosition;
            }
            navGeometryData.AddObstructionOutline(polygonVertices);
        }
        foreach (BlueprintParent blueprint in blueprintsInScene)
        {
            Vector2[] polygonVertices = blueprint.buildingObstacleBounds.Polygon;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                polygonVertices[i] += blueprint.GlobalPosition;
            }
            navGeometryData.AddObstructionOutline(polygonVertices);
        }
        //Get the obstacle data from all other misc environment obstacles
        foreach (CollisionPolygon2D miscObstacle in miscObstaclesInScene)
        {
            Vector2[] polygonVertices = miscObstacle.Polygon;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                polygonVertices[i] += miscObstacle.GlobalPosition;
            }
            navGeometryData.AddObstructionOutline(polygonVertices);
        }

        //Todo - add nav outlines for edge of maps

        
        //bake the new region (minus low obtacles) from the mesh
        NavigationServer2D.BakeFromSourceGeometryData(newNavMesh, navGeometryData);
        navigationRegionHover.NavigationPolygon = newNavMesh;
        globals.navigationAreaHover = navigationRegionHover;

        //Get obstacle data from 'low' obstacles (water etc)
        foreach (CollisionPolygon2D lowObstacle in lowObstaclesInScene)
        {
            Vector2[] polygonVertices = lowObstacle.Polygon;
            for (int i = 0; i < polygonVertices.Length; i++)
            {
                polygonVertices[i] += lowObstacle.GlobalPosition;
            }
            navGeometryData.AddObstructionOutline(polygonVertices);
        }

        //bake the new region from the main mesh
        NavigationServer2D.BakeFromSourceGeometryData(newNavMesh, navGeometryData);
        navigationRegionGround.NavigationPolygon = newNavMesh;
        globals.navigationAreaGround = navigationRegionGround;
    }
}

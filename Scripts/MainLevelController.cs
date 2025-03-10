using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using static Godot.WebSocketPeer;

//The general state the level is in:
//Will track whether we are in the pause menu or playing, and whether we have reached a victory or loss condition
public enum GeneralLevelState 
{   
    INGAME,
    INMENU,
    WINTIMER,
    LOSSTIMER,
    ENDMENU
}


//Tracks whether we are in Pilot or Strategic mode
public enum LevelControllerPlayState
{
    PERSONALPLAYER,
    RTSCOMMAND
}

//When in pilot mode, tracks whether we are placing a structure or not
public enum PersonalPlayState
{
    STANDARD,
    BUILDPLACEMENT
}

//Tracks states while in strategic mode
public enum RTSPlayState
{
    STANDARD,
    BUILDPLACEMENT,
    SETORDER_MOVE,
    SETORDER_ATTACK
}

//Code enumerations to track whether certain controls such as movement are enabled and whether certain tech is available (mostly for tutorial purposes)
public enum TechControlCode 
{ 
    TECH_UNITSCOUT,
    TECH_UNITTANK,
    TECH_UNITSNIPER,
    TECH_NETWORKHUB,
    TECH_GENERATOR,
    TECH_MINER,
    TECH_STORAGEENERGY,
    TECH_STORAGEMETAL,
    TECH_REFINERY,
    TECH_TURRET,
    TECH_COMMANDERUNIT,
    CONTROL_RTSTOGGLE,
    CONTROL_MOVEMENT,
    CONTROL_SHOOTING,
    CONTROL_RTSSELECTION,
    CONTROL_RTSORDERS
}

public class TechControlResourceLink 
{
    public ConstructInfo tech;
    public TechControlCode code;
    public bool isEnabled;

    public TechControlResourceLink(ConstructInfo tech, TechControlCode code, bool isEnabled)
    {
        this.tech = tech;
        this.code = code;
        this.isEnabled = isEnabled;
    }
    //Alternative resource link, used for control-specific codes
    public TechControlResourceLink(TechControlCode code)
    {
        this.tech = null;
        this.code = code;
        this.isEnabled = true;
    }
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

public class LevelTechAvailabilityController 
{

    //Enabling or disabling specific controls (mostly for tutorial use)
    public bool control_RTSToggle = true;
    public bool control_Shooting = true;
    public bool control_movement = true;
    public bool control_rtsSelection = true;
    public bool control_rtsOrders = true;

    //A list linking each tech availability code to a construct info, for use in enabling or disabling build buttons
    public List<TechControlResourceLink> techAvailability = new();

    //Initialising the tech controller and all the tech availability
    public LevelTechAvailabilityController()
    {
        TechControlResourceLink newLink;
        ConstructInfo newInfo;

        //Buildings
        //Network Hub
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_NetworkHub.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_NETWORKHUB, true);
        techAvailability.Add(newLink);
        //Generator
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_Generator.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_GENERATOR, true);
        techAvailability.Add(newLink);
        //Miner
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_Miner.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_MINER, true);
        techAvailability.Add(newLink);
        //Storage - Energy
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_EnergyStorage.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_STORAGEENERGY, true);
        techAvailability.Add(newLink);
        //Storage - Metal
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_MetalStorage.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_STORAGEMETAL, true);
        techAvailability.Add(newLink);
        //Refinery
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_Refinery.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_REFINERY, true);
        techAvailability.Add(newLink);
        //Turret
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Buildings/BI_Turret.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_TURRET, true);
        techAvailability.Add(newLink);

        //Units - Factory
        //Unit 1 - Scout
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Units/Unit1.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_UNITSCOUT, true);
        techAvailability.Add(newLink);
        //Unit 2 - Tank
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Units/Unit2.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_UNITTANK, true);
        techAvailability.Add(newLink);
        //Unit 3 - Sniper
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Units/Unit3.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_UNITSNIPER, true);
        techAvailability.Add(newLink);

        //Commander unit/Player
        newInfo = ResourceLoader.Load<ConstructInfo>("res://Objects/Units and Structures/Construction Info/Units/Player.tres");
        newLink = new TechControlResourceLink(newInfo, TechControlCode.TECH_COMMANDERUNIT, true);
        techAvailability.Add(newLink);
    }
}


public partial class MainLevelController : Node2D
{
    protected Globals globals;

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
    [Signal] public delegate void FactoryUniqueQueueRemovedEventHandler(ConstructInfo buildinfo);
    //Signal for when a Control Group button is pressed
    [Signal] public delegate void SelectControlGroupButtonEventHandler(int index);
    //Signal for toggling the builder on or off
    [Signal] public delegate void ToggleBuildActivityEventHandler();

    FactionController[] factionController = new FactionController[3];

    public LevelControllerPlayState playState { get; private set; } = LevelControllerPlayState.PERSONALPLAYER;
    public PersonalPlayState personalPlayState { get; private set; } = PersonalPlayState.STANDARD;
    public RTSPlayState rtsPlayState { get; private set; } = RTSPlayState.STANDARD;

    protected int levelPhase = 1;
    public LevelTechAvailabilityController techController = new();
    [Export] ConstructInfo[] buildingList_tier1;

    public Player player { get; protected set; }
    public bool playerIsBuilding = false;
    int playerFaction = 1;
	public MainUI mainUI { get; protected set; }
    protected RTSController rtsController;
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
    public List<BuildingParent> buildingsInScene { get; protected set; } = new();
    public List<BlueprintParent> blueprintsInScene { get; protected set; } = new();
    public List<UnitParent> unitsInScene { get; protected set; } = new();

    //Trackers for each faction's resources
    ResourceStats[] factionResources = new ResourceStats[2];

    //Fog of war tracking
    [Export] protected FOWController fowController;
    ImageTexture[] fowTextures;

    //Tracker for all misc environment obstacles used when baking new navmeshes
    [Export] TileMapLayer[] terrainTilemapLayers;
    List<CollisionPolygon2D> miscObstaclesInScene = new(); //walls and high obstacles
    List<CollisionPolygon2D> lowObstaclesInScene = new(); //water and obstacles that hover can go over
    
    bool navMapInitialised = false;

    PackedScene mapEdgeScene = ResourceLoader.Load<PackedScene>("res://Objects/Other/ObstacleMapEdge.tscn");
    ObstacleMapEdge[] obstacleMapEdges = new ObstacleMapEdge[4]; //the edges of the map

    //The AI that will control the RTS movements of enemies
    [Export] public MainAIController[] aiControllers = new MainAIController[0];

    //States to control whether we are in a menu or not.
    public GeneralLevelState mainLevelState { get; protected set; } = GeneralLevelState.INGAME;

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
        FactoryUniqueQueueRemoved += OnFactoryRemovedUniqueQueue;


        SetOrderState += OnOrderStatePress;
        SelectControlGroupButton += OnControlGroupSelect;

        ToggleBuildActivity += OnToggleActivity;

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
        if (player != null) 
        { 
            AddPlayer(player);
            player.SetMainCamera();
            mainUI.GetBuildQueueBar().ToggleToolActivity(true);
        }
        else 
        {
            mainUI.GetBuildQueueBar().ToggleToolActivity(false);
            CallDeferred("ToggleRTSMode"); 
        }
        
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

        //Set all the buttons on the UI to link to the controller for signal purposes
        mainUI.SetButtonConnections(this);
        //Set the level tech and make sure the building buttons are set correctly
        InitLevelControlsAndTech();
        UpdateMainBuildingButtons();


        //Set the map bounds
        UpdateMapBounds();

        //AI Initialisation
        CallDeferred("InitAIUnitLists");
    }

    public virtual void InitAIUnitLists()
    {
        //this function will be set up separately in each level's controller
    }

    //Determines what tech is available and whether certain controls are enabled at the start
    //Default everything is enabled, but can be tweaked in scenarios (particularly tutorial)
    public virtual void InitLevelControlsAndTech()
    {
        
    }
    public void UpdateGlobalTechAndControls(TechControlCode codeToUpdate, bool isEnabled)
    {
        //Find the item in the tech controller that matches the code we want to update
        //Skip if not found (ie, it's a control specific thing being changed)
        int linkToChange = techController.techAvailability.FindIndex(x => x.code == codeToUpdate);
        if (linkToChange != -1)
        {
            //Update the item and check if any of the building buttons need updating as well
            techController.techAvailability[linkToChange].isEnabled = isEnabled;
            rtsController.GetSelectedUnitTypes();
            UpdateMainBuildingButtons();
            return;
        }
        
        //The code must be a control-related item, update as needed
        switch (codeToUpdate) 
        {
            case TechControlCode.CONTROL_RTSTOGGLE:
                techController.control_RTSToggle = isEnabled; break;
            case TechControlCode.CONTROL_MOVEMENT:
                techController.control_movement = isEnabled; break;
            case TechControlCode.CONTROL_SHOOTING:
                techController.control_Shooting = isEnabled; break;
            case TechControlCode.CONTROL_RTSSELECTION:
                techController.control_rtsSelection = isEnabled; break;
            case TechControlCode.CONTROL_RTSORDERS:
                techController.control_rtsOrders = isEnabled; break;
            default: GD.Print("A tech code is missing from the tech controller!"); break;
        }
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

        if (mainLevelState == GeneralLevelState.INGAME || mainLevelState == GeneralLevelState.WINTIMER || mainLevelState == GeneralLevelState.LOSSTIMER)
        {
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
                if (rtsController.isCurrentlySelecting || mainUI.IsOverActiveUI())
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
                        if (!mainUI.IsOverActiveUI() && techController.control_rtsSelection) //Are we not over any UI?
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
                            SetUnitSelectionUI(selection);

                        }
                        specialOrderJustExecuted = false;
                    }
                    //Pressing execute order button
                    if (Input.IsActionJustPressed("RTS_ExecuteOrder") && techController.control_rtsOrders)
                    {
                        if (!mainUI.IsOverActiveUI()) //Are we not over any UI?
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
                    if (Input.IsActionPressed("RTS_Select") && !specialOrderJustExecuted && !rtsController.isCurrentlySelecting && mainUI.IsOverActiveUI())
                    {
                        if (mainUI.GetMinimap().IsInputWithinMinimapBounds(GetViewport().GetMousePosition()) && mainUI.GetMinimap().IsMinimapFullMap())
                        {
                            //Set the camera position to the relative map position to the minimap
                            rtsController.SetCameraPosition(GetMinimapToMapPosition());
                        }
                    }

                    //Pressing any of the order hotbar hotkeys
                    if (techController.control_rtsOrders)
                    {
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
                        if (!mainUI.IsOverActiveUI()) //Are we not over any UI?
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
                        if (!mainUI.IsOverActiveUI()) //Are we not over any UI?
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
            if (Input.IsActionJustPressed("RTSModeToggle") && techController.control_RTSToggle)
            {
                CallDeferred("ToggleRTSMode");
            }

            //Pausing and unpausing
            if (Input.IsActionJustPressed("PauseGame") && mainLevelState == GeneralLevelState.INGAME) //Make this control not available if win or loss conditions are met
            {
                PauseGame();
            }
            if (Input.IsActionJustPressed("EscapeMenu") && mainLevelState == GeneralLevelState.INGAME) //Make this control not available if win or loss conditions are met
            {
                OpenEscapeMenu();
            }

            //For testing specific things
            //if (Input.IsActionJustPressed("TestingButton"))
            //{
            //    GD.Print("Disabling Unit1");
            //    UpdateGlobalTechAndControls(TechControlCode.TECH_UNITSCOUT, false);
            //}

        }
        else if (mainLevelState == GeneralLevelState.INMENU) //in the escape pause menu
        {
            if (Input.IsActionJustPressed("EscapeMenu"))
            {
                CloseEscapeMenu();
            }
        }
    }

    public bool isPlayerActive()
    {
        if (IsInstanceValid(player) || playerIsBuilding) { return true; }
        return false;
    }
    public void SetBuildPlacementItem()
    {

    }

    //Setting the pause state
    public void PauseGame()
    {
        if (mainLevelState == GeneralLevelState.INGAME)
        {
            GetTree().Paused = !GetTree().Paused;
        }
    }
    public void OpenEscapeMenu()
    {
        if (!GetTree().Paused) { GetTree().Paused = true; }
        GD.Print("Pause Menu opened");
        mainLevelState = GeneralLevelState.INMENU;
    }
    public void CloseEscapeMenu()
    {
        GetTree().Paused = false;
        GD.Print("Pause Menu closed");
        mainLevelState = GeneralLevelState.INGAME;
    }
    public void FinaliseWinMenu()
    {
        if (!GetTree().Paused) { GetTree().Paused = true; }
        mainLevelState = GeneralLevelState.ENDMENU;
    }
    public void FinaliseLossMenu()
    {
        if (!GetTree().Paused) { GetTree().Paused = true; }
        mainLevelState = GeneralLevelState.ENDMENU;
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
            SetUnitSelectionUI(selection);
            SetStateToStandard();
        }
    }

    public void SetUnitSelectionUI(List<RTSSelectionType> selection)
    {
        //Check whether a single item is selected and send info for single-unit details if so
        if (rtsController.selectedItems.Count == 1)
        {
            mainUI.GetRTSToolbar().SetUnitSelectionUI(selection, rtsController.selectedItems[0]);
        }
        else
        {
            mainUI.GetRTSToolbar().SetUnitSelectionUI(selection, null);
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

        if (!mainUI.IsOverActiveUI())
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
        SetUnitSelectionUI(new List<RTSSelectionType>());
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
        if (IsInstanceValid(player))
        {
            personalPlayState = state;
            switch (state)
            {
                case PersonalPlayState.STANDARD: { player.SetPlayerState(PlayerState.COMBAT); break; }
                case PersonalPlayState.BUILDPLACEMENT: { player.SetPlayerState(PlayerState.BUILDPLACING); break; }
            }
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
        if (rtsController.selectedItems.Count > 0 && !rtsController.isCurrentlySelecting && techController.control_rtsOrders)
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
        //Set minimap FOW based on the FOW visibility
        if (fowController.Visible == true) { mainUI.GetMinimap().drawMinimapfow = true; }
        else { mainUI.GetMinimap().drawMinimapfow = false; }

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
                if (combatant.spottedByFaction[playerFaction] || fowController.Visible == false)
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
    public virtual void AddPlayer(Player player)
    {
        this.player = player;
        player.levelController = this;

        //Set the player's link to the UI and elements of the personal toolbar to the player's toolkit
        player.SetPlayerUI(mainUI);
        mainUI.GetPersonalToolbar().SetAimComponentLink(player.GetAimComponent());

        //Set the RTS Controller faction to the player, so it checks the correct collision masks
        //player.SetMainCamera();

        //Set the player and its resource and contructor components to the relevant faction controller
        //(Set as playerfaction - 1 since faction 1 will be index 0 on the controller list)
        factionController[playerFaction - 1].AddPlayer(player);
        
        //Add the player's sight component to the FOW Controller
        fowController.AddSightComponent(player.GetSightComponent());
    }
    public virtual void RemovePlayer(Player player)
    {
        fowController.RemoveSightComponent(player.GetSightComponent());
        factionController[player.GetCurrentFaction() - 1].RemovePlayer();
        this.player = null;
    }
    public virtual void AddBuilding(BuildingParent building)
    {
        buildingsInScene.Add(building);
        UpdateNavigationMap();
        factionController[building.GetCurrentFaction() - 1].AddBuilding(building);
        fowController.AddSightComponent(building.GetSightComponent());
    }
    public virtual void RemoveBuilding(BuildingParent building)
    {
        buildingsInScene.Remove(building);
        UnitDeathSelectionAndControlGroupCheck(building.GetFactionComponent());

        UpdateNavigationMap();
        factionController[building.GetCurrentFaction() - 1].RemoveBuilding(building);
        fowController.RemoveSightComponent(building.GetSightComponent());
    }
    public virtual void AddBlueprint(BlueprintParent blueprint)
    {
        blueprintsInScene.Add(blueprint);
        UpdateNavigationMap();
        factionController[blueprint.GetCurrentFaction() - 1].AddBlueprint(blueprint);
        fowController.AddSightComponent(blueprint.GetSightComponent());
    }
    public virtual void RemoveBlueprint(BlueprintParent blueprint)
    {
        blueprintsInScene.Remove(blueprint);
        UnitDeathSelectionAndControlGroupCheck(blueprint.GetFactionComponent());

        UpdateNavigationMap();
        factionController[blueprint.GetCurrentFaction() - 1].RemoveBlueprint(blueprint);
        fowController.RemoveSightComponent(blueprint.GetSightComponent());
    }
    public virtual void AddUnit(UnitParent unit)
    {
        unitsInScene.Add(unit);
        factionController[unit.GetCurrentFaction() - 1].AddUnit(unit);
        fowController.AddSightComponent(unit.GetSightComponent());
    }
    public virtual void RemoveUnit(UnitParent unit)
    {
        unitsInScene.Remove(unit);
        UnitDeathSelectionAndControlGroupCheck(unit.GetFactionComponent());
        

        factionController[unit.GetCurrentFaction() - 1].RemoveUnit(unit);
        fowController.RemoveSightComponent(unit.GetSightComponent());
    }
    public virtual void UnitDeathSelectionAndControlGroupCheck(FactionComponent unitComponent)
    {
        List<RTSSelectionType> selection = rtsController.RemoveDeadUnitFromSelection(unitComponent);
        SetUnitSelectionUI(selection);
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
        //Create a filtered list of the build items based on their tech availability
        List<ConstructInfo> filteredInfo = new();
        for (int i = 0; i < factoryInfo.Length; i++)
        {
            int linkIndex = techController.techAvailability.FindIndex(x => x.tech == factoryInfo[i]);
            if (linkIndex != -1)
            {
                if (techController.techAvailability[linkIndex].isEnabled)
                {
                    filteredInfo.Add(factoryInfo[i]);
                }
            }
        }

        mainUI.GetRTSToolbar().SetFactoryBuildingButtons(filteredInfo);
    }
    public void UpdateFactoryButtonQueueInfo() //run to update the button queue amounts
    {
        List<ConstructInfo> queueList = rtsController.GetSelectedFactoryQueue();
        mainUI.GetRTSToolbar().SetFactoryBuildingQueueAmounts(queueList);
    }
    public void ResetUIToBuildingButtons() //Resetting the UI back to the main building buttons after deselecting the factory
    {
        mainUI.GetRTSToolbar().ResetBuildingButtons();
    }
    //changes the visibility of buttons (usually run after tech availability changes)
    public void UpdateMainBuildingButtons()
    {
        //Create a filtered list of the build items based on their tech availability
        List<ConstructInfo> filteredInfo = new();
        for (int i = 0; i<buildingList_tier1.Length; i++)
        {
            int linkIndex = techController.techAvailability.FindIndex(x => x.tech == buildingList_tier1[i]);
            if (linkIndex != -1)
            {
                if (techController.techAvailability[linkIndex].isEnabled)
                {
                    filteredInfo.Add(buildingList_tier1[i]);
                }
            }
        }

        //Send this info to the buttons
        mainUI.GetPersonalToolbar().SetBuildingButtons(filteredInfo);
        mainUI.GetRTSToolbar().SetBuildingButtons(filteredInfo);
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
    public void OnObjectRemovedFromScene(Node node)
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
        SetUnitSelectionUI(selection);
    }
    public void DeselectUnitOfType(UnitInfo unitType)
    {
        List<RTSSelectionType> selection = rtsController.DeselectUnitOfType(unitType);
        //send the unit selection data to the UI to display
        SetUnitSelectionUI(selection);
    }
    //Signals for when a factory build button is pressed
    public void OnNewFactoryBuild(ConstructInfo buildInfo, int amount)
    {

        switch (buildInfo.uniqueIdentifier) 
        {
            case CI_UniqueIdentifier.PLAYER:
                if (!isPlayerActive()) 
                { 
                    rtsController.AddFactoryItemToQueue(buildInfo);
                    playerIsBuilding = true;
                    mainUI.GetRTSToolbar().ToggleFactoryBuildButtonEnable(0, false); 
                }
                return;
            default: break;
        }

        for (int i = 0; i < amount; i++)
        {
            rtsController.AddFactoryItemToQueue(buildInfo);
        }
        UpdateFactoryButtonQueueInfo();
    }
    public void OnRemoveFactoryBuild(ConstructInfo buildInfo, int amount)
    {
        bool uniqueItemRemoved = false;
        for (int i = 0; i < amount; i++)
        {
            if (rtsController.RemoveFactoryItemFromQueue(buildInfo)) { uniqueItemRemoved = true; }
        }
        UpdateFactoryButtonQueueInfo();

        if (buildInfo.uniqueIdentifier == CI_UniqueIdentifier.PLAYER && uniqueItemRemoved) { playerIsBuilding = false; mainUI.GetRTSToolbar().ToggleFactoryBuildButtonEnable(0, true); }
    }
    //Executes when a building dies that has a unique item in its build queue. To tell the controller that it's no longer being built
    public void OnFactoryRemovedUniqueQueue(ConstructInfo buildInfo)
    {
        switch (buildInfo.uniqueIdentifier)
        {
            case CI_UniqueIdentifier.PLAYER: playerIsBuilding = false; break;
            default: break;
        }
    }

    //Signal for build active toggle
    public void OnToggleActivity()
    {
        //is the player alive?
        if (IsInstanceValid(player))
        {
            bool buildToggle = player.ToggleBuildToolActive();
            mainUI.GetBuildQueueBar().ToggleToolActivity(buildToggle);
        }
    }
    

    //Baking a new navigation mesh
    public void UpdateNavigationMap()
    {
        //Generate the initial nav polygon region bounds
        NavigationPolygon newNavMesh = new();
        newNavMesh.AddOutline(mapBounds);
        newNavMesh.AgentRadius = 30;

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

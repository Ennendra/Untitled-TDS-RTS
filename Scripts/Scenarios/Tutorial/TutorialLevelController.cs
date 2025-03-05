using Godot;
using System;
using System.Collections.Generic;

public partial class TutorialLevelController : MainLevelController
{
    //List of tutorial based items
    List<TutorialWaypoint> p2Waypoints = new();
    List<BuildingParent> p3Dummies = new();

    //The scenes to instantiate the needed tutorial items
    PackedScene p2WaypointScene;
    PackedScene p3DummyScene;

    [Export] BuildingFactory tutorialFactory;






    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		base._Ready();

        //scene for waypoints on movement
        p2WaypointScene = ResourceLoader.Load<PackedScene>("res://Objects/Levels/Tutorial/TutorialWaypoint.tscn");
        p3DummyScene = ResourceLoader.Load<PackedScene>("res://Objects/Units and Structures/Buildings/BuildingMetalStorage.tscn");

        //Starting mission text
        mainUI.SetNewMissionText("This tutorial will give you a rundown of the basic controls of the game.\nWe will start with personal vehicle controls and move later into the strategic view.\nPress [Enter] to continue");
	}

    public override void InitLevelControlsAndTech()
    {
        base.InitLevelControlsAndTech();

		UpdateGlobalTechAndControls(TechControlCode.TECH_UNITSCOUT, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_UNITTANK, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_UNITSNIPER, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_COMMANDERUNIT, false);

        UpdateGlobalTechAndControls(TechControlCode.TECH_TURRET, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_REFINERY, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_GENERATOR, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_MINER, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_NETWORKHUB, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_STORAGEENERGY, false);
        UpdateGlobalTechAndControls(TechControlCode.TECH_STORAGEMETAL, false);

        UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSORDERS, false);
        UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSSELECTION, false);
        UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSTOGGLE, false);
        UpdateGlobalTechAndControls(TechControlCode.CONTROL_SHOOTING, false);
        UpdateGlobalTechAndControls(TechControlCode.CONTROL_MOVEMENT, false);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		base._Process(delta);

        ProcessMissionPhases(levelPhase);
	}

    public void ProcessMissionPhases(int phase)
    {
        switch (phase) 
        {
            case 1: //Start - will progress after pressing enter
                ProcessPhase1();
                break;
            case 2: //Movement - will progress after the 4 waypoints are driven over
                ProcessPhase2();
                break;
            case 3: //Shooting - Will progress after the 3 dummies are destroyed
                ProcessPhase3();
                break;
            case 4:
                ProcessPhase4();
                break;
            case 5:
                ProcessPhase5();
                break;
            case 6:
                ProcessPhase6();
                break;
            case 7:
                ProcessPhase7();
                break;
            case 8:
                ProcessPhase8();
                break;
            case 9:
                ProcessPhase9();
                break;
            case 10:
                ProcessPhase10();
                break;
            case 11:
                ProcessPhase11();
                break;
            case 12:
                ProcessPhase12();
                break;
            case 13:
                ProcessPhase13();
                break;
            case 14:
                ProcessPhase14();
                break;
            case 15:
                ProcessPhase15();
                break;
            case 16:
                ProcessPhase16();
                break;
            case 17:
                ProcessPhase17();
                break;
            case 18:
                ProcessPhase18();
                break;
            case 19:
                ProcessPhase19();
                break;
            case 20:
                ProcessPhase20();
                break;
            case 21:
                ProcessPhase21();
                break;
            case 22:
                ProcessPhase22();
                break;
            case 23:
                ProcessPhase23();
                break;
            case 24:
                ProcessPhase24();
                break;
            case 25:
                ProcessPhase25();
                break;
            case 26:
                ProcessPhase26();
                break;
            case 27:
                ProcessPhase27();
                break;
            case 28:
                ProcessPhase28();
                break;
            case 29:
                ProcessPhase29();
                break;
            case 30:
                ProcessPhase30();
                break;
        }

    }

    //Phase specific code
    public void ProcessPhase1()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            //Create the waypoints for player movement
            CreateMovementWaypoint(new Vector2(1125, 1325));
            CreateMovementWaypoint(new Vector2(1875, 1325));
            CreateMovementWaypoint(new Vector2(1875, 1975));
            CreateMovementWaypoint(new Vector2(1125, 1975));
            //Allow player movement
            UpdateGlobalTechAndControls(TechControlCode.CONTROL_MOVEMENT, true);
            mainUI.SetNewMissionText("We'll start with movement. Use [WASD] keys to move your vehicle around.\nMove over the waypoints that have just appeared.\nDo note that your vehicle can hover freely over water.");
            levelPhase++;
        }
    }

    public void ProcessPhase2()
    {
        if (p2Waypoints.Count > 0)
        {
            List<TutorialWaypoint> wpToRemove = new();
            foreach (TutorialWaypoint waypoint in p2Waypoints)
            {
                if (!IsInstanceValid(waypoint)) { wpToRemove.Add(waypoint); }
            }
            foreach (TutorialWaypoint waypoint in wpToRemove)
            { p2Waypoints.Remove(waypoint); }
        }
        else
        {
            //Create dummies for the shooting tutorial
            CreateTargetDummy(new Vector2(1500, 1650));
            CreateTargetDummy(new Vector2(1150, 1650));
            CreateTargetDummy(new Vector2(1850, 1650));
            //Allow player shooting
            UpdateGlobalTechAndControls(TechControlCode.CONTROL_SHOOTING, true);
            mainUI.SetNewMissionText("Very good. Now we will move to aiming and shooting.\nUse the mouse to aim your vehicle turret and the left mouse button to fire.\nDestroy the target dummies that have just appeared.");
            levelPhase++;
        }
    }

    public void ProcessPhase3()
    {
        if (p3Dummies.Count > 0)
        {
            GD.Print(p3Dummies.Count);
            List<BuildingParent> dummyToRemove = new();
            foreach (BuildingParent dummy in p3Dummies)
            {
                if (!IsInstanceValid(dummy)) { dummyToRemove.Add(dummy); }
            }
            foreach (BuildingParent dummy in dummyToRemove)
                { p3Dummies.Remove(dummy); }
        }
        else
        {
            
            mainUI.SetNewMissionText("Good. With the general vehicle basics out of the way, we will move on to base building.\nBelow, on the left side, is your build menu, where you can queue buildings to place on the field. It is currently blank but we will slowly bring the tech up as we go.\nPress [Enter] to continue");
            levelPhase++;
        }
    }
    public void ProcessPhase4()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            //Set the tech for a network hub
            UpdateGlobalTechAndControls(TechControlCode.TECH_NETWORKHUB, true);
            mainUI.SetNewMissionText("The first building is the network pylon. All other buildings need this building nearby to operate, and you can only deploy other buildings within a network pylon's range.\nClick the button below to add it to the build queue.");
            levelPhase++;
        }
    }
    public void ProcessPhase5()
    {
        if (player.buildingQueue.Count > 0)
        {
            mainUI.SetNewMissionText("On the left side of the HUD you can see building being constructed. When a building has finished constructing, you can select it for deployment by either clicking on it's button, or pressing [1-5] to select the respective queue item\nOnce selected, left click the desired location to place it on the field.\nPlace a network pylon now.");
            levelPhase++;
        }
    }
    public void ProcessPhase6()
    {
        //Make sure an extra network hub is placed on the field
        List<BuildingParent> requiredBuildings = new();
        foreach (BuildingParent building in buildingsInScene)
        {
            if (building.GetFactionComponent().unitInfo.unitName == "Network Pylon") { requiredBuildings.Add(building); }
        }
        if (requiredBuildings.Count >= 5)
        {
            mainUI.SetNewMissionText("Before progressing on buildings, we will first describe how the resource system works.\nOn the top of your HUD is the resource tracker. It tracks the generation rate of your two main resources; Energy and Metal. If either is drained, the rate in which buildings and units construct slows to match your current supply rate. This performance rate is tracked at the center of the resource tracker.\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase7()
    {
        //Checking that enter is pressed
        if (Input.IsActionJustPressed("TextContinue"))
        {
            //Set the tech for a network hub
            UpdateGlobalTechAndControls(TechControlCode.TECH_GENERATOR, true);
            mainUI.SetNewMissionText("We will now build a generator. This building passively generates energy at a steady rate.\nBuild and place a generator now.");
            levelPhase++;
        }
    }
    public void ProcessPhase8()
    {
        //Make sure a generator is placed
        List<BuildingParent> requiredBuildings = new();
        foreach (BuildingParent building in buildingsInScene)
        {
            if (building.GetFactionComponent().unitInfo.unitName == "Generator") { requiredBuildings.Add(building); }
        }
        if (requiredBuildings.Count >= 7)
        {
            mainUI.SetNewMissionText("Before we move on to the miner, we will quickly touch on the repair and reclaim tools.\nTo the right of your build menu your tools are displayed. Your repair tool is used to repair damaged structures and units, while the reclaim tool is used to unmake your buildings and units if they are not wanted or misplaced - regaining some of their cost in the process.\nLeft-click the buttons below to select the desired tool and right-click on the target to use the tool on it.\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase9()
    {
        //Checking that enter is pressed
        if (Input.IsActionJustPressed("TextContinue"))
        {
            //Set the tech for a network hub
            UpdateGlobalTechAndControls(TechControlCode.TECH_MINER, true);
            mainUI.SetNewMissionText("Now to build the miner.\nUnlike other buildings that can be built anywhere in a network hub's range, the miner must *also* be deployed on top of a mining node; the blue marking on the ground. The miner, when deployed on such a node, passively generates metal.\nBuild and deploy a miner on the nearby mining node now. If you placed a building on top of the node already, use the reclaim tool to remove the obstacle first.");
            levelPhase++;
        }
    }
    public void ProcessPhase10()
    {
        //Make sure a miner is placed
        List<BuildingParent> requiredBuildings = new();
        foreach (BuildingParent building in buildingsInScene)
        {
            if (building.GetFactionComponent().unitInfo.unitName == "Miner") { requiredBuildings.Add(building); }
        }
        if (requiredBuildings.Count >= 4)
        {
            UpdateGlobalTechAndControls(TechControlCode.TECH_REFINERY, true);
            mainUI.SetNewMissionText("Our next building is the converter. This building requires a steady stream of energy to operate, but will passively generate additional metal without requiring a mining node.\nBuild and place a refinery now.");
            levelPhase++;
        }
    }
    public void ProcessPhase11()
    {
        //Make sure a refinery/converter is placed
        List<BuildingParent> requiredBuildings = new();
        foreach (BuildingParent building in buildingsInScene)
        {
            if (building.GetFactionComponent().unitInfo.unitName == "Converter") { requiredBuildings.Add(building); }
        }
        if (requiredBuildings.Count >= 1)
        {
            UpdateGlobalTechAndControls(TechControlCode.TECH_STORAGEENERGY, true);
            UpdateGlobalTechAndControls(TechControlCode.TECH_STORAGEMETAL, true);
            mainUI.SetNewMissionText("Next up: Storage. The energy and metal storage buildings increase your maximum capacity for each resource in the case of demand spikes. Build and place one of each now.");
            levelPhase++;
        }
    }
    public void ProcessPhase12()
    {
        //Make sure a metal and energy storage is placed
        List<BuildingParent> requiredEnergyBuildings = new();
        List<BuildingParent> requiredMetalBuildings = new();
        foreach (BuildingParent building in buildingsInScene)
        {
            if (building.GetFactionComponent().unitInfo.unitName == "Energy Storage") { requiredEnergyBuildings.Add(building); }
            if (building.GetFactionComponent().unitInfo.unitName == "Metal Storage") { requiredMetalBuildings.Add(building); }
        }
        if (requiredEnergyBuildings.Count >= 9 && requiredMetalBuildings.Count >= 9)
        {
            mainUI.SetNewMissionText("Excellent. This covers the basics of base building. Other buildings will be used in your battles - such as turrets and factories - but we will now move on to the strategic view.\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase13()
    {
        //Checking that enter is pressed
        if (Input.IsActionJustPressed("TextContinue"))
        {
            UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSTOGGLE, true);
            mainUI.SetNewMissionText("Currently, everything has been done through the pilot mode; using direct control of your vehicle. In strategic mode, we can manage the production queue of factories and issue orders to units.\nTo switch from pilot mode to strategic mode (and vice versa), press the [Tab] key.\nDo so now.");
            levelPhase++;
        }
    }
    public void ProcessPhase14()
    {
        //Checking that we have switched to RTS mode
        if (playState == LevelControllerPlayState.RTSCOMMAND)
        {
            UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSTOGGLE, false);
            mainUI.SetNewMissionText("Under normal circumstances, you can freely switch between pilot mode and strategic mode, but for the purpose of this tutorial, we will lock you into strategic mode.\nIn strategic mode, we no longer pilot our vehicle but instead can move the camera freely across the field. You can do so by either moving the mouse to the edges of the HUD, or by pressing the [Arrow] keys.\nTake note that while you can add buildings to your build queue here, they can only be placed while in pilot mode.\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase15()
    {
        //Checking that enter is pressed
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("You may notice that certain areas of the map are obscured. This is the fog of war, and your units and buildings have a limited sight range to spot enemy activity.\nYou will not be able to see enemy activity outside of your vision range.\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase16()
    {
        //Checking that enter is pressed
        if (Input.IsActionJustPressed("TextContinue"))
        {
            UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSSELECTION, true);
            mainUI.SetNewMissionText("Now we will talk about unit and building selection. If you left-click on one of your buildings or units, you will select it, showing it's details below.\nOn the upper-left side of the field is a factory building.\nSelect the factory now.");
            levelPhase++;
        }
    }
    public void ProcessPhase17()
    {
        if (rtsController.selectedItems.Count == 1)
        {
            if (rtsController.selectedItems[0].unitInfo.unitName == "Basic Factory")
            {
                UpdateGlobalTechAndControls(TechControlCode.TECH_UNITSCOUT, true);
                mainUI.SetNewMissionText("Notice that as you select the factory, the build menu below changes. While you have only factories selected, the build menu will be replaced by a build menu of units that the factories can produce.\nClicking the buttons will add that unit into the factory's build queue, while holding [Shift] and clicking the button will add 5 at a time. Right-clicking will remove them from the queue.\nUse this factory to build 3 scout units.");
                levelPhase++;
            }
        }
    }
    public void ProcessPhase18()
    {
        //Make sure a refinery/converter is placed
        List<UnitParent> requiredUnits = new();
        foreach (UnitParent unit in unitsInScene)
        {
            if (unit.GetFactionComponent().unitInfo.unitName == "Scout") { requiredUnits.Add(unit); }
        }
        if (requiredUnits.Count >= 3)
        {
            UpdateGlobalTechAndControls(TechControlCode.TECH_UNITSCOUT, false);
            tutorialFactory.GetFactoryComponent().ClearBuildQueue();
            mainUI.SetNewMissionText("Excellent. Notice the extended sight range of the units you just constructed.\nNow we will select these units. To select multiple units, left-click and hold the button down. Moving the mouse created a selection box, selecting all units inside. Select your scouts now.");
            levelPhase++;
        }
    }
    public void ProcessPhase19()
    {
        int correctSelection = 0;
        foreach (FactionComponent comp in rtsController.selectedItems)
        {
            if (comp.unitInfo.unitName == "Scout") {  correctSelection++; }
        }
        if (correctSelection >= 3)
        {
            UpdateGlobalTechAndControls(TechControlCode.CONTROL_RTSORDERS, true);
            CreateTargetDummy(new Vector2(2850, 200));
            CreateTargetDummy(new Vector2(2850, 900));
            mainUI.SetNewMissionText("To issue orders to these units, simply right-click on the location you want your units to move to.\nIssuing an order on an enemy will order the selected units to attack that unit, while ordering on an ally orders them to guard and follow.\nSome dummies have been deployed to the east side. Order your units to destroy them.");
            levelPhase++;
        }
    }
    public void ProcessPhase20()
    {
        if (p3Dummies.Count > 0)
        {
            GD.Print(p3Dummies.Count);
            List<BuildingParent> dummyToRemove = new();
            foreach (BuildingParent dummy in p3Dummies)
            {
                if (!IsInstanceValid(dummy)) { dummyToRemove.Add(dummy); }
            }
            foreach (BuildingParent dummy in dummyToRemove)
            { p3Dummies.Remove(dummy); }
        }
        else
        {

            mainUI.SetNewMissionText("Excellent. You now know the basics of operating in strategic mode. We will now cover some more advanced items, beginning with control groups\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase21()
    {
        //Checking that enter is pressed
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("Control groups are custom selection groups that you may want to select quickly, such as an attack or defense group, or even a group of factories.\nTo create a control group, first select one or more units or buildings. Then, while holding the [Ctrl] key, press any number key [0-9] to assign it to that group number.\nTo select that group afterwards, simply press that number key.\nAssign one or more of your items to control group 1 now.");
            levelPhase++;
        }
    }
    public void ProcessPhase22()
    {
        if (rtsController.GetControlGroups()[0].units.Count > 0)
        {
            mainUI.SetNewMissionText("Note that when you assigned the control group, it shows up on the right side of your HUD. You can also use those buttons to select the control group.\nThere is only one last thing to cover, which will require your commander unit to be destroyed.\nPress [Enter] to continue.");
            levelPhase++;
        }
    }
    public void ProcessPhase23()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            player.GetDamageComponent().TakeDamage(player.GetDamageComponent().maxHealth, DamageType.COMBATDAMAGE, null);
            UpdateGlobalTechAndControls(TechControlCode.TECH_COMMANDERUNIT, true);
            mainUI.SetNewMissionText("If your commander unit is destroyed, you will not be able to deploy any new structures. However, your vehicle can be recovered on any of your network pylons.\nTo do so, select any pylon and then select the commander unit in the HUD to construct it, similar to how you build units from your factory.\nNote that you cannot build multiple commander units or queue them on multiple pylons.\nSet your commander unit to rebuild now.");
            levelPhase++;
        }
    }
    public void ProcessPhase24()
    {
        foreach (BuildingParent building in buildingsInScene)
        {
            if (building.GetFactionComponent().unitInfo.unitName == "Network Pylon") 
            {
                if (building.GetFactoryComponent().GetBuildQueue().Count>0)
                {
                    mainUI.SetNewMissionText("This concludes the tutorial! You may exit via the [Escape] key followed by exit tutorial. If you wish, I will give a few extra tips on controlling units in the field.\nSimply press [Enter] now and after each of these tips to read them.");
                    levelPhase++;
                }
            }
        }
    }
    //Additional tips
    public void ProcessPhase25() 
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("When selecting units or structures, if you hold shift, you will add the units to your current selection without deselecting your old units.\nDouble-clicking a unit or structure will select all identical units present on the screen.");
            levelPhase++;
        }
    }
    public void ProcessPhase26()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("Holding shift while selecting a control group will add that control group to your current selection. Holding both ctrl and shift will add your current selection to the control group");
            levelPhase++;
        }
    }
    public void ProcessPhase27()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("The minimap on the bottom-right of your HUD shows a view of the full battlefield. While in strategic view, left clicking the minimap will center the camera at that relative point on the field.\nYou can also issue orders to a relative point on the field in the same fashion.");
            levelPhase++;
        }
    }
    public void ProcessPhase28()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("On the bottom-left of your HUD is the action bar. You can designate more precise orders by selecting one of the order buttons and then left-clicking on the field.\nFor example, if you select an attack order and left click on the field, you will issue a move-attack command, and your units will stop to engage any enemies in its path.");
            levelPhase++;
        }
    }
    public void ProcessPhase29()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("If you need to hide the HUD at any point, simply hold down the [Alt] key.");
            levelPhase++;
        }
    }
    public void ProcessPhase30()
    {
        if (Input.IsActionJustPressed("TextContinue"))
        {
            mainUI.SetNewMissionText("This concludes the additional tutorial information. Press [Escape] and exit the tutorial.");
            levelPhase++;
        }
    }

    //Creates the movement tutorial waypoint
    public void CreateMovementWaypoint(Vector2 position)
    {
        TutorialWaypoint newWaypoint = p2WaypointScene.Instantiate<TutorialWaypoint>();
        GetTree().CurrentScene.AddChild(newWaypoint);
        newWaypoint.GlobalPosition = position;
        p2Waypoints.Add(newWaypoint);
    }

    public void CreateTargetDummy(Vector2 position)
    {
        BuildingParent newDummy = p3DummyScene.Instantiate<BuildingParent>();
        GetTree().CurrentScene.AddChild(newDummy);
        newDummy.GlobalPosition = position;
        newDummy.SetNewFaction(2);
        p3Dummies.Add(newDummy);
    }
}

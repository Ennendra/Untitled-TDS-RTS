using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

//A class to help with tracking and processing actions for individual groups of units
public class AIControlGroup 
{
    public MainAIController aiController;

	public bool isAttackGroup = true;

    public List<UnitParent> units = new();

    //An array that tells us how many of each unit type we want
    //0 = Unit1 (scout) --- 1 = Unit2 (Tank) --- 2 = Unit3 (Sniper)
    //NOTE: For attack groups, this is optional. It will set the first index to -1 if there is no specific group wanted, in which the group will be assembled with random units
    public int[] wantedUnits = new int[5] { 0, 0, 0, 0, 0 };
    public int[] unitsSupplied = new int[5] { 0, 0, 0, 0, 0 };
    //Units in reserve are units that are being supplied for this group, but are currently under construction
    public int[] unitsInReserve = new int[5] { 0, 0, 0, 0, 0 };

    public Vector2[] waypoints;
	public int currentWaypoint=-1;
    //Tells us when all units wanted are supplied, either by the units supplied or by the group size
    //This is only used for attack waves, will *always* be false for defenders
	public bool groupAssembled = false;

    //Utility: Use GD.Randi to get a random int between min (inclusive) and max (non-inclusive)
    //e.g., min 0 and 10 max will give a value between 0 and 9
    public int RandIntRange(int min, int max)
    {
        return (int)(GD.Randi() % max + min);
    }

    //Initialise the group waypoints and what we want for the group (either with a random set or a specific one)
    public void InitGroup(Vector2[] waypoints, int groupSize, int[] buildWeight)
	{
        this.waypoints = new Vector2[waypoints.Length];
        for (int i=0; i<waypoints.Length; i++)
        {
            this.waypoints[i] = waypoints[i];
        }
        //Generate a random set of wanted units
        for (int i=0; i <groupSize; i++)
        {
            int nextUnitIndex = RandIntRange(0, buildWeight.Length);
            int nextUnit = buildWeight[nextUnitIndex];
            wantedUnits[nextUnit]++;
        }
	}
	public void InitGroup(Vector2[] waypoints, int[] unitList)
	{
        //Setting the group's attack movement path (or defense patrol path)
        this.waypoints = new Vector2[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            this.waypoints[i] = waypoints[i];
        }
        //Adding the units to its wanted list for factories to build
        for (int i=0; i< unitList.Length; i++)
        {
            wantedUnits[unitList[i]]++;
        }
    }
	public void SetAsDefenseGroup() { isAttackGroup = false; }

    //Checks if one or all units are idle
    public bool IsAllUnitsIdle()
	{
		foreach (UnitParent unit in units)
		{
			if (unit.GetAIComponent().unitState != AIUnitState.IDLE)
			{
				return false;
			}
		}
		return true;
	}
    public bool IsUnitIdle(UnitParent unit)
    {
        return (unit.GetAIComponent().unitState == AIUnitState.IDLE || unit.GetAIComponent().unitState == AIUnitState.MOVE);
    }

    //Spaces the movement locations for multiple units (Taken from RTS controller)
    public List<Vector2> GetMovePositionArray(Vector2 firstPosition, int maxPositions)
    {
        List<Vector2> newOrderPositions = new();

        int currentPositionsFilled = 0; //How many positions have been prepared for the selected units
        int positionRotationAmount = 6, positionRotationIndex = 0, positionLength = 60; //Used to prepare positions around the center point
        while (currentPositionsFilled < maxPositions)
        {
            //Set the first order position
            if (currentPositionsFilled == 0)
            {
                newOrderPositions.Add(firstPosition);
                currentPositionsFilled++;
            }
            else
            {
                //prepare positions rotated in a circle centered at the first order position
                //It rotates at the specified 'length' an amount of times (positionRotationAmount) to run a full circle, then moves to a new layer (wider circle)
                float rotationAmount = Mathf.DegToRad(positionRotationIndex * (360 / positionRotationAmount));
                Vector2 newPositionOffset = new Vector2(positionLength, 0);
                newPositionOffset = newPositionOffset.Rotated(rotationAmount);
                newOrderPositions.Add(firstPosition + newPositionOffset);

                //increment the rotation on this layer
                positionRotationIndex++;

                //If we've done one full circle, prepare the next layer
                if (positionRotationIndex >= positionRotationAmount)
                {
                    positionLength += 60;
                    positionRotationAmount += 6;
                    positionRotationIndex = 0;
                }

                currentPositionsFilled++;
            }

        }

        return newOrderPositions;
    }

    //Attackers: Set attackmoves through waypoints, then targets nearest enemies
    //Defenders: Cycle through waypoints as a patrol route
    public void MoveToNextWaypoint()
    {
        //If there was only 1 waypoint, it just means a defense group that's holding one position.
        //It's movement check on the point is already done in AITickDefense so no more is necessary
        if (waypoints.Length > 1)
        {

            currentWaypoint++;
            if (currentWaypoint < waypoints.Length)
            {
                List<Vector2> newOrderPositions = GetMovePositionArray(waypoints[currentWaypoint], units.Count);
                int currentMoveIndex = 0;
                //move to next waypoint
                foreach (UnitParent unit in units)
                {
                    if (isAttackGroup)
                        { unit.GetAIComponent().SetNewAttackMoveOrder(newOrderPositions[currentMoveIndex]); }
                    else if (unit.GlobalPosition.DistanceTo(waypoints[currentWaypoint]) > 350)
                        { unit.GetAIComponent().SetNewMoveOrder(newOrderPositions[currentMoveIndex]); }
                    currentMoveIndex++;
                }

            }
            else
            {
                if (isAttackGroup)
                {
                    //Attack: Target nearest enemy and attackmove towards it
                    if (units.Count > 0)
                    {
                        Vector2 newAttackPosition = aiController.FindNearestEnemy(units[0].GlobalPosition);
                        
                        List<Vector2> newOrderPositions = GetMovePositionArray(newAttackPosition, units.Count);
                        int currentMoveIndex = 0;

                        foreach (UnitParent unit in units)
                        {
                            unit.GetAIComponent().SetNewAttackMoveOrder(newOrderPositions[currentMoveIndex]);
                            currentMoveIndex++;
                        }
                    }
                }
                else
                {
                    //Defense: reset patrol route
                    currentWaypoint = 0;
                }
            }
        }
        
    }


    public void ProcessAITick()
	{
        if (isAttackGroup) { AITickAttack(); }
        else { AITickDefense(); }
	}
    //Attacker functions
	public void AITickAttack()
	{
        //Move the group to each waypoint if the group was assembled at base, otherwise, wait until we have enough for the attack
        if (groupAssembled)
        {
            if (IsAllUnitsIdle())
            {
                MoveToNextWaypoint();
            }
        }
        else
        {
            //Check that each unit type has been adequately supplied
            int unitSetSupplied = 0;
            for (int i = 0; i < wantedUnits.Length; i++)
            {
                if (unitsSupplied[i] >= wantedUnits[i])
                {
                    unitSetSupplied++;
                }
            }
            //Consider the group assembled once all items have been supplied
            if (unitSetSupplied >= wantedUnits.Length)
            {
                groupAssembled = true;
            }
        }
    }
    
    //Defender functions
    public void AITickDefense()
	{
        //Only process if there is units currently active
        if (units.Count > 0)
        {

            //Set the waypoint to 0 if not assigned yet (part of preventing attack waves from starting on second WP)
            if (currentWaypoint == -1) { currentWaypoint = 0; }
            List<Vector2> newOrderPositions = GetMovePositionArray(waypoints[currentWaypoint], units.Count);
            int currentMoveIndex = 0;
            foreach (UnitParent unit in units)
            {
                //Move units back to the current defense point if idle and too far away from it
                if (unit.GetAIComponent().unitState == AIUnitState.IDLE && unit.GlobalPosition.DistanceTo(waypoints[currentWaypoint]) > 200)
                {
                    unit.GetAIComponent().SetNewMoveOrder(newOrderPositions[currentMoveIndex]);
                    currentMoveIndex++;
                }
            }
            //Continue patrol if all units are assembled and idle
            if (IsAllUnitsIdle())
            {
                MoveToNextWaypoint();
            }

        }
    }
    //Setting reactions for the defense group if attacked
    public void RespondToAttack(Vector2 positionOfAttack)
    {
        List<Vector2> newOrderPositions = GetMovePositionArray(positionOfAttack, units.Count);
        int currentMoveIndex = 0;
        foreach (UnitParent unit in units)
        {
            if (IsUnitIdle(unit))
            {
                unit.GetAIComponent().SetNewAttackMoveOrder(newOrderPositions[currentMoveIndex]);
                currentMoveIndex++;
            }
        }
    }

    //Gives a list of item codes for items that aren't fully supplied or being currently built
    public List<int> GetItemsRequiringSupply()
    {
        List<int> itemsNeeded = new();
        for (int i = 0; i < wantedUnits.Length; i++)
        {
            if ((unitsSupplied[i] + unitsInReserve[i]) < wantedUnits[i]) { itemsNeeded.Add(i); }
        }
        return itemsNeeded;
    }
    public bool IsItemRequiringSupply(int item)
    {
        return ((unitsSupplied[item] + unitsInReserve[item]) < wantedUnits[item]);
    }
    //List Manipulation functions
    public void AddUnit(UnitParent unit)
    {
        units.Add(unit);
		switch (unit.GetFactionComponent().unitInfo.unitName) 
		{
			case "Scout":
				unitsSupplied[0]++;
                unitsInReserve[0]--;
                GD.Print("Wanted/Supplied/Reserve[0] - " + wantedUnits[0] + "/" + unitsSupplied[0] + "/" + unitsInReserve[0]);
                break;
            case "Tank":
                unitsSupplied[1]++;
                unitsInReserve[1]--;
                GD.Print("Wanted/Supplied/Reserve[1] - " + wantedUnits[1] + "/" + unitsSupplied[1] + "/" + unitsInReserve[1]);
                break;
            case "Sniper":
                unitsSupplied[2]++;
                unitsInReserve[2]--;
                GD.Print("Wanted/Supplied/Reserve[2] - " + wantedUnits[2] + "/" + unitsSupplied[2] + "/" + unitsInReserve[2]);
                break;
			default: GD.Print("WARNING: AI Controller does not recognise this unit, check AIControlGroup.AddUnit"); break;
        }

	}
	public void RemoveUnit(UnitParent unit)
    {
        units.Remove(unit);
		//Remove the unit from the units supplied, in case it was killed before they actually were assembled to attack
        switch (unit.GetFactionComponent().unitInfo.unitName)
        {
            case "Unit1":
                unitsSupplied[0]--; break;
            case "Unit2":
                unitsSupplied[1]--; break;
            case "Unit3":
                unitsSupplied[2]--; break;
            default: break;
        }
    }
}

public struct UnitsInProgress
{
    public BuildingParent factory { get; private set; }
    public int itemCode { get; private set; }
    public AIControlGroup controlGroup { get; private set; }

    public UnitsInProgress(BuildingParent factory, int itemCode, AIControlGroup controlGroup)
    {
        this.factory = factory;
        this.itemCode = itemCode;
        this.controlGroup = controlGroup;
    }
}


public partial class MainAIController : Node2D
{
	[Export] public int AIFaction = 2;

    //A reference to the level controller. Used to track other units in the field for attack waves to attack after their attack path
    MainLevelController levelController;

	//All the units and buildings that this team can control
	public List<BuildingParent> buildingsInTeam = new();
	public List<UnitParent> unitsInTeam = new();
    public List<BuildingParent> buildingFactoriesInTeam = new(); //A shortlist of buildings that contain a factory component
	public List<AIControlGroup> attackGroups = new();
	public List<AIControlGroup> defenseGroups = new();
    public List<Vector2> attackRallyPoints = new();
    public List<Vector2> defenseRallyPoints = new();
    //Units that may have been built but weren't be able to be assigned to a team
    public List<UnitsInProgress> unitsInProgress = new();
    
    protected float AITickTimer = 0;
	protected float AISleepTimer = 0;
    bool isAwakened = false, AIEnabled = true;

    protected float PostAwakeningAITimer = 0;
	[Export] public float SleepTimeDuration = 180;

    //References to the construct info that will be used alongside the "item codes" to find factories that build them
    [Export] public ConstructInfo[] constructUnitInfoSet;

    //Utility: Use GD.Randi to get a random int between min (inclusive) and max (non-inclusive)
    //e.g., min 0 and 10 max will give a value between 0 and 9
    public int RandIntRange(int min, int max)
    {
        return (int)(GD.Randi() % max + min);
    }

    //Adds units to this AI controllers list for use.
    //Special code strings may be added to set the result in a particular way (e.g. setting a set as part of a defensive group that should be replenished)
    public virtual void AddUnitsInArea(Rect2 rectArea, string specialCode)
	{
		PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
        
        //Set up the shape and position of the rectangle area
        Vector2 rectangleCenter = rectArea.Position + (rectArea.Size / 2);
        RectangleShape2D shape = new();
		shape.Size = rectArea.Size;

        //Set the casting shape
        PhysicsShapeQueryParameters2D areaCast = new();
        areaCast.CollideWithAreas = true; //set collide with areas to true, so it will register building areas
        areaCast.Shape = shape;
        areaCast.Transform = new Transform2D(0, rectangleCenter);
        areaCast.CollisionMask = GetAlliedCollisionMask(); //The collision layers for buildings and unit physics

        //Run the cast
        List<FactionComponent> selectionHits = new();
        var collisionResult = spaceState.IntersectShape(areaCast, 100);
        if (collisionResult.Count > 0)
		{
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent newComponent = (FactionComponent)collidedObject;
                    selectionHits.Add(newComponent);
                }
            }
        }

		//Organise all hits into units and buildings
		foreach (FactionComponent hit in selectionHits)
		{
			if (hit.GetParent().IsInGroup("Unit"))
			{
				UnitParent newUnit = hit.GetParent<UnitParent>();
				AddUnit(newUnit);
			}
			else if (hit.GetParent().IsInGroup("Building"))
            {
				BuildingParent newBuilding = hit.GetParent<BuildingParent>();
				AddBuilding(newBuilding);
            }
        }

        //Special string code functionality can be added afterwards in the level-specific controller scripts
    }

    public override void _Ready()
    {
        base._Ready();

        levelController = (MainLevelController)GetTree().CurrentScene;
        //InitUnitConstructInfoSet();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
        //Make sure the AI processing is only happening while the game is not paused
        if (!GetTree().Paused)
        {
            AITickTimer += (float)delta;
            AISleepTimer += (float)delta;

            if (AISleepTimer > SleepTimeDuration) { isAwakened = true; }

            if (isAwakened && AIEnabled)
            {
                PostAwakeningAITimer += (float)delta;
                if (AITickTimer > 0.8f)
                {
                    ProcessAITick();
                    AITickTimer = 0;
                }
            }
            
        }
		
	}
    public void ResetSleepTimer()
    {
        AISleepTimer = 0;
        isAwakened = false;
    }

	//Runs the actions that the AI wants to take
	//This will be set in separate child scripts
	public virtual void ProcessAITick()
	{
		//Run general AI for attackers
		foreach (AIControlGroup attackers in attackGroups)
		{
            attackers.ProcessAITick();
		}
		//Run general AI for defenders
		foreach (AIControlGroup defenders in defenseGroups)
		{
            defenders.ProcessAITick();
		}

        //Check current control groups to see if there are needs for defenders, then for not fully assembled attackers
        //If there are, then search for a factory that can build this unit
        //If an assembled attack group is wiped out, mark it for removal
        foreach (AIControlGroup defenders in defenseGroups)
        {
            CheckAndProcessSupply(defenders);
        }
        List<AIControlGroup> attackGroupsToRemove = new();
        foreach (AIControlGroup attackers in attackGroups)
        {
            CheckAndProcessSupply(attackers);
            if (attackers.groupAssembled && attackers.units.Count <= 0)
            {
                attackGroupsToRemove.Add(attackers);
            }
        }
        //removed the previously marked wiped groups
        foreach (AIControlGroup wipedAttackers in attackGroupsToRemove)
        {
            attackGroups.Remove(wipedAttackers);
        }

        //If there is nothing needing to be built, we can prepare another attack wave
        //Limit to 5 attack waves to prevent overloading the game
        if (AreAllGroupsSupplied() && attackGroups.Count < 5)
        {
            GenerateNewAttackGroup();
        }

    }

    public void EnableAIController()
    {
        isAwakened = true;
    }
    public void DisableAIController()
    {
        isAwakened = false;
    }

    public void CheckAndProcessSupply(AIControlGroup controlGroup)
    {
        if (controlGroup.groupAssembled) { return; }
        //Create a subset of items that we 'want' to build
        List<int> unitCodesNeeded = controlGroup.GetItemsRequiringSupply();

        if (unitCodesNeeded.Count > 0)
        {
            //Choose from the subset randomly to select it to build
            int itemChoice = RandIntRange(0, unitCodesNeeded.Count);
            int itemToBuild = unitCodesNeeded[itemChoice];
            //Assign that item code to a construct info
            ConstructInfo unitToConstruct = constructUnitInfoSet[itemToBuild];
            //Find a factory that can build that item and add it to its build queue
            foreach (BuildingParent building in buildingFactoriesInTeam)
            {
                FactoryComponent component = building.GetFactionComponent().GetFactoryComponent();
                if (component.buildableUnits.Contains(unitToConstruct) && component.GetBuildQueue().Count == 0)
                {
                    //Setting a rally point for the unit when it's built (if applicable)
                    building.GetFactionComponent().SetRallyPoint(building.GlobalPosition + new Vector2(0, -150));
                    if (controlGroup.isAttackGroup)
                    {
                        if (attackRallyPoints.Count > 0)
                            building.GetFactionComponent().SetRallyPoint(attackRallyPoints[RandIntRange(0, attackRallyPoints.Count)]);
                    }
                    else
                    {
                        if (defenseRallyPoints.Count > 0)
                            building.GetFactionComponent().SetRallyPoint(defenseRallyPoints[RandIntRange(0, defenseRallyPoints.Count)]);
                    }

                    component.AddToBuildQueue(unitToConstruct);
                    UnitsInProgress newReserve = new UnitsInProgress(building, itemToBuild, controlGroup);
                    unitsInProgress.Add(newReserve);
                    controlGroup.unitsInReserve[itemToBuild]++;
                    //after this unit was added, don't add any more to this control group if it will be filled
                    if (!controlGroup.IsItemRequiringSupply(itemToBuild)) { break; }
                }
            }
        }
    }
    public bool AreAllGroupsSupplied()
    {
        foreach (AIControlGroup attackers in attackGroups)
        {
            if (attackers.GetItemsRequiringSupply().Count > 0) { return false; }
        }
        //Run general AI for defenders
        foreach (AIControlGroup defenders in defenseGroups)
        {
            if (defenders.GetItemsRequiringSupply().Count > 0) { return false; }
        }
        return true;
    }


	public virtual void GenerateNewAttackGroup()
	{
		//This will be set up on individual AI controllers
	}

	public virtual void GenerateNewDefenseGroup()
	{
        //This will be set up on individual AI controllers
    }

    public void SearchForFactory(ConstructInfo unitToBuild)
    {

    }
    public UnitsInProgress FindUnitInProgressByCode(int itemCode)
    {
        foreach (UnitsInProgress uip  in unitsInProgress)
        {
            if (uip.itemCode == itemCode) {  return uip; }
        }

        return new UnitsInProgress(null,-1, null);
    }
    public UnitsInProgress FindUnitInProgressByFactory(BuildingParent factory)
    {
        foreach (UnitsInProgress uip in unitsInProgress)
        {
            if (uip.factory == factory) { return uip; }
        }

        return new UnitsInProgress(null, -1, null);
    }

    //List Manipulation functions
    public void AddUnit(UnitParent unit)
	{
		unitsInTeam.Add(unit);

        //If this unit is being built for a control group, assign it to that group and remove it from the units in progress
        UnitsInProgress uip = FindUnitInProgressByFactory((BuildingParent)unit.motherFactory);
        if (uip.itemCode != -1) //-1 means it's not part of a factory build for a control group
        {
            uip.controlGroup.AddUnit(unit);
            unitsInProgress.Remove(uip);
        }
	}
	public void RemoveUnit(UnitParent unit)
	{
		unitsInTeam.Remove(unit);

        //Remove this unit from whichever control group it is a part of, if applicable
        foreach (AIControlGroup attackers in attackGroups)
        {
            if (attackers.units.Contains(unit)) 
            {
                attackers.RemoveUnit(unit);
                return;
            }
        }
        foreach (AIControlGroup defenders in defenseGroups)
        {
            if (defenders.units.Contains(unit))
            {
                defenders.RemoveUnit(unit);
                return;
            }
        }
    }
	public void AddBuilding(BuildingParent building)
	{
		buildingsInTeam.Add(building);
        //Add it to a factory shortlist if it has the component
        FactoryComponent component = building.GetFactionComponent().GetFactoryComponent();
        if (IsInstanceValid(component))
        {
            buildingFactoriesInTeam.Add(building);
        }

    }
	public void RemoveBuilding(BuildingParent building)
	{
		buildingsInTeam.Remove(building);
        if (buildingFactoriesInTeam.Contains(building)) { buildingFactoriesInTeam.Remove(building); }

        //Check if this building was currently building anything for a control group
        //If it was, remove that item from the reserves so it can be used on a different factory later
        UnitsInProgress uip = FindUnitInProgressByFactory(building);
        
        if (uip.itemCode != -1)
        {
            uip.controlGroup.unitsInReserve[uip.itemCode]--;
            unitsInProgress.Remove(uip);
        }
    }

    //Functions to quickly access the collision masks for allied or enemy units. e.g. Faction 1 would mean layer 5 for allied and layer 6-7 for enemy
    public uint GetAlliedCollisionMask()
    {
        switch (AIFaction)
        {
            case 1: return 16;
            case 2: return 32;
            case 3: return 64;
        }

        GD.Print("Error with AI controller faction check");
        return 0;
    }
    public uint GetEnemyCollisionMask()
    {
        switch (AIFaction)
        {
            case 1: return 96;
            case 2: return 80;
            case 3: return 48;
        }
        GD.Print("Error with AI controller faction check");
        return 0;
    }

    //Function to find the nearest enemy from the supplied position
    public Vector2 FindNearestEnemy(Vector2 position)
    {
        //Set up the nearest enemy by category (-1 = it has not assigned one
        float nearestNetworkHub = -1, nearestBuilding = -1, nearestCommander = -1, nearestUnit = -1;
        Vector2 nearestHubPosition = Vector2.Zero, nearestBuildingPosition = Vector2.Zero, nearestCommanderPosition = Vector2.Zero, nearestUnitPosition = Vector2.Zero;

        foreach (BuildingParent building in levelController.buildingsInScene)
        {
            if (building.GetFactionComponent().faction != AIFaction)
            {
                float distanceToTarget = position.DistanceTo(building.GlobalPosition);
                if (building.GetFactionComponent().unitInfo.unitName == "Network Pylon")
                {
                    if ((distanceToTarget < nearestNetworkHub) || (nearestNetworkHub == -1))
                    {
                        nearestNetworkHub = distanceToTarget;
                        nearestHubPosition = building.GlobalPosition;
                    }
                }
                else
                {
                    if ((distanceToTarget < nearestBuilding) || (nearestBuilding == -1))
                    {
                        nearestBuilding = distanceToTarget;
                        nearestBuildingPosition = building.GlobalPosition;
                    }
                }
            }
        }
        //If a building was found, mark that as the position now
        if (nearestNetworkHub != -1) { return nearestHubPosition; }
        if (nearestBuilding != -1) { return nearestBuildingPosition; }

        //No buildings found, find units
        foreach (UnitParent unit in  levelController.unitsInScene)
        {
            if (unit.GetFactionComponent().faction != AIFaction)
            {
                float distanceToTarget = position.DistanceTo(unit.GlobalPosition);
                if (unit.GetFactionComponent().unitInfo.unitName == "Commander")
                {
                    if ((distanceToTarget < nearestCommander) || (nearestCommander == -1))
                    {
                        nearestCommander = distanceToTarget;
                        nearestCommanderPosition = unit.GlobalPosition;
                    }
                }
                else
                {
                    if ((distanceToTarget < nearestUnit) || (nearestUnit == -1))
                    {
                        nearestUnit = distanceToTarget;
                        nearestUnitPosition = unit.GlobalPosition;
                    }
                }
            }
        }

        //Specific check on the player unit if it exists
        if (IsInstanceValid(levelController.player))
        {
            float distanceToTarget = position.DistanceTo(levelController.player.GlobalPosition);
            if ((distanceToTarget < nearestCommander) || (nearestCommander == -1))
            {
                nearestCommander = distanceToTarget;
                nearestCommanderPosition = levelController.player.GlobalPosition;
            }
        }

        if (nearestCommander != -1) { return nearestCommanderPosition; }
        if (nearestUnit != -1) { return nearestUnitPosition; }

        //default value, no enemies found
        return position;
    }
}

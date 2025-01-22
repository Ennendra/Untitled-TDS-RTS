using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Security;
using System.Reflection;

//Used for UI purposes to tell us how many units of each type are selected
public struct RTSSelectionType
{
    public UnitInfo unitInfo;
    public int amount;

    public RTSSelectionType(UnitInfo unitInfo, int amount)
    {
        this.unitInfo = unitInfo;
        this.amount = amount;
    }
}

public struct ControlGroup
{
    //The list of units in the control group
    public List<FactionComponent> units;
    //The info of the unit to be displayed on the UI
    public UnitInfo frontUnit;

    public ControlGroup(List<FactionComponent> units, UnitInfo frontUnit)
    {
        this.units = units;
        this.frontUnit = frontUnit;
    }
}

public partial class RTSController : Node2D
{
    public MainLevelController levelController;

    

    public Camera2D camera { get; protected set; }
    float cameraSpeed = 1200; //speed is pixels per second

    public int playerFaction = 1;
    Vector2 initialSelectPoint = Vector2.Zero;
    Vector2 endSelectPoint = Vector2.Zero;
    public bool isCurrentlySelecting = false;

    //Timers to track how long it has been since the last click selection, or control group press
    float selectTimer = 1, controlGroupTimer = 1;
    float doubleClickTime = 0.2f, controlGroupDoubleTime = 0.25f;
    int lastControlGroupSelected = 0;

    public List<FactionComponent> selectedItems { get; protected set; } = new();

    //Control group lists
    public ControlGroup[] controlGroups { get; protected set; } = new ControlGroup[10];

    public bool factoryBuildMenuEnabled { get; protected set; } = false;

    //The toolbar, so we can update the selected unit info etc whenever our selection changes. This is set in the main level controller.
    UI_RTSToolbar toolbar;

    //order index: 
    //0 - Move
    //1 - attack
    //2 - Guard
    [Export] Texture2D[] orderConfirmationTextures;
    [Export] PackedScene orderConfirmationScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        InitControlGroups();

        camera = GetNode<Camera2D>("Camera2D");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (IsLevelControllerSet())
        {
            //Set camera movement based on mouse on viewport boundaries and hotkey pressing
            //GlobalPosition += GetCameraMovement() * (float)delta * cameraSpeed;
            camera.Position += GetCameraMovement() * (float)delta * cameraSpeed;

            //Clamp the camera position to the edges of the map
            ClampCameraToLimitPositions();

            selectTimer += (float)delta;
            controlGroupTimer += (float)delta;
        }

        QueueRedraw();
	}

    public override void _Draw()
    {
        base._Draw();
        //drawing a selection rectangle
        if (isCurrentlySelecting )
        {
            endSelectPoint = GetGlobalMousePosition();

            Rect2 rectangle = new();
            //rectangle.Position = new Vector2(Mathf.Min(initialGlobalSelectPoint.X, endGlobalSelectPoint.X), Mathf.Min(initialGlobalSelectPoint.Y, endGlobalSelectPoint.Y));;
            rectangle.Position = initialSelectPoint;
            rectangle.End = endSelectPoint;
            //fill
            DrawRect(rectangle, new Color(0.4f, 0.4f, 0.4f, 0.5f));
            //outline
            DrawRect(rectangle, new Color(1,1,1, 0.5f),false,2);
        }

    }

    public string ScanCursorPosition(Vector2 scanPosition)
    {
        //check what's under the cursor to see whether we run an attack move command or a direct attack command
        var spaceState = GetWorld2D().DirectSpaceState;
        PhysicsPointQueryParameters2D pointCheck = new();
        pointCheck.CollideWithAreas = true;
        pointCheck.CollisionMask = GetEnemyCollisionMask() + GetAlliedCollisionMask(); //Check collisions for ALL combatants below the cursor
        pointCheck.Position = scanPosition;

        var collisionResult = spaceState.IntersectPoint(pointCheck);
        if (collisionResult.Count > 0)
        {
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent targetComponent = (FactionComponent)collidedObject;
                    if (targetComponent.faction == playerFaction)
                    {
                        //Target is ally
                        return "Guard";
                    }
                    else
                    {
                        //Target is enemy, as long as it's spotted
                        if (targetComponent.spottedByFaction[playerFaction])
                            { return "Attack"; }
                    }
                }
            }
        }
        return "Move";
    }

    public bool IsLevelControllerSet()
    {
        if (IsInstanceValid(levelController))
        {
            if (levelController.playState == LevelControllerPlayState.RTSCOMMAND)
            {
                return true;
            }
        }
        return false;
    }

    //Functions on selecting units
    public void SetInitialSelectionPoint()
    {
        //Set the initial selection box point
        initialSelectPoint = GetGlobalMousePosition();
        isCurrentlySelecting = true;
    }
    public List<RTSSelectionType> ExecuteSelection(bool isAdditive)
    {
        //Clear selected units if not holding the shift key
        if (!isAdditive) ClearUnitSelection();

        endSelectPoint = GetGlobalMousePosition();
        isCurrentlySelecting = false;
        var spaceState = GetWorld2D().DirectSpaceState;

        //Get the rectangle shape by getting the center point between the beginning and end and the size by using the difference between the two points
        endSelectPoint = GetGlobalMousePosition();
        Vector2 rectangleCenter, rectangleSize;
        Vector2 rectangleDiff = endSelectPoint - initialSelectPoint;


        rectangleCenter = initialSelectPoint + (rectangleDiff / 2);
        rectangleSize = new Vector2(Mathf.Abs(rectangleDiff.X),Mathf.Abs(rectangleDiff.Y));
        RectangleShape2D selectionShape = new();
        selectionShape.Size = rectangleSize;   

        

        //Set the casting shape
        PhysicsShapeQueryParameters2D areaCast = new();
        areaCast.CollideWithAreas = true; //set collide with areas to true, so it will register building areas
        areaCast.Shape = selectionShape;
        areaCast.Transform = new Transform2D(0, rectangleCenter);
        areaCast.CollisionMask = GetAlliedCollisionMask(); //The collision layers for buildings and unit physics


        List<FactionComponent> selectionHits = new();
        //Run the collision check
        var collisionResult = spaceState.IntersectShape(areaCast, 100);
        if (collisionResult.Count > 0)
        {
            //Generate a temp list which we will then filter out if units are selected
            
            bool isAUnitSelected = false;
            //Cast each hit to a faction component and add to the list
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent newComponent = (FactionComponent)collidedObject;
                    if (!newComponent.isPlayer)
                    {
                        selectionHits.Add(newComponent);
                        if (newComponent.isAUnit)
                            isAUnitSelected = true;
                    }
                }
            }

            if (isAUnitSelected)
            {
                List<FactionComponent> filteredSelection = new();
                foreach (FactionComponent selectComp in selectionHits)
                {
                    if (selectComp.isAUnit) filteredSelection.Add(selectComp);
                }
                AddUnitSetToSelection(filteredSelection);
            }
            else
            {
                AddUnitSetToSelection(selectionHits);
            }
        }
        //If this is a double-click, select all units grabbed by the click and select all of that type on the screen (Will only take the first
        if (selectTimer < doubleClickTime && selectionHits.Count>0)
        {
            List<FactionComponent> screenSelectionHits = new();
            Rect2 viewportRect = GetViewportRect();
            selectionShape.Size = viewportRect.Size;
            areaCast.Transform = new Transform2D(0, camera.Position);
            areaCast.Shape = selectionShape;

            collisionResult = spaceState.IntersectShape(areaCast, 100);
            if (collisionResult.Count > 0)
            {
                //Cast each hit to a faction component and add to the list
                foreach (var collision in collisionResult)
                {
                    Variant collidedObject;
                    bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                    if (colliderCheck)
                    {
                        FactionComponent newComponent = (FactionComponent)collidedObject;
                        if (newComponent.unitInfo == selectionHits[0].unitInfo)
                        {
                            screenSelectionHits.Add(newComponent);
                        }
                    }
                }
                AddUnitSetToSelection(screenSelectionHits);
            }
        }
        selectTimer = 0;


        //Now that all selection is done, split the items into their unit types
        isCurrentlySelecting = false;
        List<RTSSelectionType> types = GetSelectedUnitTypes();

        return types;
    }
    public List<RTSSelectionType> GetSelectedUnitTypes()
    {
        List<RTSSelectionType> selectionTypes = new();
        List<UnitInfo> unitTypeInSelection = new();
        foreach(FactionComponent unit in selectedItems)
        {
            UnitInfo uInfo = unit.unitInfo;
            GD.Print(uInfo.unitName);
            if (unitTypeInSelection.Contains(uInfo)) 
            {
                //Increment the 'amount' of the unit type selected by 1
                int sTypeIndex = selectionTypes.FindIndex(t => t.unitInfo == uInfo);
                RTSSelectionType type = selectionTypes[sTypeIndex];
                type.amount += 1;
                selectionTypes[sTypeIndex] = type;
            }
            else
            {
                //Generate new selection type
                RTSSelectionType newType = new(uInfo,1);
                selectionTypes.Add(newType);
                unitTypeInSelection.Add(uInfo);
            }
        }

        //Check if there is only one type and that it is a factory type. If so, send info to the level controller as we will be changing the UI
        if (selectionTypes.Count == 1)
        {
            if (selectedItems[0].GetFactoryComponent() != null)
            {
                levelController.SetFactoryButtonInfo(GetFactoryBuildableList());
                factoryBuildMenuEnabled = true;
            }
            else
            {
                levelController.ResetUIToBuildingButtons();
                factoryBuildMenuEnabled = false;
            }
        }
        else
        {
            levelController.ResetUIToBuildingButtons();
            factoryBuildMenuEnabled = false;
        }

        return selectionTypes;
    }

    //Functions on executing orders
    //Execute Standard - Right-clicking with selected units. Move based on what is under enemy
    //Nothing = move, enemy = attack, ally = guard
    public void ExecuteStandardOrder(Vector2 orderPosition)
    {
        //check what's under the cursor to determine what order is given
        var spaceState = GetWorld2D().DirectSpaceState;
        PhysicsPointQueryParameters2D pointCheck = new();
        pointCheck.CollideWithAreas = true;
        pointCheck.CollisionMask = GetEnemyCollisionMask() + GetAlliedCollisionMask(); //Check collisions for ALL combatants below the cursor
        pointCheck.Position = orderPosition;

        var collisionResult = spaceState.IntersectPoint(pointCheck);
        if (collisionResult.Count > 0)
        {
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent targetComponent = (FactionComponent)collidedObject;

                    if (targetComponent.faction == playerFaction)
                    {
                        //Target is ally
                        AIExecuteOrder_Guard(targetComponent);
                    }
                    else
                    {
                        //Target is enemy
                        AIExecuteOrder_Attack(targetComponent);
                    }

                }
            }
        }
        else //nothing under the position, it shall be a move order
        {
            AIExecuteOrder_Move(orderPosition);
        }
    }
    //Execute Attack - Sets an attack on the target, or if the target is not an enemy, execute as an attack-move
    public void ExecuteAttackOrder(Vector2 orderPosition)
    {
        //check what's under the cursor to see whether we run an attack move command or a direct attack command
        var spaceState = GetWorld2D().DirectSpaceState;
        PhysicsPointQueryParameters2D pointCheck = new();
        pointCheck.CollideWithAreas = true;
        pointCheck.CollisionMask = GetEnemyCollisionMask(); //Check collisions for ALL combatants below the cursor
        pointCheck.Position = orderPosition;

        var collisionResult = spaceState.IntersectPoint(pointCheck);
        if (collisionResult.Count > 0)
        {
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent targetComponent = (FactionComponent)collidedObject;
                    AIExecuteOrder_Attack(targetComponent);
                    return;
                }
            }
        }

        AIExecuteOrder_AttackMove(orderPosition);
    }
    //Execute as a hard move order, regardless of what's under the cursor
    public void ExecuteMoveOrder(Vector2 orderPosition)
    {
        
        AIExecuteOrder_Move(orderPosition);
    }
    public void ExecuteCancelOrder()
    {
        AIExecuteOrder_Cancel();
    }
    public void ExecuteHoldOrder()
    {
        AIExecuteOrder_Hold();
    }

    //Setting the orderstate
    //Creates a UI object to show the order placed
    public void CreateMoveConfirmationUI(Vector2 orderPosition, Texture2D orderTexture)
    {
        UIOrderConfirmation orderConfirm = (UIOrderConfirmation)orderConfirmationScene.Instantiate();
        GetTree().CurrentScene.AddChild(orderConfirm);
        orderConfirm.GlobalPosition = orderPosition;
        orderConfirm.Texture = orderTexture;
    }

    //Function related for move and attackmove orders
    //Spaces the movement locations for multiple units
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
                CreateMoveConfirmationUI(firstPosition + newPositionOffset, orderConfirmationTextures[0]);

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

    //Functions that relay the order to the AI components themselves
    public void AIExecuteOrder_Move(Vector2 orderPosition)
    {
        List<FactionComponent> movingUnits = new();

        //Set building rally points and filter all moving units
        foreach (FactionComponent component in selectedItems)
        {
            if (component.GetAIComponent() != null)
            {
                //component.GetAIComponent().SetNewMoveOrder(orderPosition);
                movingUnits.Add(component);
            }
            else if (component.CanSetRallyPoint)
            {
                component.SetRallyPoint(orderPosition);
            }
        }

        //Get the positions for all the moving units to go to
        List<Vector2> newOrderPositions = GetMovePositionArray(orderPosition,movingUnits.Count);
        
        for (int i = 0; i < movingUnits.Count; i++)
        {
            movingUnits[i].GetAIComponent().SetNewMoveOrder(newOrderPositions[i]);
        }

        CreateMoveConfirmationUI(orderPosition, orderConfirmationTextures[0]);
    }
    public void AIExecuteOrder_Attack(FactionComponent target)
    {
        foreach (FactionComponent component in selectedItems)
        {
            if (component.GetAIComponent() != null)
            {
                component.GetAIComponent().SetNewAttackOrder(target);
            }
        }

        CreateMoveConfirmationUI(target.GlobalPosition, orderConfirmationTextures[1]);
    }
    public void AIExecuteOrder_Guard(FactionComponent target)
    {
        foreach (FactionComponent component in selectedItems)
        {
            if (component.GetAIComponent() != null)
            {
                component.GetAIComponent().SetNewGuardOrder(target);
            }
        }

        CreateMoveConfirmationUI(target.GlobalPosition, orderConfirmationTextures[2]);
    }
    public void AIExecuteOrder_AttackMove(Vector2 orderPosition)
    {
        List<FactionComponent> movingUnits = new();

        foreach (FactionComponent component in selectedItems)
        {
            if (component.GetAIComponent() != null)
            {
                movingUnits.Add(component);
                //component.GetAIComponent().SetNewAttackMoveOrder(orderPosition);
            }
        }

        //Get the positions for all the moving units to go to
        List<Vector2> newOrderPositions = GetMovePositionArray(orderPosition, movingUnits.Count);

        for (int i = 0; i < movingUnits.Count; i++)
        {
            movingUnits[i].GetAIComponent().SetNewAttackMoveOrder(newOrderPositions[i]);
        }

        CreateMoveConfirmationUI(orderPosition, orderConfirmationTextures[1]);
    }
    public void AIExecuteOrder_Hold()
    {
        foreach (FactionComponent component in selectedItems)
        {
            if (component.GetAIComponent() != null)
            {
                component.GetAIComponent().SetNewHoldOrder();
            }
        }
    }
    public void AIExecuteOrder_Cancel()
    {
        foreach (FactionComponent component in selectedItems)
        {
            if (component.GetAIComponent() != null)
            {
                component.GetAIComponent().CancelOrder();
            }
        }
    }

    //Functions linked to factory build queue editing (going from button UI to MainLevelController to here)
    public void AddFactoryItemToQueue(ConstructInfo buildInfo)
    {
        foreach (FactionComponent component in selectedItems)
        {
            if (IsInstanceValid(component.GetFactoryComponent()))
            {
                component.GetFactoryComponent().AddToBuildQueue(buildInfo);
            }
        }
    }
    public void RemoveFactoryItemFromQueue(ConstructInfo buildInfo)
    {
        foreach (FactionComponent component in selectedItems)
        {
            if (IsInstanceValid(component.GetFactoryComponent()))
            {
                component.GetFactoryComponent().RemoveFromBuildQueue(buildInfo);
            }
        }
    }
    public ConstructInfo[] GetFactoryBuildableList() 
    {
        return selectedItems[0].GetFactoryComponent().buildableUnits;
    }
    public List<ConstructInfo> GetSelectedFactoryQueue()
    {
        List<ConstructInfo> totalQueue = new();
        foreach (FactionComponent component in selectedItems)
        {
            totalQueue.AddRange(component.GetFactoryComponent().GetBuildQueue());
        }
        return totalQueue;
    }

    //Camera functions
    public void SetCameraPosition(Vector2 position)
    {
        camera.Position = position;

        //Clamp the camera position to the edges of the map
        ClampCameraToLimitPositions();
    }
    public Vector2 GetCameraMovement()
    {
        Vector2 movementVector = Vector2.Zero;

        //Get vector of RTSmovement inputs (not normalising the vector like Input.GetVector does)
        if (Input.IsActionPressed("RTS_MoveLeft"))   movementVector += new Vector2(-1, 0);
        if (Input.IsActionPressed("RTS_MoveRight"))  movementVector += new Vector2(1, 0);
        if (Input.IsActionPressed("RTS_MoveUp"))     movementVector += new Vector2(0, -1);
        if (Input.IsActionPressed("RTS_MoveDown"))   movementVector += new Vector2(0, 1);

        //Add movement if the mouse is near the edge of the screen
        Rect2 viewportBoundaries = GetViewportRect();
        Vector2 mousePos = GetViewport().GetMousePosition();
        if (mousePos.X <= viewportBoundaries.Position.X + 5)
            movementVector += new Vector2(-1, 0);
        if (mousePos.Y <= viewportBoundaries.Position.Y + 5)
            movementVector += new Vector2(0, -1);
        if (mousePos.X >= viewportBoundaries.End.X - 5)
            movementVector += new Vector2(1, 0);
        if (mousePos.Y >= viewportBoundaries.End.Y - 5)
            movementVector += new Vector2(0, 1);

        

        //movementVector = movementVector.Clamp(new Vector2(-1, -1), new Vector2(1, 1));
        //Clamp the vector to a min/max of -1/1 for X and Y (normal vector2.clamp gives weird results)
        float moveX = movementVector.X;
        float moveY = movementVector.Y;
        moveX = Mathf.Clamp(moveX, -1, 1);
        moveY = Mathf.Clamp(moveY, -1, 1);
        movementVector = new Vector2(moveX, moveY);

        return movementVector;
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
    public void ClampCameraToLimitPositions()
    {
        Vector2 cameraFinalPosition = camera.Position;

        Rect2 viewport = GetViewportRect();
        //Get the map edges (top, bottom, left, right)
        float[] mapEdges = levelController.GetMapSides();
        if (camera.Position.Y < mapEdges[0] + (viewport.Size.Y / 2)) { cameraFinalPosition.Y = mapEdges[0] + (viewport.Size.Y / 2); }
        if (camera.Position.Y > mapEdges[1] - (viewport.Size.Y / 2)) { cameraFinalPosition.Y = mapEdges[1] - (viewport.Size.Y / 2); }
        if (camera.Position.X < mapEdges[2] + (viewport.Size.X / 2)) { cameraFinalPosition.X = mapEdges[2] + (viewport.Size.X / 2); }
        if (camera.Position.X > mapEdges[3] - (viewport.Size.X / 2)) { cameraFinalPosition.X = mapEdges[3] - (viewport.Size.X / 2); }

        camera.Position = cameraFinalPosition;
    }

    //Functions for manipulating unit selection list
    public void AddUnitToSelection(FactionComponent unit)
    {
        if (!unit.GetSelectionStatus())
        {
            selectedItems.Add(unit);
            unit.SetSelectionStatus(true);
        }
    }
    public void AddUnitSetToSelection(List<FactionComponent> units)
    {
        foreach (FactionComponent unit in units)
        {
            if (!unit.GetSelectionStatus())
            {
                selectedItems.Add(unit);
                unit.SetSelectionStatus(true);
            }
        }  
    }
    public void RemoveUnitToSelection(FactionComponent unit)
    {
        selectedItems.Remove(unit);
        unit.SetSelectionStatus(false);
    }
    public void ClearUnitSelection()
    {
        foreach (FactionComponent unit in selectedItems)
            unit.SetSelectionStatus(false);
        selectedItems.Clear();
    }
    public List<RTSSelectionType> SelectUnitOfType(UnitInfo unitType)
    {
        List<FactionComponent> narrowedList = new();
        foreach (FactionComponent unit in selectedItems)
        {
            if (unit.unitInfo == unitType)
            {
                narrowedList.Add(unit);
            }
        }
        ClearUnitSelection();
        AddUnitSetToSelection(narrowedList);
        return GetSelectedUnitTypes();
    }
    public List<RTSSelectionType> DeselectUnitOfType(UnitInfo unitType)
    {
        List<FactionComponent> narrowedList = new();
        foreach (FactionComponent unit in selectedItems)
        {
            if (unit.unitInfo != unitType)
            {
                narrowedList.Add(unit);
            }
        }
        ClearUnitSelection();
        AddUnitSetToSelection(narrowedList);
        return GetSelectedUnitTypes();
    }
    public List<RTSSelectionType> RemoveDeadUnitFromSelection(FactionComponent unit)
    {
        if (selectedItems.Contains(unit))
        {
            selectedItems.Remove(unit);
        }
        return GetSelectedUnitTypes();
    }


    //Control Group Functions
    public void InitControlGroups()
    {
        for (int i = 0; i <controlGroups.Length; i++)
        {
            controlGroups[i].units = new();
        }
    }
    public void SetControlGroup(List<FactionComponent> unitGroup, int index)
    {
        if (unitGroup.Count > 0)
        {
            controlGroups[index].units.Clear();
            controlGroups[index].units.AddRange(unitGroup);
            controlGroups[index].frontUnit = controlGroups[index].units[0].unitInfo;
        }
    }
    public void AddToControlGroup(List<FactionComponent> unitGroup, int index)
    {
        if (unitGroup.Count > 0)
        {
            List<FactionComponent> narrowedList = new();
            foreach (FactionComponent unit in unitGroup)
            {
                if (!controlGroups[index].units.Contains(unit))
                {
                    narrowedList.Add(unit);
                }
            }
            controlGroups[index].units.AddRange(narrowedList);
            controlGroups[index].frontUnit = controlGroups[index].units[0].unitInfo;
        }
    }
    public bool RemoveFromControlGroupOnDeath(FactionComponent unitToRemove) //This will be called whenever a unit dies, to remove it from any control group it might be in
    {
        bool unitRemoved = false;
        for (int i = 0; i < controlGroups.Length; i++)
        {
            if (controlGroups[i].units.Contains(unitToRemove))
            {
                controlGroups[i].units.Remove(unitToRemove);
                unitRemoved = true;

                if (controlGroups[i].units.Count == 0)
                    controlGroups[i].frontUnit = null;
                else
                    controlGroups[i].frontUnit = controlGroups[i].units[0].unitInfo;
            }
        }
        
        return unitRemoved;
    }
    public void ClearControlGroup(int index)
    {
        controlGroups[index].units.Clear();
        controlGroups[index].frontUnit = null;
    }
    public List<RTSSelectionType> SelectControlGroup(int index, bool isAdditive)
    {
        if (!isAdditive) { ClearUnitSelection(); }

        if (controlGroups[index].units.Count > 0 && controlGroupTimer < controlGroupDoubleTime && lastControlGroupSelected == index)
        {
            camera.Position = controlGroups[index].units[0].GlobalPosition;
        }

        controlGroupTimer = 0;
        lastControlGroupSelected = index;
        AddUnitSetToSelection(controlGroups[index].units);
        return GetSelectedUnitTypes();
    }
    public ControlGroup[] GetControlGroups()
    {
        return controlGroups;
    }

    public void SetFaction(int index)
    {
        playerFaction = index;
    }
    //Functions to quickly access the collision masks for allied or enemy units. e.g. Faction 1 would mean layer 5 for allied and layer 6-7 for enemy
    public uint GetAlliedCollisionMask()
    {
        switch (playerFaction) 
        {
            case 1: return 16;
            case 2: return 32;
            case 3: return 64;
        }

        GD.Print("Error with RTS controller faction check");
        return 0;
    }
    public uint GetEnemyCollisionMask()
    {
        switch (playerFaction)
        {
            case 1: return 96;
            case 2: return 80;
            case 3: return 48;
        }
        GD.Print("Error with RTS controller faction check");
        return 0;
    }

}

using Godot;
using System;

public partial class UI_BuildPlacement : Sprite2D
{
    //Variables related to building
    BuildingQueue currentBuildingInfo;
    Vector2 buildLocation = new Vector2(-1, -1);
    Vector2 gridOffset = new Vector2(0, 0);
    BaseNetworkController buildingPlacementNetwork;
    bool isBuildPlacementValid = false;

    Label buildGhostLabel;
    Sprite2D gridLines;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        buildGhostLabel = GetNode<Label>("GhostLabel");
        gridLines = GetNode<Sprite2D>("GridLines");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

    public void ProcessBuildingPlacement(FactionController factionLink, Vector2 buildLocationCheck, FactionComponent builder)
    {
        string buildPlacementStatus = CheckBuildingPlacement(factionLink, buildLocationCheck, builder);

        SetBuildGhostPosition(buildLocation, new Vector2(0, 25 + ((currentBuildingInfo.building.objectGridSize.Y - 1) * 25)));
        SetBuildGhostText(buildPlacementStatus);

        if (isBuildPlacementValid)
        {
            Modulate = new Color(0.5f, 1, 0.5f, 1);
        }
        else
        {
            Modulate = new Color(1, 0.5f, 0.5f, 1);
        }
    }

    public string CheckBuildingPlacement(FactionController factionLink, Vector2 buildLocationCheck, FactionComponent builder)
    {
        isBuildPlacementValid = false;
        //Snap mouse position to 50x50 grid
        buildLocation = (buildLocationCheck / 50).Floor() * 50;
        //Checks a collision rectangle based on the radius for buildings and compiles them
        var spaceState = GetWorld2D().DirectSpaceState;

        //Deprecated section -- Used in old building and used 10% of cost when placing
        ////Check that we have enough resources on hand to place the building
        //float[] costRequired = new float[2] { currentBuildingInfo.building.unitInfo.energyCost / 10, currentBuildingInfo.building.unitInfo.metalCost / 10 };
        //float[] storageOnHand;
        //storageOnHand = factionLink.GetCurrentStorage();
        //if (storageOnHand[0] < costRequired[0] && storageOnHand[1] < costRequired[1])
        //    return "More Energy and Metal required!";
        //else if (storageOnHand[0] < costRequired[0])
        //    return "More Energy required!";
        //else if (storageOnHand[1] < costRequired[1])
        //    return "More Metal required!";

        //Create the point where the building ghost will be placed
        gridOffset = Vector2.Zero;
        RectangleShape2D ghostCheckShape = new();
        ghostCheckShape.Size = new Vector2(currentBuildingInfo.building.objectGridSize.X, currentBuildingInfo.building.objectGridSize.Y) * 50;
        if (currentBuildingInfo.building.objectGridSize.X % 2 > 0) { buildLocation.X += 25; gridOffset.X = -25; }
        if (currentBuildingInfo.building.objectGridSize.Y % 2 > 0) { buildLocation.Y += 25; gridOffset.Y = -25; }

        //Check that we are close enough to the player to place the building
        if (buildLocation.DistanceTo(builder.GlobalPosition) > 500)
        {
            return "Build location not within proximity!";
        }

        //Check that there is nothing blocking the placement of the building (environment, units, other buildings etc)
        PhysicsShapeQueryParameters2D areaCast = new();
        areaCast.CollideWithAreas = true; //set collide with areas to true, so it will register building areas
        areaCast.Shape = ghostCheckShape;
        areaCast.Transform = new Transform2D(0, buildLocation);
        areaCast.CollisionMask = 1572879; //The collision layers for buildings and unit physics

        var collisionResult = spaceState.IntersectShape(areaCast);
        if (collisionResult.Count > 0)
        {
            return "There are objects in the way!";
        }

        //Check that the building, if not a network pylon, is within a network, and that it isn't a hostile network
        if (currentBuildingInfo.building.type != ConstructType.NETWORK)
        {
            //change the areacasts mask to the network area layer
            areaCast.CollisionMask = 16384;

            collisionResult = spaceState.IntersectShape(areaCast);
            if (collisionResult.Count <= 0)
            {
                return "Must be built within a network!";
            }
            else
            {
                foreach (var collision in collisionResult)
                {
                    Variant collidedObject;
                    bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                    if (colliderCheck)
                    {
                        BaseNetworkController currentControllerPoint = (BaseNetworkController)collidedObject;
                        if (currentControllerPoint.GetNetworkFaction() != factionLink.GetFaction()) //Check that this is one of our networks
                        {
                            return "Cannot place in a hostile network!";
                        }
                    }
                }
            }
        }

        //Last check for miners, make sure they are on top of a mining node
        if (currentBuildingInfo.building.type == ConstructType.MINER)
        {
            PhysicsPointQueryParameters2D pointCheck = new();
            pointCheck.CollideWithAreas = true;
            pointCheck.CollisionMask = 262144;
            pointCheck.Position = buildLocation;

            collisionResult = spaceState.IntersectPoint(pointCheck);
            if (collisionResult.Count <= 0)
            {
                return "Must build on a mining node";
            }
        }

        isBuildPlacementValid = true;
        return "Ready to place!";
    }
    public bool PlaceBuilding(FactionController factionLink)
    {
        //TODO - Redo this to place the building itself and send info back to the controller so it removes the building from the build queue
        if (isBuildPlacementValid)
        {
            //float[] costRequired = new float[2] { currentBuildingInfo.building.unitInfo.energyCost / 10, currentBuildingInfo.building.unitInfo.metalCost / 10 };

            //factionLink.RemoveFromStorage(0, costRequired[0]);
            //factionLink.RemoveFromStorage(1, costRequired[1]);

            //place the blueprint
            //BlueprintParent newBuilding = (BlueprintParent)currentBuildingInfo.building.objectToSpawn.Instantiate();
            //GetTree().CurrentScene.AddChild(newBuilding);
            //newBuilding.GlobalPosition = buildLocation;
            //newBuilding.SetNewFaction(factionLink.GetFaction());

            BuildingParent newBuilding = (BuildingParent)currentBuildingInfo.building.objectToSpawn.Instantiate();
            GetTree().CurrentScene.AddChild(newBuilding);
            newBuilding.GlobalPosition = buildLocation;
            newBuilding.SetNewFaction(factionLink.GetFaction());

            return true;
        }
        return false;
    }
    public void SetNewBuildInfo(BuildingQueue buildInfo)
    {
        currentBuildingInfo = buildInfo;
        Texture = buildInfo.building.unitInfo.iconTex;
    }

    public void SetBuildGhostTexture(Texture2D texture)
    {
        Texture = texture;
    }
    public void SetBuildGhostPosition(Vector2 position, Vector2 textOffset)
    {
        Rotation = 0;
        GlobalPosition = position;
        gridLines.Position = gridOffset;
        
        buildGhostLabel.Position = new Vector2((-buildGhostLabel.Size.X / 2), textOffset.Y);
    }
    public void SetBuildGhostText(string text)
    {
        buildGhostLabel.Text = text;
    }
}

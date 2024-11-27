using Godot;
using System;
using System.Collections.Generic;

public partial class FOWSightComponent : Node2D
{
	//The range (as a radius in px) of the unit's vision
	[Export] int sightRange = 500;
	//The faction component, so the controller can check whether it's an allied unit
	[Export] FactionComponent factionComponent;
	[Export] Texture2D visionTexture;
	public Rect2I sightRect { get; protected set; }
	public Image visionImage { get; protected set; }
	public Vector2 offset { get; protected set; }

	//Tells us whether the elements here are 
	public bool componentRescaled = false;

	//Rescales the rect to match the compute scale in 
	public void InitSightComponent(float computeScale)
	{
        visionImage = visionTexture.GetImage();

        sightRect = new Rect2I(Vector2I.Zero, (int)(sightRange * 2 * computeScale), (int)(sightRange * 2 * computeScale));
		visionImage.Resize((int)(sightRange * 2 * computeScale), (int)(sightRange * 2 * computeScale));
		offset = new Vector2(sightRange, sightRange);
	}

    public void ScanForTarget()
    {
        //Init the space state
        var spaceState = GetWorld2D().DirectSpaceState;
        //Create the collision shape for the scan
        CircleShape2D sightCheckShape = new();
        sightCheckShape.Radius = sightRange + 5;

        //Create the cast query
        PhysicsShapeQueryParameters2D areaCast = new();
        areaCast.CollideWithAreas = true;
        areaCast.Shape = sightCheckShape;
        areaCast.Transform = new Transform2D(0, this.GlobalPosition);
        //Get the collision layers for the factions that this one is *not* a part of
        //Faction1 = 16 --- Faction2 = 32 --- Faction3 = 64
        switch (factionComponent.faction)
        {
            case 1:
                areaCast.CollisionMask = 96;
                break;
            case 2:
                areaCast.CollisionMask = 80;
                break;
            case 3:
                areaCast.CollisionMask = 48;
                break;
        }

        //Execute the check
        var collisionResult = spaceState.IntersectShape(areaCast, 100);

        List<FactionComponent> componentCollisions = new();
        if (collisionResult.Count > 0)
        {
            //Generate a list of all potential targets in range
            foreach (var collision in collisionResult)
            {
                Variant collidedObject;
                bool colliderCheck = collision.TryGetValue("collider", out collidedObject);

                if (colliderCheck)
                {
                    FactionComponent potentialTarget = (FactionComponent)collidedObject;
                    componentCollisions.Add(potentialTarget);
                }
            }
        }

        //Set each of the targets as 'spotted'
        foreach (FactionComponent spottedUnit in componentCollisions)
        {
            spottedUnit.SetAsSpotted(factionComponent.faction);
        }
    }

    //A simpler spotting scan for mining nodes. Will simply use a distance check
    public void ScanMiningNodes()
    {
        var nodeGroup = GetTree().GetNodesInGroup("MetalNode");
        
        foreach (var metalNode in nodeGroup)
        {
            MetalNode nodeCast = metalNode as MetalNode;
            if (!nodeCast.marker.spottedByFaction[factionComponent.faction]) 
            {
                if (GlobalPosition.DistanceTo(nodeCast.GlobalPosition)<sightRange+20)
                {
                    nodeCast.marker.spottedByFaction[factionComponent.faction] = true;
                }
            }
        }
    }

    public int GetSightFaction()
	{
		return factionComponent.faction;
	}

}

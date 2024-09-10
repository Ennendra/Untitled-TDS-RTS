using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public struct MinimapMarkerDrawInfo
{
	public Vector2 markerPosition = new Vector2(0,0);
	public MinimapMarkerTag tag = MinimapMarkerTag.PLAYER;

	public MinimapMarkerDrawInfo(Vector2 markerPosition, MinimapMarkerTag tag)
	{
		this.markerPosition = markerPosition;
		this.tag = tag;
	}
}

public partial class UI_Minimap : Control
{
	//The object that the minimap is centered to, if in local mode (either the player or the RTS camera)
	//This is also used as the node when determining how to draw the viewport rect on the minimap
	Node2D minimapLocalCenterNode;
	//The width and height of the full map, and the center point of the map
	Vector2 minimapFullmapScanSize, minimapFullmapCenter;
    //A reference to the map edges, which is relayed from the levelcontroller. Used to clamp the minimap drawing bounds at the edges
    float topBound = -5000, leftBound = -5000, bottomBound = 5000, rightBound = 5000;
	//The width/height ratio is the full map (1,1 = even square)
	Vector2 mapSizeRatio = new Vector2(1, 1);
    //The rect of the viewport relative to the minimap
    Rect2 minimapViewport;
	//Variables determining the zoom level (-1,-1 = full map, with other values being width and height)
	int currentZoomLevel = 0;
	Vector2[] zoomLevels = new Vector2[] { new Vector2(-1, -1), new Vector2(4000, 4000), new Vector2(2500, 2500)};

	//The size of the minimap itself (in case we need to change the UI size later)
	//The initial draw size and offset can be edited to accomodate the minimap should I wish to scale it, while the others are based on the map ratio
	[Export] Vector2 initialMinimapDrawSize, initialMinimapDrawOffset;
    Vector2 minimapDrawSize, minimapDrawOffset;
    //The textures used for the minimap markers
    [Export] Texture2D markerPlayer, markerAllyUnit, markerAllyBuilding, markerEnemyUnit, markerEnemyBuilding;
    //The list of minimapmarkers in the scene that are ready to draw
    List<MinimapMarkerDrawInfo> markersToDraw = new();

    public override void _Draw()
    {
        base._Draw();

		if (IsMinimapFullMap())
		{
            Color lineColor = new Color(0.7f, 0.7f, 0.7f, 1);
            //Draw the boundaries of the minimap if the ratio is under 1 
            if (mapSizeRatio.X < 1)
            {
                //Draw vertical lines on the edges
                DrawLine(minimapDrawOffset, new Vector2(minimapDrawOffset.X, minimapDrawOffset.Y + minimapDrawSize.Y), lineColor);
                DrawLine(minimapDrawOffset + new Vector2(minimapDrawSize.X, 0), new Vector2(minimapDrawOffset.X + minimapDrawSize.X, minimapDrawOffset.Y + minimapDrawSize.Y), lineColor);
            }
            if (mapSizeRatio.Y < 1)
            {
                //Draw horizontal lines on the edges
                DrawLine(minimapDrawOffset, new Vector2(minimapDrawOffset.X + minimapDrawSize.X, minimapDrawOffset.Y), lineColor);
                DrawLine(minimapDrawOffset + new Vector2(0, minimapDrawSize.Y), new Vector2(minimapDrawOffset.X + minimapDrawSize.X, minimapDrawOffset.Y + minimapDrawSize.Y), lineColor);
            }
        }

        //Draw Each Marker
        foreach (var marker in markersToDraw)
		{
			Texture2D markerTexture = markerPlayer;
			switch (marker.tag)
			{
				case MinimapMarkerTag.PLAYER: markerTexture = markerPlayer; break;
				case MinimapMarkerTag.ALLYUNIT: markerTexture = markerAllyUnit; break;
                case MinimapMarkerTag.ALLYBUILDING: markerTexture = markerAllyBuilding; break;
                case MinimapMarkerTag.ENEMYUNIT: markerTexture = markerEnemyUnit; break;
                case MinimapMarkerTag.ENEMYBUILDING: markerTexture = markerEnemyBuilding; break;
				default: GD.Print("Missing or invalid tag for minimap marker?"); break;
            }
            Vector2 textureOffset = markerTexture.GetSize() / 2;
            DrawTexture(markerTexture, marker.markerPosition - textureOffset);
		}

		//Draw a rectangle on the minimap to represent the viewport size
		DrawRect(minimapViewport, new Color(1, 1, 0, 0.5f), false);
	}

	//Setting the node for the local minimap center
	public void SetLocalCenterNode(Node2D node)
	{
		minimapLocalCenterNode = node;
	}
	//Changing the parameters for the full map zone
	public void SetFullMapZone(float top, float bottom, float left, float right)
	{
        topBound = top;
        bottomBound = bottom;
        leftBound = left;
        rightBound = right;

        //Set the map scan zone and it's center
        Vector2 mapSize = new Vector2(right-left, bottom-top);
		minimapFullmapScanSize = mapSize;
		minimapFullmapCenter = new Vector2(right - (mapSize.X / 2), bottom - (mapSize.Y / 2));

		//Determine the map ratio
		if (mapSize.X >= mapSize.Y)
		{ 
			mapSizeRatio = mapSize / mapSize.X;
		}
		else
		{ 
			mapSizeRatio = mapSize / mapSize.Y; 
		}
        minimapDrawSize = initialMinimapDrawSize * mapSizeRatio;
        minimapDrawOffset = initialMinimapDrawOffset + ((initialMinimapDrawSize - minimapDrawSize) / 2);
    }
	//Changing zoom levels
	public void IncreaseZoomLevel()
	{
		if (currentZoomLevel < zoomLevels.Length - 1) { currentZoomLevel += 1; }
	}
	public void DecreaseZoomLevel()
	{
        if (currentZoomLevel > 0) { currentZoomLevel -= 1; }
    }
	//Checking whether we are at the full-map zoom level
    public bool IsMinimapFullMap()
    {
		if (currentZoomLevel == 0) return true;
        return false;
    }
	public bool IsMinimapFullZoom()
	{
		if (currentZoomLevel == zoomLevels.Length - 1) return true;
		return false;

    }

    //Functions to check whether a position is within a certain area (ie, a marker is within a scan range)
    public bool IsWithinScanAreaRectangle(Vector2 markerPosition, Vector2 scanCenterPosition, Vector2 scanSize)
	{
		if (markerPosition.X >= scanCenterPosition.X - (scanSize.X / 2)
			&& markerPosition.X <= scanCenterPosition.X + (scanSize.X / 2)
            &&markerPosition.Y >= scanCenterPosition.Y - (scanSize.Y / 2)
            && markerPosition.Y <= scanCenterPosition.Y + (scanSize.Y / 2))
			return true;

			return false; ;
	}

	//Functions related to minimap input
	public Vector2 CheckForMinimapInput(Vector2 mousePosition)
	{
            //Calculate where the mouse is on the minimap and return the position on the game-world
			//Any relative position changes are further calculated on what executes this function

            Vector2 minimapRelativeFromCenter = (mousePosition - (Position + minimapDrawOffset) - (minimapDrawSize/2)) / (minimapDrawSize / 2);
            
			//multiply the result wither by the local scan size or the full map, depending on what mode the minimap is in
			if (!IsMinimapFullMap())
			{
				//Local scan mode
				minimapRelativeFromCenter = minimapRelativeFromCenter * (zoomLevels[currentZoomLevel] / 2);
			}
            else
            {
				//Fullmap scan mode
                minimapRelativeFromCenter = minimapRelativeFromCenter * (minimapFullmapScanSize / 2);
            }
			//Perhaps have it return a struct that has the vector2, whether we are using full map or not, and whether the input is valid at all
			return minimapRelativeFromCenter;
    }
	public bool IsInputWithinMinimapBounds(Vector2 mousePosition)
	{
		//Checking that the mouse position when pressed is within the minimap bounds
		if (IsMinimapFullMap())
		{
			//Full map mode
            if (mousePosition.X >= Position.X + minimapDrawOffset.X
            && mousePosition.X <= Position.X + minimapDrawOffset.X + minimapDrawSize.X
            && mousePosition.Y >= Position.Y + minimapDrawOffset.Y
            && mousePosition.Y <= Position.Y + minimapDrawOffset.Y + minimapDrawSize.Y)
                return true;
        }
        else
        {
			//Local scan mode
            if (mousePosition.X >= Position.X + initialMinimapDrawOffset.X
            && mousePosition.X <= Position.X + initialMinimapDrawOffset.X + initialMinimapDrawSize.X
            && mousePosition.Y >= Position.Y + initialMinimapDrawOffset.Y
            && mousePosition.Y <= Position.Y + initialMinimapDrawOffset.Y + initialMinimapDrawSize.Y)
                return true;
        }

        return false;
	}


	
    public void ProcessMinimapTick(List<MinimapMarkerComponent> markers)
	{
		//clear the current set of drawn markers
		markersToDraw.Clear();

		//Set some temp variables to choose between the initial values or the ratio-affected values, depending on whether the zoom level is full map or not
		Vector2 tickDrawSize, tickDrawOffset;
		//Set the minimap region based on whether it's local scan mode for full map scan mode
		Vector2 drawScale, drawCenter, scanRegion;
		if (!IsMinimapFullMap())
		{
            tickDrawSize = initialMinimapDrawSize;
            tickDrawOffset = initialMinimapDrawOffset;

            scanRegion = zoomLevels[currentZoomLevel];
			drawCenter = minimapLocalCenterNode.GlobalPosition;
			drawScale = tickDrawSize / zoomLevels[currentZoomLevel];

			
        }
		else
		{
            tickDrawSize = minimapDrawSize;
            tickDrawOffset = minimapDrawOffset;

            scanRegion = minimapFullmapScanSize;
			drawCenter = minimapFullmapCenter;
			drawScale = tickDrawSize / minimapFullmapScanSize;
		}

        //Create the rectangle boundaries of the viewport relative to the minimap
        Rect2 viewport = GetViewportRect();
		minimapViewport = new Rect2();
		minimapViewport.Size = viewport.Size * drawScale;
		minimapViewport.Position = ((minimapLocalCenterNode.GlobalPosition - (viewport.Size/2)) - drawCenter) * drawScale + ((tickDrawSize / 2) + tickDrawOffset);

		//Clamp the minimap viewport to the edges of the screen if they go beyond (e.g. player unit reaching edge of map)
		//Clamp the drawcenter as well
		drawCenter = new Vector2(
			Mathf.Clamp(drawCenter.X, leftBound + (viewport.Size.X / 2), rightBound - (viewport.Size.X / 2)),
            Mathf.Clamp(drawCenter.Y, topBound + (viewport.Size.Y / 2), bottomBound - (viewport.Size.Y / 2))
			);
        minimapViewport.Position = new Vector2(
            Mathf.Clamp(minimapViewport.Position.X, tickDrawOffset.X, tickDrawSize.X - (minimapViewport.Size.X - tickDrawOffset.X)),
            Mathf.Clamp(minimapViewport.Position.Y, tickDrawOffset.Y, tickDrawSize.Y - (minimapViewport.Size.Y - tickDrawOffset.Y))
            );
		
        //Set each marker to be drawn
        foreach (MinimapMarkerComponent marker in markers)
		{
			//Check whether the marker is within the region to be shown on the minimap
			if (IsWithinScanAreaRectangle(marker.GlobalPosition, drawCenter, scanRegion))
			{
				//Set the position on the minimap (relative to the center of the minimap) that this marker will draw
				Vector2 drawPosition = (marker.GlobalPosition - drawCenter) * drawScale + (minimapDrawSize / 2) + minimapDrawOffset;

				//Add this marker to the list of markers to be drawn
				MinimapMarkerDrawInfo newMarkerInfo = new(drawPosition, marker.markerTag);
				markersToDraw.Add(newMarkerInfo);
			}
        }
		QueueRedraw();
    }
}

using Godot;
using System;

public partial class BuildQueueButton : Button
{
    //The node that these buttons will emit signals to (ie. the mainlevelcontroller)
    Node2D inputConnection;
    //what build queue index this will be linked to
    int buildIndex;
    //The texture showing the icon of the building in queue
    TextureRect buttonTexture;
    ProgressBar buildProgressBar;

    BuildingQueue buildInfo;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        buttonTexture = GetNode<TextureRect>("ButtonTex");
        buildProgressBar = GetNode<ProgressBar>("BuildProgressBar");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (buildInfo != null)
        {
            buttonTexture.Texture = buildInfo.building.unitInfo.iconTex;
            buildProgressBar.Value = buildInfo.totalSupplied;

            //Alter the color to green if finished, or magenta if not
            if (buildInfo.totalSupplied >= buildInfo.totalCost) 
                { buildProgressBar.Modulate = new Color(0,0.5f,0,0.5f); }
            else
                { buildProgressBar.Modulate = new Color(0.5f, 0, 0.3f, 0.5f); }
        }
        else
        {
            buttonTexture.Texture = null;
            buildProgressBar.Value = 0;
        }
	}

    public void SetButtonConnection(Node2D inputConnection, int index)
    {
        this.inputConnection = inputConnection;
        this.buildIndex = index;
    }

    public void OnButtonPressed()
    {
        if (inputConnection != null)
        {
            inputConnection.EmitSignal("GetBuildQueueItem", buildIndex);
        }
    }

    public void SetBuildInfo(BuildingQueue info)
    {
        if (info != null)
        {
            buildInfo = info;
            buildProgressBar.Value = buildInfo.totalSupplied;
            buildProgressBar.MaxValue = buildInfo.totalCost;
        }
        else
        {
            buildInfo = null;
            buildProgressBar.Value = 0;
        }
    }
}

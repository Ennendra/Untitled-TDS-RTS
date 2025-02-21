using Godot;
using System;



public partial class BuildButton : Button
{
	[Export] ConstructInfo buildInfo;
	TextureRect buttonTexture;
	bool isTechAvailable = true;


    //The node that these buttons will emit signals to (ie. the main level controller)
    Node2D inputConnection;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		buttonTexture = GetNode<TextureRect>("ButtonTex");
		buttonTexture.Texture = buildInfo.unitInfo.iconTex;
	}

	public void SetButtonConstructInfo(ConstructInfo info)
	{
		buildInfo = info;
		buttonTexture.Texture = buildInfo.unitInfo.iconTex;
	}

	public void SetButtonConnection(Node2D inputConnection)
	{
		this.inputConnection = inputConnection;
	}

	public bool GetTechAvailability()
	{
		return isTechAvailable;
	}


	public void OnButtonPressed()
	{
		if (inputConnection != null)
		{
			inputConnection.EmitSignal("GetNewBuildInfo", buildInfo);
		}
	}
}

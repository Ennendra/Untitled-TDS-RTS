using Godot;
using System;



public partial class FactoryBuildButton : Button
{
    [Export] ConstructInfo buildInfo;
    TextureRect buttonTexture;
    Label labelAmount;


    //The node that these buttons will emit signals to (ie. the main level controller)
    Node2D inputConnection;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        buttonTexture = GetNode<TextureRect>("ButtonTex");
        labelAmount = GetNode<Label>("LabelAmount");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        
    }

    public ConstructInfo GetButtonConstructInfo()
    {
        return buildInfo;
    }
    public void SetButtonConstructInfo(ConstructInfo info)
    {
        buildInfo = info;
        buttonTexture.Texture = buildInfo.unitInfo.iconTex;
    }
    public void SetButtonConstructAmount(int amount)
    {
        if (amount > 0) { labelAmount.Text = amount.ToString(); }
        else { labelAmount.Text = string.Empty; }
    }

    //This connection will be set to the RTS Controller
    public void SetButtonConnection(Node2D inputConnection)
    {
        this.inputConnection = inputConnection;
    }

    //public void OnButtonPressed()
    //{
    //    if (inputConnection != null)
    //    {
    //        inputConnection.EmitSignal("NewFactoryBuild", buildInfo, 1);
    //    }
    //}

    public void OnButtonPressed(InputEvent @event)
    {

        if (inputConnection != null)
        {
            if (@event is InputEventMouseButton mb)
            {
                int buildAmount = 1;
                if (Input.IsKeyPressed(Key.Shift)) { buildAmount = 5; }

                if (mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                {
                    inputConnection.EmitSignal("NewFactoryBuild", buildInfo, buildAmount);
                }
                else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed)
                {
                    inputConnection.EmitSignal("CancelFactoryBuild", buildInfo, buildAmount);
                }
            }
            //inputConnection.EmitSignal("GetBuildQueueItem", buildIndex);
        }
    }
}

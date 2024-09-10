using Godot;
using System;

//A code to tell us what this button does.
//Will send the relevant signal to the RTSController accordingly
public enum OrderButtonCode
{
    MOVE,
    ATTACK,
    STOP,
    HOLD
}

public partial class OrderButton : Button
{
    [Export] OrderButtonCode buttonCode;

    //The connection to an RTS controller. Will send signals to this when pressed
    Node2D inputConnection;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void SetButtonConnection(Node2D inputConnection)
    {
        this.inputConnection = inputConnection;
    }

    public void OnButtonPressed()
	{
        if (inputConnection != null)
        {
            //convert the code to a string (as enums cannot be used in a signal)
            string sButtonCode = "None";
            switch (buttonCode) 
            {
                case OrderButtonCode.MOVE: sButtonCode = "Move"; break;
                case OrderButtonCode.ATTACK: sButtonCode = "Attack"; break;
                case OrderButtonCode.STOP: sButtonCode = "Stop"; break;
                case OrderButtonCode.HOLD: sButtonCode = "Hold"; break;
            }


            inputConnection.EmitSignal("SetOrderState", sButtonCode);
        }
        else GD.Print("Selection button has no connection to RTS controller!");
    }
}

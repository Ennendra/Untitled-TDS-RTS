using Godot;
using System;

public partial class UI_SelectedUnitInfo : Button
{
	TextureRect buttonIcon;
	Label amountLabel;

	UnitInfo selectedUnitInfo;
	int selectedAmount;

	//The connection to an RTS controller. Will send signals to this when pressed
	Node2D inputConnection;

    public override void _Ready()
    {
        base._Ready();
		buttonIcon = GetNode<TextureRect>("UnitIcon");
		amountLabel = GetNode<Label>("UnitAmount");
    }

    public void UpdateUnitInfo(UnitInfo unitInfo, int amount)
	{
		if (unitInfo != null)
		{
			Visible = true;
            selectedUnitInfo = unitInfo;
            selectedAmount = amount;
			buttonIcon.Texture = unitInfo.iconTex;
			amountLabel.Text = amount.ToString();
        }
		else
		{
            Visible = false;
        }


	}

    public void SetButtonConnection(Node2D inputConnection)
    {
        this.inputConnection = inputConnection;
    }

    public void OnButtonPressed()
	{
		if (inputConnection != null)
		{
			if (Input.IsKeyPressed(Key.Shift))
				inputConnection.EmitSignal("DeselectThisUnit", selectedUnitInfo);
			else
				inputConnection.EmitSignal("SelectThisUnit", selectedUnitInfo);
		}
		else GD.Print("Selection button has no connection to RTS controller!");
    }
}

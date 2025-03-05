using Godot;
using System;

public partial class Scenario1Prep : CanvasLayer
{
	Globals globals;

	[Export] HSlider enemy1Timer, enemy2Timer, enemy3Timer, enemy4Timer;
	[Export] Label enemy1Label, enemy2Label, enemy3Label, enemy4Label;
    [Export] CheckBox cboxDisableAI, cBoxDisableFOW;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        //Get the globals node for static use
        globals = GetNode<Globals>("/root/Globals");

        enemy1Timer.Value = globals.scenario1_enemy1AwakenTimer;
        enemy2Timer.Value = globals.scenario1_enemy2AwakenTimer;
        enemy3Timer.Value = globals.scenario1_enemy3AwakenTimer;
        enemy4Timer.Value = globals.scenario1_enemy4AwakenTimer;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    //Button presses for difficulty
    public void OnDifficultyButtonPressedComfy()
    {
        //cboxDisableAI.ToggleMode = true;
        cboxDisableAI.ButtonPressed = true;
    }
    public void OnDifficultyButtonPressedNormal()
    {
        cboxDisableAI.ButtonPressed = false;
        enemy1Timer.Value = 240;
        enemy2Timer.Value = 360;
        enemy3Timer.Value = 600;
        enemy4Timer.Value = 900;
    }
    public void OnDifficultyButtonPressedHard()
    {
        cboxDisableAI.ButtonPressed = false;
        enemy1Timer.Value = 120;
        enemy2Timer.Value = 300;
        enemy3Timer.Value = 420;
        enemy4Timer.Value = 660;
    }
    public void OnDifficultyButtonPressedBrutal()
    {
        cboxDisableAI.ButtonPressed = false;
        enemy1Timer.Value = 60;
        enemy2Timer.Value = 180;
        enemy3Timer.Value = 300;
        enemy4Timer.Value = 500;
    }


    //Slider values for enemy awakening timers
    public void OnEnemy1TimerValueChanged(float value)
    {
        enemy1Label.Text = value.ToString();
        globals.scenario1_enemy1AwakenTimer = value;
        GD.Print("Changing enemy 1 timer on globals");
    }
    public void OnEnemy2TimerValueChanged(float value)
    {
        enemy2Label.Text = value.ToString();
        globals.scenario1_enemy2AwakenTimer = value;
        GD.Print("Changing enemy 2 timer on globals");
    }
    public void OnEnemy3TimerValueChanged(float value)
    {
        enemy3Label.Text = value.ToString();
        globals.scenario1_enemy3AwakenTimer = value;
        GD.Print("Changing enemy 3 timer on globals");
    }
    public void OnEnemy4TimerValueChanged(float value)
    {
        enemy4Label.Text = value.ToString();
        globals.scenario1_enemy4AwakenTimer = value;
        GD.Print("Changing enemy 4 timer on globals");
    }

    //Checkboxes for AI and FOW disabling
    public void OnEnemyAIDisableToggle(bool toggle)
    {
        globals.scenario1_AIDisabled = toggle;

        //Disable the AI timer sliders if we are disabling the AI
        enemy1Timer.Editable = !toggle;
        enemy1Timer.Scrollable = !toggle;
        enemy2Timer.Editable = !toggle;
        enemy2Timer.Scrollable = !toggle;
        enemy3Timer.Editable = !toggle;
        enemy3Timer.Scrollable = !toggle;
        enemy4Timer.Editable = !toggle;
        enemy4Timer.Scrollable = !toggle;

        GD.Print("AI Disabled: "+toggle);
    }
    public void OnFOWDisableToggle(bool toggle)
    {
        globals.scenario1_FOWDisabled = toggle;
        GD.Print("FOW Disabled: " + toggle);
    }
}

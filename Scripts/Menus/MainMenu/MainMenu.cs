using Godot;
using System;

enum MainMenuState 
{
	MAINMENU,
	SETTINGS,
	PLAYGAME_SCENARIO1
}


public partial class MainMenu : Node2D
{
	Globals globals;

	//The main state of the main menu, marking where in the navigation we are in
	MainMenuState menuState = MainMenuState.MAINMENU;

	//References to each canvas layer (to enable or disable depending on menu state)
	CanvasLayer menu_mainMenu, menu_settings, menu_scenario1Prep;

	//Scenes that the main menu can navigate to
	[Export] PackedScene scene_scenario1;
	[Export] PackedScene scene_tutorial;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        //Get the globals node for static use
        globals = GetNode<Globals>("/root/Globals");

		//Get the reference to all other menu elements
		menu_mainMenu = GetNode<CanvasLayer>("CL_MainMenu");
		menu_settings = GetNode<CanvasLayer>("CL_Settings");
		menu_scenario1Prep = GetNode<CanvasLayer>("CL_Scenario1Prep");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	//General element and state management functions
	public void HideAllElements()
	{
		menu_mainMenu.Hide();
		menu_settings.Hide();
		menu_scenario1Prep.Hide();

    }
	void SetNewMenuState(MainMenuState newState)
	{
		HideAllElements();
		menuState = newState;

		switch (newState)
		{
			case MainMenuState.MAINMENU:
				menu_mainMenu.Show();
				break;
			case MainMenuState.SETTINGS:
				menu_settings.Show();
				break;
			case MainMenuState.PLAYGAME_SCENARIO1:
				menu_scenario1Prep.Show();
				break;
		}

	}

	//Main Menu signals
	public void OnPlayGameButtonPressed()
	{
		SetNewMenuState(MainMenuState.PLAYGAME_SCENARIO1);
	}
	public void OnTutorialButtonPressed()
	{
		GetTree().ChangeSceneToPacked(scene_tutorial);
	}
	public void OnSettingsButtonPressed()
	{
		SetNewMenuState(MainMenuState.SETTINGS);
	}
	public void OnExitGameButtonPressed()
	{
        GetTree().Quit();
    }

	//Scenario1 prep signals
	//Note: Most of the code for that is set in it's own script. This only takes the buttons to navigate the menu or go into the scenario scene
	public void OnScenario1PlayPressed()
	{
        GetTree().ChangeSceneToPacked(scene_scenario1);
    }


	//Settings signals

	//Other misc signals
	public void OnBackButtonPressed()
	{
		if (menuState == MainMenuState.SETTINGS)
		{
            SetNewMenuState(MainMenuState.MAINMENU);
		}
        else if (menuState == MainMenuState.PLAYGAME_SCENARIO1)
        {
            SetNewMenuState(MainMenuState.MAINMENU);
        }
    }
}

using Godot;
using System;

public partial class UI_ResourceTracker : Control
{

    //Resource labels
    Label lblEnergy, lblEnergyVariance, lblMetal, lblMetalVariance, lblPerformance;
    TextureProgressBar texEnergy, texMetal;

    //numbers to track for the UI
    int energy, energyStorage, energyVariance;
    int metal, metalStorage, metalVariance;
    int resourcePerformance;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        lblEnergy = GetNode<Label>("LblEnergyCount");
        lblEnergyVariance = GetNode<Label>("LblEnergyVariance");
        lblMetal = GetNode<Label>("LblMetalCount");
        lblMetalVariance = GetNode<Label>("LblMetalVariance");
        lblPerformance = GetNode<Label>("LblResourcePerformance");

        texEnergy = GetNode<TextureProgressBar>("texEnergyPercent");
        texMetal = GetNode<TextureProgressBar>("texMetalPercent");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    //Functions on Top/Resource UI ---
    public void GetNewResourceValues(int[] newValues)
    {
        energy = newValues[0];
        energyStorage = newValues[1];
        energyVariance = newValues[2];
        metal = newValues[3];
        metalStorage = newValues[4];
        metalVariance = newValues[5];
        resourcePerformance = newValues[6];
        UpdateResourceLabels();
    }
    public void UpdateResourceLabels()
    {
        //Energy and metal resource texts
        lblEnergy.Text = energy + "/" + energyStorage;
        lblMetal.Text = metal + "/" + metalStorage;
        lblEnergyVariance.Text = Math.Abs(energyVariance).ToString();
        lblMetalVariance.Text = Math.Abs(metalVariance).ToString();

        //Energy and metal variance colorisation
        LabelSettings lblSettings;
        lblSettings = lblEnergyVariance.LabelSettings;
        if (energyVariance >= 0)
        { lblSettings.FontColor = new Color(0, 1, 0, 1); }
        else
        { lblSettings.FontColor = new Color(1, 0, 0, 1); }

        lblSettings = lblMetalVariance.LabelSettings;
        if (metalVariance >= 0)
        { lblSettings.FontColor = new Color(0, 1, 0, 1); }
        else
        { lblSettings.FontColor = new Color(1, 0, 0, 1); }

        //Resource Performance and colorisation ---
        lblSettings = lblPerformance.LabelSettings;
        lblPerformance.Text = resourcePerformance + "%";
        if (resourcePerformance >= 90)
            lblSettings.FontColor = new Color(0, 1, 0, 1); //Green
        else if (resourcePerformance >= 50)
            lblSettings.FontColor = new Color(1, 1, 0, 1); //Yellow
        else if (resourcePerformance >= 30)
            lblSettings.FontColor = new Color(1, 0.5f, 0, 1); //Orange
        else
            lblSettings.FontColor = new Color(1, 0, 0, 1); //Red

        //Resource progress bars
        texEnergy.MaxValue = energyStorage;
        texMetal.MaxValue = metalStorage;
        texEnergy.Value = energy;
        texMetal.Value = metal;
    }
    public void ClearResourceLabels()
    {
        lblEnergy.Text = "-/-";
        lblMetal.Text = "-/-";
        lblEnergyVariance.Text = "";
        lblMetalVariance.Text = "";
        texEnergy.Value = 0;
        texMetal.Value = 0;
    }
}

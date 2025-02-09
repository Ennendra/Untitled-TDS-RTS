using Godot;
using System;

public partial class UI_SingleUnitDetails : Control
{
	Label unitName, unitDescription, healthText, damageText;
	TextureRect unitIcon, damageIcon;
	ProgressBar healthBar;

    FactionComponent selectedUnit;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		unitName = GetNode<Label>("UnitName");
        unitDescription = GetNode<Label>("UnitDescription");
        healthText = GetNode<Label>("HealthText");
        damageText = GetNode<Label>("DamageText");

		unitIcon = GetNode<TextureRect>("UnitIcon");
        damageIcon = GetNode<TextureRect>("DamageIcon");
        healthBar = GetNode<ProgressBar>("HealthBar");

		
    }

	public void SetSelectedUnitInfo(FactionComponent selectedUnit)
	{
		this.selectedUnit = selectedUnit;
		//set relevant information from unitInfo
		unitName.Text = selectedUnit.unitInfo.unitName;
		unitDescription.Text = selectedUnit.unitInfo.unitDescription;
		unitIcon.Texture = selectedUnit.unitInfo.iconTex;

        damageText.Text = selectedUnit.unitInfo.damage;
        if (selectedUnit.unitInfo.damage != "") 
			{ damageIcon.Visible = true; } 
		else
			{ damageIcon.Visible = false; }

		SetUnitHealthInfo(selectedUnit);
	}

	public void SetUnitHealthInfo(FactionComponent selectedUnit)
	{
		int currentHealth = (int)Mathf.Floor(selectedUnit.GetDamageComponent().GetCurrentHealth());
		int currentMaxHealth = (int)Mathf.Floor(selectedUnit.unitInfo.maxHealth);
		healthText.Text = currentHealth.ToString() + " / " + currentMaxHealth.ToString();
		healthBar.MaxValue = currentMaxHealth;
		healthBar.Value = currentHealth;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (IsInstanceValid(selectedUnit))
        {
            SetUnitHealthInfo(selectedUnit);
        }
    }
}

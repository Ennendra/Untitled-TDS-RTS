using Godot;
using System;

//To mark what type of tool it is. Helps with dictating what to target.
//Build - Will target blueprints and supply with resources, using a constructor component.
//Repair - Targets Damage Components of the matching faction, using resources
//Mining - Targets metal nodes. Can help the player generate a small amount of extra metal
//Resource Augment - Does not target anything, but simply gives the player some form of resource augment (ie. adds an additional resourceComponent)

//Possibly add Reclamator, which targets wreckages and adds their resources to the player's resourceComponent
public enum ToolType
{
	BUILD,
	REPAIR,
	MINING,
	RESOURCEAUGMENT
}

public partial class ToolParent : Node2D
{
    [ExportCategory("UI Elements")]
    [Export] public Texture2D toolIcon { get; private set; }

    [ExportCategory("Components")]
    [Export] protected ConstructorComponent constructorComponent;
    [Export] protected ResourceComponent resourceComponent;
	[Export] protected FactionComponent factionComponent;
    [ExportCategory("Tool Traits")]
    [Export] public ToolType toolType { get; private set; }
	[Export] public  float toolRange { get; private set; }

    protected bool isActive;

	public ResourceComponent GetResourceComponent() { return resourceComponent; }
	public ConstructorComponent GetConstructorComponent() { return constructorComponent; }

	//Virtual functions for child classes
	public virtual void UseTool()
	{
		//Change code here for each respective tool
	}
	public virtual void ToggleToolActivation()
	{
		if (isActive)
			{ isActive = false; }
		else 
			{ isActive = true; }
	}
	public virtual void StopTool()
	{
		isActive = false;
	}

	//Gets the layer that the linked faction component is linked to
	public uint GetAlliedFactionLayer()
	{
		switch (factionComponent.faction)
		{
			case 1: return 16;
            case 2: return 32;
            case 3: return 64;
        }
		GD.Print("Tool error in factioncomponent check");
		return 0;
	}
	//Gets the layers that the linked faction component is NOT linked to
	public uint GetEnemyFactionLayer()
	{
        switch (factionComponent.faction)
        {
            case 1: return 96;
            case 2: return 80;
            case 3: return 48;
        }
        GD.Print("Tool error in factioncomponent check");
        return 0;
    }

	public bool IsToolConsuming()
	{
		if (isActive)
		{
			if (constructorComponent != null)
			{
				float[] constructorConsumption = constructorComponent.GetResourceComsumptionRate();

                if (constructorConsumption[0] + constructorConsumption[1] > 0)
				{
					return true;
				}
			}
            if (resourceComponent != null)
            {
				float resourceConsumption = resourceComponent.ConEnergy + resourceComponent.ConMetal;

                if (resourceConsumption > 0)
                {
                    return true;
                }
            }
        }

		return false;
	}
	public void SetToolPerformance(float value)
	{
		if (constructorComponent != null)
		{
			constructorComponent.SetNewPerformance(value);
		}
		if (resourceComponent != null)
		{
			resourceComponent.SetNewPerformance(value);
		}
	}
}

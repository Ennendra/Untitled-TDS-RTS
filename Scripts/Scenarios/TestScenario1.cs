using Godot;
using System;
using System.Linq;

public partial class TestScenario1 : MainLevelController
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

    }

    public override void InitAIUnitLists()
    {
        base.InitAIUnitLists();

        //unitAddArea = new Rect2(, , , );
        //aiControllers[].AddUnitsInArea(unitAddArea, "");

        RectangleShape2D unitadd;
        unitadd = new RectangleShape2D();


        //Initialise each AIController's units to control
        Rect2 unitAddArea;
        //AI1 - South base
        unitAddArea = new Rect2(2900, 4700, 2400, 1300);
        aiControllers[0].AddUnitsInArea(unitAddArea, "");
        unitAddArea = new Rect2(5350, 5250, 1300, 750);
        aiControllers[0].AddUnitsInArea(unitAddArea, "");

        //AI2 - Center
        unitAddArea = new Rect2(4300, 1750, 2250, 2100);
        aiControllers[1].AddUnitsInArea(unitAddArea, "");

        //AI3 - Northeast
        unitAddArea = new Rect2(7200, 0, 4800, 2100);
        aiControllers[2].AddUnitsInArea(unitAddArea, "");

        //AI4 - Southeast
        unitAddArea = new Rect2(7350, 3425, 4650, 2575);
        aiControllers[3].AddUnitsInArea(unitAddArea, "");

        GD.Print("AI1 - Buildings: " + aiControllers[0].buildingsInTeam.Count.ToString() + ", Units: " + aiControllers[0].unitsInTeam.Count.ToString());
        GD.Print("AI2 - Buildings: " + aiControllers[1].buildingsInTeam.Count.ToString() + ", Units: " + aiControllers[1].unitsInTeam.Count.ToString());
        GD.Print("AI3 - Buildings: " + aiControllers[2].buildingsInTeam.Count.ToString() + ", Units: " + aiControllers[2].unitsInTeam.Count.ToString());
        GD.Print("AI4 - Buildings: " + aiControllers[3].buildingsInTeam.Count.ToString() + ", Units: " + aiControllers[3].unitsInTeam.Count.ToString());
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		base._Process(delta);
	}

    public override void AddUnit(UnitParent unit)
    {
        base.AddUnit(unit);
        GD.Print("New Unit: " + unit.GetFactionComponent().unitInfo.unitName);
        //If this unit was added from one of the AI's factories, then make sure to add it to the team.
        for (int i=0; i<aiControllers.Length; i++)
        {
            if (aiControllers[i].buildingsInTeam.Contains((BuildingParent)unit.motherFactory))
            {
                GD.Print("Unit added to AI controller");
                aiControllers[i].AddUnit(unit);
            }
        }
    }

    public override void RemoveUnit(UnitParent unit)
    {
        base.RemoveUnit(unit);

        //Make sure to remove the unit from the AI team it's in
        for (int i = 0; i < aiControllers.Length; i++)
        {
            if (aiControllers[i].unitsInTeam.Contains(unit))
            {
                aiControllers[i].RemoveUnit(unit);
            }
        }
    }

    public override void AddBuilding(BuildingParent building)
    {
        base.AddBuilding(building);
    }

    public override void RemoveBuilding(BuildingParent building)
    {
        base.RemoveBuilding(building);

        //Make sure to remove the building from the AI team it's in
        for (int i = 0; i < aiControllers.Length; i++)
        {
            if (aiControllers[i].buildingsInTeam.Contains(building))
            {
                aiControllers[i].RemoveBuilding(building);
            }
        }
    }
}

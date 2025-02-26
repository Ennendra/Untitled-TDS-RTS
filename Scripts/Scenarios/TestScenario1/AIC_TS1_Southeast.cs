using Godot;
using System;

public partial class AIC_TS1_Southeast : MainAIController
{

    Vector2[] defensePath1, defensePath2;
    Vector2[] attackPath1, attackPath2;

    public override void _Ready()
    {
        base._Ready();

        //Define rally points for factories (Before they move on paths)
        attackRallyPoints.Add(new Vector2(7875, 4275));
        attackRallyPoints.Add(new Vector2(8775, 4275));
        attackRallyPoints.Add(new Vector2(9750, 3075));
        attackRallyPoints.Add(new Vector2(9900, 2425));
        defenseRallyPoints.Add(new Vector2(10800, 4325));

        //Set waypoint paths
        defensePath1 = new Vector2[2];
        defensePath1[0] = new Vector2(7600, 4050);
        defensePath1[1] = new Vector2(9150, 4050);
        defensePath2 = new Vector2[4];
        defensePath2[0] = new Vector2(9600, 3225);
        defensePath2[1] = new Vector2(11450, 3125);
        defensePath2[2] = new Vector2(11300, 2175);
        defensePath2[3] = new Vector2(9450, 2450);

        attackPath1 = new Vector2[6];
        attackPath1[0] = new Vector2(8275, 2825);
        attackPath1[1] = new Vector2(6925, 3100);
        attackPath1[2] = new Vector2(6775, 4600);
        attackPath1[3] = new Vector2(3900, 4150);
        attackPath1[4] = new Vector2(800, 5425);
        attackPath1[5] = new Vector2(800, 3050);
        attackPath2 = new Vector2[8];
        attackPath2[0] = new Vector2(8275, 2825);
        attackPath2[1] = new Vector2(6800, 1400);
        attackPath2[2] = new Vector2(4500, 1275);
        attackPath2[3] = new Vector2(3825, 450);
        attackPath2[4] = new Vector2(1200, 300);
        attackPath2[5] = new Vector2(950, 1225);
        attackPath2[6] = new Vector2(2550, 1475);
        attackPath2[7] = new Vector2(875, 3025);

        //Initialise defense groups
        GenerateNewDefenseGroup();
    }

    public override void ProcessAITick()
    {
        base.ProcessAITick();
    }

    

    public override void GenerateNewAttackGroup()
    {
        //Southeast: random attack waves, can be large
        int[] unitBuildWeight = new int[] { 0, 1, 1 };
        int buildSize = RandIntRange(8, 25);
        //Choosing one of the attack paths
        Vector2[] chosenAttackPath;
        float chosenPathRand = GD.Randf();
        if (chosenPathRand <= 0.5f) { chosenAttackPath = attackPath1; }
        else { chosenAttackPath = attackPath2; }
        //Create the new attack wave, which will generate a randomised group from that weighting
        AIControlGroup newGroup = new AIControlGroup();
        newGroup.InitGroup(chosenAttackPath, buildSize, unitBuildWeight);
        attackGroups.Add(newGroup);
    }
    public override void GenerateNewDefenseGroup()
    {
        //Southeast. Two teams, one on the west side, the other in the north
        int[] defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2 };
        AIControlGroup newGroup = new AIControlGroup();
        newGroup.InitGroup(defensePath1, defenseGroupComp);
        newGroup.SetAsDefenseGroup();
        defenseGroups.Add(newGroup);

        defenseGroupComp = new int[] { 0, 0, 1, 1, 1, 1, 1, 1, 2, 2 };
        newGroup = new AIControlGroup();
        newGroup.InitGroup(defensePath2, defenseGroupComp);
        newGroup.SetAsDefenseGroup();
        defenseGroups.Add(newGroup);
    }
}

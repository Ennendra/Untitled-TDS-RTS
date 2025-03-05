using Godot;
using System;

public partial class AIC_TS1_Northeast : MainAIController
{

    Vector2[] defensePath1, defensePath2, defensePath3;
    Vector2[] attackPath1, attackPath2;

    public override void _Ready()
    {
        base._Ready();

        //Define rally points for factories (Before they move on paths)
        attackRallyPoints.Add(new Vector2(9200, 2300));
        attackRallyPoints.Add(new Vector2(9200, 2800));
        attackRallyPoints.Add(new Vector2(6800, 375));
        attackRallyPoints.Add(new Vector2(6825, 975));
        defenseRallyPoints.Add(new Vector2(9400, 1350));

        //Set waypoint paths
        defensePath1 = new Vector2[1];
        defensePath1[0] = new Vector2(6825, 425);
        defensePath2 = new Vector2[2];
        defensePath2[0] = new Vector2(7700, 1825);
        defensePath2[1] = new Vector2(6975, 2775);
        defensePath3 = new Vector2[2];
        defensePath3[0] = new Vector2(8725, 2275);
        defensePath3[1] = new Vector2(10775, 2075);


        attackPath1 = new Vector2[5];
        attackPath1[0] = new Vector2(7125, 2325);
        attackPath1[1] = new Vector2(6000, 1150);
        attackPath1[2] = new Vector2(4300, 1225);
        attackPath1[3] = new Vector2(2700, 1725);
        attackPath1[4] = new Vector2(975, 3050);
        attackPath2 = new Vector2[5];
        attackPath2[0] = new Vector2(7125, 2325);
        attackPath2[1] = new Vector2(6950, 3650);
        attackPath2[2] = new Vector2(4625, 4250);
        attackPath2[3] = new Vector2(700, 5325);
        attackPath2[4] = new Vector2(950, 2950);

        //Initialise defense groups
        GenerateNewDefenseGroup();
    }

    public override void ProcessAITick()
    {
        base.ProcessAITick();
    }

    

    public override void GenerateNewAttackGroup()
    {
        //Northeast, random attack waves
        int[] unitBuildWeight = new int[] { 0, 0, 1, 1, 1, 2 };
        int buildSize = RandIntRange(10, 15);
        //Choosing one of the attack paths
        Vector2[] chosenAttackPath;
        float chosenPathRand = GD.Randf();
        if (chosenPathRand <= 0.5f) { chosenAttackPath = attackPath1; }
        else { chosenAttackPath = attackPath2; }
        //Create the new attack wave, which will generate a randomised group from that weighting
        AIControlGroup newGroup = new AIControlGroup();
        newGroup.InitGroup(chosenAttackPath, buildSize, unitBuildWeight);
        attackGroups.Add(newGroup);
        newGroup.aiController = this;
    }
    public override void GenerateNewDefenseGroup()
    {
        //Northeast - 3 teams, one stationary at west, one patrol near center, other patrol line at south
        int[] defenseGroupComp = new int[] { 0, 0, 1, 2, 2, 2, 2, 2 };
        AIControlGroup newGroup = new AIControlGroup();
        newGroup.InitGroup(defensePath1, defenseGroupComp);
        newGroup.SetAsDefenseGroup();
        defenseGroups.Add(newGroup);
        newGroup.aiController = this;

        defenseGroupComp = new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 1 };
        newGroup = new AIControlGroup();
        newGroup.InitGroup(defensePath2, defenseGroupComp);
        newGroup.SetAsDefenseGroup();
        defenseGroups.Add(newGroup);
        newGroup.aiController = this;

        defenseGroupComp = new int[] { 0, 0, 0, 1, 1, 1, 1, 1, 1, 2 };
        newGroup = new AIControlGroup();
        newGroup.InitGroup(defensePath3, defenseGroupComp);
        newGroup.SetAsDefenseGroup();
        defenseGroups.Add(newGroup);
        newGroup.aiController = this;
    }
}

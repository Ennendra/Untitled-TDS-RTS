using Godot;
using System;

public partial class AIC_TS1_Southeast : MainAIController
{

    Vector2[] defensePath;
    Vector2[] attackPath1;

    public override void _Ready()
    {
        base._Ready();

        //Define rally points for factories (Before they move on paths)
        //attackRallyPoints.Add(new Vector2(5200, 5650));
        //defenseRallyPoints.Add(new Vector2(6400, 5650));

        //Set waypoint paths
        //defensePath = new Vector2[2];
        //defensePath[0] = new Vector2(5350, 4200);
        //defensePath[1] = new Vector2(6500, 5300);

        //attackPath1 = new Vector2[4];
        //attackPath1[0] = new Vector2(6500, 5400);
        //attackPath1[1] = new Vector2(3500, 4050);
        //attackPath1[2] = new Vector2(2450, 3000);
        //attackPath1[3] = new Vector2(625, 2975);

        //Initialise defense groups
        GenerateNewDefenseGroup();
    }

    public override void ProcessAITick()
    {
        base.ProcessAITick();
    }

    

    public override void GenerateNewAttackGroup()
    {
        //Template - Generating random attack waves with varying weights and group sizes (weight is 80% Unit1, 20% Unit2)
        //int[] unitBuildWeight = new int[] { 0, 0, 0, 0, 1 };
        //int buildSize = RandIntRange(6, 9);
        //Choosing one of the attack paths
        //Vector2[] chosenAttackPath = attackPath1;
        //Create the new attack wave, which will generate a randomised group from that weighting
        //AIControlGroup newGroup = new AIControlGroup();
        //newGroup.InitGroup(chosenAttackPath, buildSize, unitBuildWeight);
        //attackGroups.Add(newGroup);
    }
    public override void GenerateNewDefenseGroup()
    {
        //Template - Generating a pre-defined defense wave (4 Unit1, 4 Unit2)
        //int[] defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1 };
        //AIControlGroup newGroup = new AIControlGroup();
        //newGroup.InitGroup(defensePath, defenseGroupComp);
        //newGroup.SetAsDefenseGroup();
        //defenseGroups.Add(newGroup);
    }
}

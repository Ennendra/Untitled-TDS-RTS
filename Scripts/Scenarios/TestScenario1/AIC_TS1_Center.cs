using Godot;
using System;

public partial class AIC_TS1_Center : MainAIController
{
    //Center AI: Makes multiple defense groups over time that patrol and make obstacles.
    //Will start making its own attack waves after an extended amount of time

    Vector2[] defensePath1, defensePath2, defensePath3, defensePath4;
    Vector2[] attackPath1, attackPath2;
    float[] defenseGroupGenerateTime = { 1, 80, 160, 300 };
    bool[] defenseGroupsGenerated = { false, false, false, false };
    

    public override void _Ready()
    {
        base._Ready();

        //Define rally points for factories (Before they move on paths)
        attackRallyPoints.Add(new Vector2(5300, 3300));
        defenseRallyPoints.Add(new Vector2(5300, 2500));

        //Set waypoint paths
        defensePath1 = new Vector2[2]; //Bottom left
        defensePath1[0] = new Vector2(3850, 3375);
        defensePath1[1] = new Vector2(4575, 4200);
        defensePath2 = new Vector2[2]; //Top left
        defensePath2[0] = new Vector2(3850, 2325);
        defensePath2[1] = new Vector2(4775, 1075);
        defensePath3 = new Vector2[2]; // Bottom line
        defensePath3[0] = new Vector2(6675, 4025);
        defensePath3[1] = new Vector2(4425, 4175);
        defensePath4 = new Vector2[2]; // Top line
        defensePath4[0] = new Vector2(6650, 1375);
        defensePath4[1] = new Vector2(4300, 1425);


        attackPath1 = new Vector2[4]; //path via bottom of base
        attackPath1[0] = new Vector2(4250, 3775);
        attackPath1[1] = new Vector2(2750, 4025);
        attackPath1[2] = new Vector2(2400, 2950);
        attackPath1[3] = new Vector2(475, 3025);
        attackPath2 = new Vector2[4]; //Path via top of base
        attackPath2[0] = new Vector2(6150, 1400);
        attackPath2[1] = new Vector2(4550, 1325);
        attackPath2[2] = new Vector2(2700, 2200);
        attackPath2[3] = new Vector2(1575, 3000);

        //Initialise defense groups
        GenerateNewDefenseGroup();
    }

    public override void ProcessAITick()
    {
        base.ProcessAITick();

        //Generate each defense group for building once the mission time has reached each stage
        for (int i = 0; i < 4; i++)
        {
            if (!defenseGroupsGenerated[i] && PostAwakeningAITimer >= defenseGroupGenerateTime[i])
            {
                GenerateNewDefenseGroup(i);
                defenseGroupsGenerated[i] = true;
            }
        }
    }

    

    public override void GenerateNewAttackGroup()
    {
        AIControlGroup newGroup = new AIControlGroup();
        attackGroups.Add(newGroup);
        newGroup.aiController = this;
        //Center - Generating the attack wave later in the game
        if (PostAwakeningAITimer > 540)
        {
            int[] unitBuildWeight = new int[] { 0, 0, 1, 1, 1, 1, 2 };
            int buildSize = RandIntRange(12, 16);
            //Choosing one of the attack paths
            Vector2[] chosenAttackPath;
            float chosenPathRand = GD.Randf();
            if (chosenPathRand <= 0.5f) { chosenAttackPath = attackPath1; }
            else { chosenAttackPath = attackPath2; }
            //Create the new attack wave, which will generate a randomised group from that weighting
            newGroup.InitGroup(chosenAttackPath, buildSize, unitBuildWeight);
        }
    }
    public override void GenerateNewDefenseGroup()
    {
        //Center - See function below, for each defense group over time
    }

    public void GenerateNewDefenseGroup(int groupIndex)
    {
        int[] defenseGroupComp;
        Vector2[] chosenPath;
        switch (groupIndex) 
        { 
            case 0:
                defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1 };
                chosenPath = defensePath1;
                break;
            case 1:
                defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1 };
                chosenPath = defensePath2;
                break;
            case 2:
                defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2 };
                chosenPath = defensePath3;
                break;
            case 3:
                defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2 };
                chosenPath = defensePath4;
                break;
            default:
                defenseGroupComp = new int[] { 0, 0, 0, 0, 1, 1, 1, 1 };
                chosenPath = defensePath1;
                break;

        }
        
        AIControlGroup newGroup = new AIControlGroup();
        newGroup.InitGroup(chosenPath, defenseGroupComp);
        newGroup.SetAsDefenseGroup();
        defenseGroups.Add(newGroup);
        newGroup.aiController = this;
    }
}

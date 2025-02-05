using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

public partial class UI_RTSToolbar : Control
{
    [Export] BuildButton[] buildingButtons;
    [Export] FactoryBuildButton[] factoryBuildingButtons;
    // [Export] UnitBuildButton unitBuildingButtons;
    [Export] UI_SelectedUnitInfo[] selectedUnitButtons;
    [Export] OrderButton[] orderButtons;
    Control buildingButtonContainer;
    Control factoryButtonContainer;
    Label orderDesignationLabel;
    TextureProgressBar healthBar;

    //The list of items that the building buttons will be linked to
    [Export] ConstructInfo[] buildingList_tier1;

    public struct FactoryQueueInfo
    {
        public ConstructInfo cInfo = null;
        public int amount = 0;
        public FactoryQueueInfo(ConstructInfo cInfo, int amount)
        {
            this.cInfo = cInfo;
            this.amount = amount;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SetBuildingButtons(buildingList_tier1);

        buildingButtonContainer = GetNode<Control>("BuildButtonContainer");
        factoryButtonContainer = GetNode<Control>("FactoryBuildButtonContainer");
        orderDesignationLabel = GetNode<Label>("LabelOrder");
        healthBar = GetNode<TextureProgressBar>("HealthBar");
    }

    //Functions for selected unit info
    public void SetUnitSelectButtonConnection(Node2D connection)
    {
        foreach (UI_SelectedUnitInfo button in selectedUnitButtons)
        {
            button.SetButtonConnection(connection);
            button.Visible = false;
        }
    }
    public void SetOrderHotbarButtonConnection(Node2D connection)
    {
        foreach (OrderButton button in orderButtons)
        {
            button.SetButtonConnection(connection);
        }
    }
    public void SetUnitSelectionUI(List<RTSSelectionType> selection)
    {
        for (int i = 0; i < selectedUnitButtons.Length; i++)
        {
            if (i < selection.Count)
            {
                selectedUnitButtons[i].UpdateUnitInfo(selection[i].unitInfo, selection[i].amount);
                selectedUnitButtons[i].Visible = true;
            }
            else
            {
                selectedUnitButtons[i].Visible = false;
            }
        }

        //Enable or disable the toolbar based on whether we selected any non-structure units
        if (selection.Count > 0)
        {
            EnableOrderHotbar();
        }
        else
        {
            DisableOrderHotbar();
        }

    }
    //Functions for the building buttons
    public void SetBuildingButtons(ConstructInfo[] buildingList)
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (i < buildingList.Length)
            {
                buildingButtons[i].SetButtonConstructInfo(buildingList[i]);
                buildingButtons[i].Visible = true;
            }
            else
            {
                buildingButtons[i].Visible = false;
            }
        }
    }
    public void SetBuildButtonConnection(Node2D connection)
    {
        foreach (BuildButton button in buildingButtons)
        {
            button.SetButtonConnection(connection);
        }
    }
    //Functions for the factory building buttons
    public void SetFactoryBuildButtonConnection(Node2D connection)
    {
        foreach (FactoryBuildButton button in factoryBuildingButtons)
        {
            button.SetButtonConnection(connection);
        }
    }
    public void SetFactoryBuildingButtons(ConstructInfo[] buildingList)
    {
        //Hide the building button container and reveal the factory button container
        buildingButtonContainer.Visible = false;
        factoryButtonContainer.Visible = true;

        for (int i = 0; i < factoryBuildingButtons.Length; i++)
        {
            if (i < buildingList.Length)
            {
                factoryBuildingButtons[i].SetButtonConstructInfo(buildingList[i]);
                factoryBuildingButtons[i].Visible = true;
            }
            else
            {
                factoryBuildingButtons[i].Visible = false;
            }
        }
    }

    public void ResetBuildingButtons()
    {
        buildingButtonContainer.Visible = true;
        factoryButtonContainer.Visible = false;
    }

    public void SetOrderLabel(string text)
    {
        orderDesignationLabel.Visible = true;
        orderDesignationLabel.Text = text;
    }
    public void DisableOrderLabel()
    {
        orderDesignationLabel.Visible= false;
    }

    public void EnableOrderHotbar()
    {
        foreach(OrderButton button in orderButtons)
        {
            button.Disabled = false;
            button.Modulate = new Color(1, 1, 1, 1);
        }
    }
    public void DisableOrderHotbar()
    {
        foreach (OrderButton button in orderButtons)
        {
            button.Disabled = true;
            button.Modulate = new Color(0.5f, 0.5f, 0.5f, 1);
        }
    }

    public void SetFactoryBuildingQueueAmounts(List<ConstructInfo> buildList)
    {
        //First, split the construct info into each item and how many of each there are
        List<FactoryQueueInfo> queueInfo = new();
        List<ConstructInfo> unitTypesInQueue = new();
        foreach (ConstructInfo info in buildList)
        {
            if (unitTypesInQueue.Contains(info))
            {
                int uTypeIndex = queueInfo.FindIndex(t => t.cInfo ==  info);
                FactoryQueueInfo queueItem = queueInfo[uTypeIndex];
                queueItem.amount += 1;
                queueInfo[uTypeIndex] = queueItem;
            }
            else
            {
                FactoryQueueInfo newQueueItem = new(info, 1);
                queueInfo.Add(newQueueItem);
                unitTypesInQueue.Add(info);
            }
        }

        //Then, check each button to see if there are units of its type in the factory queues, or set it to 0 otherwise
        for (int i = 0; i < factoryBuildingButtons.Length; i++)
        {
            ConstructInfo buttonInfo = factoryBuildingButtons[i].GetButtonConstructInfo();
            if (unitTypesInQueue.Contains(buttonInfo))
            {
                int queueInfoIndex = queueInfo.FindIndex(t => t.cInfo == buttonInfo);
                FactoryQueueInfo queueItem = queueInfo[queueInfoIndex];
                factoryBuildingButtons[i].SetButtonConstructAmount(queueItem.amount);
            }
            else
            {
                factoryBuildingButtons[i].SetButtonConstructAmount(0);
            }
        }
    }


    //Functions for the healthbar
    public void GetNewHealthData(float healthPercent)
    {
        healthBar.Value = healthPercent;
    }

}

using Godot;
using System;

public partial class Globals : Node
{
    //Marking the main battle area for pathfinding purposes
	public NavigationRegion2D navigationAreaGround;
    public NavigationRegion2D navigationAreaHover;


	public Resource cursorImage_Personal = ResourceLoader.Load("res://Textures/UI/Icons/CustomCursors/Cursor.png");
    public Resource cursorImage_Move = ResourceLoader.Load("res://Textures/UI/Icons/CustomCursors/UICursorMove.png");
    public Resource cursorImage_Attack = ResourceLoader.Load("res://Textures/UI/Icons/CustomCursors/UICursorAttack.png");
    public Resource cursorImage_Guard = ResourceLoader.Load("res://Textures/UI/Icons/CustomCursors/UICursorGuard.png");
    string currentCursor = "";
    //Set the custom mouse cursor using a string code:
    //"Personal" -- "Move" -- "Attack" -- "Guard"
    public void SetNewCustomCursor(string newCursor)
    {
        if (currentCursor == newCursor) return;

        currentCursor = newCursor;

        switch (newCursor) 
        {
            case "Personal": Input.SetCustomMouseCursor(cursorImage_Personal, Input.GetCurrentCursorShape(), new Vector2(7.5f, 7.5f)); break;
            case "Move": Input.SetCustomMouseCursor(cursorImage_Move, Input.GetCurrentCursorShape(), new Vector2(12.5f, 12.5f)); break;
            case "Attack": Input.SetCustomMouseCursor(cursorImage_Attack, Input.GetCurrentCursorShape(), new Vector2(12.5f, 12.5f)); break;
            case "Guard": Input.SetCustomMouseCursor(cursorImage_Guard, Input.GetCurrentCursorShape(), new Vector2(12.5f, 12.5f)); break;
        }

    }
}

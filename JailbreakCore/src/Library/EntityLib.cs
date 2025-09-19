using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class EntityLib
{
    //__________________________________________________________________________________________
    //
    // Code Borrowed From: https://github.com/destoer/Cs2Jailbreak/blob/master/src/Lib/Entity.cs
    //__________________________________________________________________________________________ 
    public static bool g_CellsOpened = false;

    public static void ForceEntityInput(string name, string input)
    {
        var target = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(name);

        foreach (var entity in target)
        {
            if (!entity.IsValid)
                continue;

            entity.AcceptInput(input);
        }
    }
    public static void OpenCells(string callerName = "")
    {
        g_CellsOpened = true;
        Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["doors_opened_by", callerName]);

        ForceEntityInput("func_door", "Open");
        ForceEntityInput("func_movelinear", "Open");
        ForceEntityInput("func_door_rotating", "Open");
        ForceEntityInput("prop_door_rotating", "Open");
        ForceEntityInput("func_breakable", "Break");
    }
    public static void CloseCells(string callerName = "")
    {
        g_CellsOpened = false;
        Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["doors_closed_by", callerName]);

        ForceEntityInput("func_door", "Close");
        ForceEntityInput("func_movelinear", "Close");
        ForceEntityInput("func_door_rotating", "Close");
        ForceEntityInput("prop_door_rotating", "Close");
    }
    //_______________________
    //
    // E    N    D
    //_______________________ 

}
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class GunsMenuCommands
{
    public static void Register()
    {
        foreach (var cmd in Instance.Config.GunsMenu.GunsMenuCommands)
        {
            Instance.AddCommand($"css_{cmd}", "Opens Guns Menu", Command_GunsMenu);
        }
    }
    private static void Command_GunsMenu(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
        var activeDay = SpecialDayManagement.GetActiveDay();

        if (LastRequestManagement.GetActiveRequest() != null)
            return;

        bool isTeleportDay = activeDay is TeleportDay;

        if (controller.Team == CsTeam.Terrorist)
        {
            if (jbPlayer.Role != JBRole.Guardian && !isTeleportDay)
            {
                info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["no_acces"]);
                return;
            }
        }
        if (jbPlayer.Role == JBRole.Guardian || jbPlayer.IsWarden)
        {
            GunsMenu.Display(jbPlayer);
            return;
        }

        info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["no_acces"]);
    }

}
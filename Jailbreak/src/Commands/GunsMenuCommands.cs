using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class GunsMenuCommands
{
    // imma have to store active days name here :)) (i'm dumb)
    private static string TeleportDayName => Instance.Localizer["teleport_day<name>"];
    private static string NoScopeDayName => Instance.Localizer["no_scope_day<name>"];
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
        var ActiveDay = SpecialDayManagement.GetActiveDay();

        if (jbPlayer.Role != JBRole.Guardian && ActiveDay != null && ActiveDay.Name != TeleportDayName)
        {
            // this check is made so prisoners are only allowed to use guns menu in some special days.

            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["no_acces"]);
            return;
        }

        GunsMenu.Display(jbPlayer);
    }
}
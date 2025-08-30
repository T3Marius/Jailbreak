using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class WardenCommands
{
    public static void Register()
    {
        foreach (var cmd in Instance.Config.Warden.Commands.TakeWarden)
        {
            Instance.AddCommand($"css_{cmd}", "Takes Warden", Command_TakeWarden);
        }
        foreach (var cmd in Instance.Config.Warden.Commands.GiveUpWarden)
        {
            Instance.AddCommand($"css_{cmd}", "Give up Warden", Command_GiveUpWarden);
        }
    }
    private static void Command_TakeWarden(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        var jbPlayer = JBPlayerManagement.GetOrCreate(player);

        if (jbPlayer.IsPrisoner)
        {
            jbPlayer.PrinToChat(Instance.Localizer["prefix"] + Instance.Localizer["only_guard"]);
            return;
        }

        if (JBPlayerManagement.GetWarden() != null)
        {
            jbPlayer.PrintToHtml(Instance.Localizer["aleardy_warden", JBPlayerManagement.GetWarden()!.Name], 3);
            return;
        }

        jbPlayer.SetWarden(true);

        List<JBPlayer> jbPlayers = JBPlayerManagement.GetAllPlayers();

        foreach (var jbP in jbPlayers)
        {
            jbP.PrintToHtml(Instance.Localizer["new_warden", jbPlayer.Name], 3);

            if (!string.IsNullOrEmpty(Instance.Config.Warden.WardenSetSound))
                jbP.Controller?.ExecuteClientCommand($"play {Instance.Config.Warden.WardenSetSound}");

        }
    }
    private static void Command_GiveUpWarden(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        var jbPlayer = JBPlayerManagement.GetOrCreate(player);

        if (jbPlayer.IsPrisoner)
        {
            jbPlayer.PrinToChat(Instance.Localizer["prefix"] + Instance.Localizer["only_guard"]);
            return;
        }

        if (!jbPlayer.IsWarden)
        {
            jbPlayer.PrinToChat(Instance.Localizer["prefix"] + Instance.Localizer["not_warden"]);
            return;
        }

        jbPlayer.SetWarden(false);

        List<JBPlayer> jbPlayers = JBPlayerManagement.GetAllPlayers();

        foreach (var jbP in jbPlayers)
        {
            jbP.PrintToHtml(Instance.Localizer["warden_gave_up", jbPlayer.Name, Instance.Config.Warden.Commands.TakeWarden.FirstOrDefault()!], 3);

            if (!string.IsNullOrEmpty(Instance.Config.Warden.WardenRemovedSound))
                jbP.Controller?.ExecuteClientCommand($"play {Instance.Config.Warden.WardenRemovedSound}");
        }
    }
}
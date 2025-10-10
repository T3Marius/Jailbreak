using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

namespace Jailbreak;

public static class PrisonerCommands
{
    public static HashSet<CCSPlayerController> SurrenderTries { get; set; } = new();
    public static void Register()
    {
        foreach (var cmd in Instance.Config.Prisoner.Commands.LastRequest)
        {
            Instance.AddCommand($"css_{cmd}", "Starts Last Request", Command_LastRequest);
        }
        foreach (var cmd in Instance.Config.Prisoner.Commands.Surrender)
        {
            Instance.AddCommand($"css_{cmd}", "Surrender as a rebel.", Command_Surrender);
        }
    }
    private static void Command_LastRequest(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        if (player.Team != CsTeam.Terrorist)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["last_request_must_be_prisoner"]);
            return;
        }

        List<CCSPlayerController> alivePrisoners = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive).ToList();
        List<CCSPlayerController> aliveGuardians = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive).ToList();

        if (alivePrisoners.Count != 1)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["cant_use_last_request"]);
            return;
        }
        if (!aliveGuardians.Any())
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["last_request_no_guardians"]);
            return;
        }

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(player);
        if (jbPlayer.IsRebel)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["cant_use_last_request_as_rebel"]);
            return;
        }
        LastRequestMenu.Display(jbPlayer);
    }
    private static void Command_Surrender(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        if (controller.Team != CsTeam.Terrorist)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["no_acces"]);
            return;
        }

        JBPlayer rebel = JBPlayerManagement.GetOrCreate(controller);

        if (!rebel.IsRebel)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["not_rebel"]);
            return;
        }

        if (SurrenderTries.Contains(rebel.Controller))
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["surrender_max_tries_attempt"]);
            return;
        }

        JBPlayer? warden = JBPlayerManagement.GetWarden();
        if (warden == null)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["no_warden_to_surrender"]);
            return;
        }

        SurrenderTries.Add(rebel.Controller);

        rebel.Print("chat", Instance.Localizer["prefix"] + Instance.Localizer["surrender_sent"]);
        WardenMenu.OpenSurrenderRequestMenu(warden, rebel);


    }

}

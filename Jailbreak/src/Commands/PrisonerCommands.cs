using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

namespace Jailbreak;

public static class PrisonerCommands
{
    public static void Register()
    {
        foreach (var cmd in Instance.Config.Prisoner.Commands.LastRequest)
        {
            Instance.AddCommand($"css_{cmd}", "Starts Last Request", Command_LastRequest);
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
}

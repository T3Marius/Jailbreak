using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
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
    private static void Command_TakeWarden(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
        JBPlayer? currentWarden = JBPlayerManagement.GetWarden();

        if (!controller.PawnIsAlive)
            return;

        if (jbPlayer.Role == JBRole.Prisoner)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["cant_become_warden_as_t"]);
            return;
        }

        if (currentWarden != null)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["aleardy_warden", currentWarden.PlayerName]);
            return;
        }

        jbPlayer.SetWarden(true);
        jbPlayer.Print("chat", Instance.Localizer["prefix"] + Instance.Localizer["warden_take"]);

        Library.PrintToAlertAll(Instance.Localizer["warden_take_alert", jbPlayer.PlayerName]);

        if (!string.IsNullOrEmpty(Instance.Config.Warden.WardenSetSound))
        {
            foreach (var player in Utilities.GetPlayers())
            {
                RecipientFilter filter = [player];
                player.EmitSound(Instance.Config.Warden.WardenSetSound, filter, Instance.Config.GlobalVolume.WardenSetVolume);
            }
        }

    }
    private static void Command_GiveUpWarden(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
        JBPlayer? currentWarden = JBPlayerManagement.GetWarden();

        if (jbPlayer.Role == JBRole.Prisoner)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["you_are_not_warden"]);
            return;
        }

        if (currentWarden != jbPlayer)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["you_are_not_warden"]);
            return;
        }

        currentWarden.SetWarden(false);
        currentWarden.Print("chat", Instance.Localizer["prefix"] + Instance.Localizer["gave_up_on_warden"]);

        Library.PrintToAlertAll(Instance.Localizer["warden_gave_up", currentWarden.PlayerName, Instance.Config.Warden.Commands.TakeWarden.FirstOrDefault()!]);

        if (!string.IsNullOrEmpty(Instance.Config.Warden.WardenRemovedSound))
        {
            foreach (var player in Utilities.GetPlayers())
            {
                RecipientFilter filter = [player];
                player.EmitSound(Instance.Config.Warden.WardenRemovedSound, filter, Instance.Config.GlobalVolume.WardenRemovedVolume);
            }
        }

        Instance.AddTimer(5.0f, () =>
        {
            if (JBPlayerManagement.GetWarden() == null)
            {
                Library.AssignRandomWarden();
                Library.PrintToCenterAll(Instance.Localizer["warden_take_alert", JBPlayerManagement.GetWarden()?.PlayerName ?? ""]);
            }
        });
    }
}
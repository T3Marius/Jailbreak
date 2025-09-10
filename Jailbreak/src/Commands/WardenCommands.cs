using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
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
        foreach (var cmd in Instance.Config.Warden.Commands.WardenMenu)
        {
            Instance.AddCommand($"css_{cmd}", "Open Warden Menu", Command_WardenMenu);
        }
        foreach (var cmd in Instance.Config.Warden.Commands.SpecialDaysMenu)
        {
            Instance.AddCommand($"css_{cmd}", "Open Special-Days Menu", Command_SpecialDays);
        }
        foreach (var cmd in Instance.Config.Warden.Commands.ToggleBox)
        {
            Instance.AddCommand($"css_{cmd}", "Toggles box", Command_ToggleBox);
        }
        foreach (var cmd in Instance.Config.Warden.Commands.ColorPrisoner)
        {
            Instance.AddCommand($"css_{cmd}", "Color a prisoner", Command_Color);
        }
    }
    private static void Command_TakeWarden(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
        JBPlayer? currentWarden = JBPlayerManagement.GetWarden();

        if (SpecialDayManagement.GetActiveDay() != null)
            return;

        if (LastRequestManagement.GetActiveRequest() != null)
            return;

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

        if (!string.IsNullOrEmpty(Instance.Config.Sounds.WardenSetSound))
        {
            foreach (var player in Utilities.GetPlayers())
            {
                RecipientFilter filter = [player];
                player.EmitSound(Instance.Config.Sounds.WardenSetSound, filter, Instance.Config.GlobalVolume.WardenSetVolume);
            }
        }

    }
    private static void Command_GiveUpWarden(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
        JBPlayer? currentWarden = JBPlayerManagement.GetWarden();

        if (SpecialDayManagement.GetActiveDay() != null)
            return;

        if (LastRequestManagement.GetActiveRequest() != null)
            return;

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

        if (!string.IsNullOrEmpty(Instance.Config.Sounds.WardenRemovedSound))
        {
            foreach (var player in Utilities.GetPlayers())
            {
                RecipientFilter filter = [player];
                player.EmitSound(Instance.Config.Sounds.WardenRemovedSound, filter, Instance.Config.GlobalVolume.WardenRemovedVolume);
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
    private static void Command_WardenMenu(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);

        if (!jbPlayer.IsWarden)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["you_are_not_warden"]);
            return;
        }

        WardenMenu.Display(jbPlayer);
    }
    private static void Command_SpecialDays(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);

        if (!jbPlayer.IsWarden)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["you_are_not_warden"]);
            return;
        }

        SpecialDaysMenu.Display(jbPlayer);
    }
    private static void Command_ToggleBox(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);

        if (SpecialDayManagement.GetActiveDay() != null)
            return;

        if (LastRequestManagement.GetActiveRequest() != null)
            return;

        if (!jbPlayer.IsWarden)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["you_are_not_warden"]);
            return;
        }

        Events.g_IsBoxActive = !Events.g_IsBoxActive;

        if (Events.g_IsBoxActive)
            Library.StartBox(jbPlayer.PlayerName);
        else
            Library.StopBox(jbPlayer.PlayerName);
    }
    private static void Command_Color(CCSPlayerController? controller, CommandInfo info)
    {
        if (controller == null)
            return;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);

        if (info.ArgCount <= 1)
        {
            // open color menu (in future though)
            return;
        }

        if (!jbPlayer.IsWarden)
        {
            info.ReplyToCommand(Instance.Localizer["prefix"] + Instance.Localizer["you_are_not_warden"]);
            return;
        }
        Color color = Color.Transparent;
        string targetName = info.ArgByIndex(1);

        string colorName = info.ArgByIndex(2);
        if (colorName.Equals("default"))
        {
            color = Color.FromArgb(255, 255, 255, 255);
        }
        else
            color = Color.FromName(colorName);

        foreach (var target in Utilities.GetPlayers().Where(p => p.PlayerName == targetName && p.Team == CsTeam.Terrorist))
        {
            JBPlayer targetJbPlayer = JBPlayerManagement.GetOrCreate(target);
            if (targetJbPlayer.Role == JBRole.Guardian)
                return;

            targetJbPlayer.SetColor(color);
            target.PrintToChat(Instance.Localizer["prefix"] + Instance.Localizer["prisoner_colored", jbPlayer.PlayerName, target.PlayerName]);
        }

    }
}
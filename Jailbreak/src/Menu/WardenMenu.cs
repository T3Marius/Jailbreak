using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class WardenMenu
{
    private static Random random = new Random();
    public static void Display(JBPlayer jbPlayer)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPlayer.Controller, "warden_menu<title>"));
        menu.FreezePlayer = false;

        menu.AddBoolOption(Instance.Localizer.ForPlayer(jbPlayer.Controller, "toggle_cells<option>"), defaultValue: EntityLib.g_CellsOpened, (p, o) =>
        {
            if (jbPlayer.IsWarden)
            {
                if (o is IT3Option boolOption)
                {
                    bool isEnabled = boolOption.OptionDisplay!.Contains("✔");

                    if (isEnabled)
                    {
                        EntityLib.OpenCells(jbPlayer.PlayerName);
                    }
                    else
                    {
                        EntityLib.CloseCells(jbPlayer.PlayerName);
                    }
                }
            }
        });

        menu.AddBoolOption(Instance.Localizer.ForPlayer(jbPlayer.Controller, "toggle_box<option>"), defaultValue: Events.g_IsBoxActive, (p, o) =>
        {
            if (jbPlayer.IsWarden)
            {
                if (o is IT3Option boolOption)
                {
                    bool isEnabled = boolOption.OptionDisplay!.Contains("✔");

                    if (isEnabled)
                    {
                        Library.StartBox(jbPlayer.PlayerName);
                    }
                    else
                    {
                        Library.StopBox(jbPlayer.PlayerName);
                    }
                }
            }
        });
        menu.AddOption(Instance.Localizer.ForPlayer(jbPlayer.Controller, "select_random_prisoner<option>"), (p, o) =>
        {
            if (jbPlayer.IsWarden)
            {
                List<CCSPlayerController> prisonerToSelect = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive).ToList();

                if (prisonerToSelect.Count == 0)
                {
                    jbPlayer.Print("chat", Instance.Localizer["prefix"] + Instance.Localizer["no_prisoner_avalible"]);
                    return;
                }

                CCSPlayerController randomPrisoner = prisonerToSelect[random.Next(prisonerToSelect.Count)];

                if (randomPrisoner != null)
                {
                    Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["random_prisoner_selected", randomPrisoner.PlayerName]);
                }
            }
        });
        menu.AddOption(Instance.Localizer.ForPlayer(jbPlayer.Controller, "select_special_day<option>"), (p, o) =>
        {
            if (jbPlayer.IsWarden)
                SpecialDaysMenu.Display(jbPlayer, menu);
        });

        MenuManager.OpenMainMenu(jbPlayer.Controller, menu);
    }
}
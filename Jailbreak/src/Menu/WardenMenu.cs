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

        menu.AddOption(Instance.Localizer.ForPlayer(jbPlayer.Controller, "give_freeday<option>"), (p, o) =>
        {
            if (jbPlayer.IsWarden)
            {
                List<CCSPlayerController> validPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.IsValid).ToList();

                OpenGiveFreedayMenu(jbPlayer, menu, validPlayers);
            }
        });

        menu.AddOption(Instance.Localizer.ForPlayer(jbPlayer.Controller, "remove_freeday<option>"), (p, o) =>
        {
            if (jbPlayer.IsWarden)
            {
                List<CCSPlayerController> validPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist && p.IsValid).ToList();

                OpenRemoveFreedayMenu(jbPlayer, menu, validPlayers);
            }
        });

        MenuManager.OpenMainMenu(jbPlayer.Controller, menu);
    }
    private static void OpenGiveFreedayMenu(JBPlayer jbPlayer, IT3Menu parentMenu, List<CCSPlayerController> validPlayers)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer["give_freeday_menu<title>"]);
        menu.IsSubMenu = true;
        menu.ParentMenu = parentMenu;


        foreach (var prisonerController in validPlayers)
        {
            JBPlayer prisonerJb = JBPlayerManagement.GetOrCreate(prisonerController);

            if (prisonerJb.IsFreeday)
                continue;

            menu.AddOption(prisonerJb.PlayerName, (p, o) =>
            {
                if (jbPlayer.IsWarden)
                {
                    Server.NextFrame(() =>
                    {
                        prisonerJb.SetFreeday(true);
                    });

                    Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["freeday_gave", prisonerJb.PlayerName]);
                    menu.Close(jbPlayer.Controller);
                }
            });
        }

        MenuManager.OpenSubMenu(jbPlayer.Controller, menu);
    }
    private static void OpenRemoveFreedayMenu(JBPlayer jbPlayer, IT3Menu parentMenu, List<CCSPlayerController> validPlayers)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer["remove_freeday_menu<title>"]);
        menu.IsSubMenu = true;
        menu.ParentMenu = parentMenu;


        foreach (var prisonerController in validPlayers)
        {
            JBPlayer prisonerJb = JBPlayerManagement.GetOrCreate(prisonerController);

            if (!prisonerJb.IsFreeday)
                continue;


            menu.AddOption(prisonerJb.PlayerName, (p, o) =>
            {
                if (jbPlayer.IsWarden)
                {
                    prisonerJb.SetFreeday(false);

                    Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["freeday_removed", prisonerJb.PlayerName]);
                    menu.Close(jbPlayer.Controller);
                }
            });
        }

        MenuManager.OpenSubMenu(jbPlayer.Controller, menu);
    }
}
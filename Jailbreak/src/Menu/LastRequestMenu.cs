using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using JailbreakApi;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;

namespace Jailbreak;

public static class LastRequestMenu
{
    public static void Display(JBPlayer jbPrisoner)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPrisoner.Controller, "last_request_menu<title>"));
        menu.FreezePlayer = false;

        foreach (var request in LastRequestManagement.GetRequests())
        {
            menu.AddOption(request.Name, (p, o) =>
            {
                OpenGuardianMenu(jbPrisoner, request, menu);
            });
        }
        MenuManager.OpenMainMenu(jbPrisoner.Controller, menu);
    }
    private static void OpenGuardianMenu(JBPlayer jbPrisoner, ILastRequest request, IT3Menu parentMenu)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPrisoner.Controller, "last_request_guardian_menu<title>"));
        menu.FreezePlayer = false;
        menu.IsSubMenu = true;
        menu.ParentMenu = parentMenu;

        List<CCSPlayerController> guardians = Utilities.GetPlayers().Where(g => g.IsValid && g.Team == CsTeam.CounterTerrorist && g.PawnIsAlive).ToList();

        if (!guardians.Any())
        {
            menu.AddTextOption(Instance.Localizer.ForPlayer(jbPrisoner.Controller, "last_request_no_guardians<option>"));
            return;
        }

        foreach (var g in guardians)
        {
            menu.AddOption(g.PlayerName, (p, o) =>
            {
                JBPlayer jbGuardian = JBPlayerManagement.GetOrCreate(g);
                if (request.GetAvalibleTypes != null && request.GetAvalibleTypes().Any())
                {
                    OpenTypeMenu(jbPrisoner, jbGuardian, request, menu);
                }
                else
                {
                    OpenWeaponsMenu(jbPrisoner, jbGuardian, request, menu);
                }
            });
        }
        MenuManager.OpenSubMenu(jbPrisoner.Controller, menu);
    }
    private static void OpenTypeMenu(JBPlayer jbPrisoner, JBPlayer jbGuardian, ILastRequest request, IT3Menu parentMenu)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPrisoner.Controller, "last_request_type_menu<title>"));
        menu.FreezePlayer = false;
        menu.IsSubMenu = true;
        menu.ParentMenu = parentMenu;

        foreach (var type in request.GetAvalibleTypes())
        {
            menu.AddOption(type, (p, o) =>
            {
                request.SelectedType = type;
                OpenWeaponsMenu(jbPrisoner, jbGuardian, request, menu);
            });
        }

        MenuManager.OpenSubMenu(jbPrisoner.Controller, menu);
    }
    private static void OpenWeaponsMenu(JBPlayer jbPrisoner, JBPlayer jbGuardian, ILastRequest request, IT3Menu parentMenu)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPrisoner.Controller, "last_request_weapons_menu<title>"));
        menu.FreezePlayer = false;
        menu.IsSubMenu = true;
        menu.ParentMenu = parentMenu;

        IReadOnlyList<string> avalibleWeapons = request.GetAvalibleWeapons();

        foreach (var weapon in avalibleWeapons)
        {
            menu.AddOption(weapon, (p, o) =>
            {
                request.SelectedWeapon =  weapon;
                LastRequestManagement.SelectRequest(request, jbPrisoner.Controller, jbGuardian.Controller, weapon);
                menu.Close(p);
            });
        }

        MenuManager.OpenSubMenu(jbPrisoner.Controller, menu);
    }
}
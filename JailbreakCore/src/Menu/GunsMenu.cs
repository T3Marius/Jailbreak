using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class GunsMenu
{
    private static readonly Dictionary<string, string> GlobalRifles = new()
    {
        { "AK-47", "weapon_ak47" },
        { "AWP", "weapon_awp" },
        { "M4A4", "weapon_m4a1" },
        { "M4A1-S", "weapon_m4a1_silencer" },
        { "SG 553", "weapon_sg553" },
        { "AUG", "weapon_aug" },
        { "SSG 08", "weapon_ssg08" },
        { "Negev", "weapon_negev" },
        { "M249", "weapon_m249" },
        { "FAMAS", "weapon_famas" },
        { "Galil AR", "weapon_galilar" },
        { "MP5-SD", "weapon_mp5sd" },
        { "PP-Bizon", "weapon_bizon" },
        { "UMP-45", "weapon_ump45" },
        { "MP9", "weapon_mp9" },
        { "P90", "weapon_p90" },
        { "MP7", "weapon_mp7" },
        { "MAC-10", "weapon_mac10"},
        { "SG556", "weapon_sg556"},
        { "G3SG1",  "weapon_g3sg1"},
        { "SCAR-20", "weapon_scar20" },
        { "XM1014", "weapon_xm1014"},
        { "MAG-7", "weapon_mag7"},
        { "Sawed-Off", "weapon_sawedoff"},
        { "Nova", "weapon_nova"},
    };
    private static readonly Dictionary<string, string> GlobalPistols = new()
    {
        { "Desert Eagle", "weapon_deagle" },
        { "Glock-18", "weapon_glock" },
        { "P2000", "weapon_hkp2000" },
        { "USP-S", "weapon_usp_silencer" },
        { "TEC-9", "weapon_tec9" },
        { "P250", "weapon_p250" },
        { "CZ75-Auto", "weapon_cz75a" },
        { "Dual Berettas", "weapon_elite" },
        { "Five-SeveN", "weapon_fiveseven" },
        { "R8 Revolver", "weapon_revolver" },
    };
    public static void Display(JBPlayer jbPlayer)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPlayer.Controller, "guns_menu<title>"));
        menu.FreezePlayer = false;

        foreach (var kvp in GlobalRifles)
        {
            string Rifle = kvp.Key; // WeaponName
            string ID = kvp.Value; // weapon_

            bool isEnabled = !Instance.Config.GunsMenu.ExcludeWeapons.Contains(ID);

            menu.AddOption(Rifle, (p, o) =>
            {
                DisplayPistolMenu(jbPlayer, menu, ID, Rifle);
            }, !isEnabled);
        }

        MenuManager.OpenMainMenu(jbPlayer.Controller, menu);
    }
    private static void DisplayPistolMenu(JBPlayer jbPlayer, IT3Menu parentMenu, string rifleId, string rifleName)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPlayer.Controller, "guns_menu_pistol<title>"));
        menu.IsSubMenu = true;
        menu.ParentMenu = parentMenu;
        menu.FreezePlayer = false;

        foreach (var kvp in GlobalPistols)
        {
            string Pistol = kvp.Key; // WeaponName
            string ID = kvp.Value; // weapon_

            bool isEnabled = !Instance.Config.GunsMenu.ExcludeWeapons.Contains(ID);

            menu.AddOption(Pistol, (p, o) =>
            {
                p.RemoveWeapons();

                Server.NextFrame(() =>
                {
                    p.GiveNamedItem(ID);
                    p.GiveNamedItem(rifleId);
                    p.GiveNamedItem(CsItem.Knife);
                    menu.Close(p);
                });

            }, !isEnabled);
        }

        MenuManager.OpenSubMenu(jbPlayer.Controller, menu);
    }
}
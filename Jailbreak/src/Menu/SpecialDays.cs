using CounterStrikeSharp.API.Core.Translations;
using JailbreakApi;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class SpecialDaysMenu
{
    public static void Display(JBPlayer jbPlayer, IT3Menu? parentMenu = null)
    {
        IT3Menu menu = MenuManager.CreateMenu(Instance.Localizer.ForPlayer(jbPlayer.Controller, "special_days_menu<title>"));
        menu.FreezePlayer = false;

        if (parentMenu != null)
        {
            menu.IsSubMenu = true;
            menu.ParentMenu = parentMenu;
        }

        IReadOnlyList<ISpecialDay> Days = SpecialDayManagement.GetDays();

        foreach (var day in Days)
        {
            menu.AddOption(day.Name, (p, o) =>
            {
                SpecialDayManagement.SelectDay(jbPlayer, day.Name);
            });
        }

        if (parentMenu == null)
            MenuManager.OpenMainMenu(jbPlayer.Controller, menu);
        else
            MenuManager.OpenSubMenu(jbPlayer.Controller, menu);
    }
}
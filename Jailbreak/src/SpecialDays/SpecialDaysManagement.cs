using CounterStrikeSharp.API;
using static Jailbreak.Jailbreak;
using Microsoft.Extensions.Logging;
using JailbreakApi;

namespace Jailbreak;

public static class SpecialDayManagement
{
    private static readonly List<ISpecialDay> Days = new();
    private static ISpecialDay? ActiveDay;
    private static ISpecialDay? PendingDay;
    private static int CooldownInRounds = Instance.Config.DaysConfig.CooldownInRounds;
    public static IReadOnlyList<ISpecialDay> GetDays() => Days;
    public static ISpecialDay? GetActiveDay() => ActiveDay;

    public static void RegisterDay(ISpecialDay day)
    {
        Days.Add(day);
    }
    public static void SelectDay(JBPlayer jbPlayer, string name)
    {
        if (CooldownInRounds > 0)
        {
            jbPlayer.Print("chat", Instance.Localizer["prefix"] + Instance.Localizer["day_on_cooldown", CooldownInRounds]);
            return;
        }

        PendingDay = Days.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (PendingDay != null)
            Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["special_day_pending", jbPlayer.PlayerName, PendingDay.Name]);
    }
    public static void OnRoundStart()
    {
        if (CooldownInRounds > 0)
            CooldownInRounds--;

        if (PendingDay != null)
        {
            ActiveDay = PendingDay;
            PendingDay = null;

            ActiveDay.Start();
            Library.PrintToChatAll(ActiveDay.Description);

            CooldownInRounds = Instance.Config.DaysConfig.CooldownInRounds;
        }
    }
    public static void OnRoundEnd()
    {
        ActiveDay?.End();
        ActiveDay = null;
    }
    public static void EndDay()
    {
        ActiveDay?.End();
        ActiveDay = null;
    }
}
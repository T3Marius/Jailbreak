using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public static class Library
{
    private static Random random = new Random();
    public static readonly Dictionary<CCSPlayerController, string> GlobalHtmlMessages = new();
    public static void AssignRandomWarden()
    {
        // for now, i won't exclude bots as i need it for testing.
        List<CCSPlayerController> validPlayers = Utilities.GetPlayers().Where(p => p.Team == CsTeam.CounterTerrorist && p.PawnIsAlive).ToList();

        if (validPlayers.Count == 0)
            return;

        CCSPlayerController randomPlayer = validPlayers[random.Next(validPlayers.Count)];
        // one last check before assing warden.
        if (randomPlayer != null && randomPlayer.PawnIsAlive && randomPlayer.Team == CsTeam.CounterTerrorist)
        {
            JBPlayer randomJbPlayer = JBPlayerManagement.GetOrCreate(randomPlayer);
            randomJbPlayer.SetWarden(true);

            randomJbPlayer.Print("chat", Instance.Localizer["prefix"] + Instance.Localizer["warden_take"]);
        }

    }
    public static void PrintToCenterAll(string message)
    {
        VirtualFunctions.ClientPrintAll(HudDestination.Center, message, 0, 0, 0, 0);
    }
    public static void PrintToAlertAll(string message)
    {
        VirtualFunctions.ClientPrintAll(HudDestination.Alert, message, 0, 0, 0, 0);
    }
    public static void PrintToHtmlAll(string message, int duration)
    {
        foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
        {
            player.PrintToHtml(message, duration);
        }
    }
    public static void PrintToHtml(this CCSPlayerController player, string message, float duration)
    {
        if (GlobalHtmlMessages.ContainsKey(player))
            GlobalHtmlMessages.Remove(player);

        GlobalHtmlMessages.Add(player, message);
        Instance.AddTimer(duration, () =>
        {
            if (GlobalHtmlMessages.ContainsKey(player))
                GlobalHtmlMessages.Remove(player);
        });
    }
    public static void StartBox(string callerName = "")
    {
        ConVar.Find("mp_teammates_are_enemies")?.SetValue(true);

        Events.g_IsBoxActive = true;
        Server.ExecuteCommand("sv_teamid_overhead 0");

        Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["box_started", callerName]);
    }
    public static void StopBox(string callerName = "")
    {
        ConVar.Find("mp_teammates_are_enemies")?.SetValue(false);

        Events.g_IsBoxActive = false;
        Server.ExecuteCommand("sv_teamid_overhead 1");

        Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["box_stopped", callerName]);
    }
}
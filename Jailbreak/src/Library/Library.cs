using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Timers;

namespace Jailbreak;

public static class Library
{
    private static Random random = new Random();
    public static readonly Dictionary<CCSPlayerController, string> GlobalHtmlMessages = new();
    public static readonly HashSet<CCSPlayerController> GlobalFrozenPlayers = new();
    public static readonly Dictionary<CCSPlayerController, float> PlayerSavedSpeed = new();
    public static CSTimer StartTimer(int seconds, Action<int> onTick, Action onFinished)
    {
        int remaining = seconds;

        CSTimer? timer = null;

        timer = Instance.AddTimer(1.0f, () =>
        {
            remaining--;

            if (remaining > 0)
                onTick?.Invoke(remaining);
            else
            {
                onFinished?.Invoke();
                timer?.Kill();
            }
        }, TimerFlags.REPEAT);

        return timer;
    }
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
    public static void SetGravity(this CCSPlayerController player, float value)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.ActualGravityScale = value;
        //Utilities.SetStateChanged(pawn, "CBaseEntity", "m_flActualGravityScale");
    }
    public static void SetSpeed(this CCSPlayerController player, float value)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.VelocityModifier = value;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }
    public static void SetHealth(this CCSPlayerController player, int value)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        pawn.MaxHealth = value;
        pawn.Health = value;

        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
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
    public static void Freeze(this CCSPlayerController player)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        if (!PlayerSavedSpeed.ContainsKey(player))
            PlayerSavedSpeed.Add(player, pawn.VelocityModifier);

        if (!GlobalFrozenPlayers.Contains(player))
            GlobalFrozenPlayers.Add(player);
    }
    public static void Unfreeze(this CCSPlayerController player)
    {
        CCSPlayerPawn? pawn = player.PlayerPawn.Value;
        if (pawn == null)
            return;

        if (GlobalFrozenPlayers.Contains(player))
            GlobalFrozenPlayers.Remove(player);

        if (PlayerSavedSpeed.TryGetValue(player, out float savedSpeed))
        {
            pawn.VelocityModifier = savedSpeed;
            PlayerSavedSpeed.Remove(player);
        }

    }
    public static void UpdateFrozenPlayers()
    {
        foreach (var player in GlobalFrozenPlayers)
        {
            CCSPlayerPawn? pawn = player.PlayerPawn.Value;

            if (pawn != null)
                pawn.VelocityModifier = 0.0f;
        }
    }
}
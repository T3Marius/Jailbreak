using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using JailbreakApi;

namespace Jailbreak;

public class TeleportDay : ISpecialDay
{
    public string Name => Instance.Localizer["teleport_day<name>"];
    public string Description => Instance.Localizer["teleport_day<description>"];

    public bool g_IsTimerActive = false;
    private int DelayCooldown = 10;
    public void Start()
    {
        foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
        {
            JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
            GunsMenu.Display(jbPlayer);
        }

        Library.StartTimer(DelayCooldown,
            remaining =>
            {
                DelayCooldown--;

                g_IsTimerActive = true;
                foreach (var controller in Utilities.GetPlayers().Where(p => !p.IsBot))
                {
                    JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
                    jbPlayer.Print("html", Instance.Localizer["day_starting_html", Name, DelayCooldown], 1);
                }
            },
            () =>
            {
                g_IsTimerActive = false;
            });


        Instance.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        ConVar.Find("mp_teammates_are_enemies")?.SetValue(true);
        Server.ExecuteCommand("sv_teamid_overhead 0");
    }
    public HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victim = @event.Userid;
        CCSPlayerController? attacker = @event.Attacker;

        if (attacker == null || victim == null || attacker == victim)
            return HookResult.Continue;

        CCSPlayerPawn? attackerPawn = attacker.PlayerPawn.Value;
        if (attackerPawn == null || attackerPawn.AbsOrigin == null)
            return HookResult.Continue;

        Vector? savedAttackerPos = new Vector(attackerPawn.AbsOrigin.X, attackerPawn.AbsOrigin.Y, attackerPawn.AbsOrigin.Z); // save the attacker pos before
        Vector? victimPos = victim.PlayerPawn.Value?.AbsOrigin;

        if (victimPos == null || attackerPawn == null)
            return HookResult.Continue;

        attacker.PlayerPawn.Value?.Teleport(victimPos, new QAngle(), new Vector());
        victim.PlayerPawn.Value?.Teleport(savedAttackerPos, new QAngle(), new Vector());


        return HookResult.Continue;
    }
    public HookResult OnTakeDamage(DynamicHook hook)
    {
        CTakeDamageInfo info = hook.GetParam<CTakeDamageInfo>(1);

        if (g_IsTimerActive) // disable all damage when timer is alive
            return HookResult.Handled;

        return HookResult.Continue;
    }
    public void End()
    {
        Instance.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        ConVar.Find("mp_teammates_are_enemies")?.SetValue(false);
        Server.ExecuteCommand("sv_teamid_overhead 1");
    }
}
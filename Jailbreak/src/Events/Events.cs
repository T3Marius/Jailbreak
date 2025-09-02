using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Core;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Cvars;

namespace Jailbreak;

public static class Events
{
    public static bool g_IsBoxActive = false;
    public static void RegisterVirtualFunctions()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }
    public static void RegisterEventsHandlers()
    {
        Instance.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Instance.RegisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
        Instance.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.RegisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
    }
    public static void RegisterListeners()
    {
        Instance.RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RegisterListener<OnTick>(OnTick);
    }
    private static HookResult OnTakeDamage(DynamicHook hook)
    {
        CEntityInstance entity = hook.GetParam<CEntityInstance>(0);
        CTakeDamageInfo info = hook.GetParam<CTakeDamageInfo>(1);

        var ability = info.Ability.Value;
        if (ability == null)
            return HookResult.Continue;

        if (entity.DesignerName != "player")
            return HookResult.Continue;

        var attacker = new CCSPlayerController(ability.Handle);
        var victim = new CCSPlayerController(entity.Handle);

        if (g_IsBoxActive && attacker.Team == victim.Team && victim.Team != CsTeam.Terrorist)
            return HookResult.Handled;

        return HookResult.Continue;
    }
    private static HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        CCSPlayerController? controller = @event.Userid;
        if (controller == null)
            return HookResult.Continue;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);

        jbPlayer.OnChangeTeam((CsTeam)@event.Team);

        return HookResult.Continue;
    }
    private static HookResult OnPlayerSpawned(EventPlayerSpawned @event, GameEventInfo info)
    {
        CCSPlayerController? controller = @event.Userid;
        if (controller == null)
            return HookResult.Continue;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);
        jbPlayer.OnPlayerSpawn();

        return HookResult.Continue;

    }
    private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? controller = @event.Userid;
        if (controller == null)
            return HookResult.Continue;

        JBPlayerManagement.Remove(controller);
        if (Library.GlobalHtmlMessages.ContainsKey(controller))
            Library.GlobalHtmlMessages.Remove(controller);

        return HookResult.Continue;
    }
    private static HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? victimController = @event.Userid;
        CCSPlayerController? attackerController = @event.Attacker;

        if (victimController == null || attackerController == null)
            return HookResult.Continue;

        JBPlayer victim = JBPlayerManagement.GetOrCreate(victimController);
        JBPlayer attacker = JBPlayerManagement.GetOrCreate(attackerController);

        if (SpecialDayManagement.GetActiveDay() != null)
            return HookResult.Continue;

        if (victim == attacker)
        {
            if (victim.IsWarden)
            {
                victim.SetWarden(false);

                Library.PrintToAlertAll(Instance.Localizer["warden_died", Instance.Config.Warden.Commands.TakeWarden.FirstOrDefault()!]);

                if (!string.IsNullOrEmpty(Instance.Config.Warden.WardenRemovedSound))
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        RecipientFilter filter = [player];
                        player.EmitSound(Instance.Config.Warden.WardenRemovedSound, filter, Instance.Config.GlobalVolume.WardenRemovedVolume);
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
            return HookResult.Continue;
        }

        if (victim.IsWarden)
        {
            victim.SetWarden(false);
            attacker.SetRebel(true);

            Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["warden_killed_by", attacker.PlayerName]);
            Library.PrintToAlertAll(Instance.Localizer["warden_died", Instance.Config.Warden.Commands.TakeWarden.FirstOrDefault()!]);

            if (!string.IsNullOrEmpty(Instance.Config.Warden.WardenKilledSound))
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    RecipientFilter filter = [player];
                    player.EmitSound(Instance.Config.Warden.WardenKilledSound, filter, Instance.Config.GlobalVolume.WardenKilledVolume);
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

        return HookResult.Continue;
    }
    private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        JBPlayer? currentWarden = JBPlayerManagement.GetWarden();
        List<JBPlayer> currentRebels = JBPlayerManagement.GetAllRebels();
        List<JBPlayer> currentFreedays = JBPlayerManagement.GetAllFreedays();

        if (currentWarden != null)
            currentWarden.SetWarden(false);

        foreach (var rebel in currentRebels)
            rebel.SetRebel(false);

        foreach (var freeday in currentFreedays)
            freeday.SetFreeday(false);

        g_IsBoxActive = false;
        ConVar.Find("mp_teammates_are_enemies")?.SetValue(false);

        SpecialDayManagement.OnRoundStart();

        if (SpecialDayManagement.GetActiveDay() != null)
            return HookResult.Continue;

        Instance.AddTimer(5.0f, () =>
        {
            if (JBPlayerManagement.GetWarden() == null)
            {
                Library.AssignRandomWarden();
                Library.PrintToCenterAll(Instance.Localizer["warden_take_alert", JBPlayerManagement.GetWarden()?.PlayerName ?? ""]);
            }
        });

        return HookResult.Continue;
    }
    private static HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        SpecialDayManagement.OnRoundEnd();

        return HookResult.Continue;
    }
    private static HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? victimController = @event.Userid;
        CCSPlayerController? attackerController = @event.Attacker;

        if (victimController == null || attackerController == null)
            return HookResult.Continue;

        if (victimController == attackerController)
            return HookResult.Continue;

        if (attackerController.Team == CsTeam.CounterTerrorist)
            return HookResult.Continue;

        if (SpecialDayManagement.GetActiveDay() != null)
            return HookResult.Continue;


        JBPlayer attacker = JBPlayerManagement.GetOrCreate(attackerController);
        if (!attacker.IsRebel)
        {
            attacker.SetRebel(true);

            Library.PrintToAlertAll(Instance.Localizer["became_rebel", attacker.PlayerName]);

            // maybe play a rebel sound here?
        }
        return HookResult.Continue;
    }
    private static void OnServerPrecacheResources(ResourceManifest resource)
    {
        if (!string.IsNullOrEmpty(Instance.Config.Models.WardenModel))
            resource.AddResource(Instance.Config.Models.WardenModel);

        if (!string.IsNullOrEmpty(Instance.Config.Models.GuardianModel))
            resource.AddResource(Instance.Config.Models.GuardianModel);

        if (!string.IsNullOrEmpty(Instance.Config.Models.PrisonerModel))
            resource.AddResource(Instance.Config.Models.PrisonerModel);
    }
    private static void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (Library.GlobalHtmlMessages.TryGetValue(player, out string? message))
            {
                player.PrintToCenterHtml(message);
            }
            else
            {
                // do nothing
            }
        }
    }
    public static void OnPlayerRoleChanged(JBPlayer jbPlayer, JBRole role)
    {
        //Instance.Logger.LogInformation("{0} role was changed to {1}", jbPlayer.PlayerName, role); this works
    }
    public static void Dispose()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        Instance.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Instance.DeregisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
        Instance.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

        Instance.RemoveListener<OnTick>(OnTick);
        Instance.RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    }

}
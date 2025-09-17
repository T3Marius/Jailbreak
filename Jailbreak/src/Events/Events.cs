using static CounterStrikeSharp.API.Core.Listeners;
using CounterStrikeSharp.API.Core;
using static Jailbreak.Jailbreak;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

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
        Instance.RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        Instance.RegisterEventHandler<EventPlayerPing>(OnPlayerPing);
    }
    public static void RegisterListeners()
    {
        Instance.RegisterListener<OnServerPrecacheResources>(OnServerPrecacheResources);
        Instance.RegisterListener<OnTick>(OnTick);
    }
    private static HookResult OnTakeDamage(DynamicHook hook)
    {
        var victimHandle = hook.GetParam<CBaseEntity>(0);
        if (victimHandle.DesignerName != "player")
            return HookResult.Continue;

        var info = hook.GetParam<CTakeDamageInfo>(1);
        var attackerHandle = info.Attacker;
        if (attackerHandle.Value == null || !attackerHandle.IsValid || attackerHandle.Value.DesignerName != "player")
            return HookResult.Continue;

       

        var attacker = attackerHandle.Value.As<CCSPlayerPawn>();
        var attackerController = attacker.OriginalController.Value;
        if (attackerController == null)
        {
            Instance.Logger.LogError("Attacker controller is null");
            return HookResult.Continue;
        }

        var victim = victimHandle.As<CCSPlayerPawn>();
        var victimController = victim.OriginalController.Value;
        if (victimController == null)
        {
            Instance.Logger.LogError("Victim controller is null");
            return HookResult.Continue;
        }

        LastRequestManagement.OnTakeDamage(info, attackerController, victimController);

        if (g_IsBoxActive && attacker.TeamNum == victim.TeamNum && victim.TeamNum != (int)CsTeam.Terrorist)
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

        if (Library.GlobalFrozenPlayers.Contains(controller))
            Library.GlobalFrozenPlayers.Remove(controller);

        if (Library.PlayerSavedSpeed.ContainsKey(controller))
            Library.PlayerSavedSpeed.Remove(controller);

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

        LastRequestManagement.OnPlayerDeath(victimController);

        if (SpecialDayManagement.GetActiveDay() != null) // we don't need to do anything while a special day is active
            return HookResult.Continue;

        if (LastRequestManagement.GetActiveRequest() != null)
            return HookResult.Continue;

        if (victim == attacker)
        {
            if (victim.IsWarden)
            {
                victim.SetWarden(false);

                Library.PrintToAlertAll(Instance.Localizer["warden_died", Instance.Config.Warden.Commands.TakeWarden.FirstOrDefault()!]);

                if (!string.IsNullOrEmpty(Instance.Config.Sounds.WardenRemovedSound))
                {
                    foreach (var player in Utilities.GetPlayers())
                    {
                        RecipientFilter filter = [player];
                        player.EmitSound(Instance.Config.Sounds.WardenRemovedSound, filter, Instance.Config.GlobalVolume.WardenRemovedVolume);
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

            if (!string.IsNullOrEmpty(Instance.Config.Sounds.WardenKilledSound))
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    RecipientFilter filter = [player];
                    player.EmitSound(Instance.Config.Sounds.WardenKilledSound, filter, Instance.Config.GlobalVolume.WardenKilledVolume);
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

        // here we just announce that rebel x was killed by guard y
        if (victim.IsRebel)
        {
            victim.SetRebel(false);

            Server.PrintToChatAll(Instance.Localizer["prefix"] + Instance.Localizer["rebel_killed_by", victim.PlayerName, attacker.PlayerName]);
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
        PrisonerCommands.SurrenderTries.Clear();
        LastRequestManagement.EndRequest(null, null);

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
        LastRequestManagement.EndRequest(null, null);

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

        if (LastRequestManagement.GetActiveRequest() != null)
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
    private static HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        CCSPlayerController? fireController = @event.Userid;
        if (fireController == null)
            return HookResult.Continue;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(fireController);

        if (SpecialDayManagement.GetActiveDay() != null)
            return HookResult.Continue; // ignore when special day is active

        if (LastRequestManagement.GetActiveRequest() != null)
            return HookResult.Continue;

        if (@event.Weapon.Contains("knife")) // ignore when he fires knife.
            return HookResult.Continue;

        if (jbPlayer.Role == JBRole.Prisoner && !jbPlayer.IsRebel) // only set rebel if he's prisoner and is not aleardy rebel.
        {
            // set prisoner rebel, as he used an weapon on a normal day.
            jbPlayer.SetRebel(true);
            Library.PrintToAlertAll(Instance.Localizer["became_rebel", jbPlayer.PlayerName]);
        }

        return HookResult.Continue;
    }
    private static HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        CCSPlayerController? controller = @event.Userid;
        if (controller == null)
            return HookResult.Continue;

        JBPlayer jbPlayer = JBPlayerManagement.GetOrCreate(controller);

        if (!jbPlayer.IsWarden)
            return HookResult.Continue;

        Vector? pos = new Vector(@event.X, @event.Y, @event.Z);
        if (pos == null)
            return HookResult.Continue;

        Beams.DrawPingBeacon(pos);

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

        foreach (var file in Instance.Config.Sounds.SoundEventFiles)
            resource.AddResource(file);
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

        Library.UpdateFrozenPlayers();
        List<JBPlayer> jbPlayers = JBPlayerManagement.GetAllPlayers();
        foreach (var warden in jbPlayers.Where(j => j.IsWarden))
        {
            Beams.UpdateWardenLaser(warden, warden.Controller.Buttons);
        }

    }
    public static void OnPlayerRoleChanged(JBPlayer jbPlayer, JBRole role)
    {
        //Instance.Logger.LogInformation("{0} role was changed to {1}", jbPlayer.PlayerName, role); this works.
    }
    public static void Dispose()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        Instance.DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
        Instance.DeregisterEventHandler<EventPlayerSpawned>(OnPlayerSpawned);
        Instance.DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        Instance.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        Instance.DeregisterEventHandler<EventRoundStart>(OnRoundStart);
        Instance.DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
        Instance.DeregisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        Instance.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire);

        Instance.RemoveListener<OnTick>(OnTick);
        Instance.RemoveListener<OnServerPrecacheResources>(OnServerPrecacheResources);
    }

}
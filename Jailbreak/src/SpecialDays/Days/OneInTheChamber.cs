using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using JailbreakApi;
using Microsoft.Extensions.Logging;
using System.Data;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public class OneInTheChamberDay : ISpecialDay
{
    public string Name => Instance.Localizer["one_in_the_chamber_day<name>"];
    public string Description => Instance.Localizer["one_in_the_chamber_day<description>"];

    public bool g_IsTimerActive = false;
    public int DelayCooldown = 10;
    public List<ushort> AllowedOITCWeaponsDefIndex = [(ushort)ItemDefinition.KNIFE_T, (ushort)ItemDefinition.KARAMBIT, (ushort)ItemDefinition.GUT_KNIFE,
    (ushort)ItemDefinition.FLIP_KNIFE, (ushort)ItemDefinition.BOWIE_KNIFE, (ushort)ItemDefinition.NOMAD_KNIFE, (ushort)ItemDefinition.TALON_KNIFE,
    (ushort)ItemDefinition.URSUS_KNIFE, (ushort)ItemDefinition.NAVAJA_KNIFE, (ushort)ItemDefinition.CLASSIC_KNIFE, (ushort)ItemDefinition.FALCHION_KNIFE,
    (ushort)ItemDefinition.HUNTSMAN_KNIFE, (ushort)ItemDefinition.PARACORD_KNIFE, (ushort)ItemDefinition.SKELETON_KNIFE, (ushort)ItemDefinition.STILETTO_KNIFE,
    (ushort)ItemDefinition.SURVIVAL_KNIFE, (ushort)ItemDefinition.BUTTERFLY_KNIFE, (ushort)ItemDefinition.DESERT_EAGLE]; // a better way to do this?

    public void Start()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            player.RemoveWeapons();

            Server.NextFrame(() => 
            {
                player.GiveNamedItem(CsItem.Knife);
                player.GiveNamedItem(CsItem.DesertEagle);
                player.SetAmmo(1);
                player.SetReserve(0);
            });
        }
        Library.StartTimer(DelayCooldown,
            remaining =>
            {
                g_IsTimerActive = true;
                DelayCooldown--;

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

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

        Instance.RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

        ConVar.Find("mp_teammates_are_enemies")?.SetValue(true);
        Server.ExecuteCommand("sv_teamid_overhead 0");

    }
    public HookResult OnCanAcquireFunc(DynamicHook hook)
    {
        var econItem = hook.GetParam<CEconItemView>(1);
        ushort defIndex = (ushort)econItem.ItemDefinitionIndex;

        if (!AllowedOITCWeaponsDefIndex.Contains(defIndex))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Handled;
        }
        return HookResult.Continue;
    }
    private HookResult OnTakeDamage(DynamicHook arg)
    {
        var victim = arg.GetParam<CBaseEntity>(0);
        if (victim.DesignerName != "player")
            return HookResult.Continue;

        var info = arg.GetParam<CTakeDamageInfo>(1);
        var attackerHandle = info.Attacker;
        if (attackerHandle.Value == null || !attackerHandle.IsValid || attackerHandle.Value.DesignerName != "player")
            return HookResult.Continue;

        var attacker = attackerHandle.Value.As<CCSPlayerPawn>();
        var controller = attacker.Controller.Value;
        if (controller == null)
            return HookResult.Continue;

        if (g_IsTimerActive)
            return HookResult.Continue;

        CBasePlayerWeapon? activeWeapon = attacker.GetActiveWeapon();

        if (activeWeapon != null && activeWeapon.DesignerName == "weapon_deagle")
        {
            info.Damage = 1000;

            Server.NextFrame(() =>
            {
                activeWeapon.Clip1 += 1;
                Utilities.SetStateChanged(activeWeapon, "CBasePlayerWeapon", "m_iClip1");
            });
        }

        return HookResult.Continue;
    }
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        CCSPlayerController? attacker = @event.Attacker;
        if (attacker == null)
            return HookResult.Continue;

        CCSPlayerPawn? pawn = attacker.PlayerPawn.Value;
        if (pawn == null)
            return HookResult.Continue;

        if (@event.Weapon == "deagle")
            return HookResult.Continue;


        var weapons = pawn.WeaponServices?.MyWeapons;
        if (weapons == null)
            return HookResult.Continue;

        foreach (var handle in weapons)
        {
            var weapon = handle.Value;
            if (weapon == null)
                continue;

            Server.NextFrame(() =>
            {
                weapon.Clip1 += 1;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            });
            break;
        }

        return HookResult.Continue;
    }
    public void End()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquireFunc, HookMode.Pre);

        Instance.DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

        ConVar.Find("mp_teammates_are_enemies")?.SetValue(false);
        Server.ExecuteCommand("sv_teamid_overhead 1");
    }
}

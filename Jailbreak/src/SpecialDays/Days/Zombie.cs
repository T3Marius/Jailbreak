using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using JailbreakApi;
using Microsoft.Extensions.Logging;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public class ZombieDay : ISpecialDay
{
    public string Name => Instance.Localizer["zombie_day<name>"];
    public string Description => Instance.Localizer["zombie_day<description>", Instance.Config.DaysConfig.ZombieDayConfig.ZombiesHealth];

    public int PrepareTime = Instance.Config.DaysConfig.ZombieDayConfig.PrepareTimeInSeconds;
    public string ZombieModel => Instance.Config.DaysConfig.ZombieDayConfig.ZombiesModel;

    public List<ushort> AllowedZombieWeaponsDefIndex = [(ushort)ItemDefinition.KNIFE_T, (ushort)ItemDefinition.KARAMBIT, (ushort)ItemDefinition.GUT_KNIFE,
    (ushort)ItemDefinition.FLIP_KNIFE, (ushort)ItemDefinition.BOWIE_KNIFE, (ushort)ItemDefinition.NOMAD_KNIFE, (ushort)ItemDefinition.TALON_KNIFE,
    (ushort)ItemDefinition.URSUS_KNIFE, (ushort)ItemDefinition.NAVAJA_KNIFE, (ushort)ItemDefinition.CLASSIC_KNIFE, (ushort)ItemDefinition.FALCHION_KNIFE,
    (ushort)ItemDefinition.HUNTSMAN_KNIFE, (ushort)ItemDefinition.PARACORD_KNIFE, (ushort)ItemDefinition.SKELETON_KNIFE, (ushort)ItemDefinition.STILETTO_KNIFE,
    (ushort)ItemDefinition.SURVIVAL_KNIFE, (ushort)ItemDefinition.BUTTERFLY_KNIFE]; // a better way to do this?

    public void Start()
    {
        foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
        {
            player.Freeze(); // freeze zombies for PrepareTime
            player.RemoveWeapons();
            player.TakesDamage = false; // enable god mode
            player.SetHealth(Instance.Config.DaysConfig.ZombieDayConfig.ZombiesHealth); // set zombie health

            Server.NextFrame(() => // call it on next frame because it might interact with prisoner model set.
            {
                if (!string.IsNullOrEmpty(ZombieModel))
                    player.PlayerPawn.Value?.SetModel(ZombieModel);

                player.GiveNamedItem(CsItem.KnifeT);
            });

        }

        Library.StartTimer(PrepareTime,
        remaining =>
        {
            PrepareTime--;

            Library.PrintToHtmlAll(Instance.Localizer["day_starting_html", Name, PrepareTime], 1);
        },

        () =>
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
            {
                player.Unfreeze(); // unfreeze zombies
                player.TakesDamage = true; // disable god mode
                player.SetSpeed(1.1f); // slightly faster than humans
            }
        });

        if (Instance.Config.DaysConfig.ZombieDayConfig.InfiniteReserve)
            Instance.RegisterEventHandler<EventWeaponReload>(OnWeaponReload); // apply infinite clip & only register the event if enable

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);
    }
    public HookResult OnWeaponReload(EventWeaponReload @event, GameEventInfo info)
    {
        CCSPlayerController? controller = @event.Userid;
        if (controller == null || controller.Team != CsTeam.CounterTerrorist)
            return HookResult.Continue;

        Instance.AddTimer(3.0f, () => controller.SetReserve(100));

        return HookResult.Continue;
    }
    public HookResult OnCanAcquireFunc(DynamicHook hook)
    {
        var econItem = hook.GetParam<CEconItemView>(1);
        ushort defIndex = (ushort)econItem.ItemDefinitionIndex;

        // how to do team check? how do i get player param?
        // for now, it works fine since if we get weapons from !guns we can use it.

        if (!AllowedZombieWeaponsDefIndex.Contains(defIndex))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }
    public void End()
    {
        if (Instance.Config.DaysConfig.ZombieDayConfig.InfiniteReserve)
            Instance.DeregisterEventHandler<EventWeaponReload>(OnWeaponReload);

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquireFunc, HookMode.Pre);
    }
}
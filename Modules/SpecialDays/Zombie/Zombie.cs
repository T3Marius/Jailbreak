using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using JailbreakApi;

namespace SpecialDays;

public class Zombie : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[SD] Zombie";
    public override string ModuleVersion => "1.0.0";

    public static Zombie Instance { get; set; } = new Zombie();
    public IJailbreakApi Api = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Instance = this;

        Api = IJailbreakApi.Capability.Get() ?? throw new Exception("Jailbreak Api not found!");

        if (Api.GetConfigValue("DaysConfig.ZombieRound", true))
            Api.RegisterDay(new ZombieDay());
    }
}

public class ZombieDay : ISpecialDay
{
    public Zombie Instance => Zombie.Instance;
    public IJailbreakApi Api => Instance.Api;

    public string Name => Api.GetLocalizer("zombie_day<name>");
    public string Description => Api.GetLocalizer("zombie_day<description>", Api.GetConfigValue("DaysConfig.ZombieDayConfig.ZombiesHealth", 5000));

    public int PrepareTime = Zombie.Instance.Api.GetConfigValue("DaysConfig.ZombieDayConfig.PrepareTimeInSeconds", 30);

    public string ZombieModel => Api.GetConfigValue("DaysConfig.ZombieDayConfig.ZombiesModel", "");

    public HashSet<ushort> AllowedZombieWeaponsDefIndex { get; } = new(GetAllowedWeapons());

    private static IEnumerable<ushort> GetAllowedWeapons()
    {
        var knives = new[]
        {
            ItemDefinition.KNIFE_T,
            ItemDefinition.KARAMBIT,
            ItemDefinition.GUT_KNIFE,
            ItemDefinition.FLIP_KNIFE,
            ItemDefinition.BOWIE_KNIFE,
            ItemDefinition.NOMAD_KNIFE,
            ItemDefinition.TALON_KNIFE,
            ItemDefinition.URSUS_KNIFE,
            ItemDefinition.NAVAJA_KNIFE,
            ItemDefinition.CLASSIC_KNIFE,
            ItemDefinition.FALCHION_KNIFE,
            ItemDefinition.HUNTSMAN_KNIFE,
            ItemDefinition.PARACORD_KNIFE,
            ItemDefinition.SKELETON_KNIFE,
            ItemDefinition.STILETTO_KNIFE,
            ItemDefinition.SURVIVAL_KNIFE,
            ItemDefinition.BUTTERFLY_KNIFE
        };
        foreach (var knife in knives)
            yield return (ushort)knife;
    }

    public void Start()
    {
        foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
        {
            Api.FreezePlayer(player); // freeze zombies for PrepareTime
            player.RemoveWeapons();
            player.TakesDamage = false; // enable god mode
            Api.SetHealth(player, Api.GetConfigValue("DaysConfig.ZombieDayConfig.ZombiesHealth", 5000)); // set zombie health

            Server.NextFrame(() => // call it on next frame because it might interact with prisoner model set.
            {
                if (!string.IsNullOrEmpty(ZombieModel))
                    player.PlayerPawn.Value?.SetModel(ZombieModel);

                player.GiveNamedItem(CsItem.KnifeT);
            });

        }

        Api.StartTimer(PrepareTime,
        remaining =>
        {
            PrepareTime--;
            foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
            {
                Api.PrintToHtml(player, Api.GetLocalizer("day_starting_html", Name, PrepareTime), 1);
            }
        },

        () =>
        {
            foreach (var player in Utilities.GetPlayers().Where(p => p.Team == CsTeam.Terrorist))
            {
                Api.UnfreezePlayer(player); // unfreeze zombies
                player.TakesDamage = true; // disable god mode
                Api.SetSpeed(player, 1.1f); // slightly faster than humans
            }
        });

        if (Api.GetConfigValue("DaysConfig.ZombieDayConfig.InfiniteReserve", true))
            Instance.RegisterEventHandler<EventWeaponReload>(OnWeaponReload); // apply infinite clip & only register the event if enable

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);
    }
    public HookResult OnWeaponReload(EventWeaponReload @event, GameEventInfo info)
    {
        CCSPlayerController? controller = @event.Userid;
        if (controller == null || controller.Team != CsTeam.CounterTerrorist)
            return HookResult.Continue;

        Instance.AddTimer(3.0f, () => Api.SetReserve(controller, 100));

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
        if (Api.GetConfigValue("DaysConfig.ZombieDayConfig.InfiniteReserve", true))
            Instance.DeregisterEventHandler<EventWeaponReload>(OnWeaponReload);

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquireFunc, HookMode.Pre);
    }
}
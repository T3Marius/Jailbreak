using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

using JailbreakApi;

namespace SpecialDays;

public class One_In_The_Chamber : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[SD] One In The Chamber";
    public override string ModuleVersion => "1.0.0";

    public static One_In_The_Chamber Instance { get; set; } = new One_In_The_Chamber();
    public IJailbreakApi Api = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Instance = this;

        Api = IJailbreakApi.Capability.Get() ?? throw new Exception("Jailbreak Api not found!");

        if (Api.GetConfigValue("DaysConfig.OneInTheChamberRound", true))
            Api.RegisterDay(new OneInTheChamber());
    }
}
public class OneInTheChamber : ISpecialDay
{
    public One_In_The_Chamber Instance => One_In_The_Chamber.Instance;
    public IJailbreakApi Api => Instance.Api;
    public string Name => Api.GetLocalizer("one_in_the_chamber_day<name>");
    public string Description => Api.GetLocalizer("one_in_the_chamber_day<description>");

    public bool g_IsTimerActive = false;
    public int DelayCooldown = 10;

    public HashSet<ushort> AllowedOITCWeaponsDefIndex { get; } = new(GetAllowedWeapons());

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

        var primaryWeapons = new[]
        {
            ItemDefinition.DESERT_EAGLE
        };

        foreach (var knife in knives)
            yield return (ushort)knife;

        foreach (var weapon in primaryWeapons)
            yield return (ushort)weapon;
    }

    public void Start()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            player.RemoveWeapons();

            Server.NextFrame(() =>
            {
                player.GiveNamedItem(CsItem.Knife);
                player.GiveNamedItem(CsItem.DesertEagle);
                Api.SetAmmo(player, 1);
                Api.SetReserve(player, 0);
            });
        }
        Api.StartTimer(DelayCooldown,
            remaining =>
            {
                g_IsTimerActive = true;
                DelayCooldown--;

                foreach (var player in Utilities.GetPlayers())
                {
                    Api.PrintToHtml(player, Api.GetLocalizer("day_starting_html", Name, DelayCooldown), 1);
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
        var controller = attacker.OriginalController.Value;
        if (controller == null)
            return HookResult.Continue;

        if (g_IsTimerActive)
            return HookResult.Continue;

        CBasePlayerWeapon? activeWeapon = Api.GetActiveWeapon(controller);

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

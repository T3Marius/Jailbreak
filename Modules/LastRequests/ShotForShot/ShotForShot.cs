using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using JailbreakApi;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using Microsoft.Extensions.DependencyModel;
using CounterStrikeSharp.API.Modules.Utils;

namespace LastRequests;

public class Shot_For_Shot : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[LR] Shot For Shot";
    public override string ModuleVersion => "1.0.0";
    public static IJailbreakApi Api = null!;
    public static Shot_For_Shot Instance { get; set; } = new();

    public override void Load(bool hotReload)
    {
        Instance = this;
    }
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Api = IJailbreakApi.Capability.Get() ?? throw new Exception("JailbreakAPI not found!");

        if (Api.GetConfigValue("LastRequest.ShotForShot", true))
            Api.RegisterRequest(new ShotForShot());
    }
}

public class ShotForShot : ILastRequest
{
    public IJailbreakApi Api => Shot_For_Shot.Api;
    public string Name => Api.GetLocalizer("shot_for_shot_last_request<name>");
    public string Description => string.Empty;

    public CCSPlayerController? Guardian { get; set; }
    public CCSPlayerController? Prisoner { get; set; }
    public CCSPlayerController? CurrentShooter { get; set; }
    public CCSPlayerController? NextShooter { get; set; }
    public string SelectedWeaponName { get; set; } = string.Empty;
    public string SelectedWeaponID { get; set; } = string.Empty;
    public string? SelectedType { get; set; } = string.Empty;

    public bool IsPrepTimerActive { get; set; }
    public IReadOnlyList<(string DisplayName, string ClassName)> GetAvailableWeapons() =>
    new List<(string, string)>
    {
        ("Deagle", "weapon_deagle"),
        ("USP-S", "weapon_usp_silencer"),
        ("TEC-9", "weapon_tec9"),
        ("P250", "weapon_p250"),
        ("P2000", "weapon_usp"),
        ("GLOCK-18", "weapon_glock18"),
    };
    public IReadOnlyList<string> GetAvalibleTypes() => new List<string> { };
    public HashSet<ushort> AllowedWeaponsDefIndex { get; } = new(GetAllowedWeapons());
    private static IEnumerable<ushort> GetAllowedWeapons()
    {
        var pistols = new[]
        {
            ItemDefinition.DESERT_EAGLE,
            ItemDefinition.USP_S,
            ItemDefinition.FIVE_SEVEN,
            ItemDefinition.TEC_9,
            ItemDefinition.P250,
            ItemDefinition.P2000,
            ItemDefinition.GLOCK_18

        };

        foreach (var pistol in pistols)
            yield return (ushort)pistol;
    }
    public void Start()
    {
        if (Prisoner == null || Guardian == null)
            return;

        var random = new Random().Next(0, 2);
        if (random == 1)
        {
            Api.SetAmmo(Prisoner, 1);
            Api.SetAmmo(Guardian, 0);

            Api.SetReserve(Prisoner, 0);
            Api.SetReserve(Guardian, 0);

            CurrentShooter = Prisoner;
            NextShooter = Guardian;
        }
        else
        {
            Api.SetAmmo(Prisoner, 0);
            Api.SetAmmo(Guardian, 1);

            Api.SetReserve(Prisoner, 0);
            Api.SetReserve(Guardian, 0);

            CurrentShooter = Guardian;
            NextShooter = Prisoner;
        }

        Shot_For_Shot.Instance.RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquire, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }
    public HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
    {
        CCSPlayerController? shooter = @event.Userid;
        if (shooter == null)
            return HookResult.Continue;

        if (CurrentShooter == null || NextShooter == null)
            return HookResult.Continue;

        if (shooter == CurrentShooter)
        {
            Api.SetAmmo(CurrentShooter, 0);
            Api.SetReserve(CurrentShooter, 0);

            Api.SetAmmo(NextShooter, 1);
            Api.SetReserve(NextShooter, 0);

            Api.PrintToChatAll(Api.GetLocalizer("prefix") + Api.GetLocalizer("next_shooter", NextShooter.PlayerName));

            var temp = CurrentShooter;
            CurrentShooter = NextShooter;
            NextShooter = temp;
        }

        return HookResult.Continue;
    }
    public HookResult OnCanAcquire(DynamicHook hook)
    {
        var econItem = hook.GetParam<CEconItemView>(1);
        ushort defIndex = (ushort)econItem.ItemDefinitionIndex;

        if (!AllowedWeaponsDefIndex.Contains(defIndex))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Handled;
        }
        return HookResult.Continue;
    }
    public HookResult OnTakeDamage(DynamicHook hook)
    {
        CTakeDamageInfo info = hook.GetParam<CTakeDamageInfo>(1);

        if (IsPrepTimerActive)
        {
            info.Damage = 0;
            return HookResult.Changed;
        }

        return HookResult.Continue;
    }
    public void End(CCSPlayerController? winner, CCSPlayerController? loser)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquire, HookMode.Pre);
        Shot_For_Shot.Instance.DeregisterEventHandler<EventWeaponFire>(OnWeaponFire);

        string winnerName = winner?.PlayerName ?? "None";
        string loserName = loser?.PlayerName ?? "None";

        Api.PrintToAlertAll(Api.GetLocalizer("last_request_ended", Name, winnerName, loserName));

        if (Prisoner == null || Guardian == null)
            return;

        Server.NextFrame(() => Prisoner.RemoveWeapons());

        CurrentShooter = null;
        NextShooter = null;
    }

}

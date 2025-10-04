using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using JailbreakApi;

namespace LastRequests;

public class Knife_Fight : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[LR] KnifeFight";
    public override string ModuleVersion => "1.0.0";

    public static IJailbreakApi Api = null!;
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Api = IJailbreakApi.Capability.Get() ?? throw new Exception("JailbreakApi not found!");

        if (Api.GetConfigValue("LastRequest.KnifeLastRequest", true))
            Api.RegisterRequest(new KnifeFight());
    }
}
public class KnifeFight : ILastRequest
{
    public IJailbreakApi Api => Knife_Fight.Api;
    public string Name => Api.GetLocalizer("knife_fight_last_request<name>");
    public string Description => string.Empty;

    public CCSPlayerController? Prisoner { get; set; }
    public CCSPlayerController? Guardian { get; set; }

    public string SelectedWeaponName { get; set; } = string.Empty;
    public string SelectedWeaponID { get; set; } = string.Empty;

    public IReadOnlyList<(string DisplayName, string ClassName)> GetAvailableWeapons() =>
        new List<(string, string)>
        {
            ("Knife", "weapon_knife"),
        };

    public string? SelectedType { get; set; } = string.Empty;
    public IReadOnlyList<string> GetAvalibleTypes() => new List<string> { "Normal", "Gravity", "Speed", "OneShot" };

    public HashSet<ushort> AlloweKnifesDefindex { get; } = new(GetAllowedWeapons());

    private static IEnumerable<ushort> GetAllowedWeapons()
    {
        var knifes = new[]
        {
            ItemDefinition.KNIFE_T,
            ItemDefinition.KNIFE_CT,
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

        foreach (var knife in knifes)
            yield return (ushort)knife;
    }

    public bool IsPrepTimerActive { get; set; } // this is a global bool, it's true everytime the prep timer is active

    public bool IsOneShotEnable = false;

    public void Start()
    {
        if (Prisoner == null || Guardian == null)
            return;

        Prisoner.RemoveWeapons();
        Guardian.RemoveWeapons();

        Api.SetHealth(Prisoner, 100);
        Api.SetHealth(Prisoner, 100);

        switch (SelectedType?.ToLower())
        {
            case "normal":
                IsOneShotEnable = false;
                break;
            case "gravity":
                IsOneShotEnable = false;

                Api.SetGravity(Prisoner, 0.3f);
                Api.SetGravity(Guardian, 0.3f);
                break;
            case "speed":
                IsOneShotEnable = false;

                Api.SetSpeed(Prisoner, 2.5f);
                Api.SetSpeed(Guardian, 2.5f);
                break;
            case "oneshot":
                IsOneShotEnable = true;
                break;

        }

        Server.NextFrame(() =>
        {
            Prisoner.GiveNamedItem(CsItem.KnifeT);
            Guardian.GiveNamedItem(CsItem.KnifeT);
        });

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }
    public HookResult OnCanAcquireFunc(DynamicHook hook)
    {
        var econItem = hook.GetParam<CEconItemView>(1);
        ushort defIndex = (ushort)econItem.ItemDefinitionIndex;

        if (!AlloweKnifesDefindex.Contains(defIndex))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Handled;
        }
        return HookResult.Continue;
    }
    private HookResult OnTakeDamage(DynamicHook hook)
    {
        var victim = hook.GetParam<CBaseEntity>(0);
        if (victim.DesignerName != "player")
            return HookResult.Continue;

        var info = hook.GetParam<CTakeDamageInfo>(1);
        var attackerHandle = info.Attacker;
        if (attackerHandle.Value == null || !attackerHandle.IsValid || attackerHandle.Value.DesignerName != "player")
            return HookResult.Continue;

        var attacker = attackerHandle.Value.As<CCSPlayerPawn>();
        var controller = attacker.Controller.Value;
        if (controller == null)
            return HookResult.Continue;

        if (IsPrepTimerActive)
        {
            info.Damage = 0;
            return HookResult.Handled;
        }

        if (IsOneShotEnable)
        {
            info.Damage = 1000;
            return HookResult.Changed;
        }

        return HookResult.Continue;
    }
    public void End(CCSPlayerController? winner, CCSPlayerController? loser)
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquireFunc, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        string winnerName = winner?.PlayerName ?? "None";
        string loserName = loser?.PlayerName ?? "None";

        Api.PrintToAlertAll(Api.GetLocalizer("last_request_ended", Name, winnerName, loserName));

        if (Prisoner == null || Guardian == null)
            return;

        Server.NextFrame(() => Prisoner.RemoveWeapons());

        Api.SetSpeed(Prisoner, 1.0f);
        Api.SetSpeed(Guardian, 1.0f);

        Api.SetGravity(Prisoner, 1.0f);
        Api.SetGravity(Guardian, 1.0f);
    }
}
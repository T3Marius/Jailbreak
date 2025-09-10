using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using JailbreakApi;
using static Jailbreak.Jailbreak;

namespace Jailbreak;

public class KnifeFightRequest : ILastRequest
{
    public string Name => Instance.Localizer["knife_fight_last_request<name>"];
    public string Description => string.Empty;

    public CCSPlayerController? Prisoner { get; set; }
    public CCSPlayerController? Guardian { get; set; }

    public string? SelectedWeapon { get; set; } = string.Empty;
    public IReadOnlyList<string> GetAvalibleWeapons() => new List<string> { "Knife" };

    public string? SelectedType { get; set; } = string.Empty;
    public IReadOnlyList<string> GetAvalibleTypes() => new List<string> { "Normal", "Gravity", "Speed", "OneShot" };

    public List<ushort> AllowedKnifesDefIndex = [(ushort)ItemDefinition.KNIFE_T, (ushort)ItemDefinition.KARAMBIT, (ushort)ItemDefinition.GUT_KNIFE,
    (ushort)ItemDefinition.FLIP_KNIFE, (ushort)ItemDefinition.BOWIE_KNIFE, (ushort)ItemDefinition.NOMAD_KNIFE, (ushort)ItemDefinition.TALON_KNIFE,
    (ushort)ItemDefinition.URSUS_KNIFE, (ushort)ItemDefinition.NAVAJA_KNIFE, (ushort)ItemDefinition.CLASSIC_KNIFE, (ushort)ItemDefinition.FALCHION_KNIFE,
    (ushort)ItemDefinition.HUNTSMAN_KNIFE, (ushort)ItemDefinition.PARACORD_KNIFE, (ushort)ItemDefinition.SKELETON_KNIFE, (ushort)ItemDefinition.STILETTO_KNIFE,
    (ushort)ItemDefinition.SURVIVAL_KNIFE, (ushort)ItemDefinition.BUTTERFLY_KNIFE];

    public bool IsPrepTimerActive { get; set; } // this is a global bool, it's true everytime the prep timer is active

    public bool IsOneShotEnable = false;

    public void Start()
    {
        if (Prisoner == null || Guardian == null)
            return;

        Prisoner.RemoveWeapons();
        Guardian.RemoveWeapons();

        Prisoner.SetHealth(100);
        Guardian.SetHealth(100);

        switch (SelectedType?.ToLower())
        {
            case "normal":
                IsOneShotEnable = false;
                break;
            case "gravity":
                IsOneShotEnable = false;

                Prisoner.SetGravity(0.3f);
                Guardian.SetGravity(0.3f);
                break;
            case "speed":
                IsOneShotEnable = false;

                Prisoner.SetSpeed(2.5f);
                Guardian.SetSpeed(2.5f);
                break;
            case "oneshot":
                IsOneShotEnable = true;
                break;

        }

        Server.NextFrame(() =>
        {
            Prisoner.GiveNamedItem("weapon_" + SelectedWeapon?.ToLower());
            Guardian.GiveNamedItem("weapon_" + SelectedWeapon?.ToLower());
        });

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }
    public HookResult OnCanAcquireFunc(DynamicHook hook)
    {
        var econItem = hook.GetParam<CEconItemView>(1);
        ushort defIndex = (ushort)econItem.ItemDefinitionIndex;

        if (!AllowedKnifesDefIndex.Contains(defIndex))
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

        Library.PrintToChatAll(Instance.Localizer["last_request_ended", Name, winnerName, loserName]);

        if (Prisoner == null || Guardian == null)
            return;

        Prisoner.SetSpeed(1.0f);
        Guardian.SetSpeed(1.0f);
        Prisoner.SetGravity(1.0f);
        Guardian.SetGravity(1.0f);
    }
}
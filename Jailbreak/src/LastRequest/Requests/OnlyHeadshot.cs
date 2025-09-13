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

public class OnlyHeadshot : ILastRequest
{
    public string Name => Instance.Localizer["only_headshot_fight_last_request<name>"];
    public string Description => string.Empty;

    public CCSPlayerController? Prisoner { get; set; }
    public CCSPlayerController? Guardian { get; set; }

    public string SelectedWeaponName { get; set; } = string.Empty;
    public string SelectedWeaponID { get; set; } = string.Empty;
    const int DMG_HEADSHOT = 1 << 23;

    public IReadOnlyList<(string DisplayName, string ClassName)> GetAvailableWeapons() =>
        new List<(string, string)>
        {
            ("Deagle", "weapon_deagle"),
            ("AK-47", "weapon_ak47"),
            ("Glock-18", "weapon_glock"),
            ("Five-SeveN", "weapon_fiveseven"),
            ("USP-S", "weapon_usp_silencer")
        };

    public string? SelectedType { get; set; } = string.Empty;
    public IReadOnlyList<string> GetAvalibleTypes() => new List<string> { };

    public List<ushort> AllowedWeaponsDefIndex = [(ushort)ItemDefinition.AK_47, (ushort)ItemDefinition.DESERT_EAGLE , (ushort)ItemDefinition.GLOCK_18,
    (ushort)ItemDefinition.FIVE_SEVEN, (ushort)ItemDefinition.USP_S];

    public bool IsPrepTimerActive { get; set; } // this is a global bool, it's true everytime the prep timer is active

    public void Start()
    {
        if (Prisoner == null || Guardian == null)
            return;

        Prisoner.RemoveWeapons();
        Guardian.RemoveWeapons();

        Prisoner.SetHealth(100);
        Guardian.SetHealth(100);

        Server.NextFrame(() =>
        {
            Prisoner.GiveNamedItem(SelectedWeaponID);
            Guardian.GiveNamedItem(SelectedWeaponID);
        });

        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }
    public HookResult OnCanAcquireFunc(DynamicHook hook)
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

        var hitGroupInfoId = info.GetHitGroup();
        if (hitGroupInfoId != HitGroup_t.HITGROUP_HEAD)
        {
            info.Damage = 0;
            return HookResult.Handled;
        }


        return HookResult.Continue;
    }
    public void End(CCSPlayerController? winner, CCSPlayerController? loser)
    {
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquireFunc, HookMode.Pre);
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);

        string winnerName = winner?.PlayerName ?? "None";
        string loserName = loser?.PlayerName ?? "None";

        Server.NextFrame(() => Prisoner?.RemoveWeapons());

        Library.PrintToChatAll(Instance.Localizer["last_request_ended", Name, winnerName, loserName]);
    }
}
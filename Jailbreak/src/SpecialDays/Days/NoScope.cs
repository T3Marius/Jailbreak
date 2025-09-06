using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Cvars;
using static CounterStrikeSharp.API.Core.Listeners;
using static Jailbreak.Jailbreak;
using CSTimer = CounterStrikeSharp.API.Modules.Timers.Timer;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

namespace Jailbreak;

public class NoScopeDay : ISpecialDay
{
    public string Name => Instance.Localizer["no_scope_day<name>"];
    public string Description => Instance.Localizer["no_scope_day<description>"];

    public Random random = new Random();
    public List<string> ScopeRifles = ["weapon_awp", "weapon_ssg08", "weapon_scar20", "weapon_g3sg1"];
    public static List<ushort> NoScopeWeaponsDefIndex = [(ushort)ItemDefinition.AWP, (ushort)ItemDefinition.SSG_08, (ushort)ItemDefinition.SCAR_20, (ushort)ItemDefinition.G3SG1];
    private int DelayCooldown = 10;
    public void Start()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            player.SetGravity(0.3f);
        }

        Library.StartTimer(DelayCooldown,
            remaining =>
            {
                DelayCooldown--;

                foreach (var player in Utilities.GetPlayers())
                {
                    player.RemoveWeapons(); // constantly remove until countdown ends
                    player.PrintToHtml(Instance.Localizer["day_starting_html", Name, DelayCooldown], 1.0f);
                }
            },

            () =>
            {
                string randomScopeWeapon = ScopeRifles[random.Next(ScopeRifles.Count)];
                foreach (var player in Utilities.GetPlayers())
                {
                    player.RemoveWeapons();

                    Server.NextFrame(() =>
                    {
                        player.GiveNamedItem(randomScopeWeapon);
                    });
                }

                Instance.RegisterListener<OnTick>(OnTick);
                VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnCanAcquireFunc, HookMode.Pre);

                ConVar.Find("mp_teammates_are_enemies")?.SetValue(true);
                Server.ExecuteCommand("sv_teamid_overhead 0");
            });
    }

    public void End()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            player.SetGravity(1.0f);
        }

        Instance.RemoveListener<OnTick>(OnTick);
        VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnCanAcquireFunc, HookMode.Pre);

        ConVar.Find("mp_teammates_are_enemies")?.SetValue(false);
        Server.ExecuteCommand("sv_teamid_overhead 1");
    }
    private void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            var activeWeapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon;
            if (activeWeapon?.Value == null)
                return;

            if (ScopeRifles.Contains(activeWeapon.Value.DesignerName))
            {
                activeWeapon.Value.NextSecondaryAttackTick = Server.TickCount + 500;
            }

        }
    }
    public HookResult OnCanAcquireFunc(DynamicHook hook)
    {
        var econItem = hook.GetParam<CEconItemView>(1);
        ushort defIndex = (ushort)econItem.ItemDefinitionIndex;


        if (!NoScopeWeaponsDefIndex.Contains(defIndex))
        {
            hook.SetReturn(AcquireResult.NotAllowedByProhibition);
            return HookResult.Handled;
        }


        return HookResult.Continue;
    }
}
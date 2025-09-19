using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Jailbreak;
using JailbreakApi;
using static CounterStrikeSharp.API.Core.Listeners;

namespace SpecialDays;

public class No_Scope : BasePlugin
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "[SD] No Scope";
    public override string ModuleVersion => "1.0.0";

    public static No_Scope Instance { get; set; } = new No_Scope();
    public IJailbreakApi Api = null!;

    public override void OnAllPluginsLoaded(bool hotReload)
    {
        Instance = this;

        Api = IJailbreakApi.Capability.Get() ?? throw new Exception("Jailbreak Api not found!");

        if (Api.GetConfigValue("DaysConfig.NoScopeRound", true))
            Api.RegisterDay(new NoScope());
    }
}
public class NoScope : ISpecialDay
{
    public No_Scope Instance => No_Scope.Instance;
    public IJailbreakApi Api => Instance.Api;

    public string Name => Api.GetLocalizer("no_scope_day<name>");
    public string Description => Api.GetLocalizer("no_scope_day<description>");

    public Random random = new Random();
    public List<string> ScopeRifles = ["weapon_awp", "weapon_ssg08", "weapon_scar20", "weapon_g3sg1"];
    private int DelayCooldown = 10;

    public HashSet<ushort> NoScopeWeaponsDefIndex { get; } = new(GetAllowedWeapons());

    private static IEnumerable<ushort> GetAllowedWeapons()
    {
        var rifles = new[]
        {
            ItemDefinition.AWP,
            ItemDefinition.SSG_08,
            ItemDefinition.SCAR_20,
            ItemDefinition.G3SG1
        };

        foreach (var rifle in rifles)
            yield return (ushort)rifle;
    }

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
                    Api.PrintToHtml(player, Api.GetLocalizer("day_starting_html", Name, DelayCooldown), 1);
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
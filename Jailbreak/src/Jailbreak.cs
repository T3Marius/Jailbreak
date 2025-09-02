using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using T3MenuSharedApi;
using Jailbreak.Config;
using CounterStrikeSharp.API.Core.Capabilities;

namespace Jailbreak;

public class Jailbreak : BasePlugin
{
    private ConfigManager? _configManager;

    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "Jailbreak";
    public override string ModuleVersion => "1.0.0";
    public static Jailbreak Instance { get; set; } = new();
    public JailbreakConfig Config => _configManager?.Config ?? new JailbreakConfig();
    public static IT3MenuManager MenuManager = null!;
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        if (MenuManager == null)
            MenuManager = new PluginCapability<IT3MenuManager>("t3menu:manager").Get() ?? throw new Exception("T3MenuAPI not found!");
    }
    public override void Load(bool hotReload)
    {
        Instance = this;

        _configManager = new ConfigManager(Path.Combine(
            Server.GameDirectory,
            "csgo", "addons",
            "counterstrikesharp", "configs",
            "Jailbreak", "config.yaml"), Logger);

        _configManager.Initialize();

        Events.RegisterVirtualFunctions();
        Events.RegisterEventsHandlers();
        Events.RegisterListeners();

        SpecialDayManagement.RegisterDay(new NoScopeDay());

        WardenCommands.Register();
    }
    public override void Unload(bool hotReload)
    {
        _configManager?.Dispose();
        Events.Dispose();
    }

    [ConsoleCommand("css_roletest")]
    public void OnRoleTest(CCSPlayerController player, CommandInfo info)
    {
        if (player == null)
            return;

        JBPlayer jBPlayer = JBPlayerManagement.GetOrCreate(player);

        player.PrintToChat($"Your role is: {jBPlayer.Role}");
    }
}

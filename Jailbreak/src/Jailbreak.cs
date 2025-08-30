using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using Jailbreak.Config;

namespace Jailbreak;

public class Jailbreak : BasePlugin
{
    private ConfigManager? _configManager;

    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "Jailbreak";
    public override string ModuleVersion => "1.0.0";
    public static Jailbreak Instance { get; set; } = new();
    public JailbreakConfig Config => _configManager?.Config ?? new JailbreakConfig();

    public override void OnAllPluginsLoaded(bool hotReload)
    {

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

        Events.RegisterEventsHandlers();
        Events.RegisterListeners();

        WardenCommands.Register();

        JBPlayerManagement.Initialize(Logger);
    }
    public override void Unload(bool hotReload)
    {
        _configManager?.Dispose();
    }

    // this will be keep for testing purposes.
    [ConsoleCommand("css_roletest")]
    public void Command_Test(CCSPlayerController player, CommandInfo info)
    {
        var jbPlayer = JBPlayerManagement.GetOrCreate(player);

        jbPlayer.PrintToHtml($"Your role is: <font color='red'>{jbPlayer.Role}</font>", 5);
    }
}

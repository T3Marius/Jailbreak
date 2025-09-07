using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using T3MenuSharedApi;
using Jailbreak.Config;
using CounterStrikeSharp.API.Core.Capabilities;
using JailbreakApi;

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
    public JailbreakApi Api { get; set; } = new JailbreakApi();
    public override void OnAllPluginsLoaded(bool hotReload)
    {
        if (MenuManager == null)
            MenuManager = new PluginCapability<IT3MenuManager>("t3menu:manager").Get() ?? throw new Exception("T3MenuAPI not found!");
    }
    public override void Load(bool hotReload)
    {
        Instance = this;

        Capabilities.RegisterPluginCapability(IJailbreakApi.Capability, () => Api);

        _configManager = new ConfigManager(Path.Combine(
            Server.GameDirectory,
            "csgo", "addons",
            "counterstrikesharp", "configs",
            "Jailbreak", "config.yaml"), Logger);

        _configManager.Initialize();

        Events.RegisterVirtualFunctions();
        Events.RegisterEventsHandlers();
        Events.RegisterListeners();

        if (Config.DaysConfig.NoScopeRound)
            SpecialDayManagement.RegisterDay(new NoScopeDay());

        if (Config.DaysConfig.TeleportRound)
            SpecialDayManagement.RegisterDay(new TeleportDay());

        if (Config.DaysConfig.ZombieRound)
            SpecialDayManagement.RegisterDay(new ZombieDay());

        if (Config.DaysConfig.OneInTheChamberRound)
            SpecialDayManagement.RegisterDay(new OneInTheChamberDay());

        if (Config.LastRequest.KnifeLastRequest)
            LastRequestManagement.RegisterRequest(new KnifeFightRequest());

        WardenCommands.Register();
        PrisonerCommands.Register();
        GunsMenuCommands.Register();
    }
    public override void Unload(bool hotReload)
    {
        _configManager?.Dispose();
        Events.Dispose();
    }
}

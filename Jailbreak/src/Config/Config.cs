using System.ComponentModel;
using MySqlConnector;
using YamlDotNet.Serialization;

namespace Jailbreak.Config;

public class JailbreakConfig
{
    [YamlMember(Alias = "config_version")]
    [DefaultValue(1)]
    public int ConfigVersion { get; set; } = 1;

    [YamlMember(Alias = "database")]
    public DatabaseConfig Database { get; set; } = new();

    [YamlMember(Alias = "warden")]
    public WardenConfig Warden { get; set; } = new();

    [YamlMember(Alias = "guard")]
    public GuardConfig Guard { get; set; } = new();

    [YamlMember(Alias = "prisoner")]
    public PrisonerConfig Prisoner { get; set; } = new();

    [YamlMember(Alias = "models")]
    public ModelsConfig Models { get; set; } = new();

    [YamlMember(Alias = "sounds_config")]
    public Sounds_Config Sounds { get; set; } = new();

    [YamlMember(Alias = "global_volume")]
    public VolumeConfig GlobalVolume { get; set; } = new();

    [YamlMember(Alias = "special_days")]
    public DaysConfig DaysConfig { get; set; } = new();

    [YamlMember(Alias = "guns_menu_config")]
    public GunsMenuConfig GunsMenu { get; set; } = new();

    public string[] Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(Database.Host) || string.IsNullOrEmpty(Database.Name) || string.IsNullOrEmpty(Database.User))
        {
            errors.Add("You need to setup database credentials in config.");
        }

        return errors.ToArray();
    }
}

public class DatabaseConfig
{
    [YamlMember(Alias = "host")]
    public string Host { get; set; } = "localhost";

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "jailbreak";

    [YamlMember(Alias = "user")]
    public string User { get; set; } = "root";

    [YamlMember(Alias = "pass")]
    public string Pass { get; set; } = "password";

    [YamlMember(Alias = "port")]
    public uint Port { get; set; } = 3306;

    [YamlMember(Alias = "ssl_mode")]
    public string SslMode { get; set; } = "none";
}
public class WardenConfig
{

    [YamlMember(Alias = "warden_color")]
    public string WardenColor { get; set; } = "Blue";

    [YamlMember(Alias = "commands")]
    public WardenCommandsConfig Commands { get; set; } = new();
}
public class GuardConfig
{
    [YamlMember(Alias = "show_guns_menu_on_round_start")]
    public bool ShowGunsMenuOnRoundStart { get; set; } = true;
}
public class PrisonerConfig
{
    [YamlMember(Alias = "unmute_prisoner_on_round_end")]
    public bool UnmutePrisonerOnRoundEnd { get; set; } = false;

    [YamlMember(Alias = "round_start_mute_duration")]
    public int RoundStartMuteDuration { get; set; } = 15;

    [YamlMember(Alias = "skip_mute_flags")]
    public List<string> SkipMuteFlags { get; set; } = ["@css/generic"];

}
public class WardenCommandsConfig
{
    [YamlMember(Alias = "take_warden")]
    public List<string> TakeWarden { get; set; } = ["w", "warden"];

    [YamlMember(Alias = "give_up_warden")]
    public List<string> GiveUpWarden { get; set; } = ["uw", "unwarden"];

    [YamlMember(Alias = "open_cells")]
    public List<string> OpenCells { get; set; } = ["o", "open"];

    [YamlMember(Alias = "close_cells")]
    public List<string> CloseCells { get; set; } = ["c", "close"];

    [YamlMember(Alias = "warden_menu")]
    public List<string> WardenMenu { get; set; } = ["wmenu"];

    [YamlMember(Alias = "special_days_menu")]
    public List<string> SpecialDaysMenu { get; set; } = ["sd", "specialday"];

    [YamlMember(Alias = "toggle_box")]
    public List<string> ToggleBox { get; set; } = ["box"];
}
public class ModelsConfig
{
    [YamlMember(Alias = "warden_model")]
    public string WardenModel { get; set; } = "";

    [YamlMember(Alias = "guardian_model")]
    public string GuardianModel { get; set; } = "";

    [YamlMember(Alias = "prisoner_model")]
    public string PrisonerModel { get; set; } = "";
}
public class VolumeConfig
{
    [YamlMember(Alias = "warden_set_volume")]
    public float WardenSetVolume { get; set; } = 1.0f;

    [YamlMember(Alias = "warden_removed_volume")]
    public float WardenRemovedVolume { get; set; } = 1.0f;

    [YamlMember(Alias = "warden_killed_volume")]
    public float WardenKilledVolume { get; set; } = 1.0f;

    [YamlMember(Alias = "box_start_volume")]
    public float BoxStartVolume { get; set; } = 1.0f;
}
public class DaysConfig
{
    [YamlMember(Alias = "cooldown_in_rounds")]
    public int CooldownInRounds { get; set; } = 3;

    [YamlMember(Alias = "no_scope_round")]
    public bool NoScopeRound { get; set; } = true;

    [YamlMember(Alias = "teleport_round")]
    public bool TeleportRound { get; set; } = true;

    [YamlMember(Alias = "zombie_round")]
    public bool ZombieRound { get; set; } = true;
    public ZombieDayConfig ZombieDayConfig { get; set; } = new();

    [YamlMember(Alias = "one_in_the_chamber_round")]
    public bool OneInTheChamberRound { get; set; } = true;

}
public class ZombieDayConfig
{
    [YamlMember(Alias = "zombies_health")]
    public int ZombiesHealth = 5000;

    [YamlMember(Alias = "enable_knockback")]
    public bool EnableKnockback = false;

    [YamlMember(Alias = "prepare_time_in_seconds")]
    public int PrepareTimeInSeconds { get; set; } = 60;

    [YamlMember(Alias = "zombies_model")]
    public string ZombiesModel { get; set; } = "";

    [YamlMember(Alias = "infinite_reserve")]
    public bool InfiniteReserve { get; set; } = true;
}
public class Sounds_Config
{
    [YamlMember(Alias = "sound_event_files")]
    public List<string> SoundEventFiles { get; set; } = [];

    [YamlMember(Alias = "rebel_sound")]
    public string RebelSound { get; set; } = "";

    [YamlMember(Alias = "warden_set_sound")]
    public string WardenSetSound { get; set; } = "";

    [YamlMember(Alias = "warden_removed_sound")]
    public string WardenRemovedSound { get; set; } = "";

    [YamlMember(Alias = "warden_killed_sound")]
    public string WardenKilledSound { get; set; } = "";

    [YamlMember(Alias = "box_start_sound")]
    public string BoxStartSound { get; set; } = "";


}
public class GunsMenuConfig
{
    [YamlMember(Alias = "guns_menu_commands")]
    public List<string> GunsMenuCommands { get; set; } = ["guns", "g", "gmenu", "gunsmenu"];

    [YamlMember(Alias = "exclude_weapons")]
    public List<string> ExcludeWeapons { get; set; } = [""];
}
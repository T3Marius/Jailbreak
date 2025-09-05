using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Jailbreak.Config;

public class ConfigManager : IDisposable
{
    private readonly string _filePath;
    private readonly FileSystemWatcher _watcher;
    private readonly Timer _debounceTimer;
    private readonly SemaphoreSlim _semaphore;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    private JailbreakConfig _config;
    private bool _disposed;
    private const int DebounceDelayMs = 500;

    public event Action<JailbreakConfig>? ConfigChanged;
    public event Action<Exception>? ConfigError;

    public JailbreakConfig Config => _config;
    public bool IsWatching => !_disposed && _watcher.EnableRaisingEvents;
    public ILogger _logger = null!;

    public ConfigManager(string filePath, ILogger logger)
    {
        _filePath = Path.GetFullPath(filePath);
        _config = new JailbreakConfig();
        _semaphore = new SemaphoreSlim(1, 1);
        _logger = logger;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithIndentedSequences()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
            .WithNewLine("\n")
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var fileName = Path.GetFileName(_filePath);
        _watcher = new FileSystemWatcher(directory!, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = false
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Error += OnWatcherError;

        _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    public async Task LoadAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                _config = new JailbreakConfig();
                await SaveInternalAsync();
                return;
            }

            var yaml = await File.ReadAllTextAsync(_filePath);
            var loadedConfig = _deserializer.Deserialize<JailbreakConfig>(yaml);

            if (loadedConfig == null)
            {
                loadedConfig = new JailbreakConfig();
            }

            _config = MergeWithDefaults(loadedConfig);

            var errors = _config.Validate();
            if (errors.Length > 0)
            {
                throw new InvalidOperationException($"Config validation failed: {string.Join(", ", errors)}");
            }

            await SaveInternalAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            await SaveInternalAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void StartWatching()
    {
        if (!_disposed)
        {
            _watcher.EnableRaisingEvents = true;
        }
    }

    public void StopWatching()
    {
        if (!_disposed)
        {
            _watcher.EnableRaisingEvents = false;
        }
    }

    private async Task SaveInternalAsync()
    {
        var yaml = _serializer.Serialize(_config);
        var tempFile = _filePath + ".tmp";

        await File.WriteAllTextAsync(tempFile, yaml);

        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }

        File.Move(tempFile, _filePath);
    }

    private JailbreakConfig MergeWithDefaults(JailbreakConfig loaded)
    {
        var defaults = new JailbreakConfig();

        return new JailbreakConfig
        {
            ConfigVersion = loaded.ConfigVersion > 0 ? loaded.ConfigVersion : defaults.ConfigVersion,
            Database = MergeDatabaseConfig(loaded.Database, defaults.Database),
            Guard = MergeGuardConfig(loaded.Guard, defaults.Guard),
            Prisoner = MergePrisonerConfig(loaded.Prisoner, defaults.Prisoner),
            Models = MergeModelsConfig(loaded.Models, defaults.Models)
        };
    }
    private ModelsConfig MergeModelsConfig(ModelsConfig loaded, ModelsConfig defaults)
    {
        return new ModelsConfig
        {
            WardenModel = loaded.WardenModel ?? defaults.WardenModel,
            GuardianModel = loaded.GuardianModel ?? defaults.GuardianModel,
            PrisonerModel = loaded.PrisonerModel ?? defaults.PrisonerModel
        };
    }
    private DatabaseConfig MergeDatabaseConfig(DatabaseConfig loaded, DatabaseConfig defaults)
    {
        return new DatabaseConfig
        {
            Host = !string.IsNullOrEmpty(loaded.Host) ? loaded.Host : defaults.Host,
            Name = !string.IsNullOrEmpty(loaded.Name) ? loaded.Name : defaults.Name,
            User = !string.IsNullOrEmpty(loaded.User) ? loaded.User : defaults.User,
            Pass = !string.IsNullOrEmpty(loaded.Pass) ? loaded.Pass : defaults.Pass,
            Port = loaded.Port > 0 ? loaded.Port : defaults.Port,
            SslMode = !string.IsNullOrEmpty(loaded.SslMode) ? loaded.SslMode : defaults.SslMode
        };
    }

    private GuardConfig MergeGuardConfig(GuardConfig loaded, GuardConfig defaults)
    {
        return new GuardConfig
        {
            ShowGunsMenuOnRoundStart = loaded.ShowGunsMenuOnRoundStart
        };
    }

    private PrisonerConfig MergePrisonerConfig(PrisonerConfig loaded, PrisonerConfig defaults)
    {
        return new PrisonerConfig
        {
            UnmutePrisonerOnRoundEnd = loaded.UnmutePrisonerOnRoundEnd,
            RoundStartMuteDuration = loaded.RoundStartMuteDuration > 0 ? loaded.RoundStartMuteDuration : defaults.RoundStartMuteDuration,
            SkipMuteFlags = loaded.SkipMuteFlags?.Count > 0 ? loaded.SkipMuteFlags : defaults.SkipMuteFlags
        };
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_disposed || e.FullPath != _filePath) return;

        _debounceTimer.Change(DebounceDelayMs, Timeout.Infinite);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        ConfigError?.Invoke(e.GetException());

        try
        {
            StopWatching();
            Thread.Sleep(1000);
            StartWatching();
        }
        catch { }
    }

    private async void OnDebounceTimerElapsed(object? state)
    {
        if (_disposed) return;

        try
        {
            if (!IsFileReady(_filePath)) return;

            await LoadAsync();

            ConfigChanged?.Invoke(_config);
        }
        catch (Exception ex)
        {
            ConfigError?.Invoke(ex);
        }
    }

    private static bool IsFileReady(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public void Initialize()
    {
        try
        {

            ConfigChanged += OnConfigChanged;
            ConfigError += OnConfigError;
            Task.Run(async () =>
            {
                await LoadAsync();
            });

            StartWatching();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to initialize configuration: {ex.Message}");
        }
    }

    private void OnConfigChanged(JailbreakConfig config)
    {
        _logger?.LogInformation("Configuration reloaded successfully!");
    }

    private void OnConfigError(Exception ex)
    {
        _logger?.LogError($"Configuration error: {ex.Message}");
    }
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
        _semaphore?.Dispose();
    }
}

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
        return MergeObjects(loaded, defaults);
    }

    private T MergeObjects<T>(T loaded, T defaults) where T : new()
    {
        if (loaded == null) return defaults;
        if (defaults == null) return loaded;

        var result = new T();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite) continue;

            var loadedValue = property.GetValue(loaded);
            var defaultValue = property.GetValue(defaults);

            // If the loaded value is null or default, use the default value
            if (IsNullOrDefault(loadedValue, property.PropertyType))
            {
                property.SetValue(result, defaultValue);
            }
            // If it's a complex object (not primitive/string), merge recursively
            else if (IsComplexType(property.PropertyType))
            {
                var mergedValue = MergeComplexObject(loadedValue, defaultValue, property.PropertyType);
                property.SetValue(result, mergedValue);
            }
            // Otherwise, use the loaded value
            else
            {
                property.SetValue(result, loadedValue);
            }
        }

        return result;
    }

    private object? MergeComplexObject(object? loaded, object? defaults, Type type)
    {
        if (loaded == null) return defaults;
        if (defaults == null) return loaded;

        var result = Activator.CreateInstance(type);
        var properties = type.GetProperties();

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite) continue;

            var loadedValue = property.GetValue(loaded);
            var defaultValue = property.GetValue(defaults);

            if (IsNullOrDefault(loadedValue, property.PropertyType))
            {
                property.SetValue(result, defaultValue);
            }
            else if (IsComplexType(property.PropertyType))
            {
                var mergedValue = MergeComplexObject(loadedValue, defaultValue, property.PropertyType);
                property.SetValue(result, mergedValue);
            }
            else
            {
                property.SetValue(result, loadedValue);
            }
        }

        return result;
    }

    private static bool IsNullOrDefault(object? value, Type type)
    {
        if (value == null) return true;

        // For strings, check if empty
        if (type == typeof(string))
            return string.IsNullOrEmpty((string)value);

        // For value types, check if default
        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }

        return false;
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive &&
               type != typeof(string) &&
               type != typeof(DateTime) &&
               type != typeof(decimal) &&
               !type.IsEnum &&
               type.IsClass;
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

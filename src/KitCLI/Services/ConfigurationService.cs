using System.Runtime.InteropServices;
using System.Text.Json;
using KitCLI.Models;

namespace KitCLI.Services;

public interface IConfigurationService
{
    Task<KitConfig?> LoadConfigAsync(string? profile = null);
    Task SaveConfigAsync(KitConfig config, string? profile = null);
    Task<ConfigFile> LoadConfigFileAsync();
    Task SaveConfigFileAsync(ConfigFile configFile);
    string GetConfigPath();
}

public sealed class ConfigurationService : IConfigurationService
{
    private readonly string _configPath;

    public ConfigurationService()
    {
        // Check environment variable first
        _configPath = Environment.GetEnvironmentVariable("KIT_CONFIG_PATH")
            ?? GetDefaultConfigPath();
    }

    public ConfigurationService(string configPath)
    {
        _configPath = configPath;
    }

    private static string GetDefaultConfigPath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".kit", "config.json");
    }

    public string GetConfigPath() => _configPath;

    public async Task<KitConfig?> LoadConfigAsync(string? profile = null)
    {
        // Check environment variables first (highest priority)
        var envApiKey = Environment.GetEnvironmentVariable("KIT_API_KEY");
        if (!string.IsNullOrEmpty(envApiKey))
        {
            return new KitConfig
            {
                ApiKey = envApiKey,
                ApiVersion = Environment.GetEnvironmentVariable("KIT_API_VERSION") ?? "v4"
            };
        }

        // Load from config file
        if (!File.Exists(_configPath))
        {
            return null;
        }

        try
        {
            var configFile = await LoadConfigFileAsync();
            var profileName = profile ?? configFile.CurrentProfile ?? "default";

            if (configFile.Profiles.TryGetValue(profileName, out var config))
            {
                return config;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading config: {ex.Message}");
            return null;
        }
    }

    public async Task SaveConfigAsync(KitConfig config, string? profile = null)
    {
        var configFile = File.Exists(_configPath)
            ? await LoadConfigFileAsync()
            : new ConfigFile();

        var profileName = profile ?? configFile.CurrentProfile ?? "default";
        configFile.Profiles[profileName] = config;

        if (string.IsNullOrEmpty(configFile.CurrentProfile))
        {
            configFile.CurrentProfile = profileName;
        }

        await SaveConfigFileAsync(configFile);
    }

    public async Task<ConfigFile> LoadConfigFileAsync()
    {
        if (!File.Exists(_configPath))
        {
            return new ConfigFile();
        }

        var json = await File.ReadAllTextAsync(_configPath);
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.ConfigFile)
            ?? new ConfigFile();
    }

    public async Task SaveConfigFileAsync(ConfigFile configFile)
    {
        var directory = Path.GetDirectoryName(_configPath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);

            // Set secure permissions on Unix
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                File.SetUnixFileMode(directory,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        var json = JsonSerializer.Serialize(configFile, KitJsonContext.Default.ConfigFile);
        await File.WriteAllTextAsync(_configPath, json);

        // Set secure permissions on the config file
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(_configPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        Console.WriteLine($"Configuration saved to {_configPath}");
    }
}

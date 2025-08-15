using System.Text.Json.Serialization;

namespace KitCLI.Models;

public sealed class ConfigFile
{
    [JsonPropertyName("profiles")]
    public Dictionary<string, KitConfig> Profiles { get; set; } = new();
    
    [JsonPropertyName("current_profile")]
    public string CurrentProfile { get; set; } = "default";
    
    [JsonPropertyName("settings")]
    public AppSettings Settings { get; set; } = new();
}

public sealed class KitConfig
{
    [JsonPropertyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonPropertyName("api_version")]
    public string ApiVersion { get; set; } = "v4";
    
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey);
    
    [JsonIgnore]
    public string BaseUrl => $"https://api.kit.com/{ApiVersion}";
    
    public string GetMaskedApiKey()
    {
        if (string.IsNullOrEmpty(ApiKey)) return "(not set)";
        if (ApiKey.Length <= 8) return "****";
        return $"{ApiKey[..4]}...{ApiKey[^4..]}";
    }
}

public sealed class AppSettings
{
    [JsonPropertyName("default_format")]
    public string DefaultFormat { get; set; } = "table";
    
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 50;
    
    [JsonPropertyName("auto_update")]
    public bool AutoUpdate { get; set; } = true;
    
    [JsonPropertyName("export_path")]
    public string? ExportPath { get; set; }
}
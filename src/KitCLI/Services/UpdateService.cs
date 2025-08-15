using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KitCLI.Services;

/// <summary>
/// Service for checking and downloading updates from GitHub releases
/// </summary>
public sealed class UpdateService
{
    private const string GITHUB_API = "https://api.github.com/repos/Aaronontheweb/kit-cli/releases/latest";
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;

    public UpdateService(HttpClient httpClient, string currentVersion)
    {
        _httpClient = httpClient;
        _currentVersion = currentVersion.Split('+')[0]; // Remove build metadata
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "kit-cli");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GITHUB_API, cancellationToken);
            var release = JsonSerializer.Deserialize(response, KitJsonContext.Default.GitHubRelease);

            if (release == null)
            {
                return null;
            }

            // Parse version from tag (e.g., "v1.2.3" -> "1.2.3")
            var latestVersion = release.TagName.TrimStart('v');

            if (IsNewerVersion(latestVersion, _currentVersion))
            {
                var asset = FindPlatformAsset(release.Assets);
                if (asset != null)
                {
                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = asset.BrowserDownloadUrl,
                        ReleaseNotes = release.Body,
                        PublishedAt = release.PublishedAt,
                        AssetName = asset.Name,
                        AssetSize = asset.Size
                    };
                }
            }
        }
        catch (HttpRequestException)
        {
            // Network error - silently fail
        }
        catch (TaskCanceledException)
        {
            // Timeout - silently fail
        }
        catch (Exception ex)
        {
            if (Environment.GetEnvironmentVariable("KIT_CLI_VERBOSE") == "1")
            {
                Console.Error.WriteLine($"Update check failed: {ex.Message}");
            }
        }

        return null;
    }

    private static GitHubAsset? FindPlatformAsset(GitHubAsset[] assets)
    {
        var (os, arch) = GetPlatformInfo();
        var platformString = $"{os}-{arch}";

        // Look for exact match first (e.g., "kit-linux-x64")
        var asset = assets.FirstOrDefault(a =>
            a.Name.Contains(platformString, StringComparison.OrdinalIgnoreCase));

        // Fallback to OS-only match if no exact match
        if (asset == null)
        {
            asset = assets.FirstOrDefault(a =>
                a.Name.Contains(os, StringComparison.OrdinalIgnoreCase));
        }

        return asset;
    }

    private static (string os, string arch) GetPlatformInfo()
    {
        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64"
        };

        if (OperatingSystem.IsWindows())
        {
            return ("win", arch);
        }

        if (OperatingSystem.IsMacOS())
        {
            return ("osx", arch);
        }

        if (OperatingSystem.IsLinux())
        {
            return ("linux", arch);
        }

        return ("unknown", arch);
    }

    public static bool IsNewerVersion(string latest, string current)
    {
        try
        {
            // Remove any 'v' prefix
            latest = latest.TrimStart('v');
            current = current.TrimStart('v');

            // Split into parts and parse
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();

            // Compare version parts
            for (int i = 0; i < Math.Min(latestParts.Length, currentParts.Length); i++)
            {
                if (latestParts[i] > currentParts[i])
                {
                    return true;
                }

                if (latestParts[i] < currentParts[i])
                {
                    return false;
                }
            }

            // If all compared parts are equal, newer version has more parts
            return latestParts.Length > currentParts.Length;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]?> DownloadUpdateAsync(string downloadUrl, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var memoryStream = new MemoryStream();

            while (true)
            {
                var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    progress?.Report((double)downloadedBytes / totalBytes * 100);
                }
            }

            return memoryStream.ToArray();
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Information about an available update
/// </summary>
public sealed class UpdateInfo
{
    public required string Version { get; init; }
    public required string DownloadUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public DateTime PublishedAt { get; init; }
    public required string AssetName { get; init; }
    public long AssetSize { get; init; }

    public string GetFormattedSize()
    {
        const long KB = 1024;
        const long MB = KB * 1024;

        if (AssetSize >= MB)
        {
            return $"{AssetSize / (double)MB:F1} MB";
        }

        if (AssetSize >= KB)
        {
            return $"{AssetSize / (double)KB:F1} KB";
        }

        return $"{AssetSize} bytes";
    }
}

/// <summary>
/// GitHub release model
/// </summary>
public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
}

/// <summary>
/// GitHub release asset model
/// </summary>
public sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}


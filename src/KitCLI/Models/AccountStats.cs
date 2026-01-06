using System.Text.Json.Serialization;

namespace KitCLI.Models;

/// <summary>
/// Account-level email statistics from the Kit API.
/// </summary>
public sealed class AccountStats
{
    [JsonPropertyName("sent")]
    public int Sent { get; set; }

    [JsonPropertyName("clicked")]
    public int Clicked { get; set; }

    [JsonPropertyName("opened")]
    public int Opened { get; set; }

    [JsonPropertyName("email_stats_mode")]
    public string EmailStatsMode { get; set; } = "last_90";

    [JsonPropertyName("open_tracking_enabled")]
    public bool OpenTrackingEnabled { get; set; }

    [JsonPropertyName("click_tracking_enabled")]
    public bool ClickTrackingEnabled { get; set; }

    [JsonPropertyName("starting")]
    public DateTime Starting { get; set; }

    [JsonPropertyName("ending")]
    public DateTime Ending { get; set; }

    [JsonIgnore]
    public double OpenRate => Sent > 0 ? (double)Opened / Sent * 100 : 0;

    [JsonIgnore]
    public double ClickRate => Sent > 0 ? (double)Clicked / Sent * 100 : 0;
}

/// <summary>
/// Response wrapper for account email stats endpoint.
/// </summary>
public sealed class AccountStatsResponse
{
    [JsonPropertyName("stats")]
    public AccountStats? Stats { get; set; }
}

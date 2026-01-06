namespace KitCLI.Models;

/// <summary>
/// Represents the analysis results for a single broadcast.
/// This is only used for user-facing output, not API communication.
/// </summary>
public sealed class BroadcastAnalysis
{
    public long BroadcastId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? SendAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? FromName { get; set; }

    public string? FromEmail { get; set; }

    public int Recipients { get; set; }

    /// <summary>
    /// Estimated unique opens (calculated from open_rate × recipients).
    /// Kit V4 API doesn't provide unique opens directly.
    /// </summary>
    public int UniqueOpens { get; set; }

    /// <summary>
    /// Total email opens from the stats endpoint (emails_opened).
    /// </summary>
    public int TotalOpens { get; set; }

    /// <summary>
    /// Total unique clicks across all links (summed from clicks endpoint).
    /// </summary>
    public int UniqueClicks { get; set; }

    /// <summary>
    /// Total clicks from the stats endpoint (total_clicks).
    /// </summary>
    public int TotalClicks { get; set; }

    public int Unsubscribes { get; set; }

    public double OpenRate { get; set; }

    public double ClickRate { get; set; }

    public double ClickToOpenRate { get; set; }

    /// <summary>
    /// Per-link click breakdown from the clicks endpoint.
    /// </summary>
    public LinkClickAnalysis[] LinkClicks { get; set; } = [];
}

/// <summary>
/// Analysis data for a single link in the broadcast.
/// </summary>
public sealed class LinkClickAnalysis
{
    public string Url { get; set; } = string.Empty;

    public int UniqueClicks { get; set; }

    public double ClickToDeliveryRate { get; set; }

    public double ClickToOpenRate { get; set; }
}

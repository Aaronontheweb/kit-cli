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

/// <summary>
/// Export format for broadcast click data.
/// Used for JSON output in the 'kit broadcast clicks' command.
/// </summary>
public sealed class BroadcastClicksExport
{
    public long BroadcastId { get; set; }

    public int TotalLinks { get; set; }

    public int TotalUniqueClicks { get; set; }

    public LinkClickExport[] Links { get; set; } = [];
}

/// <summary>
/// Single link click data for export.
/// </summary>
public sealed class LinkClickExport
{
    public string Url { get; set; } = string.Empty;

    public int UniqueClicks { get; set; }

    public double ClickToDeliveryRate { get; set; }

    public double ClickToOpenRate { get; set; }
}

/// <summary>
/// Represents trend data for a time period (day, week, or month).
/// </summary>
public sealed class BroadcastTrendPeriod
{
    public string Period { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int BroadcastCount { get; set; }

    public int TotalRecipients { get; set; }

    public double AverageOpenRate { get; set; }

    public double AverageClickRate { get; set; }

    public double AverageUnsubscribeRate { get; set; }

    /// <summary>
    /// Best performing broadcast in this period by open rate.
    /// </summary>
    public string? BestPerformerSubject { get; set; }

    public long? BestPerformerId { get; set; }

    public double? BestPerformerOpenRate { get; set; }
}

/// <summary>
/// Results of broadcast trend analysis.
/// </summary>
public sealed class BroadcastTrendResult
{
    public int Days { get; set; }

    public string GroupBy { get; set; } = "month";

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public BroadcastTrendPeriod[] Periods { get; set; } = [];

    public int TotalBroadcasts { get; set; }

    public int TotalRecipients { get; set; }

    public double OverallAverageOpenRate { get; set; }

    public double OverallAverageClickRate { get; set; }

    /// <summary>
    /// Trend direction for open rates: "improving", "declining", or "stable"
    /// </summary>
    public string OpenRateTrend { get; set; } = "stable";

    /// <summary>
    /// Trend direction for click rates: "improving", "declining", or "stable"
    /// </summary>
    public string ClickRateTrend { get; set; } = "stable";

    /// <summary>
    /// Month-over-month or week-over-week change percentage for open rate.
    /// </summary>
    public double OpenRateChange { get; set; }

    /// <summary>
    /// Month-over-month or week-over-week change percentage for click rate.
    /// </summary>
    public double ClickRateChange { get; set; }
}

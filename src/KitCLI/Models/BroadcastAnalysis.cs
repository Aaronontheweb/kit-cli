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

    public int UniqueOpens { get; set; }

    public int TotalOpens { get; set; }

    public int UniqueClicks { get; set; }

    public int TotalClicks { get; set; }

    public int Unsubscribes { get; set; }

    public int Bounces { get; set; }

    public int Complaints { get; set; }

    public double OpenRate { get; set; }

    public double ClickRate { get; set; }

    public double ClickToOpenRate { get; set; }
}

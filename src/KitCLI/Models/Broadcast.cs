using System.Text.Json.Serialization;

namespace KitCLI.Models;

public sealed class Broadcast
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("preview_text")]
    public string? PreviewText { get; set; }

    [JsonPropertyName("from_name")]
    public string? FromName { get; set; }

    [JsonPropertyName("from_email")]
    public string? FromEmail { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("public")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime? PublishedAt { get; set; }

    [JsonPropertyName("send_at")]
    public DateTime? SendAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("subscriber_filter")]
    public SubscriberFilterGroup[]? SubscriberFilter { get; set; }

    [JsonIgnore]
    public string Status
    {
        get
        {
            if (SendAt == null)
            {
                return "draft";
            }

            if (SendAt > DateTime.UtcNow)
            {
                return "scheduled";
            }

            return "sent";
        }
    }
}

public sealed class BroadcastContent
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_layout_template")]
    public string? EmailLayoutTemplate { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// A group of filter criteria for targeting broadcast subscribers.
/// Only one of all, any, or none should be used per filter group.
/// </summary>
public sealed class SubscriberFilterGroup
{
    /// <summary>
    /// Subscribers must match ALL of these criteria.
    /// </summary>
    [JsonPropertyName("all")]
    public FilterCriteria[]? All { get; set; }

    /// <summary>
    /// Subscribers must match ANY of these criteria.
    /// </summary>
    [JsonPropertyName("any")]
    public FilterCriteria[]? Any { get; set; }

    /// <summary>
    /// Subscribers must match NONE of these criteria (exclusion).
    /// </summary>
    [JsonPropertyName("none")]
    public FilterCriteria[]? None { get; set; }
}

/// <summary>
/// A single filter criterion specifying a type (segment or tag) and IDs to match.
/// </summary>
public sealed class FilterCriteria
{
    /// <summary>
    /// The type of filter: "segment" or "tag"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The IDs of segments or tags to match.
    /// </summary>
    [JsonPropertyName("ids")]
    public long[]? Ids { get; set; }
}

/// <summary>
/// Kit V4 API response wrapper for broadcast link clicks.
/// Response format: {"broadcast": {"id": ..., "clicks": [...]}, "pagination": {...}}
/// </summary>
public sealed class BroadcastClicksResponse
{
    [JsonPropertyName("broadcast")]
    public BroadcastWithClicks? Broadcast { get; set; }

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

/// <summary>
/// Broadcast wrapper containing the clicks array.
/// </summary>
public sealed class BroadcastWithClicks
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("clicks")]
    public LinkClick[] Clicks { get; set; } = [];
}

/// <summary>
/// Individual link click data for a broadcast.
/// </summary>
public sealed class LinkClick
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("unique_clicks")]
    public int UniqueClicks { get; set; }

    [JsonPropertyName("click_to_delivery_rate")]
    public double ClickToDeliveryRate { get; set; }

    [JsonPropertyName("click_to_open_rate")]
    public double ClickToOpenRate { get; set; }
}

/// <summary>
/// Kit V4 API response wrapper for broadcast stats.
/// Response format: {"broadcast": {"id": ..., "stats": {...}}}
/// </summary>
public sealed class BroadcastStatsResponse
{
    [JsonPropertyName("broadcast")]
    public BroadcastWithStats? Broadcast { get; set; }
}

/// <summary>
/// Broadcast wrapper containing the stats object.
/// </summary>
public sealed class BroadcastWithStats
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("stats")]
    public BroadcastStats? Stats { get; set; }
}

/// <summary>
/// Statistics for a broadcast email.
/// </summary>
public sealed class BroadcastStats
{
    [JsonPropertyName("recipients")]
    public int Recipients { get; set; }

    [JsonPropertyName("open_rate")]
    public double OpenRate { get; set; }

    [JsonPropertyName("click_rate")]
    public double ClickRate { get; set; }

    [JsonPropertyName("emails_opened")]
    public int EmailsOpened { get; set; }

    [JsonPropertyName("total_clicks")]
    public int TotalClicks { get; set; }

    [JsonPropertyName("unsubscribes")]
    public int Unsubscribes { get; set; }

    [JsonPropertyName("unsubscribe_rate")]
    public double UnsubscribeRate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    [JsonPropertyName("open_tracking_disabled")]
    public bool OpenTrackingDisabled { get; set; }

    [JsonPropertyName("click_tracking_disabled")]
    public bool ClickTrackingDisabled { get; set; }

    /// <summary>
    /// Click-to-open rate calculated from emails opened and total clicks.
    /// </summary>
    [JsonIgnore]
    public double ClickToOpenRate => EmailsOpened > 0 ? (double)TotalClicks / EmailsOpened : 0.0;
}

/// <summary>
/// A single broadcast item in a ranked top broadcasts list.
/// </summary>
public sealed class TopBroadcastItem
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("send_at")]
    public DateTime? SendAt { get; set; }

    [JsonPropertyName("recipients")]
    public int Recipients { get; set; }

    [JsonPropertyName("open_rate")]
    public double OpenRate { get; set; }

    [JsonPropertyName("click_rate")]
    public double ClickRate { get; set; }

    [JsonPropertyName("click_to_open_rate")]
    public double ClickToOpenRate { get; set; }

    [JsonPropertyName("engagement_score")]
    public double EngagementScore { get; set; }
}

/// <summary>
/// Result of a 'top broadcasts' analysis.
/// </summary>
public sealed class TopBroadcastsResult
{
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    [JsonPropertyName("metric_label")]
    public string MetricLabel { get; set; } = string.Empty;

    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total_analyzed")]
    public int TotalAnalyzed { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("top_broadcasts")]
    public TopBroadcastItem[] TopBroadcasts { get; set; } = [];
}

using System.Text.Json.Serialization;

namespace KitCLI.Models;

public sealed class Sequence
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("hold")]
    public bool Hold { get; set; }
    
    [JsonPropertyName("repeat")]
    public bool Repeat { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("subscriber_count")]
    public int SubscriberCount { get; set; }
    
    [JsonPropertyName("email_count")]
    public int EmailCount { get; set; }
    
    [JsonPropertyName("is_visual")]
    public bool IsVisual { get; set; }
    
    [JsonPropertyName("excluded_tags")]
    public Tag[]? ExcludedTags { get; set; }
    
    [JsonPropertyName("included_tags")]
    public Tag[]? IncludedTags { get; set; }
}

public sealed class SequenceEmail
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }
    
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;
    
    [JsonPropertyName("from_name")]
    public string? FromName { get; set; }
    
    [JsonPropertyName("from_email")]
    public string? FromEmail { get; set; }
    
    [JsonPropertyName("preview_text")]
    public string? PreviewText { get; set; }
    
    [JsonPropertyName("delay_days")]
    public int DelayDays { get; set; }
    
    [JsonPropertyName("delay_hours")]
    public int DelayHours { get; set; }
    
    [JsonPropertyName("delay_minutes")]
    public int DelayMinutes { get; set; }
    
    [JsonPropertyName("position")]
    public int Position { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonPropertyName("total_clicks")]
    public int TotalClicks { get; set; }
    
    [JsonPropertyName("unique_clicks")]
    public int UniqueClicks { get; set; }
    
    [JsonPropertyName("total_opens")]
    public int TotalOpens { get; set; }
    
    [JsonPropertyName("unique_opens")]
    public int UniqueOpens { get; set; }
    
    [JsonPropertyName("total_unsubscribes")]
    public int TotalUnsubscribes { get; set; }
    
    [JsonPropertyName("total_recipients")]
    public int TotalRecipients { get; set; }
    
    [JsonPropertyName("open_rate")]
    public double OpenRate { get; set; }
    
    [JsonPropertyName("click_rate")]
    public double ClickRate { get; set; }
    
    [JsonPropertyName("unsubscribe_rate")]
    public double UnsubscribeRate { get; set; }
    
    [JsonIgnore]
    public int TotalDelayMinutes => (DelayDays * 24 * 60) + (DelayHours * 60) + DelayMinutes;
    
    [JsonIgnore]
    public string DelayFormatted
    {
        get
        {
            var parts = new List<string>();
            if (DelayDays > 0) parts.Add($"{DelayDays}d");
            if (DelayHours > 0) parts.Add($"{DelayHours}h");
            if (DelayMinutes > 0) parts.Add($"{DelayMinutes}m");
            return parts.Count > 0 ? string.Join(" ", parts) : "Immediately";
        }
    }
}

public sealed class SequenceSubscriber
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("subscriber_id")]
    public long SubscriberId { get; set; }
    
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }
    
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    [JsonPropertyName("next_email_at")]
    public DateTimeOffset? NextEmailAt { get; set; }
    
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }
    
    [JsonIgnore]
    public bool IsActive => State.Equals("active", StringComparison.OrdinalIgnoreCase);
    
    [JsonIgnore]
    public bool IsCompleted => CompletedAt.HasValue;
}

public sealed class SequenceStats
{
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }
    
    [JsonPropertyName("total_subscribers")]
    public int TotalSubscribers { get; set; }
    
    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }
    
    [JsonPropertyName("completed_subscribers")]
    public int CompletedSubscribers { get; set; }
    
    [JsonPropertyName("cancelled_subscribers")]
    public int CancelledSubscribers { get; set; }
    
    [JsonPropertyName("average_open_rate")]
    public double AverageOpenRate { get; set; }
    
    [JsonPropertyName("average_click_rate")]
    public double AverageClickRate { get; set; }
    
    [JsonPropertyName("completion_rate")]
    public double CompletionRate { get; set; }
    
    [JsonPropertyName("emails_sent")]
    public int EmailsSent { get; set; }
}
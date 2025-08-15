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
    public BroadcastContent? Content { get; set; }
    
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
    public SubscriberFilter? SubscriberFilter { get; set; }
    
    [JsonIgnore]
    public string Status
    {
        get
        {
            if (SendAt == null) return "draft";
            if (SendAt > DateTime.UtcNow) return "scheduled";
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

public sealed class SubscriberFilter
{
    [JsonPropertyName("all")]
    public bool All { get; set; }
    
    [JsonPropertyName("segment_ids")]
    public long[]? SegmentIds { get; set; }
    
    [JsonPropertyName("tag_ids")]
    public long[]? TagIds { get; set; }
    
    [JsonPropertyName("none")]
    public bool None { get; set; }
}

public sealed class BroadcastStats
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("broadcast_id")]
    public long BroadcastId { get; set; }
    
    [JsonPropertyName("recipients")]
    public int Recipients { get; set; }
    
    [JsonPropertyName("open_rate")]
    public double OpenRate { get; set; }
    
    [JsonPropertyName("click_rate")]
    public double ClickRate { get; set; }
    
    [JsonPropertyName("opens")]
    public int Opens { get; set; }
    
    [JsonPropertyName("unique_opens")]
    public int UniqueOpens { get; set; }
    
    [JsonPropertyName("clicks")]
    public int Clicks { get; set; }
    
    [JsonPropertyName("unique_clicks")]
    public int UniqueClicks { get; set; }
    
    [JsonPropertyName("unsubscribes")]
    public int Unsubscribes { get; set; }
    
    [JsonPropertyName("bounces")]
    public int Bounces { get; set; }
    
    [JsonPropertyName("complaints")]
    public int Complaints { get; set; }
}
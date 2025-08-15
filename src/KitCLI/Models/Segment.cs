using System.Text.Json.Serialization;

namespace KitCLI.Models;

public sealed class Segment
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("subscriber_count")]
    public int SubscriberCount { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
    
    [JsonPropertyName("filters")]
    public SegmentFilter[]? Filters { get; set; }
    
    [JsonPropertyName("is_processing")]
    public bool IsProcessing { get; set; }
    
    [JsonPropertyName("last_processed_at")]
    public DateTimeOffset? LastProcessedAt { get; set; }
}

public sealed class SegmentFilter
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
    
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
    
    [JsonPropertyName("group")]
    public string? Group { get; set; }
}

public sealed class SegmentSubscriberRequest
{
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;
    
    [JsonPropertyName("subscriber_ids")]
    public long[]? SubscriberIds { get; set; }
}

public sealed class SegmentCreateRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("filters")]
    public SegmentFilter[]? Filters { get; set; }
}
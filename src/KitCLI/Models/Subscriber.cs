using System.Text.Json.Serialization;

namespace KitCLI.Models;

public sealed class Subscriber
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = "active"; // active, cancelled, bounced, complained, inactive

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("fields")]
    public Dictionary<string, object>? Fields { get; set; }

    [JsonPropertyName("tags")]
    public Tag[]? Tags { get; set; }

    [JsonIgnore]
    public string DisplayName => FirstName ?? EmailAddress.Split('@')[0];

    [JsonIgnore]
    public string TagList => Tags != null ? string.Join(", ", Tags.Select(t => t.Name)) : string.Empty;
}

public sealed class Tag
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}

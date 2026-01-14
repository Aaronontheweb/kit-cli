using System.Text.Json;
using System.Text.Json.Serialization;

namespace KitCLI.Models;

/// <summary>
/// Request body for creating a new subscriber.
/// </summary>
public sealed class SubscriberCreateRequest
{
    /// <summary>
    /// The subscriber's email address (required).
    /// </summary>
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// The subscriber's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// The subscriber's state (active, cancelled, bounced, complained, inactive).
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Custom field values for the subscriber.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, object>? Fields { get; set; }
}

/// <summary>
/// Request body for updating an existing subscriber.
/// Only non-null fields will be updated.
/// </summary>
public sealed class SubscriberUpdateRequest
{
    /// <summary>
    /// The subscriber's email address.
    /// </summary>
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// The subscriber's first name.
    /// </summary>
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    /// <summary>
    /// Custom field values for the subscriber.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, object>? Fields { get; set; }
}

/// <summary>
/// Request body for tagging a subscriber.
/// </summary>
public sealed class TagSubscriberRequest
{
    /// <summary>
    /// The subscriber's email address.
    /// </summary>
    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;
}

/// <summary>
/// Request body for creating a new tag.
/// </summary>
public sealed class TagCreateRequest
{
    /// <summary>
    /// The name of the tag.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response wrapper for a single tag.
/// </summary>
public sealed class TagResponse
{
    [JsonPropertyName("tag")]
    public Tag? Tag { get; set; }
}

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
    public Dictionary<string, JsonElement>? Fields { get; set; }

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

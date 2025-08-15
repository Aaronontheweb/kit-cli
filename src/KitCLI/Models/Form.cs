using System.Text.Json.Serialization;

namespace KitCLI.Models;

/// <summary>
/// Represents a Kit form (landing page/opt-in form)
/// </summary>
public sealed class Form
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("embed_js")]
    public string? EmbedJs { get; set; }

    [JsonPropertyName("embed_url")]
    public string? EmbedUrl { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Total number of subscriptions to this form
    /// </summary>
    [JsonPropertyName("total_subscriptions")]
    public int TotalSubscriptions { get; set; }

    /// <summary>
    /// Redirect URL after form submission
    /// </summary>
    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; set; }

    /// <summary>
    /// Success message displayed after form submission
    /// </summary>
    [JsonPropertyName("success_message")]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Description or purpose of the form
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Sign-up incentive/lead magnet details
    /// </summary>
    [JsonPropertyName("incentive_email")]
    public IncentiveEmail? IncentiveEmail { get; set; }
}

/// <summary>
/// Represents an incentive email (lead magnet) configuration
/// </summary>
public sealed class IncentiveEmail
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

/// <summary>
/// Response wrapper for paginated form lists
/// </summary>
public sealed class FormsResponse
{
    [JsonPropertyName("forms")]
    public Form[] Forms { get; set; } = Array.Empty<Form>();

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

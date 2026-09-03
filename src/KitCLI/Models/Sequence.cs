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

    [JsonPropertyName("subscriber_count")]
    public int SubscriberCount { get; set; }

    [JsonPropertyName("email_count")]
    public int EmailCount { get; set; }

    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("email_template_id")]
    public long? EmailTemplateId { get; set; }

    [JsonPropertyName("send_days")]
    public string[]? SendDays { get; set; }

    [JsonPropertyName("send_hour")]
    public int? SendHour { get; set; }

    [JsonPropertyName("time_zone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("exclude_subscriber_sources")]
    public string[]? ExcludeSubscriberSources { get; set; }
}

public sealed class SequenceEmail
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("preview_text")]
    public string? PreviewText { get; set; }

    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;

    [JsonPropertyName("email_template_id")]
    public long? EmailTemplateId { get; set; }

    [JsonPropertyName("published")]
    public bool Published { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("delay_value")]
    public int DelayValue { get; set; }

    [JsonPropertyName("delay_unit")]
    public string DelayUnit { get; set; } = "days";

    [JsonPropertyName("send_days")]
    public string[]? SendDays { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("stats")]
    public SequenceEmailStats? Stats { get; set; }

    [JsonIgnore]
    public string DelayFormatted
    {
        get
        {
            if (DelayValue <= 0)
            {
                return "Immediately";
            }

            return DelayUnit.ToLowerInvariant() switch
            {
                "hours" => $"{DelayValue}h",
                "days" => $"{DelayValue}d",
                _ => $"{DelayValue} {DelayUnit}"
            };
        }
    }

    [JsonIgnore]
    public string SendDaysFormatted => SendDays is { Length: > 0 } ? string.Join(", ", SendDays) : "Every day";
}

/// <summary>
/// Field-scoped update body for PUT /v4/sequences/{id}/emails/{email_id}.
/// By construction it carries exactly one of <see cref="Subject"/> or <see cref="Content"/>,
/// and (because the JSON context omits null values) serializes to exactly
/// <c>{"subject":...}</c> or <c>{"content":...}</c>. It can never transmit position,
/// published, delay, send_days, template, or preview fields. This is a serialization-only
/// type and is never deserialized.
/// </summary>
public sealed class SequenceEmailUpdateRequest
{
    [JsonPropertyName("subject")]
    public string? Subject { get; private init; }

    [JsonPropertyName("content")]
    public string? Content { get; private init; }

    private SequenceEmailUpdateRequest()
    {
    }

    public static SequenceEmailUpdateRequest ForSubject(string subject) => new() { Subject = subject };

    public static SequenceEmailUpdateRequest ForContent(string content) => new() { Content = content };
}

/// <summary>
/// Machine-readable report for a <c>sequence email update</c> operation (emitted with --format json).
/// Body content is never included verbatim; only byte counts and SHA-256 fingerprints are reported.
/// </summary>
public sealed class SequenceEmailUpdateReport
{
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }

    [JsonPropertyName("email_id")]
    public long EmailId { get; set; }

    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("changed")]
    public bool Changed { get; set; }

    [JsonPropertyName("applied")]
    public bool Applied { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("subject_before")]
    public string? SubjectBefore { get; set; }

    [JsonPropertyName("subject_after")]
    public string? SubjectAfter { get; set; }

    [JsonPropertyName("content_bytes_before")]
    public int? ContentBytesBefore { get; set; }

    [JsonPropertyName("content_bytes_after")]
    public int? ContentBytesAfter { get; set; }

    [JsonPropertyName("content_sha256_before")]
    public string? ContentSha256Before { get; set; }

    [JsonPropertyName("content_sha256_after")]
    public string? ContentSha256After { get; set; }
}

public sealed class SequenceEmailStats
{
    [JsonPropertyName("recipients")]
    public int Recipients { get; set; }

    [JsonPropertyName("opens")]
    public int Opens { get; set; }

    [JsonPropertyName("clicks")]
    public int Clicks { get; set; }

    [JsonPropertyName("email_unsubscribes")]
    public int EmailUnsubscribes { get; set; }

    [JsonPropertyName("bounces")]
    public int Bounces { get; set; }

    [JsonPropertyName("complaints")]
    public int Complaints { get; set; }

    [JsonPropertyName("open_rate")]
    public double OpenRate { get; set; }

    [JsonPropertyName("click_rate")]
    public double ClickRate { get; set; }

    [JsonPropertyName("click_to_open_rate")]
    public double ClickToOpenRate { get; set; }

    [JsonPropertyName("unsubscribe_rate")]
    public double UnsubscribeRate { get; set; }

    [JsonPropertyName("bounce_rate")]
    public double BounceRate { get; set; }

    [JsonPropertyName("complaint_rate")]
    public double ComplaintRate { get; set; }
}

public sealed class SequenceSubscriber
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("email_address")]
    public string EmailAddress { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("added_at")]
    public DateTimeOffset AddedAt { get; set; }

    [JsonIgnore]
    public bool IsActive => State.Equals("active", StringComparison.OrdinalIgnoreCase);
}

public sealed class SequenceStats
{
    [JsonPropertyName("sequence_id")]
    public long SequenceId { get; set; }

    [JsonPropertyName("total_subscribers")]
    public int TotalSubscribers { get; set; }

    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    [JsonPropertyName("cancelled_subscribers")]
    public int CancelledSubscribers { get; set; }

    [JsonPropertyName("average_open_rate")]
    public double AverageOpenRate { get; set; }

    [JsonPropertyName("average_click_rate")]
    public double AverageClickRate { get; set; }

    [JsonPropertyName("emails_sent")]
    public int EmailsSent { get; set; }
}

internal readonly record struct SequenceEmailPerformance(
    int Recipients,
    int Opens,
    int Clicks,
    double OpenRate,
    double ClickRate);

internal static class SequenceEmailMetrics
{
    public static SequenceEmailPerformance Aggregate(IEnumerable<SequenceEmail> emails)
    {
        var emailsWithStats = emails.Where(email => email.Stats is not null).ToArray();
        var totalRecipients = emailsWithStats.Sum(email => email.Stats!.Recipients);
        var totalOpens = emailsWithStats.Sum(email => email.Stats!.Opens);
        var totalClicks = emailsWithStats.Sum(email => email.Stats!.Clicks);

        if (totalRecipients == 0)
        {
            return new SequenceEmailPerformance(totalRecipients, totalOpens, totalClicks, 0, 0);
        }

        var weightedOpenRate = emailsWithStats.Sum(email => email.Stats!.OpenRate * email.Stats.Recipients) / totalRecipients;
        var weightedClickRate = emailsWithStats.Sum(email => email.Stats!.ClickRate * email.Stats.Recipients) / totalRecipients;

        return new SequenceEmailPerformance(
            totalRecipients,
            totalOpens,
            totalClicks,
            weightedOpenRate,
            weightedClickRate);
    }
}

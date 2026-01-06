using System.Text.Json.Serialization;

namespace KitCLI.Models;

/// <summary>
/// Represents a cohort of subscribers grouped by signup period.
/// </summary>
public sealed class SignupCohort
{
    /// <summary>
    /// Human-readable period label (e.g., "Jan 2025", "Q1 2025", "Week 1 2025")
    /// </summary>
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Start date of the cohort period
    /// </summary>
    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of the cohort period
    /// </summary>
    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total subscribers who signed up during this period
    /// </summary>
    [JsonPropertyName("total_subscribers")]
    public int TotalSubscribers { get; set; }

    /// <summary>
    /// Currently active subscribers from this cohort
    /// </summary>
    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    /// <summary>
    /// Subscribers who have cancelled from this cohort
    /// </summary>
    [JsonPropertyName("cancelled_subscribers")]
    public int CancelledSubscribers { get; set; }

    /// <summary>
    /// Current retention rate (active / total) as percentage 0-100
    /// </summary>
    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }

    /// <summary>
    /// Age of this cohort in days
    /// </summary>
    [JsonPropertyName("age_days")]
    public int AgeDays { get; set; }

    /// <summary>
    /// Metrics at different age intervals for this cohort
    /// </summary>
    [JsonPropertyName("metrics_by_age")]
    public CohortAgeMetric[] MetricsByAge { get; set; } = [];
}

/// <summary>
/// Metrics for a cohort at a specific age interval.
/// </summary>
public sealed class CohortAgeMetric
{
    /// <summary>
    /// Human-readable age label (e.g., "Month 1", "Month 3", "Week 4")
    /// </summary>
    [JsonPropertyName("age_label")]
    public string AgeLabel { get; set; } = string.Empty;

    /// <summary>
    /// Age in days when this metric was calculated
    /// </summary>
    [JsonPropertyName("days_old")]
    public int DaysOld { get; set; }

    /// <summary>
    /// Retention rate at this age as percentage 0-100
    /// </summary>
    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }

    /// <summary>
    /// Number of active subscribers at this age
    /// </summary>
    [JsonPropertyName("active_count")]
    public int ActiveCount { get; set; }

    /// <summary>
    /// Whether this age point is in the future for this cohort
    /// </summary>
    [JsonPropertyName("not_yet_reached")]
    public bool NotYetReached { get; set; }
}

/// <summary>
/// Complete result of a cohort analysis.
/// </summary>
public sealed class CohortAnalysisResult
{
    /// <summary>
    /// Type of analysis performed (e.g., "by-signup")
    /// </summary>
    [JsonPropertyName("analysis_type")]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// Period grouping used (e.g., "monthly", "quarterly", "weekly")
    /// </summary>
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Metric analyzed (e.g., "retention", "engagement")
    /// </summary>
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Number of days analyzed
    /// </summary>
    [JsonPropertyName("lookback_days")]
    public int LookbackDays { get; set; }

    /// <summary>
    /// Total subscribers analyzed across all cohorts
    /// </summary>
    [JsonPropertyName("total_subscribers_analyzed")]
    public int TotalSubscribersAnalyzed { get; set; }

    /// <summary>
    /// The cohorts with their analysis data
    /// </summary>
    [JsonPropertyName("cohorts")]
    public SignupCohort[] Cohorts { get; set; } = [];

    /// <summary>
    /// Auto-generated insight about the data
    /// </summary>
    [JsonPropertyName("insight")]
    public string Insight { get; set; } = string.Empty;

    /// <summary>
    /// Average retention rate across all cohorts
    /// </summary>
    [JsonPropertyName("average_retention_rate")]
    public double AverageRetentionRate { get; set; }

    /// <summary>
    /// Estimated "half-life" - when retention drops to ~50%
    /// </summary>
    [JsonPropertyName("half_life_days")]
    public int? HalfLifeDays { get; set; }
}

/// <summary>
/// Represents a cohort of subscribers grouped by tag.
/// </summary>
public sealed class TagCohort
{
    /// <summary>
    /// The tag ID
    /// </summary>
    [JsonPropertyName("tag_id")]
    public long TagId { get; set; }

    /// <summary>
    /// The tag name
    /// </summary>
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Total subscribers with this tag
    /// </summary>
    [JsonPropertyName("total_subscribers")]
    public int TotalSubscribers { get; set; }

    /// <summary>
    /// Currently active subscribers with this tag
    /// </summary>
    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    /// <summary>
    /// Subscribers who have cancelled with this tag
    /// </summary>
    [JsonPropertyName("cancelled_subscribers")]
    public int CancelledSubscribers { get; set; }

    /// <summary>
    /// Retention rate (active / total) as percentage 0-100
    /// </summary>
    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }
}

/// <summary>
/// Complete result of a tag-based cohort analysis.
/// </summary>
public sealed class TagCohortAnalysisResult
{
    /// <summary>
    /// Type of analysis performed
    /// </summary>
    [JsonPropertyName("analysis_type")]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// Number of tags analyzed
    /// </summary>
    [JsonPropertyName("tag_count")]
    public int TagCount { get; set; }

    /// <summary>
    /// Total subscribers analyzed across all tags
    /// </summary>
    [JsonPropertyName("total_subscribers_analyzed")]
    public int TotalSubscribersAnalyzed { get; set; }

    /// <summary>
    /// The tag cohorts with their analysis data
    /// </summary>
    [JsonPropertyName("cohorts")]
    public TagCohort[] Cohorts { get; set; } = [];

    /// <summary>
    /// Auto-generated insight about the data
    /// </summary>
    [JsonPropertyName("insight")]
    public string Insight { get; set; } = string.Empty;
}

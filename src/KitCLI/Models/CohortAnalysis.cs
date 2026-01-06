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

/// <summary>
/// Represents a cohort of subscribers grouped by signup form.
/// </summary>
public sealed class FormCohort
{
    /// <summary>
    /// The form ID
    /// </summary>
    [JsonPropertyName("form_id")]
    public long FormId { get; set; }

    /// <summary>
    /// The form name
    /// </summary>
    [JsonPropertyName("form_name")]
    public string FormName { get; set; } = string.Empty;

    /// <summary>
    /// The form type (e.g., "embed", "hosted", "modal")
    /// </summary>
    [JsonPropertyName("form_type")]
    public string FormType { get; set; } = string.Empty;

    /// <summary>
    /// Total subscribers from this form
    /// </summary>
    [JsonPropertyName("total_subscribers")]
    public int TotalSubscribers { get; set; }

    /// <summary>
    /// Currently active subscribers from this form
    /// </summary>
    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    /// <summary>
    /// Subscribers who have cancelled from this form
    /// </summary>
    [JsonPropertyName("cancelled_subscribers")]
    public int CancelledSubscribers { get; set; }

    /// <summary>
    /// Subscribers who bounced from this form
    /// </summary>
    [JsonPropertyName("bounced_subscribers")]
    public int BouncedSubscribers { get; set; }

    /// <summary>
    /// Subscribers who complained from this form
    /// </summary>
    [JsonPropertyName("complained_subscribers")]
    public int ComplainedSubscribers { get; set; }

    /// <summary>
    /// Retention rate (active / total) as percentage 0-100
    /// </summary>
    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }

    /// <summary>
    /// Subscribers who signed up in the last 30 days
    /// </summary>
    [JsonPropertyName("recent_signups_30d")]
    public int RecentSignups30d { get; set; }

    /// <summary>
    /// Subscribers who signed up in the last 90 days
    /// </summary>
    [JsonPropertyName("recent_signups_90d")]
    public int RecentSignups90d { get; set; }

    /// <summary>
    /// 90-day retention: % of subscribers from 90+ days ago still active
    /// </summary>
    [JsonPropertyName("retention_90d")]
    public double? Retention90d { get; set; }

    /// <summary>
    /// Whether the form is archived
    /// </summary>
    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    /// <summary>
    /// When the form was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Complete result of a form-based cohort analysis.
/// </summary>
public sealed class FormCohortAnalysisResult
{
    /// <summary>
    /// Type of analysis performed
    /// </summary>
    [JsonPropertyName("analysis_type")]
    public string AnalysisType { get; set; } = string.Empty;

    /// <summary>
    /// Number of forms analyzed
    /// </summary>
    [JsonPropertyName("form_count")]
    public int FormCount { get; set; }

    /// <summary>
    /// Total subscribers analyzed across all forms
    /// </summary>
    [JsonPropertyName("total_subscribers_analyzed")]
    public int TotalSubscribersAnalyzed { get; set; }

    /// <summary>
    /// Number of days analyzed
    /// </summary>
    [JsonPropertyName("lookback_days")]
    public int LookbackDays { get; set; }

    /// <summary>
    /// The form cohorts with their analysis data
    /// </summary>
    [JsonPropertyName("cohorts")]
    public FormCohort[] Cohorts { get; set; } = [];

    /// <summary>
    /// Auto-generated insight about the data
    /// </summary>
    [JsonPropertyName("insight")]
    public string Insight { get; set; } = string.Empty;

    /// <summary>
    /// Average retention rate across all forms
    /// </summary>
    [JsonPropertyName("average_retention_rate")]
    public double AverageRetentionRate { get; set; }

    /// <summary>
    /// Best performing form by retention
    /// </summary>
    [JsonPropertyName("best_form")]
    public string? BestForm { get; set; }

    /// <summary>
    /// Worst performing form by retention
    /// </summary>
    [JsonPropertyName("worst_form")]
    public string? WorstForm { get; set; }
}

/// <summary>
/// Represents a form with its comparison metrics.
/// </summary>
public sealed class FormComparisonItem
{
    [JsonPropertyName("form_id")]
    public long FormId { get; set; }

    [JsonPropertyName("form_name")]
    public string FormName { get; set; } = string.Empty;

    [JsonPropertyName("form_type")]
    public string FormType { get; set; } = string.Empty;

    [JsonPropertyName("total_subscribers")]
    public int TotalSubscribers { get; set; }

    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    [JsonPropertyName("cancelled_subscribers")]
    public int CancelledSubscribers { get; set; }

    [JsonPropertyName("bounced_subscribers")]
    public int BouncedSubscribers { get; set; }

    [JsonPropertyName("complained_subscribers")]
    public int ComplainedSubscribers { get; set; }

    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }

    [JsonPropertyName("signups_30d")]
    public int Signups30d { get; set; }

    [JsonPropertyName("signups_90d")]
    public int Signups90d { get; set; }

    [JsonPropertyName("daily_average")]
    public double DailyAverage { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("age_days")]
    public int AgeDays { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }
}

/// <summary>
/// Complete result of a form comparison.
/// </summary>
public sealed class FormComparisonResult
{
    [JsonPropertyName("forms")]
    public FormComparisonItem[] Forms { get; set; } = [];

    [JsonPropertyName("winner_form_id")]
    public long? WinnerFormId { get; set; }

    [JsonPropertyName("winner_form_name")]
    public string? WinnerFormName { get; set; }

    [JsonPropertyName("winner_reason")]
    public string? WinnerReason { get; set; }
}

/// <summary>
/// Trend data for a single period (day, week, or month).
/// </summary>
public sealed class FormTrendPeriod
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("signups")]
    public int Signups { get; set; }

    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }
}

/// <summary>
/// Trend data for a single form.
/// </summary>
public sealed class FormTrendData
{
    [JsonPropertyName("form_id")]
    public long FormId { get; set; }

    [JsonPropertyName("form_name")]
    public string FormName { get; set; } = string.Empty;

    [JsonPropertyName("form_type")]
    public string FormType { get; set; } = string.Empty;

    [JsonPropertyName("total_signups")]
    public int TotalSignups { get; set; }

    [JsonPropertyName("active_subscribers")]
    public int ActiveSubscribers { get; set; }

    [JsonPropertyName("retention_rate")]
    public double RetentionRate { get; set; }

    [JsonPropertyName("average_daily_signups")]
    public double AverageDailySignups { get; set; }

    [JsonPropertyName("trend_direction")]
    public string TrendDirection { get; set; } = "stable";

    [JsonPropertyName("trend_change_percent")]
    public double TrendChangePercent { get; set; }

    [JsonPropertyName("periods")]
    public FormTrendPeriod[] Periods { get; set; } = [];
}

/// <summary>
/// Complete result of form trend analysis.
/// </summary>
public sealed class FormTrendResult
{
    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("group_by")]
    public string GroupBy { get; set; } = "monthly";

    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("total_signups")]
    public int TotalSignups { get; set; }

    [JsonPropertyName("forms")]
    public FormTrendData[] Forms { get; set; } = [];

    [JsonPropertyName("overall_trend")]
    public string OverallTrend { get; set; } = "stable";

    [JsonPropertyName("best_performing_form")]
    public string? BestPerformingForm { get; set; }

    [JsonPropertyName("best_performing_form_id")]
    public long? BestPerformingFormId { get; set; }
}

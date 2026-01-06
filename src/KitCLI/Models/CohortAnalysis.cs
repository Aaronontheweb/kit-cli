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

/// <summary>
/// A scored subscriber with engagement metrics based on available data.
/// Note: Kit v4 API does not provide per-subscriber engagement metrics (opens, clicks),
/// so scoring is based on available signals: account age, tag engagement, and state.
/// </summary>
public sealed class ScoredSubscriber
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of tags associated with this subscriber
    /// </summary>
    [JsonPropertyName("tag_count")]
    public int TagCount { get; set; }

    /// <summary>
    /// Comma-separated list of tag names
    /// </summary>
    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Age of subscriber account in days
    /// </summary>
    [JsonPropertyName("account_age_days")]
    public int AccountAgeDays { get; set; }

    /// <summary>
    /// Calculated engagement score (0-100)
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// Score breakdown by component
    /// </summary>
    [JsonPropertyName("score_breakdown")]
    public ScoreBreakdown? Breakdown { get; set; }
}

/// <summary>
/// Breakdown of how a subscriber's score was calculated.
/// </summary>
public sealed class ScoreBreakdown
{
    /// <summary>
    /// Points from tag engagement (more tags = higher engagement)
    /// </summary>
    [JsonPropertyName("tag_points")]
    public double TagPoints { get; set; }

    /// <summary>
    /// Points from account maturity (established subscribers)
    /// </summary>
    [JsonPropertyName("maturity_points")]
    public double MaturityPoints { get; set; }

    /// <summary>
    /// Points from subscriber state (active vs inactive)
    /// </summary>
    [JsonPropertyName("state_points")]
    public double StatePoints { get; set; }

    /// <summary>
    /// Total calculated score
    /// </summary>
    [JsonPropertyName("total")]
    public double Total { get; set; }
}

/// <summary>
/// Complete result of subscriber scoring analysis.
/// </summary>
public sealed class SubscriberScoresResult
{
    /// <summary>
    /// Algorithm used for scoring
    /// </summary>
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of scoring algorithm
    /// </summary>
    [JsonPropertyName("algorithm_description")]
    public string AlgorithmDescription { get; set; } = string.Empty;

    /// <summary>
    /// Total subscribers analyzed
    /// </summary>
    [JsonPropertyName("total_analyzed")]
    public int TotalAnalyzed { get; set; }

    /// <summary>
    /// Number of subscribers returned (limited)
    /// </summary>
    [JsonPropertyName("returned")]
    public int Returned { get; set; }

    /// <summary>
    /// Segment ID if filtering by segment
    /// </summary>
    [JsonPropertyName("segment_id")]
    public long? SegmentId { get; set; }

    /// <summary>
    /// Segment name if filtering by segment
    /// </summary>
    [JsonPropertyName("segment_name")]
    public string? SegmentName { get; set; }

    /// <summary>
    /// Average score across all analyzed subscribers
    /// </summary>
    [JsonPropertyName("average_score")]
    public double AverageScore { get; set; }

    /// <summary>
    /// Median score across all analyzed subscribers
    /// </summary>
    [JsonPropertyName("median_score")]
    public double MedianScore { get; set; }

    /// <summary>
    /// The top scored subscribers
    /// </summary>
    [JsonPropertyName("subscribers")]
    public ScoredSubscriber[] Subscribers { get; set; } = [];

    /// <summary>
    /// Note about API limitations
    /// </summary>
    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;
}

/// <summary>
/// A subscriber identified as "cold" (disengaged) based on available signals.
/// Note: Kit v4 API does not provide per-subscriber engagement metrics,
/// so "cold" is inferred from account age, tag count, and state.
/// </summary>
public sealed class ColdSubscriber
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Age of subscriber account in days
    /// </summary>
    [JsonPropertyName("account_age_days")]
    public int AccountAgeDays { get; set; }

    /// <summary>
    /// Number of tags associated with this subscriber
    /// </summary>
    [JsonPropertyName("tag_count")]
    public int TagCount { get; set; }

    /// <summary>
    /// Comma-separated list of tag names
    /// </summary>
    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    /// <summary>
    /// Engagement tier based on tag count: none, low, medium
    /// </summary>
    [JsonPropertyName("engagement_tier")]
    public string EngagementTier { get; set; } = string.Empty;

    /// <summary>
    /// Reason subscriber was flagged as cold
    /// </summary>
    [JsonPropertyName("cold_reason")]
    public string ColdReason { get; set; } = string.Empty;
}

/// <summary>
/// Complete result of cold subscriber analysis.
/// </summary>
public sealed class ColdSubscribersResult
{
    /// <summary>
    /// Minimum account age in days for "cold" classification
    /// </summary>
    [JsonPropertyName("min_days_old")]
    public int MinDaysOld { get; set; }

    /// <summary>
    /// Maximum tag count threshold for "cold" classification
    /// </summary>
    [JsonPropertyName("max_tags")]
    public int MaxTags { get; set; }

    /// <summary>
    /// Whether filtering to "was-active" (had some engagement before)
    /// </summary>
    [JsonPropertyName("was_active_filter")]
    public bool WasActiveFilter { get; set; }

    /// <summary>
    /// Total subscribers analyzed
    /// </summary>
    [JsonPropertyName("total_analyzed")]
    public int TotalAnalyzed { get; set; }

    /// <summary>
    /// Number of cold subscribers found
    /// </summary>
    [JsonPropertyName("cold_count")]
    public int ColdCount { get; set; }

    /// <summary>
    /// Percentage of subscribers that are cold
    /// </summary>
    [JsonPropertyName("cold_percentage")]
    public double ColdPercentage { get; set; }

    /// <summary>
    /// The cold subscribers
    /// </summary>
    [JsonPropertyName("subscribers")]
    public ColdSubscriber[] Subscribers { get; set; } = [];

    /// <summary>
    /// Breakdown by engagement tier
    /// </summary>
    [JsonPropertyName("tier_breakdown")]
    public ColdTierBreakdown? TierBreakdown { get; set; }

    /// <summary>
    /// Note about API limitations
    /// </summary>
    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;
}

/// <summary>
/// Breakdown of cold subscribers by their previous engagement tier.
/// </summary>
public sealed class ColdTierBreakdown
{
    /// <summary>
    /// Count of cold subscribers with zero engagement (no tags ever)
    /// </summary>
    [JsonPropertyName("never_engaged")]
    public int NeverEngaged { get; set; }

    /// <summary>
    /// Count of cold subscribers with low previous engagement (1-2 tags)
    /// </summary>
    [JsonPropertyName("previously_low")]
    public int PreviouslyLow { get; set; }

    /// <summary>
    /// Count of cold subscribers with medium previous engagement (3-5 tags)
    /// </summary>
    [JsonPropertyName("previously_medium")]
    public int PreviouslyMedium { get; set; }

    /// <summary>
    /// Count of cold subscribers with high previous engagement (6+ tags but still cold)
    /// </summary>
    [JsonPropertyName("previously_high")]
    public int PreviouslyHigh { get; set; }
}

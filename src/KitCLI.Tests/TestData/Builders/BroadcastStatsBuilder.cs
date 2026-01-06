using KitCLI.Models;

namespace KitCLI.Tests.TestData.Builders;

/// <summary>
/// Fluent builder for creating BroadcastStats test instances.
/// Updated for Kit V4 API which provides different fields than the old API.
/// </summary>
public sealed class BroadcastStatsBuilder
{
    private int _recipients = 1000;
    private double _openRate = 0.40; // 40%
    private double _clickRate = 0.10; // 10%
    private int _emailsOpened = 500;
    private int _totalClicks = 150;
    private int _unsubscribes = 5;
    private double _unsubscribeRate = 0.005;
    private string? _status = "completed";
    private double _progress = 1.0;
    private bool _openTrackingDisabled = false;
    private bool _clickTrackingDisabled = false;

    public BroadcastStatsBuilder WithRecipients(int recipients)
    {
        _recipients = recipients;
        return this;
    }

    public BroadcastStatsBuilder WithOpenRate(double openRate)
    {
        _openRate = openRate;
        return this;
    }

    public BroadcastStatsBuilder WithClickRate(double clickRate)
    {
        _clickRate = clickRate;
        return this;
    }

    public BroadcastStatsBuilder WithEmailsOpened(int emailsOpened)
    {
        _emailsOpened = emailsOpened;
        return this;
    }

    public BroadcastStatsBuilder WithTotalClicks(int totalClicks)
    {
        _totalClicks = totalClicks;
        return this;
    }

    public BroadcastStatsBuilder WithUnsubscribes(int unsubscribes)
    {
        _unsubscribes = unsubscribes;
        _unsubscribeRate = _recipients > 0 ? (double)unsubscribes / _recipients : 0;
        return this;
    }

    public BroadcastStatsBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public BroadcastStatsBuilder WithProgress(double progress)
    {
        _progress = progress;
        return this;
    }

    public BroadcastStatsBuilder WithOpenTrackingDisabled(bool disabled = true)
    {
        _openTrackingDisabled = disabled;
        return this;
    }

    public BroadcastStatsBuilder WithClickTrackingDisabled(bool disabled = true)
    {
        _clickTrackingDisabled = disabled;
        return this;
    }

    /// <summary>
    /// Create stats with zero engagement
    /// </summary>
    public BroadcastStatsBuilder WithNoEngagement()
    {
        _openRate = 0;
        _clickRate = 0;
        _emailsOpened = 0;
        _totalClicks = 0;
        return this;
    }

    /// <summary>
    /// Create stats with high engagement (45% open, 15% click)
    /// </summary>
    public BroadcastStatsBuilder WithHighEngagement()
    {
        _openRate = 0.45;
        _clickRate = 0.15;
        _emailsOpened = (int)(_recipients * 0.45 * 1.3); // some re-opens
        _totalClicks = (int)(_recipients * 0.15 * 1.2);
        return this;
    }

    /// <summary>
    /// Create stats with low engagement (15% open, 2% click)
    /// </summary>
    public BroadcastStatsBuilder WithLowEngagement()
    {
        _openRate = 0.15;
        _clickRate = 0.02;
        _emailsOpened = (int)(_recipients * 0.15 * 1.1);
        _totalClicks = (int)(_recipients * 0.02);
        return this;
    }

    /// <summary>
    /// Create stats with typical engagement (35% open, 8% click)
    /// </summary>
    public BroadcastStatsBuilder WithTypicalEngagement()
    {
        _openRate = 0.35;
        _clickRate = 0.08;
        _emailsOpened = (int)(_recipients * 0.35 * 1.2);
        _totalClicks = (int)(_recipients * 0.08 * 1.1);
        return this;
    }

    /// <summary>
    /// Set engagement by open and click percentages (0.0-1.0)
    /// </summary>
    public BroadcastStatsBuilder WithEngagement(double openRate, double clickRate)
    {
        _openRate = openRate;
        _clickRate = clickRate;
        _emailsOpened = (int)(_recipients * openRate * 1.2);
        _totalClicks = (int)(_recipients * clickRate * 1.1);
        return this;
    }

    public BroadcastStats Build()
    {
        return new BroadcastStats
        {
            Recipients = _recipients,
            OpenRate = _openRate,
            ClickRate = _clickRate,
            EmailsOpened = _emailsOpened,
            TotalClicks = _totalClicks,
            Unsubscribes = _unsubscribes,
            UnsubscribeRate = _unsubscribeRate,
            Status = _status,
            Progress = _progress,
            OpenTrackingDisabled = _openTrackingDisabled,
            ClickTrackingDisabled = _clickTrackingDisabled
        };
    }

    /// <summary>
    /// Build stats for a broadcast (convenient pairing)
    /// </summary>
    public static BroadcastStats ForBroadcast(Broadcast broadcast, Action<BroadcastStatsBuilder>? configure = null)
    {
        var builder = new BroadcastStatsBuilder();
        configure?.Invoke(builder);
        return builder.Build();
    }

    /// <summary>
    /// Build stats for multiple broadcasts
    /// </summary>
    public static Dictionary<long, BroadcastStats> ForBroadcasts(
        IEnumerable<Broadcast> broadcasts,
        Action<BroadcastStatsBuilder, Broadcast, int>? configure = null)
    {
        var result = new Dictionary<long, BroadcastStats>();
        var index = 0;
        foreach (var broadcast in broadcasts)
        {
            var builder = new BroadcastStatsBuilder()
                .WithTypicalEngagement();

            configure?.Invoke(builder, broadcast, index);
            result[broadcast.Id] = builder.Build();
            index++;
        }
        return result;
    }
}

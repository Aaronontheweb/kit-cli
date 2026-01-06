using KitCLI.Models;

namespace KitCLI.Tests.TestData.Builders;

/// <summary>
/// Fluent builder for creating BroadcastStats test instances.
/// </summary>
public sealed class BroadcastStatsBuilder
{
    private long _id = 1;
    private long _broadcastId = 1;
    private int _recipients = 1000;
    private double _openRate = 0.40; // 40%
    private double _clickRate = 0.10; // 10%
    private int _opens = 500;
    private int _uniqueOpens = 400;
    private int _clicks = 150;
    private int _uniqueClicks = 100;
    private int _unsubscribes = 5;
    private int _bounces = 10;
    private int _complaints = 1;

    public BroadcastStatsBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public BroadcastStatsBuilder ForBroadcast(long broadcastId)
    {
        _broadcastId = broadcastId;
        return this;
    }

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

    public BroadcastStatsBuilder WithOpens(int total, int unique)
    {
        _opens = total;
        _uniqueOpens = unique;
        return this;
    }

    public BroadcastStatsBuilder WithClicks(int total, int unique)
    {
        _clicks = total;
        _uniqueClicks = unique;
        return this;
    }

    public BroadcastStatsBuilder WithUnsubscribes(int unsubscribes)
    {
        _unsubscribes = unsubscribes;
        return this;
    }

    public BroadcastStatsBuilder WithBounces(int bounces)
    {
        _bounces = bounces;
        return this;
    }

    public BroadcastStatsBuilder WithComplaints(int complaints)
    {
        _complaints = complaints;
        return this;
    }

    /// <summary>
    /// Create stats with zero engagement
    /// </summary>
    public BroadcastStatsBuilder WithNoEngagement()
    {
        _openRate = 0;
        _clickRate = 0;
        _opens = 0;
        _uniqueOpens = 0;
        _clicks = 0;
        _uniqueClicks = 0;
        return this;
    }

    /// <summary>
    /// Create stats with high engagement (45% open, 15% click)
    /// </summary>
    public BroadcastStatsBuilder WithHighEngagement()
    {
        _openRate = 0.45;
        _clickRate = 0.15;
        _uniqueOpens = (int)(_recipients * 0.45);
        _opens = (int)(_uniqueOpens * 1.3); // some re-opens
        _uniqueClicks = (int)(_recipients * 0.15);
        _clicks = (int)(_uniqueClicks * 1.2);
        return this;
    }

    /// <summary>
    /// Create stats with low engagement (15% open, 2% click)
    /// </summary>
    public BroadcastStatsBuilder WithLowEngagement()
    {
        _openRate = 0.15;
        _clickRate = 0.02;
        _uniqueOpens = (int)(_recipients * 0.15);
        _opens = (int)(_uniqueOpens * 1.1);
        _uniqueClicks = (int)(_recipients * 0.02);
        _clicks = _uniqueClicks;
        return this;
    }

    /// <summary>
    /// Create stats with typical engagement (35% open, 8% click)
    /// </summary>
    public BroadcastStatsBuilder WithTypicalEngagement()
    {
        _openRate = 0.35;
        _clickRate = 0.08;
        _uniqueOpens = (int)(_recipients * 0.35);
        _opens = (int)(_uniqueOpens * 1.2);
        _uniqueClicks = (int)(_recipients * 0.08);
        _clicks = (int)(_uniqueClicks * 1.1);
        return this;
    }

    /// <summary>
    /// Set engagement by open and click percentages (0.0-1.0)
    /// </summary>
    public BroadcastStatsBuilder WithEngagement(double openRate, double clickRate)
    {
        _openRate = openRate;
        _clickRate = clickRate;
        _uniqueOpens = (int)(_recipients * openRate);
        _opens = (int)(_uniqueOpens * 1.2);
        _uniqueClicks = (int)(_recipients * clickRate);
        _clicks = (int)(_uniqueClicks * 1.1);
        return this;
    }

    public BroadcastStats Build()
    {
        return new BroadcastStats
        {
            Id = _id,
            BroadcastId = _broadcastId,
            Recipients = _recipients,
            OpenRate = _openRate,
            ClickRate = _clickRate,
            Opens = _opens,
            UniqueOpens = _uniqueOpens,
            Clicks = _clicks,
            UniqueClicks = _uniqueClicks,
            Unsubscribes = _unsubscribes,
            Bounces = _bounces,
            Complaints = _complaints
        };
    }

    /// <summary>
    /// Build stats for a broadcast (convenient pairing)
    /// </summary>
    public static BroadcastStats ForBroadcast(Broadcast broadcast, Action<BroadcastStatsBuilder>? configure = null)
    {
        var builder = new BroadcastStatsBuilder()
            .WithId(broadcast.Id)
            .ForBroadcast(broadcast.Id);

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
                .WithId(broadcast.Id)
                .ForBroadcast(broadcast.Id)
                .WithTypicalEngagement();

            configure?.Invoke(builder, broadcast, index);
            result[broadcast.Id] = builder.Build();
            index++;
        }
        return result;
    }
}

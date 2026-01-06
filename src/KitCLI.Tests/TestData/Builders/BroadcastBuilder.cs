using KitCLI.Models;

namespace KitCLI.Tests.TestData.Builders;

/// <summary>
/// Fluent builder for creating Broadcast test instances.
/// </summary>
public sealed class BroadcastBuilder
{
    private long _id = 1;
    private string _subject = "Test Broadcast";
    private string? _previewText;
    private string? _fromName = "Test Sender";
    private string? _fromEmail = "sender@test.com";
    private string? _description;
    private BroadcastContent? _content;
    private bool _isPublic;
    private DateTime? _publishedAt;
    private DateTime? _sendAt;
    private DateTime _createdAt = DateTime.UtcNow.AddDays(-7);
    private DateTime? _updatedAt;
    private SubscriberFilter? _subscriberFilter;

    public BroadcastBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public BroadcastBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public BroadcastBuilder WithPreviewText(string previewText)
    {
        _previewText = previewText;
        return this;
    }

    public BroadcastBuilder WithFromName(string fromName)
    {
        _fromName = fromName;
        return this;
    }

    public BroadcastBuilder WithFromEmail(string fromEmail)
    {
        _fromEmail = fromEmail;
        return this;
    }

    public BroadcastBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public BroadcastBuilder WithContent(string email, string? layoutTemplate = null, string? thumbnailUrl = null)
    {
        _content = new BroadcastContent
        {
            Email = email,
            EmailLayoutTemplate = layoutTemplate,
            ThumbnailUrl = thumbnailUrl
        };
        return this;
    }

    public BroadcastBuilder AsPublic(bool isPublic = true)
    {
        _isPublic = isPublic;
        return this;
    }

    public BroadcastBuilder WithPublishedAt(DateTime publishedAt)
    {
        _publishedAt = publishedAt;
        return this;
    }

    public BroadcastBuilder WithSendAt(DateTime sendAt)
    {
        _sendAt = sendAt;
        return this;
    }

    public BroadcastBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public BroadcastBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _updatedAt = updatedAt;
        return this;
    }

    public BroadcastBuilder WithSubscriberFilter(SubscriberFilter filter)
    {
        _subscriberFilter = filter;
        return this;
    }

    /// <summary>
    /// Creates a draft broadcast (no SendAt date)
    /// </summary>
    public BroadcastBuilder AsDraft()
    {
        _sendAt = null;
        return this;
    }

    /// <summary>
    /// Creates a scheduled broadcast (SendAt in the future)
    /// </summary>
    public BroadcastBuilder AsScheduled(DateTime? sendAt = null)
    {
        _sendAt = sendAt ?? DateTime.UtcNow.AddDays(1);
        return this;
    }

    /// <summary>
    /// Creates a sent broadcast (SendAt in the past)
    /// </summary>
    public BroadcastBuilder AsSent(DateTime? sendAt = null)
    {
        _sendAt = sendAt ?? DateTime.UtcNow.AddDays(-1);
        return this;
    }

    /// <summary>
    /// Target all subscribers
    /// </summary>
    public BroadcastBuilder TargetingAll()
    {
        _subscriberFilter = new SubscriberFilter { All = true };
        return this;
    }

    /// <summary>
    /// Target specific segments
    /// </summary>
    public BroadcastBuilder TargetingSegments(params long[] segmentIds)
    {
        _subscriberFilter = new SubscriberFilter { SegmentIds = segmentIds };
        return this;
    }

    /// <summary>
    /// Target specific tags
    /// </summary>
    public BroadcastBuilder TargetingTags(params long[] tagIds)
    {
        _subscriberFilter = new SubscriberFilter { TagIds = tagIds };
        return this;
    }

    public Broadcast Build()
    {
        return new Broadcast
        {
            Id = _id,
            Subject = _subject,
            PreviewText = _previewText,
            FromName = _fromName,
            FromEmail = _fromEmail,
            Description = _description,
            Content = _content,
            IsPublic = _isPublic,
            PublishedAt = _publishedAt,
            SendAt = _sendAt,
            CreatedAt = _createdAt,
            UpdatedAt = _updatedAt,
            SubscriberFilter = _subscriberFilter
        };
    }

    /// <summary>
    /// Build multiple broadcasts with sequential IDs
    /// </summary>
    public static Broadcast[] BuildMany(int count, Action<BroadcastBuilder, int>? configure = null)
    {
        var broadcasts = new Broadcast[count];
        for (int i = 0; i < count; i++)
        {
            var builder = new BroadcastBuilder()
                .WithId(i + 1)
                .WithSubject($"Test Broadcast {i + 1}")
                .AsSent(DateTime.UtcNow.AddDays(-count + i));

            configure?.Invoke(builder, i);
            broadcasts[i] = builder.Build();
        }
        return broadcasts;
    }
}

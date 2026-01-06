using System.Text.Json.Serialization;

namespace KitCLI.Models;

public sealed class PaginatedResponse<T>
{
    [JsonPropertyName("data")]
    public T[] Data { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

// Kit V4 API specific response types
public sealed class BroadcastsResponse
{
    [JsonPropertyName("broadcasts")]
    public Broadcast[] Broadcasts { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class SubscribersResponse
{
    [JsonPropertyName("subscribers")]
    public Subscriber[] Subscribers { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class SegmentsResponse
{
    [JsonPropertyName("segments")]
    public Segment[] Segments { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class SequencesResponse
{
    [JsonPropertyName("sequences")]
    public Sequence[] Sequences { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class SequenceEmailsResponse
{
    [JsonPropertyName("emails")]
    public SequenceEmail[] Emails { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class SequenceSubscribersResponse
{
    [JsonPropertyName("subscribers")]
    public SequenceSubscriber[] Subscribers { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class TagsResponse
{
    [JsonPropertyName("tags")]
    public Tag[] Tags { get; set; } = [];

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}

public sealed class PaginationInfo
{
    [JsonPropertyName("has_previous_page")]
    public bool HasPreviousPage { get; set; }

    [JsonPropertyName("has_next_page")]
    public bool HasNextPage { get; set; }

    [JsonPropertyName("start_cursor")]
    public string? StartCursor { get; set; }

    [JsonPropertyName("end_cursor")]
    public string? EndCursor { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }
}

// Alternative pagination format that some endpoints use
public sealed class SimplePaginatedResponse<T>
{
    [JsonPropertyName("subscribers")]
    public T[]? Subscribers { get; set; }

    [JsonPropertyName("broadcasts")]
    public T[]? Broadcasts { get; set; }

    [JsonPropertyName("tags")]
    public T[]? Tags { get; set; }

    [JsonPropertyName("segments")]
    public T[]? Segments { get; set; }

    [JsonPropertyName("sequences")]
    public T[]? Sequences { get; set; }

    [JsonPropertyName("emails")]
    public T[]? Emails { get; set; }

    [JsonPropertyName("total_subscribers")]
    public int? TotalSubscribers { get; set; }

    [JsonPropertyName("total_count")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("total_pages")]
    public int? TotalPages { get; set; }
}

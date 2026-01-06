using System.Runtime.CompilerServices;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Tests.Mocks;

/// <summary>
/// A configurable mock implementation of IKitApiClient for testing commands.
/// Allows setting up specific responses for each API method.
/// </summary>
public sealed class MockKitApiClient : IKitApiClient
{
    // Subscribers
    public Func<int, string?, CancellationToken, Task<PaginatedResponse<Subscriber>>>? GetSubscribersAsyncFunc { get; set; }
    public Func<string?, CancellationToken, IAsyncEnumerable<Subscriber>>? GetAllSubscribersAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<Subscriber?>>? GetSubscriberAsyncFunc { get; set; }
    public Func<string, CancellationToken, Task<Subscriber?>>? GetSubscriberByEmailAsyncFunc { get; set; }

    // Broadcasts
    public Func<int, string?, CancellationToken, Task<PaginatedResponse<Broadcast>>>? GetBroadcastsAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<Broadcast?>>? GetBroadcastAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<BroadcastStats?>>? GetBroadcastStatsAsyncFunc { get; set; }

    // Tags
    public Func<CancellationToken, Task<Tag[]>>? GetTagsAsyncFunc { get; set; }
    public Func<long, int, string?, CancellationToken, Task<PaginatedResponse<Subscriber>>>? GetTagSubscribersAsyncFunc { get; set; }

    // Segments
    public Func<int, string?, CancellationToken, Task<PaginatedResponse<Segment>>>? GetSegmentsAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<Segment?>>? GetSegmentAsyncFunc { get; set; }
    public Func<long, int, string?, CancellationToken, Task<PaginatedResponse<Subscriber>>>? GetSegmentSubscribersAsyncFunc { get; set; }
    public Func<long, CancellationToken, IAsyncEnumerable<Subscriber>>? GetAllSegmentSubscribersAsyncFunc { get; set; }

    // Sequences
    public Func<int, string?, CancellationToken, Task<PaginatedResponse<Sequence>>>? GetSequencesAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<Sequence?>>? GetSequenceAsyncFunc { get; set; }
    public Func<long, int, string?, CancellationToken, Task<PaginatedResponse<SequenceEmail>>>? GetSequenceEmailsAsyncFunc { get; set; }
    public Func<long, string?, int, string?, CancellationToken, Task<PaginatedResponse<SequenceSubscriber>>>? GetSequenceSubscribersAsyncFunc { get; set; }
    public Func<long, string?, CancellationToken, IAsyncEnumerable<SequenceSubscriber>>? GetAllSequenceSubscribersAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<SequenceStats?>>? GetSequenceStatsAsyncFunc { get; set; }

    // Forms
    public Func<int, string?, CancellationToken, Task<PaginatedResponse<Form>>>? GetFormsAsyncFunc { get; set; }
    public Func<int, CancellationToken, IAsyncEnumerable<Form>>? GetAllFormsAsyncFunc { get; set; }
    public Func<long, CancellationToken, Task<Form?>>? GetFormAsyncFunc { get; set; }
    public Func<long, int, string?, CancellationToken, Task<PaginatedResponse<Subscriber>>>? GetFormSubscribersAsyncFunc { get; set; }
    public Func<long, int, CancellationToken, IAsyncEnumerable<Subscriber>>? GetAllFormSubscribersAsyncFunc { get; set; }
    public Func<long, string, CancellationToken, Task<bool>>? AddSubscriberToFormAsyncFunc { get; set; }

    // Connection
    public Func<CancellationToken, Task<bool>>? TestConnectionAsyncFunc { get; set; }

    // Pre-configured response data (simpler alternative to Funcs)
    public List<Subscriber> Subscribers { get; set; } = new();
    public List<Broadcast> Broadcasts { get; set; } = new();
    public Dictionary<long, BroadcastStats> BroadcastStats { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public List<Segment> Segments { get; set; } = new();
    public List<Sequence> Sequences { get; set; } = new();
    public List<Form> Forms { get; set; } = new();

    // Subscribers
    public Task<PaginatedResponse<Subscriber>> GetSubscribersAsync(
        int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetSubscribersAsyncFunc != null)
            return GetSubscribersAsyncFunc(perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Subscriber>
        {
            Data = Subscribers.Take(perPage).ToArray(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public async IAsyncEnumerable<Subscriber> GetAllSubscribersAsync(
        string? state = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (GetAllSubscribersAsyncFunc != null)
        {
            await foreach (var item in GetAllSubscribersAsyncFunc(state, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        foreach (var subscriber in Subscribers.Where(s =>
            state == null || s.State.Equals(state, StringComparison.OrdinalIgnoreCase)))
        {
            yield return subscriber;
        }
    }

    public Task<Subscriber?> GetSubscriberAsync(long id, CancellationToken cancellationToken = default)
    {
        if (GetSubscriberAsyncFunc != null)
            return GetSubscriberAsyncFunc(id, cancellationToken);

        return Task.FromResult(Subscribers.FirstOrDefault(s => s.Id == id));
    }

    public Task<Subscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (GetSubscriberByEmailAsyncFunc != null)
            return GetSubscriberByEmailAsyncFunc(email, cancellationToken);

        return Task.FromResult(Subscribers.FirstOrDefault(s =>
            s.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

    // Broadcasts
    public Task<PaginatedResponse<Broadcast>> GetBroadcastsAsync(
        int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetBroadcastsAsyncFunc != null)
            return GetBroadcastsAsyncFunc(perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Broadcast>
        {
            Data = Broadcasts.Take(perPage).ToArray(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public Task<Broadcast?> GetBroadcastAsync(long id, CancellationToken cancellationToken = default)
    {
        if (GetBroadcastAsyncFunc != null)
            return GetBroadcastAsyncFunc(id, cancellationToken);

        return Task.FromResult(Broadcasts.FirstOrDefault(b => b.Id == id));
    }

    public Task<BroadcastStats?> GetBroadcastStatsAsync(long broadcastId, CancellationToken cancellationToken = default)
    {
        if (GetBroadcastStatsAsyncFunc != null)
            return GetBroadcastStatsAsyncFunc(broadcastId, cancellationToken);

        return Task.FromResult(BroadcastStats.GetValueOrDefault(broadcastId));
    }

    // Tags
    public Task<Tag[]> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        if (GetTagsAsyncFunc != null)
            return GetTagsAsyncFunc(cancellationToken);

        return Task.FromResult(Tags.ToArray());
    }

    public Task<PaginatedResponse<Subscriber>> GetTagSubscribersAsync(
        long tagId, int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetTagSubscribersAsyncFunc != null)
            return GetTagSubscribersAsyncFunc(tagId, perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Subscriber>
        {
            Data = Array.Empty<Subscriber>(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    // Segments
    public Task<PaginatedResponse<Segment>> GetSegmentsAsync(
        int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetSegmentsAsyncFunc != null)
            return GetSegmentsAsyncFunc(perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Segment>
        {
            Data = Segments.Take(perPage).ToArray(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public Task<Segment?> GetSegmentAsync(long id, CancellationToken cancellationToken = default)
    {
        if (GetSegmentAsyncFunc != null)
            return GetSegmentAsyncFunc(id, cancellationToken);

        return Task.FromResult(Segments.FirstOrDefault(s => s.Id == id));
    }

    public Task<PaginatedResponse<Subscriber>> GetSegmentSubscribersAsync(
        long segmentId, int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetSegmentSubscribersAsyncFunc != null)
            return GetSegmentSubscribersAsyncFunc(segmentId, perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Subscriber>
        {
            Data = Array.Empty<Subscriber>(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public async IAsyncEnumerable<Subscriber> GetAllSegmentSubscribersAsync(
        long segmentId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (GetAllSegmentSubscribersAsyncFunc != null)
        {
            await foreach (var item in GetAllSegmentSubscribersAsyncFunc(segmentId, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        await Task.CompletedTask; // Satisfy compiler
        yield break;
    }

    // Sequences
    public Task<PaginatedResponse<Sequence>> GetSequencesAsync(
        int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetSequencesAsyncFunc != null)
            return GetSequencesAsyncFunc(perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Sequence>
        {
            Data = Sequences.Take(perPage).ToArray(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public Task<Sequence?> GetSequenceAsync(long id, CancellationToken cancellationToken = default)
    {
        if (GetSequenceAsyncFunc != null)
            return GetSequenceAsyncFunc(id, cancellationToken);

        return Task.FromResult(Sequences.FirstOrDefault(s => s.Id == id));
    }

    public Task<PaginatedResponse<SequenceEmail>> GetSequenceEmailsAsync(
        long sequenceId, int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetSequenceEmailsAsyncFunc != null)
            return GetSequenceEmailsAsyncFunc(sequenceId, perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<SequenceEmail>
        {
            Data = Array.Empty<SequenceEmail>(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public Task<PaginatedResponse<SequenceSubscriber>> GetSequenceSubscribersAsync(
        long sequenceId, string? state = null, int perPage = 50, string? after = null,
        CancellationToken cancellationToken = default)
    {
        if (GetSequenceSubscribersAsyncFunc != null)
            return GetSequenceSubscribersAsyncFunc(sequenceId, state, perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<SequenceSubscriber>
        {
            Data = Array.Empty<SequenceSubscriber>(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public async IAsyncEnumerable<SequenceSubscriber> GetAllSequenceSubscribersAsync(
        long sequenceId, string? state = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (GetAllSequenceSubscribersAsyncFunc != null)
        {
            await foreach (var item in GetAllSequenceSubscribersAsyncFunc(sequenceId, state, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        await Task.CompletedTask;
        yield break;
    }

    public Task<SequenceStats?> GetSequenceStatsAsync(long sequenceId, CancellationToken cancellationToken = default)
    {
        if (GetSequenceStatsAsyncFunc != null)
            return GetSequenceStatsAsyncFunc(sequenceId, cancellationToken);

        return Task.FromResult<SequenceStats?>(null);
    }

    // Forms
    public Task<PaginatedResponse<Form>> GetFormsAsync(
        int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetFormsAsyncFunc != null)
            return GetFormsAsyncFunc(perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Form>
        {
            Data = Forms.Take(perPage).ToArray(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public async IAsyncEnumerable<Form> GetAllFormsAsync(
        int limit = 100, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (GetAllFormsAsyncFunc != null)
        {
            await foreach (var item in GetAllFormsAsyncFunc(limit, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        foreach (var form in Forms.Take(limit))
        {
            yield return form;
        }
    }

    public Task<Form?> GetFormAsync(long id, CancellationToken cancellationToken = default)
    {
        if (GetFormAsyncFunc != null)
            return GetFormAsyncFunc(id, cancellationToken);

        return Task.FromResult(Forms.FirstOrDefault(f => f.Id == id));
    }

    public Task<PaginatedResponse<Subscriber>> GetFormSubscribersAsync(
        long formId, int perPage = 50, string? after = null, CancellationToken cancellationToken = default)
    {
        if (GetFormSubscribersAsyncFunc != null)
            return GetFormSubscribersAsyncFunc(formId, perPage, after, cancellationToken);

        return Task.FromResult(new PaginatedResponse<Subscriber>
        {
            Data = Array.Empty<Subscriber>(),
            Pagination = new PaginationInfo { HasNextPage = false }
        });
    }

    public async IAsyncEnumerable<Subscriber> GetAllFormSubscribersAsync(
        long formId, int limit, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (GetAllFormSubscribersAsyncFunc != null)
        {
            await foreach (var item in GetAllFormSubscribersAsyncFunc(formId, limit, cancellationToken))
            {
                yield return item;
            }
            yield break;
        }

        await Task.CompletedTask;
        yield break;
    }

    public Task<bool> AddSubscriberToFormAsync(long formId, string email, CancellationToken cancellationToken = default)
    {
        if (AddSubscriberToFormAsyncFunc != null)
            return AddSubscriberToFormAsyncFunc(formId, email, cancellationToken);

        return Task.FromResult(true);
    }

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (TestConnectionAsyncFunc != null)
            return TestConnectionAsyncFunc(cancellationToken);

        return Task.FromResult(true);
    }
}

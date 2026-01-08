using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using KitCLI.Models;

namespace KitCLI.Services;

public interface IKitApiClient
{
    // Subscribers
    Task<PaginatedResponse<Subscriber>> GetSubscribersAsync(
        int perPage = 50,
        string? after = null,
        string? state = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Subscriber> GetAllSubscribersAsync(
        string? state = null,
        CancellationToken cancellationToken = default);

    Task<Subscriber?> GetSubscriberAsync(long id, CancellationToken cancellationToken = default);
    Task<Subscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Tag[]> GetSubscriberTagsAsync(long subscriberId, CancellationToken cancellationToken = default);

    // Broadcasts
    Task<PaginatedResponse<Broadcast>> GetBroadcastsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    Task<Broadcast?> GetBroadcastAsync(long id, CancellationToken cancellationToken = default);
    Task<BroadcastStats?> GetBroadcastStatsAsync(long broadcastId, CancellationToken cancellationToken = default);
    Task<BroadcastClicksResponse?> GetBroadcastClicksAsync(long broadcastId, CancellationToken cancellationToken = default);

    // Tags
    Task<Tag[]> GetTagsAsync(CancellationToken cancellationToken = default);
    Task<PaginatedResponse<Subscriber>> GetTagSubscribersAsync(
        long tagId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    // Segments
    Task<PaginatedResponse<Segment>> GetSegmentsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    Task<Segment?> GetSegmentAsync(long id, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<Subscriber>> GetSegmentSubscribersAsync(
        long segmentId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Subscriber> GetAllSegmentSubscribersAsync(
        long segmentId,
        CancellationToken cancellationToken = default);

    // Sequences (Automations)
    Task<PaginatedResponse<Sequence>> GetSequencesAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    Task<Sequence?> GetSequenceAsync(long id, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<SequenceEmail>> GetSequenceEmailsAsync(
        long sequenceId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedResponse<SequenceSubscriber>> GetSequenceSubscribersAsync(
        long sequenceId,
        string? state = null,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<SequenceSubscriber> GetAllSequenceSubscribersAsync(
        long sequenceId,
        string? state = null,
        CancellationToken cancellationToken = default);

    Task<SequenceStats?> GetSequenceStatsAsync(long sequenceId, CancellationToken cancellationToken = default);

    // Forms
    Task<FormsResponse> GetFormsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Form> GetAllFormsAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<Form?> GetFormAsync(long id, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<Subscriber>> GetFormSubscribersAsync(
        long formId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Subscriber> GetAllFormSubscribersAsync(
        long formId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<bool> AddSubscriberToFormAsync(long formId, string email, CancellationToken cancellationToken = default);

    // Account
    Task<AccountStats?> GetAccountStatsAsync(CancellationToken cancellationToken = default);

    // Test connection
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class KitApiClient : IKitApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly KitConfig _config;
    private readonly bool _ownsHttpClient;

    public KitApiClient(KitConfig config) : this(config, null)
    {
    }

    public KitApiClient(KitConfig config, HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (!config.IsValid)
        {
            throw new ArgumentException("Invalid Kit configuration", nameof(config));
        }

        _config = config;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            var handler = new RateLimitHandler(new HttpClientHandler());
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_config.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _ownsHttpClient = true;
        }

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();

        // Kit v4 uses X-Kit-Api-Key header for API key authentication
        _httpClient.DefaultRequestHeaders.Add("X-Kit-Api-Key", _config.ApiKey);

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KitCLI/1.0");
    }

    public async Task<PaginatedResponse<Subscriber>> GetSubscribersAsync(
        int perPage = 50,
        string? after = null,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        // Kit v4 API uses 'status' parameter to filter by subscriber state
        if (!string.IsNullOrEmpty(state))
        {
            url += $"&status={state.ToLowerInvariant()}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns subscribers in a "subscribers" array
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscribersResponse);
        return new PaginatedResponse<Subscriber>
        {
            Data = result?.Subscribers ?? [],
            Pagination = result?.Pagination
        };
    }

    public async IAsyncEnumerable<Subscriber> GetAllSubscribersAsync(
        string? state = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        bool hasMore = true;
        int totalProcessed = 0;

        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            // Pass state filter to API instead of filtering client-side
            var page = await GetSubscribersAsync(100, cursor, state, cancellationToken);

            foreach (var subscriber in page.Data)
            {
                yield return subscriber;
                totalProcessed++;

                // Report progress periodically
                if (totalProcessed % 100 == 0)
                {
                    Console.Write($"\rProcessed {totalProcessed:N0} subscribers...");
                }
            }

            // Check for next page
            if (page.Pagination != null)
            {
                cursor = page.Pagination.EndCursor;
                hasMore = page.Pagination.HasNextPage;
            }
            else
            {
                hasMore = false;
            }
        }

        if (totalProcessed > 0)
        {
            Console.WriteLine($"\rProcessed {totalProcessed:N0} subscribers total.");
        }
    }

    public async Task<Subscriber?> GetSubscriberAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"subscribers/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit V4 API returns single subscriber wrapped in {"subscriber": {...}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscriberResponse);
        return result?.Subscriber;
    }

    public async Task<Subscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Kit v4 API supports email_address query parameter
        var encodedEmail = Uri.EscapeDataString(email);
        var response = await _httpClient.GetAsync($"subscribers?email_address={encodedEmail}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit v4 API returns subscribers wrapped in {"subscribers": [...]}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscribersResponse);

        return result?.Subscribers.FirstOrDefault(s =>
            s.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Tag[]> GetSubscriberTagsAsync(long subscriberId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"subscribers/{subscriberId}/tags", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit V4 API returns tags wrapped in {"tags": [...]}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.TagsResponse);
        return result?.Tags ?? [];
    }

    public async Task<PaginatedResponse<Broadcast>> GetBroadcastsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"broadcasts?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns broadcasts in a "broadcasts" array
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.BroadcastsResponse);
        return new PaginatedResponse<Broadcast>
        {
            Data = result?.Broadcasts ?? [],
            Pagination = result?.Pagination
        };
    }

    public async Task<Broadcast?> GetBroadcastAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"broadcasts/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit V4 API returns single broadcast wrapped in {"broadcast": {...}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.BroadcastResponse);
        return result?.Broadcast;
    }

    public async Task<BroadcastStats?> GetBroadcastStatsAsync(long broadcastId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"broadcasts/{broadcastId}/stats", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit V4 API returns {"broadcast": {"id": ..., "stats": {...}}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.BroadcastStatsResponse);
        return result?.Broadcast?.Stats;
    }

    public async Task<BroadcastClicksResponse?> GetBroadcastClicksAsync(long broadcastId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"broadcasts/{broadcastId}/clicks", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit V4 API returns {"broadcast": {"id": ..., "clicks": [...]}, "pagination": {...}}
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.BroadcastClicksResponse);
    }

    public async Task<Tag[]> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("tags", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Tags might come in different formats
        try
        {
            var paginated = JsonSerializer.Deserialize(json, KitJsonContext.Default.SimplePaginatedResponseTag);
            return paginated?.Tags ?? [];
        }
        catch
        {
            return JsonSerializer.Deserialize(json, KitJsonContext.Default.TagArray) ?? [];
        }
    }

    public async Task<PaginatedResponse<Subscriber>> GetTagSubscribersAsync(
        long tagId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"tags/{tagId}/subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscribersResponse);
        return new PaginatedResponse<Subscriber>
        {
            Data = result?.Subscribers ?? [],
            Pagination = result?.Pagination
        };
    }

    public async Task<PaginatedResponse<Segment>> GetSegmentsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"segments?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns segments in a "segments" array
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SegmentsResponse);
        return new PaginatedResponse<Segment>
        {
            Data = result?.Segments ?? [],
            Pagination = result?.Pagination
        };
    }

    public async Task<Segment?> GetSegmentAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"segments/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        // Kit V4 API returns single segment wrapped in {"segment": {...}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SegmentResponse);
        return result?.Segment;
    }

    public async Task<PaginatedResponse<Subscriber>> GetSegmentSubscribersAsync(
        long segmentId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"segments/{segmentId}/subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscribersResponse);
        return new PaginatedResponse<Subscriber>
        {
            Data = result?.Subscribers ?? [],
            Pagination = result?.Pagination
        };
    }

    public async IAsyncEnumerable<Subscriber> GetAllSegmentSubscribersAsync(
        long segmentId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        bool hasMore = true;
        int totalProcessed = 0;

        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            var page = await GetSegmentSubscribersAsync(segmentId, 100, cursor, cancellationToken);

            foreach (var subscriber in page.Data)
            {
                yield return subscriber;
                totalProcessed++;

                // Report progress periodically
                if (totalProcessed % 100 == 0)
                {
                    Console.Write($"\rProcessed {totalProcessed:N0} segment subscribers...");
                }
            }

            // Check for next page
            if (page.Pagination != null)
            {
                cursor = page.Pagination.EndCursor;
                hasMore = page.Pagination.HasNextPage;
            }
            else
            {
                hasMore = false;
            }
        }

        if (totalProcessed > 0)
        {
            Console.WriteLine($"\rProcessed {totalProcessed:N0} segment subscribers total.");
        }
    }

    public async Task<PaginatedResponse<Sequence>> GetSequencesAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"sequences?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns sequences in a "sequences" array
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SequencesResponse);
        return new PaginatedResponse<Sequence>
        {
            Data = result?.Sequences ?? [],
            Pagination = result?.Pagination
        };
    }

    public async Task<Sequence?> GetSequenceAsync(long id, CancellationToken cancellationToken = default)
    {
        // Kit V4 API does not have a GET /sequences/{id} endpoint
        // We need to fetch all sequences and filter by ID
        string? after = null;
        do
        {
            var response = await GetSequencesAsync(100, after, cancellationToken);
            var sequence = response.Data.FirstOrDefault(s => s.Id == id);
            if (sequence != null)
            {
                return sequence;
            }
            after = response.Pagination?.EndCursor;
        } while (!string.IsNullOrEmpty(after));

        return null;
    }

    public async Task<PaginatedResponse<SequenceEmail>> GetSequenceEmailsAsync(
        long sequenceId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        // Note: Kit V4 API does not have a /sequences/{id}/emails endpoint
        // This method returns an empty response as the endpoint doesn't exist
        var url = $"sequences/{sequenceId}/emails?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);

        // Return empty if endpoint doesn't exist (404)
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PaginatedResponse<SequenceEmail>
            {
                Data = [],
                Pagination = new PaginationInfo { HasNextPage = false }
            };
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSequenceEmail)
                ?? new PaginatedResponse<SequenceEmail>();
        }
        catch
        {
            // Try simple format with emails array
            var simple = JsonSerializer.Deserialize(json, KitJsonContext.Default.SimplePaginatedResponseSequenceEmail);
            return new PaginatedResponse<SequenceEmail>
            {
                Data = simple?.Emails ?? [],
                Pagination = new PaginationInfo
                {
                    PerPage = perPage,
                    HasNextPage = simple?.TotalPages > simple?.Page
                }
            };
        }
    }

    public async Task<PaginatedResponse<SequenceSubscriber>> GetSequenceSubscribersAsync(
        long sequenceId,
        string? state = null,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"sequences/{sequenceId}/subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        if (!string.IsNullOrEmpty(state))
        {
            url += $"&state={Uri.EscapeDataString(state)}";
        }

        var response = await _httpClient.GetAsync(url, cancellationToken);

        // Return empty if sequence not found
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PaginatedResponse<SequenceSubscriber>
            {
                Data = [],
                Pagination = new PaginationInfo { HasNextPage = false }
            };
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSequenceSubscriber)
            ?? new PaginatedResponse<SequenceSubscriber>();
    }

    public async IAsyncEnumerable<SequenceSubscriber> GetAllSequenceSubscribersAsync(
        long sequenceId,
        string? state = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        bool hasMore = true;
        int totalProcessed = 0;

        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            var page = await GetSequenceSubscribersAsync(sequenceId, state, 100, cursor, cancellationToken);

            foreach (var subscriber in page.Data)
            {
                yield return subscriber;
                totalProcessed++;

                // Report progress periodically
                if (totalProcessed % 100 == 0)
                {
                    Console.Write($"\rProcessed {totalProcessed:N0} sequence subscribers...");
                }
            }

            // Check for next page
            if (page.Pagination != null)
            {
                cursor = page.Pagination.EndCursor;
                hasMore = page.Pagination.HasNextPage;
            }
            else
            {
                hasMore = false;
            }
        }

        if (totalProcessed > 0)
        {
            Console.WriteLine($"\rProcessed {totalProcessed:N0} sequence subscribers total.");
        }
    }

    public async Task<SequenceStats?> GetSequenceStatsAsync(long sequenceId, CancellationToken cancellationToken = default)
    {
        // Calculate stats from sequence data and emails
        var sequence = await GetSequenceAsync(sequenceId, cancellationToken);
        if (sequence == null)
        {
            return null;
        }

        var emails = await GetSequenceEmailsAsync(sequenceId, 100, null, cancellationToken);

        // Aggregate stats from emails
        var stats = new SequenceStats
        {
            SequenceId = sequenceId,
            TotalSubscribers = sequence.SubscriberCount
        };

        if (emails.Data.Length > 0)
        {
            stats.AverageOpenRate = emails.Data.Average(e => e.OpenRate);
            stats.AverageClickRate = emails.Data.Average(e => e.ClickRate);
            stats.EmailsSent = emails.Data.Sum(e => e.TotalRecipients);
        }

        // Get subscriber states
        var activeCount = 0;
        var completedCount = 0;
        var cancelledCount = 0;

        await foreach (var subscriber in GetAllSequenceSubscribersAsync(sequenceId, null, cancellationToken))
        {
            if (subscriber.IsActive)
            {
                activeCount++;
            }

            if (subscriber.IsCompleted)
            {
                completedCount++;
            }

            if (subscriber.State.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
            {
                cancelledCount++;
            }
        }

        stats.ActiveSubscribers = activeCount;
        stats.CompletedSubscribers = completedCount;
        stats.CancelledSubscribers = cancelledCount;
        stats.CompletionRate = stats.TotalSubscribers > 0
            ? (double)completedCount / stats.TotalSubscribers
            : 0;

        return stats;
    }

    // Forms
    public async Task<FormsResponse> GetFormsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            query += $"&after={after}";
        }

        var response = await _httpClient.GetAsync($"forms{query}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<FormsResponse>(json, KitJsonContext.Default.FormsResponse)
               ?? new FormsResponse();
    }

    public async IAsyncEnumerable<Form> GetAllFormsAsync(
        int limit = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;
        var retrieved = 0;

        do
        {
            var response = await GetFormsAsync(50, after, cancellationToken);

            foreach (var form in response.Forms)
            {
                if (retrieved >= limit)
                {
                    yield break;
                }
                yield return form;
                retrieved++;
            }

            after = response.Pagination?.EndCursor;
        } while (!string.IsNullOrEmpty(after) && retrieved < limit);
    }

    public async Task<Form?> GetFormAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"forms/{id}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            // Kit V4 API returns single form wrapped in {"form": {...}}
            var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.FormResponse);
            return result?.Form;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<PaginatedResponse<Subscriber>> GetFormSubscribersAsync(
        long formId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
        {
            query += $"&after={after}";
        }

        var response = await _httpClient.GetAsync($"forms/{formId}/subscribers{query}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Kit V4 API returns {"subscribers": [...], "pagination": {...}}
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.SubscribersResponse);
        return new PaginatedResponse<Subscriber>
        {
            Data = result?.Subscribers ?? [],
            Pagination = result?.Pagination
        };
    }

    public async IAsyncEnumerable<Subscriber> GetAllFormSubscribersAsync(
        long formId,
        int limit,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? after = null;
        var retrieved = 0;

        do
        {
            var response = await GetFormSubscribersAsync(formId, 50, after, cancellationToken);

            foreach (var subscriber in response.Data)
            {
                if (retrieved >= limit)
                {
                    yield break;
                }
                yield return subscriber;
                retrieved++;
            }

            after = response.Pagination?.EndCursor;
        } while (!string.IsNullOrEmpty(after) && retrieved < limit);
    }

    public async Task<bool> AddSubscriberToFormAsync(long formId, string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = JsonSerializer.Serialize(new Dictionary<string, string> { { "email_address", email } }, KitJsonContext.Default.DictionaryStringString);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/forms/{formId}/subscribers", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Account
    public async Task<AccountStats?> GetAccountStatsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("account/email_stats", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.AccountStatsResponse);
        return result?.Stats;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get account info or a small list of subscribers
            var response = await _httpClient.GetAsync("account", cancellationToken);

            // If account endpoint doesn't exist, try subscribers
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                response = await _httpClient.GetAsync("subscribers?per_page=1", cancellationToken);
            }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}

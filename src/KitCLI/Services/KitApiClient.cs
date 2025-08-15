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
        CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<Subscriber> GetAllSubscribersAsync(
        string? state = null,
        CancellationToken cancellationToken = default);
    
    Task<Subscriber?> GetSubscriberAsync(long id, CancellationToken cancellationToken = default);
    Task<Subscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    // Broadcasts
    Task<PaginatedResponse<Broadcast>> GetBroadcastsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default);
    
    Task<Broadcast?> GetBroadcastAsync(long id, CancellationToken cancellationToken = default);
    Task<BroadcastStats?> GetBroadcastStatsAsync(long broadcastId, CancellationToken cancellationToken = default);
    
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
            throw new ArgumentException("Invalid Kit configuration", nameof(config));
        
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
        
        // Kit v4 uses Bearer token authentication
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KitCLI/1.0");
    }
    
    public async Task<PaginatedResponse<Subscriber>> GetSubscribersAsync(
        int perPage = 50, 
        string? after = null, 
        CancellationToken cancellationToken = default)
    {
        var url = $"/subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
            url += $"&after={after}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        
        // Try to deserialize as paginated response
        try
        {
            return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSubscriber) 
                ?? new PaginatedResponse<Subscriber>();
        }
        catch
        {
            // Fallback for different response format
            var simple = JsonSerializer.Deserialize(json, KitJsonContext.Default.SimplePaginatedResponseSubscriber);
            return new PaginatedResponse<Subscriber>
            {
                Data = simple?.Subscribers ?? [],
                Pagination = new PaginationInfo
                {
                    PerPage = perPage,
                    HasNextPage = simple?.TotalPages > simple?.Page
                }
            };
        }
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
            var page = await GetSubscribersAsync(100, cursor, cancellationToken);
            
            foreach (var subscriber in page.Data)
            {
                // Filter by state if specified
                if (state == null || subscriber.State.Equals(state, StringComparison.OrdinalIgnoreCase))
                {
                    yield return subscriber;
                    totalProcessed++;
                    
                    // Report progress periodically
                    if (totalProcessed % 100 == 0)
                    {
                        Console.Write($"\rProcessed {totalProcessed:N0} subscribers...");
                    }
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
        var response = await _httpClient.GetAsync($"/subscribers/{id}", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.Subscriber);
    }
    
    public async Task<Subscriber?> GetSubscriberByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Kit API doesn't have direct email lookup, so we need to search
        var encodedEmail = Uri.EscapeDataString(email);
        var response = await _httpClient.GetAsync($"/subscribers?email_address={encodedEmail}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return null;
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSubscriber);
        
        return result?.Data.FirstOrDefault(s => 
            s.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));
    }
    
    public async Task<PaginatedResponse<Broadcast>> GetBroadcastsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/broadcasts?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
            url += $"&after={after}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        
        try
        {
            return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseBroadcast) 
                ?? new PaginatedResponse<Broadcast>();
        }
        catch
        {
            // Fallback for different response format
            var simple = JsonSerializer.Deserialize(json, KitJsonContext.Default.SimplePaginatedResponseBroadcast);
            return new PaginatedResponse<Broadcast>
            {
                Data = simple?.Broadcasts ?? [],
                Pagination = new PaginationInfo
                {
                    PerPage = perPage,
                    HasNextPage = simple?.TotalPages > simple?.Page
                }
            };
        }
    }
    
    public async Task<Broadcast?> GetBroadcastAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/broadcasts/{id}", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.Broadcast);
    }
    
    public async Task<BroadcastStats?> GetBroadcastStatsAsync(long broadcastId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/broadcasts/{broadcastId}/stats", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.BroadcastStats);
    }
    
    public async Task<Tag[]> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("/tags", cancellationToken);
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
        var url = $"/tags/{tagId}/subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
            url += $"&after={after}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSubscriber) 
            ?? new PaginatedResponse<Subscriber>();
    }
    
    public async Task<PaginatedResponse<Segment>> GetSegmentsAsync(
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/segments?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
            url += $"&after={after}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        
        try
        {
            return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSegment) 
                ?? new PaginatedResponse<Segment>();
        }
        catch
        {
            // Fallback for different response format
            var simple = JsonSerializer.Deserialize(json, KitJsonContext.Default.SimplePaginatedResponseSegment);
            return new PaginatedResponse<Segment>
            {
                Data = simple?.Segments ?? [],
                Pagination = new PaginationInfo
                {
                    PerPage = perPage,
                    HasNextPage = simple?.TotalPages > simple?.Page
                }
            };
        }
    }
    
    public async Task<Segment?> GetSegmentAsync(long id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"/segments/{id}", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.Segment);
    }
    
    public async Task<PaginatedResponse<Subscriber>> GetSegmentSubscribersAsync(
        long segmentId,
        int perPage = 50,
        string? after = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/segments/{segmentId}/subscribers?per_page={perPage}";
        if (!string.IsNullOrEmpty(after))
            url += $"&after={after}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        
        return JsonSerializer.Deserialize(json, KitJsonContext.Default.PaginatedResponseSubscriber) 
            ?? new PaginatedResponse<Subscriber>();
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
    
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get account info or a small list of subscribers
            var response = await _httpClient.GetAsync("/account", cancellationToken);
            
            // If account endpoint doesn't exist, try subscribers
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                response = await _httpClient.GetAsync("/subscribers?per_page=1", cancellationToken);
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
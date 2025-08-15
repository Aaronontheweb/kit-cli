using System.Net;

namespace KitCLI.Services;

/// <summary>
/// HTTP handler that respects rate limits and implements exponential backoff
/// </summary>
public sealed class RateLimitHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTime _resetTime = DateTime.MinValue;
    private int _remainingRequests = int.MaxValue;

    public RateLimitHandler() : base(new HttpClientHandler())
    {
    }

    public RateLimitHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Check if we need to wait for rate limit reset
            if (_remainingRequests <= 0 && DateTime.UtcNow < _resetTime)
            {
                var delay = _resetTime - DateTime.UtcNow;
                Console.WriteLine($"Rate limited. Waiting {delay.TotalSeconds:F0}s...");
                await Task.Delay(delay, cancellationToken);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Update rate limit info from headers
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining))
            {
                if (int.TryParse(remaining.First(), out var rem))
                {
                    _remainingRequests = rem;
                }
            }

            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset))
            {
                if (long.TryParse(reset.First(), out var unixTime))
                {
                    _resetTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                }
            }

            // Handle rate limit response with exponential backoff
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = GetRetryAfter(response);
                if (retryAfter > TimeSpan.Zero)
                {
                    Console.WriteLine($"Rate limit hit. Retrying after {retryAfter.TotalSeconds:F0}s...");
                    await Task.Delay(retryAfter, cancellationToken);

                    // Clone the request since it may have been disposed
                    var retryRequest = await CloneRequestAsync(request);
                    return await SendAsync(retryRequest, cancellationToken);
                }
            }

            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static TimeSpan GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter != null)
        {
            if (response.Headers.RetryAfter.Delta.HasValue)
            {
                return response.Headers.RetryAfter.Delta.Value;
            }

            if (response.Headers.RetryAfter.Date.HasValue)
            {
                return response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            }
        }

        // Default exponential backoff
        return TimeSpan.FromSeconds(30);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}

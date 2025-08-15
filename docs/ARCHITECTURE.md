# Kit CLI Technical Architecture

## Technology Stack

- **Language**: C# 13 / .NET 9
- **Compilation**: AOT (Ahead-of-Time) for < 100ms startup
- **JSON**: System.Text.Json with source generators
- **HTTP**: HttpClient with custom rate limiting handler
- **Testing**: xUnit with mock HTTP handlers
- **CI/CD**: GitHub Actions for multi-platform builds

## Project Structure

```
kit-cli/
├── src/
│   └── KitCLI/
│       ├── KitCLI.csproj
│       ├── Program.cs                 # Entry point & command routing
│       ├── KitJsonContext.cs          # AOT JSON serialization
│       ├── Commands/                  # Command handlers
│       │   ├── ConfigCommand.cs
│       │   ├── SubscriberCommand.cs
│       │   ├── BroadcastCommand.cs
│       │   ├── TagCommand.cs
│       │   └── ...
│       ├── Models/                    # Data models
│       │   ├── Subscriber.cs
│       │   ├── Broadcast.cs
│       │   ├── Tag.cs
│       │   ├── Sequence.cs
│       │   └── ...
│       ├── Services/                  # Business logic
│       │   ├── KitApiClient.cs
│       │   ├── ConfigurationService.cs
│       │   ├── ExportService.cs
│       │   ├── BulkOperationService.cs
│       │   ├── RateLimitHandler.cs
│       │   └── UpdateService.cs
│       └── Helpers/                   # Utilities
│           ├── OutputFormatter.cs
│           ├── ProgressIndicator.cs
│           └── CommandParser.cs
├── tests/
│   └── KitCLI.Tests/
│       ├── Unit/
│       ├── Integration/
│       └── TestData/
│           └── ApiResponses/
├── scripts/
├── docs/
└── .github/
    └── workflows/
```

## Authentication Architecture

### API Key Management
```csharp
public sealed class KitApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KitConfig _config;
    
    private void ConfigureHttpClient()
    {
        // Kit v4 uses Bearer token authentication
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KitCLI/1.0");
    }
}
```

### Configuration Storage
```json
{
  "profiles": {
    "default": {
      "api_key": "kit_api_key_here",
      "api_version": "v4"
    },
    "production": {
      "api_key": "kit_prod_api_key",
      "api_version": "v4"
    }
  },
  "current_profile": "default",
  "settings": {
    "default_format": "table",
    "page_size": 50,
    "auto_update": true
  }
}
```

## Command Routing Pattern

### Simple AOT-Compatible Router
```csharp
static async Task<int> RouteCommand(string[] args, bool isReadOnly)
{
    return args[0].ToLowerInvariant() switch
    {
        "subscriber" => await HandleSubscriberCommand(args[1..], isReadOnly),
        "broadcast" => await HandleBroadcastCommand(args[1..], isReadOnly),
        "tag" => await HandleTagCommand(args[1..], isReadOnly),
        "sequence" => await HandleSequenceCommand(args[1..], isReadOnly),
        "form" => await HandleFormCommand(args[1..], isReadOnly),
        "segment" => await HandleSegmentCommand(args[1..], isReadOnly),
        "webhook" => await HandleWebhookCommand(args[1..], isReadOnly),
        "stats" => await HandleStatsCommand(args[1..], isReadOnly),
        "bulk" => await HandleBulkCommand(args[1..], isReadOnly),
        "export" => await HandleExportCommand(args[1..]),
        "config" => await HandleConfigCommand(args[1..], isReadOnly),
        "update" => await HandleUpdateCommand(args[1..]),
        _ => ShowUnknownCommand(args[0])
    };
}
```

## Data Models & Serialization

### JSON Source Generation Context
```csharp
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(DateTimeOffsetConverter)])]
[JsonSerializable(typeof(Subscriber))]
[JsonSerializable(typeof(PaginatedResponse<Subscriber>))]
[JsonSerializable(typeof(Broadcast))]
[JsonSerializable(typeof(PaginatedResponse<Broadcast>))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
[JsonSerializable(typeof(Sequence))]
[JsonSerializable(typeof(Form))]
[JsonSerializable(typeof(Segment))]
[JsonSerializable(typeof(Webhook))]
[JsonSerializable(typeof(BulkImportRequest))]
[JsonSerializable(typeof(BulkImportResult))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class KitJsonContext : JsonSerializerContext
{
}

// Separate context for formatted output
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Subscriber))]
[JsonSerializable(typeof(Broadcast))]
// ... other types
public partial class KitJsonIndentedContext : JsonSerializerContext
{
}
```

### Pagination Support
```csharp
public sealed class PaginatedResponse<T>
{
    public T[] Data { get; set; } = [];
    public PaginationInfo Pagination { get; set; }
}

public sealed class PaginationInfo
{
    public string? After { get; set; }  // Cursor for next page
    public string? Before { get; set; } // Cursor for previous page
    public int PerPage { get; set; }
    public int? TotalCount { get; set; }
    public bool HasMore { get; set; }
}
```

## API Client Architecture

### Rate Limiting Handler
```csharp
public sealed class RateLimitHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTime _resetTime = DateTime.MinValue;
    private int _remainingRequests = int.MaxValue;
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Check if we need to wait
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
                _remainingRequests = int.Parse(remaining.First());
            }
            
            if (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset))
            {
                _resetTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(reset.First())).UtcDateTime;
            }
            
            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### Cursor-Based Pagination
```csharp
public async IAsyncEnumerable<Subscriber> GetAllSubscribersAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    string? cursor = null;
    bool hasMore = true;
    
    while (hasMore)
    {
        var url = "/v4/subscribers";
        if (!string.IsNullOrEmpty(cursor))
            url += $"?after={cursor}";
        
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var page = JsonSerializer.Deserialize(json, 
            KitJsonContext.Default.PaginatedResponseSubscriber);
        
        foreach (var subscriber in page.Data)
            yield return subscriber;
        
        cursor = page.Pagination.After;
        hasMore = !string.IsNullOrEmpty(cursor);
    }
}
```

## Bulk Operations

### Async Bulk Processing
```csharp
public async Task<BulkImportResult> ImportSubscribersAsync(
    string csvPath, 
    IProgress<int>? progress = null)
{
    var subscribers = await ParseCsvAsync(csvPath);
    var batches = subscribers.Chunk(100); // Kit supports up to 100 per request
    
    var results = new List<BulkImportResult>();
    var totalProcessed = 0;
    
    foreach (var batch in batches)
    {
        var request = new BulkCreateRequest
        {
            Subscribers = batch.ToArray(),
            CallbackUrl = null // Optional webhook for async processing
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "/v4/bulk/subscribers", 
            request, 
            KitJsonContext.Default.BulkCreateRequest);
        
        response.EnsureSuccessStatusCode();
        
        totalProcessed += batch.Count();
        progress?.Report(totalProcessed);
    }
    
    return CombineResults(results);
}
```

## Output Formatting

### Table Formatter
```csharp
public static class OutputFormatter
{
    public static void PrintSubscribersTable(IEnumerable<Subscriber> subscribers)
    {
        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Email");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Tags");
        table.AddColumn("Created");
        
        foreach (var sub in subscribers)
        {
            table.AddRow(
                sub.Id.ToString(),
                sub.EmailAddress,
                sub.FirstName ?? "-",
                sub.State,
                string.Join(", ", sub.Tags?.Select(t => t.Name) ?? []),
                sub.CreatedAt.ToString("yyyy-MM-dd")
            );
        }
        
        Console.WriteLine(table.ToString());
    }
}
```

## Error Handling

### Structured Error Responses
```csharp
public sealed class ErrorResponse
{
    public string Error { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public static class ErrorHandler
{
    public static int HandleApiError(HttpResponseException ex)
    {
        return ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => HandleUnauthorized(),
            HttpStatusCode.TooManyRequests => HandleRateLimit(ex),
            HttpStatusCode.NotFound => HandleNotFound(ex),
            HttpStatusCode.BadRequest => HandleBadRequest(ex),
            _ => HandleGenericError(ex)
        };
    }
}
```

## Testing Strategy

### Mock Kit API Server
```csharp
public sealed class MockKitServer : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, Task<object>>> _handlers = new();
    
    public void SetupSubscribers(Subscriber[] subscribers)
    {
        _handlers["GET:/v4/subscribers"] = _ => Task.FromResult<object>(
            new PaginatedResponse<Subscriber> 
            { 
                Data = subscribers,
                Pagination = new() { PerPage = 50 }
            });
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var key = $"{request.Method}:{request.RequestUri?.PathAndQuery}";
        
        if (_handlers.TryGetValue(key, out var handler))
        {
            var result = await handler(request);
            var json = JsonSerializer.Serialize(result, KitJsonContext.Default.Options);
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
        
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
```

## Performance Optimizations

1. **AOT Compilation**
   - Zero reflection usage
   - Source-generated JSON serialization
   - Trimmed dependencies

2. **Streaming Operations**
   - IAsyncEnumerable for large datasets
   - Chunked processing for bulk operations
   - Progress reporting for long-running tasks

3. **Memory Efficiency**
   - ArrayPool for temporary buffers
   - Span<T> for string operations
   - Dispose pattern for HttpClient management

## Security Measures

1. **Credential Protection**
   - Secure file permissions (600 on Unix)
   - Environment variable support
   - Never log API keys

2. **Input Validation**
   - Email format validation
   - Command injection prevention
   - Path traversal protection

3. **Network Security**
   - HTTPS only
   - Certificate validation
   - Request timeouts

## Deployment

### GitHub Actions Workflow
```yaml
name: Build and Release

on:
  push:
    tags: ['v*']

jobs:
  build:
    strategy:
      matrix:
        include:
          - { os: ubuntu-latest, rid: linux-x64 }
          - { os: ubuntu-latest, rid: linux-arm64 }
          - { os: windows-latest, rid: win-x64 }
          - { os: macos-latest, rid: osx-x64 }
          - { os: macos-latest, rid: osx-arm64 }
    
    runs-on: ${{ matrix.os }}
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Publish AOT
        run: |
          dotnet publish src/KitCLI \
            -c Release \
            -r ${{ matrix.rid }} \
            /p:PublishAot=true \
            /p:StripSymbols=true \
            -o publish/
      
      - name: Package
        run: |
          cd publish
          tar -czf ../kit-${{ matrix.rid }}.tar.gz *
      
      - name: Upload
        uses: actions/upload-release-asset@v1
        with:
          upload_url: ${{ github.event.release.upload_url }}
          asset_path: ./kit-${{ matrix.rid }}.tar.gz
```
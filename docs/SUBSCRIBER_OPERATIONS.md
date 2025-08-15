# Kit CLI - Subscriber Operations Focus

## Priority Use Cases

Since most operations involve working with large lists of subscribers, the CLI is optimized for:

1. **Campaign Analytics**
   - Who opened a specific broadcast
   - Who clicked links in a campaign
   - Who didn't open (for re-engagement)
   
2. **Subscriber Segmentation**
   - Active vs inactive subscribers
   - Bounced/complained subscribers
   - Subscribers by tag combinations
   - Subscribers by form source
   
3. **List Management**
   - Bulk exports for analysis
   - Finding unsubscribed users
   - Identifying cold subscribers
   - Tag-based filtering

## Optimized Commands for Subscriber Lists

### Campaign Engagement Commands
```bash
# Get all subscribers who opened a broadcast
kit broadcast opened <broadcast-id> [--export csv]

# Get subscribers who clicked any link
kit broadcast clicked <broadcast-id> [--export csv]

# Get subscribers who DIDN'T open (for re-sending)
kit broadcast unopened <broadcast-id> [--export csv]

# Get engagement stats with subscriber details
kit broadcast engagement <broadcast-id> --detailed
```

### Subscriber Filtering Commands
```bash
# Get all unsubscribed users with unsubscribe date
kit subscriber list --status cancelled --export unsubscribed.csv

# Get bounced emails for cleanup
kit subscriber list --status bounced --export bounced.csv

# Get subscribers who complained (marked as spam)
kit subscriber list --status complained --export complained.csv

# Get cold subscribers (inactive for X days)
kit subscriber list --status inactive --days 90 --export cold.csv

# Get subscribers by multiple tags (AND logic)
kit subscriber list --tags "customer,premium" --export premium_customers.csv

# Get subscribers by any tag (OR logic)
kit subscriber list --any-tags "lead,prospect" --export leads.csv

# Get subscribers without specific tags
kit subscriber list --exclude-tags "customer" --export non_customers.csv
```

### Bulk Analysis Commands
```bash
# Analyze subscriber growth over time
kit stats subscribers --start-date 2024-01-01 --group-by month

# Get subscriber source breakdown
kit stats sources --export sources.csv

# Find duplicate subscribers
kit subscriber duplicates --export duplicates.csv

# Get subscribers by signup form
kit form subscribers <form-id> --export form_signups.csv

# Get subscribers in a sequence
kit sequence subscribers <sequence-id> --status active --export sequence.csv

# Get subscribers in a segment
kit segment subscribers <segment-id> --export segment.csv
```

## Streaming Architecture for Large Datasets

### Efficient Pagination with Progress
```csharp
public async Task ExportAllSubscribers(string outputPath, string? status = null)
{
    using var writer = new StreamWriter(outputPath);
    using var csvWriter = new CsvWriter(writer);
    
    // Write headers
    await csvWriter.WriteHeaderAsync<SubscriberExport>();
    
    var processed = 0;
    var progress = new ProgressIndicator("Exporting subscribers");
    
    // Stream subscribers using cursor pagination
    await foreach (var subscriber in GetAllSubscribersAsync(status))
    {
        await csvWriter.WriteRecordAsync(new SubscriberExport
        {
            Id = subscriber.Id,
            Email = subscriber.EmailAddress,
            FirstName = subscriber.FirstName,
            State = subscriber.State,
            Tags = string.Join(";", subscriber.Tags ?? []),
            CreatedAt = subscriber.CreatedAt,
            CustomFields = JsonSerializer.Serialize(subscriber.Fields)
        });
        
        processed++;
        if (processed % 100 == 0)
        {
            progress.Report($"Exported {processed:N0} subscribers...");
            await writer.FlushAsync(); // Flush periodically
        }
    }
    
    progress.Complete($"Exported {processed:N0} subscribers");
}
```

### Parallel Processing for Broadcast Stats
```csharp
public async Task<BroadcastEngagement> GetBroadcastEngagement(long broadcastId)
{
    // Fetch stats and subscriber lists in parallel
    var statsTask = GetBroadcastStatsAsync(broadcastId);
    var opensTask = GetBroadcastOpensAsync(broadcastId);
    var clicksTask = GetBroadcastClicksAsync(broadcastId);
    
    await Task.WhenAll(statsTask, opensTask, clicksTask);
    
    var stats = await statsTask;
    var opens = await opensTask;
    var clicks = await clicksTask;
    
    // Efficiently find non-openers
    var allRecipients = await GetBroadcastRecipientsAsync(broadcastId);
    var openedIds = opens.Select(s => s.Id).ToHashSet();
    var unopened = allRecipients.Where(r => !openedIds.Contains(r.Id));
    
    return new BroadcastEngagement
    {
        Stats = stats,
        Opened = opens,
        Clicked = clicks,
        Unopened = unopened
    };
}
```

## Filtering Engine

### Complex Filter Support
```csharp
public sealed class SubscriberFilter
{
    public string? State { get; set; }
    public string[]? Tags { get; set; }           // AND logic
    public string[]? AnyTags { get; set; }        // OR logic
    public string[]? ExcludeTags { get; set; }    // NOT logic
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int? InactiveDays { get; set; }
    public long? FormId { get; set; }
    public long? SequenceId { get; set; }
    public long? SegmentId { get; set; }
    
    public bool Matches(Subscriber subscriber)
    {
        if (State != null && subscriber.State != State)
            return false;
        
        if (Tags?.Length > 0)
        {
            var subTags = subscriber.Tags?.Select(t => t.Name).ToHashSet() ?? [];
            if (!Tags.All(t => subTags.Contains(t)))
                return false;
        }
        
        if (AnyTags?.Length > 0)
        {
            var subTags = subscriber.Tags?.Select(t => t.Name).ToHashSet() ?? [];
            if (!AnyTags.Any(t => subTags.Contains(t)))
                return false;
        }
        
        if (ExcludeTags?.Length > 0)
        {
            var subTags = subscriber.Tags?.Select(t => t.Name).ToHashSet() ?? [];
            if (ExcludeTags.Any(t => subTags.Contains(t)))
                return false;
        }
        
        // Additional filter logic...
        return true;
    }
}
```

## Export Formats Optimized for Analysis

### CSV Export with All Fields
```csv
id,email,first_name,state,tags,created_at,source,form_id,custom_field_1,custom_field_2
12345,john@example.com,John,active,"customer;premium",2024-01-15T10:30:00Z,form,456,value1,value2
12346,jane@example.com,Jane,cancelled,"lead",2024-01-16T11:00:00Z,api,,value3,
```

### JSON Export for Data Processing
```json
{
  "export_date": "2024-12-01T10:00:00Z",
  "total_count": 15234,
  "filters_applied": {
    "state": "active",
    "tags": ["customer"]
  },
  "subscribers": [
    {
      "id": 12345,
      "email": "john@example.com",
      "first_name": "John",
      "state": "active",
      "tags": ["customer", "premium"],
      "created_at": "2024-01-15T10:30:00Z",
      "custom_fields": {
        "company": "Acme Corp",
        "role": "Manager"
      }
    }
  ]
}
```

### Excel-Friendly Export
- Proper date formatting
- Boolean fields as YES/NO
- Multi-value fields semicolon-separated
- UTF-8 BOM for Excel compatibility

## Caching Strategy for Repeated Queries

### Local Cache for Tag/Form Lookups
```csharp
public sealed class MetadataCache
{
    private readonly Dictionary<long, Tag> _tags = new();
    private readonly Dictionary<long, Form> _forms = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    
    public async Task<string> GetTagName(long tagId)
    {
        if (DateTime.UtcNow - _lastRefresh > TimeSpan.FromMinutes(5))
        {
            await RefreshCache();
        }
        
        return _tags.TryGetValue(tagId, out var tag) ? tag.Name : $"tag_{tagId}";
    }
}
```

## Performance Benchmarks

### Target Performance for Subscriber Operations
- Export 10,000 subscribers: < 30 seconds
- Filter 100,000 subscribers: < 5 seconds (with streaming)
- Get broadcast engagement: < 10 seconds
- Complex tag filtering: < 2 seconds per 10,000 records

## Common Workflows

### Weekly Engagement Report
```bash
# Get all broadcasts from last week
kit broadcast list --since "7 days ago" --format json > broadcasts.json

# For each broadcast, get engagement
for id in $(jq -r '.[].id' broadcasts.json); do
  kit broadcast engagement $id --export "engagement_$id.csv"
done

# Combine into report
kit report weekly --output weekly_report.html
```

### Find and Export Cold Subscribers
```bash
# Find subscribers who haven't opened anything in 60 days
kit subscriber cold --days 60 --export cold_subscribers.csv

# Tag them for re-engagement campaign
kit bulk tag --file cold_subscribers.csv --tag "re-engage"

# Or unsubscribe them
kit bulk unsubscribe --file cold_subscribers.csv --reason "inactive"
```

### Analyze Subscriber Sources
```bash
# Get breakdown by form
kit stats forms --export form_performance.csv

# Get subscribers from top performing form
best_form=$(kit stats forms --format json | jq -r 'max_by(.subscribers) | .id')
kit form subscribers $best_form --export best_form_subscribers.csv
```
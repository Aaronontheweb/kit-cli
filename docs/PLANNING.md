# Kit CLI Planning Document

## Overview

Kit CLI is a fast, lightweight command-line interface for Kit (formerly ConvertKit) email marketing platform. Built with .NET 9 AOT compilation for instant startup and minimal resource usage, it provides comprehensive access to Kit's API v4 for managing email campaigns, subscribers, automations, and marketing analytics.

## Goals

1. **Primary Goals**
   - Query and manage email campaigns, automations, and subscribers
   - Monitor email marketing performance metrics
   - Export data for analysis and reporting
   - Create and schedule broadcasts and sequences

2. **Technical Goals**
   - < 100ms startup time with AOT compilation
   - < 15MB binary size
   - Cross-platform support (Linux, macOS, Windows)
   - Self-updating capability
   - Multiple output formats (table, JSON, CSV)
   - Secure credential management

## Authentication Strategy

Kit API v4 supports API key authentication, which is perfect for CLI tools. The authentication will follow this approach:

1. **API Key Storage**
   - Store in `~/.kit/config.json` with secure file permissions (600 on Unix)
   - Support environment variables for CI/CD: `KIT_API_KEY`
   - Multiple profile support for managing different accounts

2. **Authentication Flow**
   ```
   Authorization: Bearer <api-key>
   ```

3. **Configuration Priority**
   - Environment variables (highest)
   - Config file profiles
   - Interactive prompt (if missing)

## Command Structure

Following the resource-action pattern for intuitive usage:

### Core Commands

```bash
# Configuration
kit config set --api-key <key>
kit config get
kit config test
kit config profile <name>

# Subscribers
kit subscriber list [--tag <tag>] [--status <status>]
kit subscriber get <id|email>
kit subscriber create --email <email> --first-name <name>
kit subscriber update <id> --tag <tag>
kit subscriber delete <id>
kit subscriber search <query>
kit subscriber export --format csv

# Broadcasts (Email Campaigns)
kit broadcast list [--status draft|sent|scheduled]
kit broadcast get <id>
kit broadcast create --subject <subject> --content <file>
kit broadcast update <id> --subject <subject>
kit broadcast send <id>
kit broadcast schedule <id> --send-at <datetime>
kit broadcast stats <id>
kit broadcast delete <id>

# Sequences (Email Courses)
kit sequence list
kit sequence get <id>
kit sequence subscribers <id>
kit sequence add-subscriber <sequence-id> <subscriber-id>
kit sequence remove-subscriber <sequence-id> <subscriber-id>

# Tags
kit tag list
kit tag create --name <name>
kit tag delete <id>
kit tag subscribers <id>
kit tag add <tag-id> <subscriber-id>
kit tag remove <tag-id> <subscriber-id>

# Forms
kit form list
kit form get <id>
kit form stats <id>
kit form subscribers <id>

# Automations
kit automation list
kit automation get <id>
kit automation subscribers <id>

# Segments
kit segment list
kit segment get <id>
kit segment subscribers <id>

# Stats & Analytics
kit stats overview [--start-date <date>] [--end-date <date>]
kit stats broadcast <id>
kit stats sequence <id>
kit stats form <id>
kit stats export --format csv --output stats.csv

# Bulk Operations
kit bulk import --file subscribers.csv
kit bulk tag --file subscriber-ids.txt --tag <tag>
kit bulk export --segment <id> --format csv

# Webhooks
kit webhook list
kit webhook create --url <url> --event <event>
kit webhook delete <id>
kit webhook test <id>

# Utility Commands
kit update [--check]
kit version
kit help [command]
```

### Global Options

```bash
--format <table|json|csv>  # Output format
--output <file>            # Save output to file
--profile <name>           # Use specific config profile
--read-only               # Safe mode - no modifications
--limit <n>               # Limit results
--page <n>                # Pagination
--debug                   # Verbose output
```

## Data Models

### Core Models

```csharp
// Subscriber
public sealed class Subscriber
{
    public long Id { get; set; }
    public string EmailAddress { get; set; }
    public string? FirstName { get; set; }
    public string State { get; set; } // active, cancelled, bounced, complained, inactive
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
    public long[] TagIds { get; set; }
}

// Broadcast
public sealed class Broadcast
{
    public long Id { get; set; }
    public string Subject { get; set; }
    public string? PreviewText { get; set; }
    public string? FromName { get; set; }
    public string? FromEmail { get; set; }
    public string? Content { get; set; }
    public string? HtmlContent { get; set; }
    public string Status { get; set; } // draft, sent, scheduled
    public DateTime? SendAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public BroadcastStats? Stats { get; set; }
}

// Tag
public sealed class Tag
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int SubscriberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Form
public sealed class Form
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Format { get; set; }
    public string Url { get; set; }
    public int TotalSubscriptions { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Sequence
public sealed class Sequence
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int EmailCount { get; set; }
    public int SubscriberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### JSON Serialization Context (AOT)

```csharp
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Subscriber))]
[JsonSerializable(typeof(Subscriber[]))]
[JsonSerializable(typeof(Broadcast))]
[JsonSerializable(typeof(Broadcast[]))]
[JsonSerializable(typeof(Tag))]
[JsonSerializable(typeof(Tag[]))]
[JsonSerializable(typeof(Form))]
[JsonSerializable(typeof(Form[]))]
[JsonSerializable(typeof(Sequence))]
[JsonSerializable(typeof(Sequence[]))]
[JsonSerializable(typeof(PaginatedResponse<Subscriber>))]
[JsonSerializable(typeof(PaginatedResponse<Broadcast>))]
[JsonSerializable(typeof(BulkImportResult))]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(ErrorResponse))]
public partial class KitJsonContext : JsonSerializerContext
{
}
```

## Output Formatting

### Table Format (Default)
```
┌────────┬──────────────────────┬─────────────┬──────────┬───────────┐
│ ID     │ Email                │ Name        │ Status   │ Tags      │
├────────┼──────────────────────┼─────────────┼──────────┼───────────┤
│ 123456 │ john@example.com     │ John Doe    │ active   │ customer  │
│ 123457 │ jane@example.com     │ Jane Smith  │ active   │ lead      │
└────────┴──────────────────────┴─────────────┴──────────┴───────────┘
```

### JSON Format
```json
{
  "subscribers": [
    {
      "id": 123456,
      "email_address": "john@example.com",
      "first_name": "John Doe",
      "state": "active",
      "tags": ["customer"]
    }
  ],
  "pagination": {
    "page": 1,
    "per_page": 50,
    "total": 1250
  }
}
```

### CSV Format
```csv
id,email_address,first_name,state,tags,created_at
123456,john@example.com,John Doe,active,customer,2024-01-15T10:30:00Z
123457,jane@example.com,Jane Smith,active,lead,2024-01-16T11:45:00Z
```

## API Integration

### Rate Limiting
- Implement exponential backoff for 429 responses
- Track rate limit headers if provided
- Queue bulk operations with configurable concurrency

### Pagination
- Support cursor-based pagination (Kit v4 feature)
- Auto-fetch all pages with progress indicator for exports
- Stream large datasets to avoid memory issues

### Error Handling
- User-friendly error messages with recovery suggestions
- Detailed debug output with --debug flag
- Graceful degradation for network issues

## Testing Strategy

### Mock Server Implementation
```csharp
public class MockKitServer : HttpMessageHandler
{
    // Simulate Kit API responses for testing
    // Load sample data from TestData/ApiResponses/
    // Support error scenarios (401, 429, 500)
}
```

### Test Coverage
- Unit tests for all services
- Integration tests with mock server
- End-to-end command tests
- AOT compatibility verification

## Performance Targets

- **Startup Time**: < 100ms
- **Binary Size**: < 15MB
- **Memory Usage**: < 50MB for typical operations
- **Response Time**: < 500ms for simple queries

## Security Considerations

1. **Credential Security**
   - Never log API keys
   - Secure file permissions (600) on Unix
   - Support read-only mode for safe exploration
   - Mask API keys in output (show first/last 4 chars only)

2. **Input Validation**
   - Sanitize all user inputs
   - Validate email formats
   - Prevent command injection
   - Validate resource IDs

3. **Network Security**
   - Always use HTTPS
   - Verify SSL certificates
   - Implement request timeouts

## Distribution Plan

1. **Installers**
   - One-line install scripts for Unix/Windows
   - Homebrew formula for macOS
   - GitHub releases with checksums

2. **Self-Update Mechanism**
   - Check GitHub releases API
   - Download and replace binary atomically
   - Backup current version before update

3. **Platform Support**
   - linux-x64, linux-arm64
   - osx-x64, osx-arm64
   - win-x64

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [x] Project setup with AOT configuration
- [ ] Authentication and configuration service
- [ ] Basic HTTP client with rate limiting
- [ ] Core models and JSON contexts
- [ ] Mock server for testing

### Phase 2: Core Features (Week 2)
- [ ] Subscriber management commands
- [ ] Broadcast commands
- [ ] Tag management
- [ ] Output formatting (table, JSON, CSV)
- [ ] Error handling and progress indicators

### Phase 3: Advanced Features (Week 3)
- [ ] Sequences and automations
- [ ] Forms and segments
- [ ] Stats and analytics
- [ ] Bulk operations
- [ ] Export functionality

### Phase 4: Polish & Release (Week 4)
- [ ] Self-update mechanism
- [ ] Installation scripts
- [ ] Documentation
- [ ] CI/CD pipeline
- [ ] First release

## Success Metrics

- Fast startup (< 100ms)
- Small binary (< 15MB)
- Intuitive commands
- Comprehensive test coverage
- Cross-platform compatibility
- Active user community

## Next Steps

1. Create project structure
2. Implement authentication service
3. Build mock Kit server for testing
4. Implement core subscriber commands
5. Add output formatting
6. Create installation scripts
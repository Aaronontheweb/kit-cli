# Kit CLI - Claude Development Guide

## Project Overview
Kit CLI is a command-line interface for Kit (formerly ConvertKit) email marketing platform, optimized for analyzing large subscriber lists and campaign performance.

## Key Technical Constraints
- **AOT Compilation Required**: No reflection, use source generators
- **Performance Targets**: < 100ms startup, < 15MB binary
- **Large Datasets**: Must handle 100k+ subscribers efficiently
- **Streaming**: Use IAsyncEnumerable for pagination

## Architecture Decisions
- **.NET 9 with AOT**: For instant startup and small binaries
- **Simple command routing**: No complex frameworks (AOT incompatible)
- **Bearer token auth**: Kit v4 API uses simple API keys
- **Cursor pagination**: Kit's v4 API pagination pattern
- **CSV streaming**: Memory-efficient exports

## Current Status
✅ Project structure created
✅ Core models (Subscriber, Broadcast, Tag)
✅ AOT-compatible JSON serialization
✅ Configuration service with secure storage
✅ Basic command routing

## Next Steps
1. Implement KitApiClient with authentication
2. Add subscriber list command with filtering
3. Implement CSV export for large datasets
4. Add broadcast engagement commands

## Code Style Guidelines
- Use sealed classes for models
- Async all the way (no .Result or .Wait())
- Use IAsyncEnumerable for streaming
- Validate all inputs
- Never log API keys

## Common Tasks

### Add a new command
1. Add to Program.cs RouteCommand switch
2. Create handler method
3. Update help text

### Add a new model
1. Create in Models/ folder
2. Add to KitJsonContext
3. Add to KitJsonIndentedContext if needed

### Test AOT compatibility
```bash
dotnet publish -c Release /p:PublishAot=true
```

## API Endpoints (Kit v4)
- Base URL: https://api.kit.com/v4
- Auth: Bearer token in header
- Pagination: cursor-based (after/before)

## Testing
- Use MockKitServer for integration tests
- Test with large datasets (100k+ records)
- Verify memory usage stays < 50MB

## Important Files
- `Program.cs` - Entry point and command routing
- `KitJsonContext.cs` - AOT serialization
- `Models/` - Data models
- `Services/KitApiClient.cs` - API client (TODO)

## Debugging Tips
- Check AOT warnings during build
- Use `--debug` flag for verbose output
- Monitor memory with `dotnet-counters`
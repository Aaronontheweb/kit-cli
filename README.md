# Kit CLI

A high-performance command-line interface for Kit (formerly ConvertKit) email marketing platform, optimized for analyzing large subscriber lists and campaign performance.

## Features

- **Blazing Fast**: < 100ms startup time with native AOT compilation  
- **Memory Efficient**: Handles 100k+ subscribers with streaming
- **Multiple Formats**: Export to CSV, JSON, or view as tables
- **Comprehensive Analytics**: Subscriber insights, campaign metrics, automation performance
- **Secure**: API keys stored securely with profile support

## Performance Metrics

- **Binary Size**: 8.9MB (target < 15MB) ✅
- **Startup Time**: 13ms (target < 100ms) ✅
- **Memory Usage**: < 50MB for 100k+ records ✅
- **AOT Compiled**: No JIT overhead ✅

## Installation

### Prerequisites

- .NET 9 SDK
- Kit API key (get from your Kit account settings)

### From Source

```bash
# Clone the repository
git clone https://github.com/Aaronontheweb/kit-cli.git
cd kit-cli

# Build with AOT compilation
dotnet publish -c Release /p:PublishAot=true -o ./publish

# Add to PATH (Linux/macOS)
sudo ln -s $(pwd)/publish/kit /usr/local/bin/kit

# Or on Windows, add the publish directory to your PATH
```

## Quick Start

1. **Configure your API key**:
```bash
kit config set --api-key YOUR_API_KEY
```

2. **Test the connection**:
```bash
kit config test
```

3. **List your subscribers**:
```bash
kit subscriber list
```

## Command Reference

### Configuration

```bash
# Set API key
kit config set --api-key YOUR_KEY

# View configuration
kit config get

# Test connection
kit config test

# Use profiles for multiple accounts
kit config set --api-key KEY1 --profile personal
kit config set --api-key KEY2 --profile work
```

### Subscribers

```bash
# List subscribers
kit subscriber list
kit subscriber list --status active --limit 100
kit subscriber list --format json

# Get subscriber details
kit subscriber get 12345
kit subscriber get user@example.com

# Search subscribers
kit subscriber search "john"
kit subscriber search --query "gmail.com" --status active

# Export subscribers (memory-efficient streaming)
kit subscriber export --output subscribers.csv
kit subscriber export --all --output all-subscribers.json
kit subscriber export --status cancelled --output unsubscribed.csv

# Advanced filtering
kit subscribers date-range --from 2024-01-01 --to 2024-12-31
kit subscribers inactive --days 90
kit subscribers unsubscribed --from 2024-06-01
```

### Broadcasts

```bash
# List broadcasts
kit broadcast list
kit broadcast list --status sent

# Get broadcast details
kit broadcast get 12345

# View statistics
kit broadcast stats 12345

# Engagement tracking
kit broadcast opened 12345
kit broadcast clicked 12345
kit broadcast unopened 12345

# Export broadcasts
kit broadcast export --output campaigns.csv
kit broadcast export --all --output all-broadcasts.json
```

### Tags

```bash
# List all tags
kit tag list

# Get subscribers for a tag
kit tag subscribers 123
kit tag subscribers 123 --limit 1000

# Export tags
kit tag export --output tags.csv
```

### Segments

```bash
# List segments
kit segment list

# Get segment details
kit segment get 123

# Analyze segment
kit segment analyze 123

# Compare segments
kit segment compare 123 456
```

### Sequences (Automations)

```bash
# List sequences
kit sequence list

# View emails in sequence
kit sequence emails 123

# Get performance stats
kit sequence stats 123

# Analyze effectiveness
kit sequence analyze 123
```

## Export Options

All list commands support export to file:

```bash
# Export as CSV (default)
kit subscriber list --output subscribers.csv

# Export as JSON
kit subscriber list --output subscribers.json

# Export all data (streams large datasets efficiently)
kit subscriber export --all --output all-data.csv

# Export with filters
kit subscriber export --status active --output active-users.csv
```

## Development

### Building

```bash
# Debug build
dotnet build

# Release build with AOT
dotnet publish -c Release /p:PublishAot=true

# Run tests
dotnet test
```

### Architecture

- **.NET 9 with AOT**: Native compilation for instant startup
- **Streaming APIs**: IAsyncEnumerable for memory efficiency  
- **Source Generators**: JSON serialization without reflection
- **Rate Limiting**: Built-in exponential backoff
- **Secure Storage**: Platform-specific credential storage

### Project Structure

```
src/
├── KitCLI/
│   ├── Commands/          # Command handlers
│   ├── Models/            # Data models
│   ├── Services/          # API client and services
│   └── Helpers/           # Utilities and formatters
└── KitCLI.Tests/          # Unit tests
```

### Key Components

- **KitApiClient**: Full-featured API client with authentication and rate limiting
- **ConfigurationService**: Secure credential storage with profile support
- **OutputFormatter**: Table, JSON, and CSV formatting
- **ProgressIndicator**: Real-time progress for long operations

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific tests
dotnet test --filter "FullyQualifiedName~SubscriberCommands"
```

## Environment Variables

- `KIT_API_KEY`: API key (overrides config file)
- `KIT_CONFIG_PATH`: Custom config file location
- `KIT_API_VERSION`: API version (default: v4)

## CI/CD

The project uses GitHub Actions for continuous integration with:
- Multi-platform testing (Windows, Linux, macOS)
- AOT compilation validation
- Code quality checks
- Automated releases with binary artifacts

See `.github/workflows/` for pipeline configuration.

## Troubleshooting

### Connection Issues

```bash
# Check configuration
kit config get

# Test connection
kit config test

# Verify API key
echo $KIT_API_KEY
```

### Performance Issues

```bash
# Use streaming for large datasets
kit subscriber export --all

# Limit results for testing
kit subscriber list --limit 10

# Use specific date ranges
kit subscribers date-range --from 2024-01-01 --to 2024-01-31
```

### Build Issues

```bash
# Clean and rebuild
dotnet clean
dotnet build

# Check AOT warnings
dotnet publish -c Release /p:PublishAot=true /p:TreatWarningsAsErrors=true
```

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## License

MIT License - see LICENSE file for details.

## Support

- **Issues**: https://github.com/Aaronontheweb/kit-cli/issues
- **Documentation**: This README and CLAUDE.md for development guidelines

## Acknowledgments

Built with .NET 9 and optimized for performance with native AOT compilation.
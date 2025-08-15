# Changelog

All notable changes to Kit CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-08-15

### Added

#### Core Features
- Full Kit API v4 client implementation with authentication
- AOT (Ahead-of-Time) compilation for instant startup (<100ms)
- Memory-efficient streaming for large datasets (100k+ records)
- Secure credential storage with profile support
- Rate limiting with exponential backoff

#### Commands
- **Configuration**: Set/get API keys, test connection, manage profiles
- **Subscribers**: List, get, search, export with advanced filtering
- **Broadcasts**: List, get stats, track engagement (opened/clicked/unopened)
- **Tags**: List, get subscribers, export to CSV/JSON
- **Segments**: List, get details, analyze, compare segments
- **Sequences**: List, view emails, get stats, analyze effectiveness

#### Export Capabilities
- CSV and JSON export formats
- Streaming export with `--all` flag for large datasets
- Memory-efficient processing (<50MB for 100k+ records)
- Filter support for all exports

#### Advanced Filtering
- Date range filtering for subscribers
- Inactive subscriber detection
- Unsubscribed user tracking
- Status-based filtering (active, cancelled, bounced, complained)

### Performance
- Binary size: 8.9MB (target <15MB achieved)
- Startup time: 13ms (target <100ms achieved)  
- Memory usage: <50MB for large datasets
- Native AOT compilation eliminates JIT overhead

### Infrastructure
- GitHub Actions CI/CD pipeline
- Multi-platform testing (Windows, Linux, macOS)
- Automated release pipeline with binary artifacts
- Comprehensive unit test suite (36 tests)
- Code quality checks and formatting

### Documentation
- Comprehensive README with examples
- CLAUDE.md development guidelines
- Inline help for all commands
- Environment variable documentation

## [Unreleased]

### Planned
- Forms API support
- Self-update mechanism
- Local caching for frequently accessed data
- Enhanced error handling and retry logic
- Webhook integration
- Bulk operations support
- Interactive mode
- Export scheduling
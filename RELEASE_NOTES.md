#### 0.1.0 November 15th 2025 ####

Initial release of Kit CLI - A command-line interface for Kit (formerly ConvertKit) email marketing platform.

**Features:**
- **Subscriber Management**: List, search, filter, and export subscriber data with advanced filtering options
- **Campaign Analytics**: Analyze broadcast performance with detailed engagement metrics
- **Segment Operations**: Create and manage subscriber segments with dynamic filters
- **Automation Tracking**: Monitor sequence performance and subscriber progress
- **Tag Management**: List and manage subscriber tags efficiently
- **Data Export**: Export to CSV or JSON for analysis in Excel or other tools
- **AOT Compilation**: Lightning-fast startup (<100ms) with small binary size (<15MB)
- **Streaming Pagination**: Handle 100k+ subscribers efficiently with memory-safe operations
- **Rate Limiting**: Built-in exponential backoff for API rate limits
- **Profile Support**: Manage multiple Kit accounts with configuration profiles

**Technical Highlights:**
- Built with .NET 9 AOT compilation for instant startup
- Memory-efficient streaming for large datasets
- Cross-platform support (Linux, macOS, Windows)
- ARM64 support for Apple Silicon Macs
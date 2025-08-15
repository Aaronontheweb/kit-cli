#### 1.0.0 August 15th 2025 ####

**New Features:**
- **Profile Management**: Comprehensive profile system for managing multiple Kit accounts (#23)
- **Forms Management**: Complete forms management commands for creating and managing opt-in forms (#16)
- **Self-Update Mechanism**: Built-in update capability to keep the CLI current (#17)
- **Advanced Install Scripts**: Pre-release, dry-run, and uninstall support for easier deployment (#19)

**Improvements:**
- **Enhanced Error Handling**: Circuit breaker pattern and user-friendly error messages (#15)
- **Sequence/Automation Support**: Full support for email drip campaigns and automation workflows (#6)
- **Segment Operations**: Advanced subscriber grouping and dynamic filtering (#5)
- **Advanced Filtering**: Improved subscriber filtering capabilities (#4)
- **Version Comparison**: Enhanced semantic versioning support for proper pre-release version handling

**Bug Fixes:**
- Fixed version comparison logic to properly handle pre-release versions (beta1 vs beta2)
- Fixed NuGet publishing workflow configuration (#21)
- Removed non-existent CHANGELOG.md reference from release notes (#20)
- Fixed release workflow version handling (#18)
- Corrected license reference and removed obsolete changelog (#14)

**Dependencies:**
- Updated System.Text.Json to 9.0.8 (#11)
- Updated Microsoft.Extensions.Http to 9.0.8 (#9)

**Documentation:**
- Comprehensive documentation and usage examples (#13)
- Restructured GitHub Actions workflow (#8)

#### 1.0.0-beta2 August 15th 2025 ####

**New Features:**
- **Profile Management**: Comprehensive profile system for managing multiple Kit accounts (#23)
- **Forms Management**: Complete forms management commands for creating and managing opt-in forms (#16)
- **Self-Update Mechanism**: Built-in update capability to keep the CLI current (#17)
- **Advanced Install Scripts**: Pre-release, dry-run, and uninstall support for easier deployment (#19)

**Improvements:**
- **Enhanced Error Handling**: Circuit breaker pattern and user-friendly error messages (#15)
- **Sequence/Automation Support**: Full support for email drip campaigns and automation workflows (#6)
- **Segment Operations**: Advanced subscriber grouping and dynamic filtering (#5)
- **Advanced Filtering**: Improved subscriber filtering capabilities (#4)

**Bug Fixes:**
- Fixed NuGet publishing workflow configuration (#21)
- Removed non-existent CHANGELOG.md reference from release notes (#20)
- Fixed release workflow version handling (#18)
- Corrected license reference and removed obsolete changelog (#14)

**Dependencies:**
- Updated System.Text.Json to 9.0.8 (#11)
- Updated Microsoft.Extensions.Http to 9.0.8 (#9)

**Documentation:**
- Comprehensive documentation and usage examples (#13)
- Restructured GitHub Actions workflow (#8)

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
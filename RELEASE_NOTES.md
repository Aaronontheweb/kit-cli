#### 1.1.2 January 6th 2026 ####

**New Features:**
- **Broadcast Click Analytics**: Added `kit broadcast clicks` command (with `clicks` alias) for detailed click data export (#91, #62)
  - Multiple output formats: table, json, csv
  - Export to file with --export flag (auto-detects format from extension)
  - Shows per-link click metrics including unique clicks and rates
  - AOT-compatible JSON serialization with new models

**Bug Fixes:**
- **Broadcast Stats Display**: Fixed incorrect percentage calculations for Kit V4 API rates (#89, #88)
  - Kit V4 API returns percentages (0-100), not decimals
  - Corrected display to show actual rates instead of multiplied values (e.g., 39.2% instead of 3919%)
  - Added regression test to prevent future issues

**Testing Improvements:**
- Added comprehensive unit tests for tag commands to verify functionality (#90, #76)

#### 1.1.1 January 6th 2026 ####

**Bug Fixes:**
- **Broadcast Analytics**: Fixed broadcast stats parsing and added per-URL click tracking support for Kit V4 API (#80, #77)
- **Subscriber Tags**: Fixed subscriber tags display by fetching tags via separate V4 API endpoint (#81, #75)
- **Segment & Sequence Counts**: Fixed misleading subscriber counts to show "N/A" instead of 0 when V4 API doesn't provide data (#82, #78, #79)

**Documentation:**
- **Known Limitations**: Added documentation section explaining Kit V4 API constraints and workarounds (#86)

#### 1.1.0 January 5th 2026 ####

**New Features:**
- **Broadcast Analytics**: Added `kit broadcast analyze` command for detailed broadcast performance analysis with engagement metrics, deliverability tracking, and performance ratings (#72, #58)
- **Comprehensive Help System**: Added `--help` flag support to all commands with usage examples and detailed descriptions (#27)

**Bug Fixes:**
- **Kit V4 API Compatibility**: Fixed authentication to use X-Kit-Api-Key header instead of Bearer token, corrected response parsing for V4 API format (#73)
- **Test Infrastructure**: Fixed race conditions in parallel test execution for console output tests (#29, #28, #26)

**Platform Updates:**
- **Upgraded to .NET 10**: Migrated from .NET 9 to .NET 10 SDK with updated AOT compilation support (#53)
- Updated test infrastructure dependencies: Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4
- Updated GitHub Actions workflows: setup-dotnet 5.0.1, download-artifact v7

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
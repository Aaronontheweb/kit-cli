#### 1.4.0 January 9th 2026 ####

**New Features:**
- **Broadcast Write Operations**: Added commands for managing broadcast drafts (#126, #70)
  - `kit broadcast create` - Create new broadcast drafts with HTML content
  - `kit broadcast update` - Update existing broadcast properties
  - `kit broadcast delete` - Delete broadcasts with confirmation prompt
  - Draft-only by design: scheduling intentionally not exposed via CLI for safety
  - Segment and tag targeting support via `--segment-id` and `--tag-id`
  - Template support with `--template-id` option
  - HTML content via `--content-file` (recommended) or inline `--content`
  - Preview text customization with `--preview-text`
  - Read-only mode protection via `--read-only` flag
  - Delete confirmation prompts (skip with `--force`)

**Bug Fixes:**
- **Subscriber Email Search**: Fixed JSON deserialization for `GetSubscriberByEmailAsync` method (#125, #124)
  - Corrected response type handling for Kit v4 API format (`subscribers` vs `data` field)
  - Added `--email` flag to `kit subscriber search` command for direct email lookup
  - Implemented case-insensitive email matching

#### 1.3.0 January 6th 2026 ####

**New Features:**
- **Account Analytics**: Added `kit account stats` command for aggregate email statistics (#117, #102)
  - View total sent, opened, and clicked counts
  - Display engagement rates (open rate, click rate)
  - Shows tracking status (open/click tracking enabled)
  - Supports table, JSON, and CSV output formats

- **Top Broadcast Analysis**: Added `kit broadcast top` command to identify best-performing broadcasts (#118, #61)
  - Rank broadcasts by metric: opens, clicks, or engagement score
  - Engagement scoring: `(OpenRate * 0.6) + (ClickRate * 0.4)`
  - Configurable time range with `--days` (default: 30)
  - Limit results with `--limit` (default: 10)
  - Export capabilities with progress indicators

- **Subscriber Engagement Scoring**: Added `kit subscriber scores` command for calculating engagement scores (#119, #63)
  - Multiple scoring algorithms: weighted (default), tags, maturity
  - Weighted algorithm balances tag engagement (50 pts), account maturity (30 pts), and active status (20 pts)
  - Tags algorithm: pure tag-based scoring (10 points per tag, max 100)
  - Maturity algorithm: account age focused (80 points at 365+ days + 20 for active)
  - Filter by segment and customize result limits
  - Export with multiple output formats

- **Cold Subscriber Detection**: Added `kit subscribers cold` command to find disengaged subscribers (#120, #64)
  - Uses proxy signals: account age + tag count to identify cold subscribers
  - Configurable thresholds: `--min-days-old` (default: 90) and `--max-tags` (default: 2)
  - `--was-active` filter for previously-engaged subscribers
  - Engagement tier breakdown and export capabilities
  - Helps identify subscribers for re-engagement campaigns or list cleanup

**Bug Fixes:**
- **JSON Serialization**: Fixed `subscriber list --format json` JsonTypeInfo error by changing `Fields` type to `Dictionary<string, JsonElement>` (#116, #115)
- **Subscriber Export Limits**: Fixed export to include all subscribers by default; added `--limit` option to restrict (#116, #114)
- **State Filtering**: Fixed cancelled subscriber queries by applying state filter server-side via API parameter (#116, #113)
- **Form/Segment Subscriber Parsing**: Corrected response type handling for form and segment subscriber queries (#116, #112)

#### 1.2.0 January 6th 2026 ####

**New Features:**
- **Cohort Analysis**: Added suite of cohort analysis commands for retention and lead source tracking
  - `kit cohort by-signup` - Retention analysis by signup date (#94)
  - `kit cohort by-tag` - Tag-based cohort analysis (#96)
  - `kit cohort by-form` - Lead source analysis by form (#99)
  - Shows subscriber retention over time with configurable periods (daily, weekly, monthly)

- **Broadcast Analytics**: Enhanced broadcast performance tracking and comparison
  - `kit broadcast trends` - Performance trend analysis over time (#106)
  - `kit broadcast compare` - Side-by-side comparison of multiple broadcasts (#109, #59)
  - Identify best-performing broadcasts and content patterns

- **Form Analytics**: New form performance tracking capabilities
  - `kit form trends` - Signup trend analysis for forms (#110)
  - `kit form compare` - Compare performance across multiple forms (#108)
  - Track conversion rates and signup patterns over time

**Bug Fixes:**
- **Sequence Display**: Removed misleading subscriber/email columns from sequence list when Kit API v4 doesn't provide data (#104)
- **Segment Subscriber Display**: Fixed segment subscriber count display to accurately reflect Kit API v4 data (#96)
- **API Compatibility**: Added proper handling for missing Kit API v4 sequence endpoints to prevent errors (#103)

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
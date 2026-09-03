# Kit CLI

A high-performance command-line interface for Kit (formerly ConvertKit) email marketing platform, optimized for analyzing large subscriber lists and campaign performance.

## Features

- **Blazing Fast**: < 100ms startup time with native AOT compilation  
- **Memory Efficient**: Handles 100k+ subscribers with streaming
- **Multiple Formats**: Export to CSV, JSON, or view as tables
- **Comprehensive Analytics**: Subscriber insights, campaign metrics, automation performance
- **Multi-Profile Support**: Manage multiple Kit accounts with automatic profile switching
- **Secure**: API keys stored securely with platform-specific credential storage

## Performance Metrics

- **Binary Size**: 8.9MB (target < 15MB) ✅
- **Startup Time**: 13ms (target < 100ms) ✅
- **Memory Usage**: < 50MB for 100k+ records ✅
- **AOT Compiled**: No JIT overhead ✅

## Installation

### Quick Install

#### Linux/macOS
```bash
# Install latest stable release
curl -sSL https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.sh | bash

# Or install latest beta/pre-release
curl -sSL https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.sh | bash -s -- --beta
```

#### Windows (PowerShell)
```powershell
# Install latest stable release
iwr -useb https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.ps1 | iex

# Or install latest beta/pre-release with custom directory
.\install.ps1 -Beta -InstallDir "C:\tools\kit"
```

### Advanced Installation Options

#### Dry Run (test without installing)
```bash
# Linux/macOS
curl -sSL https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.sh | bash -s -- --dry-run

# Windows
iwr -useb https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.ps1 | iex -DryRun
```

#### Uninstall
```bash
# Linux/macOS
curl -sSL https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.sh | bash -s -- --uninstall

# Windows
iwr -useb https://raw.githubusercontent.com/Aaronontheweb/kit-cli/dev/install.ps1 | iex -Uninstall
```

### From Source

Prerequisites:
- .NET 10 SDK
- Kit V4 API key (see [Authentication](#authentication) below)

```bash
# Clone the repository
git clone https://github.com/Aaronontheweb/kit-cli.git
cd kit-cli

# Build with AOT compilation
dotnet publish src/KitCLI -c Release /p:PublishAot=true -o ./publish

# Add to PATH (Linux/macOS)
sudo ln -s $(pwd)/publish/kit /usr/local/bin/kit

# Or on Windows, add the publish directory to your PATH
```

## Authentication

Kit CLI uses the [Kit V4 API](https://developers.kit.com/api-reference/authentication) which requires an API key for authentication.

### Getting Your API Key

1. Log in to your Kit account at [app.kit.com](https://app.kit.com)
2. Navigate to **Settings → Developer → API**
3. Copy your **V4 API key**

The API key is sent with each request using the `X-Kit-Api-Key` header. Kit CLI stores your API key securely using platform-specific credential storage.

### Quick Start

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
# Set API key (creates default profile)
kit config set --api-key YOUR_KEY

# View configuration
kit config get

# Test connection
kit config test
```

#### Profile Management

Kit CLI supports multiple profiles for managing different accounts or environments:

```bash
# Create profiles for different accounts
kit config set --api-key PERSONAL_KEY --profile personal
kit config set --api-key WORK_KEY --profile work

# First profile automatically becomes default
# Additional profiles prompt to set as default:
# "Set 'work' as default profile? (current: personal) [y/N]:"

# Force set a profile as default
kit config set --api-key STAGING_KEY --profile staging --set-default

# List all profiles (shows current default)
kit config profiles
# Current default profile: personal
# 
# Available profiles:
#   * personal
#      API Key: kit_...1234
#     work  
#      API Key: kit_...5678

# Switch default profile
kit config profile work

# Use specific profile for commands
kit subscriber list --profile work
kit config test --profile staging
kit config get --profile personal

# Profile shown in verbose mode
kit subscriber list --verbose --profile work
# [Profile: work]
# [subscriber output...]
```

### Global Flags

All commands support these global flags:

```bash
# Use specific profile (overrides default)
kit <command> --profile PROFILE_NAME

# Enable verbose output (shows profile and detailed logging)
kit <command> --verbose

# Read-only mode (prevents write operations)
kit <command> --read-only
```

### Subscribers

```bash
# List subscribers
kit subscriber list
kit subscriber list --status active --limit 100
kit subscriber list --format json

# Use with different profiles
kit subscriber list --profile work
kit subscriber list --profile staging --verbose

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

# Create broadcast draft (HTML content from file)
kit broadcast create --subject "Monthly Newsletter" --content-file newsletter.html
kit broadcast create --subject "Promo" --content-file promo.html --segment-id 123
kit broadcast create --subject "Tagged Users" --content-file email.html --tag-id 456

# Create with inline content
kit broadcast create --subject "Quick Update" --content "<p>Hello!</p>"

# Create with template and preview text
kit broadcast create --subject "Newsletter" --content-file email.html \
  --template-id 789 --preview-text "This month's updates..."

# Update broadcast draft
kit broadcast update 12345 --subject "New Subject"
kit broadcast update 12345 --content-file updated.html
kit broadcast update 12345 --segment-id 456 --preview-text "Updated preview"

# Delete broadcast (with confirmation)
kit broadcast delete 12345
kit broadcast delete 12345 --force  # Skip confirmation
```

> **Note**: Broadcast scheduling is intentionally not supported via CLI for safety.
> Created broadcasts are always saved as drafts. Use the Kit UI to schedule sending.

### Tags

```bash
# List all tags
kit tag list

# Get subscribers for a tag
kit tag subscribers 123
kit tag subscribers 123 --limit 1000

# Export tags
kit tag export --output tags.csv

# Rename a tag
kit tag rename "VIP Customers" "VIPs"

# Add or remove a subscriber through the tag-first aliases
kit tag add-subscriber vip user@example.com
kit tag remove-subscriber vip user@example.com --force

# Apply or remove a tag for many subscribers
kit tag bulk-apply vip --file subscribers.csv
kit tag bulk-remove vip user1@example.com,user2@example.com --force
```

### Forms

```bash
# Subscribe one email address (or subscriber ID) to a form
kit form subscribe 123 user@example.com

# Include referral attribution
kit form subscribe 123 user@example.com --referrer https://example.com/newsletter

# Subscribe an inline list or a file of email addresses
kit form subscribe-bulk 123 user1@example.com,user2@example.com --force
kit form subscribe-bulk 123 emails.csv --referrer https://example.com/newsletter --force
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

# Include email content or performance statistics
kit sequence emails 123 --include-content
kit sequence emails 123 --include-stats --format json

# View a single sequence email
kit sequence email get 123 456 --format json

# Get performance stats
kit sequence stats 123

# Analyze effectiveness
kit sequence analyze 123
```

#### Editing a sequence email

`kit sequence email update` safely changes **only** the subject *or* the HTML content of an
existing sequence email. It never touches position, publish state, delay, send days, template, or
preview text, so it cannot reorder a sequence or trigger sends. A dry-run preview is the default;
writing requires both `--apply` and `--confirm-field-scope`, and is rejected under `--read-only`.

```bash
# Dry-run preview (default): shows the planned change, sends no PUT
kit sequence email update 123 456 --subject 'Hi {{ subscriber.first_name }}'

# Apply a subject change
kit sequence email update 123 456 --subject 'Hi {{ subscriber.first_name }}' --apply --confirm-field-scope

# Replace the HTML body from a file
kit sequence email update 123 456 --content-file ./body.html --apply --confirm-field-scope

# Guard against a stale overwrite (aborts if the live value has drifted)
kit sequence email update 123 456 --subject 'New subject' --expect-subject 'Old subject' --apply --confirm-field-scope
kit sequence email update 123 456 --content-file ./body.html --expect-content-sha256 <hex> --apply --confirm-field-scope

# Machine-readable operation report
kit sequence email update 123 456 --subject 'New subject' --format json
```

After a write, the command re-reads the email and verifies that only the requested field changed;
any drift in a protected field is reported and no compensating write is made.

#### Batch editing across many emails and sequences

`kit sequence email update-batch` applies a reviewed JSON **manifest** of single-field edits across
many emails — and across many sequences (drip campaigns) — in one guarded run. It carries the same
safety guarantees as the single-email `update`: each row sends **only** its target `subject` **or**
`content`, so it can never alter position, publish state, delay, send days, template, sender, or
preview text, and it can never reorder a sequence or trigger sends.

Before any write, a full **preflight** reads every referenced sequence and email and verifies:
identity (the email really belongs to the named sequence), the expected sequence name, the expected
publish state and position, and the per-row concurrency guard (exact subject, or content SHA-256)
against the **live** value. **If any row fails preflight, the entire batch is aborted with zero
writes.** Each applied row is then read back and verified exactly like the single-email command.

Dry-run is the default; writing requires `--apply` and `--confirm-field-scope`, and is rejected under
`--read-only`.

```bash
# Generate a candidate manifest from live state (guards pre-filled), then review/edit it
kit sequence email generate-manifest 123 --field subject --out ./remediation.json
kit sequence email generate-manifest 123 456 --field content --content-dir ./bodies --out ./remediation.json

# Dry-run (default): full preflight + planned-change report, no writes
kit sequence email update-batch ./remediation.json

# Apply, writing a redacted audit report
kit sequence email update-batch ./remediation.json --report ./run.json --apply --confirm-field-scope

# Resume a partially-completed run (skips rows already applied in the prior report)
kit sequence email update-batch ./remediation.json --resume ./run.json --apply --confirm-field-scope
```

Options: `--stop-on-error` (default; stop after the first failure) or `--continue-on-error`;
`--report <path>` writes a redacted JSON report (manifest SHA-256 for provenance, per-row results,
final counts — never the API key or raw HTML bodies); `--format text|json`.

**Manifest shape** — one independently reviewable, single-field replacement per row. Unknown keys are
rejected so a manifest can never broaden the mutation scope:

```json
{
  "schema_version": 1,
  "name": "Q3 first-name personalization",
  "source": "review matrix URL",
  "items": [
    {
      "sequence_id": 123,
      "expected_sequence_name": "Bootcamp 2.0",
      "email_id": 456,
      "field": "subject",
      "expect_subject": "Current exact subject",
      "replacement": "{% if subscriber.first_name != blank %}{{ subscriber.first_name }}, ...{% else %}...{% endif %}",
      "expected_published": true,
      "expected_position": 1
    },
    {
      "sequence_id": 123,
      "expected_sequence_name": "Bootcamp 2.0",
      "email_id": 789,
      "field": "content",
      "content_file": "./bodies/seq-123-email-789.html",
      "expect_content_sha256": "<lowercase hex sha-256 of the current body>",
      "expected_published": true,
      "expected_position": 2
    }
  ]
}
```

`content_file` paths are resolved relative to the manifest file, so keep the manifest and its HTML
bodies together. Publish/unpublish and reordering are deliberately **not** part of this command —
they are separate guarded commands (below).

#### Publishing and reordering sequence emails

Publish state and order are delivery-sensitive: publishing a `position: 0` email can make Kit
process queued subscribers (i.e. **trigger sends**), and reordering can make active subscribers
skip or repeat emails. These are therefore separate, individually guarded commands — never folded
into content edits. Each sends **only** its one field, is dry-run by default, requires `--apply`
plus a typed confirmation, is rejected under `--read-only`, and is verified by re-reading afterward.

```bash
# Publish / unpublish one email (dry-run first, then apply)
kit sequence email publish 123 456
kit sequence email publish 123 456 --apply --confirm-publish
kit sequence email unpublish 123 456 --apply --confirm-unpublish

# Publishing the first email (position 0) needs an extra confirmation (it can trigger sends)
kit sequence email publish 123 456 --apply --confirm-publish --confirm-first-email

# Reorder a sequence by declaring the complete intended email order (positions are 0-based).
# --order must be a permutation of the sequence's current email IDs (no adds/drops).
kit sequence email reorder 123 --order 456,457,789,790                       # dry-run: shows the moves
kit sequence email reorder 123 --order 456,457,789,790 --apply --confirm-reorder

# If the reorder promotes a published email into the first slot, it also needs --confirm-first-email
kit sequence email reorder 123 --order 789,456,457,790 --apply --confirm-reorder --confirm-first-email
```

`reorder` sends one `position` change per moved email, then re-reads the whole sequence and requires
the final order to match exactly; if it doesn't, it reports the discrepancy and makes no
compensating write. Creating/deleting sequences or emails, and changing delay/send-days, remain
Kit-UI operations for now.

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

- **.NET 10 with AOT**: Native compilation for instant startup
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

Configuration precedence (highest to lowest):
1. Environment variables (override everything)
2. `--profile` flag (overrides default profile)  
3. Default profile from config file
4. Default profile fallback

Available environment variables:
- `KIT_API_KEY`: API key (overrides all profile configurations)
- `KIT_CONFIG_PATH`: Custom config file location
- `KIT_API_VERSION`: API version (default: v4)
- `KIT_CLI_VERBOSE`: Enable verbose mode (1 = enabled)

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

## Known Limitations

Kit CLI uses the [Kit V4 API](https://developers.kit.com/v4) which has some limitations compared to what may be visible in the Kit web interface:

### Segment & Sequence Subscriber Counts
The V4 API does not return subscriber counts for segments or sequences in list/detail responses. These will display as "N/A" in tables and analysis output. To count subscribers in a segment or sequence, use:
```bash
kit segment subscribers <id> --all
kit sequence subscribers <id> --all
```

### Subscriber Tags
Tags for individual subscribers are fetched via a separate API call. When using `kit subscriber get`, tags are automatically retrieved.

### Broadcast Statistics
Click data requires a separate API call and is automatically fetched when using `kit broadcast analyze`.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

## License

Apache License 2.0 - see LICENSE file for details.

## Support

- **Issues**: https://github.com/Aaronontheweb/kit-cli/issues
- **Documentation**: This README and CLAUDE.md for development guidelines

## Acknowledgments

Built with .NET 10 and optimized for performance with native AOT compilation.

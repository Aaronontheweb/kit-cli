using System.Collections.Generic;

namespace KitCLI.Helpers;

public static class CommandHelp
{
    private static readonly Dictionary<string, CommandHelpInfo> HelpRegistry = new()
    {
        [""] = new CommandHelpInfo
        {
            Usage = "kit [command] [options]",
            Description = "Command-line interface for Kit (formerly ConvertKit) email marketing platform",
            Subcommands = new Dictionary<string, string>
            {
                ["profile"] = "Manage CLI profiles and configurations",
                ["subscriber"] = "Manage and analyze subscribers",
                ["broadcast"] = "Manage email broadcasts and campaigns",
                ["tag"] = "Manage subscriber tags",
                ["segment"] = "Manage subscriber segments",
                ["form"] = "Manage subscription forms",
                ["sequence"] = "Manage email sequences",
                ["webhook"] = "Manage webhooks",
                ["purchase"] = "Manage purchase data",
                ["export"] = "Export data to various formats",
                ["cohort"] = "Analyze subscriber cohorts over time",
                ["account"] = "View account-level statistics"
            },
            Options = new Dictionary<string, string>
            {
                ["--profile, -p <name>"] = "Use a specific profile",
                ["--debug"] = "Enable debug output",
                ["--help, -h"] = "Show help information",
                ["--version"] = "Show version information"
            }
        },
        ["config"] = new CommandHelpInfo
        {
            Usage = "kit config <subcommand> [options]",
            Description = "Manage Kit CLI configuration (legacy - use 'kit profile' instead)",
            Subcommands = new Dictionary<string, string>
            {
                ["set"] = "Set configuration values",
                ["get"] = "Get current configuration",
                ["test"] = "Test connection to Kit API",
                ["profile"] = "Switch to a different profile",
                ["profiles"] = "List all configured profiles"
            }
        },
        ["config set"] = new CommandHelpInfo
        {
            Usage = "kit config set [options]",
            Description = "Set Kit configuration values",
            RequiredOptions = new Dictionary<string, string>
            {
                ["--api-key, -k <key>"] = "Kit API key"
            },
            Options = new Dictionary<string, string>
            {
                ["--profile, -p <name>"] = "Configuration profile name",
                ["--set-default, -d"] = "Set this profile as default"
            },
            Examples = new[]
            {
                "kit config set --api-key abc123xyz",
                "kit config set -k abc123xyz --profile work --set-default"
            }
        },
        ["config get"] = new CommandHelpInfo
        {
            Usage = "kit config get [options]",
            Description = "Display the current Kit configuration",
            Options = new Dictionary<string, string>
            {
                ["--profile, -p <name>"] = "Configuration profile to display"
            },
            Examples = new[] { "kit config get", "kit config get --profile work" }
        },
        ["config test"] = new CommandHelpInfo
        {
            Usage = "kit config test [options]",
            Description = "Test the connection to Kit API using current configuration",
            Options = new Dictionary<string, string>
            {
                ["--profile, -p <name>"] = "Profile to test"
            },
            Examples = new[] { "kit config test", "kit config test --profile work" }
        },
        ["profile"] = new CommandHelpInfo
        {
            Usage = "kit profile <subcommand> [options]",
            Description = "Manage Kit CLI profiles and configurations",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List all configured profiles",
                ["add"] = "Add a new profile",
                ["remove"] = "Remove a profile",
                ["set-default"] = "Set the default profile",
                ["test"] = "Test profile connection"
            }
        },
        ["profile list"] = new CommandHelpInfo
        {
            Usage = "kit profile list",
            Description = "List all configured profiles with their status",
            Examples = new[] { "kit profile list" }
        },
        ["profile add"] = new CommandHelpInfo
        {
            Usage = "kit profile add <name> [options]",
            Description = "Add a new Kit API profile",
            RequiredOptions = new Dictionary<string, string>
            {
                ["--api-key, -k <key>"] = "Kit API key"
            },
            Options = new Dictionary<string, string>
            {
                ["--set-default"] = "Set as default profile"
            },
            Examples = new[]
            {
                "kit profile add personal --api-key abc123xyz",
                "kit profile add work -k abc123xyz --set-default"
            }
        },
        ["profile remove"] = new CommandHelpInfo
        {
            Usage = "kit profile remove <name>",
            Description = "Remove a configured profile",
            Examples = new[] { "kit profile remove old-profile" }
        },
        ["profile set-default"] = new CommandHelpInfo
        {
            Usage = "kit profile set-default <name>",
            Description = "Set the default profile to use when --profile is not specified",
            Examples = new[] { "kit profile set-default personal" }
        },
        ["profile test"] = new CommandHelpInfo
        {
            Usage = "kit profile test [name]",
            Description = "Test connection to Kit API using specified or default profile",
            Options = new Dictionary<string, string>
            {
                ["--profile, -p <name>"] = "Profile to test (default: current profile)"
            },
            Examples = new[]
            {
                "kit profile test",
                "kit profile test --profile work"
            }
        },
        ["subscriber"] = new CommandHelpInfo
        {
            Usage = "kit subscriber <subcommand> [options]",
            Description = "Manage and analyze Kit subscribers",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List subscribers with filtering",
                ["get"] = "Get detailed subscriber information",
                ["search"] = "Search subscribers",
                ["stats"] = "Show subscriber statistics",
                ["export"] = "Export subscribers to CSV"
            }
        },
        ["subscriber list"] = new CommandHelpInfo
        {
            Usage = "kit subscriber list [options]",
            Description = "List subscribers with optional filtering and pagination",
            Options = new Dictionary<string, string>
            {
                ["--status, -s <status>"] = "Filter by status (active, bounced, cancelled, complained, inactive)",
                ["--tag, -t <tag>"] = "Filter by tag ID or name",
                ["--created-after <date>"] = "Filter by creation date (ISO 8601)",
                ["--created-before <date>"] = "Filter by creation date (ISO 8601)",
                ["--updated-after <date>"] = "Filter by update date (ISO 8601)",
                ["--updated-before <date>"] = "Filter by update date (ISO 8601)",
                ["--limit, -l <number>"] = "Number of results per page (default: 50, max: 100)",
                ["--after <cursor>"] = "Pagination cursor for next page",
                ["--before <cursor>"] = "Pagination cursor for previous page",
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)",
                ["--output, -o <file>"] = "Output to file instead of console"
            },
            Examples = new[]
            {
                "kit subscriber list --status active",
                "kit subscriber list --tag newsletter --limit 100",
                "kit subscriber list --created-after 2024-01-01 --format csv --output subs.csv"
            }
        },
        ["subscriber get"] = new CommandHelpInfo
        {
            Usage = "kit subscriber get <id-or-email> [options]",
            Description = "Get detailed information about a specific subscriber",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format (json, text) (default: text)",
                ["--include-tags"] = "Include all subscriber tags",
                ["--include-sequences"] = "Include sequence subscriptions",
                ["--include-purchases"] = "Include purchase history"
            },
            Examples = new[]
            {
                "kit subscriber get 12345",
                "kit subscriber get user@example.com",
                "kit subscriber get 12345 --include-tags --format json"
            }
        },
        ["subscriber search"] = new CommandHelpInfo
        {
            Usage = "kit subscriber search <query> [options]",
            Description = "Search subscribers by email or name",
            Options = new Dictionary<string, string>
            {
                ["--limit, -l <number>"] = "Maximum results (default: 50)",
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)"
            },
            Examples = new[]
            {
                "kit subscriber search john",
                "kit subscriber search @example.com --limit 100"
            }
        },
        ["subscriber stats"] = new CommandHelpInfo
        {
            Usage = "kit subscriber stats [options]",
            Description = "Display subscriber statistics and growth metrics",
            Options = new Dictionary<string, string>
            {
                ["--period, -p <period>"] = "Time period (day, week, month, year, all) (default: month)",
                ["--format, -f <format>"] = "Output format (text, json) (default: text)"
            },
            Examples = new[]
            {
                "kit subscriber stats",
                "kit subscriber stats --period year",
                "kit subscriber stats --format json"
            }
        },
        ["subscriber export"] = new CommandHelpInfo
        {
            Usage = "kit subscriber export [options]",
            Description = "Export subscribers to CSV file with streaming for large datasets",
            RequiredOptions = new Dictionary<string, string>
            {
                ["--output, -o <file>"] = "Output CSV file path"
            },
            Options = new Dictionary<string, string>
            {
                ["--status, -s <status>"] = "Filter by status",
                ["--tag, -t <tag>"] = "Filter by tag",
                ["--fields <fields>"] = "Comma-separated fields to export (default: all)",
                ["--batch-size <number>"] = "Batch size for streaming (default: 1000)"
            },
            Examples = new[]
            {
                "kit subscriber export --output all-subs.csv",
                "kit subscriber export -o active.csv --status active",
                "kit subscriber export -o tagged.csv --tag vip --fields email,name,created_at"
            }
        },
        ["subscribers"] = new CommandHelpInfo
        {
            Usage = "kit subscribers <subcommand> [options]",
            Description = "Advanced subscriber filtering and analysis",
            Subcommands = new Dictionary<string, string>
            {
                ["date-range"] = "Find subscribers by date range",
                ["inactive"] = "Find inactive subscribers",
                ["unsubscribed"] = "Find unsubscribed users"
            }
        },
        ["subscribers date-range"] = new CommandHelpInfo
        {
            Usage = "kit subscribers date-range [options]",
            Description = "Find subscribers created or updated within a date range",
            Options = new Dictionary<string, string>
            {
                ["--created-after <date>"] = "Filter by creation date (ISO 8601)",
                ["--created-before <date>"] = "Filter by creation date (ISO 8601)",
                ["--updated-after <date>"] = "Filter by update date (ISO 8601)",
                ["--updated-before <date>"] = "Filter by update date (ISO 8601)",
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)",
                ["--output, -o <file>"] = "Output to file"
            },
            Examples = new[]
            {
                "kit subscribers date-range --created-after 2024-01-01",
                "kit subscribers date-range --updated-after 2024-06-01 --format csv -o recent.csv"
            }
        },
        ["subscribers inactive"] = new CommandHelpInfo
        {
            Usage = "kit subscribers inactive [options]",
            Description = "Find subscribers who haven't engaged with recent broadcasts",
            Options = new Dictionary<string, string>
            {
                ["--days <number>"] = "Number of days to consider (default: 90)",
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)",
                ["--output, -o <file>"] = "Output to file"
            },
            Examples = new[]
            {
                "kit subscribers inactive --days 30",
                "kit subscribers inactive --days 180 --format csv -o inactive.csv"
            }
        },
        ["subscribers unsubscribed"] = new CommandHelpInfo
        {
            Usage = "kit subscribers unsubscribed [options]",
            Description = "Find all unsubscribed users",
            Options = new Dictionary<string, string>
            {
                ["--since <date>"] = "Unsubscribed since date (ISO 8601)",
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)",
                ["--output, -o <file>"] = "Output to file"
            },
            Examples = new[]
            {
                "kit subscribers unsubscribed",
                "kit subscribers unsubscribed --since 2024-01-01 --format csv -o unsubs.csv"
            }
        },
        ["campaign"] = new CommandHelpInfo
        {
            Usage = "kit campaign <subcommand> [options]",
            Description = "Campaign analysis and comparison tools",
            Subcommands = new Dictionary<string, string>
            {
                ["compare"] = "Compare performance of two campaigns"
            }
        },
        ["campaign compare"] = new CommandHelpInfo
        {
            Usage = "kit campaign compare <id1> <id2> [options]",
            Description = "Compare the performance metrics of two campaigns",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format (table, json) (default: table)"
            },
            Examples = new[]
            {
                "kit campaign compare 12345 67890",
                "kit campaign compare 12345 67890 --format json"
            }
        },
        ["broadcast"] = new CommandHelpInfo
        {
            Usage = "kit broadcast <subcommand> [options]",
            Description = "Manage email broadcasts and campaigns",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List broadcasts",
                ["get"] = "Get broadcast details",
                ["stats"] = "Show broadcast statistics",
                ["analyze"] = "Detailed single broadcast analysis",
                ["trends"] = "Analyze broadcast performance trends over time",
                ["compare"] = "Compare performance of multiple broadcasts",
                ["top"] = "Find top-performing broadcasts",
                ["export"] = "Export broadcast data"
            }
        },
        ["broadcast top"] = new CommandHelpInfo
        {
            Usage = "kit broadcast top [options]",
            Description = "Find the top-performing broadcasts by a specified metric. Ranks broadcasts by open rate, click rate, or engagement score.",
            Options = new Dictionary<string, string>
            {
                ["--metric, -m <metric>"] = "Metric to rank by: opens (default), clicks, engagement",
                ["--limit, -l <number>"] = "Number of top broadcasts to show (default: 10)",
                ["--days, -d <number>"] = "Look back period in days (default: 365)",
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export, -o <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit broadcast top",
                "kit broadcast top --metric clicks --limit 5",
                "kit broadcast top --metric engagement --days 180",
                "kit broadcast top --format json",
                "kit broadcast top --export top-broadcasts.csv"
            }
        },
        ["broadcast compare"] = new CommandHelpInfo
        {
            Usage = "kit broadcast compare --ids <id1,id2,...> [options]",
            Description = "Compare performance metrics across multiple broadcasts. Shows open rates, click rates, click-to-open rates, and identifies best performers.",
            RequiredOptions = new Dictionary<string, string>
            {
                ["--ids <ids>"] = "Comma-separated list of broadcast IDs to compare"
            },
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export, -o <path>"] = "Export to file (CSV or JSON based on extension)",
                ["--sort, -s <metric>"] = "Sort by metric: opens (default), clicks, cto, unsubs"
            },
            Examples = new[]
            {
                "kit broadcast compare --ids 123,456,789",
                "kit broadcast compare --ids 123,456 --format json",
                "kit broadcast compare --ids 123,456,789 --export comparison.csv",
                "kit broadcast compare --ids 123,456 --sort clicks"
            }
        },
        ["broadcast list"] = new CommandHelpInfo
        {
            Usage = "kit broadcast list [options]",
            Description = "List email broadcasts with filtering",
            Options = new Dictionary<string, string>
            {
                ["--status <status>"] = "Filter by status (draft, scheduled, sent)",
                ["--limit, -l <number>"] = "Results per page (default: 50)",
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)"
            },
            Examples = new[]
            {
                "kit broadcast list",
                "kit broadcast list --status sent --limit 10"
            }
        },
        ["broadcast get"] = new CommandHelpInfo
        {
            Usage = "kit broadcast get <id> [options]",
            Description = "Get detailed broadcast information including stats",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format (json, text) (default: text)",
                ["--include-stats"] = "Include detailed statistics"
            },
            Examples = new[]
            {
                "kit broadcast get 12345",
                "kit broadcast get 12345 --include-stats --format json"
            }
        },
        ["broadcast stats"] = new CommandHelpInfo
        {
            Usage = "kit broadcast stats <id> [options]",
            Description = "Display detailed broadcast performance statistics",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format (text, json, csv) (default: text)"
            },
            Examples = new[]
            {
                "kit broadcast stats 12345",
                "kit broadcast stats 12345 --format csv"
            }
        },
        ["broadcast trends"] = new CommandHelpInfo
        {
            Usage = "kit broadcast trends [options]",
            Description = "Analyze broadcast performance trends over time. Groups broadcasts by day, week, or month and shows engagement metrics, trend direction, and best performers.",
            Options = new Dictionary<string, string>
            {
                ["--days, -d <days>"] = "Lookback period in days (default: 365)",
                ["--group-by, -g <period>"] = "Group by: day, week, month (default: month)",
                ["--format, -f <format>"] = "Output format: table (default), json",
                ["--export, -o <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit broadcast trends",
                "kit broadcast trends --days 180 --group-by week",
                "kit broadcast trends --group-by month --format json",
                "kit broadcast trends --days 365 --export trends.csv"
            }
        },
        ["tag"] = new CommandHelpInfo
        {
            Usage = "kit tag <subcommand> [options]",
            Description = "Manage subscriber tags",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List all tags",
                ["get"] = "Get tag details",
                ["subscribers"] = "List subscribers with a tag"
            }
        },
        ["tag list"] = new CommandHelpInfo
        {
            Usage = "kit tag list [options]",
            Description = "List all tags in your account",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format (table, json, csv) (default: table)",
                ["--sort <field>"] = "Sort by field (name, created_at, subscriber_count)"
            },
            Examples = new[]
            {
                "kit tag list",
                "kit tag list --sort subscriber_count --format json"
            }
        },
        ["segment"] = new CommandHelpInfo
        {
            Usage = "kit segment <subcommand> [options]",
            Description = "Manage subscriber segments",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List all segments",
                ["get"] = "Get segment details",
                ["subscribers"] = "Get subscribers in segment",
                ["analyze"] = "Analyze segment composition",
                ["compare"] = "Compare two segments",
                ["export"] = "Export segments to file"
            }
        },
        ["sequence"] = new CommandHelpInfo
        {
            Usage = "kit sequence <subcommand> [options]",
            Description = "Manage email sequences (automations)",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List all sequences",
                ["get"] = "Get sequence details",
                ["emails"] = "List emails in sequence",
                ["subscribers"] = "Get subscribers in sequence",
                ["stats"] = "Get sequence statistics",
                ["analyze"] = "Analyze sequence performance"
            }
        },
        ["sequence list"] = new CommandHelpInfo
        {
            Usage = "kit sequence list [options]",
            Description = "List all email sequences. Note: Kit API does not return subscriber/email counts in the list response - use 'kit sequence stats <id>' for detailed metrics.",
            Options = new Dictionary<string, string>
            {
                ["--limit, -l <number>"] = "Maximum results (default: 50)",
                ["--format, -f <format>"] = "Output format: table (default), json"
            },
            Examples = new[]
            {
                "kit sequence list",
                "kit sequence list --limit 100",
                "kit sequence list --format json"
            }
        },
        ["sequence get"] = new CommandHelpInfo
        {
            Usage = "kit sequence get <id> [options]",
            Description = "Get details for a specific sequence",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format: table (default), json"
            },
            Examples = new[]
            {
                "kit sequence get 12345",
                "kit sequence get 12345 --format json"
            }
        },
        ["sequence stats"] = new CommandHelpInfo
        {
            Usage = "kit sequence stats <id>",
            Description = "Display sequence statistics including subscriber counts and email performance",
            Examples = new[]
            {
                "kit sequence stats 12345"
            }
        },
        ["form"] = new CommandHelpInfo
        {
            Usage = "kit form <subcommand> [options]",
            Description = "Manage subscription forms",
            Subcommands = new Dictionary<string, string>
            {
                ["list"] = "List all forms",
                ["get"] = "Get form details",
                ["subscribers"] = "Get form subscribers",
                ["compare"] = "Compare performance of multiple forms",
                ["trends"] = "Analyze form signup trends over time"
            }
        },
        ["form compare"] = new CommandHelpInfo
        {
            Usage = "kit form compare <id1> <id2> [id3...] [options]",
            Description = "Compare performance metrics of multiple forms. Analyzes subscriber counts, retention rates, recent signups, and daily averages to determine the best performing form.",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export, -o <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit form compare 12345 67890",
                "kit form compare 111 222 333 --format json",
                "kit form compare 12345 67890 --export comparison.csv"
            }
        },
        ["form trends"] = new CommandHelpInfo
        {
            Usage = "kit form trends [options]",
            Description = "Analyze form signup trends over time. Groups signups by day, week, or month and shows trend direction, retention rates, and best performing forms.",
            Options = new Dictionary<string, string>
            {
                ["--form-ids <ids>"] = "Comma-separated form IDs to analyze (default: all forms)",
                ["--days, -d <days>"] = "Lookback period in days (default: 365)",
                ["--group-by, -g <period>"] = "Group by: daily, weekly, monthly (default: monthly)",
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export, -o <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit form trends",
                "kit form trends --days 180 --group-by weekly",
                "kit form trends --form-ids 12345,67890 --format json",
                "kit form trends --days 365 --export form-trends.csv"
            }
        },
        ["export"] = new CommandHelpInfo
        {
            Usage = "kit export <type> [options]",
            Description = "Export data to various formats (optimized for large datasets)",
            Subcommands = new Dictionary<string, string>
            {
                ["subscribers"] = "Export all subscribers",
                ["broadcasts"] = "Export broadcast data",
                ["tags"] = "Export tag data",
                ["full"] = "Full account export"
            },
            Options = new Dictionary<string, string>
            {
                ["--output, -o <file>"] = "Output file path",
                ["--format <format>"] = "Export format (csv, json) (default: csv)",
                ["--compress"] = "Compress output with gzip"
            },
            Examples = new[]
            {
                "kit export subscribers --output subs.csv",
                "kit export full --output backup.json --compress"
            }
        },
        ["cohort"] = new CommandHelpInfo
        {
            Usage = "kit cohort <subcommand> [options]",
            Description = "Analyze subscriber cohorts over time to understand engagement patterns",
            Subcommands = new Dictionary<string, string>
            {
                ["by-signup"] = "Analyze engagement by signup date cohort",
                ["by-tag"] = "Compare subscriber cohorts by tag",
                ["by-form"] = "Analyze lead source quality by signup form"
            }
        },
        ["cohort by-signup"] = new CommandHelpInfo
        {
            Usage = "kit cohort by-signup [options]",
            Description = "Track engagement decay by signup date. Groups subscribers by when they signed up and analyzes retention over time.",
            Options = new Dictionary<string, string>
            {
                ["--period, -p <period>"] = "Cohort period: weekly, monthly (default), quarterly",
                ["--metric, -m <metric>"] = "Metric: retention (default), engagement",
                ["--days, -d <days>"] = "Lookback days (default: 365)",
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit cohort by-signup",
                "kit cohort by-signup --period quarterly",
                "kit cohort by-signup --days 730 --export cohorts.csv",
                "kit cohort by-signup --period weekly --format json"
            }
        },
        ["cohort by-tag"] = new CommandHelpInfo
        {
            Usage = "kit cohort by-tag [options]",
            Description = "Compare subscriber cohorts by tag. Analyzes retention and engagement metrics for subscribers with specific tags.",
            Options = new Dictionary<string, string>
            {
                ["--tags, -t <tags>"] = "Comma-separated list of tag names or IDs to compare",
                ["--pattern, -p <pattern>"] = "Glob pattern to match tag names (e.g., 'training-*')",
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit cohort by-tag --tags newsletter,webinar,course",
                "kit cohort by-tag --pattern 'lead-*'",
                "kit cohort by-tag --tags 12345,67890 --format json",
                "kit cohort by-tag --pattern 'course-*' --export tag-cohorts.csv"
            }
        },
        ["cohort by-form"] = new CommandHelpInfo
        {
            Usage = "kit cohort by-form [options]",
            Description = "Analyze lead source quality by signup form. Compares retention and engagement metrics for subscribers from different forms.",
            Options = new Dictionary<string, string>
            {
                ["--form-ids <ids>"] = "Comma-separated form IDs to analyze (default: all forms)",
                ["--days, -d <days>"] = "Lookback days (default: 365)",
                ["--compare"] = "Head-to-head comparison of two forms",
                ["--include-archived"] = "Include archived forms in analysis",
                ["--format, -f <format>"] = "Output format: table (default), json, csv",
                ["--export <path>"] = "Export to file (CSV or JSON based on extension)"
            },
            Examples = new[]
            {
                "kit cohort by-form",
                "kit cohort by-form --form-ids 12345,67890 --compare",
                "kit cohort by-form --days 180 --format json",
                "kit cohort by-form --include-archived --export form-cohorts.csv"
            }
        },
        ["account"] = new CommandHelpInfo
        {
            Usage = "kit account <subcommand> [options]",
            Description = "View account-level statistics and metrics",
            Subcommands = new Dictionary<string, string>
            {
                ["stats"] = "Show aggregate email statistics for the account"
            }
        },
        ["account stats"] = new CommandHelpInfo
        {
            Usage = "kit account stats [options]",
            Description = "Display aggregate email statistics for the account, including total sent, opened, and clicked counts with engagement rates.",
            Options = new Dictionary<string, string>
            {
                ["--format, -f <format>"] = "Output format: table (default), json, csv"
            },
            Examples = new[]
            {
                "kit account stats",
                "kit account stats --format json",
                "kit account stats -f csv"
            }
        }
    };

    public static bool CheckForHelp(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg == "--help" || arg == "-h")
            {
                return true;
            }
        }
        return false;
    }

    public static void ShowHelp(params string[] commandPath)
    {
        var key = string.Join(" ", commandPath);

        if (!HelpRegistry.TryGetValue(key, out var helpInfo))
        {
            Console.WriteLine($"No help available for '{key}'");
            return;
        }

        Console.WriteLine($"Usage: {helpInfo.Usage}");

        if (!string.IsNullOrEmpty(helpInfo.Description))
        {
            Console.WriteLine();
            Console.WriteLine(helpInfo.Description);
        }

        if (helpInfo.Subcommands?.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Subcommands:");
            foreach (var (name, desc) in helpInfo.Subcommands)
            {
                Console.WriteLine($"  {name,-15} {desc}");
            }
        }

        if (helpInfo.RequiredOptions?.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Required options:");
            foreach (var (option, desc) in helpInfo.RequiredOptions)
            {
                Console.WriteLine($"  {option,-35} {desc}");
            }
        }

        if (helpInfo.Options?.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Options:");
            foreach (var (option, desc) in helpInfo.Options)
            {
                Console.WriteLine($"  {option,-35} {desc}");
            }
        }

        if (helpInfo.Subcommands?.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Run 'kit {key} <subcommand> --help' for more information on a subcommand.");
        }

        if (helpInfo.Examples?.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Examples:");
            foreach (var example in helpInfo.Examples)
            {
                Console.WriteLine($"  {example}");
            }
        }
    }

    public static int ShowHelpAndReturn(params string[] commandPath)
    {
        ShowHelp(commandPath);
        return 0;
    }
}

public sealed class CommandHelpInfo
{
    public string Usage { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string>? Subcommands { get; set; }
    public Dictionary<string, string>? RequiredOptions { get; set; }
    public Dictionary<string, string>? Options { get; set; }
    public string[]? Examples { get; set; }
}


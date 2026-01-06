using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class BroadcastCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        string format = "table";
        int limit = 50;
        string? status = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                    {
                        limit = l;
                    }

                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        status = args[++i];
                    }

                    break;
            }
        }

        using var progress = new ProgressIndicator("Fetching broadcasts");

        var response = await client.GetBroadcastsAsync(limit);
        var broadcasts = response.Data;

        // Filter by status if specified
        if (!string.IsNullOrEmpty(status))
        {
            broadcasts = broadcasts.Where(b =>
                b.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        progress.Complete($"Found {broadcasts.Length:N0} broadcasts");

        OutputFormatter.PrintBroadcasts(broadcasts, format);
        return 0;
    }

    public static async Task<int> HandleGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast get <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid broadcast ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "json";

        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i];
            }
        }

        using var progress = new ProgressIndicator($"Fetching broadcast {id}");

        var broadcast = await client.GetBroadcastAsync(id);

        if (broadcast == null)
        {
            progress.Complete($"Broadcast not found: {id}");
            return 1;
        }

        progress.Complete($"Found broadcast: {broadcast.Subject}");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                broadcast,
                KitJsonIndentedContext.Default.Broadcast);
            Console.WriteLine(json);
        }
        else
        {
            OutputFormatter.PrintBroadcasts([broadcast], format);
        }

        return 0;
    }

    public static async Task<int> HandleStats(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast stats <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid broadcast ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";

        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i];
            }
        }

        using var progress = new ProgressIndicator($"Fetching broadcast statistics for {id}");

        var stats = await client.GetBroadcastStatsAsync(id);

        if (stats == null)
        {
            progress.Complete($"Broadcast not found: {id}");
            return 1;
        }

        progress.Complete("Retrieved broadcast statistics");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                stats,
                KitJsonIndentedContext.Default.BroadcastStats);
            Console.WriteLine(json);
        }
        else
        {
            OutputFormatter.PrintBroadcastStats(stats, id);
        }

        return 0;
    }

    public static async Task<int> HandleOpened(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast opened <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --output, -o <file>    Export to file");
            return 1;
        }

        if (!long.TryParse(args[0], out var broadcastId))
        {
            Console.WriteLine("Invalid broadcast ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";
        string? outputPath = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[++i];
                    }

                    break;
            }
        }

        using var progress = new ProgressIndicator($"Finding subscribers who opened broadcast {broadcastId}");

        // Get broadcast stats first
        var stats = await client.GetBroadcastStatsAsync(broadcastId);
        if (stats == null)
        {
            progress.Complete($"Broadcast not found: {broadcastId}");
            return 1;
        }

        // Kit V4 API provides emails_opened (total) and open_rate (as percentage 0-100)
        // Estimate unique opens from rate (divide by 100 since API returns percentage)
        var estimatedUniqueOpens = (int)(stats.Recipients * stats.OpenRate / 100.0);
        progress.Complete($"Found {stats.EmailsOpened:N0} opens ({stats.OpenRate:F1}%)");

        Console.WriteLine($"Broadcast opened {stats.EmailsOpened:N0} times ({stats.OpenRate:F1}% open rate)");
        Console.WriteLine($"Estimated unique opens: ~{estimatedUniqueOpens:N0} subscribers");
        Console.WriteLine("Note: Kit API doesn't provide a list of subscribers who opened.");

        return 0;
    }

    /// <summary>
    /// Handle the 'kit broadcast clicks' command - export detailed click data.
    /// This is an alias for HandleClicked.
    /// </summary>
    public static Task<int> HandleClicks(string[] args, IKitApiClient client)
        => HandleClicked(args, client);

    public static async Task<int> HandleClicked(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast clicks <id> [options]");
            Console.WriteLine("       kit broadcast clicked <id> [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --export, -o <file>    Export to file (format auto-detected from extension)");
            return 1;
        }

        if (!long.TryParse(args[0], out var broadcastId))
        {
            Console.WriteLine("Invalid broadcast ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";
        string? outputPath = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--export":
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[++i];
                    }

                    break;
            }
        }

        using var progress = new ProgressIndicator($"Fetching link clicks for broadcast {broadcastId}");

        // Get link clicks from the clicks endpoint
        var clicksResponse = await client.GetBroadcastClicksAsync(broadcastId);
        if (clicksResponse?.Broadcast == null)
        {
            progress.Complete($"Broadcast not found: {broadcastId}");
            return 1;
        }

        var clicks = clicksResponse.Broadcast.Clicks;
        var totalUniqueClicks = clicks.Sum(c => c.UniqueClicks);
        progress.Complete($"Found {clicks.Length} links with {totalUniqueClicks:N0} unique clicks");

        // Handle file export
        if (!string.IsNullOrEmpty(outputPath))
        {
            return await ExportClicksToFile(broadcastId, clicks, outputPath);
        }

        // Handle different output formats
        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintClicksJson(broadcastId, clicks);
                break;
            case "csv":
                PrintClicksCsv(broadcastId, clicks);
                break;
            case "table":
            default:
                PrintClicksTable(broadcastId, clicks, totalUniqueClicks);
                break;
        }

        return 0;
    }

    private static void PrintClicksTable(long broadcastId, LinkClick[] clicks, int totalUniqueClicks)
    {
        Console.WriteLine($"Link Clicks for Broadcast {broadcastId}");
        Console.WriteLine(new string('─', 80));
        Console.WriteLine($"Total links: {clicks.Length}");
        Console.WriteLine($"Total unique clicks: {totalUniqueClicks:N0}");
        Console.WriteLine();

        if (clicks.Length > 0)
        {
            Console.WriteLine($"{"Clicks",-10} {"CTR",-8} {"CTOR",-8} URL");
            Console.WriteLine(new string('─', 80));
            foreach (var click in clicks.OrderByDescending(c => c.UniqueClicks))
            {
                var displayUrl = click.Url.Length > 50 ? click.Url[..47] + "..." : click.Url;
                // Kit V4 API returns rates as percentages (0-100)
                Console.WriteLine($"{click.UniqueClicks,-10:N0} {click.ClickToDeliveryRate,-7:F2}% {click.ClickToOpenRate,-7:F2}% {displayUrl}");
            }
        }
        else
        {
            Console.WriteLine("No link clicks recorded for this broadcast.");
        }
    }

    private static void PrintClicksJson(long broadcastId, LinkClick[] clicks)
    {
        var output = new BroadcastClicksExport
        {
            BroadcastId = broadcastId,
            TotalLinks = clicks.Length,
            TotalUniqueClicks = clicks.Sum(c => c.UniqueClicks),
            Links = clicks.OrderByDescending(c => c.UniqueClicks).Select(c => new LinkClickExport
            {
                Url = c.Url,
                UniqueClicks = c.UniqueClicks,
                ClickToDeliveryRate = c.ClickToDeliveryRate,
                ClickToOpenRate = c.ClickToOpenRate
            }).ToArray()
        };
        var json = System.Text.Json.JsonSerializer.Serialize(output, KitJsonIndentedContext.Default.BroadcastClicksExport);
        Console.WriteLine(json);
    }

    private static void PrintClicksCsv(long broadcastId, LinkClick[] clicks)
    {
        Console.WriteLine("broadcast_id,url,unique_clicks,click_to_delivery_rate,click_to_open_rate");
        foreach (var click in clicks.OrderByDescending(c => c.UniqueClicks))
        {
            var escapedUrl = EscapeCsvField(click.Url);
            Console.WriteLine($"{broadcastId},{escapedUrl},{click.UniqueClicks},{click.ClickToDeliveryRate:F2},{click.ClickToOpenRate:F2}");
        }
    }

    private static async Task<int> ExportClicksToFile(long broadcastId, LinkClick[] clicks, string outputPath)
    {
        // Determine format from file extension
        var fileFormat = Path.GetExtension(outputPath).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!outputPath.Contains('.'))
        {
            outputPath += ".csv";
        }

        await using var writer = new StreamWriter(outputPath);

        if (fileFormat == "json")
        {
            var output = new BroadcastClicksExport
            {
                BroadcastId = broadcastId,
                TotalLinks = clicks.Length,
                TotalUniqueClicks = clicks.Sum(c => c.UniqueClicks),
                Links = clicks.OrderByDescending(c => c.UniqueClicks).Select(c => new LinkClickExport
                {
                    Url = c.Url,
                    UniqueClicks = c.UniqueClicks,
                    ClickToDeliveryRate = c.ClickToDeliveryRate,
                    ClickToOpenRate = c.ClickToOpenRate
                }).ToArray()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(output, KitJsonIndentedContext.Default.BroadcastClicksExport);
            await writer.WriteAsync(json);
        }
        else
        {
            // CSV format
            await writer.WriteLineAsync("broadcast_id,url,unique_clicks,click_to_delivery_rate,click_to_open_rate");
            foreach (var click in clicks.OrderByDescending(c => c.UniqueClicks))
            {
                var escapedUrl = EscapeCsvField(click.Url);
                await writer.WriteLineAsync($"{broadcastId},{escapedUrl},{click.UniqueClicks},{click.ClickToDeliveryRate:F2},{click.ClickToOpenRate:F2}");
            }
        }

        Console.WriteLine($"✓ Exported {clicks.Length} link clicks to {outputPath}");
        return 0;
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return "";
        }

        field = field.Replace("\"", "\"\"");

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field}\"";
        }

        return field;
    }

    public static async Task<int> HandleUnopened(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast unopened <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --output, -o <file>    Export to file");
            return 1;
        }

        if (!long.TryParse(args[0], out var broadcastId))
        {
            Console.WriteLine("Invalid broadcast ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";
        string? outputPath = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[++i];
                    }

                    break;
            }
        }

        using var progress = new ProgressIndicator($"Finding subscribers who didn't open broadcast {broadcastId}");

        // Get broadcast stats first
        var stats = await client.GetBroadcastStatsAsync(broadcastId);
        if (stats == null)
        {
            progress.Complete($"Broadcast not found: {broadcastId}");
            return 1;
        }

        // Estimate unique opens from rate (Kit V4 API returns percentage 0-100)
        var estimatedUniqueOpens = (int)(stats.Recipients * stats.OpenRate / 100.0);
        var unopened = stats.Recipients - estimatedUniqueOpens;
        var unopenedRate = 100.0 - stats.OpenRate;

        progress.Complete($"Found ~{unopened:N0} subscribers who didn't open ({unopenedRate:F1}%)");

        Console.WriteLine($"Estimated {unopened:N0} subscribers didn't open ({unopenedRate:F1}%)");
        Console.WriteLine("Note: Kit API doesn't provide a list of subscribers who didn't open.");

        return 0;
    }

    public static async Task<int> HandleAnalyze(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast analyze <id> [options]");
            Console.WriteLine();
            Console.WriteLine("Provides detailed analysis of a single broadcast, including engagement metrics.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json)");
            Console.WriteLine("  --export <file>        Export analysis to CSV or JSON file");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  kit broadcast analyze 12345");
            Console.WriteLine("  kit broadcast analyze 12345 --export analysis.json");
            return 1;
        }

        if (!long.TryParse(args[0], out var broadcastId))
        {
            Console.WriteLine("Invalid broadcast ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";
        string? exportPath = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }
                    break;
                case "--export":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }
                    break;
            }
        }

        using var progress = new ProgressIndicator($"Analyzing broadcast {broadcastId}");

        // Fetch broadcast, stats, and clicks in parallel
        var broadcastTask = client.GetBroadcastAsync(broadcastId);
        var statsTask = client.GetBroadcastStatsAsync(broadcastId);
        var clicksTask = client.GetBroadcastClicksAsync(broadcastId);

        await Task.WhenAll(broadcastTask, statsTask, clicksTask);

        var broadcast = await broadcastTask;
        var stats = await statsTask;
        var clicksResponse = await clicksTask;

        if (broadcast == null)
        {
            progress.Complete($"Broadcast not found: {broadcastId}");
            return 1;
        }

        progress.Complete($"Analyzed: {broadcast.Subject}");

        // Build analysis results
        var recipients = stats?.Recipients ?? 0;
        // Estimate unique opens from rate (Kit V4 API returns percentage 0-100)
        var estimatedUniqueOpens = (int)(recipients * (stats?.OpenRate ?? 0) / 100.0);

        // Get actual unique clicks by summing from the clicks endpoint
        var linkClicks = clicksResponse?.Broadcast?.Clicks ?? [];
        var uniqueClicks = linkClicks.Sum(c => c.UniqueClicks);

        // Map link clicks to analysis format
        // Kit V4 API returns rates as percentages (0-100), normalize to decimals (0-1)
        var linkClickAnalysis = linkClicks
            .OrderByDescending(c => c.UniqueClicks)
            .Select(c => new LinkClickAnalysis
            {
                Url = c.Url,
                UniqueClicks = c.UniqueClicks,
                ClickToDeliveryRate = c.ClickToDeliveryRate / 100.0,
                ClickToOpenRate = c.ClickToOpenRate / 100.0
            })
            .ToArray();

        var analysis = new BroadcastAnalysis
        {
            BroadcastId = broadcastId,
            Subject = broadcast.Subject,
            Status = broadcast.Status,
            SendAt = broadcast.SendAt,
            CreatedAt = broadcast.CreatedAt,
            FromName = broadcast.FromName,
            FromEmail = broadcast.FromEmail,
            Recipients = recipients,
            UniqueOpens = estimatedUniqueOpens,
            TotalOpens = stats?.EmailsOpened ?? 0,
            UniqueClicks = uniqueClicks,
            TotalClicks = stats?.TotalClicks ?? 0,
            Unsubscribes = stats?.Unsubscribes ?? 0,
            // Kit V4 API returns rates as percentages (0-100), normalize to decimals (0-1)
            OpenRate = (stats?.OpenRate ?? 0) / 100.0,
            ClickRate = (stats?.ClickRate ?? 0) / 100.0,
            ClickToOpenRate = (stats?.ClickToOpenRate ?? 0) / 100.0,
            LinkClicks = linkClickAnalysis
        };

        // Output
        if (!string.IsNullOrEmpty(exportPath))
        {
            await ExportAnalysis(analysis, exportPath);
            Console.WriteLine($"✓ Analysis exported to {exportPath}");
        }

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                analysis,
                KitJsonIndentedContext.Default.BroadcastAnalysis);
            Console.WriteLine(json);
        }
        else
        {
            PrintAnalysis(analysis);
        }

        return 0;
    }

    private static void PrintAnalysis(BroadcastAnalysis analysis)
    {
        Console.WriteLine();
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine($"  BROADCAST ANALYSIS");
        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        // Header info
        Console.WriteLine($"  Subject:    {analysis.Subject}");
        Console.WriteLine($"  From:       {analysis.FromName} <{analysis.FromEmail}>");
        Console.WriteLine($"  Status:     {analysis.Status.ToUpperInvariant()}");

        if (analysis.SendAt.HasValue)
        {
            Console.WriteLine($"  Sent:       {analysis.SendAt:yyyy-MM-dd HH:mm:ss} UTC");
        }

        Console.WriteLine($"  Recipients: {analysis.Recipients:N0}");
        Console.WriteLine();

        // Engagement metrics
        Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  ENGAGEMENT METRICS");
        Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine();

        // Opens
        var openBar = GenerateProgressBar(analysis.OpenRate, 30);
        Console.WriteLine($"  Opens:         {analysis.UniqueOpens:N0} unique ({analysis.TotalOpens:N0} total)");
        Console.WriteLine($"  Open Rate:     {analysis.OpenRate * 100:F1}%  {openBar}");
        Console.WriteLine();

        // Clicks
        var clickBar = GenerateProgressBar(analysis.ClickRate, 30);
        Console.WriteLine($"  Clicks:        {analysis.UniqueClicks:N0} unique ({analysis.TotalClicks:N0} total)");
        Console.WriteLine($"  Click Rate:    {analysis.ClickRate * 100:F1}%  {clickBar}");
        Console.WriteLine();

        // Click-to-open rate (engagement of opened emails)
        if (analysis.UniqueOpens > 0)
        {
            var ctoBar = GenerateProgressBar(analysis.ClickToOpenRate, 30);
            Console.WriteLine($"  Click-to-Open: {analysis.ClickToOpenRate * 100:F1}%  {ctoBar}");
            Console.WriteLine($"                 (% of openers who clicked)");
            Console.WriteLine();
        }

        // Link clicks breakdown
        if (analysis.LinkClicks.Length > 0)
        {
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine($"  LINK CLICKS (Top {Math.Min(analysis.LinkClicks.Length, 10)})");
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine();

            foreach (var link in analysis.LinkClicks.Take(10))
            {
                var displayUrl = link.Url.Length > 50 ? link.Url[..47] + "..." : link.Url;
                Console.WriteLine($"  {link.UniqueClicks,5:N0} clicks  {displayUrl}");
            }

            if (analysis.LinkClicks.Length > 10)
            {
                Console.WriteLine($"  ... and {analysis.LinkClicks.Length - 10} more links");
            }
            Console.WriteLine();
        }

        // Negative metrics
        if (analysis.Unsubscribes > 0)
        {
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine($"  DELIVERABILITY");
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine();

            var unsubRate = analysis.Recipients > 0 ? (double)analysis.Unsubscribes / analysis.Recipients : 0;
            Console.WriteLine($"  Unsubscribes:  {analysis.Unsubscribes:N0} ({unsubRate * 100:F2}%)");
            Console.WriteLine();
        }

        // Performance summary
        Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  PERFORMANCE SUMMARY");
        Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
        Console.WriteLine();

        var rating = GetPerformanceRating(analysis);
        Console.WriteLine($"  Overall: {rating}");
        Console.WriteLine();

        Console.WriteLine($"═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    private static string GenerateProgressBar(double percentage, int width)
    {
        var filled = (int)(percentage * width);
        var empty = width - filled;
        return $"[{"█".PadRight(filled, '█')}{"░".PadRight(empty, '░')}]";
    }

    private static string GetPerformanceRating(BroadcastAnalysis analysis)
    {
        // Industry benchmarks (approximate):
        // Open rate: 20-40% is good, >40% is excellent
        // Click rate: 2-5% is good, >5% is excellent
        // Unsubscribe rate: <0.5% is good

        var openScore = analysis.OpenRate switch
        {
            >= 0.40 => 3, // Excellent
            >= 0.25 => 2, // Good
            >= 0.15 => 1, // Average
            _ => 0        // Below average
        };

        var clickScore = analysis.ClickRate switch
        {
            >= 0.10 => 3, // Excellent
            >= 0.05 => 2, // Good
            >= 0.02 => 1, // Average
            _ => 0        // Below average
        };

        var unsubRate = analysis.Recipients > 0 ? (double)analysis.Unsubscribes / analysis.Recipients : 0;
        var deliveryScore = unsubRate switch
        {
            <= 0.001 => 3, // Excellent
            <= 0.005 => 2, // Good
            <= 0.01 => 1,  // Average
            _ => 0         // High unsubscribes
        };

        var totalScore = openScore + clickScore + deliveryScore;

        return totalScore switch
        {
            >= 8 => "⭐⭐⭐⭐⭐ EXCELLENT - Outstanding engagement!",
            >= 6 => "⭐⭐⭐⭐ GREAT - Above average performance",
            >= 4 => "⭐⭐⭐ GOOD - Solid engagement metrics",
            >= 2 => "⭐⭐ AVERAGE - Room for improvement",
            _ => "⭐ BELOW AVERAGE - Consider optimizing subject line and content"
        };
    }

    private static async Task ExportAnalysis(BroadcastAnalysis analysis, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        using var writer = new StreamWriter(path);

        if (extension == ".json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                analysis,
                KitJsonIndentedContext.Default.BroadcastAnalysis);
            await writer.WriteAsync(json);
        }
        else // CSV
        {
            await writer.WriteLineAsync("metric,value");
            await writer.WriteLineAsync($"broadcast_id,{analysis.BroadcastId}");
            await writer.WriteLineAsync($"subject,\"{EscapeCsvField(analysis.Subject)}\"");
            await writer.WriteLineAsync($"status,{analysis.Status}");
            await writer.WriteLineAsync($"send_at,{analysis.SendAt:yyyy-MM-dd HH:mm:ss}");
            await writer.WriteLineAsync($"recipients,{analysis.Recipients}");
            await writer.WriteLineAsync($"unique_opens,{analysis.UniqueOpens}");
            await writer.WriteLineAsync($"total_opens,{analysis.TotalOpens}");
            await writer.WriteLineAsync($"open_rate,{analysis.OpenRate:F4}");
            await writer.WriteLineAsync($"unique_clicks,{analysis.UniqueClicks}");
            await writer.WriteLineAsync($"total_clicks,{analysis.TotalClicks}");
            await writer.WriteLineAsync($"click_rate,{analysis.ClickRate:F4}");
            await writer.WriteLineAsync($"click_to_open_rate,{analysis.ClickToOpenRate:F4}");
            await writer.WriteLineAsync($"unsubscribes,{analysis.Unsubscribes}");

            // Export link clicks as separate rows
            if (analysis.LinkClicks.Length > 0)
            {
                await writer.WriteLineAsync();
                await writer.WriteLineAsync("link_url,unique_clicks,click_to_delivery_rate,click_to_open_rate");
                foreach (var link in analysis.LinkClicks)
                {
                    await writer.WriteLineAsync($"\"{EscapeCsvField(link.Url)}\",{link.UniqueClicks},{link.ClickToDeliveryRate:F4},{link.ClickToOpenRate:F4}");
                }
            }
        }
    }

    public static async Task<int> HandleExport(string[] args, IKitApiClient client)
    {
        string outputPath = "broadcasts.csv";
        bool allBroadcasts = false;
        string? status = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[++i];
                    }

                    break;
                case "--all":
                    allBroadcasts = true;
                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        status = args[++i];
                    }

                    break;
            }
        }

        // Determine format from file extension
        var format = Path.GetExtension(outputPath).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!outputPath.Contains('.'))
        {
            outputPath += ".csv";
        }

        using var progress = new ProgressIndicator($"Exporting broadcasts to {outputPath}");

        List<Broadcast> broadcasts = new();
        string? cursor = null;
        bool hasMore = true;

        // Fetch broadcasts with pagination
        while (hasMore && (allBroadcasts || broadcasts.Count < 100))
        {
            var response = await client.GetBroadcastsAsync(100, cursor);

            foreach (var broadcast in response.Data)
            {
                if (status == null || broadcast.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                {
                    broadcasts.Add(broadcast);
                }
            }

            if (response.Pagination != null && allBroadcasts)
            {
                cursor = response.Pagination.EndCursor;
                hasMore = response.Pagination.HasNextPage;
            }
            else
            {
                hasMore = false;
            }
        }

        progress.Complete($"Exporting {broadcasts.Count:N0} broadcasts");

        // Write to file
        using var writer = new StreamWriter(outputPath);

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                broadcasts.ToArray(),
                KitJsonIndentedContext.Default.BroadcastArray);
            await writer.WriteAsync(json);
        }
        else
        {
            // CSV format
            await writer.WriteLineAsync("id,subject,status,send_at,recipients,opens,clicks,created_at");

            foreach (var broadcast in broadcasts)
            {
                var subject = EscapeCsvField(broadcast.Subject);
                var sendAt = broadcast.SendAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") ?? "";

                await writer.WriteLineAsync(
                    $"{broadcast.Id},{subject},{broadcast.Status},{sendAt},,,{broadcast.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}");
            }
        }

        Console.WriteLine($"✓ Exported {broadcasts.Count:N0} broadcasts to {outputPath}");
        return 0;
    }
}

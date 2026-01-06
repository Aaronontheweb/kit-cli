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
            OutputFormatter.PrintBroadcastStats(stats);
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

        // For now, we'll need to implement the API endpoint for getting opened subscribers
        // This is a placeholder that shows the pattern
        progress.Complete($"Found {stats.UniqueOpens:N0} unique opens ({stats.OpenRate:P1})");

        Console.WriteLine($"Broadcast opened by {stats.UniqueOpens:N0} subscribers ({stats.OpenRate:P1})");
        Console.WriteLine("Note: Detailed subscriber list for opens requires additional API implementation.");

        return 0;
    }

    public static async Task<int> HandleClicked(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit broadcast clicked <id> [options]");
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

        using var progress = new ProgressIndicator($"Finding subscribers who clicked links in broadcast {broadcastId}");

        // Get broadcast stats first
        var stats = await client.GetBroadcastStatsAsync(broadcastId);
        if (stats == null)
        {
            progress.Complete($"Broadcast not found: {broadcastId}");
            return 1;
        }

        progress.Complete($"Found {stats.UniqueClicks:N0} unique clicks ({stats.ClickRate:P1})");

        Console.WriteLine($"Broadcast clicked by {stats.UniqueClicks:N0} subscribers ({stats.ClickRate:P1})");
        Console.WriteLine("Note: Detailed subscriber list for clicks requires additional API implementation.");

        return 0;
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

        var unopened = stats.Recipients - stats.UniqueOpens;
        var unopenedRate = 1.0 - stats.OpenRate;

        progress.Complete($"Found {unopened:N0} subscribers who didn't open ({unopenedRate:P1})");

        Console.WriteLine($"Broadcast not opened by {unopened:N0} subscribers ({unopenedRate:P1})");
        Console.WriteLine("Note: Detailed subscriber list for unopened requires additional API implementation.");

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

        // Fetch broadcast and stats in parallel
        var broadcastTask = client.GetBroadcastAsync(broadcastId);
        var statsTask = client.GetBroadcastStatsAsync(broadcastId);

        await Task.WhenAll(broadcastTask, statsTask);

        var broadcast = await broadcastTask;
        var stats = await statsTask;

        if (broadcast == null)
        {
            progress.Complete($"Broadcast not found: {broadcastId}");
            return 1;
        }

        progress.Complete($"Analyzed: {broadcast.Subject}");

        // Build analysis results
        var analysis = new BroadcastAnalysis
        {
            BroadcastId = broadcastId,
            Subject = broadcast.Subject,
            Status = broadcast.Status,
            SendAt = broadcast.SendAt,
            CreatedAt = broadcast.CreatedAt,
            FromName = broadcast.FromName,
            FromEmail = broadcast.FromEmail,
            Recipients = stats?.Recipients ?? 0,
            UniqueOpens = stats?.UniqueOpens ?? 0,
            TotalOpens = stats?.Opens ?? 0,
            UniqueClicks = stats?.UniqueClicks ?? 0,
            TotalClicks = stats?.Clicks ?? 0,
            Unsubscribes = stats?.Unsubscribes ?? 0,
            Bounces = stats?.Bounces ?? 0,
            Complaints = stats?.Complaints ?? 0,
            OpenRate = stats?.OpenRate ?? 0,
            ClickRate = stats?.ClickRate ?? 0,
            ClickToOpenRate = stats?.ClickToOpenRate ?? 0
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
        Console.WriteLine($"  Open Rate:     {analysis.OpenRate:P1}  {openBar}");
        Console.WriteLine();

        // Clicks
        var clickBar = GenerateProgressBar(analysis.ClickRate, 30);
        Console.WriteLine($"  Clicks:        {analysis.UniqueClicks:N0} unique ({analysis.TotalClicks:N0} total)");
        Console.WriteLine($"  Click Rate:    {analysis.ClickRate:P1}  {clickBar}");
        Console.WriteLine();

        // Click-to-open rate (engagement of opened emails)
        if (analysis.UniqueOpens > 0)
        {
            var ctoBar = GenerateProgressBar(analysis.ClickToOpenRate, 30);
            Console.WriteLine($"  Click-to-Open: {analysis.ClickToOpenRate:P1}  {ctoBar}");
            Console.WriteLine($"                 (% of openers who clicked)");
            Console.WriteLine();
        }

        // Negative metrics
        if (analysis.Unsubscribes > 0 || analysis.Bounces > 0 || analysis.Complaints > 0)
        {
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine($"  DELIVERABILITY");
            Console.WriteLine($"  ─────────────────────────────────────────────────────────────────────────────");
            Console.WriteLine();

            if (analysis.Unsubscribes > 0)
            {
                var unsubRate = analysis.Recipients > 0 ? (double)analysis.Unsubscribes / analysis.Recipients : 0;
                Console.WriteLine($"  Unsubscribes:  {analysis.Unsubscribes:N0} ({unsubRate:P2})");
            }

            if (analysis.Bounces > 0)
            {
                var bounceRate = analysis.Recipients > 0 ? (double)analysis.Bounces / analysis.Recipients : 0;
                Console.WriteLine($"  Bounces:       {analysis.Bounces:N0} ({bounceRate:P2})");
            }

            if (analysis.Complaints > 0)
            {
                var complaintRate = analysis.Recipients > 0 ? (double)analysis.Complaints / analysis.Recipients : 0;
                Console.WriteLine($"  Complaints:    {analysis.Complaints:N0} ({complaintRate:P2})");
            }
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
            await writer.WriteLineAsync($"bounces,{analysis.Bounces}");
            await writer.WriteLineAsync($"complaints,{analysis.Complaints}");
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
}

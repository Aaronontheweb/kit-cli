using System.Globalization;
using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class AdvancedFilteringCommands
{
    public static async Task<int> HandleSubscribersByDateRange(string[] args, IKitApiClient client)
    {
        DateTime? startDate = null;
        DateTime? endDate = null;
        string? status = null;
        string format = "table";
        string? outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--from":
                case "-f":
                    if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var from))
                    {
                        startDate = from;
                    }

                    break;
                case "--to":
                case "-t":
                    if (i + 1 < args.Length && DateTime.TryParse(args[++i], out var to))
                    {
                        endDate = to;
                    }

                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        status = args[++i];
                    }

                    break;
                case "--format":
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

        if (startDate == null && endDate == null)
        {
            Console.WriteLine("Usage: kit subscribers date-range [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --from, -f <date>     Start date (YYYY-MM-DD)");
            Console.WriteLine("  --to, -t <date>       End date (YYYY-MM-DD)");
            Console.WriteLine("  --status, -s <state>  Filter by status");
            Console.WriteLine("  --format <format>     Output format (table, json, csv)");
            Console.WriteLine("  --output, -o <file>   Export to file");
            return 1;
        }

        using var progress = new ProgressIndicator($"Fetching subscribers from {startDate?.ToString("yyyy-MM-dd") ?? "beginning"} to {endDate?.ToString("yyyy-MM-dd") ?? "now"}");

        var subscribers = new List<Subscriber>();

        await foreach (var subscriber in client.GetAllSubscribersAsync(status))
        {
            var createdDate = subscriber.CreatedAt.Date;

            if (startDate.HasValue && createdDate < startDate.Value)
            {
                continue;
            }

            if (endDate.HasValue && createdDate > endDate.Value)
            {
                continue;
            }

            subscribers.Add(subscriber);
        }

        progress.Complete($"Found {subscribers.Count:N0} subscribers in date range");

        if (outputPath != null)
        {
            await ExportSubscribers(subscribers, outputPath);
            Console.WriteLine($"✓ Exported {subscribers.Count:N0} subscribers to {outputPath}");
        }
        else
        {
            OutputFormatter.PrintSubscribers(subscribers, format);
        }

        return 0;
    }

    public static async Task<int> HandleInactiveSubscribers(string[] args, IKitApiClient client)
    {
        int days = 30;
        string format = "table";
        string? outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--days":
                case "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var d))
                    {
                        days = d;
                    }

                    break;
                case "--format":
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

        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        using var progress = new ProgressIndicator($"Finding subscribers inactive for {days} days");

        // This would need API support for last_activity tracking
        // For now, we'll demonstrate the pattern
        var subscribers = new List<Subscriber>();

        await foreach (var subscriber in client.GetAllSubscribersAsync("active"))
        {
            // In a real implementation, we'd check last_activity_at or similar field
            // For now, we'll use created_at as a proxy
            if (subscriber.CreatedAt < DateTimeOffset.UtcNow.AddDays(-days))
            {
                subscribers.Add(subscriber);
            }
        }

        progress.Complete($"Found {subscribers.Count:N0} potentially inactive subscribers");

        if (outputPath != null)
        {
            await ExportSubscribers(subscribers, outputPath);
            Console.WriteLine($"✓ Exported {subscribers.Count:N0} subscribers to {outputPath}");
        }
        else
        {
            OutputFormatter.PrintSubscribers(subscribers.Take(100).ToList(), format);
            if (subscribers.Count > 100)
            {
                Console.WriteLine($"... and {subscribers.Count - 100:N0} more. Use --output to export all.");
            }
        }

        return 0;
    }

    public static async Task<int> HandleCampaignComparison(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit campaign compare <id1> <id2> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --metric <metric>  Metric to compare (opens, clicks, all)");
            return 1;
        }

        if (!long.TryParse(args[0], out var id1) || !long.TryParse(args[1], out var id2))
        {
            Console.WriteLine("Invalid broadcast IDs. Please provide numeric IDs.");
            return 1;
        }

        string metric = "all";

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "--metric" && i + 1 < args.Length)
            {
                metric = args[++i];
            }
        }

        using var progress = new ProgressIndicator($"Comparing campaigns {id1} and {id2}");

        var stats1Task = client.GetBroadcastStatsAsync(id1);
        var stats2Task = client.GetBroadcastStatsAsync(id2);
        var broadcast1Task = client.GetBroadcastAsync(id1);
        var broadcast2Task = client.GetBroadcastAsync(id2);

        await Task.WhenAll(stats1Task, stats2Task, broadcast1Task, broadcast2Task);

        var stats1 = await stats1Task;
        var stats2 = await stats2Task;
        var broadcast1 = await broadcast1Task;
        var broadcast2 = await broadcast2Task;

        if (stats1 == null || broadcast1 == null)
        {
            progress.Complete($"Campaign {id1} not found");
            return 1;
        }

        if (stats2 == null || broadcast2 == null)
        {
            progress.Complete($"Campaign {id2} not found");
            return 1;
        }

        progress.Complete("Retrieved campaign data");

        Console.WriteLine("\nCampaign Comparison");
        Console.WriteLine(new string('═', 80));

        Console.WriteLine($"\n{"Metric",-25} │ {TruncateString(broadcast1.Subject, 25),-25} │ {TruncateString(broadcast2.Subject, 25),-25}");
        Console.WriteLine(new string('─', 80));

        Console.WriteLine($"{"Recipients",-25} │ {stats1.Recipients,25:N0} │ {stats2.Recipients,25:N0}");
        Console.WriteLine($"{"Emails Opened",-25} │ {stats1.EmailsOpened,25:N0} │ {stats2.EmailsOpened,25:N0}");
        Console.WriteLine($"{"Open Rate",-25} │ {stats1.OpenRate,25:P1} │ {stats2.OpenRate,25:P1}");
        Console.WriteLine($"{"Total Clicks",-25} │ {stats1.TotalClicks,25:N0} │ {stats2.TotalClicks,25:N0}");
        Console.WriteLine($"{"Click Rate",-25} │ {stats1.ClickRate,25:P1} │ {stats2.ClickRate,25:P1}");
        Console.WriteLine($"{"Click-to-Open Rate",-25} │ {stats1.ClickToOpenRate,25:P1} │ {stats2.ClickToOpenRate,25:P1}");
        Console.WriteLine($"{"Unsubscribes",-25} │ {stats1.Unsubscribes,25:N0} │ {stats2.Unsubscribes,25:N0}");

        Console.WriteLine(new string('─', 80));

        // Highlight winner for each metric
        Console.WriteLine("\n📊 Performance Summary:");

        if (stats1.OpenRate > stats2.OpenRate)
        {
            Console.WriteLine($"✓ Campaign 1 had {((stats1.OpenRate - stats2.OpenRate) * 100):F1}% higher open rate");
        }
        else if (stats2.OpenRate > stats1.OpenRate)
        {
            Console.WriteLine($"✓ Campaign 2 had {((stats2.OpenRate - stats1.OpenRate) * 100):F1}% higher open rate");
        }

        if (stats1.ClickRate > stats2.ClickRate)
        {
            Console.WriteLine($"✓ Campaign 1 had {((stats1.ClickRate - stats2.ClickRate) * 100):F1}% higher click rate");
        }
        else if (stats2.ClickRate > stats1.ClickRate)
        {
            Console.WriteLine($"✓ Campaign 2 had {((stats2.ClickRate - stats1.ClickRate) * 100):F1}% higher click rate");
        }

        return 0;
    }

    public static async Task<int> HandleBulkUnsubscribed(string[] args, IKitApiClient client)
    {
        DateTime? since = null;
        string? outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--since":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        if (DateTime.TryParse(args[++i], out var date))
                        {
                            since = date;
                        }
                        else if (int.TryParse(args[i], out var days))
                        {
                            since = DateTime.UtcNow.AddDays(-days);
                        }
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

        using var progress = new ProgressIndicator("Finding unsubscribed users");

        var unsubscribed = new List<Subscriber>();

        await foreach (var subscriber in client.GetAllSubscribersAsync("cancelled"))
        {
            if (since == null || subscriber.CreatedAt >= new DateTimeOffset(since.Value))
            {
                unsubscribed.Add(subscriber);
            }
        }

        progress.Complete($"Found {unsubscribed.Count:N0} unsubscribed users");

        if (outputPath != null)
        {
            await ExportSubscribers(unsubscribed, outputPath);
            Console.WriteLine($"✓ Exported {unsubscribed.Count:N0} unsubscribed users to {outputPath}");
        }
        else
        {
            OutputFormatter.PrintSubscribers(unsubscribed.Take(50).ToList(), "table");
            if (unsubscribed.Count > 50)
            {
                Console.WriteLine($"\n... and {unsubscribed.Count - 50:N0} more. Use --output to export all.");
            }
        }

        return 0;
    }

    private static async Task ExportSubscribers(List<Subscriber> subscribers, string outputPath)
    {
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

        using var writer = new StreamWriter(outputPath);

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                subscribers.ToArray(),
                KitJsonIndentedContext.Default.SubscriberArray);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("id,email_address,first_name,state,tags,created_at");

            foreach (var sub in subscribers)
            {
                var tags = EscapeCsvField(sub.TagList);
                var name = EscapeCsvField(sub.FirstName ?? "");
                var email = EscapeCsvField(sub.EmailAddress);

                await writer.WriteLineAsync(
                    $"{sub.Id},{email},{name},{sub.State},{tags},{sub.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}");
            }
        }
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 3)] + "...";
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

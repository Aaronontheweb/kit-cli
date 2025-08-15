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
                        format = args[++i];
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                        limit = l;
                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                        status = args[++i];
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
                        format = args[++i];
                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputPath = args[++i];
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
                        format = args[++i];
                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputPath = args[++i];
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
                        format = args[++i];
                    break;
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputPath = args[++i];
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
                        outputPath = args[++i];
                    break;
                case "--all":
                    allBroadcasts = true;
                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                        status = args[++i];
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
            outputPath += ".csv";
        
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
            return "";
        
        field = field.Replace("\"", "\"\"");
        
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field}\"";
        }
        
        return field;
    }
}
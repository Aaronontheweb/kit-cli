using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class SubscriberCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        string? status = null;
        string format = "table";
        int limit = 50;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                        status = args[++i];
                    break;
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
            }
        }
        
        using var progress = new ProgressIndicator("Fetching subscribers");
        
        var response = await client.GetSubscribersAsync(limit);
        var subscribers = response.Data;
        
        // Filter by status if specified
        if (!string.IsNullOrEmpty(status))
        {
            subscribers = subscribers.Where(s => 
                s.State.Equals(status, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        
        progress.Complete($"Found {subscribers.Length:N0} subscribers");
        
        OutputFormatter.PrintSubscribers(subscribers, format);
        return 0;
    }
    
    public static async Task<int> HandleGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit subscriber get <id|email>");
            return 1;
        }
        
        var identifier = args[0];
        string format = "table";
        
        for (int i = 1; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i];
            }
        }
        
        Subscriber? subscriber;
        
        // Check if it's an ID or email
        if (long.TryParse(identifier, out var id))
        {
            subscriber = await client.GetSubscriberAsync(id);
        }
        else if (identifier.Contains('@'))
        {
            subscriber = await client.GetSubscriberByEmailAsync(identifier);
        }
        else
        {
            Console.WriteLine("Invalid identifier. Please provide a subscriber ID or email address.");
            return 1;
        }
        
        if (subscriber == null)
        {
            Console.WriteLine($"Subscriber not found: {identifier}");
            return 1;
        }
        
        OutputFormatter.PrintSubscribers([subscriber], format);
        return 0;
    }
    
    public static async Task<int> HandleSearch(string[] args, IKitApiClient client)
    {
        string? query = null;
        string? status = null;
        string format = "table";
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--query":
                case "-q":
                    if (i + 1 < args.Length)
                        query = args[++i];
                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                        status = args[++i];
                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                        format = args[++i];
                    break;
                default:
                    // If no flag, treat as query
                    if (!args[i].StartsWith("-"))
                        query = string.IsNullOrEmpty(query) ? args[i] : $"{query} {args[i]}";
                    break;
            }
        }
        
        if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(status))
        {
            Console.WriteLine("Usage: kit subscriber search [query] [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --query, -q <text>    Search query");
            Console.WriteLine("  --status, -s <state>  Filter by status");
            Console.WriteLine("  --format, -f <format> Output format");
            return 1;
        }
        
        using var progress = new ProgressIndicator("Searching subscribers");
        
        // For now, we'll fetch and filter client-side
        // In a real implementation, we'd use the API's search endpoint
        var response = await client.GetSubscribersAsync(100);
        var subscribers = response.Data;
        
        if (!string.IsNullOrEmpty(status))
        {
            subscribers = subscribers.Where(s => 
                s.State.Equals(status, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        
        if (!string.IsNullOrEmpty(query))
        {
            var lowerQuery = query.ToLowerInvariant();
            subscribers = subscribers.Where(s =>
                s.EmailAddress.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                (s.FirstName?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                s.TagList.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)
            ).ToArray();
        }
        
        progress.Complete($"Found {subscribers.Length:N0} matching subscribers");
        
        if (subscribers.Length == 0)
        {
            Console.WriteLine("No subscribers found matching your search criteria.");
            return 0;
        }
        
        OutputFormatter.PrintSubscribers(subscribers, format);
        return 0;
    }
    
    public static async Task<int> HandleExport(string[] args, IKitApiClient client)
    {
        string outputPath = "subscribers.csv";
        string? status = null;
        bool allSubscribers = false;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputPath = args[++i];
                    break;
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                        status = args[++i];
                    break;
                case "--all":
                    allSubscribers = true;
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
        
        using var progress = new ProgressIndicator($"Exporting subscribers to {outputPath}");
        
        List<Subscriber> subscribers;
        
        if (allSubscribers)
        {
            // Stream all subscribers
            subscribers = new List<Subscriber>();
            await foreach (var subscriber in client.GetAllSubscribersAsync(status))
            {
                subscribers.Add(subscriber);
            }
        }
        else
        {
            // Just get first page
            var response = await client.GetSubscribersAsync(100);
            subscribers = response.Data.ToList();
            
            if (!string.IsNullOrEmpty(status))
            {
                subscribers = subscribers.Where(s => 
                    s.State.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
        
        progress.Complete($"Exporting {subscribers.Count:N0} subscribers");
        
        // Write to file
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
            // CSV format
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
        
        Console.WriteLine($"✓ Exported {subscribers.Count:N0} subscribers to {outputPath}");
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
using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class TagCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        string format = "table";
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                        format = args[++i];
                    break;
            }
        }
        
        using var progress = new ProgressIndicator("Fetching tags");
        
        var tags = await client.GetTagsAsync();
        
        progress.Complete($"Found {tags.Length:N0} tags");
        
        OutputFormatter.PrintTags(tags, format);
        return 0;
    }
    
    public static async Task<int> HandleSubscribers(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit tag subscribers <tag-id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --limit, -l <number>   Maximum subscribers to fetch");
            Console.WriteLine("  --output, -o <file>    Export to file");
            return 1;
        }
        
        if (!long.TryParse(args[0], out var tagId))
        {
            Console.WriteLine("Invalid tag ID. Please provide a numeric ID.");
            return 1;
        }
        
        string format = "table";
        int limit = 100;
        string? outputPath = null;
        bool fetchAll = false;
        
        for (int i = 1; i < args.Length; i++)
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
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputPath = args[++i];
                    break;
                case "--all":
                    fetchAll = true;
                    break;
            }
        }
        
        using var progress = new ProgressIndicator($"Fetching subscribers for tag {tagId}");
        
        List<Subscriber> subscribers = new();
        string? cursor = null;
        bool hasMore = true;
        int fetched = 0;
        
        // Fetch subscribers with pagination
        while (hasMore && (fetchAll || fetched < limit))
        {
            var response = await client.GetTagSubscribersAsync(tagId, 100, cursor);
            
            foreach (var subscriber in response.Data)
            {
                if (!fetchAll && fetched >= limit)
                    break;
                    
                subscribers.Add(subscriber);
                fetched++;
            }
            
            if (response.Pagination != null && (fetchAll || fetched < limit))
            {
                cursor = response.Pagination.EndCursor;
                hasMore = response.Pagination.HasNextPage;
            }
            else
            {
                hasMore = false;
            }
        }
        
        progress.Complete($"Found {subscribers.Count:N0} subscribers with tag {tagId}");
        
        // Handle output
        if (!string.IsNullOrEmpty(outputPath))
        {
            // Determine format from file extension
            var fileFormat = Path.GetExtension(outputPath).ToLowerInvariant() switch
            {
                ".json" => "json",
                ".csv" => "csv",
                _ => "csv"
            };
            
            if (!outputPath.Contains('.'))
                outputPath += ".csv";
            
            using var writer = new StreamWriter(outputPath);
            
            if (fileFormat == "json")
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
        }
        else
        {
            OutputFormatter.PrintSubscribers(subscribers, format);
        }
        
        return 0;
    }
    
    public static async Task<int> HandleExport(string[] args, IKitApiClient client)
    {
        string outputPath = "tags.csv";
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                        outputPath = args[++i];
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
        
        using var progress = new ProgressIndicator($"Exporting tags to {outputPath}");
        
        var tags = await client.GetTagsAsync();
        
        progress.Complete($"Exporting {tags.Length:N0} tags");
        
        // Write to file
        using var writer = new StreamWriter(outputPath);
        
        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                tags, 
                KitJsonIndentedContext.Default.TagArray);
            await writer.WriteAsync(json);
        }
        else
        {
            // CSV format
            await writer.WriteLineAsync("id,name,created_at");
            
            foreach (var tag in tags.OrderBy(t => t.Name))
            {
                var name = EscapeCsvField(tag.Name);
                var createdAt = tag.CreatedAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") ?? "";
                
                await writer.WriteLineAsync($"{tag.Id},{name},{createdAt}");
            }
        }
        
        Console.WriteLine($"✓ Exported {tags.Length:N0} tags to {outputPath}");
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
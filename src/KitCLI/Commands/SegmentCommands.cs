using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class SegmentCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        string format = "table";
        int limit = 50;

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
            }
        }

        using var progress = new ProgressIndicator("Fetching segments");

        var response = await client.GetSegmentsAsync(limit);
        var segments = response.Data;

        progress.Complete($"Found {segments.Length:N0} segments");

        PrintSegments(segments, format);
        return 0;
    }

    public static async Task<int> HandleGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit segment get <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid segment ID. Please provide a numeric ID.");
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

        using var progress = new ProgressIndicator($"Fetching segment {id}");

        var segment = await client.GetSegmentAsync(id);

        if (segment == null)
        {
            progress.Complete($"Segment not found: {id}");
            return 1;
        }

        progress.Complete($"Found segment: {segment.Name}");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                segment,
                KitJsonIndentedContext.Default.Segment);
            Console.WriteLine(json);
        }
        else
        {
            PrintSegments([segment], format);
        }

        return 0;
    }

    public static async Task<int> HandleSubscribers(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit segment subscribers <segment-id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --limit, -l <number>   Maximum subscribers to fetch");
            Console.WriteLine("  --output, -o <file>    Export to file");
            Console.WriteLine("  --all                  Fetch all subscribers (may take time)");
            return 1;
        }

        if (!long.TryParse(args[0], out var segmentId))
        {
            Console.WriteLine("Invalid segment ID. Please provide a numeric ID.");
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
                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[++i];
                    }

                    break;
                case "--all":
                    fetchAll = true;
                    break;
            }
        }

        using var progress = new ProgressIndicator($"Fetching subscribers for segment {segmentId}");

        List<Subscriber> subscribers = new();

        if (fetchAll)
        {
            await foreach (var subscriber in client.GetAllSegmentSubscribersAsync(segmentId))
            {
                subscribers.Add(subscriber);
            }
        }
        else
        {
            string? cursor = null;
            int fetched = 0;
            bool hasMore = true;

            while (hasMore && fetched < limit)
            {
                var response = await client.GetSegmentSubscribersAsync(segmentId, Math.Min(100, limit - fetched), cursor);

                foreach (var subscriber in response.Data)
                {
                    subscribers.Add(subscriber);
                    fetched++;
                    if (fetched >= limit)
                    {
                        break;
                    }
                }

                if (response.Pagination != null && fetched < limit)
                {
                    cursor = response.Pagination.EndCursor;
                    hasMore = response.Pagination.HasNextPage;
                }
                else
                {
                    hasMore = false;
                }
            }
        }

        progress.Complete($"Found {subscribers.Count:N0} subscribers in segment");

        if (!string.IsNullOrEmpty(outputPath))
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

    public static async Task<int> HandleAnalyze(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit segment analyze <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid segment ID. Please provide a numeric ID.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Analyzing segment {id}");

        var segment = await client.GetSegmentAsync(id);

        if (segment == null)
        {
            progress.Complete($"Segment not found: {id}");
            return 1;
        }

        progress.Complete($"Analyzing segment: {segment.Name}");

        Console.WriteLine("\nSegment Analysis");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Name: {segment.Name}");
        Console.WriteLine($"Description: {segment.Description ?? "(none)"}");
        Console.WriteLine($"Subscribers: {segment.SubscriberCount:N0}");
        Console.WriteLine($"Created: {segment.CreatedAt:yyyy-MM-dd}");
        Console.WriteLine($"Last Updated: {segment.UpdatedAt?.ToString("yyyy-MM-dd") ?? "Never"}");
        Console.WriteLine($"Processing: {(segment.IsProcessing ? "Yes" : "No")}");

        if (segment.Filters != null && segment.Filters.Length > 0)
        {
            Console.WriteLine("\nSegment Filters:");
            Console.WriteLine(new string('─', 60));
            foreach (var filter in segment.Filters)
            {
                Console.WriteLine($"  • {filter.Field} {filter.Operator} {filter.Value}");
            }
        }

        // Sample some subscribers to show characteristics
        if (segment.SubscriberCount > 0)
        {
            Console.WriteLine("\nSample Subscribers (first 5):");
            Console.WriteLine(new string('─', 60));

            var response = await client.GetSegmentSubscribersAsync(id, 5);
            foreach (var subscriber in response.Data)
            {
                Console.WriteLine($"  • {subscriber.EmailAddress} ({subscriber.State})");
            }
        }

        return 0;
    }

    public static async Task<int> HandleCompare(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit segment compare <id1> <id2>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id1) || !long.TryParse(args[1], out var id2))
        {
            Console.WriteLine("Invalid segment IDs. Please provide numeric IDs.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Comparing segments {id1} and {id2}");

        var segment1Task = client.GetSegmentAsync(id1);
        var segment2Task = client.GetSegmentAsync(id2);

        await Task.WhenAll(segment1Task, segment2Task);

        var segment1 = await segment1Task;
        var segment2 = await segment2Task;

        if (segment1 == null)
        {
            progress.Complete($"Segment {id1} not found");
            return 1;
        }

        if (segment2 == null)
        {
            progress.Complete($"Segment {id2} not found");
            return 1;
        }

        progress.Complete("Retrieved segment data");

        Console.WriteLine("\nSegment Comparison");
        Console.WriteLine(new string('═', 80));

        Console.WriteLine($"\n{"Metric",-25} │ {TruncateString(segment1.Name, 25),-25} │ {TruncateString(segment2.Name, 25),-25}");
        Console.WriteLine(new string('─', 80));

        Console.WriteLine($"{"Subscribers",-25} │ {segment1.SubscriberCount,25:N0} │ {segment2.SubscriberCount,25:N0}");
        Console.WriteLine($"{"Created",-25} │ {segment1.CreatedAt,25:yyyy-MM-dd} │ {segment2.CreatedAt,25:yyyy-MM-dd}");
        Console.WriteLine($"{"Last Updated",-25} │ {segment1.UpdatedAt?.ToString("yyyy-MM-dd") ?? "Never",25} │ {segment2.UpdatedAt?.ToString("yyyy-MM-dd") ?? "Never",25}");
        Console.WriteLine($"{"Filter Count",-25} │ {segment1.Filters?.Length ?? 0,25} │ {segment2.Filters?.Length ?? 0,25}");
        Console.WriteLine($"{"Is Processing",-25} │ {(segment1.IsProcessing ? "Yes" : "No"),25} │ {(segment2.IsProcessing ? "Yes" : "No"),25}");

        Console.WriteLine(new string('─', 80));

        // Calculate differences
        var diff = segment1.SubscriberCount - segment2.SubscriberCount;
        var percentDiff = segment2.SubscriberCount > 0
            ? (double)diff / segment2.SubscriberCount * 100
            : 100.0;

        Console.WriteLine("\n📊 Analysis:");

        if (Math.Abs(diff) > 0)
        {
            if (diff > 0)
            {
                Console.WriteLine($"✓ Segment 1 has {Math.Abs(diff):N0} more subscribers ({percentDiff:+0.0;-0.0}%)");
            }
            else
            {
                Console.WriteLine($"✓ Segment 2 has {Math.Abs(diff):N0} more subscribers ({-percentDiff:+0.0;-0.0}%)");
            }
        }
        else
        {
            Console.WriteLine("✓ Both segments have the same number of subscribers");
        }

        // Find overlapping subscribers (would need additional API support)
        Console.WriteLine("\nNote: Subscriber overlap analysis requires additional API implementation.");

        return 0;
    }

    public static async Task<int> HandleExport(string[] args, IKitApiClient client)
    {
        string outputPath = "segments.csv";

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
            }
        }

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

        using var progress = new ProgressIndicator($"Exporting segments to {outputPath}");

        List<Segment> segments = new();
        string? cursor = null;
        bool hasMore = true;

        while (hasMore)
        {
            var response = await client.GetSegmentsAsync(100, cursor);
            segments.AddRange(response.Data);

            if (response.Pagination != null)
            {
                cursor = response.Pagination.EndCursor;
                hasMore = response.Pagination.HasNextPage;
            }
            else
            {
                hasMore = false;
            }
        }

        progress.Complete($"Exporting {segments.Count:N0} segments");

        using var writer = new StreamWriter(outputPath);

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                segments.ToArray(),
                KitJsonIndentedContext.Default.SegmentArray);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("id,name,description,subscriber_count,created_at,updated_at");

            foreach (var segment in segments.OrderBy(s => s.Name))
            {
                var name = EscapeCsvField(segment.Name);
                var description = EscapeCsvField(segment.Description ?? "");

                await writer.WriteLineAsync(
                    $"{segment.Id},{name},{description},{segment.SubscriberCount}," +
                    $"{segment.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}," +
                    $"{segment.UpdatedAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") ?? ""}");
            }
        }

        Console.WriteLine($"✓ Exported {segments.Count:N0} segments to {outputPath}");
        return 0;
    }

    private static void PrintSegments(IEnumerable<Segment> segments, string format)
    {
        var segmentList = segments.ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                segmentList.ToArray(),
                KitJsonIndentedContext.Default.SegmentArray);
            Console.WriteLine(json);
            return;
        }

        if (!segmentList.Any())
        {
            Console.WriteLine("No segments found.");
            return;
        }

        // Table format
        const int idWidth = 10;
        const int nameWidth = 30;
        const int descWidth = 35;
        const int countWidth = 12;
        const int createdWidth = 12;

        Console.WriteLine(new string('─', idWidth + nameWidth + descWidth + countWidth + createdWidth + 10));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Name",-nameWidth} │ {"Description",-descWidth} │ {"Subscribers",-countWidth} │ {"Created",-createdWidth} │");
        Console.WriteLine(new string('─', idWidth + nameWidth + descWidth + countWidth + createdWidth + 10));

        foreach (var segment in segmentList.OrderBy(s => s.Name))
        {
            var name = TruncateString(segment.Name, nameWidth);
            var desc = TruncateString(segment.Description ?? "", descWidth);
            var created = segment.CreatedAt.ToString("yyyy-MM-dd");

            Console.WriteLine($"│ {segment.Id,-idWidth} │ {name,-nameWidth} │ {desc,-descWidth} │ {segment.SubscriberCount,countWidth:N0} │ {created,-createdWidth} │");
        }

        Console.WriteLine(new string('─', idWidth + nameWidth + descWidth + countWidth + createdWidth + 10));
        Console.WriteLine($"Total: {segmentList.Count:N0} segment(s)");
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

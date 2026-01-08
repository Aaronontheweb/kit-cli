using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class SubscriberCommands
{
    public static async Task<int> HandleList(string[] args, IKitApiClient client)
    {
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("subscriber", "list");
        }

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
                    {
                        status = args[++i];
                    }

                    break;
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
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("subscriber", "get");
        }

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

        // Fetch tags separately (Kit V4 API returns tags via separate endpoint)
        var tags = await client.GetSubscriberTagsAsync(subscriber.Id);
        subscriber.Tags = tags;

        OutputFormatter.PrintSubscribers([subscriber], format);
        return 0;
    }

    public static async Task<int> HandleSearch(string[] args, IKitApiClient client)
    {
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("subscriber", "search");
        }

        string? query = null;
        string? email = null;
        string? status = null;
        string format = "table";

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--query":
                case "-q":
                    if (i + 1 < args.Length)
                    {
                        query = args[++i];
                    }

                    break;
                case "--email":
                case "-e":
                    if (i + 1 < args.Length)
                    {
                        email = args[++i];
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
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i];
                    }

                    break;
                default:
                    // If no flag, treat as query
                    if (!args[i].StartsWith("-"))
                    {
                        query = string.IsNullOrEmpty(query) ? args[i] : $"{query} {args[i]}";
                    }

                    break;
            }
        }

        if (string.IsNullOrEmpty(query) && string.IsNullOrEmpty(status) && string.IsNullOrEmpty(email))
        {
            Console.WriteLine("Usage: kit subscriber search [query] [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --email, -e <email>   Search by exact email address");
            Console.WriteLine("  --query, -q <text>    Search query (searches name, email, tags)");
            Console.WriteLine("  --status, -s <state>  Filter by status");
            Console.WriteLine("  --format, -f <format> Output format");
            return 1;
        }

        using var progress = new ProgressIndicator("Searching subscribers");

        // If searching by email, use the direct email lookup
        if (!string.IsNullOrEmpty(email))
        {
            var subscriber = await client.GetSubscriberByEmailAsync(email);
            progress.Complete($"Found {(subscriber != null ? 1 : 0)} matching subscribers");

            if (subscriber == null)
            {
                Console.WriteLine("No subscribers found matching your search criteria.");
                return 0;
            }

            // Fetch tags for the subscriber
            var tags = await client.GetSubscriberTagsAsync(subscriber.Id);
            subscriber.Tags = tags;

            OutputFormatter.PrintSubscribers([subscriber], format);
            return 0;
        }

        // For query-based search, fetch all subscribers and filter client-side
        var allSubscribers = new List<Subscriber>();
        await foreach (var subscriber in client.GetAllSubscribersAsync(status))
        {
            allSubscribers.Add(subscriber);
        }

        var subscribers = allSubscribers.ToArray();

        // Filter by query if provided (status is already filtered by the API)
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
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("subscriber", "export");
        }

        string outputPath = "subscribers.csv";
        string? status = null;
        int? limit = null; // null means all subscribers (default)

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
                case "--status":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        status = args[++i];
                    }

                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var limitVal))
                    {
                        limit = limitVal;
                    }

                    break;
                case "--all": // Keep for backwards compatibility, but it's now the default
                    limit = null;
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

        using var progress = new ProgressIndicator($"Exporting subscribers to {outputPath}");

        List<Subscriber> subscribers = new List<Subscriber>();
        int count = 0;

        // Stream all subscribers (default) or up to limit
        await foreach (var subscriber in client.GetAllSubscribersAsync(status))
        {
            subscribers.Add(subscriber);
            count++;

            if (limit.HasValue && count >= limit.Value)
            {
                break;
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

    public static async Task<int> HandleScores(string[] args, IKitApiClient client)
    {
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("subscriber", "scores");
        }

        string algorithm = "weighted";
        long? segmentId = null;
        int limit = 100;
        string format = "table";
        string? exportPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--algorithm":
                case "-a":
                    if (i + 1 < args.Length)
                    {
                        algorithm = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--segment-id":
                case "--segment":
                    if (i + 1 < args.Length && long.TryParse(args[++i], out var segId))
                    {
                        segmentId = segId;
                    }
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                    {
                        limit = l;
                    }
                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--export":
                case "-e":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }
                    break;
            }
        }

        // Validate algorithm
        if (algorithm != "weighted" && algorithm != "tags" && algorithm != "maturity")
        {
            Console.WriteLine($"Unknown algorithm: {algorithm}");
            Console.WriteLine("Available algorithms: weighted, tags, maturity");
            return 1;
        }

        using var progress = new ProgressIndicator("Analyzing subscriber engagement");

        // Get subscribers (from segment or all)
        var allSubscribers = new List<Subscriber>();
        string? segmentName = null;

        if (segmentId.HasValue)
        {
            var segment = await client.GetSegmentAsync(segmentId.Value);
            if (segment == null)
            {
                Console.WriteLine($"Segment not found: {segmentId}");
                return 1;
            }
            segmentName = segment.Name;

            await foreach (var subscriber in client.GetAllSegmentSubscribersAsync(segmentId.Value))
            {
                allSubscribers.Add(subscriber);
            }
        }
        else
        {
            await foreach (var subscriber in client.GetAllSubscribersAsync("active"))
            {
                allSubscribers.Add(subscriber);
            }
        }

        progress.Dispose();
        Console.WriteLine();

        if (allSubscribers.Count == 0)
        {
            Console.WriteLine("No subscribers found.");
            return 0;
        }

        // Fetch tags for each subscriber (needed for scoring)
        Console.Write("Fetching subscriber tags...");
        var subscriberTagCounts = new Dictionary<long, (int count, string tagNames)>();
        var batchSize = 10;
        var batches = allSubscribers.Chunk(batchSize);
        var processed = 0;

        foreach (var batch in batches)
        {
            var tagTasks = batch.Select(async s =>
            {
                try
                {
                    var tags = await client.GetSubscriberTagsAsync(s.Id);
                    return (s.Id, Count: tags.Length, Names: string.Join(", ", tags.Select(t => t.Name)));
                }
                catch
                {
                    return (s.Id, Count: s.Tags?.Length ?? 0, Names: s.TagList);
                }
            });

            var results = await Task.WhenAll(tagTasks);
            foreach (var (id, count, names) in results)
            {
                subscriberTagCounts[id] = (count, names);
            }

            processed += batch.Length;
            Console.Write($"\rFetching subscriber tags... {processed:N0}/{allSubscribers.Count:N0}");
        }

        Console.WriteLine($"\rFetching subscriber tags... Done ({processed:N0} subscribers)");

        // Score each subscriber
        var now = DateTime.UtcNow;
        var allScores = new List<double>();
        var scoredSubscribers = allSubscribers.Select(s =>
        {
            var (tagCount, tagNames) = subscriberTagCounts.GetValueOrDefault(s.Id, (s.Tags?.Length ?? 0, s.TagList));
            var accountAgeDays = (int)(now - s.CreatedAt).TotalDays;
            var breakdown = CalculateScore(s, tagCount, accountAgeDays, algorithm);
            allScores.Add(breakdown.Total);

            return new ScoredSubscriber
            {
                Id = s.Id,
                Email = s.EmailAddress,
                FirstName = s.FirstName,
                State = s.State,
                CreatedAt = s.CreatedAt,
                TagCount = tagCount,
                Tags = tagNames,
                AccountAgeDays = accountAgeDays,
                Score = breakdown.Total,
                Breakdown = breakdown
            };
        })
        .OrderByDescending(s => s.Score)
        .Take(limit)
        .ToList();

        // Assign ranks
        for (int i = 0; i < scoredSubscribers.Count; i++)
        {
            scoredSubscribers[i].Rank = i + 1;
        }

        // Calculate median
        allScores.Sort();
        var median = allScores.Count > 0
            ? allScores.Count % 2 == 0
                ? (allScores[allScores.Count / 2 - 1] + allScores[allScores.Count / 2]) / 2
                : allScores[allScores.Count / 2]
            : 0;

        var result = new SubscriberScoresResult
        {
            Algorithm = algorithm,
            AlgorithmDescription = GetAlgorithmDescription(algorithm),
            TotalAnalyzed = allSubscribers.Count,
            Returned = scoredSubscribers.Count,
            SegmentId = segmentId,
            SegmentName = segmentName,
            AverageScore = allScores.Count > 0 ? allScores.Average() : 0,
            MedianScore = median,
            Subscribers = scoredSubscribers.ToArray(),
            Note = "Scores are based on available data (tags, account age, state). " +
                   "Kit v4 API does not provide per-subscriber engagement metrics (opens, clicks)."
        };

        // Output
        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result,
                KitJsonIndentedContext.Default.SubscriberScoresResult);
            Console.WriteLine(json);
        }
        else if (format == "csv")
        {
            PrintScoresCsv(result);
        }
        else
        {
            PrintScoresTable(result);
        }

        // Export if requested
        if (!string.IsNullOrEmpty(exportPath))
        {
            await ExportScores(result, exportPath);
        }

        return 0;
    }

    private static ScoreBreakdown CalculateScore(Subscriber subscriber, int tagCount, int accountAgeDays, string algorithm)
    {
        var breakdown = new ScoreBreakdown();

        switch (algorithm)
        {
            case "tags":
                // Pure tag-based scoring
                // Max 100 points for 10+ tags
                breakdown.TagPoints = Math.Min(tagCount * 10, 100);
                breakdown.MaturityPoints = 0;
                breakdown.StatePoints = 0;
                break;

            case "maturity":
                // Account maturity focused scoring
                breakdown.TagPoints = 0;
                // Score based on account age (max 80 points at 365+ days)
                breakdown.MaturityPoints = Math.Min(accountAgeDays / 365.0 * 80, 80);
                // Active state bonus (20 points)
                breakdown.StatePoints = subscriber.State.Equals("active", StringComparison.OrdinalIgnoreCase) ? 20 : 0;
                break;

            case "weighted":
            default:
                // Balanced scoring using all signals
                // Tag engagement: More tags = more engaged (max 50 points for 5+ tags)
                breakdown.TagPoints = Math.Min(tagCount * 10, 50);

                // Account maturity: Established subscribers are valuable (max 30 points)
                // Peak at 180 days, slight decrease after for recency
                if (accountAgeDays <= 180)
                {
                    breakdown.MaturityPoints = accountAgeDays / 6.0; // 0-30 points
                }
                else
                {
                    breakdown.MaturityPoints = 30 - Math.Min((accountAgeDays - 180) / 365.0 * 10, 10); // Decrease over time
                }

                // State points: Active = 20, others = 0
                breakdown.StatePoints = subscriber.State.Equals("active", StringComparison.OrdinalIgnoreCase) ? 20 : 0;
                break;
        }

        breakdown.Total = Math.Round(breakdown.TagPoints + breakdown.MaturityPoints + breakdown.StatePoints, 1);
        return breakdown;
    }

    private static string GetAlgorithmDescription(string algorithm)
    {
        return algorithm switch
        {
            "tags" => "Tag-based: Score = TagCount * 10 (max 100)",
            "maturity" => "Maturity-based: Score = AccountAge/365 * 80 (max 80) + ActiveState (20)",
            "weighted" => "Weighted: TagPoints (max 50) + MaturityPoints (max 30) + StatePoints (max 20)",
            _ => "Unknown algorithm"
        };
    }

    private static void PrintScoresTable(SubscriberScoresResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"Subscriber Engagement Scores ({result.Algorithm} algorithm)");
        Console.WriteLine(new string('=', 80));

        if (result.SegmentName != null)
        {
            Console.WriteLine($"Segment: {result.SegmentName} (ID: {result.SegmentId})");
        }

        Console.WriteLine($"Algorithm: {result.AlgorithmDescription}");
        Console.WriteLine($"Total analyzed: {result.TotalAnalyzed:N0}");
        Console.WriteLine($"Average score: {result.AverageScore:F1}");
        Console.WriteLine($"Median score: {result.MedianScore:F1}");
        Console.WriteLine();
        Console.WriteLine("Note: " + result.Note);
        Console.WriteLine();

        // Header
        Console.WriteLine($"{"Rank",-5} {"Email",-35} {"Score",-8} {"Tags",-5} {"Age",-6} {"State",-10}");
        Console.WriteLine(new string('-', 80));

        foreach (var subscriber in result.Subscribers)
        {
            var email = subscriber.Email.Length > 33
                ? subscriber.Email[..30] + "..."
                : subscriber.Email;

            var ageStr = subscriber.AccountAgeDays > 365
                ? $"{subscriber.AccountAgeDays / 365}y"
                : $"{subscriber.AccountAgeDays}d";

            Console.WriteLine($"{subscriber.Rank,-5} {email,-35} {subscriber.Score,-8:F1} {subscriber.TagCount,-5} {ageStr,-6} {subscriber.State,-10}");
        }

        Console.WriteLine();
        Console.WriteLine($"Showing top {result.Returned} of {result.TotalAnalyzed:N0} subscribers");
    }

    private static void PrintScoresCsv(SubscriberScoresResult result)
    {
        Console.WriteLine("rank,id,email,first_name,score,tag_count,tags,account_age_days,state,tag_points,maturity_points,state_points");

        foreach (var subscriber in result.Subscribers)
        {
            var email = EscapeCsvField(subscriber.Email);
            var name = EscapeCsvField(subscriber.FirstName ?? "");
            var tags = EscapeCsvField(subscriber.Tags);

            Console.WriteLine($"{subscriber.Rank},{subscriber.Id},{email},{name},{subscriber.Score:F1},{subscriber.TagCount},{tags},{subscriber.AccountAgeDays},{subscriber.State},{subscriber.Breakdown?.TagPoints:F1},{subscriber.Breakdown?.MaturityPoints:F1},{subscriber.Breakdown?.StatePoints:F1}");
        }
    }

    private static async Task ExportScores(SubscriberScoresResult result, string path)
    {
        var format = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!path.Contains('.'))
        {
            path += ".csv";
        }

        using var writer = new StreamWriter(path);

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result,
                KitJsonIndentedContext.Default.SubscriberScoresResult);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("rank,id,email,first_name,score,tag_count,tags,account_age_days,state,tag_points,maturity_points,state_points");

            foreach (var subscriber in result.Subscribers)
            {
                var email = EscapeCsvField(subscriber.Email);
                var name = EscapeCsvField(subscriber.FirstName ?? "");
                var tags = EscapeCsvField(subscriber.Tags);

                await writer.WriteLineAsync($"{subscriber.Rank},{subscriber.Id},{email},{name},{subscriber.Score:F1},{subscriber.TagCount},{tags},{subscriber.AccountAgeDays},{subscriber.State},{subscriber.Breakdown?.TagPoints:F1},{subscriber.Breakdown?.MaturityPoints:F1},{subscriber.Breakdown?.StatePoints:F1}");
            }
        }

        Console.WriteLine($"Exported scores to {path}");
    }

    public static async Task<int> HandleCold(string[] args, IKitApiClient client)
    {
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("subscribers", "cold");
        }

        int minDaysOld = 90;
        int maxTags = 2;
        bool wasActiveFilter = false;
        int limit = 100;
        string format = "table";
        string? exportPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--min-days-old":
                case "--no-opens-days":
                case "-d":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var days))
                    {
                        minDaysOld = days;
                    }
                    break;
                case "--max-tags":
                case "-t":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var tags))
                    {
                        maxTags = tags;
                    }
                    break;
                case "--was-active":
                    wasActiveFilter = true;
                    break;
                case "--limit":
                case "-l":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var l))
                    {
                        limit = l;
                    }
                    break;
                case "--format":
                case "-f":
                    if (i + 1 < args.Length)
                    {
                        format = args[++i].ToLowerInvariant();
                    }
                    break;
                case "--export":
                case "-e":
                    if (i + 1 < args.Length)
                    {
                        exportPath = args[++i];
                    }
                    break;
            }
        }

        using var progress = new ProgressIndicator("Analyzing subscribers for cold contacts");

        // Get all active subscribers
        var allSubscribers = new List<Subscriber>();
        await foreach (var subscriber in client.GetAllSubscribersAsync("active"))
        {
            allSubscribers.Add(subscriber);
        }

        progress.Dispose();
        Console.WriteLine();

        if (allSubscribers.Count == 0)
        {
            Console.WriteLine("No subscribers found.");
            return 0;
        }

        // Fetch tags for each subscriber
        Console.Write("Fetching subscriber tags...");
        var subscriberTagCounts = new Dictionary<long, (int count, string tagNames)>();
        var batchSize = 10;
        var batches = allSubscribers.Chunk(batchSize);
        var processed = 0;

        foreach (var batch in batches)
        {
            var tagTasks = batch.Select(async s =>
            {
                try
                {
                    var tags = await client.GetSubscriberTagsAsync(s.Id);
                    return (s.Id, Count: tags.Length, Names: string.Join(", ", tags.Select(t => t.Name)));
                }
                catch
                {
                    return (s.Id, Count: s.Tags?.Length ?? 0, Names: s.TagList);
                }
            });

            var results = await Task.WhenAll(tagTasks);
            foreach (var (id, count, names) in results)
            {
                subscriberTagCounts[id] = (count, names);
            }

            processed += batch.Length;
            Console.Write($"\rFetching subscriber tags... {processed:N0}/{allSubscribers.Count:N0}");
        }

        Console.WriteLine($"\rFetching subscriber tags... Done ({processed:N0} subscribers)");

        // Filter for cold subscribers
        var now = DateTime.UtcNow;
        var coldSubscribers = new List<ColdSubscriber>();
        var tierBreakdown = new ColdTierBreakdown();

        foreach (var subscriber in allSubscribers)
        {
            var accountAgeDays = (int)(now - subscriber.CreatedAt).TotalDays;
            var (tagCount, tagNames) = subscriberTagCounts.GetValueOrDefault(subscriber.Id, (0, ""));

            // Skip if account is too new
            if (accountAgeDays < minDaysOld)
            {
                continue;
            }

            // Determine engagement tier based on tag count
            string engagementTier;
            if (tagCount == 0)
            {
                engagementTier = "none";
            }
            else if (tagCount <= 2)
            {
                engagementTier = "low";
            }
            else if (tagCount <= 5)
            {
                engagementTier = "medium";
            }
            else
            {
                engagementTier = "high";
            }

            // Check if subscriber is "cold" (low tag count for their age)
            bool isCold = tagCount <= maxTags;

            // If was-active filter, only include those with at least 1 tag
            if (wasActiveFilter && tagCount == 0)
            {
                continue;
            }

            if (isCold)
            {
                var coldReason = tagCount == 0
                    ? $"No tags after {accountAgeDays} days"
                    : $"Only {tagCount} tag(s) after {accountAgeDays} days";

                coldSubscribers.Add(new ColdSubscriber
                {
                    Id = subscriber.Id,
                    Email = subscriber.EmailAddress,
                    FirstName = subscriber.FirstName,
                    State = subscriber.State,
                    CreatedAt = subscriber.CreatedAt,
                    AccountAgeDays = accountAgeDays,
                    TagCount = tagCount,
                    Tags = tagNames,
                    EngagementTier = engagementTier,
                    ColdReason = coldReason
                });

                // Update tier breakdown
                switch (engagementTier)
                {
                    case "none":
                        tierBreakdown.NeverEngaged++;
                        break;
                    case "low":
                        tierBreakdown.PreviouslyLow++;
                        break;
                    case "medium":
                        tierBreakdown.PreviouslyMedium++;
                        break;
                    case "high":
                        tierBreakdown.PreviouslyHigh++;
                        break;
                }
            }
        }

        // Sort by account age descending (oldest first)
        coldSubscribers = coldSubscribers
            .OrderByDescending(s => s.AccountAgeDays)
            .Take(limit)
            .ToList();

        var coldPercentage = allSubscribers.Count > 0
            ? (double)coldSubscribers.Count / allSubscribers.Count * 100
            : 0;

        var result = new ColdSubscribersResult
        {
            MinDaysOld = minDaysOld,
            MaxTags = maxTags,
            WasActiveFilter = wasActiveFilter,
            TotalAnalyzed = allSubscribers.Count,
            ColdCount = coldSubscribers.Count,
            ColdPercentage = Math.Round(coldPercentage, 1),
            Subscribers = coldSubscribers.ToArray(),
            TierBreakdown = tierBreakdown,
            Note = "Cold subscribers are identified by low tag counts relative to account age. " +
                   "Kit v4 API does not provide per-subscriber engagement metrics (opens, clicks)."
        };

        // Output
        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result,
                KitJsonIndentedContext.Default.ColdSubscribersResult);
            Console.WriteLine(json);
        }
        else if (format == "csv")
        {
            PrintColdCsv(result);
        }
        else
        {
            PrintColdTable(result);
        }

        // Export if requested
        if (!string.IsNullOrEmpty(exportPath))
        {
            await ExportCold(result, exportPath);
        }

        return 0;
    }

    private static void PrintColdTable(ColdSubscribersResult result)
    {
        Console.WriteLine();
        Console.WriteLine($"Cold Subscribers Analysis");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"Criteria: Account age >= {result.MinDaysOld} days, Tag count <= {result.MaxTags}");
        if (result.WasActiveFilter)
        {
            Console.WriteLine("Filter: Only previously engaged (had at least 1 tag)");
        }
        Console.WriteLine($"Total analyzed: {result.TotalAnalyzed:N0}");
        Console.WriteLine($"Cold subscribers: {result.ColdCount:N0} ({result.ColdPercentage:F1}%)");
        Console.WriteLine();

        if (result.TierBreakdown != null)
        {
            Console.WriteLine("Engagement Tier Breakdown:");
            Console.WriteLine($"  Never engaged (0 tags):     {result.TierBreakdown.NeverEngaged:N0}");
            Console.WriteLine($"  Previously low (1-2 tags):  {result.TierBreakdown.PreviouslyLow:N0}");
            Console.WriteLine($"  Previously medium (3-5):    {result.TierBreakdown.PreviouslyMedium:N0}");
            Console.WriteLine($"  Previously high (6+):       {result.TierBreakdown.PreviouslyHigh:N0}");
            Console.WriteLine();
        }

        Console.WriteLine("Note: " + result.Note);
        Console.WriteLine();

        if (result.Subscribers.Length == 0)
        {
            Console.WriteLine("No cold subscribers found matching criteria.");
            return;
        }

        // Header
        Console.WriteLine($"{"Email",-35} {"Age",-6} {"Tags",-5} {"Tier",-8} {"Reason",-20}");
        Console.WriteLine(new string('-', 80));

        foreach (var subscriber in result.Subscribers)
        {
            var email = subscriber.Email.Length > 33
                ? subscriber.Email[..30] + "..."
                : subscriber.Email;

            var ageStr = subscriber.AccountAgeDays > 365
                ? $"{subscriber.AccountAgeDays / 365}y"
                : $"{subscriber.AccountAgeDays}d";

            var reason = subscriber.ColdReason.Length > 18
                ? subscriber.ColdReason[..15] + "..."
                : subscriber.ColdReason;

            Console.WriteLine($"{email,-35} {ageStr,-6} {subscriber.TagCount,-5} {subscriber.EngagementTier,-8} {reason,-20}");
        }

        Console.WriteLine();
        Console.WriteLine($"Showing {result.Subscribers.Length} of {result.ColdCount:N0} cold subscribers");
    }

    private static void PrintColdCsv(ColdSubscribersResult result)
    {
        Console.WriteLine("id,email,first_name,state,created_at,account_age_days,tag_count,tags,engagement_tier,cold_reason");

        foreach (var subscriber in result.Subscribers)
        {
            var email = EscapeCsvField(subscriber.Email);
            var name = EscapeCsvField(subscriber.FirstName ?? "");
            var tags = EscapeCsvField(subscriber.Tags);
            var reason = EscapeCsvField(subscriber.ColdReason);

            Console.WriteLine($"{subscriber.Id},{email},{name},{subscriber.State},{subscriber.CreatedAt:yyyy-MM-dd},{subscriber.AccountAgeDays},{subscriber.TagCount},{tags},{subscriber.EngagementTier},{reason}");
        }
    }

    private static async Task ExportCold(ColdSubscribersResult result, string path)
    {
        var format = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".json" => "json",
            ".csv" => "csv",
            _ => "csv"
        };

        if (!path.Contains('.'))
        {
            path += ".csv";
        }

        using var writer = new StreamWriter(path);

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(result,
                KitJsonIndentedContext.Default.ColdSubscribersResult);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("id,email,first_name,state,created_at,account_age_days,tag_count,tags,engagement_tier,cold_reason");

            foreach (var subscriber in result.Subscribers)
            {
                var email = EscapeCsvField(subscriber.Email);
                var name = EscapeCsvField(subscriber.FirstName ?? "");
                var tags = EscapeCsvField(subscriber.Tags);
                var reason = EscapeCsvField(subscriber.ColdReason);

                await writer.WriteLineAsync($"{subscriber.Id},{email},{name},{subscriber.State},{subscriber.CreatedAt:yyyy-MM-dd},{subscriber.AccountAgeDays},{subscriber.TagCount},{tags},{subscriber.EngagementTier},{reason}");
            }
        }

        Console.WriteLine($"Exported cold subscribers to {path}");
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

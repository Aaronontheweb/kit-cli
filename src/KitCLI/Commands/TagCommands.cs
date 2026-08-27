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
                    {
                        format = args[++i];
                    }

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
                {
                    break;
                }

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
            {
                outputPath += ".csv";
            }

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
                    {
                        outputPath = args[++i];
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

    // ==============================
    // Tag administration (write ops)
    // ==============================

    /// <summary>
    /// Creates a single tag: kit tag create &lt;name&gt;
    /// </summary>
    public static async Task<int> HandleCreate(string[] args, IKitApiClient client)
    {
        if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.WriteLine("Usage: kit tag create <name>");
            return 1;
        }

        var name = string.Join(" ", args).Trim();

        using var progress = new ProgressIndicator($"Creating tag '{name}'");

        try
        {
            var tag = await client.CreateTagAsync(new TagCreateRequest { Name = name });

            if (tag == null)
            {
                progress.Complete("Failed");
                Console.Error.WriteLine("Failed to create tag: API returned no tag.");
                return 1;
            }

            progress.Complete($"✓ Created tag '{tag.Name}' (ID: {tag.Id})");
            return 0;
        }
        catch (Exception ex)
        {
            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to create tag: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Renames a tag: kit tag rename &lt;id|name&gt; &lt;new-name&gt;
    /// </summary>
    public static async Task<int> HandleRename(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit tag rename <id|name> <new-name>");
            return 1;
        }

        var tag = await ResolveTagAsync(client, args[0]);
        if (tag == null)
        {
            Console.Error.WriteLine($"Tag not found: {args[0]}");
            return 1;
        }

        var newName = string.Join(" ", args[1..]).Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            Console.Error.WriteLine("New tag name cannot be empty.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Renaming tag '{tag.Name}' to '{newName}'");

        try
        {
            var updated = await client.RenameTagAsync(tag.Id, newName);

            if (updated == null)
            {
                progress.Complete("Failed");
                Console.Error.WriteLine("Failed to rename tag: API returned no tag.");
                return 1;
            }

            progress.Complete($"✓ Renamed tag '{updated.Name}' (ID: {updated.Id})");
            return 0;
        }
        catch (Exception ex)
        {
            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to rename tag: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Deletes a tag: kit tag delete &lt;id|name&gt; [--force|-y]
    /// Requires strong confirmation because deletion is destructive.
    /// </summary>
    public static async Task<int> HandleDelete(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit tag delete <id|name> [--force|-y]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --force, -y    Skip confirmation prompt");
            return 1;
        }

        bool force = args.Contains("--force") || args.Contains("-y") || args.Contains("--yes");

        var tag = await ResolveTagAsync(client, args[0]);
        if (tag == null)
        {
            Console.Error.WriteLine($"Tag not found: {args[0]}");
            return 1;
        }

        if (!ConfirmDestructive(
                $"WARNING: You are about to PERMANENTLY delete tag '{tag.Name}' (ID: {tag.Id}). " +
                "Subscribers are not deleted, but this tag association is lost forever. This action cannot be undone.",
                force))
        {
            Console.WriteLine("Cancelled.");
            return 0;
        }

        using var progress = new ProgressIndicator($"Deleting tag '{tag.Name}'");

        try
        {
            var success = await client.DeleteTagAsync(tag.Id);

            if (success)
            {
                progress.Complete($"✓ Deleted tag '{tag.Name}' (ID: {tag.Id})");
                return 0;
            }

            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to delete tag: {tag.Name}");
            return 1;
        }
        catch (Exception ex)
        {
            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to delete tag: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Adds a subscriber to a single tag: kit tag add-subscriber &lt;tag-id|tag-name&gt; &lt;email&gt;
    /// </summary>
    public static async Task<int> HandleAddSubscriber(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit tag add-subscriber <tag-id|tag-name> <email>");
            return 1;
        }

        var tag = await ResolveTagAsync(client, args[0]);
        if (tag == null)
        {
            Console.Error.WriteLine($"Tag not found: {args[0]}");
            return 1;
        }

        var email = args[1].Trim();
        if (!email.Contains('@'))
        {
            Console.Error.WriteLine($"Invalid email address: {email}");
            return 1;
        }

        using var progress = new ProgressIndicator($"Adding {email} to tag '{tag.Name}'");

        try
        {
            var success = await client.TagSubscriberAsync(tag.Id, email);

            if (success)
            {
                progress.Complete($"✓ Added {email} to tag '{tag.Name}' (ID: {tag.Id})");
                return 0;
            }

            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to add {email} to tag '{tag.Name}'");
            return 1;
        }
        catch (Exception ex)
        {
            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to add {email} to tag '{tag.Name}': {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Removes a subscriber from a single tag: kit tag remove-subscriber &lt;tag-id|tag-name&gt; &lt;id|email&gt; [--force|-y]
    /// Requires strong confirmation because untagging is destructive.
    /// </summary>
    public static async Task<int> HandleRemoveSubscriber(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit tag remove-subscriber <tag-id|tag-name> <id|email> [--force|-y]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --force, -y    Skip confirmation prompt");
            return 1;
        }

        bool force = args.Contains("--force") || args.Contains("-y") || args.Contains("--yes");

        var tag = await ResolveTagAsync(client, args[0]);
        if (tag == null)
        {
            Console.Error.WriteLine($"Tag not found: {args[0]}");
            return 1;
        }

        var identifier = args[1].Trim();

        Subscriber? subscriber;
        if (long.TryParse(identifier, out var subId))
        {
            subscriber = await client.GetSubscriberAsync(subId);
        }
        else if (identifier.Contains('@'))
        {
            subscriber = await client.GetSubscriberByEmailAsync(identifier);
        }
        else
        {
            Console.Error.WriteLine("Invalid subscriber identifier. Please provide a subscriber ID or email address.");
            return 1;
        }

        if (subscriber == null)
        {
            Console.Error.WriteLine($"Subscriber not found: {identifier}");
            return 1;
        }

        if (!ConfirmDestructive(
                $"WARNING: This will remove tag '{tag.Name}' from subscriber {subscriber.EmailAddress} (ID: {subscriber.Id}). " +
                "This action is destructive and cannot be undone.",
                force))
        {
            Console.WriteLine("Cancelled.");
            return 0;
        }

        using var progress = new ProgressIndicator($"Removing tag '{tag.Name}' from {subscriber.EmailAddress}");

        try
        {
            var success = await client.UntagSubscriberAsync(tag.Id, subscriber.Id);

            if (success)
            {
                progress.Complete($"✓ Removed tag '{tag.Name}' from {subscriber.EmailAddress}");
                return 0;
            }

            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to remove tag from {subscriber.EmailAddress}");
            return 1;
        }
        catch (Exception ex)
        {
            progress.Complete("Failed");
            Console.Error.WriteLine($"Failed to remove tag from {subscriber.EmailAddress}: {ex.Message}");
            return 1;
        }
    }

    // ==============================
    // Bulk tag operations
    // ==============================

    /// <summary>
    /// Bulk creates tags: kit tag bulk-create &lt;name1,name2,...&gt; | --file &lt;path&gt;
    /// Preflights input, preserves per-record failures, returns 1 if any record fails.
    /// </summary>
    public static async Task<int> HandleBulkCreate(string[] args, IKitApiClient client)
    {
        var (items, error) = ReadBulkItems(args, "tag names");
        if (error != null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        items = items.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (items.Count == 0)
        {
            Console.WriteLine("Usage: kit tag bulk-create <name1,name2,...> | --file <path>");
            return 1;
        }

        ShowPreflight($"creating {items.Count:N0} tag(s)", items);

        int successCount = 0;
        int failCount = 0;

        foreach (var name in items)
        {
            try
            {
                var tag = await client.CreateTagAsync(new TagCreateRequest { Name = name });

                if (tag != null)
                {
                    Console.WriteLine($"✓ Created tag '{tag.Name}' (ID: {tag.Id})");
                    successCount++;
                }
                else
                {
                    Console.Error.WriteLine($"Failed to create tag '{name}': API returned no tag");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create tag '{name}': {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Bulk create complete — Tags created: {successCount}, Failed: {failCount}");

        return failCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// Bulk applies a tag to many subscribers: kit tag bulk-apply &lt;tag-id|tag-name&gt; &lt;email1,id1,...&gt; | --file &lt;path&gt;
    /// Preflights input, preserves per-record failures, returns 1 if any record fails.
    /// </summary>
    public static async Task<int> HandleBulkApply(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit tag bulk-apply <tag-id|tag-name> <email1,id1,...> | --file <path>");
            return 1;
        }

        var tag = await ResolveTagAsync(client, args[0]);
        if (tag == null)
        {
            Console.Error.WriteLine($"Tag not found: {args[0]}");
            return 1;
        }

        var (items, error) = ReadBulkItems(args[1..], "subscribers");
        if (error != null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        items = items.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (items.Count == 0)
        {
            Console.WriteLine("No subscribers to tag.");
            return 1;
        }

        ShowPreflight($"applying tag '{tag.Name}' (ID: {tag.Id}) to {items.Count:N0} subscriber(s)", items);

        int successCount = 0;
        int failCount = 0;

        foreach (var item in items)
        {
            try
            {
                string email;

                if (item.Contains('@'))
                {
                    email = item;
                }
                else if (long.TryParse(item, out var subId))
                {
                    var sub = await client.GetSubscriberAsync(subId);
                    if (sub == null)
                    {
                        Console.Error.WriteLine($"Subscriber not found: {item}");
                        failCount++;
                        continue;
                    }

                    email = sub.EmailAddress;
                }
                else
                {
                    Console.Error.WriteLine($"Invalid subscriber identifier: {item}");
                    failCount++;
                    continue;
                }

                var success = await client.TagSubscriberAsync(tag.Id, email);

                if (success)
                {
                    Console.WriteLine($"✓ Tagged {email}");
                    successCount++;
                }
                else
                {
                    Console.Error.WriteLine($"Failed to tag {item}");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to tag '{item}': {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Bulk apply complete — Tagged: {successCount}, Failed: {failCount}");

        return failCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// Bulk removes a tag from many subscribers: kit tag bulk-remove &lt;tag-id|tag-name&gt; &lt;id1,email1,...&gt; | --file &lt;path&gt; [--force|-y]
    /// Requires strong confirmation because untagging is destructive.
    /// </summary>
    public static async Task<int> HandleBulkRemove(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit tag bulk-remove <tag-id|tag-name> <id1,email1,...> | --file <path> [--force|-y]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --force, -y    Skip confirmation prompt");
            return 1;
        }

        bool force = args.Contains("--force") || args.Contains("-y") || args.Contains("--yes");

        var tag = await ResolveTagAsync(client, args[0]);
        if (tag == null)
        {
            Console.Error.WriteLine($"Tag not found: {args[0]}");
            return 1;
        }

        var (items, error) = ReadBulkItems(args[1..], "subscribers");
        if (error != null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        items = items.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (items.Count == 0)
        {
            Console.WriteLine("No subscribers to untag.");
            return 1;
        }

        ShowPreflight($"removing tag '{tag.Name}' (ID: {tag.Id}) from {items.Count:N0} subscriber(s)", items);

        if (!ConfirmDestructive(
                $"WARNING: This will remove tag '{tag.Name}' from {items.Count:N0} subscriber(s). " +
                "This action is destructive and cannot be undone.",
                force))
        {
            Console.WriteLine("Cancelled.");
            return 0;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var item in items)
        {
            try
            {
                Subscriber? subscriber;

                if (long.TryParse(item, out var subId))
                {
                    subscriber = await client.GetSubscriberAsync(subId);
                }
                else if (item.Contains('@'))
                {
                    subscriber = await client.GetSubscriberByEmailAsync(item);
                }
                else
                {
                    Console.Error.WriteLine($"Invalid subscriber identifier: {item}");
                    failCount++;
                    continue;
                }

                if (subscriber == null)
                {
                    Console.Error.WriteLine($"Subscriber not found: {item}");
                    failCount++;
                    continue;
                }

                var success = await client.UntagSubscriberAsync(tag.Id, subscriber.Id);

                if (success)
                {
                    Console.WriteLine($"✓ Removed tag from {subscriber.EmailAddress}");
                    successCount++;
                }
                else
                {
                    Console.Error.WriteLine($"Failed to remove tag from {item}");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to remove tag from '{item}': {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Bulk remove complete — Removed: {successCount}, Failed: {failCount}");

        return failCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// Bulk deletes tags: kit tag bulk-delete &lt;id1,name1,...&gt; | --file &lt;path&gt; [--force|-y]
    /// Requires strong confirmation because deletion is destructive.
    /// </summary>
    public static async Task<int> HandleBulkDelete(string[] args, IKitApiClient client)
    {
        bool force = args.Contains("--force") || args.Contains("-y") || args.Contains("--yes");

        var (items, error) = ReadBulkItems(args, "tags");
        if (error != null)
        {
            Console.Error.WriteLine(error);
            return 1;
        }

        items = items.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (items.Count == 0)
        {
            Console.WriteLine("Usage: kit tag bulk-delete <id1,name1,...> | --file <path> [--force|-y]");
            return 1;
        }

        // Resolve identifiers (numeric IDs or tag names) up front so preflight
        // reports accurate totals and samples before confirmation.
        var allTags = await client.GetTagsAsync();
        var resolved = new List<(string Identifier, Tag Tag)>();

        foreach (var item in items)
        {
            Tag? tag = null;

            if (long.TryParse(item, out var id))
            {
                tag = allTags.FirstOrDefault(t => t.Id == id);
            }
            else
            {
                tag = allTags.FirstOrDefault(t => t.Name.Equals(item, StringComparison.OrdinalIgnoreCase));
            }

            if (tag != null)
            {
                resolved.Add((item, tag));
            }
        }

        int unresolved = items.Count - resolved.Count;

        ShowPreflight(
            $"deleting {resolved.Count:N0} tag(s)",
            resolved.Select(r => $"{r.Tag.Name} (ID: {r.Tag.Id})").ToList());

        if (unresolved > 0)
        {
            Console.WriteLine($"Note: {unresolved:N0} item(s) did not match an existing tag and will be reported as failures.");
        }

        if (!ConfirmDestructive(
                $"WARNING: This will PERMANENTLY delete {resolved.Count:N0} tag(s). " +
                "Subscribers are not deleted, but tag associations are lost forever. This cannot be undone.",
                force))
        {
            Console.WriteLine("Cancelled.");
            return 0;
        }

        int successCount = 0;
        int failCount = unresolved;

        foreach (var (identifier, tag) in resolved)
        {
            try
            {
                var success = await client.DeleteTagAsync(tag.Id);

                if (success)
                {
                    Console.WriteLine($"✓ Deleted tag '{tag.Name}' (ID: {tag.Id})");
                    successCount++;
                }
                else
                {
                    Console.Error.WriteLine($"Failed to delete tag '{tag.Name}'");
                    failCount++;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to delete tag '{identifier}': {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Bulk delete complete — Deleted: {successCount}, Failed: {failCount}");

        return failCount > 0 ? 1 : 0;
    }

    // ==============================
    // Helpers
    // ==============================

    /// <summary>
    /// Resolves a tag identifier (numeric ID or case-insensitive name) to a Tag.
    /// </summary>
    private static async Task<Tag?> ResolveTagAsync(IKitApiClient client, string identifier)
    {
        var tags = await client.GetTagsAsync();

        if (long.TryParse(identifier, out var id))
        {
            return tags.FirstOrDefault(t => t.Id == id);
        }

        return tags.FirstOrDefault(t => t.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reads bulk items from inline comma-separated values and/or --file &lt;path&gt;.
    /// Each non-flag argument is split on commas; file lines are split the same way.
    /// </summary>
    private static (List<string> Items, string? Error) ReadBulkItems(string[] args, string valueLabel)
    {
        var items = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--file")
            {
                if (i + 1 >= args.Length)
                {
                    return (items, $"Missing file path after --file.");
                }

                var path = args[++i];

                if (!File.Exists(path))
                {
                    return (items, $"File not found: {path}");
                }

                foreach (var line in File.ReadLines(path))
                {
                    foreach (var part in line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        items.Add(part);
                    }
                }
            }
            else
            {
                foreach (var part in args[i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    items.Add(part);
                }
            }
        }

        return (items, null);
    }

    /// <summary>
    /// Displays a preflight summary: total count and a sample of the items.
    /// </summary>
    private static void ShowPreflight(string action, IReadOnlyList<string> items, int sampleSize = 5)
    {
        Console.WriteLine($"Preflight: {action}");
        Console.WriteLine($"  Total: {items.Count:N0} item(s)");

        if (items.Count > 0)
        {
            Console.WriteLine("  Sample:");
            foreach (var item in items.Take(sampleSize))
            {
                Console.WriteLine($"    - {item}");
            }

            if (items.Count > sampleSize)
            {
                Console.WriteLine($"    ... and {items.Count - sampleSize:N0} more");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Strong confirmation prompt. Returns true when --force is set or the user answers y/yes.
    /// </summary>
    private static bool ConfirmDestructive(string warning, bool force)
    {
        if (force)
        {
            return true;
        }

        Console.WriteLine(warning);
        Console.Write("Are you sure? [y/N]: ");

        var response = Console.ReadLine()?.Trim();
        return response != null &&
               (response.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                response.Equals("yes", StringComparison.OrdinalIgnoreCase));
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

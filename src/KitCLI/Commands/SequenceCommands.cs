using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

public static class SequenceCommands
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

        using var progress = new ProgressIndicator("Fetching sequences");

        var response = await client.GetSequencesAsync(limit);
        var sequences = response.Data;

        progress.Complete($"Found {sequences.Length:N0} sequences");

        PrintSequences(sequences, format);
        return 0;
    }

    public static async Task<int> HandleGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence get <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
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

        using var progress = new ProgressIndicator($"Fetching sequence {id}");

        var sequence = await client.GetSequenceAsync(id);

        if (sequence == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        progress.Complete($"Found sequence: {sequence.Name}");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                sequence,
                KitJsonIndentedContext.Default.Sequence);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequences([sequence], format);
        }

        return 0;
    }

    public static async Task<int> HandleEmails(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence emails <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json)");
            Console.WriteLine("  --include-content      Include email HTML content in response");
            Console.WriteLine("  --include-stats        Include email performance stats");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";
        bool includeContent = false;
        bool includeStats = false;

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
                case "--include-content":
                    includeContent = true;
                    break;
                case "--include-stats":
                    includeStats = true;
                    break;
            }
        }

        using var progress = new ProgressIndicator($"Fetching emails for sequence {sequenceId}");

        var emails = new List<SequenceEmail>();
        await foreach (var email in client.GetAllSequenceEmailsAsync(
                           sequenceId,
                           includeContent: includeContent,
                           includeStats: includeStats))
        {
            emails.Add(email);
        }

        progress.Complete($"Found {emails.Count:N0} emails in sequence");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                emails.ToArray(),
                KitJsonIndentedContext.Default.SequenceEmailArray);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequenceEmails(emails);
        }

        return 0;
    }

    public static async Task<int> HandleEmailGet(string[] args, IKitApiClient client)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: kit sequence email get <sequence-id> <email-id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --format, -f <format>  Output format (table, json)");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        if (!long.TryParse(args[1], out var emailId))
        {
            Console.WriteLine("Invalid email ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "json";

        for (int i = 2; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i];
            }
        }

        using var progress = new ProgressIndicator($"Fetching email {emailId} for sequence {sequenceId}");

        var email = await client.GetSequenceEmailAsync(sequenceId, emailId);

        if (email == null)
        {
            progress.Complete($"Email not found: {emailId} in sequence {sequenceId}");
            return 1;
        }

        progress.Complete($"Found email: {email.Subject}");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                email,
                KitJsonIndentedContext.Default.SequenceEmail);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequenceEmails([email]);
        }

        return 0;
    }

    public static async Task<int> HandleSubscribers(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence subscribers <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --status, -s <status>  Filter by status (active, inactive, bounced, complained, cancelled, all)");
            Console.WriteLine("  --format, -f <format>  Output format (table, json, csv)");
            Console.WriteLine("  --output, -o <file>    Export to file");
            Console.WriteLine("  --all                  Fetch all subscribers");
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        string? status = null;
        string format = "table";
        string? outputPath = null;
        bool fetchAll = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--status":
                case "--state":
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

        using var progress = new ProgressIndicator($"Fetching subscribers for sequence {sequenceId}");

        List<SequenceSubscriber> subscribers = new();

        if (fetchAll)
        {
            await foreach (var subscriber in client.GetAllSequenceSubscribersAsync(sequenceId, status))
            {
                subscribers.Add(subscriber);
            }
        }
        else
        {
            var response = await client.GetSequenceSubscribersAsync(sequenceId, status, 100);
            subscribers.AddRange(response.Data);
        }

        progress.Complete($"Found {subscribers.Count:N0} subscribers in sequence");

        if (!string.IsNullOrEmpty(outputPath))
        {
            await ExportSequenceSubscribers(subscribers, outputPath);
            Console.WriteLine($"✓ Exported {subscribers.Count:N0} subscribers to {outputPath}");
        }
        else
        {
            PrintSequenceSubscribers(subscribers, format);
        }

        return 0;
    }

    public static async Task<int> HandleStats(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence stats <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Calculating stats for sequence {id}");

        var sequence = await client.GetSequenceAsync(id);

        if (sequence == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        var emails = new List<SequenceEmail>();
        await foreach (var email in client.GetAllSequenceEmailsAsync(id, includeStats: true))
        {
            emails.Add(email);
        }

        progress.Complete($"Retrieved stats for sequence: {sequence.Name}");

        Console.WriteLine("\nSequence Statistics");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Name: {sequence.Name}");
        Console.WriteLine("Total Subscribers: N/A (use 'kit sequence subscribers <id> --all' to count)");
        Console.WriteLine($"Total Emails: {sequence.EmailCount:N0}");
        Console.WriteLine($"Status: {(sequence.Active ? "Active" : "Inactive")}");
        Console.WriteLine($"On Hold: {(sequence.Hold ? "Yes" : "No")}");
        Console.WriteLine($"Repeating: {(sequence.Repeat ? "Yes" : "No")}");

        if (emails.Count > 0)
        {
            Console.WriteLine("\nEmail Performance:");
            Console.WriteLine(new string('─', 60));

            var performance = SequenceEmailMetrics.Aggregate(emails);

            Console.WriteLine($"Total Emails Sent: {performance.Recipients:N0}");
            Console.WriteLine($"Total Opens: {performance.Opens:N0}");
            Console.WriteLine($"Total Clicks: {performance.Clicks:N0}");
            Console.WriteLine($"Average Open Rate: {performance.OpenRate:F2}%");
            Console.WriteLine($"Average Click Rate: {performance.ClickRate:F2}%");

            Console.WriteLine("\nTop Performing Emails:");
            Console.WriteLine(new string('─', 60));

            var topEmails = emails
                .OrderByDescending(e => e.Stats?.OpenRate ?? 0)
                .Take(3);

            foreach (var email in topEmails)
            {
                Console.WriteLine($"  • \"{TruncateString(email.Subject, 40)}\"");
                Console.WriteLine($"    Position: {email.Position}, Delay: {email.DelayFormatted}");
                Console.WriteLine($"    Opens: {email.Stats?.OpenRate ?? 0:F2}%, Clicks: {email.Stats?.ClickRate ?? 0:F2}%");
            }
        }

        return 0;
    }

    public static async Task<int> HandleAnalyze(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence analyze <id>");
            return 1;
        }

        if (!long.TryParse(args[0], out var id))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        using var progress = new ProgressIndicator($"Analyzing sequence {id}");

        var stats = await client.GetSequenceStatsAsync(id);

        if (stats == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        progress.Complete("Analysis complete");

        Console.WriteLine("\nSequence Analysis");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Total Subscribers: {stats.TotalSubscribers:N0}");
        var activeRate = stats.TotalSubscribers > 0
            ? (double)stats.ActiveSubscribers / stats.TotalSubscribers
            : 0;
        Console.WriteLine($"Active: {stats.ActiveSubscribers:N0} ({activeRate:P1})");
        Console.WriteLine($"Cancelled: {stats.CancelledSubscribers:N0}");
        Console.WriteLine();
        Console.WriteLine($"Emails Sent: {stats.EmailsSent:N0}");
        Console.WriteLine($"Average Open Rate: {stats.AverageOpenRate:F2}%");
        Console.WriteLine($"Average Click Rate: {stats.AverageClickRate:F2}%");

        Console.WriteLine("\nInsights:");
        Console.WriteLine(new string('─', 60));

        if (stats.AverageOpenRate < 20)
        {
            Console.WriteLine("⚠️  Below average open rate - review subject lines and preview text");
        }

        if (stats.AverageClickRate < 2)
        {
            Console.WriteLine("⚠️  Low click rate - consider improving CTAs and content relevance");
        }

        return 0;
    }

    private static void PrintSequences(IEnumerable<Sequence> sequences, string format)
    {
        var sequenceList = sequences.ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                sequenceList.ToArray(),
                KitJsonIndentedContext.Default.SequenceArray);
            Console.WriteLine(json);
            return;
        }

        if (!sequenceList.Any())
        {
            Console.WriteLine("No sequences found.");
            return;
        }

        // Table format
        // Note: Kit V4 API list endpoint only returns id, name, hold, repeat, created_at
        // Use 'kit sequence stats <id>' or 'kit sequence analyze <id>' for subscriber/email counts
        const int idWidth = 10;
        const int nameWidth = 40;
        const int statusWidth = 10;
        const int holdWidth = 7;
        const int createdWidth = 12;

        Console.WriteLine(new string('─', idWidth + nameWidth + statusWidth + holdWidth + createdWidth + 11));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Name",-nameWidth} │ {"Status",-statusWidth} │ {"On Hold",-holdWidth} │ {"Created",createdWidth} │");
        Console.WriteLine(new string('─', idWidth + nameWidth + statusWidth + holdWidth + createdWidth + 11));

        foreach (var sequence in sequenceList.OrderBy(s => s.Name))
        {
            var name = TruncateString(sequence.Name, nameWidth);
            var status = sequence.Active ? "Active" : "Inactive";
            var hold = sequence.Hold ? "Yes" : "No";
            var created = sequence.CreatedAt.ToString("yyyy-MM-dd");

            Console.WriteLine($"│ {sequence.Id,-idWidth} │ {name,-nameWidth} │ {status,-statusWidth} │ {hold,-holdWidth} │ {created,createdWidth} │");
        }

        Console.WriteLine(new string('─', idWidth + nameWidth + statusWidth + holdWidth + createdWidth + 11));
        Console.WriteLine($"Total: {sequenceList.Count:N0} sequence(s)");
        Console.WriteLine("\nTip: Use 'kit sequence stats <id>' for subscriber and email counts.");
    }

    private static void PrintSequenceEmails(IEnumerable<SequenceEmail> emails)
    {
        var emailList = emails.OrderBy(e => e.Position).ToList();

        if (!emailList.Any())
        {
            Console.WriteLine("No emails found in sequence.");
            return;
        }

        Console.WriteLine("\nSequence Emails:");
        Console.WriteLine(new string('─', 80));

        foreach (var email in emailList)
        {
            Console.WriteLine($"\n{email.Position}. {email.Subject}");
            Console.WriteLine($"   Delay: {email.DelayFormatted}");
            Console.WriteLine($"   Sender: {email.EmailAddress}");
            Console.WriteLine($"   Published: {(email.Published ? "Yes" : "No")}");

            if (email.EmailTemplateId.HasValue)
            {
                Console.WriteLine($"   Template ID: {email.EmailTemplateId.Value}");
            }

            if (email.SendDays is { Length: > 0 })
            {
                Console.WriteLine($"   Send Days: {email.SendDaysFormatted}");
            }

            if (!string.IsNullOrEmpty(email.Content))
            {
                Console.WriteLine("   Content:");
                Console.WriteLine("   ┌─");
                foreach (var line in email.Content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
                {
                    Console.WriteLine($"   │ {line}");
                }
                Console.WriteLine("   └─");
            }

            if (email.Stats != null)
            {
                Console.WriteLine($"   Recipients: {email.Stats.Recipients:N0}");
                Console.WriteLine($"   Opens: {email.Stats.Opens:N0} ({email.Stats.OpenRate:F2}%)");
                Console.WriteLine($"   Clicks: {email.Stats.Clicks:N0} ({email.Stats.ClickRate:F2}%)");

                if (email.Stats.EmailUnsubscribes > 0)
                {
                    Console.WriteLine($"   Unsubscribes: {email.Stats.EmailUnsubscribes} ({email.Stats.UnsubscribeRate:F2}%)");
                }
            }
        }
    }

    private static void PrintSequenceSubscribers(IEnumerable<SequenceSubscriber> subscribers, string format)
    {
        var subscriberList = subscribers.ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                subscriberList.ToArray(),
                KitJsonContext.Default.SequenceSubscriberArray);
            Console.WriteLine(json);
            return;
        }

        if (!subscriberList.Any())
        {
            Console.WriteLine("No subscribers found in sequence.");
            return;
        }

        // Table format (simplified)
        Console.WriteLine($"\n{"Email",-40} {"State",-12} {"Added",-20}");
        Console.WriteLine(new string('─', 75));

        foreach (var sub in subscriberList.Take(50))
        {
            var email = TruncateString(sub.EmailAddress, 40);
            var addedAt = sub.AddedAt.ToString("yyyy-MM-dd HH:mm");

            Console.WriteLine($"{email,-40} {sub.State,-12} {addedAt,-20}");
        }

        if (subscriberList.Count > 50)
        {
            Console.WriteLine($"\n... and {subscriberList.Count - 50:N0} more");
        }
    }

    private static async Task ExportSequenceSubscribers(List<SequenceSubscriber> subscribers, string outputPath)
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
                KitJsonContext.Default.SequenceSubscriberArray);
            await writer.WriteAsync(json);
        }
        else
        {
            await writer.WriteLineAsync("id,email_address,first_name,state,created_at,added_at");

            foreach (var sub in subscribers)
            {
                var email = EscapeCsvField(sub.EmailAddress);
                var name = EscapeCsvField(sub.FirstName ?? "");
                await writer.WriteLineAsync(
                    $"{sub.Id},{email},{name},{sub.State}," +
                    $"{sub.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'},{sub.AddedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}");
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

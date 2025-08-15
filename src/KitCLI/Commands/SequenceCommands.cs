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
            return 1;
        }

        if (!long.TryParse(args[0], out var sequenceId))
        {
            Console.WriteLine("Invalid sequence ID. Please provide a numeric ID.");
            return 1;
        }

        string format = "table";

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
            }
        }

        using var progress = new ProgressIndicator($"Fetching emails for sequence {sequenceId}");

        var response = await client.GetSequenceEmailsAsync(sequenceId, 100);
        var emails = response.Data;

        progress.Complete($"Found {emails.Length:N0} emails in sequence");

        if (format == "json")
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                emails,
                KitJsonIndentedContext.Default.SequenceEmailArray);
            Console.WriteLine(json);
        }
        else
        {
            PrintSequenceEmails(emails);
        }

        return 0;
    }

    public static async Task<int> HandleSubscribers(string[] args, IKitApiClient client)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: kit sequence subscribers <id> [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  --state, -s <state>    Filter by state (active, completed, cancelled)");
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

        string? state = null;
        string format = "table";
        string? outputPath = null;
        bool fetchAll = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--state":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        state = args[++i];
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
            await foreach (var subscriber in client.GetAllSequenceSubscribersAsync(sequenceId, state))
            {
                subscribers.Add(subscriber);
            }
        }
        else
        {
            var response = await client.GetSequenceSubscribersAsync(sequenceId, state, 100);
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

        var sequenceTask = client.GetSequenceAsync(id);
        var emailsTask = client.GetSequenceEmailsAsync(id, 100);

        await Task.WhenAll(sequenceTask, emailsTask);

        var sequence = await sequenceTask;
        var emails = await emailsTask;

        if (sequence == null)
        {
            progress.Complete($"Sequence not found: {id}");
            return 1;
        }

        progress.Complete($"Retrieved stats for sequence: {sequence.Name}");

        Console.WriteLine("\nSequence Statistics");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine($"Name: {sequence.Name}");
        Console.WriteLine($"Total Subscribers: {sequence.SubscriberCount:N0}");
        Console.WriteLine($"Total Emails: {sequence.EmailCount:N0}");
        Console.WriteLine($"Status: {(sequence.Hold ? "On Hold" : "Active")}");
        Console.WriteLine($"Repeating: {(sequence.Repeat ? "Yes" : "No")}");

        if (emails.Data.Length > 0)
        {
            Console.WriteLine("\nEmail Performance:");
            Console.WriteLine(new string('─', 60));

            var totalOpens = emails.Data.Sum(e => e.TotalOpens);
            var totalClicks = emails.Data.Sum(e => e.TotalClicks);
            var totalRecipients = emails.Data.Sum(e => e.TotalRecipients);
            var avgOpenRate = emails.Data.Average(e => e.OpenRate);
            var avgClickRate = emails.Data.Average(e => e.ClickRate);

            Console.WriteLine($"Total Emails Sent: {totalRecipients:N0}");
            Console.WriteLine($"Total Opens: {totalOpens:N0}");
            Console.WriteLine($"Total Clicks: {totalClicks:N0}");
            Console.WriteLine($"Average Open Rate: {avgOpenRate:P1}");
            Console.WriteLine($"Average Click Rate: {avgClickRate:P1}");

            Console.WriteLine("\nTop Performing Emails:");
            Console.WriteLine(new string('─', 60));

            var topEmails = emails.Data
                .OrderByDescending(e => e.OpenRate)
                .Take(3);

            foreach (var email in topEmails)
            {
                Console.WriteLine($"  • \"{TruncateString(email.Subject, 40)}\"");
                Console.WriteLine($"    Position: {email.Position}, Delay: {email.DelayFormatted}");
                Console.WriteLine($"    Opens: {email.OpenRate:P1}, Clicks: {email.ClickRate:P1}");
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
        Console.WriteLine($"Active: {stats.ActiveSubscribers:N0} ({(double)stats.ActiveSubscribers / stats.TotalSubscribers:P1})");
        Console.WriteLine($"Completed: {stats.CompletedSubscribers:N0} ({stats.CompletionRate:P1})");
        Console.WriteLine($"Cancelled: {stats.CancelledSubscribers:N0}");
        Console.WriteLine();
        Console.WriteLine($"Emails Sent: {stats.EmailsSent:N0}");
        Console.WriteLine($"Average Open Rate: {stats.AverageOpenRate:P1}");
        Console.WriteLine($"Average Click Rate: {stats.AverageClickRate:P1}");

        Console.WriteLine("\nInsights:");
        Console.WriteLine(new string('─', 60));

        if (stats.CompletionRate < 0.5)
        {
            Console.WriteLine("⚠️  Low completion rate - consider reviewing email timing or content");
        }

        if (stats.AverageOpenRate < 0.2)
        {
            Console.WriteLine("⚠️  Below average open rate - review subject lines and preview text");
        }

        if (stats.AverageClickRate < 0.02)
        {
            Console.WriteLine("⚠️  Low click rate - consider improving CTAs and content relevance");
        }

        if (stats.CompletionRate > 0.7)
        {
            Console.WriteLine("✓ High completion rate - sequence is performing well");
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
        const int idWidth = 10;
        const int nameWidth = 35;
        const int subsWidth = 12;
        const int emailsWidth = 8;
        const int statusWidth = 10;

        Console.WriteLine(new string('─', idWidth + nameWidth + subsWidth + emailsWidth + statusWidth + 10));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Name",-nameWidth} │ {"Subscribers",subsWidth} │ {"Emails",emailsWidth} │ {"Status",-statusWidth} │");
        Console.WriteLine(new string('─', idWidth + nameWidth + subsWidth + emailsWidth + statusWidth + 10));

        foreach (var sequence in sequenceList.OrderBy(s => s.Name))
        {
            var name = TruncateString(sequence.Name, nameWidth);
            var status = sequence.Hold ? "On Hold" : "Active";

            Console.WriteLine($"│ {sequence.Id,-idWidth} │ {name,-nameWidth} │ {sequence.SubscriberCount,subsWidth:N0} │ {sequence.EmailCount,emailsWidth} │ {status,-statusWidth} │");
        }

        Console.WriteLine(new string('─', idWidth + nameWidth + subsWidth + emailsWidth + statusWidth + 10));
        Console.WriteLine($"Total: {sequenceList.Count:N0} sequence(s)");
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
            Console.WriteLine($"   Recipients: {email.TotalRecipients:N0}");
            Console.WriteLine($"   Opens: {email.UniqueOpens:N0} ({email.OpenRate:P1})");
            Console.WriteLine($"   Clicks: {email.UniqueClicks:N0} ({email.ClickRate:P1})");

            if (email.TotalUnsubscribes > 0)
            {
                Console.WriteLine($"   Unsubscribes: {email.TotalUnsubscribes} ({email.UnsubscribeRate:P1})");
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
        Console.WriteLine($"\n{"Email",-40} {"State",-12} {"Next Email",-20}");
        Console.WriteLine(new string('─', 75));

        foreach (var sub in subscriberList.Take(50))
        {
            var email = TruncateString(sub.EmailAddress, 40);
            var nextEmail = sub.NextEmailAt?.ToString("yyyy-MM-dd HH:mm") ??
                           (sub.IsCompleted ? "Completed" : "-");

            Console.WriteLine($"{email,-40} {sub.State,-12} {nextEmail,-20}");
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
            await writer.WriteLineAsync("subscriber_id,email_address,first_name,state,created_at,next_email_at,completed_at");

            foreach (var sub in subscribers)
            {
                var email = EscapeCsvField(sub.EmailAddress);
                var name = EscapeCsvField(sub.FirstName ?? "");
                var nextEmail = sub.NextEmailAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") ?? "";
                var completed = sub.CompletedAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") ?? "";

                await writer.WriteLineAsync(
                    $"{sub.SubscriberId},{email},{name},{sub.State}," +
                    $"{sub.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'},{nextEmail},{completed}");
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

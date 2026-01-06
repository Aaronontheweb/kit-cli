using System.Text;
using System.Text.Json;
using KitCLI.Models;

namespace KitCLI.Helpers;

public static class OutputFormatter
{
    public static void PrintSubscribers(IEnumerable<Subscriber> subscribers, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintSubscribersJson(subscribers);
                break;
            case "csv":
                PrintSubscribersCsv(subscribers);
                break;
            case "table":
            default:
                PrintSubscribersTable(subscribers);
                break;
        }
    }

    public static void PrintSubscribersTable(IEnumerable<Subscriber> subscribers)
    {
        var subscriberList = subscribers.ToList();
        if (!subscriberList.Any())
        {
            Console.WriteLine("No subscribers found.");
            return;
        }

        // Calculate column widths
        const int idWidth = 10;
        const int emailWidth = 35;
        const int nameWidth = 20;
        const int stateWidth = 12;
        const int tagsWidth = 30;
        const int createdWidth = 12;

        // Header
        Console.WriteLine(new string('─', idWidth + emailWidth + nameWidth + stateWidth + tagsWidth + createdWidth + 13));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Email",-emailWidth} │ {"Name",-nameWidth} │ {"State",-stateWidth} │ {"Tags",-tagsWidth} │ {"Created",-createdWidth} │");
        Console.WriteLine(new string('─', idWidth + emailWidth + nameWidth + stateWidth + tagsWidth + createdWidth + 13));

        // Data rows
        foreach (var sub in subscriberList)
        {
            var email = TruncateString(sub.EmailAddress, emailWidth);
            var name = TruncateString(sub.DisplayName, nameWidth);
            var tags = TruncateString(sub.TagList, tagsWidth);
            var created = sub.CreatedAt.ToString("yyyy-MM-dd");

            Console.WriteLine($"│ {sub.Id,-idWidth} │ {email,-emailWidth} │ {name,-nameWidth} │ {sub.State,-stateWidth} │ {tags,-tagsWidth} │ {created,-createdWidth} │");
        }

        // Footer
        Console.WriteLine(new string('─', idWidth + emailWidth + nameWidth + stateWidth + tagsWidth + createdWidth + 13));
        Console.WriteLine($"Total: {subscriberList.Count:N0} subscriber(s)");
    }

    public static void PrintSubscribersJson(IEnumerable<Subscriber> subscribers)
    {
        var json = JsonSerializer.Serialize(subscribers.ToArray(), KitJsonIndentedContext.Default.SubscriberArray);
        Console.WriteLine(json);
    }

    public static void PrintSubscribersCsv(IEnumerable<Subscriber> subscribers)
    {
        Console.WriteLine("id,email_address,first_name,state,tags,created_at");

        foreach (var sub in subscribers)
        {
            var tags = EscapeCsvField(sub.TagList);
            var name = EscapeCsvField(sub.FirstName ?? "");
            var email = EscapeCsvField(sub.EmailAddress);

            Console.WriteLine($"{sub.Id},{email},{name},{sub.State},{tags},{sub.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}");
        }
    }

    public static void PrintBroadcasts(IEnumerable<Broadcast> broadcasts, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintBroadcastsJson(broadcasts);
                break;
            case "csv":
                PrintBroadcastsCsv(broadcasts);
                break;
            case "table":
            default:
                PrintBroadcastsTable(broadcasts);
                break;
        }
    }

    public static void PrintBroadcastsTable(IEnumerable<Broadcast> broadcasts)
    {
        var broadcastList = broadcasts.ToList();
        if (!broadcastList.Any())
        {
            Console.WriteLine("No broadcasts found.");
            return;
        }

        // Calculate column widths
        const int idWidth = 10;
        const int subjectWidth = 40;
        const int statusWidth = 12;
        const int sentWidth = 20;

        // Header
        Console.WriteLine(new string('─', idWidth + subjectWidth + statusWidth + sentWidth + 7));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Subject",-subjectWidth} │ {"Status",-statusWidth} │ {"Sent/Scheduled",-sentWidth} │");
        Console.WriteLine(new string('─', idWidth + subjectWidth + statusWidth + sentWidth + 7));

        // Data rows
        foreach (var broadcast in broadcastList)
        {
            var subject = TruncateString(broadcast.Subject, subjectWidth);
            var sentDate = broadcast.SendAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

            Console.WriteLine($"│ {broadcast.Id,-idWidth} │ {subject,-subjectWidth} │ {broadcast.Status,-statusWidth} │ {sentDate,-sentWidth} │");
        }

        // Footer
        Console.WriteLine(new string('─', idWidth + subjectWidth + statusWidth + sentWidth + 7));
        Console.WriteLine($"Total: {broadcastList.Count:N0} broadcast(s)");
    }

    public static void PrintBroadcastsJson(IEnumerable<Broadcast> broadcasts)
    {
        var json = JsonSerializer.Serialize(broadcasts.ToArray(), KitJsonIndentedContext.Default.BroadcastArray);
        Console.WriteLine(json);
    }

    public static void PrintBroadcastsCsv(IEnumerable<Broadcast> broadcasts)
    {
        Console.WriteLine("id,subject,status,send_at,created_at");

        foreach (var broadcast in broadcasts)
        {
            var subject = EscapeCsvField(broadcast.Subject);
            var sendAt = broadcast.SendAt?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") ?? "";

            Console.WriteLine($"{broadcast.Id},{subject},{broadcast.Status},{sendAt},{broadcast.CreatedAt:yyyy-MM-dd'T'HH:mm:ss'Z'}");
        }
    }

    public static void PrintBroadcastStats(BroadcastStats stats, long broadcastId)
    {
        Console.WriteLine($"Broadcast Statistics (ID: {broadcastId})");
        Console.WriteLine(new string('─', 50));
        Console.WriteLine($"Recipients:      {stats.Recipients:N0}");
        // Kit V4 API returns rates as percentages (0-100), not decimals
        Console.WriteLine($"Opens:           {stats.EmailsOpened:N0} ({stats.OpenRate:F1}%)");
        Console.WriteLine($"Clicks:          {stats.TotalClicks:N0} ({stats.ClickRate:F1}%)");
        Console.WriteLine($"Unsubscribes:    {stats.Unsubscribes:N0}");
        if (!string.IsNullOrEmpty(stats.Status))
        {
            Console.WriteLine($"Status:          {stats.Status}");
        }
    }

    public static void PrintTags(IEnumerable<Tag> tags, string format)
    {
        var tagList = tags.ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = JsonSerializer.Serialize(tagList.ToArray(), KitJsonIndentedContext.Default.TagArray);
            Console.WriteLine(json);
            return;
        }

        if (!tagList.Any())
        {
            Console.WriteLine("No tags found.");
            return;
        }

        // Table format
        const int idWidth = 10;
        const int nameWidth = 40;
        const int createdWidth = 12;

        Console.WriteLine(new string('─', idWidth + nameWidth + createdWidth + 7));
        Console.WriteLine($"│ {"ID",-idWidth} │ {"Name",-nameWidth} │ {"Created",-createdWidth} │");
        Console.WriteLine(new string('─', idWidth + nameWidth + createdWidth + 7));

        foreach (var tag in tagList.OrderBy(t => t.Name))
        {
            var name = TruncateString(tag.Name, nameWidth);
            var created = tag.CreatedAt?.ToString("yyyy-MM-dd") ?? "-";

            Console.WriteLine($"│ {tag.Id,-idWidth} │ {name,-nameWidth} │ {created,-createdWidth} │");
        }

        Console.WriteLine(new string('─', idWidth + nameWidth + createdWidth + 7));
        Console.WriteLine($"Total: {tagList.Count:N0} tag(s)");
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

        // Escape quotes by doubling them
        field = field.Replace("\"", "\"\"");

        // Wrap in quotes if contains comma, quote, or newline
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field}\"";
        }

        return field;
    }
}

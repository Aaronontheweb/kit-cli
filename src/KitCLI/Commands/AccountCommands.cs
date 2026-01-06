using System.Text.Json;
using KitCLI.Helpers;
using KitCLI.Models;
using KitCLI.Services;

namespace KitCLI.Commands;

/// <summary>
/// Handles account-level commands.
/// </summary>
public static class AccountCommands
{
    /// <summary>
    /// Handle the 'kit account stats' command.
    /// Displays aggregate email statistics for the account.
    /// </summary>
    public static async Task<int> HandleStats(string[] args, IKitApiClient client)
    {
        if (CommandHelp.CheckForHelp(args))
        {
            return CommandHelp.ShowHelpAndReturn("account", "stats");
        }

        string format = "table";

        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--format" || args[i] == "-f") && i + 1 < args.Length)
            {
                format = args[++i].ToLowerInvariant();
            }
        }

        using var progress = new ProgressIndicator("Fetching account statistics");

        var stats = await client.GetAccountStatsAsync();

        if (stats == null)
        {
            progress.Complete("Failed to fetch stats");
            Console.WriteLine("Unable to retrieve account statistics. Please check your API key permissions.");
            return 1;
        }

        progress.Complete("Retrieved account statistics");

        PrintAccountStats(stats, format);
        return 0;
    }

    private static void PrintAccountStats(AccountStats stats, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                PrintAccountStatsJson(stats);
                break;
            case "csv":
                PrintAccountStatsCsv(stats);
                break;
            default:
                PrintAccountStatsTable(stats);
                break;
        }
    }

    private static void PrintAccountStatsTable(AccountStats stats)
    {
        var periodLabel = stats.EmailStatsMode switch
        {
            "last_90" => "Last 90 Days",
            "last_30" => "Last 30 Days",
            "last_7" => "Last 7 Days",
            _ => stats.EmailStatsMode
        };

        Console.WriteLine();
        Console.WriteLine($"Account Email Statistics ({periodLabel})");
        Console.WriteLine(new string('─', 50));
        Console.WriteLine($"Period:        {stats.Starting:yyyy-MM-dd} to {stats.Ending:yyyy-MM-dd}");
        Console.WriteLine($"Sent:          {stats.Sent:N0}");
        Console.WriteLine($"Opened:        {stats.Opened:N0} ({stats.OpenRate:F1}%)");
        Console.WriteLine($"Clicked:       {stats.Clicked:N0} ({stats.ClickRate:F1}%)");
        Console.WriteLine();
        Console.WriteLine("Tracking:");
        Console.WriteLine($"  Open tracking:  {(stats.OpenTrackingEnabled ? "Enabled" : "Disabled")}");
        Console.WriteLine($"  Click tracking: {(stats.ClickTrackingEnabled ? "Enabled" : "Disabled")}");
    }

    private static void PrintAccountStatsJson(AccountStats stats)
    {
        var json = JsonSerializer.Serialize(stats, KitJsonIndentedContext.Default.AccountStats);
        Console.WriteLine(json);
    }

    private static void PrintAccountStatsCsv(AccountStats stats)
    {
        Console.WriteLine("starting,ending,sent,opened,clicked,open_rate,click_rate,open_tracking,click_tracking");
        Console.WriteLine($"{stats.Starting:yyyy-MM-dd},{stats.Ending:yyyy-MM-dd},{stats.Sent},{stats.Opened},{stats.Clicked},{stats.OpenRate:F2},{stats.ClickRate:F2},{stats.OpenTrackingEnabled},{stats.ClickTrackingEnabled}");
    }
}

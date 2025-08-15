using KitCLI.Services;

namespace KitCLI.Commands;

/// <summary>
/// Handles self-update functionality for the CLI
/// </summary>
public static class UpdateCommand
{
    public static async Task<int> HandleUpdate(string[] args, string currentVersion)
    {
        var checkOnly = false;
        var force = false;

        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--check":
                case "-c":
                    checkOnly = true;
                    break;
                case "--force":
                case "-f":
                    force = true;
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;
            }
        }

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        var updateService = new UpdateService(httpClient, currentVersion);

        Console.WriteLine("🔍 Checking for updates...");
        var update = await updateService.CheckForUpdateAsync();

        if (update == null)
        {
            Console.WriteLine($"✓ You're running the latest version (v{currentVersion})");
            return 0;
        }

        // Display update information
        Console.WriteLine();
        Console.WriteLine($"📦 New version available: v{update.Version}");
        Console.WriteLine($"   Current version: v{currentVersion}");
        Console.WriteLine($"   Download size: {update.GetFormattedSize()}");
        Console.WriteLine($"   Published: {update.PublishedAt:yyyy-MM-dd}");

        if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
        {
            Console.WriteLine();
            Console.WriteLine("📝 Release notes:");
            var lines = update.ReleaseNotes.Split('\n');
            foreach (var line in lines.Take(10))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine($"   {line.Trim()}");
                }
            }
            if (lines.Length > 10)
            {
                Console.WriteLine("   ... (truncated)");
            }
        }

        if (checkOnly)
        {
            Console.WriteLine();
            Console.WriteLine("💡 Run 'kit update' to install this version");
            return 0;
        }

        // Check if we have permissions to update
        if (!SelfUpdater.CanUpdate())
        {
            Console.WriteLine();
            Console.Error.WriteLine("❌ Cannot update: insufficient permissions");
            Console.Error.WriteLine("   Try running with elevated privileges (sudo on Unix, admin on Windows)");
            return 1;
        }

        // Prompt for confirmation unless forced
        if (!force)
        {
            Console.WriteLine();
            Console.Write("Do you want to update now? [y/N]: ");
            var response = Console.ReadLine();
            if (response?.Trim().ToLowerInvariant() != "y")
            {
                Console.WriteLine("Update cancelled.");
                return 0;
            }
        }

        // Download the update
        Console.WriteLine();
        Console.WriteLine($"📥 Downloading v{update.Version}...");

        var progress = new Progress<double>(percent =>
        {
            Console.Write($"\r   Progress: {percent:F1}%");
        });

        var downloadedData = await updateService.DownloadUpdateAsync(update.DownloadUrl, progress);
        Console.WriteLine(); // New line after progress

        if (downloadedData == null || downloadedData.Length == 0)
        {
            Console.Error.WriteLine("❌ Download failed");
            return 1;
        }

        Console.WriteLine($"✓ Downloaded {update.GetFormattedSize()}");

        // Perform the update
        Console.WriteLine("🔄 Installing update...");
        var updater = new SelfUpdater();
        var success = await updater.PerformUpdateAsync(update, downloadedData);

        if (!success)
        {
            Console.Error.WriteLine("❌ Update installation failed");
            return 1;
        }

        // If we get here, the update process should have started
        // and this process should be exiting
        return 0;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage: kit update [options]");
        Console.WriteLine();
        Console.WriteLine("Check for and install updates to Kit CLI");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --check, -c     Check for updates without installing");
        Console.WriteLine("  --force, -f     Install update without confirmation");
        Console.WriteLine("  --help, -h      Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  kit update              Check and install updates");
        Console.WriteLine("  kit update --check      Check for updates only");
        Console.WriteLine("  kit update --force      Install updates without prompting");
    }
}


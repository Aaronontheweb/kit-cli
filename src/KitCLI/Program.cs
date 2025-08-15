using System.Reflection;
using KitCLI.Services;
using KitCLI.Models;
using KitCLI.Helpers;
using KitCLI.Commands;

// Check for special flags
bool isReadOnly = false;
bool isVerbose = false;
var argsList = args.ToList();

// Check for read-only mode
if (argsList.Contains("--read-only") || argsList.Contains("-ro"))
{
    isReadOnly = true;
    argsList.RemoveAll(a => a == "--read-only" || a == "-ro");
}

// Check for verbose mode
if (argsList.Contains("--verbose") || argsList.Contains("-V"))
{
    isVerbose = true;
    argsList.RemoveAll(a => a == "--verbose" || a == "-V");
    Environment.SetEnvironmentVariable("KIT_CLI_VERBOSE", "1");
}

args = argsList.ToArray();

if (args.Length == 0)
{
    ShowHelp();
    return 0;
}

// Get version information from assembly
var assembly = Assembly.GetExecutingAssembly();
var version = assembly.GetName().Version?.ToString() ?? "0.1.0";
var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

// Handle special flags
if (args[0] == "--version" || args[0] == "-v")
{
    Console.WriteLine($"Kit CLI v{informationalVersion}");
    Console.WriteLine("Built with .NET 9 AOT compilation");
    Console.WriteLine();
    Console.WriteLine("Email marketing analytics for Kit (formerly ConvertKit)");
    Console.WriteLine("https://github.com/stannardlabs/kit-cli");
    return 0;
}

if (args[0] == "--test-aot")
{
    Console.WriteLine("AOT compilation test successful!");
    Console.WriteLine($"Binary: kit");
    Console.WriteLine($"Version: {informationalVersion}");
    Console.WriteLine($"Runtime: .NET 9 AOT");
    return 0;
}

if (args[0] == "--help" || args[0] == "-h")
{
    ShowHelp();
    return 0;
}

// Route to appropriate command handler
try
{
    if (isReadOnly)
    {
        Console.WriteLine("🔒 Running in READ-ONLY mode. All write operations are disabled.");
    }
    if (isVerbose)
    {
        Console.WriteLine("🔍 Verbose mode enabled. Detailed logging will be shown.");
    }
    return await RouteCommand(args, isReadOnly, isVerbose);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void ShowHelp()
{
    Console.WriteLine("Kit CLI - Command-line interface for Kit email marketing platform");
    Console.WriteLine();
    Console.WriteLine("Usage: kit <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  config set        Set configuration values");
    Console.WriteLine("  config get        Get current configuration");
    Console.WriteLine("  config test       Test connection to Kit API");
    Console.WriteLine();
    Console.WriteLine("  subscriber list   List subscribers with filters");
    Console.WriteLine("  subscriber get    Get subscriber details");
    Console.WriteLine("  subscriber search Search subscribers");
    Console.WriteLine("  subscriber export Export subscribers to file");
    Console.WriteLine();
    Console.WriteLine("  subscribers date-range    Find by date range");
    Console.WriteLine("  subscribers inactive      Find inactive users");
    Console.WriteLine("  subscribers unsubscribed  Find unsubscribed");
    Console.WriteLine();
    Console.WriteLine("  broadcast list    List broadcasts");
    Console.WriteLine("  broadcast get     Get broadcast details");
    Console.WriteLine("  broadcast stats   Get broadcast statistics");
    Console.WriteLine("  broadcast opened  Get subscribers who opened");
    Console.WriteLine("  broadcast clicked Get subscribers who clicked");
    Console.WriteLine("  broadcast export  Export broadcasts to file");
    Console.WriteLine();
    Console.WriteLine("  campaign compare  Compare campaign performance");
    Console.WriteLine();
    Console.WriteLine("  tag list          List all tags");
    Console.WriteLine("  tag subscribers   Get subscribers for a tag");
    Console.WriteLine("  tag export        Export tags to file");
    Console.WriteLine();
    Console.WriteLine("  segment list      List all segments");
    Console.WriteLine("  segment get       Get segment details");
    Console.WriteLine("  segment analyze   Analyze segment composition");
    Console.WriteLine("  segment compare   Compare two segments");
    Console.WriteLine();
    Console.WriteLine("  sequence list     List all sequences (automations)");
    Console.WriteLine("  sequence emails   View emails in a sequence");
    Console.WriteLine("  sequence stats    Get sequence performance");
    Console.WriteLine("  sequence analyze  Analyze sequence effectiveness");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --version, -v     Show version information");
    Console.WriteLine("  --help, -h        Show this help message");
    Console.WriteLine("  --read-only, -ro  Run in read-only mode (no writes)");
    Console.WriteLine("  --verbose, -V     Enable verbose output for debugging");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  kit config set --api-key YOUR_API_KEY");
    Console.WriteLine("  kit subscriber list --status cancelled --export unsubscribed.csv");
    Console.WriteLine("  kit broadcast stats 12345");
    Console.WriteLine();
    Console.WriteLine("For more information, visit: https://github.com/stannardlabs/kit-cli");
}

static async Task<int> RouteCommand(string[] args, bool isReadOnly = false, bool isVerbose = false)
{
    if (args.Length < 1)
    {
        ShowHelp();
        return 1;
    }

    return args[0].ToLowerInvariant() switch
    {
        "config" => await HandleConfigCommand(args[1..], isReadOnly),
        "subscriber" => await HandleSubscriberCommand(args[1..], isReadOnly),
        "subscribers" => await HandleSubscribersCommand(args[1..], isReadOnly),
        "broadcast" => await HandleBroadcastCommand(args[1..], isReadOnly),
        "campaign" => await HandleCampaignCommand(args[1..], isReadOnly),
        "tag" => await HandleTagCommand(args[1..], isReadOnly),
        "segment" => await HandleSegmentCommand(args[1..], isReadOnly),
        "sequence" => await HandleSequenceCommand(args[1..], isReadOnly),
        _ => ShowUnknownCommand(args[0])
    };
}

static int ShowUnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    Console.Error.WriteLine("Run 'kit --help' for usage information.");
    return 1;
}

static int ShowReadOnlyError(string operation)
{
    Console.Error.WriteLine($"❌ Operation '{operation}' is not allowed in read-only mode.");
    Console.Error.WriteLine("Remove --read-only flag to perform write operations.");
    return 1;
}

static async Task<int> HandleConfigCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit config <subcommand>");
        Console.WriteLine("  set     Set configuration values");
        Console.WriteLine("  get     Get current configuration");
        Console.WriteLine("  test    Test connection to Kit API");
        return 1;
    }

    var configService = new ConfigurationService();

    return args[0].ToLowerInvariant() switch
    {
        "set" => isReadOnly ? ShowReadOnlyError("config set") : await HandleConfigSet(args[1..], configService),
        "get" => await HandleConfigGet(configService),
        "test" => await HandleConfigTest(configService),
        _ => ShowUnknownCommand($"config {args[0]}")
    };
}

static async Task<int> HandleConfigSet(string[] args, ConfigurationService configService)
{
    string? apiKey = null;
    string? profile = null;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--api-key":
            case "-k":
                if (i + 1 < args.Length)
                {
                    apiKey = args[++i];
                }

                break;
            case "--profile":
            case "-p":
                if (i + 1 < args.Length)
                {
                    profile = args[++i];
                }

                break;
        }
    }

    if (string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine("Usage: kit config set [options]");
        Console.WriteLine("Options:");
        Console.WriteLine("  --api-key, -k <key>      Kit API key");
        Console.WriteLine("  --profile, -p <profile>  Configuration profile name");
        return 1;
    }

    var config = new KitConfig
    {
        ApiKey = apiKey,
        ApiVersion = "v4"
    };

    await configService.SaveConfigAsync(config, profile);
    Console.WriteLine($"✓ Configuration saved for profile: {profile ?? "default"}");
    return 0;
}

static async Task<int> HandleConfigGet(ConfigurationService configService)
{
    var config = await configService.LoadConfigAsync();

    if (config == null)
    {
        Console.WriteLine("No configuration found. Use 'kit config set' to configure.");
        return 1;
    }

    Console.WriteLine($"API Key: {config.GetMaskedApiKey()}");
    Console.WriteLine($"API Version: {config.ApiVersion}");
    Console.WriteLine($"Base URL: {config.BaseUrl}");
    Console.WriteLine($"Config Path: {configService.GetConfigPath()}");
    return 0;
}

static async Task<int> HandleConfigTest(ConfigurationService configService)
{
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    Console.WriteLine($"Testing connection to {config.BaseUrl}...");

    using var client = new KitApiClient(config);
    var success = await client.TestConnectionAsync();

    if (success)
    {
        Console.WriteLine("✓ Connection successful!");
        return 0;
    }
    else
    {
        Console.WriteLine("✗ Connection failed. Please check your API key.");
        return 1;
    }
}

static async Task<int> HandleSubscriberCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit subscriber <subcommand>");
        Console.WriteLine("  list      List subscribers");
        Console.WriteLine("  get       Get subscriber details");
        Console.WriteLine("  search    Search subscribers");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "list" => await SubscriberCommands.HandleList(args[1..], client),
        "get" => await SubscriberCommands.HandleGet(args[1..], client),
        "search" => await SubscriberCommands.HandleSearch(args[1..], client),
        "export" => await SubscriberCommands.HandleExport(args[1..], client),
        _ => ShowUnknownCommand($"subscriber {args[0]}")
    };
}

static async Task<int> HandleBroadcastCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit broadcast <subcommand>");
        Console.WriteLine("  list      List broadcasts");
        Console.WriteLine("  get       Get broadcast details");
        Console.WriteLine("  stats     Get broadcast statistics");
        Console.WriteLine("  opened    List subscribers who opened");
        Console.WriteLine("  clicked   List subscribers who clicked");
        Console.WriteLine("  unopened  List subscribers who didn't open");
        Console.WriteLine("  export    Export broadcasts to file");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "list" => await BroadcastCommands.HandleList(args[1..], client),
        "get" => await BroadcastCommands.HandleGet(args[1..], client),
        "stats" => await BroadcastCommands.HandleStats(args[1..], client),
        "opened" => await BroadcastCommands.HandleOpened(args[1..], client),
        "clicked" => await BroadcastCommands.HandleClicked(args[1..], client),
        "unopened" => await BroadcastCommands.HandleUnopened(args[1..], client),
        "export" => await BroadcastCommands.HandleExport(args[1..], client),
        _ => ShowUnknownCommand($"broadcast {args[0]}")
    };
}

static async Task<int> HandleSubscribersCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit subscribers <subcommand>");
        Console.WriteLine("  date-range    Find subscribers by date range");
        Console.WriteLine("  inactive      Find inactive subscribers");
        Console.WriteLine("  unsubscribed  Find unsubscribed users");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "date-range" => await AdvancedFilteringCommands.HandleSubscribersByDateRange(args[1..], client),
        "inactive" => await AdvancedFilteringCommands.HandleInactiveSubscribers(args[1..], client),
        "unsubscribed" => await AdvancedFilteringCommands.HandleBulkUnsubscribed(args[1..], client),
        _ => ShowUnknownCommand($"subscribers {args[0]}")
    };
}

static async Task<int> HandleCampaignCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit campaign <subcommand>");
        Console.WriteLine("  compare    Compare two campaigns");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "compare" => await AdvancedFilteringCommands.HandleCampaignComparison(args[1..], client),
        _ => ShowUnknownCommand($"campaign {args[0]}")
    };
}

static async Task<int> HandleTagCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit tag <subcommand>");
        Console.WriteLine("  list         List all tags");
        Console.WriteLine("  subscribers  Get subscribers for a tag");
        Console.WriteLine("  export       Export tags to file");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "list" => await TagCommands.HandleList(args[1..], client),
        "subscribers" => await TagCommands.HandleSubscribers(args[1..], client),
        "export" => await TagCommands.HandleExport(args[1..], client),
        _ => ShowUnknownCommand($"tag {args[0]}")
    };
}

static async Task<int> HandleSequenceCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit sequence <subcommand>");
        Console.WriteLine("  list         List all sequences");
        Console.WriteLine("  get          Get sequence details");
        Console.WriteLine("  emails       List emails in sequence");
        Console.WriteLine("  subscribers  Get subscribers in sequence");
        Console.WriteLine("  stats        Get sequence statistics");
        Console.WriteLine("  analyze      Analyze sequence performance");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "list" => await SequenceCommands.HandleList(args[1..], client),
        "get" => await SequenceCommands.HandleGet(args[1..], client),
        "emails" => await SequenceCommands.HandleEmails(args[1..], client),
        "subscribers" => await SequenceCommands.HandleSubscribers(args[1..], client),
        "stats" => await SequenceCommands.HandleStats(args[1..], client),
        "analyze" => await SequenceCommands.HandleAnalyze(args[1..], client),
        _ => ShowUnknownCommand($"sequence {args[0]}")
    };
}

static async Task<int> HandleSegmentCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit segment <subcommand>");
        Console.WriteLine("  list         List all segments");
        Console.WriteLine("  get          Get segment details");
        Console.WriteLine("  subscribers  Get subscribers in segment");
        Console.WriteLine("  analyze      Analyze segment composition");
        Console.WriteLine("  compare      Compare two segments");
        Console.WriteLine("  export       Export segments to file");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    using var client = new KitApiClient(config);

    return args[0].ToLowerInvariant() switch
    {
        "list" => await SegmentCommands.HandleList(args[1..], client),
        "get" => await SegmentCommands.HandleGet(args[1..], client),
        "subscribers" => await SegmentCommands.HandleSubscribers(args[1..], client),
        "analyze" => await SegmentCommands.HandleAnalyze(args[1..], client),
        "compare" => await SegmentCommands.HandleCompare(args[1..], client),
        "export" => await SegmentCommands.HandleExport(args[1..], client),
        _ => ShowUnknownCommand($"segment {args[0]}")
    };
}


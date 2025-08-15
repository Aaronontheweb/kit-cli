using System.Reflection;
using KitCLI.Services;
using KitCLI.Models;

// Check for read-only mode flag
bool isReadOnly = false;
var argsList = args.ToList();
if (argsList.Contains("--read-only") || argsList.Contains("-ro"))
{
    isReadOnly = true;
    argsList.RemoveAll(a => a == "--read-only" || a == "-ro");
    args = argsList.ToArray();
}

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
    return await RouteCommand(args, isReadOnly);
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
    Console.WriteLine();
    Console.WriteLine("  broadcast list    List broadcasts");
    Console.WriteLine("  broadcast get     Get broadcast details");
    Console.WriteLine("  broadcast stats   Get broadcast statistics");
    Console.WriteLine();
    Console.WriteLine("  tag list          List all tags");
    Console.WriteLine("  export            Export data to file");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --version, -v     Show version information");
    Console.WriteLine("  --help, -h        Show this help message");
    Console.WriteLine("  --read-only, -ro  Run in read-only mode (no writes)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  kit config set --api-key YOUR_API_KEY");
    Console.WriteLine("  kit subscriber list --status cancelled --export unsubscribed.csv");
    Console.WriteLine("  kit broadcast stats 12345");
    Console.WriteLine();
    Console.WriteLine("For more information, visit: https://github.com/stannardlabs/kit-cli");
}

static async Task<int> RouteCommand(string[] args, bool isReadOnly = false)
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
        "broadcast" => await HandleBroadcastCommand(args[1..], isReadOnly),
        "tag" => await HandleTagCommand(args[1..], isReadOnly),
        "export" => await HandleExportCommand(args[1..]),
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
                    apiKey = args[++i];
                break;
            case "--profile":
            case "-p":
                if (i + 1 < args.Length)
                    profile = args[++i];
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
    
    // TODO: Implement actual API test once KitApiClient is created
    Console.WriteLine("⚠️  API client not yet implemented. Connection test pending.");
    
    return 0;
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

    // TODO: Implement subscriber commands
    Console.WriteLine($"⚠️  Subscriber command '{args[0]}' not yet implemented.");
    return 0;
}

static async Task<int> HandleBroadcastCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit broadcast <subcommand>");
        Console.WriteLine("  list      List broadcasts");
        Console.WriteLine("  get       Get broadcast details");
        Console.WriteLine("  stats     Get broadcast statistics");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    // TODO: Implement broadcast commands
    Console.WriteLine($"⚠️  Broadcast command '{args[0]}' not yet implemented.");
    return 0;
}

static async Task<int> HandleTagCommand(string[] args, bool isReadOnly)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit tag <subcommand>");
        Console.WriteLine("  list      List all tags");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();

    if (config == null || !config.IsValid)
    {
        Console.WriteLine("Invalid or missing configuration. Use 'kit config set' to configure.");
        return 1;
    }

    // TODO: Implement tag commands
    Console.WriteLine($"⚠️  Tag command '{args[0]}' not yet implemented.");
    return 0;
}

static async Task<int> HandleExportCommand(string[] args)
{
    if (args.Length < 1)
    {
        Console.WriteLine("Usage: kit export <type> [options]");
        Console.WriteLine("Types:");
        Console.WriteLine("  subscribers       Export subscribers to file");
        Console.WriteLine("  broadcasts        Export broadcasts to file");
        return 1;
    }

    var configService = new ConfigurationService();
    var config = await configService.LoadConfigAsync();
    
    if (config == null || !config.IsValid)
    {
        Console.WriteLine("No valid configuration found. Run 'kit config set' first.");
        return 1;
    }

    // TODO: Implement export commands
    Console.WriteLine($"⚠️  Export command '{args[0]}' not yet implemented.");
    return 0;
}